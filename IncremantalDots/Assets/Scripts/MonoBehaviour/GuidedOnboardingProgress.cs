using System;

namespace DeadWalls
{
    public enum GuidedOnboardingStep
    {
        None = 0,
        EconomyOpen = 1,
        WorkerShare = 2,
        EconomyClose = 3,
        BarracksOpen = 4,
        BasicArcher = 5,
        SpeedTwo = 6,
        Rally = 7,
        CouncilChoice = 8,
        ArrowRefill = 9,
        CastleHeart = 10,
        Housing = 11,
        WallRepair = 12
    }

    public readonly struct GuidedOnboardingCopy
    {
        public readonly string Title;
        public readonly string Body;

        public GuidedOnboardingCopy(string title, string body)
        {
            Title = title;
            Body = body;
        }
    }

    /// <summary>
    /// Yeni UI Toolkit first-run akışının durable flag ve saf sıra kurallarıdır. Gameplay
    /// transaction'i yapmaz; yalnız başarıyla tamamlanan gerçek player action'ını kaydeder.
    /// </summary>
    public static class GuidedOnboardingProgress
    {
        public const string EconomyOpenFlagId = "tutorial.v2.economy_open";
        public const string WorkerShareFlagId = "tutorial.v2.worker_share";
        public const string EconomyCloseFlagId = "tutorial.v2.economy_close";
        public const string BarracksOpenFlagId = "tutorial.v2.barracks_open";
        public const string BasicArcherFlagId = "tutorial.v2.basic_archer";
        public const string SpeedTwoFlagId = "tutorial.v2.speed_2x";
        public const string RallyFlagId = "tutorial.v2.rally";
        public const string CouncilChoiceFlagId = "tutorial.v2.council";
        public const string ArrowRefillFlagId = "tutorial.v2.arrow_refill";
        public const string CastleHeartFlagId = "tutorial.v2.castle_heart";
        public const string HousingFlagId = "tutorial.v2.housing";
        public const string WallRepairFlagId = "tutorial.v2.wall_repair";
        public const string CompleteFlagId = "tutorial.v2.complete";

        private static readonly string[] ProgressFlagIds =
        {
            EconomyOpenFlagId,
            WorkerShareFlagId,
            EconomyCloseFlagId,
            BarracksOpenFlagId,
            BasicArcherFlagId,
            SpeedTwoFlagId,
            RallyFlagId,
            CouncilChoiceFlagId,
            ArrowRefillFlagId,
            CastleHeartFlagId,
            HousingFlagId,
            WallRepairFlagId,
            CompleteFlagId
        };

        public static string[] GetProgressFlagIds()
        {
            return (string[])ProgressFlagIds.Clone();
        }

        public static bool IsCoreStep(GuidedOnboardingStep step)
        {
            return step >= GuidedOnboardingStep.EconomyOpen
                && step <= GuidedOnboardingStep.SpeedTwo;
        }

        public static int GetCoreStepNumber(GuidedOnboardingStep step)
        {
            return IsCoreStep(step) ? (int)step : 0;
        }

        public static GuidedOnboardingStep ResolveCoreStep(
            bool suppressTutorial,
            bool economyOpenComplete,
            bool workerShareComplete,
            bool economyCloseComplete,
            bool barracksOpenComplete,
            bool basicArcherComplete,
            bool speedTwoComplete)
        {
            if (suppressTutorial)
                return GuidedOnboardingStep.None;
            if (!economyOpenComplete)
                return GuidedOnboardingStep.EconomyOpen;
            if (!workerShareComplete)
                return GuidedOnboardingStep.WorkerShare;
            if (!economyCloseComplete)
                return GuidedOnboardingStep.EconomyClose;
            if (!barracksOpenComplete)
                return GuidedOnboardingStep.BarracksOpen;
            if (!basicArcherComplete)
                return GuidedOnboardingStep.BasicArcher;
            return speedTwoComplete
                ? GuidedOnboardingStep.None
                : GuidedOnboardingStep.SpeedTwo;
        }

        public static GuidedOnboardingStep ResolveContextualStep(
            bool suppressTutorial,
            bool coreComplete,
            bool councilComplete,
            bool councilEligible,
            bool rallyComplete,
            bool rallyEligible,
            bool repairComplete,
            bool repairEligible,
            bool arrowRefillComplete,
            bool arrowRefillEligible,
            bool castleHeartComplete,
            bool castleHeartEligible,
            bool housingComplete,
            bool housingEligible)
        {
            if (suppressTutorial || !coreComplete)
                return GuidedOnboardingStep.None;
            if (!councilComplete && councilEligible)
                return GuidedOnboardingStep.CouncilChoice;
            if (!rallyComplete && rallyEligible)
                return GuidedOnboardingStep.Rally;
            if (!repairComplete && repairEligible)
                return GuidedOnboardingStep.WallRepair;
            if (!arrowRefillComplete && arrowRefillEligible)
                return GuidedOnboardingStep.ArrowRefill;
            if (!castleHeartComplete && castleHeartEligible)
                return GuidedOnboardingStep.CastleHeart;
            if (!housingComplete && housingEligible)
                return GuidedOnboardingStep.Housing;
            return GuidedOnboardingStep.None;
        }

