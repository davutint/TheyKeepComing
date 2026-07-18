using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadWalls.Tests
{
    public class NarrativePresentationTests
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenuScene.unity";
        private const string NarrativeSpecPath = "Assets/Docs/DEAD_WALLS_V1_NARRATIVE_PREMISE.md";

        [Test]
        public void CanonCopy_IsShortStableAndMechanicallyFaithful()
        {
            Assert.That(MainMenuSceneUI.ProductTitle, Is.EqualTo("DEAD WALLS"));
            Assert.That(MainMenuSceneUI.OpeningTagline,
                Is.EqualTo("THE WORLD ENDED. THE SIEGE DID NOT."));
            Assert.That(MainMenuSceneUI.NewStandLabel, Is.EqualTo("BEGIN THE STAND"));

            string spec = File.ReadAllText(NarrativeSpecPath);
            Assert.That(spec, Does.Contain(MainMenuSceneUI.OpeningTagline));
            Assert.That(spec, Does.Contain(MainMenuSceneUI.NewStandLabel));
            Assert.That(spec, Does.Contain("Grave Essence"));
            Assert.That(spec, Does.Contain("Last Embers"));
            Assert.That(spec, Does.Contain("No boss, elite, origin reveal"));
        }

        [Test]
        public void MainMenuScene_UsesCanonOpeningWithoutAPrologueModal()
        {
            Scene scene = SceneManager.GetSceneByPath(MainMenuScenePath);
            bool openedByTest = !scene.IsValid() || !scene.isLoaded;
            if (openedByTest)
                scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Additive);

            try
            {
                MainMenuSceneUI menu = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<MainMenuSceneUI>(true))
                    .Single();

                Assert.That(ReadTextField(menu, "TitleText"),
                    Is.EqualTo(MainMenuSceneUI.ProductTitle));
                Assert.That(ReadTextField(menu, "TaglineText"),
                    Is.EqualTo(MainMenuSceneUI.OpeningTagline));

                Component newStandLabel = menu.NewRunButton
                    .GetComponentsInChildren<Component>(true)
                    .First(component => component.GetType().Name == "TextMeshProUGUI");
                Assert.That(ReadText(newStandLabel),
                    Is.EqualTo(MainMenuSceneUI.NewStandLabel));

                string[] forbiddenModalNames =
                {
                    "ProloguePanel", "NarrativeModal", "LorePanel", "OpeningCutscene"
                };
                foreach (string forbidden in forbiddenModalNames)
                {
                    bool exists = scene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                        .Any(transform => transform.name == forbidden);
                    Assert.That(exists, Is.False, $"V1 opening modal eklememeli: {forbidden}");
                }
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
