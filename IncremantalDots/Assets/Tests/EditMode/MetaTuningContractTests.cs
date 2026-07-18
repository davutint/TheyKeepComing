using NUnit.Framework;
using UnityEditor;

namespace DeadWalls.Tests
{
    public class MetaTuningContractTests
    {
        private const string CatalogPath =
            "Assets/ScriptableObject/MobileCastle/Meta/MetaUpgradeCatalog.asset";

        [Test]
        public void DefaultReward_GivesExactlyOneSoulPerKillAndKeepsRunBonuses()
        {
            var settings = new MetaRewardSettings();

            Assert.That(MetaRewardCalculator.TryCalculate(
                settings, 10, 10_000, 200, 9, out MetaRewardQuote quote), Is.True);

            Assert.That(quote.KillSouls, Is.EqualTo(10_000));
            Assert.That(quote.DaySouls, Is.EqualTo(100));
            Assert.That(quote.NightSouls, Is.EqualTo(225));
            Assert.That(quote.PopulationSouls, Is.EqualTo(40));
            Assert.That(quote.RecordSouls, Is.EqualTo(500));
            Assert.That(quote.TotalSouls, Is.EqualTo(10_865));
            Assert.That(quote.NightsSurvived, Is.EqualTo(9));
            Assert.That(quote.NewRecord, Is.True);
            Assert.That(MetaRewardCalculator.IsStructurallyValid(quote), Is.True);
        }

        [Test]
        public void NonRecordRun_OmitsRecordComponentButKeepsSurvivalAndPopulation()
        {
            var settings = new MetaRewardSettings();

            Assert.That(MetaRewardCalculator.TryCalculate(
                settings, 4, 1000, 100, 4, out MetaRewardQuote quote), Is.True);

            Assert.That(quote.KillSouls, Is.EqualTo(1000));
            Assert.That(quote.DaySouls, Is.EqualTo(40));
            Assert.That(quote.NightSouls, Is.EqualTo(75));
            Assert.That(quote.PopulationSouls, Is.EqualTo(20));
            Assert.That(quote.RecordSouls, Is.Zero);
            Assert.That(quote.TotalSouls, Is.EqualTo(1135));
            Assert.That(quote.NewRecord, Is.False);
        }

        [Test]
        public void RewardSettings_RejectIncreasingOrInvalidBands()
        {
            var settings = new MetaRewardSettings
            {
                FirstKillBandLimit = 100,
                SecondKillBandLimit = 100,
                FirstBandSoulsPerKill = 0.1f,
                SecondBandSoulsPerKill = 0.2f
            };

            Assert.That(settings.IsValid(), Is.False);
            Assert.That(MetaRewardCalculator.TryCalculate(
                settings, 1, 10, 10, 0, out _), Is.False);
        }

        [Test]
        public void QuotedReward_AppliesExactAmountAndRemainsIdempotent()
        {
            var state = new MetaProgressState { BestDay = 3 };
            Assert.That(MetaRewardCalculator.TryCalculate(
                new MetaRewardSettings(), 4, 1000, 100, state.BestDay,
                out MetaRewardQuote quote), Is.True);

            MetaRunResult first = MetaProgression.ApplyRunResult(state, "quoted-run", quote);
            MetaRunResult duplicate = MetaProgression.ApplyRunResult(state, "quoted-run", quote);

            Assert.That(first.SoulsEarned, Is.EqualTo(1335));
            Assert.That(state.Souls, Is.EqualTo(1335));
            Assert.That(state.BestDay, Is.EqualTo(4));
            Assert.That(state.TotalKillsAllTime, Is.EqualTo(1000));
            Assert.That(duplicate.AlreadyRewarded, Is.True);
            Assert.That(duplicate.SoulsEarned, Is.Zero);
            Assert.That(state.Souls, Is.EqualTo(1335));
        }