        public static GuidedOnboardingCopy GetCopy(GuidedOnboardingStep step)
        {
            return step switch
            {
                GuidedOnboardingStep.EconomyOpen => new GuidedOnboardingCopy(
                    "OPEN THE ECONOMY",
                    "Manage how your people produce resources."),
                GuidedOnboardingStep.WorkerShare => new GuidedOnboardingCopy(
                    "SET A WORKER SHARE",
                    "Drag the Wood slider to change the workforce target."),
                GuidedOnboardingStep.EconomyClose => new GuidedOnboardingCopy(
                    "CLOSE THE ECONOMY",
                    "Return to the command rail before opening the Barracks."),
                GuidedOnboardingStep.BarracksOpen => new GuidedOnboardingCopy(
                    "OPEN THE BARRACKS",
                    "Recruit defenders before night falls."),
                GuidedOnboardingStep.BasicArcher => new GuidedOnboardingCopy(
                    "RECRUIT A BASIC ARCHER",
                    "Buy one Basic Archer to strengthen the wall."),
                GuidedOnboardingStep.SpeedTwo => new GuidedOnboardingCopy(
                    "SET GAME SPEED TO 2X",
                    "Use 2X while preparing. You can change speed later."),
                GuidedOnboardingStep.Rally => new GuidedOnboardingCopy(
                    "USE RALLY",
                    "Press 2 or tap Rally to boost archer fire rate."),
                GuidedOnboardingStep.CouncilChoice => new GuidedOnboardingCopy(
                    "CHOOSE A COUNCIL OUTCOME",
                    "Compare both exact outcomes, then choose one."),
                GuidedOnboardingStep.ArrowRefill => new GuidedOnboardingCopy(
                    "RESTOCK YOUR ARROWS",
                    "Open Arrow Supply and buy any refill package."),
                GuidedOnboardingStep.CastleHeart => new GuidedOnboardingCopy(
                    "OPEN THE CASTLE HEART",
                    "Inspect permanent run upgrades after the battle."),
                GuidedOnboardingStep.Housing => new GuidedOnboardingCopy(
                    "EXPAND HOUSING",
                    "Add beds when your population reaches capacity."),
                GuidedOnboardingStep.WallRepair => new GuidedOnboardingCopy(
                    "REPAIR THE WALL",
                    "Use Emergency Repair when the wall is damaged at night."),
                _ => new GuidedOnboardingCopy(string.Empty, string.Empty)
            };
        }

        public static bool IsCoreComplete()
        {
            return HasFlag(EconomyOpenFlagId)
                && HasFlag(WorkerShareFlagId)
                && HasFlag(EconomyCloseFlagId)
                && HasFlag(BarracksOpenFlagId)
                && HasFlag(BasicArcherFlagId)
                && HasFlag(SpeedTwoFlagId);
        }

        public static bool TryComplete(GuidedOnboardingStep step)
        {
            string flagId = GetFlagId(step);
            if (string.IsNullOrEmpty(flagId))
                return false;

            bool persisted = HasFlag(flagId) || MetaProgression.SetTutorialFlag(flagId, true);
            if (persisted)
                EnsureGlobalCompletionPersisted();
            return persisted;
        }

        public static bool EnsureGlobalCompletionPersisted()
        {
            if (HasFlag(CompleteFlagId))
                return true;

            for (int i = 0; i < ProgressFlagIds.Length - 1; i++)
            {
                if (!HasFlag(ProgressFlagIds[i]))
                    return false;
            }

            return MetaProgression.SetTutorialFlag(CompleteFlagId, true);
        }

        private static bool HasFlag(string flagId)
        {
            return MetaProgression.HasTutorialFlag(flagId);
        }

        private static string GetFlagId(GuidedOnboardingStep step)
        {
            return step switch
            {
                GuidedOnboardingStep.EconomyOpen => EconomyOpenFlagId,
                GuidedOnboardingStep.WorkerShare => WorkerShareFlagId,
                GuidedOnboardingStep.EconomyClose => EconomyCloseFlagId,
                GuidedOnboardingStep.BarracksOpen => BarracksOpenFlagId,
                GuidedOnboardingStep.BasicArcher => BasicArcherFlagId,
                GuidedOnboardingStep.SpeedTwo => SpeedTwoFlagId,
                GuidedOnboardingStep.Rally => RallyFlagId,
                GuidedOnboardingStep.CouncilChoice => CouncilChoiceFlagId,
                GuidedOnboardingStep.ArrowRefill => ArrowRefillFlagId,
                GuidedOnboardingStep.CastleHeart => CastleHeartFlagId,
                GuidedOnboardingStep.Housing => HousingFlagId,
                GuidedOnboardingStep.WallRepair => WallRepairFlagId,
                _ => string.Empty
            };
        }
    }
}
