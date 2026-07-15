using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class HudPhaseAreaPresentationTests
    {
        private const string HudPrefabPath =
            "Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab";

        [Test]
        public void ActiveHudPrefab_UsesMinimalTopCenterPhaseArea()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
            RectTransform panel = transforms.First(t => t.name == "CyclePanel") as RectTransform;
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.anchorMin, Is.EqualTo(new Vector2(0.5f, 1f)));
            Assert.That(panel.anchorMax, Is.EqualTo(new Vector2(0.5f, 1f)));
            Assert.That(panel.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(panel.anchoredPosition, Is.EqualTo(new Vector2(0f, -67f)));
            Assert.That(panel.sizeDelta, Is.EqualTo(new Vector2(340f, 78f)));
            Assert.That(panel.sizeDelta.x * panel.sizeDelta.y,
                Is.LessThan(384f * 106f * 0.7f));

            string[] requiredChildren =
            {
                "CyclePhaseText",
                "CycleDayCounterText",
                "CycleProgressTrack",
                "CycleProgressFill",
                "CycleProgressMarker",
                "CycleDayLabelText",
                "CycleDuskLabelText",
                "CycleNightLabelText"
            };
            foreach (string childName in requiredChildren)
                Assert.That(transforms.Any(t => t.name == childName), Is.True, childName);

            RectTransform track = transforms.First(t => t.name == "CycleProgressTrack") as RectTransform;
            Assert.That(track.sizeDelta, Is.EqualTo(new Vector2(280f, 10f)));
            Assert.That(transforms.First(t => t.name == "CycleProgressFill").parent, Is.EqualTo(track));
            Assert.That(transforms.First(t => t.name == "CycleProgressMarker").parent, Is.EqualTo(track));

            Component phaseText = transforms.First(t => t.name == "CyclePhaseText")
                .GetComponent("TextMeshProUGUI");
            float phaseFontSize = (float)phaseText.GetType().GetProperty("fontSize").GetValue(phaseText);
            Assert.That(phaseFontSize, Is.EqualTo(18f));

            foreach (string labelName in new[]
                     { "CycleDayLabelText", "CycleDuskLabelText", "CycleNightLabelText" })
            {
                Component label = transforms.First(t => t.name == labelName)
                    .GetComponent("TextMeshProUGUI");
                float fontSize = (float)label.GetType().GetProperty("fontSize").GetValue(label);
                Assert.That(fontSize, Is.EqualTo(8.5f), labelName);
            }
        }
    }
}
