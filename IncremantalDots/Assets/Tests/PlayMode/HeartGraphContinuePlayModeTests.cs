using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
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
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (GameManager.Instance != null && _catalogField != null)
            {
                _catalogField.SetValue(GameManager.Instance, _originalCatalog);
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
            Assert.That(gameManager.TryBuildHeartPresentation(
                out HeartGraphPresentation presentation,
                out IReadOnlyList<string> errors), Is.True, string.Join(" | ", errors));
            Assert.That(
                presentation.Nodes.Single(node => node.ExactNodeId == "rapid_unlock").Level,
                Is.EqualTo(1));
            Assert.That(
                presentation.Nodes.Single(node => node.ExactNodeId == "wall_access").Level,
                Is.EqualTo(1));

            Assert.That(gameManager.SaveRunSnapshot(), Is.True);
            RunSaveState replayedSave = RunPersistence.TryLoad();
            Assert.That(replayedSave, Is.Not.Null);
            Assert.That(replayedSave.HasHeartGraph, Is.True);
            Assert.That(replayedSave.GraveEssence, Is.EqualTo(765));
            Assert.That(JsonUtility.ToJson(replayedSave.HeartGraph), Is.EqualTo(exactGraphJson),
                "Continue sonrasi graph source catalog'dan yeniden zar atilmamali.");
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
                    new[] { HeartGraphConstants.RepeatableSinkTag }),
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
                HeartNodeVisibility.Revealed, 0);
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
    }
}
