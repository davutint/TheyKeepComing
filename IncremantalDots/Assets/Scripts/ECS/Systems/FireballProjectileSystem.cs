using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    /// <summary>
    /// Ates Topu mermisini hedefe tasir (polish). Varista FireballStrike uretir — hasar,
    /// patlama SFX'i ve alan mantigi mevcut FireballStrikeSystem'de kalir (tek sorumluluk).
    /// Rotasyon ucus yonune yazilir; Mono gorsel (SpellCastUI) pozisyon+rotasyonu kopyalar.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ArrowMoveSystem))]
    [UpdateBefore(typeof(ArrowHitSystem))]
    public partial struct FireballProjectileSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FireballProjectile>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<GameStateData>())
            {
                var gameState = SystemAPI.GetSingleton<GameStateData>();
                if (gameState.IsGameOver || gameState.IsLevelUpPending)
                    return;
            }

            float dt = SystemAPI.Time.DeltaTime;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (transform, projectile, entity) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<FireballProjectile>>().WithEntityAccess())
            {
                float2 position = transform.ValueRO.Position.xy;
                float2 toTarget = projectile.ValueRO.Target - position;
                float distance = math.length(toTarget);
                float step = math.max(0.01f, projectile.ValueRO.Speed) * dt;

                if (distance <= step)
                {
                    // Varis: hasari mevcut strike kanalina devret (SFX dahil), mermiyi sil
                    var strike = ecb.CreateEntity();
                    ecb.AddComponent(strike, new FireballStrike
                    {
                        Position = projectile.ValueRO.Target,
                        Radius = projectile.ValueRO.Radius,
                        Damage = projectile.ValueRO.Damage,
                        Kind = FireballStrikeKind.Primary,
                        Evolutions = projectile.ValueRO.Evolutions
                    });
                    ecb.DestroyEntity(entity);
                    continue;
                }

                float2 direction = toTarget / distance;
                position += direction * step;
                transform.ValueRW.Position = new float3(position.x, position.y, MobileCastleRenderDepth.ProjectileZ);
                transform.ValueRW.Rotation = quaternion.RotateZ(math.atan2(direction.y, direction.x));
            }
        }
    }
}
