using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls.Tests
{
    public class HudPhaseAreaPresentationTests
    {
        private const string HudPrefabPath =
            "Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab";

        [Test]
        public void ActiveHudPrefab_UsesOwnerApprovedCelestialDial()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
            RectTransform panel = transforms.First(t => t.name == "CyclePanel") as RectTransform;
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.anchorMin, Is.EqualTo(new Vector2(0.5f, 1f)));
            Assert.That(panel.anchorMax, Is.EqualTo(new Vector2(0.5f, 1f)));
            Assert.That(panel.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(panel.anchoredPosition, Is.EqualTo(new Vector2(0f, -68f)));
            Assert.That(panel.sizeDelta, Is.EqualTo(new Vector2(290f, 68f)));
            Assert.That(panel.sizeDelta.x * panel.sizeDelta.y,
                Is.LessThan(340f * 78f * 0.8f));
            Assert.That(panel.GetComponent<Image>().color.a, Is.EqualTo(0f).Within(0.001f));

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
            Assert.That(track.anchoredPosition, Is.EqualTo(new Vector2(22f, -1f)));
            Assert.That(track.sizeDelta, Is.EqualTo(new Vector2(178f, 44f)));
            Assert.That(transforms.First(t => t.name == "CycleProgressFill").parent, Is.EqualTo(track));
            Assert.That(transforms.First(t => t.name == "CycleProgressMarker").parent, Is.EqualTo(track));
            Assert.That(transforms.First(t => t.name == "CycleProgressFill").gameObject.activeSelf, Is.False);
            Assert.That(transforms.First(t => t.name == "CyclePhaseText").gameObject.activeSelf, Is.False);
            Assert.That(transforms.First(t => t.name == "CycleDayLabelText").gameObject.activeSelf, Is.False);
            Assert.That(transforms.First(t => t.name == "CycleDuskLabelText").gameObject.activeSelf, Is.False);
            Assert.That(transforms.First(t => t.name == "CycleNightLabelText").gameObject.activeSelf, Is.False);

            RectTransform segments = transforms.First(t => t.name == "CycleCelestialArcSegments") as RectTransform;
            Assert.That(segments, Is.Not.Null);
            Assert.That(segments.childCount, Is.EqualTo(44));
            Assert.That(transforms.Any(t => t.name == "CycleCelestialGlow"), Is.True);
            Assert.That(transforms.Any(t => t.name == "CycleCelestialCore"), Is.True);
            Assert.That(transforms.Any(t => t.name == "CycleDawnHorizon"), Is.True);
            Assert.That(transforms.First(t => t.name == "CycleDayDivider").gameObject.activeSelf, Is.False);

            string[] pillParts =
            {
                "CyclePillShadowBody",
                "CyclePillShadowLeftCap",
                "CyclePillShadowRightCap",
                "CyclePillBody",
                "CyclePillLeftCap",
                "CyclePillRightCap"
            };
            foreach (string partName in pillParts)
            {
                Transform part = transforms.FirstOrDefault(t => t.name == partName);
                Assert.That(part, Is.Not.Null, partName);
                Assert.That(part.gameObject.activeSelf, Is.True, partName);
                Assert.That(part.GetComponent<Image>().raycastTarget, Is.False, partName);
            }

            Component dayText = transforms.First(t => t.name == "CycleDayCounterText")
                .GetComponent("TextMeshProUGUI");
            RectTransform dayRect = transforms.First(t => t.name == "CycleDayCounterText") as RectTransform;
            Assert.That(dayRect.anchoredPosition, Is.EqualTo(new Vector2(-102f, 0f)));
            float dayFontSize = (float)dayText.GetType().GetProperty("fontSize").GetValue(dayText);
            Assert.That(dayFontSize, Is.EqualTo(11f));

            foreach (string partName in new[] { "CyclePillBody", "CyclePillLeftCap", "CyclePillRightCap" })
                Assert.That(transforms.First(t => t.name == partName).GetComponent<Image>().color.a,
                    Is.EqualTo(1f).Within(0.001f), partName);
            foreach (string capName in new[] { "CyclePillLeftCap", "CyclePillRightCap" })
                Assert.That(transforms.First(t => t.name == capName).GetComponent<Image>().sprite.name,
                    Is.EqualTo("CelestialPillCircle"), capName);
        }

        [Test]
        public void CelestialMarkerPath_IsShallowArcFromLeftToRight()
        {
            MethodInfo method = typeof(HUDController).GetMethod(
                "CalculateCelestialMarkerPosition",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);

            Vector2 start = (Vector2)method.Invoke(null, new object[] { 178f, 44f, 8f, 0f });
            Vector2 middle = (Vector2)method.Invoke(null, new object[] { 178f, 44f, 8f, 0.5f });
            Vector2 end = (Vector2)method.Invoke(null, new object[] { 178f, 44f, 8f, 1f });

            Assert.That(start.x, Is.LessThan(0f));
            Assert.That(end.x, Is.GreaterThan(0f));
            Assert.That(start.y, Is.EqualTo(end.y).Within(0.001f));
            Assert.That(middle.y, Is.GreaterThan(start.y + 20f));
        }
    }
}
