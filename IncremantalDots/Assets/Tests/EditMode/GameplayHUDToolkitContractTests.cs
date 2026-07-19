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
            Assert.That(root.Q<VisualElement>("phaseDayFill"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("phaseDuskFill"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("phaseNightFill"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("phaseDawnFill"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>(className: "dw-icon--wood"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>(className: "dw-icon--repair"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>(className: "dw-icon--arrows"), Is.Not.Null);
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
        }

        private static VisualTreeAsset LoadHud()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudPath);
            Assert.That(asset, Is.Not.Null, $"Gameplay HUD UXML bulunamadi: {HudPath}");
            return asset;
        }
    }
}
