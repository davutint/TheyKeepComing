using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeadWalls.Tests
{
    public class GameplayHUDToolkitContractTests
    {
        private const string HudPath = "Assets/UI/Toolkit/GameplayHUD/GameplayHUD.uxml";
        private const string MainMenuPath = "Assets/UI/Toolkit/MainMenu/MainMenu.uxml";

        [Test]
        public void ProductionHud_DoesNotExposeDuplicateTechnologySurface()
        {
            TemplateContainer root = LoadHud().CloneTree();

            Assert.That(root.Q<Button>("techButton"), Is.Null);
            Assert.That(root.Q<VisualElement>("techScreen"), Is.Null);
            Assert.That(root.Q<Button>("heartButton"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("heartScreen"), Is.Not.Null);
        }

        [Test]
        public void CastleHeart_UsesSingleResearchFlowWithoutVisibleBranchLegendOrBulkControls()
        {
            TemplateContainer root = LoadHud().CloneTree();

            Assert.That(root.Q<VisualElement>("heartViewport"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("heartGraphContent"), Is.Not.Null);
            Assert.That(root.Q<Button>("heartZoomOut"), Is.Not.Null);
            Assert.That(root.Q<Label>("heartZoomValue"), Is.Not.Null);
            Assert.That(root.Q<Button>("heartZoomIn"), Is.Not.Null);
            Assert.That(root.Q<Button>("heartZoomReset"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("heartInspectorIcon"), Is.Not.Null);
            Assert.That(root.Q<Label>("heartInspectorMeta"), Is.Not.Null);
            Assert.That(root.Q<Label>("heartInspectorStatus"), Is.Not.Null);
            Assert.That(root.Q<Button>("heartPurchase"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>(className: "heart-legend"), Is.Null);
            Assert.That(root.Q<Button>("heartQuantityOne"), Is.Null);
            Assert.That(root.Q<Button>("heartQuantityTen"), Is.Null);
            Assert.That(root.Q<Button>("heartQuantityMax"), Is.Null);
        }

        [Test]
        public void CastleHeart_AllProductionNodesUseReviewedRpgPixelIcons()
        {
            HeartNodeCatalogSO catalog = AssetDatabase.LoadAssetAtPath<HeartNodeCatalogSO>(
                "Assets/ScriptableObject/MobileCastle/CastleHeart/HeartNodeCatalog.asset");

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Nodes, Has.Length.EqualTo(37));
            for (int i = 0; i < catalog.Nodes.Length; i++)
            {
                HeartNodeDefinitionSO node = catalog.Nodes[i];
                Assert.That(node, Is.Not.Null);
                Assert.That(node.Icon, Is.Not.Null, $"Icon eksik: {node.Id}");
                Assert.That(
                    AssetDatabase.GetAssetPath(node.Icon),
                    Does.StartWith("Assets/RPG Icons Pixel Art/"),
                    $"Onayli paket disi icon: {node.Id}");
            }
        }

        [Test]
        public void ProductionHud_ContainsWorkerAndStructuredGameOverContracts()
        {
            TemplateContainer root = LoadHud().CloneTree();

            Assert.That(root.Q<ScrollView>("economyRows"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("gameOverModal"), Is.Not.Null);
            Assert.That(root.Q<Label>("gameOverDay"), Is.Not.Null);
            Assert.That(root.Q<Label>("gameOverKills"), Is.Not.Null);
            Assert.That(root.Q<Label>("gameOverEarned"), Is.Not.Null);
            Assert.That(root.Q<Label>("gameOverBalance"), Is.Not.Null);
            Assert.That(root.Q<ScrollView>("metaShopRows"), Is.Not.Null);
            Assert.That(root.Q<Button>("restartButton"), Is.Not.Null);
        }

        [Test]
        public void ProductionHud_ContainsPolishedCycleArcAndSemanticIcons()
        {
            TemplateContainer root = LoadHud().CloneTree();

            Assert.That(root.Q<VisualElement>("cycleArc"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("cycleCelestial"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("cycleCelestialMarker"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("phaseDayFill"), Is.Null);
            Assert.That(root.Q<VisualElement>("phaseDuskFill"), Is.Null);
            Assert.That(root.Q<VisualElement>("phaseNightFill"), Is.Null);
            Assert.That(root.Q<VisualElement>("phaseDawnFill"), Is.Null);
            Assert.That(root.Q<VisualElement>(className: "dw-icon--wood"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>(className: "dw-icon--repair"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>(className: "dw-icon--arrows"), Is.Not.Null);
        }

        [Test]
        public void ProductionHud_ExposesRunCurrenciesAndArrowReserveWithoutOpeningDrawers()
        {
            TemplateContainer root = LoadHud().CloneTree();

            Assert.That(root.Q<VisualElement>("graveEssenceAnchor"), Is.Not.Null);
            Assert.That(root.Q<Label>("graveEssenceHudValue"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>(className: "dw-icon--essence"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("soulAnchor"), Is.Not.Null);
            Assert.That(root.Q<Label>("soulValue"), Is.Not.Null);
            Assert.That(root.Q<Button>("arrowsButton"), Is.Not.Null);
            Assert.That(root.Q<Label>("arrowDockValue"), Is.Not.Null);

            Label heartEssence = root.Q<Label>("graveEssenceValue");
            Assert.That(heartEssence, Is.Not.Null);
            Assert.That(heartEssence.text, Is.EqualTo("0 ESSENCE"));
            Assert.That(heartEssence.parent.ClassListContains("heart-essence"), Is.True);
            Assert.That(
                heartEssence.parent.Q<VisualElement>(className: "dw-icon--essence"),
                Is.Not.Null);

            MethodInfo formatter = typeof(GameplayHUDToolkitUI).GetMethod(
                "FormatHeartEssenceCost",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(formatter, Is.Not.Null);
            Assert.That(formatter.Invoke(null, new object[] { 13L }), Is.EqualTo("13 ESSENCE"));
        }

        [Test]
        public void ProductionHud_SeparatesResourceValuesFromRates()
        {
            TemplateContainer root = LoadHud().CloneTree();

            Assert.That(root.Q<Label>("woodValue").parent.ClassListContains("readout-number-stack"), Is.True);
            Assert.That(root.Q<Label>("woodRate").parent, Is.SameAs(root.Q<Label>("woodValue").parent));
            Assert.That(root.Q<Label>("stoneRate").parent, Is.SameAs(root.Q<Label>("stoneValue").parent));
            Assert.That(root.Q<Label>("ironRate").parent, Is.SameAs(root.Q<Label>("ironValue").parent));
            Assert.That(root.Q<Label>("foodRate").parent, Is.SameAs(root.Q<Label>("foodValue").parent));
        }

        [Test]
        public void ProductionHud_AllowsBattlefieldPointerThroughHudLayer()
        {
            TemplateContainer root = LoadHud().CloneTree();
            VisualElement screen = root.Q<VisualElement>("screen");
            VisualElement hudLayer = root.Q<VisualElement>("hudLayer");

            Assert.That(screen, Is.Not.Null);
            Assert.That(screen.pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(hudLayer, Is.Not.Null);
            Assert.That(hudLayer.pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(root.Q<Button>("fireballButton").pickingMode, Is.EqualTo(PickingMode.Position));
            Assert.That(root.Q<Button>("economyButton").pickingMode, Is.EqualTo(PickingMode.Position));
        }

        [Test]
        public void ApprovedArtsystackIcons_AreImportedAndWorkerProductionDiffersFromRepair()
        {
            Texture2D workerProduction = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Artsystack - Fantasy RPG GUI/Resources/Sprites/flaticon/textured/128/fist_128_T.png");
            Texture2D repairGate = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Artsystack - Fantasy RPG GUI/Resources/Sprites/flaticon/textured/128/tools_2_128_T.png");

            Assert.That(workerProduction, Is.Not.Null);
            Assert.That(repairGate, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(workerProduction), Is.Not.EqualTo(AssetDatabase.GetAssetPath(repairGate)));

            MethodInfo metaRole = typeof(GameplayHUDToolkitUI).GetMethod(
                "MetaUpgradeIconRole", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo levelRole = typeof(GameplayHUDToolkitUI).GetMethod(
                "LevelUpIconRole", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(metaRole, Is.Not.Null);
            Assert.That(levelRole, Is.Not.Null);

            MetaUpgradeSO production = ScriptableObject.CreateInstance<MetaUpgradeSO>();
            production.Id = "production";
            Assert.That(metaRole.Invoke(null, new object[] { production }), Is.EqualTo("production"));
            Assert.That(levelRole.Invoke(null, new object[] { UpgradeType.RepairGate }), Is.EqualTo("repair"));
            Object.DestroyImmediate(production);
        }

        [Test]
        public void MainMenu_RemainsMinimalWhileSettingsUseApprovedSemanticIcons()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuPath);
            Assert.That(asset, Is.Not.Null);
            TemplateContainer root = asset.CloneTree();

            Assert.That(root.Q<Button>("continueButton").Q<VisualElement>(className: "menu-setting-icon"), Is.Null);
            Assert.That(root.Q<Button>("settingsButton").Q<VisualElement>(className: "menu-setting-icon"), Is.Null);
            Assert.That(root.Q<Label>("tutorialResetButtonLabel"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>(className: "menu-setting-icon--sound"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>(className: "menu-setting-icon--music"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>(className: "menu-setting-icon--horde"), Is.Not.Null);
            Assert.That(root.Q<Button>("zombieLimitPrevious"), Is.Not.Null);
            Assert.That(root.Q<Label>("zombieLimitValue"), Is.Not.Null);
            Assert.That(root.Q<Button>("zombieLimitNext"), Is.Not.Null);
        }

        [Test]
        public void ProductionSettings_ExposeSameZombieLimitContractAndHideBarracksHardCap()
        {
            TemplateContainer root = LoadHud().CloneTree();

            Assert.That(root.Q<Button>("zombieLimitPrevious"), Is.Not.Null);
            Assert.That(root.Q<Label>("zombieLimitValue"), Is.Not.Null);
            Assert.That(root.Q<Label>("zombieLimitHint"), Is.Not.Null);
            Assert.That(root.Q<Button>("zombieLimitNext"), Is.Not.Null);
            Assert.That(root.Q<Label>("archerCapacity").text, Is.EqualTo("0 ARCHERS DEPLOYED"));
        }

        private static VisualTreeAsset LoadHud()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudPath);
            Assert.That(asset, Is.Not.Null, $"Gameplay HUD UXML bulunamadi: {HudPath}");
            return asset;
        }
    }
}
