using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadWalls.Tests
{
    public class GameplayTelemetryPlayModeTests
    {
        private readonly List<GameplayTelemetryRecord> _records =
            new List<GameplayTelemetryRecord>();
        private byte[] _originalRunSave;
        private string _runSavePath;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _runSavePath = Path.Combine(Application.persistentDataPath, "run_save.json");
            _originalRunSave = File.Exists(_runSavePath)
                ? File.ReadAllBytes(_runSavePath)
                : null;
            RunPersistence.Delete();
            _records.Clear();
            GameplayTelemetry.Emitted += OnTelemetryEmitted;
            GameBootstrap.PendingAction = GameBootstrap.StartAction.None;

            SceneManager.LoadScene("NewGameScene", LoadSceneMode.Single);
            for (int frame = 0; frame < 300; frame++)
            {
                if (GameManager.Instance != null && GameManager.Instance.ContinuousSiegeCycle.Enabled)
                    break;
                yield return null;
            }
            Assert.That(GameManager.Instance, Is.Not.Null);
            Assert.That(GameManager.Instance.ContinuousSiegeCycle.Enabled, Is.True);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameplayTelemetry.Emitted -= OnTelemetryEmitted;
            RunPersistence.Delete();
            if (_originalRunSave != null)
                File.WriteAllBytes(_runSavePath, _originalRunSave);
            GameBootstrap.PendingAction = GameBootstrap.StartAction.None;
            yield return null;
        }

        [UnityTest]
        public IEnumerator NewRun_EmitsExactRunStartedOnce_AndContinueDoesNotDuplicate()
        {
            GameManager gameManager = GameManager.Instance;
            _records.Clear();
            gameManager.RestartGame();

            for (int frame = 0; frame < 180 && _records.Count == 0; frame++)
                yield return null;

            Assert.That(_records.Count, Is.EqualTo(1));
            GameplayTelemetryRecord record = _records[0];
            Assert.That(record.EventName, Is.EqualTo(GameplayTelemetry.RunStartedEventName));
            Assert.That(record.RunId, Is.EqualTo(gameManager.CurrentRunId));
            RunStartedTelemetryPayload payload =
                JsonUtility.FromJson<RunStartedTelemetryPayload>(record.PayloadJson);

            Assert.That(payload.MetaCatalogConfigured, Is.EqualTo(gameManager.MetaCatalog != null));
            Assert.That(payload.MetaCatalogDefinitionCount,
                Is.EqualTo(gameManager.MetaCatalog?.Upgrades?.Length ?? 0));
            Assert.That(payload.MetaLevels.Count,
                Is.EqualTo(gameManager.MetaCatalog?.Upgrades?.Length ?? 0));
            Assert.That(payload.StartingResources.Wood, Is.EqualTo(gameManager.Resources.Wood));
            Assert.That(payload.StartingResources.Stone, Is.EqualTo(gameManager.Resources.Stone));
            Assert.That(payload.StartingResources.Iron, Is.EqualTo(gameManager.Resources.Iron));
            Assert.That(payload.StartingResources.Food, Is.EqualTo(gameManager.Resources.Food));
            Assert.That(payload.StartingResources.Arrows, Is.EqualTo(gameManager.ArrowSupply.Current));
            Assert.That(payload.StartingResources.Population, Is.EqualTo(gameManager.Population.Total));

            HeartRuntimeTuningTelemetry heart = gameManager.GetHeartRuntimeTuningTelemetry();
            Assert.That(payload.Heart.CatalogConfigured, Is.EqualTo(heart.HasCatalog));
            Assert.That(payload.Heart.RuntimeAttempted, Is.True);
            Assert.That(payload.Heart.GraphReady, Is.EqualTo(heart.RuntimeReady));
            Assert.That(payload.Heart.GraphVersion, Is.EqualTo(heart.GraphVersion));
            Assert.That(payload.Heart.CatalogVersion, Is.EqualTo(heart.CatalogVersion));
            Assert.That(payload.Heart.Seed, Is.EqualTo(heart.Seed));

            string originalRunId = gameManager.CurrentRunId;
            Assert.That(gameManager.SaveRunSnapshot(), Is.True);
            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);
            Assert.That(gameManager.CurrentRunId, Is.EqualTo(originalRunId));
            for (int frame = 0; frame < 5; frame++)
                yield return null;

            Assert.That(_records.Count, Is.EqualTo(1),
                "Continue ayni run icin ikinci run_started uretmemeli.");
        }

        private void OnTelemetryEmitted(GameplayTelemetryRecord record)
        {
            if (record.EventName == GameplayTelemetry.RunStartedEventName)
                _records.Add(record);
        }
    }
}
