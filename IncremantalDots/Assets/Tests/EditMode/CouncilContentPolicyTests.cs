using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class CouncilContentPolicyTests
    {
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();
        private CouncilEventCatalogSO _catalog;
        private CouncilTemplateSO _template;
        private CouncilEffectAtomSO _gain;
        private CouncilEffectAtomSO _boost;

        [SetUp]
        public void SetUp()
        {
            _gain = CreateAtom("gain", CouncilEffectKind.GainResource);
            _boost = CreateAtom("boost", CouncilEffectKind.TempProductionBoost);
            _template = CreateTemplate("boundary_template", CouncilContrastType.NowVsLater);
            _template.OptionAAtomIds = new[] { _gain.Id };
            _template.OptionBAtomIds = new[] { _boost.Id };

            _catalog = ScriptableObject.CreateInstance<CouncilEventCatalogSO>();
            _catalog.Atoms = new[] { _gain, _boost };
            _catalog.Templates = new[] { _template };
            _catalog.RecentTemplateMemory = 1;
            _created.Add(_catalog);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(_created[i]);
            _created.Clear();
        }

        [Test]
        public void RoleWhitelist_YalnizCouncilRunDecisionDomainlariniIcerir()
        {
            CouncilEffectKind[] allowed = Enum.GetValues(typeof(CouncilEffectKind))
                .Cast<CouncilEffectKind>()
                .Where(CouncilContentPolicy.IsCouncilOwnedEffectKind)
                .ToArray();

            CollectionAssert.AreEquivalent(new[]
            {
                CouncilEffectKind.GainResource,
                CouncilEffectKind.PayResource,
                CouncilEffectKind.TempProductionBoost,
                CouncilEffectKind.TempProductionPenalty,
                CouncilEffectKind.WorkerCapBonus,
                CouncilEffectKind.GainPopulation,
                CouncilEffectKind.GainFreeArchers,
                CouncilEffectKind.HealDefensePercent,
                CouncilEffectKind.NextNightSpawnDelta,
            }, allowed);
            Assert.That(CouncilContentPolicy.IsCouncilOwnedEffectKind(CouncilEffectKind.None), Is.False);
            Assert.That(CouncilContentPolicy.IsCouncilOwnedEffectKind((CouncilEffectKind)999), Is.False);
        }

        [Test]
        public void CatalogGate_BilinmeyenRoleVeYanlisOptionRecetesiniReddeder()
        {
            _boost.Kind = (CouncilEffectKind)999;
            Assert.That(_catalog.ValidateCatalog(), Has.Some.Contains("role ownership"));

            _boost.Kind = CouncilEffectKind.TempProductionBoost;
            _template.OptionBAtomIds = new[] { _gain.Id };
            Assert.That(_catalog.ValidateCatalog(), Has.Some.Contains("OptionB Council content recetesi"));
        }

        [Test]
        public void ComposedEventGate_CatalogTemplateVeOptionRecetesiniZorunluTutar()
        {
            ComposedCouncilEvent composed = CouncilComposer.Compose(
                _catalog,
                443u,
                new CouncilContext
                {
                    Day = 3,
                    Wood = 100,
                    Stone = 100,
                    Iron = 100,
                    Food = 100,
                    WoodPerMin = 10f,
                    StonePerMin = 10f,
                    IronPerMin = 10f,
                    FoodPerMin = 10f,
                    Defense01 = 1f,
                    Flags = new Dictionary<string, int>(),
                    RecentTemplateIds = new List<string>(),
                    UsedOneShotTemplateIds = new HashSet<string>(),
                });

            Assert.That(composed, Is.Not.Null);
            Assert.That(CouncilContentPolicy.TryValidateComposedEvent(
                _catalog, composed, out string validProblem), Is.True, validProblem);

            composed.TemplateId = "catalog_disi";
            Assert.That(CouncilContentPolicy.TryValidateComposedEvent(
                _catalog, composed, out string templateProblem), Is.False);
            Assert.That(templateProblem, Does.Contain("catalog disi template"));

            composed.TemplateId = _template.Id;
            ComposedCouncilEffect invalid = composed.OptionA.Effects[0];
            invalid.Kind = CouncilEffectKind.HealDefensePercent;
            composed.OptionA.Effects[0] = invalid;
            Assert.That(CouncilContentPolicy.TryValidateComposedEvent(
                _catalog, composed, out string recipeProblem), Is.False);
            Assert.That(recipeProblem, Does.Contain("content recetesi disinda"));
        }

        [Test]
        public void ProductionCatalog_RoleVeContentGateindenTemizGecer()
        {
            const string path = "Assets/ScriptableObject/MobileCastle/Council/CouncilEventCatalog.asset";
            CouncilEventCatalogSO production = AssetDatabase.LoadAssetAtPath<CouncilEventCatalogSO>(path);

            Assert.That(production, Is.Not.Null);
            Assert.That(production.TryValidateRuntimeContent(out string problem), Is.True, problem);
        }

        private CouncilEffectAtomSO CreateAtom(string id, CouncilEffectKind kind)
        {
            CouncilEffectAtomSO atom = ScriptableObject.CreateInstance<CouncilEffectAtomSO>();
            atom.Id = id;
            atom.Kind = kind;
            atom.BudgetMinutes = 1f;
            _created.Add(atom);
            return atom;
        }

        private CouncilTemplateSO CreateTemplate(string id, CouncilContrastType contrast)
        {
            CouncilTemplateSO template = ScriptableObject.CreateInstance<CouncilTemplateSO>();
            template.Id = id;
            template.Title = id;
            template.Body = id;
            template.OutcomeA = id;
            template.OutcomeB = id;
            template.Contrast = contrast;
            _created.Add(template);
            return template;
        }
    }
}
