using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    public struct PhysicsBody : IComponentData
    {
        public float2 Velocity;
        public float2 Force;
        public float Mass;
        public float Damping;
    }

    public struct CollisionRadius : IComponentData
    {
        public float Value;
    }
}
