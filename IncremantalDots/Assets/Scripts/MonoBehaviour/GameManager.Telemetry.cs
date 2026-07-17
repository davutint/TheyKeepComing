using System;

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
    }
}
