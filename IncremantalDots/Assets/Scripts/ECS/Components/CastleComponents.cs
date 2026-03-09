using Unity.Entities;

namespace DeadWalls
{
    public struct WallSegment : IComponentData
    {
        public float MaxHP;
        public float CurrentHP;
    }

    public struct GateComponent : IComponentData
    {
        public float MaxHP;
        public float CurrentHP;
    }

    public struct CastleHP : IComponentData
    {
        public float MaxHP;
        public float CurrentHP;
    }

    public struct WallXPosition : IComponentData
    {
        public float Value;
    }
}
