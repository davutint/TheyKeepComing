using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    public static class CombatHitFeedbackBudget
    {
        public const float SpatialCellSize = 0.75f;
        public const int CandidateCapacity = 512;
        public const int MaxVfxEventsPerFrame = 24;
        public const int MinimumVfxPerPresentType = 4;

        public static int3 GetSpatialKey(float3 position, CombatVfxType type)
        {
            int2 cell = (int2)math.floor(position.xy / SpatialCellSize);
            return new int3(cell.x, cell.y, (int)type);
        }

        public static void ResolveVfxBudgets(
            int arrowCandidateCount,
            int frostCandidateCount,
            out int arrowBudget,
            out int frostBudget)
        {
            int safeArrowCount = math.max(0, arrowCandidateCount);
            int safeFrostCount = math.max(0, frostCandidateCount);

            if (safeArrowCount == 0)
            {
                arrowBudget = 0;
                frostBudget = math.min(safeFrostCount, MaxVfxEventsPerFrame);
                return;
            }

            if (safeFrostCount == 0)
            {
                arrowBudget = math.min(safeArrowCount, MaxVfxEventsPerFrame);
                frostBudget = 0;
                return;
            }

            int frostReserve = math.max(
                MinimumVfxPerPresentType,
                MaxVfxEventsPerFrame / 3);
            frostBudget = math.min(safeFrostCount, frostReserve);
            arrowBudget = math.min(safeArrowCount, MaxVfxEventsPerFrame - frostBudget);

            int remaining = MaxVfxEventsPerFrame - arrowBudget - frostBudget;
            int additionalFrost = math.min(safeFrostCount - frostBudget, remaining);
            frostBudget += additionalFrost;
            remaining -= additionalFrost;
            arrowBudget += math.min(safeArrowCount - arrowBudget, remaining);
        }
    }

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
        CastleHit,
        // M-D his katmani eklemeleri
        ZombieDeath,
        FireballBlast
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
        public int Multiplicity;
    }

    public struct CombatHitFeedbackCandidate
    {
        public float3 Position;
        public float3 Direction;
        public CombatVfxType Type;
        public float Scale;
    }

    public struct CombatFeedbackBudgetTelemetryData : IComponentData
    {
        public int LastSpatialCandidateCount;
        public int LastVfxEventsEmitted;
        public int LastSfxEventsEmitted;
        public int LastVfxCandidatesDropped;
        public long TotalSpatialCandidateCount;
        public long TotalVfxEventsEmitted;
        public long TotalSfxEventsEmitted;
        public long TotalVfxCandidatesDropped;
    }
}
