using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    public enum CombatVfxType : byte
    {
        ArrowMuzzle,
        ArrowHit,
        FrostHit,
        CastleHit
    }

    public enum CombatSfxType : byte
    {
        ArrowShoot,
        ArrowHit,
        FrostHit,
        CastleHit
    }

    public struct CombatVfxEvent : IComponentData
    {
        public float3 Position;
        public float3 Direction;
        public CombatVfxType Type;
        public float Scale;
        public Entity FollowTarget;
        public float3 FollowOffset;
        public bool FollowTargetPosition;
    }

    public struct CombatSfxEvent : IComponentData
    {
        public float3 Position;
        public CombatSfxType Type;
        public float Volume;
        public float Pitch;
    }
}
