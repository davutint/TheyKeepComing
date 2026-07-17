using System;
using System.Collections.Generic;

namespace DeadWalls
{
    public partial class GameManager
    {
        private string _runStartedTelemetryEmittedRunId;
        private string _runStartedTelemetrySuppressedRunId;
        private string _runStartedTelemetryRejectedRunId;
        private bool _phaseChangedTelemetryHandled;
        private string _phaseChangedTelemetryHandledRunId;
        private int _phaseChangedTelemetryHandledDay;
        private SiegeCyclePhase _phaseChangedTelemetryHandledPhase;
        private string _runEndedTelemetryHandledRunId;

        private void TryEmitRunStartedTelemetry()
        {
            if (!_initialized || GameState.IsGameOver
                || GameBootstrap.PendingAction != GameBootstrap.StartAction.None
                || !_heartRuntimeAttempted || string.IsNullOrWhiteSpace(_currentRunId))
            {
                return;
            }

            if (string.Equals(_runStartedTelemetryEmittedRunId, _currentRunId,
                    StringComparison.Ordinal)
                || string.Equals(_runStartedTelemetrySuppressedRunId, _currentRunId,
                    StringComparison.Ordinal)
                || string.Equals(_runStartedTelemetryRejectedRunId, _currentRunId,
                    StringComparison.Ordinal))
            {
                return;
            }

            int arrowCapacity = ArrowEconomyUtility.GetCapacity(
                ArrowSupply,
                GetEconomyPriceTuning());
            RunStartedTelemetryPayload payload = RunStartedTelemetryFactory.Create(
                metaUpgradeCatalog,
                MetaProgression.State,
                Resources,
                ArrowSupply,
                arrowCapacity,
                Population,
                GetHeartRuntimeTuningTelemetry());

            if (!GameplayTelemetry.TryEmitRunStarted(
                    _currentRunId,
                    payload,
                    out _,
                    out string error))
            {
                _runStartedTelemetryRejectedRunId = _currentRunId;
                UnityEngine.Debug.LogError(
                    $"[GameManager] run_started telemetry reddedildi: {error}");
                return;
            }

            _runStartedTelemetryEmittedRunId = _currentRunId;
        }

        private void TryEmitPhaseChangedTelemetry()
        {
            if (!_initialized || GameState.IsGameOver
                || GameBootstrap.PendingAction != GameBootstrap.StartAction.None
                || string.IsNullOrWhiteSpace(_currentRunId)
                || !ContinuousSiegeCycle.Enabled
                || !IsRunIdentityReadyForPhaseTelemetry())
            {
                return;
            }

            int day = ContinuousSiegeCycle.CycleIndex >= int.MaxValue
                ? int.MaxValue
                : Math.Max(1, ContinuousSiegeCycle.CycleIndex + 1);
            if (IsPhaseChangedTelemetryHandled(
                    _currentRunId,
                    day,
                    ContinuousSiegeCycle.Phase))
            {
                return;
            }

            PhaseChangedTelemetryPayload payload = PhaseChangedTelemetryFactory.Create(
                ContinuousSiegeCycle,
                WaveState,
                ContinuousSpawnBudget);
            bool emitted = GameplayTelemetry.TryEmitPhaseChanged(
                _currentRunId,
                payload,
                out _,
                out string error);
            MarkPhaseChangedTelemetryHandled(
                _currentRunId,
                day,
                ContinuousSiegeCycle.Phase);
            if (!emitted)
            {
                UnityEngine.Debug.LogError(
                    $"[GameManager] phase_changed telemetry reddedildi: {error}");
            }
        }

        private bool IsRunIdentityReadyForPhaseTelemetry()
        {
            return string.Equals(
                    _runStartedTelemetryEmittedRunId,
                    _currentRunId,
                    StringComparison.Ordinal)
                || string.Equals(
                    _runStartedTelemetrySuppressedRunId,
                    _currentRunId,
                    StringComparison.Ordinal);
        }