        [Test]
        public void QuoteValidation_RejectsTamperedComponentSum()
        {
            Assert.That(MetaRewardCalculator.TryCalculate(
                new MetaRewardSettings(), 2, 250, 60, 1,
                out MetaRewardQuote quote), Is.True);

            quote.TotalSouls++;

            Assert.That(MetaRewardCalculator.IsStructurallyValid(quote), Is.False);
        }

        [Test]
        public void ProductionCatalog_KeepsAuditedIncrementalCostsAndEffects()
        {
            MetaUpgradeCatalogSO catalog = AssetDatabase.LoadAssetAtPath<MetaUpgradeCatalogSO>(CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.ValidateCatalog(), Is.Empty);
            Assert.That(catalog.RewardSettings.FirstBandSoulsPerKill, Is.EqualTo(1f));
            Assert.That(catalog.RewardSettings.SecondBandSoulsPerKill, Is.EqualTo(1f));
            Assert.That(catalog.RewardSettings.OverflowSoulsPerKill, Is.EqualTo(1f));
            Assert.That(catalog.Upgrades, Has.Length.EqualTo(11));
            AssertUpgrade(catalog, "start_wood", 150, 0.60f, 0, 75f);
            AssertUpgrade(catalog, "start_stone", 175, 0.65f, 0, 50f);
            AssertUpgrade(catalog, "start_iron", 225, 0.70f, 0, 30f);
            AssertUpgrade(catalog, "start_food", 150, 0.60f, 0, 60f);
            AssertUpgrade(catalog, "start_archers", 400, 1.00f, 1000, 1f);
            AssertUpgrade(catalog, "start_beds", 250, 0.75f, 0, 5f);
            AssertUpgrade(catalog, "wall_hp", 300, 0.80f, 5, 0.05f);
            AssertUpgrade(catalog, "production", 350, 0.80f, 5, 0.03f);
            AssertUpgrade(catalog, "arrow_efficiency", 500, 0.90f, 10, 1f);
            AssertUpgrade(catalog, "essence_gain", 600, 0.90f, 10, 0.05f);
            AssertUpgrade(catalog, "node_pool_unlock", 2000, 0f, 1, 0f);

            Assert.That(catalog.GetUpgrade("wall_hp").GetTotalEffect(5), Is.EqualTo(0.25d).Within(0.0001d));
            Assert.That(catalog.GetUpgrade("production").GetTotalEffect(5), Is.EqualTo(0.15d).Within(0.0001d));
            Assert.That(catalog.GetUpgrade("essence_gain").GetTotalEffect(10), Is.EqualTo(0.50d).Within(0.0001d));
        }

        [TestCase(0)]
        [TestCase(99)]
        [TestCase(100)]
        [TestCase(101)]
        [TestCase(1000)]
        [TestCase(1001)]
        [TestCase(10_000)]
        public void ProductionReward_EverySkeletonContributesExactlyOneSoul(int kills)
        {
            MetaUpgradeCatalogSO catalog = AssetDatabase.LoadAssetAtPath<MetaUpgradeCatalogSO>(CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            Assert.That(MetaRewardCalculator.TryCalculate(
                catalog.RewardSettings,
                1,
                kills,
                0,
                1,
                out MetaRewardQuote quote), Is.True);
            Assert.That(quote.KillSouls, Is.EqualTo(kills));
        }

        private static void AssertUpgrade(
            MetaUpgradeCatalogSO catalog,
            string id,
            int baseCost,
            float growth,
            int maxLevel,
            float valuePerLevel)
        {
            MetaUpgradeSO upgrade = catalog.GetUpgrade(id);
            Assert.That(upgrade, Is.Not.Null, id);
            Assert.That(upgrade.BaseCost, Is.EqualTo(baseCost), id);
            Assert.That(upgrade.CostGrowthPerLevel, Is.EqualTo(growth).Within(0.0001f), id);
            Assert.That(upgrade.MaxLevel, Is.EqualTo(maxLevel), id);
            Assert.That(upgrade.ValuePerLevel, Is.EqualTo(valuePerLevel).Within(0.0001f), id);
        }
    }
}
