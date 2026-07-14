using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class HeartDataModelTests
    {
        [Test]
        public void NodeTypes_ExposeTheFourBlueprintClassifications()
        {
            Array values = Enum.GetValues(typeof(HeartNodeType));

            Assert.That(values.Length, Is.EqualTo(4));
            Assert.That(values, Does.Contain(HeartNodeType.Unlock));
            Assert.That(values, Does.Contain(HeartNodeType.Repeatable));
            Assert.That(values, Does.Contain(HeartNodeType.Evolution));
            Assert.That(values, Does.Contain(HeartNodeType.Keystone));
        }

        [Test]
        public void Repeatable_IsDerivedFromNodeType_NotDuplicatedAsRuntimeState()
        {
            HeartNodeDefinitionSO definition = ScriptableObject.CreateInstance<HeartNodeDefinitionSO>();
            try
            {
                definition.Type = HeartNodeType.Repeatable;
                Assert.That(definition.IsRepeatable, Is.True);

                definition.Type = HeartNodeType.Unlock;
                Assert.That(definition.IsRepeatable, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void DefinitionValidation_RejectsInvalidDepthCostAndNonKeystoneConflict()
        {
            HeartNodeDefinitionSO definition = ScriptableObject.CreateInstance<HeartNodeDefinitionSO>();
            try
            {
                definition.Id = "damage_sink";
                definition.Type = HeartNodeType.Repeatable;
                definition.MinimumDepth = 4;
                definition.MaximumDepth = 2;
                definition.BaseGraveEssenceCost = 0;
                definition.CostGrowthPerLevel = double.NaN;
                definition.ConflictNodeIds = new[] { "other_keystone" };
                var errors = new List<string>();

                definition.CollectValidationErrors(errors);

                Assert.That(errors, Has.Count.EqualTo(4));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void KeystoneValidation_RequiresExactlyOneDifferentConflictNode()
        {
            HeartNodeDefinitionSO definition = ScriptableObject.CreateInstance<HeartNodeDefinitionSO>();
            try
            {
                definition.Id = "keystone_a";
                definition.Type = HeartNodeType.Keystone;
                definition.ConflictNodeIds = new[] { "keystone_b" };
                var validErrors = new List<string>();
                definition.CollectValidationErrors(validErrors);
                Assert.That(validErrors, Is.Empty);

                definition.ConflictNodeIds = new[] { "keystone_a" };
                var invalidErrors = new List<string>();
                definition.CollectValidationErrors(invalidErrors);
                Assert.That(invalidErrors, Has.Some.Contains("kendisiyle"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void SourceDefinition_DoesNotCarryRunLevelRevealOrLockState()
        {
            Type type = typeof(HeartNodeDefinitionSO);

            Assert.That(type.GetField("Level", BindingFlags.Instance | BindingFlags.Public), Is.Null);
            Assert.That(type.GetField("Visibility", BindingFlags.Instance | BindingFlags.Public), Is.Null);
            Assert.That(type.GetField("Revealed", BindingFlags.Instance | BindingFlags.Public), Is.Null);
            Assert.That(type.GetField("LockState", BindingFlags.Instance | BindingFlags.Public), Is.Null);
            Assert.That(type.GetField("LockedByNodeId", BindingFlags.Instance | BindingFlags.Public), Is.Null);
        }

        [Test]
        public void GeneratedRunGraph_JsonRoundTripPreservesSeedEdgesVisibilityLevelsAndLocks()
        {
            var graph = new GeneratedRunGraph
            {
                Seed = 987654321u,
                RootNodeId = "castle_heart"
            };
            graph.Nodes.Add(new GeneratedHeartNodeState
            {
                NodeId = "castle_heart",
                Branch = HeartNodeBranch.HeartMagic,
                Depth = 0,
                Visibility = HeartNodeVisibility.Revealed,
                Level = 1,
                LockState = HeartNodeLockState.Available
            });
            graph.Nodes.Add(new GeneratedHeartNodeState
            {
                NodeId = "keystone_b",
                Branch = HeartNodeBranch.Army,
                Depth = 5,
                Visibility = HeartNodeVisibility.Hidden,
                Level = 0,
                LockState = HeartNodeLockState.KeystoneConflict,
                LockedByNodeId = "keystone_a"
            });
            graph.Edges.Add(new GeneratedHeartEdge
            {
                FromNodeId = "castle_heart",
                ToNodeId = "keystone_b"
            });

            string json = JsonUtility.ToJson(graph);
            GeneratedRunGraph restored = JsonUtility.FromJson<GeneratedRunGraph>(json);

            Assert.That(restored.GraphVersion, Is.EqualTo(GeneratedRunGraph.CurrentGraphVersion));
            Assert.That(restored.Seed, Is.EqualTo(987654321u));
            Assert.That(restored.Nodes, Has.Count.EqualTo(2));
            Assert.That(restored.Nodes[1].Visibility, Is.EqualTo(HeartNodeVisibility.Hidden));
            Assert.That(restored.Nodes[1].LockState, Is.EqualTo(HeartNodeLockState.KeystoneConflict));
            Assert.That(restored.Nodes[1].LockedByNodeId, Is.EqualTo("keystone_a"));
            Assert.That(restored.Edges[0].FromNodeId, Is.EqualTo("castle_heart"));
        }

        [Test]
        public void GeneratedGraphState_DoesNotReferenceUnityAssets()
        {
            AssertNoUnityObjectFields(typeof(GeneratedRunGraph));
            AssertNoUnityObjectFields(typeof(GeneratedHeartNodeState));
            AssertNoUnityObjectFields(typeof(GeneratedHeartEdge));
        }

        [Test]
        public void GraveEssence_IsRunSaveStateAndNotMetaProgressState()
        {
            Assert.That(typeof(RunSaveState).GetField(nameof(RunSaveState.GraveEssence)), Is.Not.Null);
            Assert.That(typeof(MetaProgressState).GetField("GraveEssence"), Is.Null);
        }

        private static void AssertNoUnityObjectFields(Type type)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                Assert.That(typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType), Is.False,
                    $"{type.Name}.{field.Name} source asset referansi tasimamali.");
            }
        }
    }
}
