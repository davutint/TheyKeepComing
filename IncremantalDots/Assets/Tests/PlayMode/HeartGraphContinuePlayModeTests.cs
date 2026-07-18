using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadWalls.Tests
{
    public class HeartGraphContinuePlayModeTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();
        private string _runSavePath;
        private byte[] _originalRunSave;
        private FieldInfo _catalogField;
        private HeartNodeCatalogSO _originalCatalog;
        private FieldInfo _graphSettingsField;
        private HeartGraphRuntimeSettings _originalGraphSettings;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _runSavePath = Path.Combine(Application.persistentDataPath, "run_save.json");
            _originalRunSave = File.Exists(_runSavePath) ? File.ReadAllBytes(_runSavePath) : null;
            RunPersistence.Delete();
            GameBootstrap.PendingAction = GameBootstrap.StartAction.None;
            SceneManager.LoadScene("NewGameScene", LoadSceneMode.Single);
            for (int frame = 0; frame < 120 && GameManager.Instance == null; frame++)
                yield return null;
            Assert.That(GameManager.Instance, Is.Not.Null);
            _catalogField = typeof(GameManager).GetField(
                "heartCatalog",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(_catalogField, Is.Not.Null);
            _originalCatalog = _catalogField.GetValue(GameManager.Instance) as HeartNodeCatalogSO;
            _graphSettingsField = typeof(GameManager).GetField(
                "heartGraphSettings",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(_graphSettingsField, Is.Not.Null);
            _originalGraphSettings =
                (_graphSettingsField.GetValue(GameManager.Instance) as HeartGraphRuntimeSettings)?.Clone();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (GameManager.Instance != null && _catalogField != null)
            {
                _catalogField.SetValue(GameManager.Instance, _originalCatalog);
                if (_graphSettingsField != null && _originalGraphSettings != null)
                    _graphSettingsField.SetValue(GameManager.Instance, _originalGraphSettings.Clone());
                MethodInfo resetMethod = typeof(GameManager).GetMethod(
                    "ResetHeartRuntime",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                resetMethod?.Invoke(GameManager.Instance, null);
            }
            RunPersistence.Delete();
            if (_originalRunSave != null)
                File.WriteAllBytes(_runSavePath, _originalRunSave);
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_createdObjects[i]);
            _createdObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Continue_ReplaysExactSavedHeartGraphWithoutReroll()
        {
            GameManager gameManager = GameManager.Instance;
            bool snapshotReady = false;
            for (int frame = 0; frame < 300; frame++)
            {
                if (gameManager.SaveRunSnapshot())
                {
                    snapshotReady = true;
                    break;
                }
                yield return null;
            }
            Assert.That(snapshotReady, Is.True, "GameManager snapshot icin hazir olmadi.");

            RunSaveState save = RunPersistence.TryLoad();
            Assert.That(save, Is.Not.Null);
            HeartNodeCatalogSO catalog = CreateCatalog();
            GeneratedRunGraph graph = CreateSavedGraph(catalog.CatalogVersion);
            save.HasHeartGraph = true;
            save.HeartGraph = HeartGraphPersistenceUtility.CloneExact(graph);
            save.GraveEssence = 765;
            Assert.That(RunPersistence.Save(save), Is.True);
            string exactGraphJson = JsonUtility.ToJson(save.HeartGraph);

            _catalogField.SetValue(gameManager, catalog);

            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);
            Assert.That(gameManager.IsHeartRuntimeReady, Is.True);
            Assert.That(gameManager.IsArcherTypeUnlocked(ArcherType.Rapid), Is.True,
                "Saved Rapid unlock behavior effect replay edilmedi.");
            Assert.That(gameManager.FireballUnlocked, Is.True,
                "Saved Fireball unlock behavior effect replay edilmedi.");
            Assert.That(gameManager.TryBuildHeartPresentation(
                out HeartGraphPresentation presentation,
                out IReadOnlyList<string> errors), Is.True, string.Join(" | ", errors));
            Assert.That(
                presentation.Nodes.Single(node => node.ExactNodeId == "rapid_unlock").Level,
                Is.EqualTo(1));
            Assert.That(
                presentation.Nodes.Single(node => node.ExactNodeId == "wall_access").Level,
                Is.EqualTo(1));
            Assert.That(
                presentation.Nodes.Single(node => node.ExactNodeId == "fireball_unlock").Level,
                Is.EqualTo(1));

            HeartRuntimeTuningTelemetry telemetryBefore =
                gameManager.GetHeartRuntimeTuningTelemetry();
            HeartGraphRuntimeSettings liveSettings =
                (HeartGraphRuntimeSettings)_graphSettingsField.GetValue(gameManager);
            liveSettings.MinimumBranchDepth = 1;
            liveSettings.MaximumBranchDepth = 1;
            liveSettings.StandardRarityWeight = 1;
            liveSettings.RareRarityWeight = 99;
            HeartRuntimeTuningTelemetry telemetryAfter =
                gameManager.GetHeartRuntimeTuningTelemetry();

            Assert.That(telemetryAfter.Seed, Is.EqualTo(telemetryBefore.Seed));
            Assert.That(telemetryAfter.NodeCount, Is.EqualTo(telemetryBefore.NodeCount));
            Assert.That(telemetryAfter.EdgeCount, Is.EqualTo(telemetryBefore.EdgeCount));
            Assert.That(telemetryAfter.PurchasedNodeCount,
                Is.EqualTo(telemetryBefore.PurchasedNodeCount));

            Assert.That(gameManager.SaveRunSnapshot(), Is.True);
            RunSaveState replayedSave = RunPersistence.TryLoad();
            Assert.That(replayedSave, Is.Not.Null);
            Assert.That(replayedSave.HasHeartGraph, Is.True);
            Assert.That(replayedSave.GraveEssence, Is.EqualTo(765));
            Assert.That(JsonUtility.ToJson(replayedSave.HeartGraph), Is.EqualTo(exactGraphJson),
                "Continue sonrasi veya future-run tuning degisince aktif graph yeniden zar atilmamali.");
        }

        [UnityTest]
        public IEnumerator HeartArcherEffects_RebaseExistingAndFutureUnits_AndContinueDoesNotCompound()
        {
            GameManager gameManager = GameManager.Instance;
            bool snapshotReady = false;
            for (int frame = 0; frame < 300; frame++)
            {
                if (gameManager.SaveRunSnapshot())
                {
                    snapshotReady = true;
                    break;
                }
                yield return null;
            }
            Assert.That(snapshotReady, Is.True, "GameManager snapshot icin hazir olmadi.");

            RunSaveState save = RunPersistence.TryLoad();
            Assert.That(save, Is.Not.Null);
            HeartNodeCatalogSO catalog = CreateCatalog();
            GeneratedRunGraph graph = CreateSavedGraph(catalog.CatalogVersion);
            GeneratedHeartNodeState frostUnlock = graph.Nodes.Single(node =>
                node.NodeId == "frost_unlock");
            frostUnlock.Visibility = HeartNodeVisibility.Revealed;
            frostUnlock.Level = 1;
            GeneratedHeartNodeState armySink = graph.Nodes.Single(node =>
                node.NodeId == "army_sink");
            armySink.Visibility = HeartNodeVisibility.Revealed;
            armySink.Level = 0;

            save.HasHeartGraph = true;
            save.HeartGraph = HeartGraphPersistenceUtility.CloneExact(graph);
            save.GraveEssence = 1_000;
            save.BasicArchers = Math.Max(1, save.BasicArchers);
            save.FrostArchers = Math.Max(1, save.FrostArchers);
            Assert.That(RunPersistence.Save(save), Is.True);

            _catalogField.SetValue(gameManager, catalog);
            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);
            Assert.That(gameManager.IsHeartRuntimeReady, Is.True);

            EntityManager entityManager =
                World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity basicEntity = FindArcher(entityManager, ArcherType.Basic);
            Entity frostEntity = FindArcher(entityManager, ArcherType.Frost);
            Assert.That(basicEntity, Is.Not.EqualTo(Entity.Null));
            Assert.That(frostEntity, Is.Not.EqualTo(Entity.Null));
            ArcherUnit basicBefore = entityManager.GetComponentData<ArcherUnit>(basicEntity);
            ArcherUnit frostBefore = entityManager.GetComponentData<ArcherUnit>(frostEntity);

            HeartPurchaseResult purchase = gameManager.TryPurchaseHeartNode(
                "army_sink", HeartPurchaseQuantity.One);
            Assert.That(purchase, Is.Not.Null);
            Assert.That(purchase.Succeeded, Is.True, purchase.Message);
            yield return null;

            ArcherUnit basicAfter = entityManager.GetComponentData<ArcherUnit>(basicEntity);
            ArcherUnit frostAfter = entityManager.GetComponentData<ArcherUnit>(frostEntity);
            Assert.That(basicAfter.ArrowDamage, Is.GreaterThan(basicBefore.ArrowDamage));
            Assert.That(basicAfter.FireRate, Is.GreaterThan(basicBefore.FireRate));
            Assert.That(basicAfter.Range, Is.GreaterThan(basicBefore.Range));
            Assert.That(frostAfter.SlowMultiplier, Is.LessThan(frostBefore.SlowMultiplier));
            Assert.That(frostAfter.SlowDuration, Is.EqualTo(frostBefore.SlowDuration).Within(0.0001f),
                "Heart Frost progression slow multiplier'i sahiplenir; duration uydurmaz.");

            HashSet<Entity> basicEntitiesBeforeBuy = GetArchers(entityManager, ArcherType.Basic);
            FieldInfo freeEconomyField = typeof(GameManager).GetField(
                "freeEconomyTestMode", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(freeEconomyField, Is.Not.Null);
            freeEconomyField.SetValue(gameManager, true);
            try
            {
                Assert.That(gameManager.BuyArcher(ArcherType.Basic), Is.True);
            }
            finally
            {
                freeEconomyField.SetValue(gameManager, false);
            }

            Entity newBasicEntity = GetArchers(entityManager, ArcherType.Basic)
                .Single(entity => !basicEntitiesBeforeBuy.Contains(entity));
            ArcherUnit newBasic = entityManager.GetComponentData<ArcherUnit>(newBasicEntity);
            AssertArcherStatsEqual(basicAfter, newBasic,
                "Heart satin alimindan sonra uretilen okcu effective stat'leri miras almali.");

            Assert.That(gameManager.SaveRunSnapshot(), Is.True);
            RunSaveState heartSave = RunPersistence.TryLoad();
            Assert.That(heartSave, Is.Not.Null);
            Assert.That(heartSave.ArcherTypeLevels, Is.Not.Null.And.Empty);
            Assert.That(heartSave.HeartGraph.Nodes.Single(node => node.NodeId == "army_sink").Level,
                Is.EqualTo(1));

            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);
            ArcherUnit basicAfterContinue = entityManager.GetComponentData<ArcherUnit>(
                FindArcher(entityManager, ArcherType.Basic));
            ArcherUnit frostAfterContinue = entityManager.GetComponentData<ArcherUnit>(
                FindArcher(entityManager, ArcherType.Frost));
            AssertArcherStatsEqual(basicAfter, basicAfterContinue,
                "Continue Heart damage/fire-rate/range effect'ini compound etmemeli.");
            AssertArcherStatsEqual(frostAfter, frostAfterContinue,
                "Continue Frost slow effect'ini compound etmemeli.");
        }

        private HeartNodeCatalogSO CreateCatalog()
        {
            HeartNodeCatalogSO catalog = ScriptableObject.CreateInstance<HeartNodeCatalogSO>();
            _createdObjects.Add(catalog);
            catalog.CatalogVersion = 42;
            catalog.Nodes = new[]
            {
                CreateDefinition("rapid_unlock", HeartNodeBranch.Army, HeartNodeType.Unlock,
                    new[] { HeartGraphConstants.RapidGuaranteeTag },
                    new HeartNodeEffect
                    {
                        Type = HeartNodeEffectType.UnlockArcherType,
                        ArcherType = ArcherType.Rapid
                    }),
                CreateDefinition("frost_unlock", HeartNodeBranch.Army, HeartNodeType.Unlock,
                    new[] { HeartGraphConstants.FrostGuaranteeTag },
                    new HeartNodeEffect
                    {
                        Type = HeartNodeEffectType.UnlockArcherType,
                        ArcherType = ArcherType.Frost
                    }),
                CreateDefinition("army_sink", HeartNodeBranch.Army, HeartNodeType.Repeatable,
                    new[] { HeartGraphConstants.RepeatableSinkTag },
                    new HeartNodeEffect
                    {
                        Type = HeartNodeEffectType.ModifyArcherDamagePercent,
                        ArcherType = ArcherType.Basic,
                        Value = 0.25d
                    },
                    new HeartNodeEffect
                    {
                        Type = HeartNodeEffectType.ModifyArcherFireRatePercent,
                        ArcherType = ArcherType.Basic,
                        Value = 0.20d,
                        SoftCap = 0.75d
                    },
                    new HeartNodeEffect
                    {
                        Type = HeartNodeEffectType.AddArcherRange,
                        ArcherType = ArcherType.Basic,
                        Value = 0.80d,
                        SoftCap = 3d
                    },
                    new HeartNodeEffect
                    {
                        Type = HeartNodeEffectType.ReduceFrostSlowMultiplier,
                        ArcherType = ArcherType.Frost,
                        Value = 0.10d,
                        SoftCap = 0.35d
                    }),
                CreateDefinition("wall_access", HeartNodeBranch.Defense, HeartNodeType.Unlock,
                    new[] { HeartGraphConstants.WallGuaranteeTag },
                    new HeartNodeEffect
                    {
                        Type = HeartNodeEffectType.ModifyWallMaxHpPercent,
                        Value = 0.25d
                    }),
                CreateDefinition("defense_sink", HeartNodeBranch.Defense, HeartNodeType.Repeatable,
                    new[] { HeartGraphConstants.RepeatableSinkTag }),
                CreateDefinition("production_sink", HeartNodeBranch.Production, HeartNodeType.Repeatable,
                    new[] { HeartGraphConstants.RepeatableSinkTag }),
                CreateDefinition("fireball_unlock", HeartNodeBranch.HeartMagic, HeartNodeType.Unlock,
                    new[] { HeartGraphConstants.FireballGuaranteeTag },
                    new HeartNodeEffect { Type = HeartNodeEffectType.UnlockSpellcasting }),
                CreateDefinition("heart_sink", HeartNodeBranch.HeartMagic, HeartNodeType.Repeatable,
                    new[] { HeartGraphConstants.RepeatableSinkTag })
            };
            return catalog;
        }

        private HeartNodeDefinitionSO CreateDefinition(
            string id,
            HeartNodeBranch branch,
            HeartNodeType type,
            string[] tags,
            params HeartNodeEffect[] effects)
        {
            HeartNodeDefinitionSO definition = ScriptableObject.CreateInstance<HeartNodeDefinitionSO>();
            _createdObjects.Add(definition);
            definition.Id = id;
            definition.Title = id;
            definition.Description = id + " test description";
            definition.Branch = branch;
            definition.Type = type;
            definition.MinimumDepth = 1;
            definition.MaximumDepth = 3;
            definition.BaseGraveEssenceCost = 10;
            definition.Tags = tags ?? Array.Empty<string>();
            definition.Effects = effects ?? Array.Empty<HeartNodeEffect>();
            definition.ConflictNodeIds = Array.Empty<string>();
            return definition;
        }

        private static GeneratedRunGraph CreateSavedGraph(int catalogVersion)
        {
            var graph = new GeneratedRunGraph
            {
                CatalogVersion = catalogVersion,
                Seed = 0xCA57u,
                RootNodeId = HeartGraphConstants.RootNodeId
            };
            AddNode(graph, HeartGraphConstants.RootNodeId, HeartNodeBranch.HeartMagic, 0,
                HeartNodeVisibility.Revealed, 1);
            AddNode(graph, "rapid_unlock", HeartNodeBranch.Army, 1,
                HeartNodeVisibility.Revealed, 1);
            AddNode(graph, "frost_unlock", HeartNodeBranch.Army, 2,
                HeartNodeVisibility.Revealed, 0);
            AddNode(graph, "army_sink", HeartNodeBranch.Army, 3);
            AddNode(graph, "wall_access", HeartNodeBranch.Defense, 1,
                HeartNodeVisibility.Revealed, 1);
            AddNode(graph, "defense_sink", HeartNodeBranch.Defense, 2,
                HeartNodeVisibility.Revealed, 0);
            AddNode(graph, "production_sink", HeartNodeBranch.Production, 1,
                HeartNodeVisibility.Revealed, 0);
            AddNode(graph, "fireball_unlock", HeartNodeBranch.HeartMagic, 1,
                HeartNodeVisibility.Revealed, 1);
            AddNode(graph, "heart_sink", HeartNodeBranch.HeartMagic, 2);

            AddEdge(graph, HeartGraphConstants.RootNodeId, "rapid_unlock");
            AddEdge(graph, "rapid_unlock", "frost_unlock");
            AddEdge(graph, "frost_unlock", "army_sink");
            AddEdge(graph, HeartGraphConstants.RootNodeId, "wall_access");
            AddEdge(graph, "wall_access", "defense_sink");
            AddEdge(graph, HeartGraphConstants.RootNodeId, "production_sink");
            AddEdge(graph, HeartGraphConstants.RootNodeId, "fireball_unlock");
            AddEdge(graph, "fireball_unlock", "heart_sink");
            return graph;
        }

        private static void AddNode(
            GeneratedRunGraph graph,
            string id,
            HeartNodeBranch branch,
            int depth,
            HeartNodeVisibility visibility = HeartNodeVisibility.Hidden,
            int level = 0)
        {
            graph.Nodes.Add(new GeneratedHeartNodeState
            {
                NodeId = id,
                Branch = branch,
                Depth = depth,
                Visibility = visibility,
                Level = level,
                LockState = HeartNodeLockState.Available,
                LockedByNodeId = string.Empty
            });
        }

        private static void AddEdge(GeneratedRunGraph graph, string from, string to)
        {
            graph.Edges.Add(new GeneratedHeartEdge { FromNodeId = from, ToNodeId = to });
        }

        private static Entity FindArcher(EntityManager entityManager, ArcherType type)
        {
            HashSet<Entity> entities = GetArchers(entityManager, type);
            return entities.Count > 0 ? entities.First() : Entity.Null;
        }

        private static HashSet<Entity> GetArchers(EntityManager entityManager, ArcherType type)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<ArcherUnit>() },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
            var result = new HashSet<Entity>();
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (entityManager.GetComponentData<ArcherUnit>(entity).Type == type)
                    result.Add(entity);
            }
            return result;
        }

        private static void AssertArcherStatsEqual(
            ArcherUnit expected, ArcherUnit actual, string message)
        {
            Assert.That(actual.Type, Is.EqualTo(expected.Type), message);
            Assert.That(actual.ArrowDamage, Is.EqualTo(expected.ArrowDamage).Within(0.0001f), message);
            Assert.That(actual.FireRate, Is.EqualTo(expected.FireRate).Within(0.0001f), message);
            Assert.That(actual.Range, Is.EqualTo(expected.Range).Within(0.0001f), message);
            Assert.That(actual.SlowDuration, Is.EqualTo(expected.SlowDuration).Within(0.0001f), message);
            Assert.That(actual.SlowMultiplier, Is.EqualTo(expected.SlowMultiplier).Within(0.0001f), message);
        }
    }
}