        private bool IsPhaseChangedTelemetryHandled(
            string runId,
            int day,
            SiegeCyclePhase phase)
        {
            return _phaseChangedTelemetryHandled
                && string.Equals(_phaseChangedTelemetryHandledRunId, runId,
                    StringComparison.Ordinal)
                && _phaseChangedTelemetryHandledDay == day
                && _phaseChangedTelemetryHandledPhase == phase;
        }

        private void MarkPhaseChangedTelemetryHandled(
            string runId,
            int day,
            SiegeCyclePhase phase)
        {
            _phaseChangedTelemetryHandled = true;
            _phaseChangedTelemetryHandledRunId = runId;
            _phaseChangedTelemetryHandledDay = day;
            _phaseChangedTelemetryHandledPhase = phase;
        }

        private void SuppressSessionStartTelemetryForRestoredRun()
        {
            if (string.IsNullOrWhiteSpace(_currentRunId))
                return;

            _runStartedTelemetrySuppressedRunId = _currentRunId;
            if (!ContinuousSiegeCycle.Enabled)
                return;

            int day = ContinuousSiegeCycle.CycleIndex >= int.MaxValue
                ? int.MaxValue
                : Math.Max(1, ContinuousSiegeCycle.CycleIndex + 1);
            MarkPhaseChangedTelemetryHandled(
                _currentRunId,
                day,
                ContinuousSiegeCycle.Phase);
        }

        private void TryEmitResourceSpentTelemetry(
            ResourceCost cost,
            string purchaseType,
            int resultingLevel,
            int resultingCount)
        {
            if (freeEconomyTestMode)
                return;

            System.Collections.Generic.List<ResourceSpentTelemetryPayload> payloads =
                ResourceSpentTelemetryFactory.Create(
                    cost,
                    purchaseType,
                    resultingLevel,
                    resultingCount);
            for (int i = 0; i < payloads.Count; i++)
                TryEmitResourceSpentTelemetry(payloads[i]);
        }

        private void TryEmitResourceSpentTelemetry(
            string resource,
            long amount,
            string purchaseType,
            int resultingLevel,
            int resultingCount)
        {
            TryEmitResourceSpentTelemetry(ResourceSpentTelemetryFactory.CreateSingle(
                resource,
                amount,
                purchaseType,
                resultingLevel,
                resultingCount));
        }

        private void TryEmitResourceSpentTelemetry(ResourceSpentTelemetryPayload payload)
        {
            if (!_initialized || string.IsNullOrWhiteSpace(_currentRunId)
                || !IsRunIdentityReadyForPhaseTelemetry())
            {
                return;
            }

            if (!GameplayTelemetry.TryEmitResourceSpent(
                    _currentRunId,
                    payload,
                    out _,
                    out string error))
            {
                UnityEngine.Debug.LogError(
                    $"[GameManager] resource_spent telemetry reddedildi: {error}");
            }
        }

        private void TryEmitArcherChangedTelemetry(ArcherChangedTelemetryPayload payload)
        {
            if (!_initialized || string.IsNullOrWhiteSpace(_currentRunId)
                || !IsRunIdentityReadyForPhaseTelemetry())
            {
                return;
            }

            if (!GameplayTelemetry.TryEmitArcherChanged(
                    _currentRunId,
                    payload,
                    out _,
                    out string error))
            {
                UnityEngine.Debug.LogError(
                    $"[GameManager] archer_changed telemetry reddedildi: {error}");
            }
        }

        private void TryEmitHeartNodeBoughtTelemetry(HeartNodeBoughtTelemetryPayload payload)
        {
            if (!_initialized || string.IsNullOrWhiteSpace(_currentRunId)
                || !IsRunIdentityReadyForPhaseTelemetry())
            {
                return;
            }

            if (!GameplayTelemetry.TryEmitHeartNodeBought(
                    _currentRunId,
                    payload,
                    out _,
                    out string error))
            {
                UnityEngine.Debug.LogError(
                    $"[GameManager] heart_node_bought telemetry reddedildi: {error}");
            }
        }

