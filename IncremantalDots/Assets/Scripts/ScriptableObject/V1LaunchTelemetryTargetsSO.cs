using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace DeadWalls
{
    public enum V1TelemetryTargetCategory
    {
        Spawn = 0,
        Economy = 1,
        Combat = 2,
        Council = 3,
        Meta = 4
    }

    public enum V1TelemetryTargetUnit
    {
        Day = 0,
        Ratio = 1,
        Count = 2,
        LastEmbers = 3,
        DayDelta = 4
    }

    [Serializable]
    public sealed class V1TelemetryTargetDefinition
    {
        public string Id = string.Empty;
        public string Label = string.Empty;
        public V1TelemetryTargetCategory Category;
        public V1TelemetryTargetUnit Unit;
        public float MinInclusive;
        public float MaxInclusive = 1f;
        [Min(1)] public int MinimumSamples = 100;
        public string Cohort = string.Empty;
        [Tooltip("Virgulle ayrilmis canonical GameplayTelemetry event adlari.")]
        public string SourceEvents = string.Empty;
        [TextArea(1, 3)] public string Interpretation = string.Empty;
    }

    /// <summary>
    /// V1 launch balance telemetry bantlarinin provider-bagimsiz production owner'i.
    /// Bu asset runtime tuning'i otomatik degistirmez; yeterli orneklem sonrasinda designer
    /// review'u tetikleyen olculebilir kabul araliklarini tutar.
    /// </summary>
    [CreateAssetMenu(
        fileName = "V1LaunchTelemetryTargets",
        menuName = "DeadWalls/Mobile Castle/V1 Launch Telemetry Targets")]
    public sealed class V1LaunchTelemetryTargetsSO : ScriptableObject
    {
        public const int CurrentVersion = 1;
        public const string ProductionAssetPath =
            "Assets/ScriptableObject/MobileCastle/Tuning/V1LaunchTelemetryTargets.asset";

        [Header("Identity")]
        public int Version = CurrentVersion;
        public string ProfileId = "dead_walls_v1_launch_targets";
        [Min(1)] public int MinimumCompletedRuns = 100;

        [Header("Provider-Independent Target Bands")]
        public V1TelemetryTargetDefinition[] Targets = Array.Empty<V1TelemetryTargetDefinition>();

        public V1TelemetryTargetDefinition GetTarget(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || Targets == null)
                return null;

            for (int i = 0; i < Targets.Length; i++)
            {
                V1TelemetryTargetDefinition target = Targets[i];
                if (target != null && string.Equals(target.Id, id, StringComparison.Ordinal))
                    return target;
            }

            return null;
        }

        public List<string> ValidateProfile()
        {
            var problems = new List<string>();
            if (Version != CurrentVersion)
                problems.Add($"Telemetry target profile v{Version}; beklenen v{CurrentVersion}.");
            if (string.IsNullOrWhiteSpace(ProfileId))
                problems.Add("Telemetry target ProfileId bos.");
            if (MinimumCompletedRuns < 1)
                problems.Add("MinimumCompletedRuns sifirdan buyuk olmali.");
            if (Targets == null || Targets.Length == 0)
            {
                problems.Add("Telemetry target listesi bos.");
                return problems;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var coveredCategories = new HashSet<V1TelemetryTargetCategory>();
            for (int i = 0; i < Targets.Length; i++)
            {
                V1TelemetryTargetDefinition target = Targets[i];
                if (target == null)
                {
                    problems.Add($"Targets[{i}] null.");
                    continue;
                }

                string label = string.IsNullOrWhiteSpace(target.Id)
                    ? $"Targets[{i}]"
                    : target.Id.Trim();
                if (string.IsNullOrWhiteSpace(target.Id))
                    problems.Add($"Targets[{i}] Id bos.");
                else if (!ids.Add(target.Id.Trim()))
                    problems.Add($"Duplicate telemetry target Id: '{target.Id.Trim()}'.");

                coveredCategories.Add(target.Category);
                if (string.IsNullOrWhiteSpace(target.Label))
                    problems.Add($"'{label}' Label bos.");
                if (string.IsNullOrWhiteSpace(target.Cohort))
                    problems.Add($"'{label}' Cohort bos.");
                if (string.IsNullOrWhiteSpace(target.Interpretation))
                    problems.Add($"'{label}' Interpretation bos.");
                if (target.MinimumSamples < 1)
                    problems.Add($"'{label}' MinimumSamples sifirdan buyuk olmali.");
                if (!IsFinite(target.MinInclusive) || !IsFinite(target.MaxInclusive)
                    || target.MinInclusive > target.MaxInclusive)
                {
                    problems.Add($"'{label}' min/max bandi gecersiz.");
                }
                else if (target.Unit == V1TelemetryTargetUnit.Ratio
                         && (target.MinInclusive < 0f || target.MaxInclusive > 1f))
                {
                    problems.Add($"'{label}' ratio bandi 0..1 disinda.");
                }
                else if (target.Unit != V1TelemetryTargetUnit.DayDelta
                         && target.MinInclusive < 0f)
                {
                    problems.Add($"'{label}' negatif alt sinir tasiyor.");
                }

                ValidateSourceEvents(label, target.SourceEvents, problems);
            }

            foreach (V1TelemetryTargetCategory category in
                     Enum.GetValues(typeof(V1TelemetryTargetCategory)))
            {
                if (!coveredCategories.Contains(category))
                    problems.Add($"Telemetry target category eksik: {category}.");
            }

            return problems;
        }

        public string ComputeFingerprint()
        {
            var canonical = new StringBuilder(2048);
            canonical.Append(Version).Append('|')
                .Append((ProfileId ?? string.Empty).Trim()).Append('|')
                .Append(MinimumCompletedRuns);

            if (Targets != null)
            {
                for (int i = 0; i < Targets.Length; i++)
                {
                    V1TelemetryTargetDefinition target = Targets[i];
                    canonical.Append('\n');
                    if (target == null)
                    {
                        canonical.Append("NULL");
                        continue;
                    }

                    canonical.Append((target.Id ?? string.Empty).Trim()).Append('|')
                        .Append((int)target.Category).Append('|')
                        .Append((int)target.Unit).Append('|')
                        .Append(target.MinInclusive.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                        .Append(target.MaxInclusive.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                        .Append(target.MinimumSamples).Append('|')
                        .Append((target.Cohort ?? string.Empty).Trim()).Append('|')
                        .Append(NormalizeSourceEvents(target.SourceEvents)).Append('|')
                        .Append((target.Label ?? string.Empty).Trim()).Append('|')
                        .Append((target.Interpretation ?? string.Empty).Trim());
                }
            }

            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()));
            var result = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                result.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
            return result.ToString();
        }

        private static void ValidateSourceEvents(
            string label,
            string sourceEvents,
            List<string> problems)
        {
            string normalized = NormalizeSourceEvents(sourceEvents);
            if (string.IsNullOrEmpty(normalized))
            {
                problems.Add($"'{label}' SourceEvents bos.");
                return;
            }

            string[] events = normalized.Split(',');
            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < events.Length; i++)
            {
                string eventName = events[i];
                if (!unique.Add(eventName))
                    problems.Add($"'{label}' duplicate source event tasiyor: {eventName}.");
                if (!IsCanonicalGameplayEvent(eventName))
                    problems.Add($"'{label}' bilinmeyen source event tasiyor: {eventName}.");
            }
        }

        private static string NormalizeSourceEvents(string sourceEvents)
        {
            if (string.IsNullOrWhiteSpace(sourceEvents))
                return string.Empty;

            string[] raw = sourceEvents.Split(',');
            var normalized = new List<string>(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                string value = raw[i].Trim();
                if (!string.IsNullOrEmpty(value))
                    normalized.Add(value);
            }

            return string.Join(",", normalized);
        }

        private static bool IsCanonicalGameplayEvent(string eventName)
        {
            switch (eventName)
            {
                case GameplayTelemetry.RunStartedEventName:
                case GameplayTelemetry.PhaseChangedEventName:
                case GameplayTelemetry.ResourceSpentEventName:
                case GameplayTelemetry.ArcherChangedEventName:
                case GameplayTelemetry.HeartNodeBoughtEventName:
                case GameplayTelemetry.CouncilResolvedEventName:
                case GameplayTelemetry.AbilityCastEventName:
                case GameplayTelemetry.WallRepairedEventName:
                case GameplayTelemetry.RunEndedEventName:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
