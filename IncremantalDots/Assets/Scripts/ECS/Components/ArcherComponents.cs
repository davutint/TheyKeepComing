using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    public enum ArcherType : byte
    {
        Basic,
        Rapid,
        Frost
    }

    public struct ArcherUnit : IComponentData
    {
        public float FireRate;
        public float FireTimer;
        public float ArrowDamage;
        public float Range;
        public ArcherType Type;
        public float SlowDuration;
        public float SlowMultiplier;
        public float2 FacingDirection;
        public float AttackAnimTimer;
    }

    public struct ArrowProjectile : IComponentData
    {
        public float Speed;
        public float Damage;
        public Entity Target;
        public uint TargetPoolGeneration;
        public ArcherType ArcherType;
        public float SlowDuration;
        public float SlowMultiplier;
    }

    public struct ArrowTag : IComponentData { }

    public static class ArcherVisualStyle
    {
        public static float4 NormalTint => new float4(1f, 1f, 1f, 1f);
        public static float4 RapidTint => new float4(1f, 0.82f, 0.32f, 1f);
        public static float4 FrostTint => new float4(0.45f, 0.78f, 1f, 1f);
        public static float4 SlowedZombieTint => new float4(0.55f, 0.82f, 1f, 1f);

        public static float4 GetTint(ArcherType type)
        {
            switch (type)
            {
                case ArcherType.Rapid:
                    return RapidTint;
                case ArcherType.Frost:
                    return FrostTint;
                default:
                    return NormalTint;
            }
        }
    }
}
