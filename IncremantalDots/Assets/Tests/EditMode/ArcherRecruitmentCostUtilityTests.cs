using NUnit.Framework;
using UnityEditor;

namespace DeadWalls.Tests
{
    public class ArcherRecruitmentCostUtilityTests
    {
        [Test]
        public void GetScaledCost_UsesTargetTypeCountCurve()
        {
            var baseCost = new ResourceCost(100, 50, 25, 10);

            ResourceCost first = ArcherRecruitmentCostUtility.GetScaledCost(
                baseCost, 0, 25, 2f);
            ResourceCost afterTwentyFive = ArcherRecruitmentCostUtility.GetScaledCost(
                baseCost, 25, 25, 2f);

            Assert.That(first.Wood, Is.EqualTo(100));
            Assert.That(first.Stone, Is.EqualTo(50));
            Assert.That(first.Iron, Is.EqualTo(25));
            Assert.That(first.Food, Is.EqualTo(10));
            Assert.That(afterTwentyFive.Wood, Is.EqualTo(400));
            Assert.That(afterTwentyFive.Stone, Is.EqualTo(200));
            Assert.That(afterTwentyFive.Iron, Is.EqualTo(100));
            Assert.That(afterTwentyFive.Food, Is.EqualTo(40));
        }

        [Test]
        public void GetScaledCost_SanitizesInvalidInputsAndSaturatesOverflow()
        {
            ResourceCost sanitized = ArcherRecruitmentCostUtility.GetScaledCost(
                new ResourceCost(-10, 20, 0, 5), -100, 0, float.NaN);
            ResourceCost saturated = ArcherRecruitmentCostUtility.GetScaledCost(
                new ResourceCost(int.MaxValue, 1, 0, 0), 1000, 1, 10f);

            Assert.That(sanitized.Wood, Is.Zero);
            Assert.That(sanitized.Stone, Is.EqualTo(20));
            Assert.That(sanitized.Iron, Is.Zero);
            Assert.That(sanitized.Food, Is.EqualTo(5));
            Assert.That(saturated.Wood, Is.EqualTo(int.MaxValue));
            Assert.That(saturated.Stone, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void DefaultCatalog_DefinitionsOwnCanonicalCombatAndRecruitmentTuning()
        {
            const string catalogPath =
                "Assets/ScriptableObject/MobileCastle/Archers/ArcherRecruitmentCatalog.asset";
            ArcherRecruitmentCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<ArcherRecruitmentCatalogSO>(catalogPath);

            Assert.That(catalog, Is.Not.Null);
            ArcherDefinitionSO basic = catalog.GetDefinition(ArcherType.Basic);
            ArcherDefinitionSO rapid = catalog.GetDefinition(ArcherType.Rapid);
            ArcherDefinitionSO frost = catalog.GetDefinition(ArcherType.Frost);

            AssertDefinition(basic, 10f, 1.5f, 15f,
                new ResourceCost(45, 0, 0, 20), ResourceCost.Zero);
            AssertDefinition(rapid, 6f, 3f, 14f,
                new ResourceCost(55, 0, 35, 20), new ResourceCost(55, 0, 35, 0));
            AssertDefinition(frost, 5f, 1.2f, 14f,
                new ResourceCost(45, 55, 25, 0), new ResourceCost(45, 55, 25, 0));
        }

        [Test]
        public void ArrowDrainContract_IsOnePerSuccessfulProjectileRent()
        {
            Assert.That(ArcherShootSystem.ArrowCostPerSuccessfulProjectileRent, Is.EqualTo(1));
        }

        private static void AssertDefinition(ArcherDefinitionSO definition,
            float damage, float fireRate, float range,
            ResourceCost buyCost, ResourceCost retrainCost)
        {
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.Damage, Is.EqualTo(damage));
            Assert.That(definition.FireRate, Is.EqualTo(fireRate));
            Assert.That(definition.Range, Is.EqualTo(range));
            Assert.That(definition.PopulationCost, Is.EqualTo(1));
            Assert.That(definition.CostGrowthInterval,
                Is.EqualTo(ArcherRecruitmentCostUtility.DefaultGrowthInterval));
            Assert.That(definition.CostGrowthExponent,
                Is.EqualTo(ArcherRecruitmentCostUtility.DefaultGrowthExponent));
            AssertCost(definition.BuyCost, buyCost);
            AssertCost(definition.RetrainCost, retrainCost);

            ResourceCost scaledBuy = ArcherRecruitmentCostUtility.GetScaledCost(
                definition.BuyCost,
                definition.CostGrowthInterval,
                definition.CostGrowthInterval,
                definition.CostGrowthExponent);
            Assert.That(scaledBuy.Wood,
                Is.EqualTo(definition.BuyCost.Wood <= 0 ? 0 : definition.BuyCost.Wood * 4));
            Assert.That(scaledBuy.Stone,
                Is.EqualTo(definition.BuyCost.Stone <= 0 ? 0 : definition.BuyCost.Stone * 4));
            Assert.That(scaledBuy.Iron,
                Is.EqualTo(definition.BuyCost.Iron <= 0 ? 0 : definition.BuyCost.Iron * 4));
            Assert.That(scaledBuy.Food,
                Is.EqualTo(definition.BuyCost.Food <= 0 ? 0 : definition.BuyCost.Food * 4));
        }

        private static void AssertCost(ResourceCost actual, ResourceCost expected)
        {
            Assert.That(actual.Wood, Is.EqualTo(expected.Wood));
            Assert.That(actual.Stone, Is.EqualTo(expected.Stone));
            Assert.That(actual.Iron, Is.EqualTo(expected.Iron));
            Assert.That(actual.Food, Is.EqualTo(expected.Food));
        }
    }
}
