using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Unity.Mathematics;
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
        public void CombatRebuildPolicy_10KField_IsCompactValidAndDeterministic()
        {
            const int enemyCount = 10_000;
            var samples = new List<CombatRebuildCaptureSample>(enemyCount);
            for (int i = 0; i < enemyCount; i++)
            {
                int column = i % 100;
                int row = i / 100;
                samples.Add(new CombatRebuildCaptureSample
                {
                    Position = new float3(
                        10f + column * 0.12f,
                        -6.5f + row * 0.13f,
                        MobileCastleRenderDepth.UnitZ),
                    Scale = 1.4f,
                    MoveSpeed = 0.85f,
                    MaxHP = 20f,
                    CurrentHP = 20f,
                    AttackDamage = 5f,
                    AttackCooldown = 1f,
                    AttackTimer = 0.25f,
                    XPReward = 10,
                    State = (int)ZombieStateType.Moving,
                    SlowMultiplier = 1f
                });
            }

            uint seed = CombatRebuildUtility.CreateSeed(987654321u, 9, 777, enemyCount);
            CombatRebuildRunSaveState first = CombatRebuildUtility.BuildSnapshot(
                samples, seed, out int[] firstMapping);
            CombatRebuildRunSaveState second = CombatRebuildUtility.BuildSnapshot(
                samples, seed, out int[] secondMapping);

            Assert.That(CombatRebuildUtility.IsValid(first, out string error), Is.True, error);
            Assert.That(first.TotalZombies, Is.EqualTo(enemyCount));
            Assert.That(first.Buckets.Count,
                Is.LessThanOrEqualTo(
                    CombatRebuildUtility.DefaultXCellCount
                    * CombatRebuildUtility.DefaultYCellCount));
            Assert.That(firstMapping, Is.EqualTo(secondMapping));

            int rebuiltCount = 0;
            for (int bucketIndex = 0; bucketIndex < first.Buckets.Count; bucketIndex++)
            {
                Assert.That(second.Buckets[bucketIndex].Count,
                    Is.EqualTo(first.Buckets[bucketIndex].Count));
                for (int itemIndex = 0; itemIndex < first.Buckets[bucketIndex].Count; itemIndex++)
                {
                    float3 firstPosition = CombatRebuildUtility.GetRebuiltPosition(
                        first, bucketIndex, itemIndex);
                    float3 secondPosition = CombatRebuildUtility.GetRebuiltPosition(
                        second, bucketIndex, itemIndex);
                    Assert.That(secondPosition, Is.EqualTo(firstPosition));
                    Assert.That(firstPosition.x, Is.InRange(first.MinX, first.MaxX));
                    Assert.That(firstPosition.y, Is.InRange(first.MinY, first.MaxY));
                    rebuiltCount++;
                }
            }
            Assert.That(rebuiltCount, Is.EqualTo(enemyCount));

            var save = new RunSaveState
            {
                RunId = "run_rebuild_10k",
                HasCombatRebuild = true,
                CombatRebuild = first
            };
            int jsonBytes = Encoding.UTF8.GetByteCount(JsonUtility.ToJson(save, false));
            Assert.That(save.ActiveZombies, Is.Empty,
                "v14 aggregate snapshot entity basina legacy liste yazmamali.");
            Assert.That(jsonBytes, Is.LessThan(512 * 1024),
                "Deterministik 10K rebuild payload'i 512 KiB compact budget'i asmamali.");
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
                ArrowCapacityLevel = 3,
                ArrowEfficiencyLevel = 4,
                GraveEssence = 9_876_543_210L,
                GraveEssenceMetaGainAccumulator = 0.375d,
                HasHeartGraph = true,
                HeartGraph = new GeneratedRunGraph
                {
                    CatalogVersion = 7,
                    Seed = 777u,
                    RootNodeId = HeartGraphConstants.RootNodeId
                },
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
                RallyCooldownRemaining = 31.5f,
                EmergencyRepairCooldownRemaining = 74.25f,
                LastRegularCouncilDay = 6,
                CouncilRunSalt = 987654321u,
                CouncilWoodCapBonus = 11,
                CouncilStoneCapBonus = 12,
                CouncilIronCapBonus = 13,
                CouncilFoodCapBonus = 14,
                HasActiveCouncilEvent = true,
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
                },
                PendingEconomyEvent = 2,
                EconomyEventWave = 8,
                EconomyEventCooldownWaves = 3,
                ProductionBonusResource = (int)EconomyFocusType.Stone,
                ProductionBonusMultiplier = 1.35f,
                ProductionBonusExpiresAfterWave = 10,
                EconomyRandomSeed = 246813579u,
                NextNightSpawnMultiplier = 0.72f,
                NightSpawnExpiresAfterWave = 9
            };
            state.CouncilFlags.Add(new CouncilFlagEntry { Flag = "council_prior_choice_b", Day = 3 });
            state.RecentCouncilTemplates.Add("prior_choice");
            state.RecentCouncilTemplates.Add("council_test");
            state.UsedOneShotCouncils.Add("one_shot_used");
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
            state.HeartGraph.Nodes.Add(new GeneratedHeartNodeState
            {
                NodeId = HeartGraphConstants.RootNodeId,
                Branch = HeartNodeBranch.HeartMagic,
                Depth = 0,
                Visibility = HeartNodeVisibility.Revealed,
                Level = 1,
                LockState = HeartNodeLockState.Available,
                LockedByNodeId = string.Empty
            });
            state.HeartGraph.Nodes.Add(new GeneratedHeartNodeState
            {
                NodeId = "hidden_army",
                Branch = HeartNodeBranch.Army,
                Depth = 1,
                Visibility = HeartNodeVisibility.Hidden,
                Level = 0,
                LockState = HeartNodeLockState.KeystoneConflict,
                LockedByNodeId = "chosen_keystone"
            });
            state.HeartGraph.Edges.Add(new GeneratedHeartEdge
            {
                FromNodeId = HeartGraphConstants.RootNodeId,
                ToNodeId = "hidden_army"
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
            Assert.That(restored.ArrowCapacityLevel, Is.EqualTo(3));
            Assert.That(restored.ArrowEfficiencyLevel, Is.EqualTo(4));
            Assert.That(restored.GraveEssence, Is.EqualTo(9_876_543_210L));
            Assert.That(restored.GraveEssenceMetaGainAccumulator, Is.EqualTo(0.375d));
            Assert.That(restored.HasHeartGraph, Is.True);
            Assert.That(restored.HeartGraph, Is.Not.Null);
            Assert.That(restored.HeartGraph.CatalogVersion, Is.EqualTo(7));
            Assert.That(restored.HeartGraph.Seed, Is.EqualTo(777u));
            Assert.That(restored.HeartGraph.Nodes[1].Visibility, Is.EqualTo(HeartNodeVisibility.Hidden));
            Assert.That(restored.HeartGraph.Nodes[1].LockedByNodeId, Is.EqualTo("chosen_keystone"));
            Assert.That(restored.HeartGraph.Edges[0].ToNodeId, Is.EqualTo("hidden_army"));
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
            Assert.That(restored.LastRegularCouncilDay, Is.EqualTo(6));
            Assert.That(restored.CouncilRunSalt, Is.EqualTo(987654321u));
            Assert.That(restored.CouncilWoodCapBonus, Is.EqualTo(11));
            Assert.That(restored.CouncilStoneCapBonus, Is.EqualTo(12));
            Assert.That(restored.CouncilIronCapBonus, Is.EqualTo(13));
            Assert.That(restored.CouncilFoodCapBonus, Is.EqualTo(14));
            Assert.That(restored.CouncilFlags.Count, Is.EqualTo(1));
            Assert.That(restored.CouncilFlags[0].Flag, Is.EqualTo("council_prior_choice_b"));
            Assert.That(restored.CouncilFlags[0].Day, Is.EqualTo(3));
            Assert.That(restored.RecentCouncilTemplates,
                Is.EqualTo(new[] { "prior_choice", "council_test" }));
            Assert.That(restored.UsedOneShotCouncils, Is.EqualTo(new[] { "one_shot_used" }));
            Assert.That(restored.HasActiveCouncilEvent, Is.True);
            Assert.That(restored.ActiveCouncilEvent.TemplateId, Is.EqualTo("council_test"));
            Assert.That(restored.ActiveCouncilEvent.OptionA.Effects.Count, Is.EqualTo(1));
            Assert.That(restored.ActiveCouncilEvent.OptionA.Effects[0].Amount, Is.EqualTo(50));
            Assert.That(restored.ActiveZombies.Count, Is.EqualTo(1));
            Assert.That(restored.ActiveZombies[0].SlowEnabled, Is.True);
            Assert.That(restored.ActiveArrows[0].TargetZombieIndex, Is.EqualTo(0));
            Assert.That(restored.ActiveArrows[0].RemainingLifetime, Is.EqualTo(2.75f));
            Assert.That(restored.ActiveFireball.Active, Is.True);
            Assert.That(restored.FireballCooldownRemaining, Is.EqualTo(12.5f));
            Assert.That(restored.RallyCooldownRemaining, Is.EqualTo(31.5f));
            Assert.That(restored.EmergencyRepairCooldownRemaining, Is.EqualTo(74.25f));
            Assert.That(restored.PendingEconomyEvent, Is.EqualTo(2));
            Assert.That(restored.EconomyEventWave, Is.EqualTo(8));
            Assert.That(restored.EconomyEventCooldownWaves, Is.EqualTo(3));
            Assert.That(restored.ProductionBonusResource, Is.EqualTo((int)EconomyFocusType.Stone));
            Assert.That(restored.ProductionBonusMultiplier, Is.EqualTo(1.35f));
            Assert.That(restored.ProductionBonusExpiresAfterWave, Is.EqualTo(10));
            Assert.That(restored.EconomyRandomSeed, Is.EqualTo(246813579u));
            Assert.That(restored.NextNightSpawnMultiplier, Is.EqualTo(0.72f));
            Assert.That(restored.NightSpawnExpiresAfterWave, Is.EqualTo(9));
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
                Assert.That(restored.HasActiveCouncilEvent, Is.False);
                Assert.That(restored.ActiveCouncilEvent, Is.Null,
                    "JsonUtility bos nested event'i phantom active Council yapmamali.");
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
        public void TryLoad_Version13ExactCombat_MigratesWithoutInventingAggregatePayload()
        {
            string path = Path.Combine(Application.persistentDataPath, "run_save.json");
            byte[] original = File.Exists(path) ? File.ReadAllBytes(path) : null;
            var legacy = new RunSaveState
            {
                Version = 13,
                RunId = "run_v13_combat_migration_" + Guid.NewGuid().ToString("N")
            };
            legacy.ActiveZombies.Add(new ZombieRunSaveState
            {
                X = 12f,
                Y = -2f,
                MaxHP = 20f,
                CurrentHP = 13f,
                State = (int)ZombieStateType.Attacking
            });

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(legacy));

                RunSaveState restored = RunPersistence.TryLoad();

                Assert.That(restored, Is.Not.Null);
                Assert.That(restored.Version, Is.EqualTo(RunSaveState.CurrentVersion));
                Assert.That(restored.HasCombatRebuild, Is.False);
                Assert.That(restored.CombatRebuild, Is.Null);
                Assert.That(restored.ActiveZombies.Count, Is.EqualTo(1));
                Assert.That(restored.ActiveZombies[0].X, Is.EqualTo(12f));
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
        public void TryLoad_Version14InvalidCombatRebuild_FailsClosed()
        {
            string path = Path.Combine(Application.persistentDataPath, "run_save.json");
            byte[] original = File.Exists(path) ? File.ReadAllBytes(path) : null;
            var corrupt = new RunSaveState
            {
                Version = RunSaveState.CurrentVersion,
                RunId = "run_v14_corrupt_rebuild_" + Guid.NewGuid().ToString("N"),
                HasCombatRebuild = true,
                CombatRebuild = new CombatRebuildRunSaveState
                {
                    PolicyVersion = CombatRebuildUtility.CurrentPolicyVersion,
                    Seed = 123u,
                    TotalZombies = 1,
                    XCellCount = CombatRebuildUtility.DefaultXCellCount,
                    YCellCount = CombatRebuildUtility.DefaultYCellCount,
                    HealthBandCount = CombatRebuildUtility.DefaultHealthBandCount,
                    MinX = 0f,
                    MaxX = 1f,
                    MinY = 0f,
                    MaxY = 1f
                }
            };

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(corrupt));

                Assert.That(RunPersistence.TryLoad(), Is.Null,
                    "Discriminator true iken eksik bucket payload'i sifir horde gibi acilmamali.");
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
        public void TryLoad_Version3Snapshot_MigratesWorkerAllocationBedBuildingFormationAndAmmoStateToCurrent()
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
                Assert.That(restored.Version, Is.EqualTo(RunSaveState.CurrentVersion));
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
                Assert.That(restored.ArrowCapacityLevel, Is.Zero);
                Assert.That(restored.ArrowEfficiencyLevel, Is.Zero);
                Assert.That(restored.GraveEssence, Is.Zero);
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
                Assert.That(restored.Version, Is.EqualTo(RunSaveState.CurrentVersion));
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
                Assert.That(restored.Version, Is.EqualTo(RunSaveState.CurrentVersion));
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
                Assert.That(restored.Version, Is.EqualTo(RunSaveState.CurrentVersion));
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
        public void TryLoad_Version8Snapshot_MigratesToZeroGraveEssence()
        {
            string path = Path.Combine(Application.persistentDataPath, "run_save.json");
            byte[] original = File.Exists(path) ? File.ReadAllBytes(path) : null;
            var legacy = new RunSaveState
            {
                Version = 8,
                RunId = "run_v8_heart_migration_" + Guid.NewGuid().ToString("N"),
                GraveEssence = 999
            };

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(legacy));

                RunSaveState restored = RunPersistence.TryLoad();

                Assert.That(restored, Is.Not.Null);
                Assert.That(restored.Version, Is.EqualTo(RunSaveState.CurrentVersion));
                Assert.That(restored.GraveEssence, Is.Zero);
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
        public void TryLoad_Version9Snapshot_DoesNotInventMissingHeartGraph()
        {
            string path = Path.Combine(Application.persistentDataPath, "run_save.json");
            byte[] original = File.Exists(path) ? File.ReadAllBytes(path) : null;
            var legacy = new RunSaveState
            {
                Version = 9,
                RunId = "run_v9_graph_migration_" + Guid.NewGuid().ToString("N"),
                GraveEssence = 125
            };

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(legacy));

                RunSaveState restored = RunPersistence.TryLoad();

                Assert.That(restored, Is.Not.Null);
                Assert.That(restored.Version, Is.EqualTo(RunSaveState.CurrentVersion));
                Assert.That(restored.GraveEssence, Is.EqualTo(125));
                Assert.That(restored.HasHeartGraph, Is.False);
                Assert.That(restored.HeartGraph, Is.Null,
                    "v9 eksik graph'i aktif catalog'dan sessizce uretilemez.");
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
        public void TryLoad_Version12Snapshot_InitializesMetaEssenceRemainderToZero()
        {
            string path = Path.Combine(Application.persistentDataPath, "run_save.json");
            byte[] original = File.Exists(path) ? File.ReadAllBytes(path) : null;
            var legacy = new RunSaveState
            {
                Version = 12,
                RunId = "run_v12_essence_meta_migration_" + Guid.NewGuid().ToString("N"),
                GraveEssence = 125,
                GraveEssenceMetaGainAccumulator = 0.75d
            };

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(legacy));

                RunSaveState restored = RunPersistence.TryLoad();

                Assert.That(restored, Is.Not.Null);
                Assert.That(restored.Version, Is.EqualTo(RunSaveState.CurrentVersion));
                Assert.That(restored.GraveEssence, Is.EqualTo(125));
                Assert.That(restored.GraveEssenceMetaGainAccumulator, Is.Zero);
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
        public void TryLoad_Version10ChanceFailure_DoesNotConsumeScheduledRegularCouncil()
        {
            string path = Path.Combine(Application.persistentDataPath, "run_save.json");
            byte[] original = File.Exists(path) ? File.ReadAllBytes(path) : null;
            var legacy = new RunSaveState
            {
                Version = 10,
                RunId = "run_v10_council_fail_" + Guid.NewGuid().ToString("N"),
                CycleIndex = 5,
                LastCouncilRollDay = 6,
                CouncilDaysSinceEvent = 4,
                LastRegularCouncilDay = 999
            };

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(legacy));

                RunSaveState restored = RunPersistence.TryLoad();

                Assert.That(restored, Is.Not.Null);
                Assert.That(restored.Version, Is.EqualTo(RunSaveState.CurrentVersion));
                Assert.That(restored.LastRegularCouncilDay, Is.EqualTo(-1));
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
        public void TryLoad_Version10ProducedEvent_PreservesHandledScheduledDay()
        {
            string path = Path.Combine(Application.persistentDataPath, "run_save.json");
            byte[] original = File.Exists(path) ? File.ReadAllBytes(path) : null;
            var legacy = new RunSaveState
            {
                Version = 10,
                RunId = "run_v10_council_handled_" + Guid.NewGuid().ToString("N"),
                CycleIndex = 5,
                LastCouncilRollDay = 6,
                CouncilDaysSinceEvent = 4,
                LastRegularCouncilDay = -1,
                ActiveCouncilEvent = new ComposedCouncilEvent
                {
                    TemplateId = "legacy_active",
                    OptionA = new ComposedCouncilOption { Label = "A" },
                    OptionB = new ComposedCouncilOption { Label = "B" }
                }
            };

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(legacy));

                RunSaveState restored = RunPersistence.TryLoad();

                Assert.That(restored, Is.Not.Null);
                Assert.That(restored.Version, Is.EqualTo(RunSaveState.CurrentVersion));
                Assert.That(restored.LastRegularCouncilDay, Is.EqualTo(6));
                Assert.That(restored.HasActiveCouncilEvent, Is.True);
                Assert.That(restored.ActiveCouncilEvent.TemplateId, Is.EqualTo("legacy_active"));
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
        public void CommitDeath_DeletesRunSnapshotContainingGraveEssence()
        {
            string runPath = Path.Combine(Application.persistentDataPath, "run_save.json");
            string receiptPath = Path.Combine(Application.persistentDataPath, "run_death_receipt.json");
            byte[] originalRun = File.Exists(runPath) ? File.ReadAllBytes(runPath) : null;
            byte[] originalReceipt = File.Exists(receiptPath) ? File.ReadAllBytes(receiptPath) : null;
            string runId = "run_dead_heart_" + Guid.NewGuid().ToString("N");

            try
            {
                Assert.That(RunPersistence.Save(new RunSaveState
                {
                    RunId = runId,
                    GraveEssence = 42_000
                }), Is.True);

                Assert.That(
                    RunPersistence.CommitDeath(new RunDeathReceipt { RunId = runId }),
                    Is.True);

                Assert.That(File.Exists(runPath), Is.False);
                Assert.That(File.Exists(receiptPath), Is.True);
                Assert.That(RunPersistence.TryLoad(), Is.Null);
            }
            finally
            {
                if (originalRun != null)
                    File.WriteAllBytes(runPath, originalRun);
                else if (File.Exists(runPath))
                    File.Delete(runPath);

                if (originalReceipt != null)
                    File.WriteAllBytes(receiptPath, originalReceipt);
                else if (File.Exists(receiptPath))
                    File.Delete(receiptPath);
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
        public void CorruptDeathReceiptMarker_FailsClosedAndInvalidatesSnapshot()
        {
            var state = new RunSaveState { RunId = "run_corrupt_receipt" };

            Assert.That(RunPersistence.IsLoadableState(state, null), Is.True);
            Assert.That(
                RunPersistence.IsLoadableState(state, null, hasPendingDeathMarker: true),
                Is.False);
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

        [Test]
        public void PendingDeathReward_RecoversOnceAndSurvivesReload()
        {
            string runPath = Path.Combine(Application.persistentDataPath, "run_save.json");
            string receiptPath = Path.Combine(Application.persistentDataPath, "run_death_receipt.json");
            string metaPath = Path.Combine(Application.persistentDataPath, "meta_progress.json");
            byte[] originalRun = File.Exists(runPath) ? File.ReadAllBytes(runPath) : null;
            byte[] originalReceipt = File.Exists(receiptPath) ? File.ReadAllBytes(receiptPath) : null;
            byte[] originalMeta = File.Exists(metaPath) ? File.ReadAllBytes(metaPath) : null;
            string runId = "run_recovery_" + Guid.NewGuid().ToString("N");

            try
            {
                DeleteFileAndTemp(runPath);
                DeleteFileAndTemp(receiptPath);
                DeleteFileAndTemp(metaPath);
                MetaProgression.Load();

                Assert.That(RunPersistence.Save(new RunSaveState { RunId = runId }), Is.True);
                Assert.That(RunPersistence.CommitDeath(new RunDeathReceipt
                {
                    RunId = runId,
                    Day = 4,
                    Kills = 100
                }), Is.True);
                Assert.That(File.Exists(runPath), Is.False);
                Assert.That(File.Exists(receiptPath), Is.True);

                Assert.That(RunPersistence.RecoverPendingDeathReward(), Is.True);
                Assert.That(File.Exists(receiptPath), Is.False);
                Assert.That(MetaProgression.HasRewardedRun(runId), Is.True);
                Assert.That(MetaProgression.State.Souls, Is.EqualTo(300));
                Assert.That(MetaProgression.State.TotalRuns, Is.EqualTo(1));

                MetaProgression.Load(); // process restart simulation
                int soulsAfterReload = MetaProgression.State.Souls;
                int runsAfterReload = MetaProgression.State.TotalRuns;
                Assert.That(MetaProgression.HasRewardedRun(runId), Is.True);
                Assert.That(RunPersistence.RecoverPendingDeathReward(), Is.False);
                Assert.That(MetaProgression.State.Souls, Is.EqualTo(soulsAfterReload));
                Assert.That(MetaProgression.State.TotalRuns, Is.EqualTo(runsAfterReload));
            }
            finally
            {
                RestoreFile(runPath, originalRun);
                RestoreFile(receiptPath, originalReceipt);
                RestoreFile(metaPath, originalMeta);
                MetaProgression.Load();
            }
        }

        [Test]
        public void PendingDeathReceipt_RecoversOrphanedDurableTemp()
        {
            string receiptPath = Path.Combine(Application.persistentDataPath, "run_death_receipt.json");
            string tempPath = receiptPath + ".tmp";
            byte[] originalReceipt = File.Exists(receiptPath) ? File.ReadAllBytes(receiptPath) : null;
            byte[] originalTemp = File.Exists(tempPath) ? File.ReadAllBytes(tempPath) : null;
            var expected = new RunDeathReceipt
            {
                RunId = "run_temp_" + Guid.NewGuid().ToString("N"),
                Day = 7,
                Kills = 321
            };

            try
            {
                DeleteFileAndTemp(receiptPath);
                File.WriteAllText(tempPath, JsonUtility.ToJson(expected, false));

                RunDeathReceipt recovered = RunPersistence.TryLoadPendingDeath();

                Assert.That(recovered, Is.Not.Null);
                Assert.That(recovered.RunId, Is.EqualTo(expected.RunId));
                Assert.That(recovered.Day, Is.EqualTo(7));
                Assert.That(recovered.Kills, Is.EqualTo(321));
                Assert.That(File.Exists(receiptPath), Is.True);
                Assert.That(File.Exists(tempPath), Is.False);
            }
            finally
            {
                DeleteFileAndTemp(receiptPath);
                if (originalReceipt != null)
                    File.WriteAllBytes(receiptPath, originalReceipt);
                if (originalTemp != null)
                    File.WriteAllBytes(tempPath, originalTemp);
            }
        }

        private static void DeleteFileAndTemp(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
            if (File.Exists(path + ".tmp"))
                File.Delete(path + ".tmp");
        }

        private static void RestoreFile(string path, byte[] contents)
        {
            DeleteFileAndTemp(path);
            if (contents != null)
                File.WriteAllBytes(path, contents);
        }
    }
}
