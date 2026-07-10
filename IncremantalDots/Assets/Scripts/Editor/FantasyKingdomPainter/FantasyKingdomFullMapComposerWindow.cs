#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DeadWalls
{
    public sealed class FantasyKingdomFullMapComposerWindow : EditorWindow
    {
        private const float OverlayMinY = -8f;
        private const float OverlayMaxY = 8f;

        [SerializeField] private FantasyKingdomMapLayout layout;
        [SerializeField] private Grid targetGrid;
        [SerializeField] private bool showLayoutAsset = true;
        [SerializeField] private bool showSceneOverlay = true;
        [SerializeField] private Vector2 scroll;

        private FantasyKingdomFullMapPreviewReport report;
        private string status = "V3 render-band replacement preview hazir.";
        private MessageType statusType = MessageType.Info;

        [MenuItem("Window/DeadWalls/Fantasy Kingdom Full Map Composer")]
        public static void Open()
        {
            GetWindow<FantasyKingdomFullMapComposerWindow>(
                "FK Full Map Composer").Show();
        }

        private void OnEnable()
        {
            if (layout == null)
                layout = FantasyKingdomMapLayoutFactory.LoadDefault();
            RefreshTargetGrid();
            SceneView.duringSceneGui += DuringSceneGui;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGui;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            FantasyKingdomFullMapPreviewService.ClearPreview(targetGrid);
        }

        private void OnHierarchyChange()
        {
            if (targetGrid == null)
                RefreshTargetGrid();
            Repaint();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Fantasy Kingdom Full Map Composer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "V3 - all-stone castle, clear resource sites, dense living/enemy forests, " +
                "calm battlefield and S-shaped caravan road. Placement'lar Ground (z=0), " +
                "Behind Units (z=0), Unit (z=-1) ve Front Occluder (z=-2) sozlesmesini kullanir. " +
                "Bu pencerenin preview kontrolleri sahneyi degistirmez. Owner onayli V3 icin " +
                "kalici apply ayri Fantasy Kingdom menu komutundadir ve sahneyi otomatik kaydetmez.",
                MessageType.Info);

            DrawTargetSection();
            DrawLayoutSection();
            DrawActions();
            DrawReport();

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(status, statusType);
            EditorGUILayout.EndScrollView();
        }

        private void DrawTargetSection()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            FantasyKingdomMapLayout nextLayout =
                (FantasyKingdomMapLayout)EditorGUILayout.ObjectField(
                    "Map Layout",
                    layout,
                    typeof(FantasyKingdomMapLayout),
                    false);
            Grid nextGrid = (Grid)EditorGUILayout.ObjectField(
                "Target Grid",
                targetGrid,
                typeof(Grid),
                true);
            if (EditorGUI.EndChangeCheck())
            {
                FantasyKingdomFullMapPreviewService.ClearPreview(targetGrid);
                layout = nextLayout;
                targetGrid = nextGrid;
                report = null;
                SceneView.RepaintAll();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("LOAD / CREATE DEFAULT V3"))
            {
                try
                {
                    layout = FantasyKingdomMapLayoutFactory.CreateOrLoadDefault();
                    Selection.activeObject = layout;
                    status = "Default full-map draft hazir: " +
                             FantasyKingdomMapLayoutFactory.DefaultLayoutPath;
                    statusType = MessageType.Info;
                }
                catch (Exception exception)
                {
                    status = "Default layout olusturulamadi: " + exception.Message;
                    statusType = MessageType.Error;
                }
            }
            if (GUILayout.Button("USE ACTIVE SCENE GRID"))
                RefreshTargetGrid();
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("REBUILD APPROVED V3 RECIPE ASSETS"))
            {
                try
                {
                    FantasyKingdomFullMapPreviewService.ClearPreview(targetGrid);
                    layout = FantasyKingdomV3MapDraftBuilder.CreateOrRefreshDraft();
                    Selection.activeObject = layout;
                    status = "Onayli V3 asset recetesi yeniden uretildi; sahne verisi degismedi.";
                    statusType = MessageType.Info;
                }
                catch (Exception exception)
                {
                    status = "V3 recetesi uretilemedi: " + exception.Message;
                    statusType = MessageType.Error;
                }
            }

            showSceneOverlay = EditorGUILayout.ToggleLeft(
                "Scene View zone / marker / corridor overlay",
                showSceneOverlay);
        }

        private void DrawLayoutSection()
        {
            showLayoutAsset = EditorGUILayout.BeginFoldoutHeaderGroup(
                showLayoutAsset,
                "Layout Recipe");
            if (showLayoutAsset && layout != null)
            {
                var serializedLayout = new SerializedObject(layout);
                serializedLayout.Update();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(serializedLayout.FindProperty("schemaVersion"));
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedLayout.FindProperty("targetScene"));
                EditorGUILayout.PropertyField(serializedLayout.FindProperty("targetGridPath"));
                EditorGUILayout.PropertyField(serializedLayout.FindProperty("profileId"));
                EditorGUILayout.PropertyField(serializedLayout.FindProperty("seed"));
                EditorGUILayout.PropertyField(
                    serializedLayout.FindProperty("placements"),
                    true);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedLayout.ApplyModifiedProperties();
                    FantasyKingdomFullMapPreviewService.ClearPreview(targetGrid);
                    report = null;
                    status = "Layout degisti; full preview yeniden uretilmeli.";
                    statusType = MessageType.Warning;
                    SceneView.RepaintAll();
                }
                else
                {
                    serializedLayout.ApplyModifiedProperties();
                }
            }
            else if (showLayoutAsset)
            {
                EditorGUILayout.HelpBox(
                    "Bir layout sec veya default draft olustur.",
                    MessageType.Warning);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Dry-Run", EditorStyles.boldLabel);
            bool canRun = layout != null && targetGrid != null &&
                          !EditorApplication.isPlayingOrWillChangePlaymode;
            EditorGUI.BeginDisabledGroup(!canRun);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("CREATE / UPDATE FULL PREVIEW", GUILayout.Height(28f)))
                CreateOrUpdatePreview();
            if (GUILayout.Button("ANALYZE ONLY", GUILayout.Height(28f)))
                AnalyzeOnly();
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("CLEAR FULL PREVIEW"))
            {
                FantasyKingdomFullMapPreviewService.ClearPreview(targetGrid);
                report = null;
                status = "Full preview temizlendi. Sahne tilemap'leri degismedi.";
                statusType = MessageType.Info;
            }

            EditorGUILayout.HelpBox(
                "V3 render bandlari: Ground z=0, zombie/unit z=-1, front forest occluder z=-2. " +
                "Legacy visual overlap, kalici apply oncesi emekliye ayrilacak eski gorsel " +
                "hucreleri gosteren migration warning'idir; gorsel kalite onayi sayilmaz. " +
                "outside*, marker, zone, " +
                "canonical layer ve solid-footprint cakismalari hard conflict'tir. " +
                "Settlement'in 16:9 referans viewport disina tasmasi ve marker->keep duz " +
                "referans hatti riski warning'dir; tam yapilar bu hat icin kesilmez. Full " +
                "preview GroundDetail/Structures/OverlayProps/Roof* legacy renderer'larini gecici " +
                "gizler; clear, reload ve Play Mode gecisi tum durumlari geri yukler.",
                MessageType.None);
        }

        private void DrawReport()
        {
            if (report == null)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Aggregate Contract Report", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                report.BuildSummary(),
                report.HasHardConflicts ? MessageType.Error :
                report.WarningCount > 0 ? MessageType.Warning : MessageType.Info);

            for (int i = 0; i < report.Issues.Count; i++)
            {
                FantasyKingdomFullMapIssue issue = report.Issues[i];
                EditorGUILayout.HelpBox(
                    issue.ToString(),
                    issue.Severity == FantasyKingdomFullMapIssueSeverity.Error
                        ? MessageType.Error
                        : MessageType.Warning);
            }
        }

        private void CreateOrUpdatePreview()
        {
            try
            {
                report = FantasyKingdomFullMapPreviewService.CreateOrUpdatePreview(
                    layout,
                    targetGrid);
                status = report.HasHardConflicts
                    ? "Full preview olustu; hard conflict'ler duzeltilmeden kalici faza gecilmez."
                    : "Full preview olustu. Kalici apply bu fazda bilincli olarak sunulmuyor.";
                statusType = report.HasHardConflicts ? MessageType.Error :
                    report.WarningCount > 0 ? MessageType.Warning : MessageType.Info;
            }
            catch (Exception exception)
            {
                report = null;
                status = "Full preview basarisiz: " + exception.Message;
                statusType = MessageType.Error;
            }
        }

        private void AnalyzeOnly()
        {
            try
            {
                report = FantasyKingdomFullMapPreviewService.AnalyzeLayout(layout, targetGrid);
                status = "Aggregate analiz tamamlandi; preview tilemap'i uretilmedi.";
                statusType = report.HasHardConflicts ? MessageType.Error :
                    report.WarningCount > 0 ? MessageType.Warning : MessageType.Info;
            }
            catch (Exception exception)
            {
                report = null;
                status = "Analiz basarisiz: " + exception.Message;
                statusType = MessageType.Error;
            }
        }

        private void RefreshTargetGrid()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            Grid next = FantasyKingdomStampPreviewService.FindDefaultTargetGrid(activeScene);
            if (targetGrid != next)
                FantasyKingdomFullMapPreviewService.ClearPreview(targetGrid);
            targetGrid = next;
            report = null;
            status = targetGrid != null
                ? "Active scene target Grid bulundu: " + targetGrid.name
                : "Active scene icinde target Grid bulunamadi.";
            statusType = targetGrid != null ? MessageType.Info : MessageType.Warning;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                FantasyKingdomFullMapPreviewService.ClearPreview(targetGrid);
        }

        private void DuringSceneGui(SceneView sceneView)
        {
            if (!showSceneOverlay || targetGrid == null ||
                !targetGrid.gameObject.scene.IsValid() ||
                targetGrid.gameObject.scene != SceneManager.GetActiveScene())
            {
                return;
            }

            Color previousColor = Handles.color;
            Matrix4x4 previousMatrix = Handles.matrix;
            CompareFunction previousZTest = Handles.zTest;
            try
            {
                Handles.matrix = Matrix4x4.identity;
                Handles.zTest = CompareFunction.Always;
                DrawZone(-8.22f, -1.5f, new Color(0.2f, 0.65f, 0.35f, 0.045f), "SETTLEMENT");
                DrawZone(-1.5f, 1.5f, new Color(1f, 0.3f, 0.25f, 0.055f), "WALL CLEAR");
                DrawZone(1.5f, 4f, new Color(0.15f, 0.55f, 1f, 0.05f), "MOAT");
                DrawZone(4f, 18f, new Color(0.85f, 0.55f, 0.15f, 0.04f), "BATTLEFIELD");
                DrawZone(18f, 29f, new Color(0.15f, 0.45f, 0.2f, 0.045f), "ENEMY FOREST FRAME");
                DrawZone(27f, 29f, new Color(0.8f, 0.2f, 0.8f, 0.055f), "HIDDEN SPAWN");

                Handles.color = new Color(0.85f, 0.85f, 0.85f, 0.75f);
                Handles.DrawDottedLine(
                    new Vector3(-8.22f, OverlayMinY, 0f),
                    new Vector3(-8.22f, OverlayMaxY, 0f),
                    4f);
                Handles.Label(new Vector3(-8.22f, OverlayMaxY - 2.1f, 0f), "REF 16:9 LEFT EDGE");

                Handles.DrawDottedLine(
                    new Vector3(20.22f, OverlayMinY, 0f),
                    new Vector3(20.22f, OverlayMaxY, 0f),
                    4f);
                Handles.Label(new Vector3(20.22f, OverlayMaxY - 2.1f, 0f), "REF 16:9 RIGHT EDGE");

                Handles.color = new Color(0.2f, 0.8f, 1f, 0.85f);
                Handles.DrawDottedLine(
                    new Vector3(25.2f, OverlayMinY, 0f),
                    new Vector3(25.2f, OverlayMaxY, 0f),
                    4f);
                Handles.Label(new Vector3(25.2f, OverlayMaxY - 2.9f, 0f), "MAX 2.4 EDGE");

                Handles.color = new Color(1f, 0.2f, 0.2f, 0.9f);
                Handles.DrawAAPolyLine(
                    3f,
                    new Vector3(-0.5f, OverlayMinY, 0f),
                    new Vector3(-0.5f, OverlayMaxY, 0f));
                Handles.Label(new Vector3(-0.5f, OverlayMaxY - 0.35f, 0f), "FRONTLINE -0.5");

                DrawMarkersAndCorridors();
                DrawPlacementAnchors();
            }
            finally
            {
                Handles.color = previousColor;
                Handles.matrix = previousMatrix;
                Handles.zTest = previousZTest;
            }
        }

        private static void DrawZone(float minX, float maxX, Color color, string label)
        {
            var vertices = new[]
            {
                new Vector3(minX, OverlayMinY, 0f),
                new Vector3(minX, OverlayMaxY, 0f),
                new Vector3(maxX, OverlayMaxY, 0f),
                new Vector3(maxX, OverlayMinY, 0f)
            };
            Color outline = new Color(color.r, color.g, color.b, 0.45f);
            Handles.DrawSolidRectangleWithOutline(vertices, color, outline);
            Handles.Label(new Vector3(minX + 0.1f, OverlayMaxY - 0.7f, 0f), label);
        }

        private void DrawMarkersAndCorridors()
        {
            GameObject root = targetGrid.gameObject.scene.GetRootGameObjects()
                .FirstOrDefault(item => string.Equals(
                    item.name,
                    "VillageMarkers",
                    StringComparison.Ordinal));
            if (root == null)
                return;

            Transform keep = root.transform.Find("CastleKeepMarker");
            string[] names =
            {
                "CastleKeepMarker",
                "WoodSiteMarker",
                "StoneSiteMarker",
                "FoodSiteMarker",
                "IronSiteMarker"
            };

            for (int i = 0; i < names.Length; i++)
            {
                Transform marker = root.transform.Find(names[i]);
                if (marker == null)
                    continue;

                Handles.color = names[i] == "CastleKeepMarker"
                    ? new Color(1f, 0.85f, 0.2f, 1f)
                    : new Color(0.25f, 1f, 0.7f, 1f);
                float size = HandleUtility.GetHandleSize(marker.position) * 0.07f;
                Handles.DrawWireDisc(marker.position, Vector3.forward, size);
                Handles.Label(marker.position + Vector3.up * size, names[i].Replace("SiteMarker", string.Empty));

                if (keep != null && marker != keep)
                {
                    Handles.color = new Color(0.25f, 1f, 0.7f, 0.45f);
                    Handles.DrawDottedLine(marker.position, keep.position, 4f);
                }
            }
        }

        private void DrawPlacementAnchors()
        {
            if (layout == null)
                return;

            IReadOnlyList<FantasyKingdomMapPlacement> placements = layout.Placements;
            for (int i = 0; i < placements.Count; i++)
            {
                FantasyKingdomMapPlacement placement = placements[i];
                if (placement == null || !placement.Enabled || placement.Stamp == null)
                    continue;
                Vector3 world = targetGrid.GetCellCenterWorld(placement.TargetAnchorCell);
                Handles.color = new Color(1f, 1f, 1f, 0.9f);
                float size = HandleUtility.GetHandleSize(world) * 0.045f;
                Handles.DrawWireCube(world, Vector3.one * size);
                Handles.Label(
                    world + Vector3.up * size,
                    (string.IsNullOrEmpty(placement.Label) ? placement.Id : placement.Label) +
                    " [" + placement.RenderBand + "]");
            }
        }
    }
}
#endif
