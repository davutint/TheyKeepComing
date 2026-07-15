using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls.Tests
{
    public class HudWorkerHousingPresentationTests
    {
        private const string HudPrefabPath =
            "Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab";

        [Test]
        public void ActiveHudPrefab_UsesSingleBottomLeftWorkersHousingSurface()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            RectTransform[] rects = prefab.GetComponentsInChildren<RectTransform>(true);
            RectTransform[] panels = rects
                .Where(rect => rect.name == "WorkerEconomyDrawerPanel")
                .ToArray();
            Assert.That(panels, Has.Length.EqualTo(1));

            RectTransform panel = panels[0];
            Assert.That(panel.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(panel.anchorMax, Is.EqualTo(Vector2.zero));
            Assert.That(panel.pivot, Is.EqualTo(Vector2.zero));
            Assert.That(panel.anchoredPosition, Is.EqualTo(new Vector2(24f, 160f)));
            Assert.That(panel.sizeDelta, Is.EqualTo(new Vector2(980f, 382f)));

            RectTransform toggle = rects.Single(rect => rect.name == "WorkerDrawerToggleButton");
            Assert.That(toggle.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(toggle.anchorMax, Is.EqualTo(Vector2.zero));
            Assert.That(toggle.pivot, Is.EqualTo(Vector2.zero));
            Assert.That(toggle.anchoredPosition, Is.EqualTo(new Vector2(24f, 28f)));
            Assert.That(toggle.sizeDelta, Is.EqualTo(new Vector2(206f, 56f)));
            Component toggleText = toggle.GetComponentsInChildren<Component>(true)
                .Single(component => component.GetType().Name == "TextMeshProUGUI");
            string toggleLabel = (string)toggleText.GetType().GetProperty("text")
                .GetValue(toggleText);
            Assert.That(toggleLabel, Is.EqualTo("WORKERS + HOUSING"));

            RectTransform housingRow = rects.Single(rect => rect.name == "HousingRow");
            Assert.That(housingRow.parent, Is.EqualTo(panel));
            Assert.That(housingRow.anchoredPosition, Is.EqualTo(new Vector2(0f, -146f)));
            Assert.That(housingRow.sizeDelta, Is.EqualTo(new Vector2(956f, 44f)));

            string[] requiredHousingControls =
            {
                "HousingCapacityText",
                "HousingAvailabilityText",
                "HousingPurchasedText",
                "HousingBuyOneButton",
                "HousingBuyTenButton",
                "HousingBuyHundredButton"
            };
            foreach (string controlName in requiredHousingControls)
                Assert.That(rects.Count(rect => rect.name == controlName), Is.EqualTo(1), controlName);

            Assert.That(rects.Single(rect => rect.name == "HousingBuyOneButton")
                .GetComponent<Button>(), Is.Not.Null);
            Assert.That(rects.Single(rect => rect.name == "HousingBuyTenButton")
                .GetComponent<Button>(), Is.Not.Null);
            Assert.That(rects.Single(rect => rect.name == "HousingBuyHundredButton")
                .GetComponent<Button>(), Is.Not.Null);

            RectTransform abilityBar = rects.Single(rect => rect.name == "AbilityBarPanel");
            string[] abilityButtonNames =
            {
                "FireballButton",
                "RallyAbilityButton",
                "EmergencyRepairAbilityButton"
            };
            float abilityContentTop = abilityButtonNames
                .Select(name => rects.Single(rect => rect.name == name))
                .Max(rect => abilityBar.anchoredPosition.y + rect.anchoredPosition.y
                    + rect.sizeDelta.y * (1f - rect.pivot.y));
            Assert.That(panel.anchoredPosition.y, Is.GreaterThan(abilityContentTop));
        }

        [Test]
        public void WorkerDrawerController_ResolvesAllHousingBindingsFromPrefabNames()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            GameObject instance = Object.Instantiate(prefab);
            instance.SetActive(false);
            try
            {
                Component controller = instance.AddComponent(typeof(WorkerEconomyDrawerUI));
                MethodInfo resolver = typeof(WorkerEconomyDrawerUI).GetMethod(
                    "ResolveMissingHousingControls",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(resolver, Is.Not.Null);
                resolver.Invoke(controller, null);

                string[] fieldNames =
                {
                    "HousingCapacityText",
                    "HousingAvailabilityText",
                    "HousingPurchasedText",
                    "HousingBuyOneButton",
                    "HousingBuyTenButton",
                    "HousingBuyHundredButton"
                };
                foreach (string fieldName in fieldNames)
                {
                    FieldInfo field = typeof(WorkerEconomyDrawerUI).GetField(fieldName);
                    Assert.That(field, Is.Not.Null, fieldName);
                    Assert.That(field.GetValue(controller), Is.Not.Null, fieldName);
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
