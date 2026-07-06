using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DeadWalls.Tests
{
    /// <summary>
    /// CouncilComposer saf-mantik testleri. Katalog runtime CreateInstance ile kurulur
    /// (disk asset'ine bagimlilik yok) — composer'in determinizmi, butce dengesi,
    /// zincir filtreleri ve uretim-oranli olcekleme dogrulanir.
    /// </summary>
    public class CouncilComposerTests
    {
        private CouncilEventCatalogSO _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<CouncilEventCatalogSO>();
            _catalog.Atoms = new[]
            {
                MakeAtom("gain_resource", CouncilEffectKind.GainResource, a => { a.MinutesOfProduction = 1.5f; a.BudgetMinutes = 1.5f; a.ScarcityWeightMult = 3f; }),
                MakeAtom("gain_cache", CouncilEffectKind.GainResource, a => { a.MinutesOfProduction = 2.2f; a.BudgetMinutes = 2.2f; }),
                MakeAtom("pay_resource", CouncilEffectKind.PayResource, a => { a.MinutesOfProduction = 1.2f; a.BudgetMinutes = 1.2f; }),
                MakeAtom("boost_production", CouncilEffectKind.TempProductionBoost, a => { a.Rate = 0.25f; a.DurationDays = 2; a.BudgetMinutes = 2f; }),
                MakeAtom("penalty_production", CouncilEffectKind.TempProductionPenalty, a => { a.Rate = 0.2f; a.DurationDays = 1; a.BudgetMinutes = 1.2f; }),
                MakeAtom("gain_population", CouncilEffectKind.GainPopulation, a => { a.Rate = 6f; a.PerDay = 0.5f; a.BudgetMinutes = 2.2f; }),
                MakeAtom("free_archers", CouncilEffectKind.GainFreeArchers, a => { a.Rate = 1.4f; a.PerDay = 0.1f; a.BudgetMinutes = 2.5f; }),
                MakeAtom("heal_defense", CouncilEffectKind.HealDefensePercent, a => { a.Rate = 0.2f; a.BudgetMinutes = 1.8f; }),
                MakeAtom("calm_night", CouncilEffectKind.NextNightSpawnDelta, a => { a.Rate = 0.25f; a.BudgetMinutes = 1.5f; }),
                MakeAtom("wild_night", CouncilEffectKind.NextNightSpawnDelta, a => { a.Rate = 0.2f; a.BudgetMinutes = 1.5f; }),
            };
            _catalog.Templates = new[]
            {
                MakeTemplate("trade", CouncilContrastType.ResourceTrade, t =>
                {
                    t.OptionAAtomIds = new[] { "pay_resource" };
                    t.OptionBAtomIds = new[] { "gain_resource" };
                    t.BodyVariants = new[] { "Pay {PAY_N} {PAY_RES} for {GAIN_N} {GAIN_RES} on day {DAY}." };
                    t.OutcomeA = "Paid {PAY_N} {PAY_RES}, got {GAIN_N} {GAIN_RES}.";
                    t.OutcomeB = "Got {GAIN_N} {GAIN_RES} instead.";
                }),
                MakeTemplate("cache", CouncilContrastType.NowVsLater, t =>
                {
                    t.OptionAAtomIds = new[] { "gain_cache" };
                    t.OptionBAtomIds = new[] { "boost_production" };
                    t.BodyVariants = new[] { "{GAIN_N} {GAIN_RES} now or {BOOST_PCT}% later." };
                    t.OutcomeA = "+{GAIN_N} {GAIN_RES}.";
                    t.OutcomeB = "{BOOST_RES} +{BOOST_PCT}% for {BOOST_D} days.";
                }),
                MakeTemplate("refugees", CouncilContrastType.PopulationVsResource, t =>
                {
                    t.OptionAAtomIds = new[] { "gain_population" };
                    t.OptionBAtomIds = new[] { "gain_resource" };
                    t.SetsFlagOnA = "refugees_taken";
                    t.BodyVariants = new[] { "{POP_N} people at the gate." };
                    t.OutcomeA = "+{POP_N} people.";
                    t.OutcomeB = "+{GAIN_N} {GAIN_RES}.";
                }),
                MakeTemplate("veterans", CouncilContrastType.EconomyVsDefense, t =>
                {
                    t.OptionAAtomIds = new[] { "free_archers" };
                    t.OptionBAtomIds = new[] { "heal_defense" };
                    t.BodyVariants = new[] { "{ARCHER_N} archers for {PAY_N} {PAY_RES}." };
                    t.OutcomeA = "+{ARCHER_N} archers, -{PAY_N} {PAY_RES}.";
                    t.OutcomeB = "Defenses +{HEAL_PCT}%.";
                }),
                MakeTemplate("bonfires", CouncilContrastType.SafeVsRisky, t =>
                {
                    t.OptionAAtomIds = new[] { "calm_night" };
                    t.OptionBAtomIds = new[] { "wild_night" };
                    t.BodyVariants = new[] { "Risk the camps?" };
                    t.OutcomeA = "Night {NIGHT_PCT}% quieter.";
                    t.OutcomeB = "+{GAIN_N} {GAIN_RES}, night {NIGHT_PCT}% harder.";
                }),
                MakeTemplate("coldsnap", CouncilContrastType.PayOrSuffer, t =>
                {
                    t.OptionAAtomIds = new[] { "penalty_production" };
                    t.OptionBAtomIds = new[] { "pay_resource" };
                    t.BodyVariants = new[] { "Pay {PAY_RES} or suffer {PEN_PCT}%." };
                    t.OutcomeA = "{PEN_RES} -{PEN_PCT}% for {PEN_D} days.";
                    t.OutcomeB = "-{PAY_N} {PAY_RES}.";
                }),
                MakeTemplate("chain_child", CouncilContrastType.NowVsLater, t =>
                {
                    t.OptionAAtomIds = new[] { "gain_cache" };
                    t.OptionBAtomIds = new[] { "boost_production" };
                    t.RequiredFlags = new[] { "refugees_taken" };
                    t.ChainDelayDays = 2;
                    t.OneShot = true;
                    t.BodyVariants = new[] { "A craftsman among them." };
                    t.OutcomeA = "+{GAIN_N} {GAIN_RES}.";
                    t.OutcomeB = "{BOOST_RES} +{BOOST_PCT}% for {BOOST_D} days.";
                }),
            };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var atom in _catalog.Atoms) Object.DestroyImmediate(atom);
            foreach (var template in _catalog.Templates) Object.DestroyImmediate(template);
            Object.DestroyImmediate(_catalog);
        }

        [Test]
        public void Compose_AyniSeed_AyniSonucuVerir()
        {
            var context = MakeContext(day: 5);
            var first = CouncilComposer.Compose(_catalog, 12345u, context);
            var second = CouncilComposer.Compose(_catalog, 12345u, context);

            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            Assert.AreEqual(first.TemplateId, second.TemplateId);
            Assert.AreEqual(first.OptionA.Label, second.OptionA.Label);
            Assert.AreEqual(first.OptionB.Label, second.OptionB.Label);
        }

        [Test]
        public void Compose_500Seed_HicNullDegil_VeButcelerToleransta()
        {
            var context = MakeContext(day: 6);
            for (uint i = 1; i <= 500; i++)
            {
                uint seed = Unity.Mathematics.math.hash(new Unity.Mathematics.uint2(777u, i));
                var composed = CouncilComposer.Compose(_catalog, seed, context);
                Assert.IsNotNull(composed, $"seed {seed} null uretti");
                Assert.IsNotEmpty(composed.OptionA.Label);
                Assert.IsNotEmpty(composed.OptionB.Label);

                float a = Mathf.Max(0.1f, composed.OptionA.BudgetMinutes);
                float b = Mathf.Max(0.1f, composed.OptionB.BudgetMinutes);
                float ratio = Mathf.Max(a, b) / Mathf.Min(a, b);
                // BalanceBudgets toleransi 1.25; olcekleme 5'e-yuvarlama nedeniyle pay birakiyoruz
                Assert.LessOrEqual(ratio, 2.6f,
                    $"seed {seed} [{composed.TemplateId}] butce dengesiz: A={a:0.00} B={b:0.00}\nA: {composed.OptionA.Label}\nB: {composed.OptionB.Label}");
            }
        }

        [Test]
        public void MetinTokenlari_TumUretimlerde_Cozulur()
        {
            var context = MakeContext(day: 6);
            for (uint i = 1; i <= 300; i++)
            {
                uint seed = Unity.Mathematics.math.hash(new Unity.Mathematics.uint2(555u, i));
                var composed = CouncilComposer.Compose(_catalog, seed, context);
                Assert.IsNotNull(composed);
                AssertNoTokens(composed.TemplateId, "Body", composed.Body);
                AssertNoTokens(composed.TemplateId, "OutcomeA", composed.OutcomeA);
                AssertNoTokens(composed.TemplateId, "OutcomeB", composed.OutcomeB);
            }
        }

        private static void AssertNoTokens(string templateId, string field, string text)
        {
            Assert.IsNotEmpty(text, $"[{templateId}] {field} bos");
            Assert.IsFalse(text.Contains("{"),
                $"[{templateId}] {field} cozulmemis token iceriyor: \"{text}\"");
        }

        [Test]
        public void ZincirSablonu_FlagOlmadan_AslaCikmaz()
        {
            var context = MakeContext(day: 10); // flag yok
            for (uint i = 1; i <= 200; i++)
            {
                var composed = CouncilComposer.Compose(_catalog, i * 31u + 7u, context);
                Assert.IsNotNull(composed);
                Assert.AreNotEqual("chain_child", composed.TemplateId, "flag'siz zincir cocugu uretildi");
            }
        }

        [Test]
        public void ZincirSablonu_FlagVeGecikmeSonrasi_Cikabilir()
        {
            var context = MakeContext(day: 6);
            context.Flags["refugees_taken"] = 3; // gun 3'te secildi; delay 2 -> gun 5+ uygun

            bool seen = false;
            for (uint i = 1; i <= 300 && !seen; i++)
            {
                var composed = CouncilComposer.Compose(_catalog, i * 17u + 3u, context);
                if (composed != null && composed.TemplateId == "chain_child")
                    seen = true;
            }

            Assert.IsTrue(seen, "flag + gecikme saglandigi halde zincir cocugu 300 denemede hic cikmadi");
        }

        [Test]
        public void ZincirSablonu_GecikmeDolmadan_Cikmaz()
        {
            var context = MakeContext(day: 4);
            context.Flags["refugees_taken"] = 3; // delay 2 -> gun 5'ten once yasak

            for (uint i = 1; i <= 200; i++)
            {
                var composed = CouncilComposer.Compose(_catalog, i * 13u + 11u, context);
                Assert.IsNotNull(composed);
                Assert.AreNotEqual("chain_child", composed.TemplateId, "gecikme dolmadan zincir cocugu uretildi");
            }
        }

        [Test]
        public void KaynakMiktarlari_UretimleOlceklenir()
        {
            var slow = MakeContext(day: 5);
            var fast = MakeContext(day: 5);
            fast.WoodPerMin *= 3f; fast.StonePerMin *= 3f; fast.IronPerMin *= 3f; fast.FoodPerMin *= 3f;

            // Ayni seed'de ayni sablon/atom/band secilir (context agirliklari ayni kaldigi surece);
            // kaynak miktari uretimle dogru orantili buyumeli.
            int comparisons = 0;
            for (uint i = 1; i <= 200 && comparisons < 20; i++)
            {
                uint seed = Unity.Mathematics.math.hash(new Unity.Mathematics.uint2(42u, i));
                var slowEvent = CouncilComposer.Compose(_catalog, seed, slow);
                var fastEvent = CouncilComposer.Compose(_catalog, seed, fast);
                if (slowEvent == null || fastEvent == null || slowEvent.TemplateId != fastEvent.TemplateId)
                    continue;

                int slowAmount = FirstResourceAmount(slowEvent);
                int fastAmount = FirstResourceAmount(fastEvent);
                if (slowAmount <= 0 || fastAmount <= 0)
                    continue;

                comparisons++;
                float scale = fastAmount / (float)slowAmount;
                Assert.That(scale, Is.InRange(2.0f, 4.5f),
                    $"seed {seed} [{slowEvent.TemplateId}]: 3x uretimde miktar olcegi {scale:0.00} (yaklasik 3 beklenir)");
            }

            Assert.GreaterOrEqual(comparisons, 5, "yeterli karsilastirilabilir ornek uretilemedi");
        }

        // ---------------------------------------------------------------
        // Yardimcilar
        // ---------------------------------------------------------------
        private static CouncilEffectAtomSO MakeAtom(string id, CouncilEffectKind kind,
            System.Action<CouncilEffectAtomSO> configure)
        {
            var atom = ScriptableObject.CreateInstance<CouncilEffectAtomSO>();
            atom.Id = id;
            atom.Kind = kind;
            configure?.Invoke(atom);
            return atom;
        }

        private static CouncilTemplateSO MakeTemplate(string id, CouncilContrastType contrast,
            System.Action<CouncilTemplateSO> configure)
        {
            var template = ScriptableObject.CreateInstance<CouncilTemplateSO>();
            template.Id = id;
            template.Title = id.ToUpperInvariant();
            template.Contrast = contrast;
            configure?.Invoke(template);
            return template;
        }

        private static CouncilContext MakeContext(int day)
        {
            return new CouncilContext
            {
                Day = day,
                Wood = 400, Stone = 150, Iron = 60, Food = 300,
                WoodPerMin = 160f, StonePerMin = 55f, IronPerMin = 30f, FoodPerMin = 105f,
                Defense01 = 0.9f,
                Flags = new Dictionary<string, int>(),
                RecentTemplateIds = new List<string>(),
                UsedOneShotTemplateIds = new HashSet<string>(),
            };
        }

        private static int FirstResourceAmount(ComposedCouncilEvent composed)
        {
            foreach (var effect in composed.OptionA.Effects)
            {
                if (effect.Kind == CouncilEffectKind.GainResource || effect.Kind == CouncilEffectKind.PayResource)
                    return effect.Amount;
            }
            foreach (var effect in composed.OptionB.Effects)
            {
                if (effect.Kind == CouncilEffectKind.GainResource || effect.Kind == CouncilEffectKind.PayResource)
                    return effect.Amount;
            }
            return 0;
        }
    }
}
