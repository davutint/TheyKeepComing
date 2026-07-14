using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadWalls.Tests
{
    public class ArcherFormationPlayModeTests
    {
        private string _runSavePath;
        private byte[] _originalRunSave;

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

            Assert.That(GameManager.Instance, Is.Not.Null,
                "NewGameScene GameManager olusturmadi.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            RunPersistence.Delete();
            if (_originalRunSave != null)
                File.WriteAllBytes(_runSavePath, _originalRunSave);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FormationV1_BuildsStableThousandPointsAndContinueUsesSameLayout()
        {
            GameManager gameManager = GameManager.Instance;
            bool runtimeReady = false;
            for (int frame = 0; frame < 300; frame++)
            {
                if (gameManager.SaveRunSnapshot())
                {
                    runtimeReady = true;
                    break;
                }
                yield return null;
            }
            Assert.That(runtimeReady, Is.True,
                "GameManager/SubScene 300 frame icinde hazir olmadi.");

            MobileCastleArcherTilePlacement placement =
                Object.FindFirstObjectByType<MobileCastleArcherTilePlacement>(
                    FindObjectsInactive.Include);
            Assert.That(placement, Is.Not.Null);
            Assert.That(placement.FormationDefinition, Is.Not.Null,
                "Scene versioned ArcherFormationV1 asset'ine bagli degil.");
            Assert.That(placement.FormationVersion,
                Is.EqualTo(ArcherFormationUtility.CurrentVersion));
            Assert.That(placement.SpawnCellCount,
                Is.EqualTo(ArcherFormationUtility.RequiredTileCount));
            Assert.That(placement.FormationCapacity,
                Is.EqualTo(ArcherFormationUtility.TotalCapacity));
            Assert.That(gameManager.ActiveArcherFormationVersion,
                Is.EqualTo(placement.FormationVersion));

            float3[] before = ReadAllPositions(placement);
            Assert.That(new HashSet<string>(Quantize(before)).Count,
                Is.EqualTo(ArcherFormationUtility.TotalCapacity));

            Assert.That(gameManager.SaveRunSnapshot(), Is.True);
            RunSaveState save = RunPersistence.TryLoad();
            Assert.That(save, Is.Not.Null);
            Assert.That(save.ArcherFormationVersion,
                Is.EqualTo(ArcherFormationUtility.CurrentVersion));

            placement.RebuildCache();
            float3[] afterRebuild = ReadAllPositions(placement);
            AssertPositionsEqual(before, afterRebuild);

            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);
            yield return null;

            MobileCastleArcherTilePlacement restoredPlacement =
                Object.FindFirstObjectByType<MobileCastleArcherTilePlacement>(
                    FindObjectsInactive.Include);
            Assert.That(restoredPlacement, Is.Not.Null);
            Assert.That(gameManager.ActiveArcherFormationVersion,
                Is.EqualTo(save.ArcherFormationVersion));
            AssertPositionsEqual(before, ReadAllPositions(restoredPlacement));
            AssertLiveArchersUseFirstFormationSlots(
                World.DefaultGameObjectInjectionWorld.EntityManager,
                restoredPlacement);
        }

        private static float3[] ReadAllPositions(MobileCastleArcherTilePlacement placement)
        {
            var positions = new float3[ArcherFormationUtility.TotalCapacity];
            for (int i = 0; i < positions.Length; i++)
            {
                Assert.That(placement.TryGetSpawnPosition(
                    i, ArcherFormationUtility.CurrentVersion, out positions[i]), Is.True,
                    $"Formation slot {i} okunamadi.");
            }

            return positions;
        }

        private static IEnumerable<string> Quantize(float3[] positions)
        {
            for (int i = 0; i < positions.Length; i++)
                yield return $"{positions[i].x:F6}|{positions[i].y:F6}|{positions[i].z:F6}";
        }

        private static void AssertPositionsEqual(float3[] expected, float3[] actual)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(math.distance(expected[i], actual[i]), Is.LessThan(0.000001f),
                    $"Formation slot {i} deterministic degil.");
            }
        }

        private static void AssertLiveArchersUseFirstFormationSlots(
            EntityManager entityManager,
            MobileCastleArcherTilePlacement placement)
        {
            var query = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(ArcherUnit), typeof(LocalTransform) },
                None = new ComponentType[] { typeof(Prefab) }
            });
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            var expected = new List<float3>(entities.Length);
            for (int i = 0; i < entities.Length; i++)
            {
                Assert.That(placement.TryGetSpawnPosition(i, out float3 position), Is.True);
                expected.Add(position);
            }

            for (int i = 0; i < entities.Length; i++)
            {
                float3 actual = entityManager.GetComponentData<LocalTransform>(entities[i]).Position;
                int match = expected.FindIndex(item => math.distance(item, actual) < 0.000001f);
                Assert.That(match, Is.GreaterThanOrEqualTo(0),
                    $"Canli okcu {i} ilk formation slot setinde degil.");
                expected.RemoveAt(match);
            }

            query.Dispose();
        }
    }
}
