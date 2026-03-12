using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    /// <summary>
    /// Double-buffered spatial hash grid.
    /// ReadMap: onceki frame'in verisi, consumer'lar (Collision, Boundary) okur.
    /// WriteMap: bu frame'de hash job doldurur. Frame basinda swap edilir.
    /// .Complete() cagrilmaz — main thread bloklanmaz.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ApplyMovementForceSystem))]
    [UpdateBefore(typeof(PhysicsCollisionSystem))]
    public partial struct BuildSpatialHashSystem : ISystem
    {
        public static NativeParallelMultiHashMap<int, Entity> ReadMap;
        public static NativeParallelMultiHashMap<int, Entity> WriteMap;

        private EntityQuery _zombieQuery;
        private JobHandle _hashJobHandle;

        public void OnCreate(ref SystemState state)
        {
            const int initialCapacity = 16384;
            ReadMap = new NativeParallelMultiHashMap<int, Entity>(initialCapacity, Allocator.Persistent);
            WriteMap = new NativeParallelMultiHashMap<int, Entity>(initialCapacity, Allocator.Persistent);

            _zombieQuery = SystemAPI.QueryBuilder()
                .WithAll<ZombieTag, LocalTransform, PhysicsBody>()
                .WithNone<DeathTimer>()
                .Build();
            state.RequireForUpdate<ZombieTag>();
        }

        public void OnDestroy(ref SystemState state)
        {
            _hashJobHandle.Complete();
            if (ReadMap.IsCreated) ReadMap.Dispose();
            if (WriteMap.IsCreated) WriteMap.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Onceki frame'in hash job'i tamamlanmis olmali
            // (WaveSpawnSystem sequential + ArcherShootSystem sync tetikliyor)
            _hashJobHandle.Complete();

            // Swap: ReadMap artik taze veri, WriteMap yeniden doldurulacak
            (ReadMap, WriteMap) = (WriteMap, ReadMap);

            int count = _zombieQuery.CalculateEntityCount();
            if (count == 0)
            {
                WriteMap.Clear();
                return;
            }

            // Nadir capacity resize — oyun boyunca 5-10 kez
            int capacity = (int)math.ceilpow2(math.max(count * 2, 1024));
            if (WriteMap.Capacity < capacity)
            {
                WriteMap.Dispose();
                WriteMap = new NativeParallelMultiHashMap<int, Entity>(capacity, Allocator.Persistent);
            }

            // ClearMapJob: WriteMap'i temizle (state.Dependency'ye bagimli → onceki frame'in consumer'lari)
            var clearHandle = new ClearMapJob
            {
                Map = WriteMap
            }.Schedule(state.Dependency);

            // HashJob: pozisyonlari WriteMap'e yaz
            _hashJobHandle = new HashJob
            {
                CellSize = SpatialHash.DefaultCellSize,
                Map = WriteMap.AsParallelWriter()
            }.ScheduleParallel(_zombieQuery, clearHandle);

            // state.Dependency'ye hash job'u ekle → component dependency zinciri devam etsin
            state.Dependency = _hashJobHandle;
        }

        [BurstCompile]
        struct ClearMapJob : IJob
        {
            public NativeParallelMultiHashMap<int, Entity> Map;
            public void Execute() { Map.Clear(); }
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
