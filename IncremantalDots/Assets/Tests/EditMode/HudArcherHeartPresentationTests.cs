using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls.Tests
{
    public class HudArcherHeartPresentationTests
    {
        private const string HudPrefabPath =
            "Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab";

        [Test]
        public void ActiveHudPrefab_UsesSingleBottomRightArcherHeartSurface()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            RectTransform[] rects = prefab.GetComponentsInChildren<RectTransform>(true);
            RectTransform drawerPanel = rects.Single(rect => rect.name == "ArcherDrawerPanel");
            RectTransform archerButton = rects.Single(rect => rect.name == "DrawerToggleButton");
            RectTransform heartButton = rects.Single(rect => rect.name == "CastleHeartOpenButton");

            AssertBottomRightRect(drawerPanel, new Vector2(-24f, 160f), new Vector2(540f, 350f));
            AssertBottomRightRect(archerButton, new Vector2(-190f, 28f), new Vector2(156f, 56f));
            AssertBottomRightRect(heartButton, new Vector2(-24f, 28f), new Vector2(156f, 56f));

            Assert.That(archerButton.parent, Is.EqualTo(drawerPanel.parent));
            Assert.That(heartButton.parent, Is.EqualTo(drawerPanel.parent));
            Assert.That(archerButton.GetComponent<Button>(), Is.Not.Null);
            Assert.That(heartButton.GetComponent<Button>(), Is.Not.Null);
            Assert.That(GetLabel(archerButton), Is.EqualTo("ARCHERS"));
            Assert.That(GetLabel(heartButton), Is.EqualTo("CASTLE HEART"));
            AssertDockLabel(archerButton);
            AssertDockLabel(heartButton);

            Assert.That(prefab.GetComponentsInChildren<MarketUI>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<HeartScreenUI>(true), Is.Empty);
        }

        [Test]
        public void ArcherHeartSurface_FitsReferenceFrameWithoutAbilityOrWorkerOverlap()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            RectTransform[] rects = prefab.GetComponentsInChildren<RectTransform>(true);
            Rect archerButton = ToReferenceRect(rects.Single(rect => rect.name == "DrawerToggleButton"));
            Rect heartButton = ToReferenceRect(rects.Single(rect => rect.name == "CastleHeartOpenButton"));
            Rect archerPanel = ToReferenceRect(rects.Single(rect => rect.name == "ArcherDrawerPanel"));
            Rect workerPanel = ToReferenceRect(rects.Single(rect => rect.name == "WorkerEconomyDrawerPanel"));
            Rect abilityBar = ToReferenceRect(rects.Single(rect => rect.name == "AbilityBarPanel"));

            Assert.That(archerButton.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(heartButton.xMax, Is.LessThanOrEqualTo(1920f));
            Assert.That(archerPanel.yMax, Is.LessThanOrEqualTo(1080f));
            Assert.That(archerButton.Overlaps(heartButton), Is.False);
            Assert.That(archerButton.Overlaps(abilityBar), Is.False);
            Assert.That(heartButton.Overlaps(abilityBar), Is.False);
            Assert.That(archerPanel.Overlaps(workerPanel), Is.False);
            Assert.That(archerPanel.yMin, Is.GreaterThan(abilityBar.yMax));
        }

        [Test]
        public void MarketUi_OnEnableStartsDrawerClosed()
        {
            GameObject owner = new GameObject("MarketUiDefaultsTest", typeof(RectTransform));
            owner.SetActive(false);
            try
            {
                GameObject panelObject = new GameObject("ArcherDrawerPanel", typeof(RectTransform));
                RectTransform panel = panelObject.GetComponent<RectTransform>();
                panel.SetParent(owner.transform, false);
                panel.anchoredPosition = new Vector2(-24f, 160f);
                panel.sizeDelta = new Vector2(540f, 350f);

                MarketUI market = owner.AddComponent<MarketUI>();
                market.ArcherDrawerPanel = panel;
                MethodInfo onEnable = typeof(MarketUI).GetMethod(
                    "OnEnable",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(onEnable, Is.Not.Null);
                onEnable.Invoke(market, null);

                Assert.That(panel.anchoredPosition, Is.EqualTo(new Vector2(556f, 160f)));
                Assert.That(market.OpenOnWaveCompleted, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        private static void AssertBottomRightRect(
            RectTransform rect,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(1f, 0f)), rect.name);
            Assert.That(rect.anchorMax, Is.EqualTo(new Vector2(1f, 0f)), rect.name);
            Assert.That(rect.pivot, Is.EqualTo(new Vector2(1f, 0f)), rect.name);
            Assert.That(rect.anchoredPosition, Is.EqualTo(anchoredPosition), rect.name);
            Assert.That(rect.sizeDelta, Is.EqualTo(size), rect.name);
        }

        private static string GetLabel(RectTransform root)
        {
            Component text = root.GetComponentsInChildren<Component>(true)
                .Single(component => component.GetType().Name == "TextMeshProUGUI");
            return (string)text.GetType()
                .GetProperty("text", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(text);
        }

        private static void AssertDockLabel(RectTransform root)
        {
            Component text = root.GetComponentsInChildren<Component>(true)
                .Single(component => component.GetType().Name == "TextMeshProUGUI");
            RectTransform rect = (RectTransform)text.transform;
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero), root.name);
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one), root.name);
            Assert.That(rect.anchoredPosition, Is.EqualTo(Vector2.zero), root.name);
            Assert.That(rect.sizeDelta, Is.EqualTo(Vector2.zero), root.name);
            Assert.That(rect.localRotation, Is.EqualTo(Quaternion.identity), root.name);
        }

        private static Rect ToReferenceRect(RectTransform rect)
        {
            Vector2 anchorPoint = new Vector2(
                Mathf.Lerp(0f, 1920f, rect.anchorMin.x),
                Mathf.Lerp(0f, 1080f, rect.anchorMin.y));
            Vector2 pivotPoint = anchorPoint + rect.anchoredPosition;
            Vector2 minimum = pivotPoint - Vector2.Scale(rect.sizeDelta, rect.pivot);
            return new Rect(minimum, rect.sizeDelta);
        }
    }
}
