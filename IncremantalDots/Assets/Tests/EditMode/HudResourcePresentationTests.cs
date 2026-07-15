using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class HudResourcePresentationTests
    {
        private const string HudPrefabPath =
            "Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab";

        [Test]
        public void FormatResourceValue_UsesCompactSingleLineRate()
        {
            Assert.That(HUDController.FormatResourceValue(4280, 54f), Is.EqualTo("4,280  +54/m"));
            Assert.That(HUDController.FormatResourceValue(1960, -32f), Is.EqualTo("1,960  -32/m"));
            Assert.That(HUDController.FormatResourceValue(74, 0f), Is.EqualTo("74"));
        }

        [Test]
        public void ActiveHudPrefab_UsesCompactSixChipResourceStrip()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
            RectTransform resourceBar = transforms.First(t => t.name == "ResourceBar") as RectTransform;
            Assert.That(resourceBar, Is.Not.Null);
            Assert.That(resourceBar.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(resourceBar.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(resourceBar.pivot, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(resourceBar.anchoredPosition, Is.EqualTo(new Vector2(28f, -28f)));
            Assert.That(resourceBar.sizeDelta, Is.EqualTo(new Vector2(560f, 48f)));

            string[] prefixes = { "Wood", "Stone", "Iron", "Food", "Population", "Arrow" };
            var labelColors = new HashSet<string>();
            foreach (string prefix in prefixes)
            {
                RectTransform chip = transforms.First(t => t.name == prefix + "Chip") as RectTransform;
                Assert.That(chip, Is.Not.Null, prefix + " chip missing");
                Assert.That(chip.sizeDelta, Is.EqualTo(new Vector2(84f, 42f)), prefix);

                Component label = transforms.First(t => t.name == prefix + "Label")
                    .GetComponent("TextMeshProUGUI");
                Assert.That(label, Is.Not.Null, prefix + " label missing");
                Color labelColor = (Color)label.GetType().GetProperty("color").GetValue(label);
                labelColors.Add(ColorUtility.ToHtmlStringRGB(labelColor));

                string valueName = prefix == "Population" ? "PopulationText" : prefix + "Text";
                Component value = transforms.First(t => t.name == valueName)
                    .GetComponent("TextMeshProUGUI");
                Assert.That(value, Is.Not.Null, valueName + " missing");
                string previewText = (string)value.GetType().GetProperty("text").GetValue(value);
                bool autoSizing = (bool)value.GetType().GetProperty("enableAutoSizing").GetValue(value);
                Assert.That(previewText, Does.Not.Contain("\n"), valueName);
                Assert.That(autoSizing, Is.True, valueName);
            }

            Assert.That(labelColors.Count, Is.EqualTo(prefixes.Length));
            Assert.That(transforms.First(t => t.name == "ArrowChip").GetComponent("Button"), Is.Not.Null);
        }
    }
}
