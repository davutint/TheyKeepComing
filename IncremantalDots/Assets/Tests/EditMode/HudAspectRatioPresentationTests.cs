using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeadWalls.Tests
{
    public class HudAspectRatioPresentationTests
    {
        private const string HudPrefabPath =
            "Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab";
        private const string MainScenePath = "Assets/Scenes/NewGameScene.unity";
        private const string CombatScenePath =
            "Assets/Scenes/NewGameScene/MobileCastleCombatSubScene.unity";

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private static readonly Vector2[] SupportedResolutions =
        {
            new Vector2(1920f, 1080f),
            new Vector2(3440f, 1440f)
        };

        private static readonly string[] CriticalHudRectNames =
        {
            "ResourceBar",
            "CyclePanel",
            "CastleDefensePanel",
            "AbilityBarPanel",
            "WorkerDrawerToggleButton",
            "WorkerEconomyDrawerPanel",
            "DrawerToggleButton",
            "ArcherDrawerPanel",
            "CastleHeartOpenButton",
            "CastleHeartPanel",
            "CouncilEventPanel",
            "AmmoPurchasePanel",
            "ArrowSupplyToggleButton",
            "ArrowChip"
        };

        [Test]
        public void ActiveHudPrefab_UsesResponsiveStretchVisualRoot()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            CanvasScaler scaler = prefab.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(ReferenceResolution));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f));

            Transform visualRootTransform = prefab.transform.Find("MobileCastleHudRoot");
            Assert.That(visualRootTransform, Is.Not.Null,
                "Aktif HUD prefabinin dogrudan gorsel koku bulunmali.");
            Assert.That(visualRootTransform.parent, Is.SameAs(prefab.transform));

            RectTransform visualRoot = visualRootTransform as RectTransform;
            Assert.That(visualRoot, Is.Not.Null);
            Assert.That(visualRoot.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(visualRoot.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(visualRoot.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(visualRoot.sizeDelta, Is.EqualTo(Vector2.zero));
            Assert.That(visualRoot.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(visualRoot.localScale, Is.EqualTo(Vector3.one));
        }

        [TestCase(1920, 1080)]
        [TestCase(3440, 1440)]
        public void CriticalHudRects_StayInsideSupportedCanvas(int screenWidth, int screenHeight)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            GameObject viewport = new GameObject("HudAspectRatioTestViewport", typeof(RectTransform));
            GameObject instance = null;
            try
            {
                RectTransform viewportRect = viewport.GetComponent<RectTransform>();
                viewportRect.sizeDelta = CalculateVirtualCanvasSize(screenWidth, screenHeight);

                instance = UnityEngine.Object.Instantiate(prefab);
                DestroyRootCanvasComponents(instance);

                RectTransform rootRect = instance.GetComponent<RectTransform>();
                rootRect.SetParent(viewportRect, false);
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
                rootRect.localScale = Vector3.one;

                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);

                foreach (string objectName in CriticalHudRectNames)
                {
                    RectTransform criticalRect = FindUniqueRect(instance, objectName);
                    AssertRectInsideViewport(criticalRect, viewportRect,
                        $"{screenWidth}x{screenHeight} - {objectName}");
                }

                Rect cycleBounds = GetViewportBounds(
                    FindUniqueRect(instance, "CyclePanel"), viewportRect);
                Rect defenseBounds = GetViewportBounds(
                    FindUniqueRect(instance, "CastleDefensePanel"), viewportRect);
                Assert.That(cycleBounds.Overlaps(defenseBounds), Is.False,
                    $"{screenWidth}x{screenHeight} - Celestial Dial ve savunma paneli cakismamali.");
            }
            finally
            {
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
                UnityEngine.Object.DestroyImmediate(viewport);
            }
        }

        [Test]
        public void FixedCamera_KeepsBattlefieldAndHiddenSpawnFramingAtSupportedRatios()
        {
            Scene mainScene = default;
            Scene combatScene = default;
            bool closeMainScene = false;
            bool closeCombatScene = false;

            try
            {
                mainScene = GetOrOpenScene(MainScenePath, out closeMainScene);
                combatScene = GetOrOpenScene(CombatScenePath, out closeCombatScene);

                Camera camera = FindUniqueInScene<Camera>(mainScene, "Main Camera");
                MobileCastleCombatAuthoring combat =
                    FindUniqueInScene<MobileCastleCombatAuthoring>(combatScene, null);

                Assert.That(camera.orthographic, Is.True);
                Assert.That(camera.orthographicSize, Is.GreaterThan(0f));
                Assert.That(combat.SingleFrontEnabled, Is.True);

                foreach (Vector2 resolution in SupportedResolutions)
                {
                    float aspect = resolution.x / resolution.y;
                    float halfWidth = camera.orthographicSize * aspect;
                    float left = camera.transform.position.x - halfWidth;
                    float right = camera.transform.position.x + halfWidth;
                    float bottom = camera.transform.position.y - camera.orthographicSize;
                    float top = camera.transform.position.y + camera.orthographicSize;
                    string context = $"{resolution.x:0}x{resolution.y:0}";

                    Assert.That(combat.FrontlineX, Is.InRange(left, right),
                        context + " savunma hatti gorunur kalmali.");
                    Assert.That(combat.CastleCenter.x, Is.InRange(left, right),
                        context + " kale merkezi gorunur kalmali.");
                    Assert.That(combat.SpawnLineX - right, Is.GreaterThanOrEqualTo(1f),
                        context + " zombi spawn seridi ekranin saginda gizli kalmali.");
                    Assert.That(-combat.SpawnBandYHalf, Is.GreaterThanOrEqualTo(bottom),
                        context + " spawn bandinin alt siniri kamera kapsamina uymali.");
                    Assert.That(combat.SpawnBandYHalf, Is.LessThanOrEqualTo(top),
                        context + " spawn bandinin ust siniri kamera kapsamina uymali.");
                }
            }
            finally
            {
                if (closeCombatScene && combatScene.IsValid())
                    EditorSceneManager.CloseScene(combatScene, true);
                if (closeMainScene && mainScene.IsValid())
                    EditorSceneManager.CloseScene(mainScene, true);
            }
        }

        private static Vector2 CalculateVirtualCanvasSize(float screenWidth, float screenHeight)
        {
            float widthScale = screenWidth / ReferenceResolution.x;
            float heightScale = screenHeight / ReferenceResolution.y;
            float logWeightedScale = Mathf.Lerp(
                Mathf.Log(widthScale, 2f), Mathf.Log(heightScale, 2f), 0.5f);
            float scaleFactor = Mathf.Pow(2f, logWeightedScale);
            return new Vector2(screenWidth / scaleFactor, screenHeight / scaleFactor);
        }

        private static void DestroyRootCanvasComponents(GameObject instance)
        {
            GraphicRaycaster raycaster = instance.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                UnityEngine.Object.DestroyImmediate(raycaster);

            CanvasScaler scaler = instance.GetComponent<CanvasScaler>();
            if (scaler != null)
                UnityEngine.Object.DestroyImmediate(scaler);

            Canvas canvas = instance.GetComponent<Canvas>();
            if (canvas != null)
                UnityEngine.Object.DestroyImmediate(canvas);
        }

        private static RectTransform FindUniqueRect(GameObject root, string objectName)
        {
            RectTransform[] matches = root.GetComponentsInChildren<RectTransform>(true)
                .Where(rect => rect.gameObject.name == objectName)
                .ToArray();
            Assert.That(matches.Length, Is.EqualTo(1),
                objectName + " aktif HUD prefabinda tam bir kez bulunmali.");
            return matches[0];
        }

        private static void AssertRectInsideViewport(
            RectTransform target, RectTransform viewport, string context)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);

            const float tolerance = 0.1f;
            foreach (Vector3 corner in corners)
            {
                Vector3 local = viewport.InverseTransformPoint(corner);
                Assert.That(local.x, Is.GreaterThanOrEqualTo(viewport.rect.xMin - tolerance),
                    context + " sol sinirin disina tasiyor.");
                Assert.That(local.x, Is.LessThanOrEqualTo(viewport.rect.xMax + tolerance),
                    context + " sag sinirin disina tasiyor.");
                Assert.That(local.y, Is.GreaterThanOrEqualTo(viewport.rect.yMin - tolerance),
                    context + " alt sinirin disina tasiyor.");
                Assert.That(local.y, Is.LessThanOrEqualTo(viewport.rect.yMax + tolerance),
                    context + " ust sinirin disina tasiyor.");
            }
        }

        private static Rect GetViewportBounds(RectTransform target, RectTransform viewport)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);

            Vector3 bottomLeft = viewport.InverseTransformPoint(corners[0]);
            Vector3 topRight = viewport.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
        }

        private static Scene GetOrOpenScene(string path, out bool openedByTest)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            if (scene.IsValid() && scene.isLoaded)
            {
                openedByTest = false;
                return scene;
            }

            openedByTest = true;
            return EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        private static T FindUniqueInScene<T>(Scene scene, string objectName) where T : Component
        {
            T[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .Where(component => objectName == null || component.gameObject.name == objectName)
                .ToArray();
            Assert.That(matches.Length, Is.EqualTo(1),
                $"{typeof(T).Name} {scene.path} icinde tam bir kez bulunmali.");
            return matches[0];
        }
    }
}
