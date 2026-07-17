using Unity.Entities;

namespace DeadWalls
{
    public struct GameStateData : IComponentData
    {
        public int XP;
        public int Level;
        public int XPToNextLevel;
        public bool IsGameOver;
        public bool IsLevelUpPending;
        // Kosu boyunca oldurulen toplam zombi — meta-progression kazanim kaynagi
        // Meta reward input'i; DamageCleanupSystem sayar, GameOver'da MetaRewardCalculator agirliklandirir.
        public int TotalKills;
    }

    /// <summary>
    /// Run sonu telemetry'sinin scalar ECS accumulator'i. Ayri bir singleton owner kurmaz;
    /// mevcut GameState entity'sinde WaveStateData ile birlikte yasar.
    /// </summary>
    public struct RunTelemetryData : IComponentData
    {
        public int PeakEnemies;
    }

    /// <summary>
    /// Wall'a gercekten uygulanmis hasari day/phase bucket'larinda biriktirir.
    /// Per-hit event tutmadigi icin buyuk hordelerde allocation ve payload patlamasi yaratmaz.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct RunWallDamageTelemetryElement : IBufferElementData
    {
        public int Day;
        public SiegeCyclePhase Phase;
        public float Damage;
    }

    public static class RunTelemetryAccumulator
    {
        public static void ObserveEnemyCount(ref RunTelemetryData telemetry, int aliveEnemies)
        {
            if (aliveEnemies > telemetry.PeakEnemies)
                telemetry.PeakEnemies = aliveEnemies;
        }

        public static void RecordWallDamage(
            DynamicBuffer<RunWallDamageTelemetryElement> timeline,
            int day,
            SiegeCyclePhase phase,
            float damage)
        {
            if (!timeline.IsCreated || day < 1 || damage <= 0f
                || float.IsNaN(damage) || float.IsInfinity(damage))
            {
                return;
            }

            if (timeline.Length > 0)
            {
                int lastIndex = timeline.Length - 1;
                RunWallDamageTelemetryElement last = timeline[lastIndex];
                if (last.Day == day && last.Phase == phase)
                {
                    double total = (double)last.Damage + damage;
                    last.Damage = total >= float.MaxValue ? float.MaxValue : (float)total;
                    timeline[lastIndex] = last;
                    return;
                }
            }

            timeline.Add(new RunWallDamageTelemetryElement
            {
                Day = day,
                Phase = phase,
                Damage = damage
            });
        }
    }

    public enum RunPhaseType : byte
    {
        DayPrep,
        NightCombat
    }

    public struct WaveStateData : IComponentData
    {
        public int CurrentWave;
        public int ZombiesToSpawn;
        public int ZombiesSpawned;
        public int ZombiesAlive;
        public float SpawnTimer;
        public float SpawnInterval;
        public float ZombieHP;
        public float ZombieDamage;
        public float ZombieSpeed;
        public bool WaveActive;
        public RunPhaseType Phase;
        public float PrepTimer;
        public float PrepDuration;
        public float WaveStartDelay;
        public float WaveStartTimer;
        public bool StressTestMode;
        // Exact save/Continue icin spawn RNG stream state'i. 0 ise default seed kullanilir.
        public uint SpawnRandomState;
    }
}
