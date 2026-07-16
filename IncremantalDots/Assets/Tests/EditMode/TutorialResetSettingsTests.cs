using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadWalls.Tests
{
    public class TutorialResetSettingsTests
    {
        [TestCase("Assets/Scenes/NewGameScene.unity")]
        [TestCase("Assets/Scenes/MainMenuScene.unity")]
        public void SettingsScene_HasSingleBoundTutorialResetControl(string scenePath)
        {
            Scene scene = GetOrOpenScene(scenePath, out bool openedByTest);
            try
            {
                SettingsUI[] settingsControllers = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<SettingsUI>(true))
                    .ToArray();
                Assert.That(settingsControllers, Has.Length.EqualTo(1), scenePath);

                SettingsUI settings = settingsControllers[0];
                Assert.That(settings.SettingsPanel, Is.Not.Null, scenePath);
                Assert.That(settings.TutorialResetButton, Is.Not.Null, scenePath);
                Component tutorialResetLabel = GetComponentField(
                    settings, "TutorialResetLabel");
                Component tutorialResetStatus = GetComponentField(
                    settings, "TutorialResetStatusText");
                Assert.That(tutorialResetLabel, Is.Not.Null, scenePath);
                Assert.That(tutorialResetStatus, Is.Not.Null, scenePath);
                Assert.That(settings.TutorialResetButton.gameObject.name,
                    Is.EqualTo("TutorialResetButton"), scenePath);
                Assert.That(ReadText(tutorialResetLabel),
                    Is.EqualTo(SettingsUI.TutorialResetDefaultLabel), scenePath);
                Assert.That(ReadText(tutorialResetStatus),
                    Is.EqualTo(SettingsUI.TutorialResetDefaultStatus), scenePath);
                Assert.That(settings.TutorialResetButton.transform.IsChildOf(
                    settings.SettingsPanel.transform), Is.True, scenePath);
                Assert.That(tutorialResetStatus.transform.IsChildOf(
                    settings.SettingsPanel.transform), Is.True, scenePath);
                Assert.That((tutorialResetStatus as UnityEngine.UI.Graphic)?.raycastTarget,
                    Is.False, scenePath);
            }
            finally
            {
                if (openedByTest)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void TutorialResetCopy_EveryRuntimeStateUsesApprovedEnglish()
        {
            string[] actualCopy =
            {
                SettingsUI.TutorialResetDefaultLabel,
                SettingsUI.TutorialResetDefaultStatus,
                SettingsUI.TutorialResetConfirmLabel,
                SettingsUI.TutorialResetConfirmStatus,
                SettingsUI.TutorialResetSuccessStatus,
                SettingsUI.TutorialResetFailureStatus
            };
            string[] approvedEnglishCopy =
            {
                "RESET TUTORIAL",
                "RESETS ONBOARDING ONLY. RUN AND UPGRADES STAY.",
                "CONFIRM RESET",
                "CLICK AGAIN TO RESET ALL TUTORIAL STEPS.",
                "TUTORIAL RESET. IT WILL START AGAIN IN GAME.",
                "RESET FAILED. META SAVE WAS NOT CHANGED."
            };

            Assert.That(actualCopy, Is.EqualTo(approvedEnglishCopy));
            foreach (string copy in actualCopy)
                Assert.That(copy, Does.Match("^[A-Z0-9 .]+$"), copy);
        }

        private static Scene GetOrOpenScene(string path, out bool openedByTest)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loadedScene = SceneManager.GetSceneAt(i);
                if (loadedScene.path == path)
                {
                    openedByTest = false;
                    return loadedScene;
                }
            }

            openedByTest = true;
            Scene openedScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Assert.That(openedScene.IsValid(), Is.True, path);
            return openedScene;
        }

        private static Component GetComponentField(SettingsUI settings, string fieldName)
        {
            return typeof(SettingsUI).GetField(fieldName)?.GetValue(settings) as Component;
        }

        private static string ReadText(Component textComponent)
        {
            return textComponent?.GetType().GetProperty("text")?.GetValue(textComponent) as string;
        }
    }
}
