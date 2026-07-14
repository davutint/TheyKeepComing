using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class RunPersistenceTests
    {
        [Test]
        public void SchemaVersion_RejectsLegacyCheckpoint_AndAcceptsExactSnapshot()
        {
            Assert.That(RunPersistence.IsSupportedVersion(2), Is.False);
            Assert.That(RunPersistence.IsSupportedVersion(RunSaveState.CurrentVersion), Is.True);
            Assert.That(RunPersistence.IsSupportedVersion(RunSaveState.CurrentVersion + 1), Is.False);
        }

        [Test]
        public void JsonRoundTrip_PreservesExactCycleCombatCouncilAndAbilityState()
        {
            var state = new RunSaveState
            {
                RunId = "run_exact_01",
                CycleIndex = 7,
                CyclePhase = (int)SiegeCyclePhase.Night,
                CycleTimer = 41.25f,
                CycleProgress01 = 0.6875f,
                PhaseProgress01 = 0.3125f,
                SpawnRandomState = 123456u,
                SpawnBacklog = 77,
                TotalDemandedEnemies = 1234,
                TotalBudgetSpawnedEnemies = 1157,
                DemandPerInterval = 9,
                DayQuantityMultiplier = 1.4f,
                DayBaseSpawnInterval = 0.42f,
                PhaseIntensityMultiplier = 1.65f,
                EffectiveSpawnInterval = 0.2545f,
                Wood = 321,
                Stone = 222,
                Iron = 111,
                Food = 77,
                ArrowCurrent = 456,
                ArrowAccumulator = 0.75f,
                WoodBuildingCapacityLevel = 3,
                WoodBuildingEfficiencyLevel = 4,
                StoneBuildingCapacityLevel = 5,
                StoneBuildingEfficiencyLevel = 6,
                IronBuildingCapacityLevel = 7,
                IronBuildingEfficiencyLevel = 8,
                FoodBuildingCapacityLevel = 9,
                FoodBuildingEfficiencyLevel = 10,
                WallCurrentHP = 187.5f,
                ArcherFormationVersion = ArcherFormationUtility.CurrentVersion,
                FireballCooldownRemaining = 12.5f,
                RallyTimer = 4.25f,
                ActiveCouncilEvent = new ComposedCouncilEvent
                {
                    TemplateId = "council_test",
                    Title = "A Hard Choice",
                    Body = "Choose.",
                    OptionA = new ComposedCouncilOption { Label = "A", BudgetMinutes = 1.5f },
                    OptionB = new ComposedCouncilOption { Label = "B", BudgetMinutes = 1.5f }
                },
                ActiveFireball = new FireballRunSaveState
                {
                    Active = true,
                    X = 4f,
                    Y = 5f,
                    TargetX = 8f,
                    TargetY = 2f,
                    Damage = 90f,
                    Radius = 3.5f
                }
            };
            state.ActiveCouncilEvent.OptionA.Effects.Add(new ComposedCouncilEffect
            {
                Kind = CouncilEffectKind.GainResource,
                Resource = EconomyFocusType.Wood,
                Amount = 50
            });
            state.ActiveZombies.Add(new ZombieRunSaveState
            {
                X = 12f,
                Y = -2f,
                CurrentHP = 13f,
                MaxHP = 20f,
                State = (int)ZombieStateType.Attacking,
                SlowEnabled = true,
                SlowDuration = 0.8f,
                SlowMultiplier = 0.6f
            });
            state.ActiveArrows.Add(new ArrowRunSaveState
            {
                X = 3f,
                Y = 1f,
                TargetZombieIndex = 0,
                Damage = 9f,
                ArcherType = (int)ArcherType.Frost,
                RemainingLifetime = 2.75f
            });

            string json = JsonUtility.ToJson(state);
            RunSaveState restored = JsonUtility.FromJson<RunSaveState>(json);

            Assert.That(restored.Version, Is.EqualTo(RunSaveState.CurrentVersion));
            Assert.That(restored.RunId, Is.EqualTo("run_exact_01"));
            Assert.That(restored.CycleIndex, Is.EqualTo(7));
            Assert.That(restored.CyclePhase, Is.EqualTo((int)SiegeCyclePhase.Night));
            Assert.That(restored.CycleTimer, Is.EqualTo(41.25f));
            Assert.That(restored.SpawnRandomState, Is.EqualTo(123456u));
            Assert.That(restored.SpawnBacklog, Is.EqualTo(77));
            Assert.That(restored.TotalDemandedEnemies, Is.EqualTo(1234));
            Assert.That(restored.TotalBudgetSpawnedEnemies, Is.EqualTo(1157));
            Assert.That(restored.DayBaseSpawnInterval, Is.EqualTo(0.42f));
            Assert.That(restored.ArrowCurrent, Is.EqualTo(456));
            Assert.That(restored.WoodBuildingCapacityLevel, Is.EqualTo(3));
            Assert.That(restored.WoodBuildingEfficiencyLevel, Is.EqualTo(4));
            Assert.That(restored.StoneBuildingCapacityLevel, Is.EqualTo(5));
            Assert.That(restored.StoneBuildingEfficiencyLevel, Is.EqualTo(6));
            Assert.That(restored.IronBuildingCapacityLevel, Is.EqualTo(7));
            Assert.That(restored.IronBuildingEfficiencyLevel, Is.EqualTo(8));
            Assert.That(restored.FoodBuildingCapacityLevel, Is.EqualTo(9));
            Assert.That(restored.FoodBuildingEfficiencyLevel, Is.EqualTo(10));
            Assert.That(restored.ArcherFormationVersion,
                Is.EqualTo(ArcherFormationUtility.CurrentVersion));
            Assert.That(restored.ActiveCouncilEvent.TemplateId, Is.EqualTo("council_test"));
            Assert.That(restored.ActiveCouncilEvent.OptionA.Effects.Count, Is.EqualTo(1));
            Assert.That(restored.ActiveCouncilEvent.OptionA.Effects[0].Amount, Is.EqualTo(50));
            Assert.That(restored.ActiveZombies.Count, Is.EqualTo(1));
            Assert.That(restored.ActiveZombies[0].SlowEnabled, Is.True);
            Assert.That(restored.ActiveArrows[0].TargetZombieIndex, Is.EqualTo(0));
            Assert.That(restored.ActiveArrows[0].RemainingLifetime, Is.EqualTo(2.75f));
            Assert.That(restored.ActiveFireball.Active, Is.True);
            Assert.That(restored.FireballCooldownRemaining, Is.EqualTo(12.5f));
        }

        [Test]
        public void DeathReceipt_RoundTrip_PreservesRunIdentityAndRewardInputs()
        {
            var receipt = new RunDeathReceipt
            {
                RunId = "run_dead_01",
                Day = 12,
                Kills = 9876
            };

            string json = JsonUtility.ToJson(receipt);
            RunDeathReceipt restored = JsonUtility.FromJson<RunDeathReceipt>(json);

            Assert.That(restored.RunId, Is.EqualTo("run_dead_01"));
            Assert.That(restored.Day, Is.EqualTo(12));
            Assert.That(restored.Kills, Is.EqualTo(9876));
        }

        [Test]
        public void Save_WritesCompactJson_AndRemainsLoadable()
        {
            string path = Path.Combine(Application.persistentDataPath, "run_save.json");
            byte[] original = File.Exists(path) ? File.ReadAllBytes(path) : null;
            string runId = "run_compact_" + Guid.NewGuid().ToString("N");

            try
            {
                Assert.That(RunPersistence.Save(new RunSaveState { RunId = runId }), Is.True);

                string json = File.ReadAllText(path);
                Assert.That(json, Does.Not.Contain("\r"));
                Assert.That(json, Does.Not.Contain("\n"));

                RunSaveState restored = RunPersistence.TryLoad();
                Assert.That(restored, Is.Not.Null);
                Assert.That(restored.RunId, Is.EqualTo(runId));
            }
            finally
            {
                if (original != null)
                    File.WriteAllBytes(path, original);
                else if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test]
        public void TryLoad_Version3Snapshot_MigratesWorkerAllocationBedBuildingAndFormationStateToVersion7()
        {
            string path = Path.Combine(Application.persistentDataPath, "run_save.json");
            byte[] original = File.Exists(path) ? File.ReadAllBytes(path) : null;
            var legacy = new RunSaveState
            {
                Version = 3,
                RunId = "run_v3_worker_migration_" + Guid.NewGuid().ToString("N"),
                PopulationTotal = 60,
                WoodWorkers = 20,
                StoneWorkers = 10,
                IronWorkers = 8,
                FoodWorkers = 15,
                BasicArchers = 4
            };

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(legacy));

                RunSaveState restored = RunPersistence.TryLoad();

                Assert.That(restored, Is.Not.Null);
                Assert.That(restored.Version, Is.EqualTo(7));
                Assert.That(restored.WoodWorkerTargetRatioBps, Is.EqualTo(3774));
                Assert.That(restored.StoneWorkerTargetRatioBps, Is.EqualTo(1887));
                Assert.That(restored.IronWorkerTargetRatioBps, Is.EqualTo(1509));
                Assert.That(restored.FoodWorkerTargetRatioBps, Is.EqualTo(2830));
                Assert.That(restored.WorkerIdlePopulation, Is.EqualTo(3));
                Assert.That(restored.LastObservedPopulation, Is.EqualTo(60));
                Assert.That(restored.BedBaseCapacity, Is.EqualTo(60));
                Assert.That(restored.PurchasedBedCapacity, Is.Zero);
                Assert.That(restored.WoodBuildingCapacityLevel, Is.Zero);
                Assert.That(restored.FoodBuildingEfficiencyLevel, Is.Zero);
                Assert.That(restored.ArcherFormationVersion,
                    Is.EqualTo(ArcherFormationUtility.CurrentVersion));
            }
            finally
            {
                if (original != null)
                    File.WriteAllBytes(path, original);
                else if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test]
        public void TryLoad_Version4UnlimitedCapacity_MigratesToPopulationSafeBedBase()
        {
            string path = Path.Combine(Application.persistentDataPath, "run_save.json");
            byte[] original = File.Exists(path) ? File.ReadAllBytes(path) : null;
            var legacy = new RunSaveState
            {
                Version = 4,
                RunId = "run_v4_bed_migration_" + Guid.NewGuid().ToString("N"),
                PopulationTotal = 135,
                PopulationCapacity = 999999,
                PopulationBaseCapacity = 999999
            };

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(legacy));

                RunSaveState restored = RunPersistence.TryLoad();

                Assert.That(restored, Is.Not.Null);
                Assert.That(restored.Version, Is.EqualTo(7));
                Assert.That(restored.BedBaseCapacity, Is.EqualTo(135));
                Assert.That(restored.PurchasedBedCapacity, Is.Zero);
                Assert.That(restored.IronBuildingEfficiencyLevel, Is.Zero);
            }
            finally
            {
                if (original != null)
                    File.WriteAllBytes(path, original);
                else if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test]
        public void TryLoad_Version5Snapshot_MigratesToCleanWorkerBuildingLevels()
        {
            string path = Path.Combine(Application.persistentDataPath, "run_save.json");
            byte[] original = File.Exists(path) ? File.ReadAllBytes(path) : null;
            var legacy = new RunSaveState
            {
                Version = 5,
                RunId = "run_v5_building_migration_" + Guid.NewGuid().ToString("N")
            };

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(legacy));

                RunSaveState restored = RunPersistence.TryLoad();

                Assert.That(restored, Is.Not.Null);
                Assert.That(restored.Version, Is.EqualTo(7));
                Assert.That(restored.WoodBuildingCapacityLevel, Is.Zero);
                Assert.That(restored.WoodBuildingEfficiencyLevel, Is.Zero);
                Assert.That(restored.StoneBuildingCapacityLevel, Is.Zero);
                Assert.That(restored.StoneBuildingEfficiencyLevel, Is.Zero);
                Assert.That(restored.IronBuildingCapacityLevel, Is.Zero);
                Assert.That(restored.IronBuildingEfficiencyLevel, Is.Zero);
                Assert.That(restored.FoodBuildingCapacityLevel, Is.Zero);
                Assert.That(restored.FoodBuildingEfficiencyLevel, Is.Zero);
                Assert.That(restored.ArcherFormationVersion,
                    Is.EqualTo(ArcherFormationUtility.CurrentVersion));
            }
            finally
            {
                if (original != null)
                    File.WriteAllBytes(path, original);
                else if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test]
        public void TryLoad_Version6Snapshot_MigratesToFormationVersion1()
        {
            string path = Path.Combine(Application.persistentDataPath, "run_save.json");
            byte[] original = File.Exists(path) ? File.ReadAllBytes(path) : null;
            var legacy = new RunSaveState
            {
                Version = 6,
                ArcherFormationVersion = 0,
                RunId = "run_v6_formation_migration_" + Guid.NewGuid().ToString("N"),
                BasicArchers = 40,
                RapidArchers = 20,
                FrostArchers = 5
            };

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(legacy));

                RunSaveState restored = RunPersistence.TryLoad();

                Assert.That(restored, Is.Not.Null);
                Assert.That(restored.Version, Is.EqualTo(7));
                Assert.That(restored.ArcherFormationVersion,
                    Is.EqualTo(ArcherFormationUtility.CurrentVersion));
                Assert.That(restored.BasicArchers, Is.EqualTo(40));
                Assert.That(restored.RapidArchers, Is.EqualTo(20));
                Assert.That(restored.FrostArchers, Is.EqualTo(5));
            }
            finally
            {
                if (original != null)
                    File.WriteAllBytes(path, original);
                else if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test]
        public void PendingDeathReceipt_InvalidatesMatchingRunSnapshot()
        {
            var state = new RunSaveState { RunId = "run_dead_02" };
            var matchingReceipt = new RunDeathReceipt { RunId = "run_dead_02" };
            var differentReceipt = new RunDeathReceipt { RunId = "run_other" };

            Assert.That(RunPersistence.IsLoadableState(state, null), Is.True);
            Assert.That(RunPersistence.IsLoadableState(state, differentReceipt), Is.True);
            Assert.That(RunPersistence.IsLoadableState(state, matchingReceipt), Is.False);
        }

        [Test]
        public void MetaRunResult_SameRunId_IsRewardedOnlyOnce()
        {
            var state = new MetaProgressState();

            MetaRunResult first = MetaProgression.ApplyRunResult(state, "run_reward_01", 4, 100);
            int soulsAfterFirst = state.Souls;
            int runsAfterFirst = state.TotalRuns;
            MetaRunResult duplicate = MetaProgression.ApplyRunResult(state, "run_reward_01", 4, 100);

            Assert.That(first.AlreadyRewarded, Is.False);
            Assert.That(first.SoulsEarned, Is.EqualTo(300));
            Assert.That(duplicate.AlreadyRewarded, Is.True);
            Assert.That(duplicate.SoulsEarned, Is.Zero);
            Assert.That(state.Souls, Is.EqualTo(soulsAfterFirst));
            Assert.That(state.TotalRuns, Is.EqualTo(runsAfterFirst));
            Assert.That(state.RewardedRunIds, Is.EqualTo(new[] { "run_reward_01" }));
        }
    }
}
