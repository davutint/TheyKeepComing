using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    public static class ArrowPoolRuntimeUtility
    {
        public static bool EnsureInitialized(EntityManager entityManager, Entity poolEntity, Entity arrowPrefab)
        {
            if (!IsValidPoolOwner(entityManager, poolEntity)
                || !IsValidArrowPrefab(entityManager, arrowPrefab))
                return false;

            var state = entityManager.GetComponentData<ArrowPoolRuntimeData>(poolEntity);
            if (state.Initialized != 0)
                return true;

            state.Initialized = 1;
            state.ExpandRequested = 0;
            entityManager.SetComponentData(poolEntity, state);

            if (state.PrewarmTarget > 0)
                Expand(entityManager, poolEntity, arrowPrefab, state.PrewarmTarget, false);

            return true;
        }

        public static bool Maintain(EntityManager entityManager, Entity poolEntity, Entity arrowPrefab)
        {
            if (!EnsureInitialized(entityManager, poolEntity, arrowPrefab))
                return false;

            ReconcileDeferredReturns(entityManager, poolEntity);
            var state = entityManager.GetComponentData<ArrowPoolRuntimeData>(poolEntity);
            if (state.ExpandRequested == 0)
                return true;

            state.ExpandRequested = 0;
            entityManager.SetComponentData(poolEntity, state);
            Expand(entityManager, poolEntity, arrowPrefab, math.max(1, state.ExpandBatch), true);
            return true;
        }

        public static bool TryRent(EntityManager entityManager, Entity poolEntity,
            Entity arrowPrefab, out Entity entity)
        {
            entity = Entity.Null;
            if (!EnsureInitialized(entityManager, poolEntity, arrowPrefab))
                return false;

            ReconcileDeferredReturns(entityManager, poolEntity);
            var available = entityManager.GetBuffer<ArrowPoolAvailable>(poolEntity);
            if (!TryPopValid(entityManager, available, out entity))
            {
                var stateBeforeExpand = entityManager.GetComponentData<ArrowPoolRuntimeData>(poolEntity);
                Expand(entityManager, poolEntity, arrowPrefab,
                    math.max(1, stateBeforeExpand.ExpandBatch), true);
                available = entityManager.GetBuffer<ArrowPoolAvailable>(poolEntity);
                if (!TryPopValid(entityManager, available, out entity))
                    return false;
            }

            Activate(entityManager, poolEntity, arrowPrefab, entity, available.Length);
            return true;
        }

        public static bool Return(EntityManager entityManager, Entity poolEntity, Entity entity)
        {
            if (!IsValidPoolOwner(entityManager, poolEntity))
                return false;

            ReconcileDeferredReturns(entityManager, poolEntity);
            return ReturnInternal(entityManager, poolEntity, entity);
        }

        public static int ReturnAllActive(EntityManager entityManager, Entity poolEntity)
        {
            if (!IsValidPoolOwner(entityManager, poolEntity))
                return 0;

            ReconcileDeferredReturns(entityManager, poolEntity);
            using var query = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<ArrowTag>(),
                    ComponentType.ReadOnly<ArrowPoolMember>()
                },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });

            int returned = 0;
            using var entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (ReturnInternal(entityManager, poolEntity, entities[i]))
                    returned++;
            }

            return returned;
        }

        private static bool ReturnInternal(EntityManager entityManager, Entity poolEntity, Entity entity)
        {
            if (entity == Entity.Null
                || !entityManager.Exists(entity)
                || !entityManager.HasComponent<ArrowPoolMember>(entity)
                || !entityManager.HasComponent<ArrowTag>(entity)
                || !entityManager.IsComponentEnabled<ArrowTag>(entity))
                return false;

            ResetInactive(entityManager, entity);
            var available = entityManager.GetBuffer<ArrowPoolAvailable>(poolEntity);
            available.Add(new ArrowPoolAvailable { Entity = entity });

            var state = entityManager.GetComponentData<ArrowPoolRuntimeData>(poolEntity);
            state.AvailableCount = available.Length;
            state.ActiveCount = math.max(0, state.ActiveCount - 1);
            state.TotalReturnCount++;
            entityManager.SetComponentData(poolEntity, state);
            return true;
        }

        private static void ReconcileDeferredReturns(EntityManager entityManager, Entity poolEntity)
        {
            var state = entityManager.GetComponentData<ArrowPoolRuntimeData>(poolEntity);
            var available = entityManager.GetBuffer<ArrowPoolAvailable>(poolEntity);
            int previousAvailableCount = state.AvailableCount;

            for (int i = available.Length - 1; i >= 0; i--)
            {
                Entity entity = available[i].Entity;
                if (entity == Entity.Null
                    || !entityManager.Exists(entity)
                    || !entityManager.HasComponent<ArrowPoolMember>(entity)
                    || !entityManager.HasComponent<ArrowTag>(entity)
                    || entityManager.IsComponentEnabled<ArrowTag>(entity))
                    available.RemoveAt(i);
            }

            int deferredReturnCount = math.max(0, available.Length - previousAvailableCount);
            state.AvailableCount = available.Length;
            state.ActiveCount = math.max(0, state.ActiveCount - deferredReturnCount);
            state.TotalReturnCount += deferredReturnCount;
            entityManager.SetComponentData(poolEntity, state);
        }

        private static void Activate(EntityManager entityManager, Entity poolEntity,
            Entity arrowPrefab, Entity entity, int availableCount)
        {
            ArrowProjectile defaults = entityManager.GetComponentData<ArrowProjectile>(arrowPrefab);
            defaults.Target = Entity.Null;
            defaults.TargetPoolGeneration = 0u;
            defaults.RemainingLifetime = math.max(0.1f, defaults.RemainingLifetime);
            entityManager.SetComponentData(entity, defaults);

            if (entityManager.HasComponent<LocalTransform>(entity))
                entityManager.SetComponentData(entity,
                    LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            if (entityManager.HasComponent<SpriteTint>(entity))
                entityManager.SetComponentData(entity, new SpriteTint { Value = ArcherVisualStyle.NormalTint });

            var member = entityManager.GetComponentData<ArrowPoolMember>(entity);
            member.Generation = NextGeneration(member.Generation);
            entityManager.SetComponentData(entity, member);
            entityManager.SetComponentEnabled<ArrowTag>(entity, true);

            var state = entityManager.GetComponentData<ArrowPoolRuntimeData>(poolEntity);
            state.AvailableCount = availableCount;
            state.ActiveCount++;
            state.TotalRentCount++;
            entityManager.SetComponentData(poolEntity, state);
        }

        private static void Expand(EntityManager entityManager, Entity poolEntity,
            Entity arrowPrefab, int count, bool runtimeExpansion)
        {
            if (count <= 0)
                return;

            using var created = entityManager.Instantiate(arrowPrefab, count, Allocator.Temp);
            for (int i = 0; i < created.Length; i++)
            {
                Entity entity = created[i];
                if (!entityManager.HasComponent<ArrowPoolMember>(entity))
                    entityManager.AddComponentData(entity, new ArrowPoolMember());
                else
                    entityManager.SetComponentData(entity, new ArrowPoolMember());
                ResetInactive(entityManager, entity);
            }

            var available = entityManager.GetBuffer<ArrowPoolAvailable>(poolEntity);
            available.EnsureCapacity(available.Length + created.Length);
            for (int i = 0; i < created.Length; i++)
                available.Add(new ArrowPoolAvailable { Entity = created[i] });

            var state = entityManager.GetComponentData<ArrowPoolRuntimeData>(poolEntity);
            state.TotalCreated += created.Length;
            state.AvailableCount = available.Length;
            if (runtimeExpansion)
                state.ExpansionCount++;
            entityManager.SetComponentData(poolEntity, state);
        }

        private static bool TryPopValid(EntityManager entityManager,
            DynamicBuffer<ArrowPoolAvailable> available, out Entity entity)
        {
            while (available.Length > 0)
            {
                int index = available.Length - 1;
                entity = available[index].Entity;
                available.RemoveAt(index);
                if (entity != Entity.Null
                    && entityManager.Exists(entity)
                    && entityManager.HasComponent<ArrowPoolMember>(entity)
                    && entityManager.HasComponent<ArrowTag>(entity)
                    && !entityManager.IsComponentEnabled<ArrowTag>(entity))
                    return true;
            }

            entity = Entity.Null;
            return false;
        }

        private static void ResetInactive(EntityManager entityManager, Entity entity)
        {
            if (entityManager.HasComponent<ArrowProjectile>(entity))
                entityManager.SetComponentData(entity, new ArrowProjectile
                {
                    Target = Entity.Null,
                    SlowMultiplier = 1f,
                    RemainingLifetime = 0f
                });

            if (entityManager.HasComponent<SpriteTint>(entity))
                entityManager.SetComponentData(entity, new SpriteTint { Value = ArcherVisualStyle.NormalTint });

            if (entityManager.HasComponent<LocalTransform>(entity))
                entityManager.SetComponentData(entity,
                    LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 0f));

            if (entityManager.HasComponent<ArrowTag>(entity))
                entityManager.SetComponentEnabled<ArrowTag>(entity, false);
        }

        private static bool IsValidPoolOwner(EntityManager entityManager, Entity poolEntity)
        {
            return poolEntity != Entity.Null
                && entityManager.Exists(poolEntity)
                && entityManager.HasComponent<ArrowPoolRuntimeData>(poolEntity)
                && entityManager.HasBuffer<ArrowPoolAvailable>(poolEntity);
        }

        private static bool IsValidArrowPrefab(EntityManager entityManager, Entity arrowPrefab)
        {
            return arrowPrefab != Entity.Null
                && entityManager.Exists(arrowPrefab)
                && entityManager.HasComponent<ArrowTag>(arrowPrefab)
                && entityManager.HasComponent<ArrowProjectile>(arrowPrefab)
                && entityManager.HasComponent<ArrowPoolMember>(arrowPrefab);
        }

        private static uint NextGeneration(uint generation)
        {
            generation++;
            return generation == 0u ? 1u : generation;
        }
    }
}
