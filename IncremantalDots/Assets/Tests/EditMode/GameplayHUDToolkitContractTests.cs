using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace DeadWalls.Tests
{
    public class GameplayHUDToolkitContractTests
    {
        private const string HudPath = "Assets/UI/Toolkit/GameplayHUD/GameplayHUD.uxml";

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

        private static VisualTreeAsset LoadHud()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudPath);
            Assert.That(asset, Is.Not.Null, $"Gameplay HUD UXML bulunamadi: {HudPath}");
            return asset;
        }
    }
}
