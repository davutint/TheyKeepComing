using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeadWalls.Tests
{
    public class MetaProgressionSchemaTests
    {
        private string _metaPath;
        private string _tempPath;
        private byte[] _originalMeta;
        private byte[] _originalTemp;
        private bool _hadMeta;
        private bool _hadTemp;

        [SetUp]
        public void SetUp()
        {
            _metaPath = Path.Combine(Application.persistentDataPath, "meta_progress.json");
            _tempPath = _metaPath + ".tmp";
            _hadMeta = File.Exists(_metaPath);
            _hadTemp = File.Exists(_tempPath);
            _originalMeta = _hadMeta ? File.ReadAllBytes(_metaPath) : null;
            _originalTemp = _hadTemp ? File.ReadAllBytes(_tempPath) : null;

            DeleteIfExists(_metaPath);
            DeleteIfExists(_tempPath);
            TutorialSessionProgress.BeginNewPlaySession();
            MetaProgression.Load();
        }

        [TearDown]
        public void TearDown()
        {
            DeleteIfExists(_metaPath);
            DeleteIfExists(_tempPath);
            RestoreIfNeeded(_metaPath, _hadMeta, _originalMeta);
            RestoreIfNeeded(_tempPath, _hadTemp, _originalTemp);
            TutorialSessionProgress.BeginNewPlaySession();
            MetaProgression.Load();
        }

        [Test]
        public void MissingSave_CreatesCanonicalWritableState()
        {
            Assert.That(MetaProgression.LoadStatus, Is.EqualTo(MetaProgressLoadStatus.CreatedNew));
            Assert.That(MetaProgression.CanPersist, Is.True);
            Assert.That(MetaProgression.State.Version, Is.EqualTo(MetaProgressState.CurrentVersion));
            Assert.That(MetaProgression.State.Upgrades, Is.Not.Null);
            Assert.That(MetaProgression.State.UnlockedPoolIds, Is.Not.Null);
            Assert.That(MetaProgression.State.TutorialFlags, Is.Not.Null);
            Assert.That(MetaProgression.State.RewardedRunIds, Is.Not.Null);
        }

        [Test]
        public void Version2_MigratesToV3AndPreservesCurrencyUpgradesAndReceipts()
        {
            var legacy = new MetaProgressState
            {
                Version = 2,
                Souls = 725,
                TotalSoulsEarned = 1200,
                BestDay = 14,
                TotalRuns = 3,
                TotalKillsAllTime = 987,
                Upgrades = new List<MetaUpgradeLevel>
                {
                    new MetaUpgradeLevel { Id = "future_upgrade", Level = 4 }
                },
                UnlockedPoolIds = null,
                TutorialFlags = null,
                RewardedRunIds = new List<string> { "run-v2" }
            };
            File.WriteAllText(_metaPath, JsonUtility.ToJson(legacy, true));

            MetaProgression.Load();

            Assert.That(MetaProgression.LoadStatus, Is.EqualTo(MetaProgressLoadStatus.Migrated));
            Assert.That(MetaProgression.CanPersist, Is.True);
            Assert.That(MetaProgression.State.Version, Is.EqualTo(3));
            Assert.That(MetaProgression.State.Souls, Is.EqualTo(725));
            Assert.That(MetaProgression.State.TotalSoulsEarned, Is.EqualTo(1200));
            Assert.That(MetaProgression.State.BestDay, Is.EqualTo(14));
            Assert.That(MetaProgression.State.TotalRuns, Is.EqualTo(3));
            Assert.That(MetaProgression.State.TotalKillsAllTime, Is.EqualTo(987));
            Assert.That(MetaProgression.GetUpgradeLevel("future_upgrade"), Is.EqualTo(4));
            Assert.That(MetaProgression.HasRewardedRun("run-v2"), Is.True);
            Assert.That(MetaProgression.State.UnlockedPoolIds, Is.Empty);
            Assert.That(MetaProgression.State.TutorialFlags, Is.Empty);

            var durable = JsonUtility.FromJson<MetaProgressState>(File.ReadAllText(_metaPath));
            Assert.That(durable.Version, Is.EqualTo(MetaProgressState.CurrentVersion));
        }

        [Test]
        public void Version1_MigratesThroughReceiptSchemaWithoutInventingOwnedState()
        {
            var legacy = new MetaProgressState
            {
                Version = 1,
                Souls = 90,
                TotalSoulsEarned = 100,
                Upgrades = new List<MetaUpgradeLevel>
                {
                    new MetaUpgradeLevel { Id = "start_wood", Level = 2 }
                },
                UnlockedPoolIds = null,
                TutorialFlags = null,
                RewardedRunIds = null
            };
            File.WriteAllText(_metaPath, JsonUtility.ToJson(legacy));

            MetaProgression.Load();

            Assert.That(MetaProgression.LoadStatus, Is.EqualTo(MetaProgressLoadStatus.Migrated));
            Assert.That(MetaProgression.State.Version, Is.EqualTo(3));
            Assert.That(MetaProgression.State.Souls, Is.EqualTo(90));
            Assert.That(MetaProgression.GetUpgradeLevel("start_wood"), Is.EqualTo(2));
            Assert.That(MetaProgression.State.RewardedRunIds, Is.Empty);
            Assert.That(MetaProgression.State.UnlockedPoolIds, Is.Empty);
            Assert.That(MetaProgression.State.TutorialFlags, Is.Empty);
        }

        [Test]
        public void FutureVersion_FailsClosedAndCannotOverwriteOriginalFile()
        {
            const string futureJson = "{\"Version\":99,\"Souls\":999999}";
            File.WriteAllText(_metaPath, futureJson);
            LogAssert.Expect(LogType.Error, new Regex("Meta save fail-closed kilitlendi: Desteklenmeyen meta schema v99"));

            MetaProgression.Load();

            Assert.That(MetaProgression.LoadStatus, Is.EqualTo(MetaProgressLoadStatus.UnsupportedVersion));
            Assert.That(MetaProgression.CanPersist, Is.False);
            Assert.That(MetaProgression.State.Souls, Is.Zero);
            Assert.That(MetaProgression.ResetTutorialFlags(
                FirstRunOnboardingUI.GetTutorialProgressFlagIds()), Is.True,
                "Tutorial session reset'i meta save yazma durumundan bagimsizdir.");
            LogAssert.Expect(LogType.Error, new Regex("Save reddedildi; load status: UnsupportedVersion"));
            Assert.That(MetaProgression.Save(), Is.False);
            Assert.That(File.ReadAllText(_metaPath), Is.EqualTo(futureJson));
        }

        [Test]
        public void CorruptJson_FailsClosedAndCannotOverwriteOriginalFile()
        {
            const string corruptJson = "{ definitely-not-json";
            File.WriteAllText(_metaPath, corruptJson);
            LogAssert.Expect(LogType.Error, new Regex("Meta save fail-closed kilitlendi:"));

            MetaProgression.Load();

            Assert.That(MetaProgression.LoadStatus, Is.EqualTo(MetaProgressLoadStatus.Corrupt));
            Assert.That(MetaProgression.CanPersist, Is.False);
            Assert.That(MetaProgression.ResetTutorialFlags(
                FirstRunOnboardingUI.GetTutorialProgressFlagIds()), Is.True,
                "Tutorial session reset'i corrupt meta save'den bagimsizdir.");
            LogAssert.Expect(LogType.Error, new Regex("Save reddedildi; load status: Corrupt"));
            Assert.That(MetaProgression.Save(), Is.False);
            Assert.That(File.ReadAllText(_metaPath), Is.EqualTo(corruptJson));
        }

        [Test]
        public void CanonicalV3_NormalizesOwnedListsWithoutDroppingUnknownUpgradeIds()
        {
            var state = new MetaProgressState
            {
                Version = 3,
                Souls = -5,
                TotalSoulsEarned = -8,
                Upgrades = new List<MetaUpgradeLevel>
                {
                    null,
                    new MetaUpgradeLevel { Id = " future_upgrade ", Level = 2 },
                    new MetaUpgradeLevel { Id = "future_upgrade", Level = 5 },
                    new MetaUpgradeLevel { Id = "invalid", Level = -1 }
                },
                UnlockedPoolIds = new List<string> { "spell_pool", "", "spell_pool", "node_pool" },
                TutorialFlags = new List<string> { "tutorial.complete", " tutorial.complete " },
                RewardedRunIds = BuildReceiptIds(130)
            };
            File.WriteAllText(_metaPath, JsonUtility.ToJson(state));

            MetaProgression.Load();

            Assert.That(MetaProgression.LoadStatus, Is.EqualTo(MetaProgressLoadStatus.Loaded));
            Assert.That(MetaProgression.State.Souls, Is.Zero);
            Assert.That(MetaProgression.State.TotalSoulsEarned, Is.Zero);
            Assert.That(MetaProgression.GetUpgradeLevel("future_upgrade"), Is.EqualTo(5));
            Assert.That(MetaProgression.GetUpgradeLevel("invalid"), Is.Zero);
            Assert.That(MetaProgression.State.Upgrades, Has.Count.EqualTo(1));
            Assert.That(MetaProgression.State.UnlockedPoolIds, Is.EqualTo(new[] { "spell_pool", "node_pool" }));
            Assert.That(MetaProgression.State.TutorialFlags, Is.Empty,
                "Legacy tutorial flags meta JSON'dan yuklenmemelidir.");
            Assert.That(MetaProgression.State.RewardedRunIds, Has.Count.EqualTo(128));
            Assert.That(MetaProgression.State.RewardedRunIds[0], Is.EqualTo("run-002"));
            Assert.That(MetaProgression.State.RewardedRunIds[127], Is.EqualTo("run-129"));
        }

        [Test]
        public void PoolUnlockPersistsWhileTutorialProgressResetsWithPlaySession()
        {
            Assert.That(MetaProgression.TryUnlockPoolContent("future_spell_pool"), Is.True);
            Assert.That(MetaProgression.SetTutorialFlag("tutorial.complete", true), Is.True);

            MetaProgression.Load();

            Assert.That(MetaProgression.LoadStatus, Is.EqualTo(MetaProgressLoadStatus.Loaded));
            Assert.That(MetaProgression.HasPoolUnlock("future_spell_pool"), Is.True);
            Assert.That(MetaProgression.HasTutorialFlag("tutorial.complete"), Is.True);
            Assert.That(File.ReadAllText(_metaPath), Does.Not.Contain("TutorialFlags"));

            TutorialSessionProgress.BeginNewPlaySession();
            MetaProgression.Load();
            Assert.That(MetaProgression.HasTutorialFlag("tutorial.complete"), Is.False);
            Assert.That(MetaProgression.HasPoolUnlock("future_spell_pool"), Is.True);
        }

        [Test]
        public void TutorialSessionReset_ClearsExactSetAndPreservesOtherMetaState()
        {
            string[] tutorialFlags = FirstRunOnboardingUI.GetTutorialProgressFlagIds();
            foreach (string flagId in tutorialFlags)
                Assert.That(MetaProgression.SetTutorialFlag(flagId, true), Is.True, flagId);

            const string futureTutorialFlag = "tutorial.future.keep";
            Assert.That(MetaProgression.SetTutorialFlag(futureTutorialFlag, true), Is.True);
            Assert.That(MetaProgression.TryUnlockPoolContent("future_spell_pool"), Is.True);
            MetaProgression.State.Souls = 321;
            Assert.That(MetaProgression.Save(), Is.True);
            Assert.That(File.ReadAllText(_metaPath), Does.Not.Contain("TutorialFlags"));

            Assert.That(MetaProgression.ResetTutorialFlags(tutorialFlags), Is.True);
            foreach (string flagId in tutorialFlags)
                Assert.That(MetaProgression.HasTutorialFlag(flagId), Is.False, flagId);
            Assert.That(MetaProgression.HasTutorialFlag(futureTutorialFlag), Is.True);
            Assert.That(MetaProgression.HasPoolUnlock("future_spell_pool"), Is.True);
            Assert.That(MetaProgression.State.Souls, Is.EqualTo(321));

            MetaProgression.Load();

            foreach (string flagId in tutorialFlags)
                Assert.That(MetaProgression.HasTutorialFlag(flagId), Is.False, flagId);
            Assert.That(MetaProgression.HasTutorialFlag(futureTutorialFlag), Is.True);
            Assert.That(MetaProgression.HasPoolUnlock("future_spell_pool"), Is.True);
            Assert.That(MetaProgression.State.Souls, Is.EqualTo(321));
        }

        private static List<string> BuildReceiptIds(int count)
        {
            var result = new List<string>(count);
            for (int i = 0; i < count; i++)
                result.Add($"run-{i:000}");
            return result;
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static void RestoreIfNeeded(string path, bool hadFile, byte[] contents)
        {
            if (hadFile)
                File.WriteAllBytes(path, contents);
        }
    }
}