        private void TryEmitCouncilResolvedTelemetry(
            int day,
            ComposedCouncilEvent councilEvent,
            ComposedCouncilOption option,
            string resolution)
        {
            if (!_initialized || string.IsNullOrWhiteSpace(_currentRunId)
                || !IsRunIdentityReadyForPhaseTelemetry())
            {
                return;
            }

            CouncilResolvedTelemetryPayload payload = option == null
                ? CouncilResolvedTelemetryFactory.CreateExpired(day, councilEvent)
                : CouncilResolvedTelemetryFactory.Create(day, councilEvent, option, resolution);
            if (!GameplayTelemetry.TryEmitCouncilResolved(
                    _currentRunId,
                    payload,
                    out _,
                    out string error))
            {
                UnityEngine.Debug.LogError(
                    $"[GameManager] council_resolved telemetry reddedildi: {error}");
            }
        }

        private SiegeCyclePhase ResolveTelemetryPhase()
        {
            return TryGetContinuousSiegeCycle(out ContinuousSiegeCycleData cycle)
                ? cycle.Phase
                : ContinuousSiegeCycle.Phase;
        }

        private void TryEmitAbilityCastTelemetry(AbilityCastTelemetryPayload payload)
        {
            if (!_initialized || string.IsNullOrWhiteSpace(_currentRunId)
                || !IsRunIdentityReadyForPhaseTelemetry())
            {
                return;
            }

            if (!GameplayTelemetry.TryEmitAbilityCast(
                    _currentRunId,
                    payload,
                    out _,
                    out string error))
            {
                UnityEngine.Debug.LogError(
                    $"[GameManager] ability_cast telemetry reddedildi: {error}");
            }
        }

        private void TryEmitWallRepairedTelemetry(
            SiegeCyclePhase phase,
            int stoneCost,
            float hpBefore,
            float hpAfter)
        {
            if (freeEconomyTestMode || !_initialized || string.IsNullOrWhiteSpace(_currentRunId)
                || !IsRunIdentityReadyForPhaseTelemetry())
            {
                return;
            }

            WallRepairedTelemetryPayload payload = WallRepairedTelemetryFactory.Create(
                phase,
                stoneCost,
                hpBefore,
                hpAfter);
            if (!GameplayTelemetry.TryEmitWallRepaired(
                    _currentRunId,
                    payload,
                    out _,
                    out string error))
            {
                UnityEngine.Debug.LogError(
                    $"[GameManager] wall_repaired telemetry reddedildi: {error}");
            }
        }

        private void CaptureRunTelemetryForSave(RunSaveState save)
        {
            if (save == null)
                return;

            save.TelemetryPeakEnemies = Math.Max(0, WaveState.ZombiesAlive);
            if (_entityManager.HasComponent<RunTelemetryData>(_gameStateEntity))
            {
                RunTelemetryData telemetry =
                    _entityManager.GetComponentData<RunTelemetryData>(_gameStateEntity);
                save.TelemetryPeakEnemies = Math.Max(
                    save.TelemetryPeakEnemies,
                    telemetry.PeakEnemies);
            }

            if (save.WallDamageTimeline == null)
                save.WallDamageTimeline = new List<RunWallDamageTelemetrySaveState>();
            else
                save.WallDamageTimeline.Clear();

            if (!_entityManager.HasBuffer<RunWallDamageTelemetryElement>(_gameStateEntity))
                return;

            var timeline =
                _entityManager.GetBuffer<RunWallDamageTelemetryElement>(_gameStateEntity, true);
            for (int i = 0; i < timeline.Length; i++)
            {
                RunWallDamageTelemetryElement entry = timeline[i];
                save.WallDamageTimeline.Add(new RunWallDamageTelemetrySaveState
                {
                    Day = entry.Day,
                    Phase = (int)entry.Phase,
                    Damage = entry.Damage
                });
            }
        }

