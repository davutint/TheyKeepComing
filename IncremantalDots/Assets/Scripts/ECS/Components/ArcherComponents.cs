using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    public struct ArcherUnit : IComponentData
    {
        public float FireRate;
        public float FireTimer;
        public float ArrowDamage;
        public float Range;
    }

    public struct ArrowProjectile : IComponentData
    {
        public float Speed;
        public float Damage;
        public Entity Target;
    }

    public struct ArrowTag : IComponentData { }
}
