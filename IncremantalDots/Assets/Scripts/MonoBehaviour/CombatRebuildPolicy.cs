using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Save aninda okunan tek bir aktif zombie'nin gecici capture verisi. Disk semasi degildir.
    /// </summary>
    public struct CombatRebuildCaptureSample
    {
        public float3 Position;
        public float Scale;
        public float MoveSpeed;
        public float MaxHP;
        public float CurrentHP;
        public float AttackDamage;
        public float AttackCooldown;
        public float AttackTimer;
        public int XPReward;
        public int State;
        public bool SlowEnabled;
        public float SlowDuration;
        public float SlowMultiplier;
        public float2 Velocity;
        public float2 Force;
        public bool HasDeathTimer;
        public float DeathTimer;
    }

    /// <summary>
    /// 10K combat alanini entity basina pozisyon yazmadan, sabit spatial/state/HP bucket'lari
    /// ve snapshot'a ait seed ile perceptually faithful olarak yeniden kurar.
    /// </summary>
    public static class CombatRebuildUtility
    {
        public const int CurrentPolicyVersion = 1;
        public const int DefaultXCellCount = 24;
        public const int DefaultYCellCount = 16;
        public const int DefaultHealthBandCount = 4;

        private const float MinimumAxisExtent = 0.01f;

        public static uint CreateSeed(uint spawnRandomState, int cycleIndex, int totalKills, int zombieCount)
        {
            uint seed = spawnRandomState != 0u ? spawnRandomState : 0x9E3779B9u;
            seed = Mix(seed ^ unchecked((uint)cycleIndex * 0x85EBCA6Bu));
            seed = Mix(seed ^ unchecked((uint)totalKills * 0xC2B2AE35u));
            seed = Mix(seed ^ unchecked((uint)zombieCount * 0x27D4EB2Fu));
            return seed != 0u ? seed : 1u;
        }

        public static CombatRebuildRunSaveState BuildSnapshot(
            IReadOnlyList<CombatRebuildCaptureSample> samples,
            uint seed,
            out int[] bucketIndicesBySample)
        {
            int sampleCount = samples != null ? samples.Count : 0;
            bucketIndicesBySample = new int[sampleCount];
            var snapshot = new CombatRebuildRunSaveState
            {
                PolicyVersion = CurrentPolicyVersion,
                Seed = seed != 0u ? seed : 1u,
                TotalZombies = sampleCount,
                XCellCount = DefaultXCellCount,
                YCellCount = DefaultYCellCount,
                HealthBandCount = DefaultHealthBandCount,
                Buckets = new List<CombatRebuildBucketRunSaveState>()
            };

            if (sampleCount == 0)
            {
                snapshot.MinX = 0f;
                snapshot.MaxX = MinimumAxisExtent;
                snapshot.MinY = 0f;
                snapshot.MaxY = MinimumAxisExtent;
                return snapshot;
            }

            ResolveBounds(samples, out float minX, out float maxX, out float minY, out float maxY);
            snapshot.MinX = minX;
            snapshot.MaxX = maxX;
            snapshot.MinY = minY;
            snapshot.MaxY = maxY;

            var accumulators = new Dictionary<BucketKey, BucketAccumulator>();
            var sampleKeys = new BucketKey[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                CombatRebuildCaptureSample sample = Sanitize(samples[i]);
                var key = new BucketKey(
                    ResolveCell(sample.Position.x, minX, maxX, snapshot.XCellCount),
                    ResolveCell(sample.Position.y, minY, maxY, snapshot.YCellCount),
                    Mathf.Clamp(sample.State, (int)ZombieStateType.Moving, (int)ZombieStateType.Queued),
                    ResolveHealthBand(sample.CurrentHP, sample.MaxHP, snapshot.HealthBandCount),
                    sample.SlowEnabled,
                    sample.HasDeathTimer);
                sampleKeys[i] = key;

                if (!accumulators.TryGetValue(key, out BucketAccumulator accumulator))
                {
                    accumulator = new BucketAccumulator();
                    accumulators.Add(key, accumulator);
                }
                accumulator.Add(sample);
            }

            var sortedKeys = new List<BucketKey>(accumulators.Keys);
            sortedKeys.Sort(BucketKey.Compare);
            var sortedIndexByKey = new Dictionary<BucketKey, int>(sortedKeys.Count);
            for (int i = 0; i < sortedKeys.Count; i++)
            {
                BucketKey key = sortedKeys[i];
                sortedIndexByKey.Add(key, i);
                snapshot.Buckets.Add(accumulators[key].ToSaveState(key));
            }

            for (int i = 0; i < sampleKeys.Length; i++)
                bucketIndicesBySample[i] = sortedIndexByKey[sampleKeys[i]];

            return snapshot;
        }

        public static bool IsValid(CombatRebuildRunSaveState snapshot, out string error)
        {
            error = string.Empty;
            if (snapshot == null)
            {
                error = "Payload null.";
                return false;
            }
            if (snapshot.PolicyVersion != CurrentPolicyVersion)
            {
                error = $"Policy v{snapshot.PolicyVersion} desteklenmiyor.";
                return false;
            }
            if (snapshot.Seed == 0u)
            {
                error = "Seed sifir olamaz.";
                return false;
            }
            if (snapshot.TotalZombies < 0)
            {
                error = "TotalZombies negatif.";
                return false;
            }
            if (snapshot.XCellCount <= 0 || snapshot.XCellCount > 256
                || snapshot.YCellCount <= 0 || snapshot.YCellCount > 256
                || snapshot.HealthBandCount <= 0 || snapshot.HealthBandCount > 32)
            {
                error = "Cell/HP band boyutu policy siniri disinda.";
                return false;
            }
            if (!IsFinite(snapshot.MinX) || !IsFinite(snapshot.MaxX)
                || !IsFinite(snapshot.MinY) || !IsFinite(snapshot.MaxY)
                || snapshot.MaxX <= snapshot.MinX || snapshot.MaxY <= snapshot.MinY)
            {
                error = "Spatial bounds gecersiz.";
                return false;
            }
            if (snapshot.Buckets == null)
            {
                error = "Bucket listesi null.";
                return false;
            }

            long total = 0L;
            for (int i = 0; i < snapshot.Buckets.Count; i++)
            {
                CombatRebuildBucketRunSaveState bucket = snapshot.Buckets[i];
                if (bucket == null || bucket.Count <= 0)
                {
                    error = $"Bucket {i} bos/gecersiz.";
                    return false;
                }
                if (bucket.XCell < 0 || bucket.XCell >= snapshot.XCellCount
                    || bucket.YCell < 0 || bucket.YCell >= snapshot.YCellCount
                    || bucket.HealthBand < 0 || bucket.HealthBand >= snapshot.HealthBandCount
                    || bucket.State < (int)ZombieStateType.Moving
                    || bucket.State > (int)ZombieStateType.Queued)
                {
                    error = $"Bucket {i} key siniri disinda.";
                    return false;
                }
                if (!HasFinitePayload(bucket))
                {
                    error = $"Bucket {i} finite olmayan runtime degeri tasiyor.";
                    return false;
                }

                total += bucket.Count;
                if (total > int.MaxValue)
                {
                    error = "Bucket toplam zombie sayisi int sinirini asti.";
                    return false;
                }
            }

            if (total != snapshot.TotalZombies)
            {
                error = $"Bucket toplamı {total}, TotalZombies {snapshot.TotalZombies}.";
                return false;
            }

            return true;
        }

        public static float3 GetRebuiltPosition(
            CombatRebuildRunSaveState snapshot,
            int bucketIndex,
            int itemIndex)
        {
            CombatRebuildBucketRunSaveState bucket = snapshot.Buckets[bucketIndex];
            int itemCount = Mathf.Max(1, bucket.Count);
            int columnCount = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(itemCount)));
            int rowCount = Mathf.Max(1, Mathf.CeilToInt(itemCount / (float)columnCount));
            int column = itemIndex % columnCount;
            int row = itemIndex / columnCount;

            uint baseHash = Mix(snapshot.Seed
                                ^ unchecked((uint)(bucketIndex + 1) * 0x9E3779B9u)
                                ^ unchecked((uint)(itemIndex + 1) * 0x85EBCA6Bu));
            float jitterX = Hash01(baseHash);
            float jitterY = Hash01(Mix(baseHash ^ 0xC2B2AE35u));
            float u = Mathf.Clamp01((column + 0.1f + jitterX * 0.8f) / columnCount);
            float v = Mathf.Clamp01((row + 0.1f + jitterY * 0.8f) / rowCount);

            float cellWidth = (snapshot.MaxX - snapshot.MinX) / snapshot.XCellCount;
            float cellHeight = (snapshot.MaxY - snapshot.MinY) / snapshot.YCellCount;
            float x = snapshot.MinX + (bucket.XCell + u) * cellWidth;
            float y = snapshot.MinY + (bucket.YCell + v) * cellHeight;
            return new float3(x, y, bucket.Z);
        }

        public static int SelectTargetOrdinal(
            uint seed,
            int bucketIndex,
            int arrowIndex,
            int bucketCount)
        {
            if (bucketCount <= 1)
                return 0;

            uint hash = Mix(seed
                            ^ unchecked((uint)(bucketIndex + 1) * 0x27D4EB2Fu)
                            ^ unchecked((uint)(arrowIndex + 1) * 0x165667B1u));
            return (int)(hash % (uint)bucketCount);
        }

        private static void ResolveBounds(
            IReadOnlyList<CombatRebuildCaptureSample> samples,
            out float minX,
            out float maxX,
            out float minY,
            out float maxY)
        {
            minX = maxX = SafeFinite(samples[0].Position.x);
            minY = maxY = SafeFinite(samples[0].Position.y);
            for (int i = 1; i < samples.Count; i++)
            {
                float x = SafeFinite(samples[i].Position.x);
                float y = SafeFinite(samples[i].Position.y);
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }

            ExpandDegenerateBounds(ref minX, ref maxX);
            ExpandDegenerateBounds(ref minY, ref maxY);
        }

        private static void ExpandDegenerateBounds(ref float min, ref float max)
        {
            if (max - min >= MinimumAxisExtent)
                return;

            float center = (min + max) * 0.5f;
            min = center - MinimumAxisExtent * 0.5f;
            max = center + MinimumAxisExtent * 0.5f;
        }

        private static int ResolveCell(float value, float min, float max, int cellCount)
        {
            float normalized = Mathf.InverseLerp(min, max, SafeFinite(value));
            return Mathf.Clamp(Mathf.FloorToInt(normalized * cellCount), 0, cellCount - 1);
        }

        private static int ResolveHealthBand(float currentHp, float maxHp, int bandCount)
        {
            float safeMax = Mathf.Max(0.0001f, SafeFinite(maxHp, 1f));
            float ratio = Mathf.Clamp01(SafeFinite(currentHp) / safeMax);
            return Mathf.Clamp(Mathf.FloorToInt(ratio * bandCount), 0, bandCount - 1);
        }

        private static CombatRebuildCaptureSample Sanitize(CombatRebuildCaptureSample sample)
        {
            sample.Position = new float3(
                SafeFinite(sample.Position.x),
                SafeFinite(sample.Position.y),
                SafeFinite(sample.Position.z, MobileCastleRenderDepth.UnitZ));
            sample.Scale = Mathf.Max(0.01f, SafeFinite(sample.Scale, 1f));
            sample.MoveSpeed = Mathf.Max(0f, SafeFinite(sample.MoveSpeed));
            sample.MaxHP = Mathf.Max(0.0001f, SafeFinite(sample.MaxHP, 1f));
            sample.CurrentHP = Mathf.Clamp(SafeFinite(sample.CurrentHP), 0f, sample.MaxHP);
            sample.AttackDamage = Mathf.Max(0f, SafeFinite(sample.AttackDamage));
            sample.AttackCooldown = Mathf.Max(0.0001f, SafeFinite(sample.AttackCooldown, 1f));
            sample.AttackTimer = SafeFinite(sample.AttackTimer);
            sample.XPReward = Mathf.Max(0, sample.XPReward);
            sample.SlowDuration = Mathf.Max(0f, SafeFinite(sample.SlowDuration));
            sample.SlowMultiplier = Mathf.Max(0f, SafeFinite(sample.SlowMultiplier, 1f));
            sample.Velocity = new float2(
                SafeFinite(sample.Velocity.x), SafeFinite(sample.Velocity.y));
            sample.Force = new float2(SafeFinite(sample.Force.x), SafeFinite(sample.Force.y));
            sample.DeathTimer = SafeFinite(sample.DeathTimer);
            return sample;
        }

        private static bool HasFinitePayload(CombatRebuildBucketRunSaveState bucket)
        {
            return IsFinite(bucket.Z)
                   && IsFinite(bucket.Scale) && bucket.Scale > 0f
                   && IsFinite(bucket.MoveSpeed) && bucket.MoveSpeed >= 0f
                   && IsFinite(bucket.MaxHP) && bucket.MaxHP > 0f
                   && IsFinite(bucket.CurrentHP) && bucket.CurrentHP >= 0f
                   && IsFinite(bucket.AttackDamage) && bucket.AttackDamage >= 0f
                   && IsFinite(bucket.AttackCooldown) && bucket.AttackCooldown > 0f
                   && IsFinite(bucket.AttackTimer)
                   && bucket.XPReward >= 0
                   && IsFinite(bucket.SlowDuration) && bucket.SlowDuration >= 0f
                   && IsFinite(bucket.SlowMultiplier) && bucket.SlowMultiplier >= 0f
                   && IsFinite(bucket.VelocityX) && IsFinite(bucket.VelocityY)
                   && IsFinite(bucket.ForceX) && IsFinite(bucket.ForceY)
                   && IsFinite(bucket.DeathTimer);
        }

        private static float SafeFinite(float value, float fallback = 0f)
        {
            return IsFinite(value) ? value : fallback;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float Hash01(uint value)
        {
            return (value & 0x00FFFFFFu) / 16777216f;
        }

        private static uint Mix(uint value)
        {
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return value;
        }

        private readonly struct BucketKey : IEquatable<BucketKey>
        {
            public readonly int XCell;
            public readonly int YCell;
            public readonly int State;
            public readonly int HealthBand;
            public readonly int Flags;

            public BucketKey(
                int xCell,
                int yCell,
                int state,
                int healthBand,
                bool slowEnabled,
                bool hasDeathTimer)
            {
                XCell = xCell;
                YCell = yCell;
                State = state;
                HealthBand = healthBand;
                Flags = (slowEnabled ? 1 : 0) | (hasDeathTimer ? 2 : 0);
            }

            public bool SlowEnabled => (Flags & 1) != 0;
            public bool HasDeathTimer => (Flags & 2) != 0;

            public static int Compare(BucketKey a, BucketKey b)
            {
                int value = a.XCell.CompareTo(b.XCell);
                if (value != 0) return value;
                value = a.YCell.CompareTo(b.YCell);
                if (value != 0) return value;
                value = a.State.CompareTo(b.State);
                if (value != 0) return value;
                value = a.HealthBand.CompareTo(b.HealthBand);
                return value != 0 ? value : a.Flags.CompareTo(b.Flags);
            }

            public bool Equals(BucketKey other)
            {
                return XCell == other.XCell
                       && YCell == other.YCell
                       && State == other.State
                       && HealthBand == other.HealthBand
                       && Flags == other.Flags;
            }

            public override bool Equals(object obj)
            {
                return obj is BucketKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = XCell;
                    hash = (hash * 397) ^ YCell;
                    hash = (hash * 397) ^ State;
                    hash = (hash * 397) ^ HealthBand;
                    return (hash * 397) ^ Flags;
                }
            }
        }

        private sealed class BucketAccumulator
        {
            private int _count;
            private double _z;
            private double _scale;
            private double _moveSpeed;
            private double _maxHp;
            private double _currentHp;
            private double _attackDamage;
            private double _attackCooldown;
            private double _attackTimer;
            private long _xpReward;
            private double _slowDuration;
            private double _slowMultiplier;
            private double _velocityX;
            private double _velocityY;
            private double _forceX;
            private double _forceY;
            private double _deathTimer;

            public void Add(CombatRebuildCaptureSample sample)
            {
                _count++;
                _z += sample.Position.z;
                _scale += sample.Scale;
                _moveSpeed += sample.MoveSpeed;
                _maxHp += sample.MaxHP;
                _currentHp += sample.CurrentHP;
                _attackDamage += sample.AttackDamage;
                _attackCooldown += sample.AttackCooldown;
                _attackTimer += sample.AttackTimer;
                _xpReward += sample.XPReward;
                _slowDuration += sample.SlowDuration;
                _slowMultiplier += sample.SlowMultiplier;
                _velocityX += sample.Velocity.x;
                _velocityY += sample.Velocity.y;
                _forceX += sample.Force.x;
                _forceY += sample.Force.y;
                _deathTimer += sample.DeathTimer;
            }

            public CombatRebuildBucketRunSaveState ToSaveState(BucketKey key)
            {
                double divisor = Math.Max(1, _count);
                return new CombatRebuildBucketRunSaveState
                {
                    XCell = key.XCell,
                    YCell = key.YCell,
                    State = key.State,
                    HealthBand = key.HealthBand,
                    Count = _count,
                    SlowEnabled = key.SlowEnabled,
                    HasDeathTimer = key.HasDeathTimer,
                    Z = (float)(_z / divisor),
                    Scale = (float)(_scale / divisor),
                    MoveSpeed = (float)(_moveSpeed / divisor),
                    MaxHP = (float)(_maxHp / divisor),
                    CurrentHP = (float)(_currentHp / divisor),
                    AttackDamage = (float)(_attackDamage / divisor),
                    AttackCooldown = (float)(_attackCooldown / divisor),
                    AttackTimer = (float)(_attackTimer / divisor),
                    XPReward = Mathf.Max(0, (int)Math.Round(_xpReward / divisor)),
                    SlowDuration = (float)(_slowDuration / divisor),
                    SlowMultiplier = (float)(_slowMultiplier / divisor),
                    VelocityX = (float)(_velocityX / divisor),
                    VelocityY = (float)(_velocityY / divisor),
                    ForceX = (float)(_forceX / divisor),
                    ForceY = (float)(_forceY / divisor),
                    DeathTimer = (float)(_deathTimer / divisor)
                };
            }
        }
    }
}
