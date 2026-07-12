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
}
