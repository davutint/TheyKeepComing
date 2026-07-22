using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace DeadWalls
{
    public static class SimulationSpeedUtility
    {
        public const float Normal = 1f;
        public const float Fast = 2f;
        public const float VeryFast = 3f;

        public static bool IsSupported(float timeScale)
        {
            return Mathf.Approximately(timeScale, Normal)
                   || Mathf.Approximately(timeScale, Fast)
                   || Mathf.Approximately(timeScale, VeryFast);
        }
    }

    public interface ISimulationPauseBackend
    {
        float TimeScale { get; set; }
        bool TryGetSimulationEnabled(out bool enabled);
        void SetSimulationEnabled(bool enabled);
    }

    /// <summary>
    /// Birden fazla modal yuzeyin ayni simulation pause'unu guvenle paylasmasini saglar.
    /// Ilk lease onceki state'i yakalar; son lease kapaninca exact state geri yuklenir.
    /// </summary>
    public sealed class SimulationPauseCoordinator
    {
        private readonly ISimulationPauseBackend _backend;
        private readonly HashSet<long> _activeLeases = new HashSet<long>();
        private long _nextLeaseId;
        private float _previousTimeScale = 1f;
        private bool _previousSimulationEnabled;
        private bool _capturedSimulationState;

        public SimulationPauseCoordinator(ISimulationPauseBackend backend)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        public bool IsPaused => _activeLeases.Count > 0;
        public int ActiveLeaseCount => _activeLeases.Count;
        public float RunningTimeScale => IsPaused ? _previousTimeScale : _backend.TimeScale;

        public bool TrySetRunningTimeScale(float timeScale)
        {
            if (!SimulationSpeedUtility.IsSupported(timeScale))
                return false;

            if (IsPaused)
                _previousTimeScale = timeScale;
            else
                _backend.TimeScale = timeScale;
            return true;
        }

        public IDisposable Acquire(string owner)
        {
            if (string.IsNullOrWhiteSpace(owner))
                throw new ArgumentException("Pause lease owner bos olamaz.", nameof(owner));

            if (_activeLeases.Count == 0)
            {
                _previousTimeScale = _backend.TimeScale;
                _capturedSimulationState = _backend.TryGetSimulationEnabled(
                    out _previousSimulationEnabled);
            }

            long leaseId = ++_nextLeaseId;
            _activeLeases.Add(leaseId);
            EnforcePausedState();
            return new Lease(this, leaseId, owner);
        }

        public void EnforcePausedState()
        {
            if (!IsPaused)
                return;

            _backend.TimeScale = 0f;
            _backend.SetSimulationEnabled(false);
        }

        private void Release(long leaseId)
        {
            if (!_activeLeases.Remove(leaseId) || _activeLeases.Count > 0)
                return;

            if (_capturedSimulationState)
                _backend.SetSimulationEnabled(_previousSimulationEnabled);
            _backend.TimeScale = _previousTimeScale;
            _capturedSimulationState = false;
        }

        private sealed class Lease : IDisposable
        {
            private SimulationPauseCoordinator _owner;
            private readonly long _leaseId;

            public Lease(SimulationPauseCoordinator owner, long leaseId, string debugOwner)
            {
                _owner = owner;
                _leaseId = leaseId;
                DebugOwner = debugOwner;
            }

            public string DebugOwner { get; }

            public void Dispose()
            {
                SimulationPauseCoordinator owner = _owner;
                _owner = null;
                owner?.Release(_leaseId);
            }
        }
    }

    public static class SimulationPauseService
    {
        private static SimulationPauseCoordinator _coordinator = CreateCoordinator();

        public static bool IsPaused => _coordinator.IsPaused;
        public static int ActiveLeaseCount => _coordinator.ActiveLeaseCount;
        public static float RunningTimeScale => _coordinator.RunningTimeScale;

        public static bool TrySetRunningTimeScale(float timeScale)
        {
            return _coordinator.TrySetRunningTimeScale(timeScale);
        }

        public static IDisposable Acquire(string owner)
        {
            return _coordinator.Acquire(owner);
        }

        public static void EnforcePausedState()
        {
            _coordinator.EnforcePausedState();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewPlayerLoop()
        {
            _coordinator = CreateCoordinator();
        }

        private static SimulationPauseCoordinator CreateCoordinator()
        {
            return new SimulationPauseCoordinator(new UnitySimulationPauseBackend());
        }

        private sealed class UnitySimulationPauseBackend : ISimulationPauseBackend
        {
            public float TimeScale
            {
                get => Time.timeScale;
                set => Time.timeScale = value;
            }

            public bool TryGetSimulationEnabled(out bool enabled)
            {
                SimulationSystemGroup group = GetSimulationGroup();
                enabled = group != null && group.Enabled;
                return group != null;
            }

            public void SetSimulationEnabled(bool enabled)
            {
                SimulationSystemGroup group = GetSimulationGroup();
                if (group != null)
                    group.Enabled = enabled;
            }

            private static SimulationSystemGroup GetSimulationGroup()
            {
                World world = World.DefaultGameObjectInjectionWorld;
                return world != null && world.IsCreated
                    ? world.GetExistingSystemManaged<SimulationSystemGroup>()
                    : null;
            }
        }
    }
}
