using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class CouncilOptionPresentationUtilityTests
    {
        [Test]
        public void PopulationQuote_ShowsExactPeopleAndOneTimeFoodCost()
        {
            ComposedCouncilOption option = MakeOption("Take them in  —  stale summary",
                new ComposedCouncilEffect
                {
                    Kind = CouncilEffectKind.GainPopulation,
                    Amount = 4
                });
            CouncilOptionPresentationContext context = MakeContext();

            CouncilOptionPresentation quote = CouncilOptionPresentationUtility.Build(option, context);

            Assert.That(quote.CanApplyExactly, Is.True);
            StringAssert.Contains("<b>Take them in</b>", quote.RichText);
            StringAssert.Contains("+4 PEOPLE", quote.RichText);
            StringAssert.Contains("-8 FOOD", quote.RichText);
            StringAssert.DoesNotContain("stale summary", quote.RichText);
        }

        [Test]
        public void PopulationQuote_WhenExactAmountCannotFit_DisablesAndExplainsShortfall()
        {
            ComposedCouncilOption option = MakeOption("Open the gate",
                new ComposedCouncilEffect
                {
                    Kind = CouncilEffectKind.GainPopulation,
                    Amount = 4
                });
            CouncilOptionPresentationContext context = MakeContext();
            context.Resources.Food = 5;

            CouncilOptionPresentation quote = CouncilOptionPresentationUtility.Build(option, context);

            Assert.That(quote.CanApplyExactly, Is.False);
            Assert.That(quote.UnavailableReason, Is.EqualTo("NEED 3 MORE FOOD"));
            StringAssert.Contains("-8 FOOD", quote.RichText);
            StringAssert.Contains("NEED 3 MORE FOOD", quote.RichText);
        }

        [Test]
        public void FreeArcherQuote_ShowsIdlePopulationCostAndCommonCapShortfall()
        {
            ComposedCouncilOption option = MakeOption("Arm them",
                new ComposedCouncilEffect
                {
                    Kind = CouncilEffectKind.GainFreeArchers,
                    Amount = 3
                });
            CouncilOptionPresentationContext context = MakeContext();
            context.TotalArchers = 998;
            context.IdlePopulation = 10;

            CouncilOptionPresentation quote = CouncilOptionPresentationUtility.Build(option, context);

            Assert.That(quote.CanApplyExactly, Is.False);
            StringAssert.Contains("+3 BASIC ARCHERS", quote.RichText);
            StringAssert.Contains("-3 IDLE PEOPLE", quote.RichText);
            Assert.That(quote.UnavailableReason, Is.EqualTo("NEED 1 MORE ARMY SLOTS"));
        }

        [Test]
        public void WallAndNightQuote_ShowActualClampedResult()
        {
            ComposedCouncilOption option = MakeOption("Hold the line",
                new ComposedCouncilEffect
                {
                    Kind = CouncilEffectKind.HealDefensePercent,
                    Rate = 0.25f
                },
                new ComposedCouncilEffect
                {
                    Kind = CouncilEffectKind.NextNightSpawnDelta,
                    Rate = 100f
                });
            CouncilOptionPresentationContext context = MakeContext();
            context.WallCurrentHp = 90f;
            context.WallMaxHp = 100f;

            CouncilOptionPresentation quote = CouncilOptionPresentationUtility.Build(option, context);

            Assert.That(quote.CanApplyExactly, Is.True);
            StringAssert.Contains("+10 WALL HP (25% MAX)", quote.RichText);
            StringAssert.Contains("NIGHT HORDE +100%", quote.RichText);
        }

        [Test]
        public void DecisionWindow_UsesAuthoritativeCycleProgressAndCeilCountdown()
        {
            ContinuousSiegeCycleData cycle = new ContinuousSiegeCycleData
            {
                Enabled = true,
                Phase = SiegeCyclePhase.Dawn,
                DawnDuration = 5f,
                DayDuration = 30f,
                PhaseProgress01 = 0.4f,
            };

            Assert.That(CouncilDecisionWindowUtility.GetRemainingSeconds(cycle), Is.EqualTo(33f).Within(0.001f));

            cycle.Phase = SiegeCyclePhase.Day;
            cycle.PhaseProgress01 = 0.25f;
            Assert.That(CouncilDecisionWindowUtility.GetRemainingSeconds(cycle), Is.EqualTo(22.5f).Within(0.001f));
            Assert.That(CouncilDecisionWindowUtility.FormatCountdown(22.01f), Is.EqualTo("DECIDE  23s"));
        }

        [Test]
        public void GeneratedHudPrefab_ContainsNumericalDecisionTimerText()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab");
            Assert.That(prefab, Is.Not.Null);

            Transform timer = FindChildRecursive(prefab.transform, "CouncilTimerText");
            Assert.That(timer, Is.Not.Null);
            Assert.That(timer.gameObject.activeSelf, Is.True);

            Component textComponent = timer.GetComponent("TextMeshProUGUI");
            Assert.That(textComponent, Is.Not.Null);
            SerializedObject serializedText = new SerializedObject(textComponent);
            Assert.That(serializedText.FindProperty("m_fontAsset").objectReferenceValue, Is.Not.Null,
                "Council timer font asset olmadan render edilemez.");
        }

        private static ComposedCouncilOption MakeOption(
            string label,
            params ComposedCouncilEffect[] effects)
        {
            ComposedCouncilOption option = new ComposedCouncilOption { Label = label };
            option.Effects.AddRange(effects);
            return option;
        }

        private static CouncilOptionPresentationContext MakeContext()
        {
            return new CouncilOptionPresentationContext
            {
                RuntimeReady = true,
                PopulationRulesReady = true,
                Resources = new ResourceData
                {
                    Wood = 100,
                    Stone = 100,
                    Iron = 100,
                    Food = 20,
                },
                CurrentPopulation = 10,
                TotalBedCapacity = 20,
                FoodCostPerArrival = 2,
                TotalArchers = 4,
                IdlePopulation = 6,
                WallCurrentHp = 50f,
                WallMaxHp = 100f,
            };
        }

        private static Transform FindChildRecursive(Transform root, string objectName)
        {
            foreach (Transform child in root)
            {
                if (child.name == objectName)
                    return child;

                Transform nested = FindChildRecursive(child, objectName);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}
