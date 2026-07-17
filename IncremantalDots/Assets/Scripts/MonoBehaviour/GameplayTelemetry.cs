using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    [Serializable]
    public sealed class GameplayTelemetryEnvelope
    {
        public string EventName;
        public int SchemaVersion;
        public string RunId;
        public string PayloadJson;
    }

    public readonly struct GameplayTelemetryRecord
    {
        public readonly string EventName;
        public readonly int SchemaVersion;
        public readonly string RunId;
        public readonly string PayloadJson;
        public readonly string SerializedEnvelope;

        public GameplayTelemetryRecord(
            string eventName,
            int schemaVersion,
            string runId,
            string payloadJson,
            string serializedEnvelope)
        {
            EventName = eventName;
            SchemaVersion = schemaVersion;
            RunId = runId;
            PayloadJson = payloadJson;
            SerializedEnvelope = serializedEnvelope;
        }
    }

    [Serializable]
    public sealed class TelemetryMetaLevelSnapshot
    {
        public string UpgradeId;
        public int Level;
    }

    [Serializable]
    public struct TelemetryStartingResources
    {
        public int Wood;
        public int Stone;
        public int Iron;
        public int Food;
        public int Arrows;
        public int ArrowCapacity;
        public long GraveEssence;
        public int Population;
        public int PopulationCapacity;
    }

    [Serializable]
    public struct TelemetryHeartGraphIdentity
    {
        public bool CatalogConfigured;
        public bool RuntimeAttempted;
        public bool GraphReady;
        public int GraphVersion;
        public int CatalogVersion;
        public uint Seed;
    }

    [Serializable]
    public sealed class RunStartedTelemetryPayload
    {
        public int MetaProgressVersion;
        public bool MetaCatalogConfigured;
        public int MetaCatalogDefinitionCount;
        public List<TelemetryMetaLevelSnapshot> MetaLevels =
            new List<TelemetryMetaLevelSnapshot>();
        public TelemetryStartingResources StartingResources;
        public TelemetryHeartGraphIdentity Heart;
    }

    [Serializable]
    public sealed class PhaseChangedTelemetryPayload
    {
        public int Day;
        public string Phase;
        public int AliveEnemies;
        public long SpawnBacklog;
    }

    /// <summary>
    /// Gameplay event'lerinin provider-bagimsiz cikis siniri. Runtime state sahiplenmez ve
    /// dis analytics SDK'si secmez; subscriber'lara immutable JSON snapshot yollar.
    /// </summary>
    public static class GameplayTelemetry
    {
        public const string LogPrefix = "[DW-TELEMETRY]";
        public const string RunStartedEventName = "run_started";
        public const int RunStartedSchemaVersion = 1;
        public const string PhaseChangedEventName = "phase_changed";
        public const int PhaseChangedSchemaVersion = 1;

        public static event Action<GameplayTelemetryRecord> Emitted;

        public static bool TryEmitRunStarted(
            string runId,
            RunStartedTelemetryPayload payload,
            out GameplayTelemetryRecord record,
            out string error)
        {
            record = default;
            if (!TryNormalizeRunId(runId, RunStartedEventName, out string normalizedRunId, out error))
                return false;
            if (!TryValidateRunStarted(payload, out error))
                return false;

            return EmitValidated(
                RunStartedEventName,
                RunStartedSchemaVersion,
                normalizedRunId,
                payload,
                out record,
                out error);
        }

        public static bool TryEmitPhaseChanged(
            string runId,
            PhaseChangedTelemetryPayload payload,
            out GameplayTelemetryRecord record,
            out string error)
        {
            record = default;
            if (!TryNormalizeRunId(runId, PhaseChangedEventName, out string normalizedRunId, out error))
                return false;
            if (!TryValidatePhaseChanged(payload, out error))
                return false;

            return EmitValidated(
                PhaseChangedEventName,
                PhaseChangedSchemaVersion,
                normalizedRunId,
                payload,
                out record,
                out error);
        }

        private static bool TryNormalizeRunId(
            string runId,
            string eventName,
            out string normalizedRunId,
            out string error)
        {
            normalizedRunId = runId?.Trim();
            if (!string.IsNullOrEmpty(normalizedRunId))
            {
                error = string.Empty;
                return true;
            }

            error = $"{eventName} RunId bos olamaz.";
            return false;
        }

        private static bool EmitValidated(
            string eventName,
            int schemaVersion,
            string runId,
            object payload,
            out GameplayTelemetryRecord record,
            out string error)
        {
            string payloadJson = JsonUtility.ToJson(payload, false);
            var envelope = new GameplayTelemetryEnvelope
            {
                EventName = eventName,
                SchemaVersion = schemaVersion,
                RunId = runId,
                PayloadJson = payloadJson
            };
            string serializedEnvelope = JsonUtility.ToJson(envelope, false);
            record = new GameplayTelemetryRecord(
                envelope.EventName,
                envelope.SchemaVersion,
                envelope.RunId,
                payloadJson,
                serializedEnvelope);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"{LogPrefix} {serializedEnvelope}");
#endif

            try
            {
                Emitted?.Invoke(record);
            }
            catch (Exception exception)
            {
                // Telemetry subscriber'i gameplay transaction'ini bozamamalidir.
                Debug.LogError($"{LogPrefix} Subscriber hatasi: {exception.Message}");
            }

            error = string.Empty;
            return true;
        }

        internal static bool TryValidateRunStarted(
            RunStartedTelemetryPayload payload,
            out string error)
        {
            if (payload == null)
            {
                error = "run_started payload bos.";
                return false;
            }
            if (payload.MetaProgressVersion < 0 || payload.MetaCatalogDefinitionCount < 0)
            {
                error = "run_started meta identity negatif deger tasiyor.";
                return false;
            }
            if (payload.MetaLevels == null)
            {
                error = "run_started MetaLevels listesi bos referans.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < payload.MetaLevels.Count; i++)
            {
                TelemetryMetaLevelSnapshot level = payload.MetaLevels[i];
                string id = level?.UpgradeId?.Trim();
                if (string.IsNullOrEmpty(id) || level.Level < 0 || !ids.Add(id))
                {
                    error = $"run_started MetaLevels[{i}] gecersiz veya duplicate.";
                    return false;
                }
            }

            TelemetryStartingResources resources = payload.StartingResources;
            if (resources.Wood < 0 || resources.Stone < 0 || resources.Iron < 0
                || resources.Food < 0 || resources.Arrows < 0 || resources.ArrowCapacity < 0
                || resources.GraveEssence < 0L || resources.Population < 0
                || resources.PopulationCapacity < 0 || resources.Arrows > resources.ArrowCapacity)
            {
                error = "run_started starting resource snapshot'i gecersiz.";
                return false;
            }

            TelemetryHeartGraphIdentity heart = payload.Heart;
            if (heart.GraphReady
                && (!heart.CatalogConfigured || !heart.RuntimeAttempted
                    || heart.GraphVersion <= 0 || heart.CatalogVersion <= 0 || heart.Seed == 0u))
            {
                error = "run_started hazir Heart graph identity'si eksik.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        internal static bool TryValidatePhaseChanged(
            PhaseChangedTelemetryPayload payload,
            out string error)
        {
            if (payload == null)
            {
                error = "phase_changed payload bos.";
                return false;
            }
            if (payload.Day < 1 || payload.AliveEnemies < 0 || payload.SpawnBacklog < 0L)
            {
                error = "phase_changed day veya horde snapshot'i gecersiz.";
                return false;
            }
            if (!PhaseChangedTelemetryFactory.IsContractPhase(payload.Phase))
            {
                error = "phase_changed phase kimligi gecersiz.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }

    internal static class RunStartedTelemetryFactory
    {
        internal static RunStartedTelemetryPayload Create(
            MetaUpgradeCatalogSO metaCatalog,
            MetaProgressState metaState,
            ResourceData resources,
            ArrowSupply arrowSupply,
            int arrowCapacity,
            PopulationState population,
            HeartRuntimeTuningTelemetry heart)
        {
            var levels = new List<TelemetryMetaLevelSnapshot>();
            int definitionCount = 0;
            if (metaCatalog != null && metaCatalog.Upgrades != null)
            {
                var seenIds = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < metaCatalog.Upgrades.Length; i++)
                {
                    MetaUpgradeSO definition = metaCatalog.Upgrades[i];
                    if (definition == null)
                        continue;

                    definitionCount++;
                    string id = definition.Id?.Trim();
                    if (string.IsNullOrEmpty(id) || !seenIds.Add(id))
                        continue;

                    levels.Add(new TelemetryMetaLevelSnapshot
                    {
                        UpgradeId = id,
                        Level = FindMetaLevel(metaState, id)
                    });
                }
            }
            levels.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.UpgradeId, right.UpgradeId));

            int safeArrowCapacity = Math.Max(0, arrowCapacity);
            return new RunStartedTelemetryPayload
            {
                MetaProgressVersion = Math.Max(0, metaState?.Version ?? 0),
                MetaCatalogConfigured = metaCatalog != null,
                MetaCatalogDefinitionCount = definitionCount,
                MetaLevels = levels,
                StartingResources = new TelemetryStartingResources
                {
                    Wood = Math.Max(0, resources.Wood),
                    Stone = Math.Max(0, resources.Stone),
                    Iron = Math.Max(0, resources.Iron),
                    Food = Math.Max(0, resources.Food),
                    Arrows = Math.Min(Math.Max(0, arrowSupply.Current), safeArrowCapacity),
                    ArrowCapacity = safeArrowCapacity,
                    GraveEssence = Math.Max(0L, heart.GraveEssence),
                    Population = Math.Max(0, population.Total),
                    PopulationCapacity = Math.Max(0, population.Capacity)
                },
                Heart = new TelemetryHeartGraphIdentity
                {
                    CatalogConfigured = heart.HasCatalog,
                    RuntimeAttempted = heart.RuntimeAttempted,
                    GraphReady = heart.RuntimeReady,
                    GraphVersion = Math.Max(0, heart.GraphVersion),
                    CatalogVersion = Math.Max(0, heart.CatalogVersion),
                    Seed = heart.Seed
                }
            };
        }

        private static int FindMetaLevel(MetaProgressState state, string id)
        {
            if (state?.Upgrades == null)
                return 0;

            for (int i = 0; i < state.Upgrades.Count; i++)
            {
                MetaUpgradeLevel entry = state.Upgrades[i];
                if (entry != null && string.Equals(entry.Id, id, StringComparison.Ordinal))
                    return Math.Max(0, entry.Level);
            }
            return 0;
        }
    }

    internal static class PhaseChangedTelemetryFactory
    {
        internal static PhaseChangedTelemetryPayload Create(
            ContinuousSiegeCycleData cycle,
            WaveStateData wave,
            ContinuousSpawnBudgetData spawnBudget)
        {
            int day = cycle.CycleIndex >= int.MaxValue
                ? int.MaxValue
                : Math.Max(1, cycle.CycleIndex + 1);
            return new PhaseChangedTelemetryPayload
            {
                Day = day,
                Phase = ToContractPhase(cycle.Phase),
                AliveEnemies = Math.Max(0, wave.ZombiesAlive),
                SpawnBacklog = Math.Max(0L, spawnBudget.PendingEnemies)
            };
        }

        internal static bool IsContractPhase(string phase)
        {
            return string.Equals(phase, "day", StringComparison.Ordinal)
                || string.Equals(phase, "dusk", StringComparison.Ordinal)
                || string.Equals(phase, "night", StringComparison.Ordinal)
                || string.Equals(phase, "dawn", StringComparison.Ordinal);
        }

        private static string ToContractPhase(SiegeCyclePhase phase)
        {
            switch (phase)
            {
                case SiegeCyclePhase.Day:
                    return "day";
                case SiegeCyclePhase.Dusk:
                    return "dusk";
                case SiegeCyclePhase.Night:
                    return "night";
                case SiegeCyclePhase.Dawn:
                    return "dawn";
                default:
                    return string.Empty;
            }
        }
    }
}
