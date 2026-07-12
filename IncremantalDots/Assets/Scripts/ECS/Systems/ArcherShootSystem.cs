using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WaveSpawnSystem))]
    [UpdateBefore(typeof(ApplyMovementForceSystem))]
    public partial struct ArcherShootSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArrowPrefabData>();
            state.RequireForUpdate<GameStateData>();
            state.RequireForUpdate<ArrowSupply>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.IsGameOver || gameState.IsLevelUpPending)
                return;

            float dt = SystemAPI.Time.DeltaTime;
            var arrowPrefab = SystemAPI.GetSingleton<ArrowPrefabData>().ArrowPrefab;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var arrowSupplyRW = SystemAPI.GetSingletonRW<ArrowSupply>();
            bool unlimitedArrows = SystemAPI.HasSingleton<MobileCastleCombatConfig>()
                && SystemAPI.GetSingleton<MobileCastleCombatConfig>().UnlimitedArrows;
            float fireRateMultiplier = 1f;
            var poolMemberLookup = SystemAPI.GetComponentLookup<EnemyPoolMember>(true);
            if (SystemAPI.HasSingleton<CastleYardPrepState>())
            {
                var prep = SystemAPI.GetSingleton<CastleYardPrepState>();
                if (prep.RallyTimer > 0f)
                    fireRateMultiplier = math.max(0.01f, prep.RallyFireRateMultiplier);
            }

            foreach (var (archer, archerTransform) in
                SystemAPI.Query<RefRW<ArcherUnit>, RefRO<LocalTransform>>())
            {
                archer.ValueRW.FireTimer -= dt;
                if (archer.ValueRO.FireTimer > 0f)
                    continue;

                float3 archerPos = archerTransform.ValueRO.Position;
                float rangeSq = archer.ValueRO.Range * archer.ValueRO.Range;

                Entity closestZombie = Entity.Null;
                float closestDistSq = float.MaxValue;
                float3 closestZombiePosition = float3.zero;

                // Brute-force — ~10 okcu x 6000 zombi = 60K distance check, Burst ile trivial
                // Spatial hash 3721 hucre taramasi bundan DAHA YAVAS
                foreach (var (zombieState, zombieTransform, zombieEntity) in
                    SystemAPI.Query<RefRO<ZombieState>, RefRO<LocalTransform>>()
                        .WithAll<ZombieTag>()
                        .WithNone<DeathTimer>()
                        .WithEntityAccess())
                {
                    if (zombieState.ValueRO.Value == ZombieStateType.Dead)
                        continue;

                    float distSq = math.distancesq(archerPos, zombieTransform.ValueRO.Position);
                    if (distSq < closestDistSq && distSq <= rangeSq)
                    {
                        closestDistSq = distSq;
                        closestZombie = zombieEntity;
                        closestZombiePosition = zombieTransform.ValueRO.Position;
                    }
                }

                if (closestZombie == Entity.Null)
                    continue;

                // Ok kontrolu — ok yoksa ates etme
                if (!unlimitedArrows)
                {
                    if (arrowSupplyRW.ValueRO.Current <= 0)
                        break;

                    arrowSupplyRW.ValueRW.Current--;
                }
                float effectiveFireRate = math.max(0.01f, archer.ValueRO.FireRate * fireRateMultiplier);
                archer.ValueRW.FireTimer = 1f / effectiveFireRate;
                float2 facingDirection = ResolveFacingDirection(
                    closestZombiePosition.xy - archerPos.xy,
                    archer.ValueRO.FacingDirection);
                archer.ValueRW.FacingDirection = facingDirection;
                archer.ValueRW.AttackAnimTimer = GetAttackAnimDuration(effectiveFireRate);

                var arrow = ecb.Instantiate(arrowPrefab);
                float3 arrowPos = new float3(archerPos.x, archerPos.y, MobileCastleRenderDepth.ProjectileZ);
                ecb.SetComponent(arrow, LocalTransform.FromPosition(arrowPos));
                ecb.SetComponent(arrow, new ArrowProjectile
                {
                    Speed = 12f,
                    Damage = archer.ValueRO.ArrowDamage,
                    Target = closestZombie,
                    TargetPoolGeneration = poolMemberLookup.HasComponent(closestZombie)
                        ? poolMemberLookup[closestZombie].Generation
                        : 0u,
                    ArcherType = archer.ValueRO.Type,
                    SlowDuration = archer.ValueRO.SlowDuration,
                    SlowMultiplier = archer.ValueRO.SlowMultiplier
                });
                ecb.SetComponent(arrow, new SpriteTint
                {
                    Value = ArcherVisualStyle.GetTint(archer.ValueRO.Type)
                });

                // Shoot particle kapali; okun cikis ani su an sadece SFX ile okunuyor.
                var sfxEvent = ecb.CreateEntity();
                ecb.AddComponent(sfxEvent, new CombatSfxEvent
                {
                    Position = arrowPos,
                    Type = CombatSfxType.ArrowShoot,
                    Volume = 0.35f,
                    Pitch = 1f
                });
            }
        }

        private static float2 ResolveFacingDirection(float2 aimDirection, float2 fallback)
        {
            if (math.lengthsq(aimDirection) > 0.0001f)
                return math.normalize(aimDirection);

            if (math.lengthsq(fallback) > 0.0001f)
                return math.normalize(fallback);

            return new float2(1f, 0f);
        }

        private static float GetAttackAnimDuration(float fireRate)
        {
            float shotInterval = 1f / math.max(0.01f, fireRate);
            return math.clamp(shotInterval * 0.75f, 0.18f, 0.45f);
        }
    }
}
