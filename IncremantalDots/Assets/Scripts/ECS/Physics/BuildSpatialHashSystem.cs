using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ApplyMovementForceSystem))]
    [UpdateBefore(typeof(PhysicsCollisionSystem))]
    public partial struct BuildSpatialHashSystem : ISystem
    {
        public static NativeParallelMultiHashMap<int, Entity> SpatialMap;

        public void OnCreate(ref SystemState state)
        {
            SpatialMap = new NativeParallelMultiHashMap<int, Entity>(1024, Allocator.Persistent);
            state.RequireForUpdate<ZombieTag>();
        }

        public void OnDestroy(ref SystemState state)
        {
            if (SpatialMap.IsCreated)
                SpatialMap.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder()
                .WithAll<ZombieTag, LocalTransform, PhysicsBody>()
                .WithNone<DeathTimer>()
                .Build();

            int count = query.CalculateEntityCount();
            if (count == 0)
            {
                SpatialMap.Clear();
                return;
            }

            int capacity = (int)math.ceilpow2(math.max(count * 2, 1024));
            if (SpatialMap.Capacity < capacity)
            {
                SpatialMap.Dispose();
                SpatialMap = new NativeParallelMultiHashMap<int, Entity>(capacity, Allocator.Persistent);
            }
            else
            {
                SpatialMap.Clear();
            }

            new HashJob
            {
                CellSize = SpatialHash.DefaultCellSize,
                Map = SpatialMap.AsParallelWriter()
            }.ScheduleParallel(query, state.Dependency).Complete();
        }

        [BurstCompile]
        partial struct HashJob : IJobEntity
        {
            public float CellSize;
            public NativeParallelMultiHashMap<int, Entity>.ParallelWriter Map;

            void Execute(Entity entity, in LocalTransform transform)
            {
                int key = SpatialHash.Hash(transform.Position.xy, CellSize);
                Map.Add(key, entity);
            }
        }
    }
}
