#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    public static class DevelopmentTestRules
    {
        public const int Horde2K = 2_000;
        public const int Horde5K = 5_000;
        public const int Horde10K = 10_000;
        public const float HorizontalSpacing = 0.12f;
        public const float VerticalSpacing = 0.13f;

        public static bool IsSupportedHordeSize(int count)
        {
            return count == Horde2K || count == Horde5K || count == Horde10K;
        }

        public static float3 GetGridPosition(int index, int totalCount, float centerX)
        {
            int safeTotal = math.max(1, totalCount);
            int columns = math.max(1, (int)math.ceil(math.sqrt(safeTotal)));
            int rows = math.max(1, (safeTotal + columns - 1) / columns);
            int column = math.clamp(index, 0, safeTotal - 1) % columns;
            int row = math.clamp(index, 0, safeTotal - 1) / columns;

            return new float3(
                centerX + (column - (columns - 1) * 0.5f) * HorizontalSpacing,
                (row - (rows - 1) * 0.5f) * VerticalSpacing,
                MobileCastleRenderDepth.UnitZ);
        }
    }

    public partial class GameManager
    {
        private bool _developmentTestSessionActive;
        private int _developmentTestHordeTarget;
        private bool _developmentFreeEconomyCaptured;
        private bool _developmentOriginalFreeEconomyTestMode;
        private bool _developmentOriginalRapidUnlocked;
        private bool _developmentOriginalFrostUnlocked;
        private bool _developmentOriginalFireballUnlocked;
        private float _developmentOriginalFireballCooldown;
        private float _developmentOriginalRallyCooldown;
        private float _developmentOriginalRepairCooldown;

        public bool DevelopmentTestSessionActive => _developmentTestSessionActive;
        public int DevelopmentTestHordeTarget => _developmentTestHordeTarget;

        public bool TryEnableDevelopmentCombat(out string message)
        {
            if (!TryInitialize())
            {
                message = "GameManager/ECS is not ready yet.";
                return false;
            }

            if (_entityManager.Exists(_gameStateEntity))
            {
                GameStateData gameState = _entityManager.GetComponentData<GameStateData>(_gameStateEntity);
                if (gameState.IsGameOver)
                {
                    message = "Start a living run before using combat tests.";
                    return false;
                }

                gameState.IsLevelUpPending = false;
                _entityManager.SetComponentData(_gameStateEntity, gameState);
            }

            if (!_developmentFreeEconomyCaptured)
            {
                _developmentOriginalFreeEconomyTestMode = freeEconomyTestMode;
                _developmentOriginalRapidUnlocked = _unlockedArcherTypes.Contains(ArcherType.Rapid);
                _developmentOriginalFrostUnlocked = _unlockedArcherTypes.Contains(ArcherType.Frost);
                _developmentOriginalFireballUnlocked = _fireballUnlocked;
                _developmentOriginalFireballCooldown = _fireballCooldownRemaining;
                _developmentOriginalRallyCooldown = _rallyCooldownRemaining;
                _developmentOriginalRepairCooldown = _emergencyRepairCooldownRemaining;
                _developmentFreeEconomyCaptured = true;
            }

            _developmentTestSessionActive = true;
            freeEconomyTestMode = true;
            _unlockedArcherTypes.Add(ArcherType.Rapid);
            _unlockedArcherTypes.Add(ArcherType.Frost);
            _fireballUnlocked = true;
            _fireballCooldownRemaining = 0f;
            _rallyCooldownRemaining = 0f;
            _emergencyRepairCooldownRemaining = 0f;

            OnGameStateChanged?.Invoke();
            message = "Combat tech unlocked, cooldowns reset and recruitment is free for this Play Mode session.";
            return true;
        }

        public bool TryResetDevelopmentCooldowns(out string message)
        {
            if (!TryEnableDevelopmentCombat(out message))
                return false;

            message = "Fireball, Rally and Repair cooldowns are ready.";
            return true;
        }

        public bool TrySpawnDevelopmentHorde(int requestedCount, out int spawned, out string message)
        {
            spawned = 0;
            if (!DevelopmentTestRules.IsSupportedHordeSize(requestedCount))
            {
                message = "Only the exact 2K, 5K and 10K test sizes are supported.";
                return false;
            }

            if (!TryEnableDevelopmentCombat(out message)
                || !TryGetMobileConfigEntity(out Entity configEntity)
                || _enemyPoolEntity == Entity.Null
                || !_entityManager.Exists(_enemyPoolEntity)
                || !_entityManager.HasComponent<EnemyCatalogRuntimeData>(_enemyPoolEntity)
                || !_entityManager.HasBuffer<EnemyCatalogEntryData>(_enemyPoolEntity))
            {
                if (string.IsNullOrEmpty(message))
                    message = "Enemy catalog/pool is not ready.";
                return false;
            }

            _entityManager.CompleteAllTrackedJobs();
            EnemyPoolRuntimeUtility.ReturnAllActive(_entityManager, _enemyPoolEntity);
            using (EntityQuery nonPoolZombieQuery = _entityManager.CreateEntityQuery(new EntityQueryDesc
                   {
                       All = new[] { ComponentType.ReadOnly<ZombieTag>() },
                       None = new[]
                       {
                           ComponentType.ReadOnly<Unity.Entities.Prefab>(),
                           ComponentType.ReadOnly<EnemyPoolMember>()
                       }
                   }))
            {
                if (!nonPoolZombieQuery.IsEmpty)
                    _entityManager.DestroyEntity(nonPoolZombieQuery);
            }

            EnemyCatalogRuntimeData catalog =
                _entityManager.GetComponentData<EnemyCatalogRuntimeData>(_enemyPoolEntity);
            DynamicBuffer<EnemyCatalogEntryData> entries =
                _entityManager.GetBuffer<EnemyCatalogEntryData>(_enemyPoolEntity, true);
            int activeIndex = EnemyCatalogRuntimeUtility.ResolveActiveIndex(catalog, entries.Length);
            if (activeIndex < 0)
            {
                message = "Enemy catalog has no active entry.";
                return false;
            }

            EnemyCatalogEntryData definition = entries[activeIndex];
            MobileCastleCombatConfig config =
                _entityManager.GetComponentData<MobileCastleCombatConfig>(configEntity);
            float centerX = math.min(
                config.SpawnLineX - 6f,
                math.max(config.FrontlineX + 8f, 13f));

            for (int index = 0; index < requestedCount; index++)
            {
                if (!EnemyPoolRuntimeUtility.TryRent(
                        _entityManager, _enemyPoolEntity, out Entity zombie))
                {
                    break;
                }

                _entityManager.SetComponentData(
                    zombie,
                    LocalTransform.FromPositionRotationScale(
                        DevelopmentTestRules.GetGridPosition(index, requestedCount, centerX),
                        quaternion.identity,
                        definition.Scale));
                _entityManager.SetComponentData(
                    zombie,
                    new ZombieState { Value = ZombieStateType.Moving });
                _entityManager.SetComponentData(
                    zombie,
                    new ZombieStats
                    {
                        MoveSpeed = definition.BaseMoveSpeed,
                        MaxHP = definition.BaseHP,
                        CurrentHP = definition.BaseHP,
                        AttackDamage = 0f,
                        AttackCooldown = 1f,
                        AttackTimer = 0f,
                        XPReward = definition.XPReward
                    });
                spawned++;
            }

            config.MaxAliveZombies = requestedCount;
            config.StressMaxAliveZombies = requestedCount;
            _entityManager.SetComponentData(configEntity, config);

            if (_entityManager.HasComponent<ContinuousSpawnBudgetData>(configEntity))
            {
                ContinuousSpawnBudgetData budget =
                    _entityManager.GetComponentData<ContinuousSpawnBudgetData>(configEntity);
                budget.PendingEnemies = 0;
                budget.LastDemandedEnemies = 0;
                budget.LastSpawnedEnemies = 0;
                _entityManager.SetComponentData(configEntity, budget);
            }

            WaveStateData wave = _entityManager.GetComponentData<WaveStateData>(_waveStateEntity);
            wave.CurrentWave = math.max(1, wave.CurrentWave);
            wave.ZombiesToSpawn = spawned;
            wave.ZombiesSpawned = spawned;
            wave.ZombiesAlive = spawned;
            wave.SpawnTimer = float.MaxValue;
            wave.WaveActive = true;
            wave.Phase = RunPhaseType.NightCombat;
            wave.PrepTimer = 0f;
            wave.WaveStartTimer = 0f;
            wave.StressTestMode = false;
            _entityManager.SetComponentData(_waveStateEntity, wave);

            _developmentTestHordeTarget = spawned;
            ReadECSData();
            OnGameStateChanged?.Invoke();

            bool exact = spawned == requestedCount;
            message = exact
                ? $"{spawned:N0} zombies are active. Combat feedback remains enabled; wall damage is disabled for this test horde."
                : $"Pool stopped at {spawned:N0}/{requestedCount:N0} zombies.";
            return exact;
        }

        public int ClearDevelopmentHorde()
        {
            if (!TryInitialize()
                || _enemyPoolEntity == Entity.Null
                || !_entityManager.Exists(_enemyPoolEntity))
            {
                return 0;
            }

            _entityManager.CompleteAllTrackedJobs();
            int returned = EnemyPoolRuntimeUtility.ReturnAllActive(
                _entityManager, _enemyPoolEntity);
            WaveStateData wave = _entityManager.GetComponentData<WaveStateData>(_waveStateEntity);
            wave.ZombiesAlive = 0;
            wave.ZombiesSpawned = 0;
            wave.ZombiesToSpawn = 0;
            wave.SpawnTimer = float.MaxValue;
            _entityManager.SetComponentData(_waveStateEntity, wave);
            _developmentTestHordeTarget = 0;
            ReadECSData();
            OnGameStateChanged?.Invoke();
            return returned;
        }

        public bool CompleteDevelopmentTestSession()
        {
            if (_developmentTestHordeTarget > 0)
                return false;

            _developmentTestSessionActive = false;
            _developmentTestHordeTarget = 0;
            if (_developmentFreeEconomyCaptured)
            {
                freeEconomyTestMode = _developmentOriginalFreeEconomyTestMode;
                SetDevelopmentArcherUnlock(ArcherType.Rapid, _developmentOriginalRapidUnlocked);
                SetDevelopmentArcherUnlock(ArcherType.Frost, _developmentOriginalFrostUnlocked);
                _fireballUnlocked = _developmentOriginalFireballUnlocked;
                _fireballCooldownRemaining = _developmentOriginalFireballCooldown;
                _rallyCooldownRemaining = _developmentOriginalRallyCooldown;
                _emergencyRepairCooldownRemaining = _developmentOriginalRepairCooldown;
            }
            _developmentFreeEconomyCaptured = false;
            OnGameStateChanged?.Invoke();
            return true;
        }

        private void SetDevelopmentArcherUnlock(ArcherType type, bool unlocked)
        {
            if (unlocked)
                _unlockedArcherTypes.Add(type);
            else
                _unlockedArcherTypes.Remove(type);
        }
    }
}
#endif
