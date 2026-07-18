using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeadWalls.Tests
{
    public class MetaIdentityPresentationTests
    {
        private const string CatalogPath =
            "Assets/ScriptableObject/MobileCastle/Meta/MetaUpgradeCatalog.asset";
        private const string IconPath =
            "Assets/Art/Generated/Meta/last_embers_icon.png";
        private const string ScenePath = "Assets/Scenes/NewGameScene.unity";

        [Test]
        public void ProductionCatalog_UsesDistinctLastEmbersIdentityWithoutChangingLegacySaveKeys()
        {
            MetaUpgradeCatalogSO catalog = AssetDatabase.LoadAssetAtPath<MetaUpgradeCatalogSO>(CatalogPath);
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);

            Assert.That(catalog, Is.Not.Null);
            Assert.That(icon, Is.Not.Null);
            Assert.That(catalog.ValidateCatalog(), Is.Empty);
            Assert.That(catalog.Presentation.Version, Is.EqualTo(MetaPresentationSettings.CurrentVersion));
            Assert.That(catalog.Presentation.CurrencyId, Is.EqualTo("last_embers"));
            Assert.That(catalog.Presentation.DisplayName, Is.EqualTo("LAST EMBERS"));
            Assert.That(catalog.Presentation.ShortName, Is.EqualTo("EMBERS"));
            Assert.That(catalog.Presentation.CurrencyIcon, Is.SameAs(icon));
            Assert.That(catalog.Presentation.DeathTitle, Is.EqualTo("THE WALL HAS FALLEN"));
            Assert.That(catalog.Presentation.RestartLabel, Is.EqualTo("BEGIN NEXT RUN"));
            Assert.That(MetaProgression.CurrencyName, Is.EqualTo("LAST EMBERS"));
            Assert.That(MetaProgression.LegacyCurrencyName, Is.EqualTo("SOULS"));
            Assert.That(typeof(MetaProgressState).GetField(nameof(MetaProgressState.Souls)), Is.Not.Null,
                "Presentation migration yayinlanmis Souls save alanini yeniden adlandirmamali.");
            Assert.That(catalog.Upgrades.Select(upgrade => upgrade.Id), Is.EqualTo(new[]
            {
                "start_wood", "start_stone", "start_iron", "start_food",
                "start_archers", "start_beds", "wall_hp", "production",
                "arrow_efficiency", "essence_gain", "node_pool_unlock"
            }));
        }

        [Test]
        public void NewGameScene_DeathScreenUsesScrollablePolishedMetaPresentation()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedByTest = !scene.IsValid() || !scene.isLoaded;
            if (openedByTest)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                GameOverUI gameOver = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<GameOverUI>(true))
                    .Single();
                MetaProgressionUI meta = gameOver.GetComponent<MetaProgressionUI>();
                RectTransform panel = gameOver.GetComponent<RectTransform>();
                MetaUpgradeCatalogSO catalog = AssetDatabase.LoadAssetAtPath<MetaUpgradeCatalogSO>(CatalogPath);
                SoulCounterUI soulCounter = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<SoulCounterUI>(true))
                    .Single();

                Assert.That(panel.rect.width, Is.GreaterThanOrEqualTo(1120f));
                Assert.That(panel.rect.height, Is.GreaterThanOrEqualTo(880f));
                Assert.That(ReadTextField(gameOver, "GameOverText"),
                    Is.EqualTo(catalog.Presentation.DeathTitle));
                Assert.That(ReadTextField(gameOver, "StatsText"),
                    Is.EqualTo(catalog.Presentation.DeathSubtitle));
                Component restartLabel = gameOver.RestartButton.GetComponentsInChildren<Component>(true)
                    .First(component => component.GetType().Name == "TextMeshProUGUI");
                Assert.That(ReadText(restartLabel),
                    Is.EqualTo(catalog.Presentation.RestartLabel));

                Assert.That(ReadComponentField(meta, "MetaRecordText"), Is.Not.Null);
                Assert.That(ReadComponentField(meta, "MetaEarnedText"), Is.Not.Null);
                Assert.That(meta.MetaRewardIcon.sprite, Is.SameAs(catalog.Presentation.CurrencyIcon));
                Assert.That(meta.MetaCurrencyIcon.sprite, Is.SameAs(catalog.Presentation.CurrencyIcon));
                Assert.That(ReadTextField(meta, "MetaShopTitleText"),
                    Is.EqualTo(catalog.Presentation.ShopTitle));
                Assert.That(ReadTextField(meta, "MetaShopHintText"),
                    Is.EqualTo(catalog.Presentation.ShopHint));
                Assert.That(ReadTextField(soulCounter, "CounterText"),
                    Is.EqualTo($"ON DEATH  +0 {catalog.Presentation.ShortName}"),
                    "Run HUD ilk frame'de legacy SOULS placeholder'i gostermemeli.");

                RectTransform viewport = meta.MetaShopListRoot.parent as RectTransform;
                Assert.That(viewport, Is.Not.Null);
                Assert.That(viewport.name, Is.EqualTo("MetaShopViewport"));
                Assert.That(viewport.GetComponent<Mask>(), Is.Not.Null);
                ScrollRect scroll = viewport.GetComponent<ScrollRect>();
                Assert.That(scroll, Is.Not.Null);
                Assert.That(scroll.content, Is.SameAs(meta.MetaShopListRoot));
                Assert.That(scroll.vertical, Is.True);
                Assert.That(scroll.horizontal, Is.False);
                Assert.That(meta.MetaShopListRoot.GetComponent<ContentSizeFitter>().verticalFit,
                    Is.EqualTo(ContentSizeFitter.FitMode.PreferredSize));

                Transform template = meta.MetaShopRowTemplate.transform;
                Assert.That(template.parent, Is.SameAs(meta.MetaShopListRoot));
                Assert.That(template.Find("RowDescriptionText"), Is.Not.Null);
                Assert.That(template.GetComponent<LayoutElement>().preferredHeight,
                    Is.GreaterThanOrEqualTo(68f));

                RectTransform restart = gameOver.RestartButton.GetComponent<RectTransform>();
                var viewportCorners = new Vector3[4];
                var restartCorners = new Vector3[4];
                viewport.GetWorldCorners(viewportCorners);
                restart.GetWorldCorners(restartCorners);
                Assert.That(restartCorners[1].y, Is.LessThan(viewportCorners[0].y - 20f),
                    "Scrollable shop Restart CTA'nin ustune tasamaz.");
            }
            finally
            {
                if (openedByTest && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Component ReadComponentField(object target, string fieldName)
        {
            return target.GetType().GetField(fieldName)?.GetValue(target) as Component;
        }

        private static string ReadTextField(object target, string fieldName)
        {
            return ReadText(ReadComponentField(target, fieldName));
        }

        private static string ReadText(Component textComponent)
        {
            Assert.That(textComponent, Is.Not.Null);
            return textComponent.GetType().GetProperty("text")?.GetValue(textComponent) as string;
        }
    }
}
