using System;

namespace DeadWalls
{
    public partial class GameManager
    {
        private string _runStartedTelemetryEmittedRunId;
        private string _runStartedTelemetrySuppressedRunId;
        private string _runStartedTelemetryRejectedRunId;

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

        private void SuppressRunStartedTelemetryForRestoredRun()
        {
            if (!string.IsNullOrWhiteSpace(_currentRunId))
                _runStartedTelemetrySuppressedRunId = _currentRunId;
        }
    }
}