        private void RestoreRunTelemetryFromSave(RunSaveState save)
        {
            if (save == null || !_entityManager.Exists(_gameStateEntity))
                return;

            int currentAlive = _entityManager.HasComponent<WaveStateData>(_gameStateEntity)
                ? Math.Max(0,
                    _entityManager.GetComponentData<WaveStateData>(_gameStateEntity).ZombiesAlive)
                : 0;
            if (_entityManager.HasComponent<RunTelemetryData>(_gameStateEntity))
            {
                _entityManager.SetComponentData(_gameStateEntity, new RunTelemetryData
                {
                    PeakEnemies = Math.Max(currentAlive, save.TelemetryPeakEnemies)
                });
            }

            if (!_entityManager.HasBuffer<RunWallDamageTelemetryElement>(_gameStateEntity))
                return;

            var timeline =
                _entityManager.GetBuffer<RunWallDamageTelemetryElement>(_gameStateEntity);
            timeline.Clear();
            if (save.WallDamageTimeline == null)
                return;

            for (int i = 0; i < save.WallDamageTimeline.Count; i++)
            {
                RunWallDamageTelemetrySaveState entry = save.WallDamageTimeline[i];
                if (entry == null)
                    continue;

                timeline.Add(new RunWallDamageTelemetryElement
                {
                    Day = entry.Day,
                    Phase = (SiegeCyclePhase)entry.Phase,
                    Damage = entry.Damage
                });
            }
        }

        private void ResetRunTelemetryState()
        {
            if (!_entityManager.Exists(_gameStateEntity))
                return;

            if (_entityManager.HasComponent<RunTelemetryData>(_gameStateEntity))
            {
                _entityManager.SetComponentData(
                    _gameStateEntity,
                    new RunTelemetryData { PeakEnemies = 0 });
            }
            if (_entityManager.HasBuffer<RunWallDamageTelemetryElement>(_gameStateEntity))
            {
                _entityManager.GetBuffer<RunWallDamageTelemetryElement>(_gameStateEntity).Clear();
            }
        }

        private void TryEmitRunEndedTelemetry(
            int day,
            int kills,
            int peakPopulation,
            MetaRunResult result)
        {
            if (!result.Persisted || string.IsNullOrWhiteSpace(_currentRunId)
                || string.Equals(_runEndedTelemetryHandledRunId, _currentRunId,
                    StringComparison.Ordinal))
            {
                return;
            }

            int peakEnemies = Math.Max(0, WaveState.ZombiesAlive);
            var wallDamage = new List<RunEndedWallDamageTelemetryEntry>();
            if (_entityManager.Exists(_gameStateEntity))
            {
                if (_entityManager.HasComponent<RunTelemetryData>(_gameStateEntity))
                {
                    RunTelemetryData telemetry =
                        _entityManager.GetComponentData<RunTelemetryData>(_gameStateEntity);
                    peakEnemies = Math.Max(peakEnemies, telemetry.PeakEnemies);
                }

                if (_entityManager.HasBuffer<RunWallDamageTelemetryElement>(_gameStateEntity))
                {
                    var timeline = _entityManager.GetBuffer<RunWallDamageTelemetryElement>(
                        _gameStateEntity,
                        true);
                    for (int i = 0; i < timeline.Length; i++)
                    {
                        RunWallDamageTelemetryElement entry = timeline[i];
                        wallDamage.Add(new RunEndedWallDamageTelemetryEntry
                        {
                            Day = entry.Day,
                            Phase = PhaseChangedTelemetryFactory.ToContractPhase(entry.Phase),
                            Damage = entry.Damage
                        });
                    }
                }
            }

            RunEndedTelemetryPayload payload = RunEndedTelemetryFactory.Create(
                day,
                kills,
                peakEnemies,
                peakPopulation,
                wallDamage,
                result.Reward.TotalSouls);
            _runEndedTelemetryHandledRunId = _currentRunId;
            if (!GameplayTelemetry.TryEmitRunEnded(
                    _currentRunId,
                    payload,
                    out _,
                    out string error))
            {
                UnityEngine.Debug.LogError(
                    $"[GameManager] run_ended telemetry reddedildi: {error}");
            }
        }
    }
}
