using Unity.Entities;

namespace DeadWalls
{
    public struct ArrowPoolRuntimeData : IComponentData
    {
        public byte Initialized;
        public int PrewarmTarget;
        public int ExpandBatch;
        public int ExpandRequested;
        public int TotalCreated;
        public int AvailableCount;
        public int ActiveCount;
        public int ExpansionCount;
        public long TotalRentCount;
        public long TotalReturnCount;
    }

    public struct ArrowPoolMember : IComponentData
    {
        public uint Generation;
    }

    public struct ArrowPoolAvailable : IBufferElementData
    {
        public Entity Entity;
    }
}
