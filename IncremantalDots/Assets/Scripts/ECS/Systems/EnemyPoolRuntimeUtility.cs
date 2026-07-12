using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    public static class EnemyPoolRuntimeUtility
    {
        public static bool EnsureInitialized(EntityManager entityManager, Entity poolEntity)
        {
            if (!IsValidPoolOwner(entityManager, poolEntity))
                return false;

            var state = entityManager.GetComponentData<EnemyPoolRuntimeData>(poolEntity);
            if (state.Initialized != 0)
                return true;

            var entries = entityManager.GetBuffer<EnemyCatalogEntryData>(poolEntity, true);
            int activeIndex = EnemyCatalogRuntimeUtility.ResolveActiveIndex(
                entityManager.GetComponentData<EnemyCatalogRuntimeData>(poolEntity), entries.Length);
            if (activeIndex < 0 || entries[activeIndex].Prefab == Entity.Null)
                return false;

            state.ActiveEntryIndex = activeIndex;
            state.PrewarmTarget = math.max(0, entries[activeIndex].PoolPrewarm);
            state.ExpandBatch = math.max(1, entries[activeIndex].PoolExpandBatch);
            state.Initialized = 1;
            entityManager.SetComponentData(poolEntity, state);

            if (state.PrewarmTarget > 0)
                Expand(entityManager, poolEntity, state.PrewarmTarget, false);

            return true;
        }

        public static bool TryRent(EntityManager entityManager, Entity poolEntity, out Entity entity)
        {
            entity = Entity.Null;
            if (!EnsureInitialized(entityManager, poolEntity))
                return false;

            var available = entityManager.GetBuffer<EnemyPoolAvailable>(poolEntity);
            if (!TryPopValid(entityManager, available, out entity))
            {
                var stateBeforeExpand = entityManager.GetComponentData<EnemyPoolRuntimeData>(poolEntity);
                Expand(entityManager, poolEntity, math.max(1, stateBeforeExpand.ExpandBatch), true);
                available = entityManager.GetBuffer<EnemyPoolAvailable>(poolEntity);
                if (!TryPopValid(entityManager, available, out entity))
                    return false;
            }

            ResetTransientState(entityManager, entity, false);
            var member = entityManager.GetComponentData<EnemyPoolMember>(entity);
            member.Generation = NextGeneration(member.Generation);
            entityManager.SetComponentData(entity, member);
            entityManager.SetComponentEnabled<ZombieTag>(entity, true);

            var state = entityManager.GetComponentData<EnemyPoolRuntimeData>(poolEntity);
            state.AvailableCount = available.Length;
            state.ActiveCount++;
            state.TotalRentCount++;
            entityManager.SetComponentData(poolEntity, state);
            return true;
        }

        public static bool Return(EntityManager entityManager, Entity poolEntity, Entity entity)
        {
            if (!IsValidPoolOwner(entityManager, poolEntity)
                || entity == Entity.Null
                || !entityManager.Exists(entity)
                || !entityManager.HasComponent<EnemyPoolMember>(entity)
                || !entityManager.HasComponent<ZombieTag>(entity)
                || !entityManager.IsComponentEnabled<ZombieTag>(entity))
                return false;

            ResetTransientState(entityManager, entity, true);
            entityManager.SetComponentEnabled<ZombieTag>(entity, false);

            var available = entityManager.GetBuffer<EnemyPoolAvailable>(poolEntity);
            available.Add(new EnemyPoolAvailable { Entity = entity });

            var state = entityManager.GetComponentData<EnemyPoolRuntimeData>(poolEntity);
            state.AvailableCount = available.Length;
            state.ActiveCount = math.max(0, state.ActiveCount - 1);
            state.TotalReturnCount++;
            entityManager.SetComponentData(poolEntity, state);
            return true;
        }

        public static int ReturnAllActive(EntityManager entityManager, Entity poolEntity)
        {
            if (!IsValidPoolOwner(entityManager, poolEntity))
                return 0;

            var query = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<ZombieTag>(),
                    ComponentType.ReadOnly<EnemyPoolMember>()
                },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });

            int returned = 0;
            using var entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (Return(entityManager, poolEntity, entities[i]))
                    returned++;
            }
            query.Dispose();
            return returned;
        }

        public static uint GetGeneration(EntityManager entityManager, Entity entity)
        {
            return entity != Entity.Null
                && entityManager.Exists(entity)
                && entityManager.HasComponent<EnemyPoolMember>(entity)
                ? entityManager.GetComponentData<EnemyPoolMember>(entity).Generation
                : 0u;
        }

        private static void Expand(EntityManager entityManager, Entity poolEntity, int count, bool runtimeExpansion)
        {
            if (count <= 0)
                return;

            var state = entityManager.GetComponentData<EnemyPoolRuntimeData>(poolEntity);
            var entries = entityManager.GetBuffer<EnemyCatalogEntryData>(poolEntity, true);
            if (state.ActiveEntryIndex < 0 || state.ActiveEntryIndex >= entries.Length)
                return;

            Entity prefab = entries[state.ActiveEntryIndex].Prefab;
            if (prefab == Entity.Null || !entityManager.Exists(prefab))
                return;

            using var created = entityManager.Instantiate(prefab, count, Allocator.Temp);
            for (int i = 0; i < created.Length; i++)
            {
                Entity entity = created[i];
                if (entityManager.HasComponent<EnemyPoolMember>(entity))
                {
                    entityManager.SetComponentData(entity, new EnemyPoolMember
                    {
                        CatalogEntryIndex = state.ActiveEntryIndex,
                        Generation = 0u
                    });
                }
                else
                {
                    entityManager.AddComponentData(entity, new EnemyPoolMember
                    {
                        CatalogEntryIndex = state.ActiveEntryIndex,
                        Generation = 0u
                    });
                }

                ResetTransientState(entityManager, entity, true);
                entityManager.SetComponentEnabled<ZombieTag>(entity, false);
            }

            // EnemyPoolMember eklemek structural change'tir; buffer handle'i ancak
            // tum entity archetype degisiklikleri bittikten sonra alinmalidir.
            var available = entityManager.GetBuffer<EnemyPoolAvailable>(poolEntity);
            for (int i = 0; i < created.Length; i++)
                available.Add(new EnemyPoolAvailable { Entity = created[i] });

            state.TotalCreated += created.Length;
            state.AvailableCount = available.Length;
            if (runtimeExpansion)
                state.ExpansionCount++;
            entityManager.SetComponentData(poolEntity, state);
        }

        private static bool TryPopValid(EntityManager entityManager,
            DynamicBuffer<EnemyPoolAvailable> available, out Entity entity)
        {
            while (available.Length > 0)
            {
                int index = available.Length - 1;
                entity = available[index].Entity;
                available.RemoveAt(index);
                if (entity != Entity.Null
                    && entityManager.Exists(entity)
                    && entityManager.HasComponent<EnemyPoolMember>(entity)
                    && entityManager.HasComponent<ZombieTag>(entity)
                    && !entityManager.IsComponentEnabled<ZombieTag>(entity))
                    return true;
            }

            entity = Entity.Null;
            return false;
        }

        private static void ResetTransientState(EntityManager entityManager, Entity entity, bool hide)
        {
            if (entityManager.HasComponent<ZombieState>(entity))
                entityManager.SetComponentData(entity, new ZombieState { Value = ZombieStateType.Moving });

            if (entityManager.HasComponent<ZombieStats>(entity))
            {
                var stats = entityManager.GetComponentData<ZombieStats>(entity);
                stats.CurrentHP = stats.MaxHP;
                stats.AttackTimer = 0f;
                entityManager.SetComponentData(entity, stats);
            }

            if (entityManager.HasComponent<ZombieSlow>(entity))
            {
                entityManager.SetComponentData(entity, new ZombieSlow { Duration = 0f, SpeedMultiplier = 1f });
                entityManager.SetComponentEnabled<ZombieSlow>(entity, false);
            }

            if (entityManager.HasComponent<DeathTimer>(entity))
            {
                entityManager.SetComponentData(entity, new DeathTimer { Value = 0f });
                entityManager.SetComponentEnabled<DeathTimer>(entity, false);
            }

            if (entityManager.HasComponent<PhysicsBody>(entity))
            {
                var body = entityManager.GetComponentData<PhysicsBody>(entity);
                body.Velocity = float2.zero;
                body.Force = float2.zero;
                entityManager.SetComponentData(entity, body);
            }

            if (entityManager.HasComponent<SpriteTint>(entity))
                entityManager.SetComponentData(entity, new SpriteTint { Value = ArcherVisualStyle.NormalTint });

            if (entityManager.HasComponent<SpriteAnimation>(entity))
            {
                var animation = entityManager.GetComponentData<SpriteAnimation>(entity);
                animation.DirectionRow = 0;
                animation.FrameCount = 15;
                animation.CurrentFrame = 0;
                animation.FrameTimer = 0f;
                entityManager.SetComponentData(entity, animation);
            }

            if (hide && entityManager.HasComponent<LocalTransform>(entity))
                entityManager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(
                    float3.zero, quaternion.identity, 0f));
        }

        private static bool IsValidPoolOwner(EntityManager entityManager, Entity poolEntity)
        {
            return poolEntity != Entity.Null
                && entityManager.Exists(poolEntity)
                && entityManager.HasComponent<EnemyPoolRuntimeData>(poolEntity)
                && entityManager.HasBuffer<EnemyPoolAvailable>(poolEntity)
                && entityManager.HasBuffer<EnemyCatalogEntryData>(poolEntity)
                && entityManager.HasComponent<EnemyCatalogRuntimeData>(poolEntity);
        }

        private static uint NextGeneration(uint generation)
        {
            generation++;
            return generation == 0u ? 1u : generation;
        }
    }
}
