using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls.Tests
{
    public class HudAbilityBarPresentationTests
    {
        private const string HudPrefabPath =
            "Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab";

        [Test]
        public void ActiveHudPrefab_UsesSingleBottomCenterThreeSlotAbilityBar()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            RectTransform[] rects = prefab.GetComponentsInChildren<RectTransform>(true);
            RectTransform[] panels = rects.Where(rect => rect.name == "AbilityBarPanel").ToArray();
            Assert.That(panels, Has.Length.EqualTo(1));

            RectTransform panel = panels[0];
            Assert.That(panel.gameObject.activeSelf, Is.True);
            Assert.That(panel.anchorMin, Is.EqualTo(new Vector2(0.5f, 0f)));
            Assert.That(panel.anchorMax, Is.EqualTo(new Vector2(0.5f, 0f)));
            Assert.That(panel.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(panel.anchoredPosition, Is.EqualTo(new Vector2(0f, 63f)));
            Assert.That(panel.sizeDelta, Is.EqualTo(new Vector2(496f, 90f)));

            string[] buttonNames =
            {
                "FireballButton",
                "RallyAbilityButton",
                "EmergencyRepairAbilityButton"
            };
            string[] fillNames =
            {
                "FireballButtonCooldownFill",
                "RallyAbilityButtonCooldownFill",
                "EmergencyRepairAbilityButtonCooldownFill"
            };
            Vector2[] expectedMin =
            {
                new Vector2(-232f, 10f),
                new Vector2(-72f, 10f),
                new Vector2(78f, 10f)
            };
            Vector2[] expectedMax =
            {
                new Vector2(-78f, 80f),
                new Vector2(72f, 80f),
                new Vector2(232f, 80f)
            };

            for (int i = 0; i < buttonNames.Length; i++)
            {
                RectTransform buttonRect = rects.Single(rect => rect.name == buttonNames[i]);
                Assert.That(buttonRect.parent, Is.EqualTo(panel), buttonNames[i]);
                Assert.That(buttonRect.gameObject.activeSelf, Is.True, buttonNames[i]);
                Assert.That(buttonRect.GetComponent<Button>(), Is.Not.Null, buttonNames[i]);
                Assert.That(buttonRect.offsetMin, Is.EqualTo(expectedMin[i]), buttonNames[i]);
                Assert.That(buttonRect.offsetMax, Is.EqualTo(expectedMax[i]), buttonNames[i]);

                Image cooldownFill = buttonRect.GetComponentsInChildren<Image>(true)
                    .Single(image => image.name == fillNames[i]);
                Assert.That(cooldownFill.type, Is.EqualTo(Image.Type.Filled), fillNames[i]);
                Assert.That(cooldownFill.fillMethod, Is.EqualTo(Image.FillMethod.Vertical), fillNames[i]);
                Assert.That(cooldownFill.fillOrigin, Is.Zero, fillNames[i]);
                Assert.That(cooldownFill.raycastTarget, Is.False, fillNames[i]);
            }

            string[] objectNames = prefab.GetComponentsInChildren<Transform>(true)
                .Select(transform => transform.name)
                .ToArray();
            Assert.That(objectNames.Any(name => name == "SpellUiRoot" || name == "SpellPanel"),
                Is.False);
        }

        [Test]
        public void CooldownFill_MapsRemainingDurationToNormalizedOverlay()
        {
            MethodInfo method = typeof(SpellCastUI).GetMethod(
                "SetCooldownFill",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);

            GameObject testObject = new GameObject("CooldownFillTest", typeof(RectTransform), typeof(Image));
            try
            {
                Image fill = testObject.GetComponent<Image>();
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Vertical;

                method.Invoke(null, new object[] { fill, 15f, 30f });
                Assert.That(fill.fillAmount, Is.EqualTo(0.5f).Within(0.001f));

                method.Invoke(null, new object[] { fill, -5f, 30f });
                Assert.That(fill.fillAmount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(testObject);
            }
        }
    }
}
