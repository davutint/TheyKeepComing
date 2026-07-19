using UnityEngine;

namespace DeadWalls
{
    public enum ZombieLimitPreset
    {
        Balanced = 0,
        High = 1,
        Massive = 2,
        Extreme = 3
    }

    /// <summary>
    /// Cihaz-bazli, run save'den bagimsiz zombi yogunlugu tercihi. Degerler mevcut
    /// 900 release cap'i ile 2K/5K/10K horde kabul kademelerini ayni kontratta toplar.
    /// </summary>
    public static class GameplayPerformanceSettings
    {
        private const string ZombieLimitPresetKey = "dw_zombie_limit_preset_v1";

        public const int BalancedLimit = 900;
        public const int HighLimit = 2_000;
        public const int MassiveLimit = 5_000;
        public const int ExtremeLimit = 10_000;
        public const int PresetCount = 4;

        public static ZombieLimitPreset CurrentZombieLimitPreset
        {
            get
            {
                int stored = PlayerPrefs.GetInt(
                    ZombieLimitPresetKey,
                    (int)ZombieLimitPreset.Balanced);
                return IsValidPreset(stored)
                    ? (ZombieLimitPreset)stored
                    : ZombieLimitPreset.Balanced;
            }
            set
            {
                ZombieLimitPreset safeValue = IsValidPreset((int)value)
                    ? value
                    : ZombieLimitPreset.Balanced;
                PlayerPrefs.SetInt(ZombieLimitPresetKey, (int)safeValue);
            }
        }

        public static int MaxAliveZombies => GetLimit(CurrentZombieLimitPreset);

        public static int GetLimit(ZombieLimitPreset preset)
        {
            switch (preset)
            {
                case ZombieLimitPreset.High:
                    return HighLimit;
                case ZombieLimitPreset.Massive:
                    return MassiveLimit;
                case ZombieLimitPreset.Extreme:
                    return ExtremeLimit;
                default:
                    return BalancedLimit;
            }
        }

        public static string GetDisplayName(ZombieLimitPreset preset)
        {
            switch (preset)
            {
                case ZombieLimitPreset.High:
                    return "HIGH  ·  2,000";
                case ZombieLimitPreset.Massive:
                    return "MASSIVE  ·  5,000";
                case ZombieLimitPreset.Extreme:
                    return "EXTREME  ·  10,000";
                default:
                    return "BALANCED  ·  900";
            }
        }

        public static string GetPerformanceHint(ZombieLimitPreset preset)
        {
            switch (preset)
            {
                case ZombieLimitPreset.High:
                    return "DENSER BATTLES WITH A HIGHER PERFORMANCE COST.";
                case ZombieLimitPreset.Massive:
                    return "HEAVY BATTLE DENSITY. DESKTOP-CLASS HARDWARE ADVISED.";
                case ZombieLimitPreset.Extreme:
                    return "STRESS-LEVEL DENSITY. EXPECT A SIGNIFICANT PERFORMANCE COST.";
                default:
                    return "RECOMMENDED DEFAULT FOR PERFORMANCE AND BATTLE READABILITY.";
            }
        }

        public static ZombieLimitPreset Step(ZombieLimitPreset current, int direction)
        {
            int next = Mathf.Clamp((int)current + direction, 0, PresetCount - 1);
            return (ZombieLimitPreset)next;
        }

        public static bool CanStep(ZombieLimitPreset current, int direction)
        {
            return Step(current, direction) != current;
        }

        private static bool IsValidPreset(int value)
        {
            return value >= 0 && value < PresetCount;
        }
    }
}
