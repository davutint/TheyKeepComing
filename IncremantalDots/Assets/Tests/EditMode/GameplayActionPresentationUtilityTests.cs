using NUnit.Framework;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class GameplayActionPresentationUtilityTests
    {
        [Test]
        public void MissingResources_UsesFullNamesAndExactDeficits()
        {
            var cost = new ResourceCost(100, 25, 30, 0);
            var available = new ResourceData { Wood = 86, Stone = 25, Iron = 9 };

            Assert.That(
                GameplayActionFeedbackUtility.BuildMissingResourceReason(cost, available),
                Is.EqualTo("NOT ENOUGH RESOURCES  ·  NEED 14 MORE WOOD  ·  21 MORE IRON"));
        }

        [Test]
        public void ArcherRecruitment_PrioritizesActionableGameplayBlocker()
        {
            var cost = new ResourceCost(50, 0, 10, 0);
            var available = new ResourceData { Wood = 500, Iron = 500 };

            Assert.That(
                GameplayActionFeedbackUtility.BuildArcherRecruitmentFailure(
                    true, 10, 0, 1, cost, available),
                Is.EqualTo("NOT ENOUGH WORKERS  ·  NEED 1 MORE"));
            Assert.That(
                GameplayActionFeedbackUtility.BuildArcherRecruitmentFailure(
                    true, 0, 50, 1, cost, available),
                Is.EqualTo("GARRISON FULL  ·  MAXIMUM ARCHER CAPACITY REACHED"));

            Assert.That(
                GameplayActionFeedbackUtility.CanExplainArcherRecruitmentFailure(true, 10),
                Is.True);
            Assert.That(
                GameplayActionFeedbackUtility.CanExplainArcherRecruitmentFailure(false, 10),
                Is.False);
            Assert.That(
                GameplayActionFeedbackUtility.CanExplainArcherRecruitmentFailure(true, 0),
                Is.False);
        }

        [Test]
        public void MetaUpgradeFailure_ReportsExactMissingCurrency()
        {
            Assert.That(
                GameplayActionFeedbackUtility.BuildMetaUpgradeFailure(
                    true, false, 350, 125, "Embers"),
                Is.EqualTo("NOT ENOUGH EMBERS  ·  NEED 225 MORE EMBERS"));
        }

        [Test]
        public void ResearchFailures_TranslateInternalReasonsIntoEnglishPlayerCopy()
        {
            var cost = new ResourceCost(100, 0, 25, 0);
            var resources = new ResourceData { Wood = 90, Iron = 5 };

            Assert.That(
                GameplayActionFeedbackUtility.BuildTechResearchFailure(
                    "NEED 10W 20I",
                    cost,
                    resources),
                Is.EqualTo("NOT ENOUGH RESOURCES  ·  NEED 10 MORE WOOD  ·  20 MORE IRON"));

            var quote = new HeartPurchaseQuote { TotalGraveEssenceCost = 250 };
            Assert.That(
                GameplayActionFeedbackUtility.BuildHeartPurchaseFailure(
                    HeartPurchaseFailureReason.InsufficientGraveEssence,
                    quote,
                    175),
                Is.EqualTo("NOT ENOUGH GRAVE ESSENCE  ·  NEED 75 MORE"));
            Assert.That(
                GameplayActionFeedbackUtility.BuildHeartPurchaseFailure(
                    HeartPurchaseFailureReason.Hidden,
                    null,
                    0),
                Is.EqualTo("TECHNOLOGY HIDDEN  ·  REVEAL A CONNECTED NODE FIRST"));
        }

        [Test]
        public void MetaEffectProgression_ExplainsCurrentAndAfterPurchaseValues()
        {
            MetaUpgradeSO upgrade = ScriptableObject.CreateInstance<MetaUpgradeSO>();
            try
            {
                upgrade.EffectType = MetaUpgradeEffectType.StartingResource;
                upgrade.Resource = EconomyFocusType.Wood;
                upgrade.ValuePerLevel = 75f;

                Assert.That(
                    MetaUpgradePresentationUtility.BuildEffectProgression(upgrade, 2),
                    Is.EqualTo("NEXT RUN STARTING WOOD: +150  →  +225"));

                upgrade.EffectType = MetaUpgradeEffectType.WallHpPercent;
                upgrade.ValuePerLevel = 0.05f;
                Assert.That(
                    MetaUpgradePresentationUtility.BuildEffectProgression(upgrade, 1),
                    Is.EqualTo("WALL MAX HP: +5%  →  +10%"));

                upgrade.MaxLevel = 2;
                Assert.That(
                    MetaUpgradePresentationUtility.BuildEffectProgression(upgrade, 2),
                    Is.EqualTo("WALL MAX HP: +10%  ·  MAXIMUM ACTIVE"));
            }
            finally
            {
                Object.DestroyImmediate(upgrade);
            }
        }
    }
}
