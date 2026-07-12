using Unity.Collections;
using Unity.Entities;

namespace DeadWalls
{
    public struct EnemyCatalogRuntimeData : IComponentData
    {
        public int EntryCount;
        public int ActiveEntryIndex;
    }

    public struct EnemyCatalogEntryData : IBufferElementData
    {
        public FixedString64Bytes Id;
        public Entity Prefab;
        public float BaseHP;
        public float BaseDamage;
        public float BaseMoveSpeed;
        public float Scale;
        public int XPReward;
        public float SpawnWeight;
        public int PoolPrewarm;
        public int PoolExpandBatch;
    }

    public struct EnemyPoolRuntimeData : IComponentData
    {
        public byte Initialized;
        public int ActiveEntryIndex;
        public int PrewarmTarget;
        public int ExpandBatch;
        public int TotalCreated;
        public int AvailableCount;
        public int ActiveCount;
        public int ExpansionCount;
        public long TotalRentCount;
        public long TotalReturnCount;
    }

    public struct EnemyPoolMember : IComponentData
    {
        public int CatalogEntryIndex;
        public uint Generation;
    }

    public struct EnemyPoolAvailable : IBufferElementData
    {
        public Entity Entity;
    }
}
