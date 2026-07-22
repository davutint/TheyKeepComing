using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
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
                    t.ForbiddenFlags = new[] { "refugees_taken" };
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
            _catalog.CuratedChains = new[]
            {
                new CouncilCuratedChain
                {
                    SourceTemplateId = "refugees",
                    SourceBranch = CouncilChoiceBranch.OptionA,
                    Flag = "refugees_taken",
                    TargetTemplateId = "chain_child",
                },
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
        public void EffectBands_DefaultsPreserveLegacyThirtyFiveFiftyFifteenDistribution()
        {
            CouncilEffectBandSettings settings = _catalog.EffectBands;

            Assert.That(settings.TryValidate(out string problem), Is.True, problem);
            Assert.That(CouncilComposer.ResolveEffectBand(settings, 0f), Is.EqualTo(0.7f));
            Assert.That(CouncilComposer.ResolveEffectBand(settings, 0.34999f), Is.EqualTo(0.7f));
            Assert.That(CouncilComposer.ResolveEffectBand(settings, 0.35f), Is.EqualTo(1f));
            Assert.That(CouncilComposer.ResolveEffectBand(settings, 0.84999f), Is.EqualTo(1f));
            Assert.That(CouncilComposer.ResolveEffectBand(settings, 0.85f), Is.EqualTo(1.4f));
            Assert.That(CouncilComposer.ResolveEffectBand(settings, 1f), Is.EqualTo(1.4f));
        }

        [Test]
        public void EffectBands_CustomWeightsAndMultipliersDriveCanonicalResolver()
        {
            CouncilEffectBandSettings settings = _catalog.EffectBands;
            settings.SmallMultiplier = 0.5f;
            settings.FairMultiplier = 1.1f;
            settings.GenerousMultiplier = 2f;
            settings.SmallWeight = 1f;
            settings.FairWeight = 2f;
            settings.GenerousWeight = 1f;

            Assert.That(CouncilComposer.ResolveEffectBand(settings, 0.24999f), Is.EqualTo(0.5f));
            Assert.That(CouncilComposer.ResolveEffectBand(settings, 0.25f), Is.EqualTo(1.1f));
            Assert.That(CouncilComposer.ResolveEffectBand(settings, 0.74999f), Is.EqualTo(1.1f));
            Assert.That(CouncilComposer.ResolveEffectBand(settings, 0.75f), Is.EqualTo(2f));
        }

        [Test]
        public void EffectBands_InvalidOrderFailsCatalogAndComposeClosed()
        {
            _catalog.EffectBands.FairMultiplier = 0.1f;

            Assert.That(_catalog.ValidateCatalog(), Has.Some.Contains("Small <= Fair <= Generous"));
            Assert.That(CouncilComposer.Compose(_catalog, 17u, MakeContext(6)), Is.Null);
        }

        [Test]
        public void RecentMemory_TrimKeepsNewestEntriesAndEnforcesMinimumOne()
        {
            var recent = new List<string> { "a", "b", "c", "d" };

            CouncilRecentTemplateMemory.TrimInPlace(recent, 2);
            Assert.That(recent, Is.EqualTo(new[] { "c", "d" }));

            CouncilRecentTemplateMemory.TrimInPlace(recent, 0);
            Assert.That(recent, Is.EqualTo(new[] { "d" }));
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

        [Test]
        public void KaynakSecimi_StokDakikasinaGoreEnKitKaynagiKullanir()
        {
            SetOnlyTemplateActive("cache");
            var context = MakeContext(day: 6);
            context.Wood = 800;
            context.Stone = 800;
            context.Iron = 800;
            context.Food = 1;
            context.WoodPerMin = 100f;
            context.StonePerMin = 100f;
            context.IronPerMin = 100f;
            context.FoodPerMin = 100f;

            ComposedCouncilEvent composed = CouncilComposer.Compose(_catalog, 9182u, context);

            Assert.That(composed, Is.Not.Null);
            Assert.That(composed.TemplateId, Is.EqualTo("cache"));
            Assert.That(composed.OptionA.Effects[0].Resource, Is.EqualTo(EconomyFocusType.Food));
            Assert.That(composed.OptionB.Effects[0].Resource, Is.EqualTo(EconomyFocusType.Food));
        }

        [Test]
        public void DusukWall_BTarafiHealAtomuylaDefenseTemplateAgirliginiArtirir()
        {
            SetOnlyTemplatesActive("cache", "veterans");
            FindAtom("free_archers").LowDefenseWeightMult = 1f;
            FindAtom("heal_defense").LowDefenseWeightMult = 8f;

            var healthy = MakeContext(day: 6);
            healthy.Defense01 = 0.9f;
            var damaged = MakeContext(day: 6);
            damaged.Defense01 = 0.2f;

            int healthyDefenseCards = CountTemplate("veterans", healthy, 800);
            int damagedDefenseCards = CountTemplate("veterans", damaged, 800);

            Assert.That(damagedDefenseCards, Is.GreaterThan(healthyDefenseCards + 200),
                $"Wall context B-tarafi heal atomuna yansimadi: healthy={healthyDefenseCards}, damaged={damagedDefenseCards}");
        }

        [Test]
        public void RecentTemplate_AlternatifVarkenHavuzdanTamamenCikarilir()
        {
            var context = MakeContext(day: 6);
            context.RecentTemplateIds.Add("trade");

            for (uint seed = 1; seed <= 400; seed++)
            {
                ComposedCouncilEvent composed = CouncilComposer.Compose(_catalog, seed, context);
                Assert.That(composed, Is.Not.Null);
                Assert.That(composed.TemplateId, Is.Not.EqualTo("trade"),
                    $"Alternatif varken recent template tekrarlandi; seed={seed}.");
            }
        }

        [Test]
        public void RecentTemplate_TekUygunAdayIseScheduledFallbackOlarakKullanilir()
        {
            SetOnlyTemplateActive("trade");
            var context = MakeContext(day: 6);
            context.RecentTemplateIds.Add("trade");

            ComposedCouncilEvent composed = CouncilComposer.Compose(_catalog, 77u, context);

            Assert.That(composed, Is.Not.Null);
            Assert.That(composed.TemplateId, Is.EqualTo("trade"));
        }

        [Test]
        public void ChainFlag_ContextteOlsaBileCuratedContractYoksaTargetAcilmaz()
        {
            SetOnlyTemplateActive("chain_child");
            _catalog.CuratedChains = new CouncilCuratedChain[0];
            var context = MakeContext(day: 6);
            context.Flags["refugees_taken"] = 3;

            Assert.That(CouncilComposer.Compose(_catalog, 42u, context), Is.Null);
        }

        [Test]
        public void ChainSource_TetikleyenSecimeKadarUygun_FlagSonraKosudaEmekliOlur()
        {
            SetOnlyTemplateActive("refugees");
            var context = MakeContext(day: 6);

            ComposedCouncilEvent beforeChoice = CouncilComposer.Compose(_catalog, 42u, context);
            Assert.That(beforeChoice, Is.Not.Null);
            Assert.That(beforeChoice.TemplateId, Is.EqualTo("refugees"));

            context.Flags["refugees_taken"] = 6;
            Assert.That(CouncilComposer.Compose(_catalog, 42u, context), Is.Null);
        }

        [Test]
        public void ValidateCatalog_CuratedSourceFlagTargetContractiniDogrular()
        {
            Assert.That(_catalog.ValidateCatalog(), Is.Empty);

            _catalog.CuratedChains = new CouncilCuratedChain[0];
            List<string> problems = _catalog.ValidateCatalog();

            Assert.That(problems, Has.Some.Contains("onaysiz Council chain"));
        }

        [Test]
        public void ProductionCatalog_YalnizIkiMevcutCuratedChainiTasirVeValidedir()
        {
            const string path = "Assets/ScriptableObject/MobileCastle/Council/CouncilEventCatalog.asset";
            CouncilEventCatalogSO production = AssetDatabase.LoadAssetAtPath<CouncilEventCatalogSO>(path);

            Assert.That(production, Is.Not.Null);
            Assert.That(production.CuratedChains, Has.Length.EqualTo(2));
            Assert.That(production.IsApprovedChainSource(
                "refugees_at_gate", true, "refugees_taken"), Is.True);
            Assert.That(production.IsApprovedChainSourceRetirement(
                "refugees_at_gate", "refugees_taken"), Is.True);
            Assert.That(production.IsApprovedChainConstraint(
                "among_the_refugees", "refugees_taken"), Is.True);
            Assert.That(production.IsApprovedChainSource(
                "merchant_caravan", true, "traded_with_merchant"), Is.True);
            Assert.That(production.IsApprovedChainSourceRetirement(
                "merchant_caravan", "traded_with_merchant"), Is.True);
            Assert.That(production.IsApprovedChainConstraint(
                "an_old_friend", "traded_with_merchant"), Is.True);
            Assert.That(production.GetTemplate("refugees_at_gate").ForbiddenFlags,
                Does.Contain("refugees_taken"));
            Assert.That(production.GetTemplate("merchant_caravan").ForbiddenFlags,
                Does.Contain("traded_with_merchant"));
            Assert.That(production.ValidateCatalog(), Is.Empty);
        }

        [Test]
        public void ProductionCatalog_LaunchTemplateAcilisGunleriniKatmanliTutar()
        {
            const string path = "Assets/ScriptableObject/MobileCastle/Council/CouncilEventCatalog.asset";
            CouncilEventCatalogSO production = AssetDatabase.LoadAssetAtPath<CouncilEventCatalogSO>(path);
            var expectedDays = new Dictionary<string, int>
            {
                { "abandoned_cache", 1 },
                { "merchant_caravan", 1 },
                { "quarry_crew", 1 },
                { "refugees_at_gate", 6 },
                { "wandering_veterans", 6 },
                { "cold_snap", 6 },
                { "strange_bonfires", 9 },
            };

            Assert.That(production, Is.Not.Null);
            foreach (KeyValuePair<string, int> expected in expectedDays)
            {
                CouncilTemplateSO template = production.GetTemplate(expected.Key);
                Assert.That(template, Is.Not.Null, $"Production template eksik: {expected.Key}");
                Assert.That(template.MinDay, Is.EqualTo(expected.Value),
                    $"{expected.Key} launch staging gununden sapti.");
            }
        }

        [Test]
        public void ProductionCatalog_LaunchTemplateButceleriVeTokenlariOnayliSinirdaKalir()
        {
            const string path = "Assets/ScriptableObject/MobileCastle/Council/CouncilEventCatalog.asset";
            CouncilEventCatalogSO production = AssetDatabase.LoadAssetAtPath<CouncilEventCatalogSO>(path);
            Assert.That(production, Is.Not.Null);

            int[] sampleDays = { 1, 3, 12, 30 };
            foreach (CouncilTemplateSO template in production.Templates)
            {
                Assert.That(template.BodyVariants, Has.Length.GreaterThanOrEqualTo(2),
                    $"[{template.Id}] launch metni en az iki authored govde varyanti tasimali.");
                foreach (int sampleDay in sampleDays)
                {
                    int day = Mathf.Max(template.MinDay, sampleDay);
                    if (template.RequiredFlags != null && template.RequiredFlags.Length > 0)
                        day = Mathf.Max(day, 1 + template.ChainDelayDays);
                    for (uint seed = 1; seed <= 200; seed++)
                    {
                        CouncilContext context = MakeContext(day);
                        foreach (CouncilTemplateSO other in production.Templates)
                        {
                            if (other != null && other.Id != template.Id)
                                context.RecentTemplateIds.Add(other.Id);
                        }

                        if (template.RequiredFlags != null)
                        {
                            foreach (string flag in template.RequiredFlags)
                            {
                                if (!string.IsNullOrEmpty(flag))
                                    context.Flags[flag] = Mathf.Max(1, day - template.ChainDelayDays);
                            }
                        }

                        ComposedCouncilEvent composed = CouncilComposer.Compose(
                            production,
                            Unity.Mathematics.math.hash(new Unity.Mathematics.uint2(seed, (uint)day)),
                            context);

                        Assert.That(composed, Is.Not.Null,
                            $"[{template.Id}] Day {day}, seed {seed}: compose null.");
                        Assert.That(composed.TemplateId, Is.EqualTo(template.Id),
                            $"[{template.Id}] isolation recent-memory kontrati bozuldu.");

                        float a = Mathf.Abs(composed.OptionA.BudgetMinutes);
                        float b = Mathf.Abs(composed.OptionB.BudgetMinutes);
                        float ratio = Mathf.Max(a, b) / Mathf.Max(0.1f, Mathf.Min(a, b));
                        Assert.That(ratio, Is.LessThanOrEqualTo(1.2501f),
                            $"[{template.Id}] Day {day}, seed {seed}: budget ratio {ratio:0.###}.");

                        AssertNoTokens(composed.TemplateId, "Body", composed.Body);
                        AssertNoTokens(composed.TemplateId, "OutcomeA", composed.OutcomeA);
                        AssertNoTokens(composed.TemplateId, "OutcomeB", composed.OutcomeB);
                        Assert.That(CouncilContentPolicy.TryValidateComposedEvent(
                            production, composed, out string problem), Is.True, problem);
                    }
                }
            }
        }

        [Test]
        public void ProductionCatalog_EveryDayHasAtLeastOneValidCard()
        {
            const string path = "Assets/ScriptableObject/MobileCastle/Council/CouncilEventCatalog.asset";
            CouncilEventCatalogSO production = AssetDatabase.LoadAssetAtPath<CouncilEventCatalogSO>(path);
            Assert.That(production, Is.Not.Null);

            for (int day = 1; day <= 30; day++)
            {
                for (uint seed = 1; seed <= 64; seed++)
                {
                    ComposedCouncilEvent composed = CouncilComposer.Compose(
                        production,
                        Unity.Mathematics.math.hash(new Unity.Mathematics.uint2(seed, (uint)day)),
                        MakeContext(day));
                    Assert.That(composed, Is.Not.Null, $"Day {day}, seed {seed}: daily Council null.");
                    Assert.That(CouncilContentPolicy.TryValidateComposedEvent(
                        production, composed, out string problem), Is.True, problem);
                }
            }
        }

        [Test]
        public void ProductionCatalog_CapBonusAtomuDormantVeLaunchReceteleriDisindaKalir()
        {
            const string path = "Assets/ScriptableObject/MobileCastle/Council/CouncilEventCatalog.asset";
            CouncilEventCatalogSO production = AssetDatabase.LoadAssetAtPath<CouncilEventCatalogSO>(path);
            Assert.That(production, Is.Not.Null);
            Assert.That(production.GetAtom("cap_bonus"), Is.Not.Null,
                "Legacy atom compatibility icin asset korunmali.");
            Assert.That(CouncilContentPolicy.IsReferencedAtomAllowed(
                CouncilContrastType.NowVsLater, false, CouncilEffectKind.WorkerCapBonus), Is.False);

            foreach (CouncilTemplateSO template in production.Templates)
            {
                CollectionAssert.DoesNotContain(template.OptionAAtomIds, "cap_bonus", template.Id);
                CollectionAssert.DoesNotContain(template.OptionBAtomIds, "cap_bonus", template.Id);
            }
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

        private void SetOnlyTemplateActive(string templateId)
        {
            SetOnlyTemplatesActive(templateId);
        }

        private void SetOnlyTemplatesActive(params string[] templateIds)
        {
            var active = new HashSet<string>(templateIds);
            foreach (CouncilTemplateSO template in _catalog.Templates)
                template.BaseWeight = active.Contains(template.Id) ? 1f : 0f;
        }

        private CouncilEffectAtomSO FindAtom(string atomId)
        {
            foreach (CouncilEffectAtomSO atom in _catalog.Atoms)
            {
                if (atom.Id == atomId)
                    return atom;
            }

            Assert.Fail($"Test atomu bulunamadi: {atomId}");
            return null;
        }

        private int CountTemplate(string templateId, CouncilContext context, int sampleCount)
        {
            int count = 0;
            for (uint seed = 1; seed <= sampleCount; seed++)
            {
                ComposedCouncilEvent composed = CouncilComposer.Compose(_catalog, seed, context);
                if (composed != null && composed.TemplateId == templateId)
                    count++;
            }

            return count;
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
