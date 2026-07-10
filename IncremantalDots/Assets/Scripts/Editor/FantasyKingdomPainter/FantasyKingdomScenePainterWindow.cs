#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Fantasy Kingdom Example Scene analizcisi, multi-layer stamp extractor ve dry-run preview.
    /// Bu surum hedef sahnenin kalici tilemap katmanlarina tile yazmaz.
    /// </summary>
    public sealed partial class FantasyKingdomScenePainterWindow : EditorWindow
    {
        private const string DefaultReferenceScenePath =
            "Assets/SmallScaleInt/Fantasy kingdom Tileset/Example scene/Example scene.unity";
        private const string DefaultOutputFolder =
            "Assets/Editor/FantasyKingdomPainter/Stamps";

        [SerializeField] private SceneAsset referenceScene;
        [SerializeField] private int minimumRoofTiles = 3;
        [SerializeField] private int candidateMergeDistance = 0;
        [SerializeField] private int extractionPadding = 2;
        [SerializeField] private Vector2Int regionMin;
        [SerializeField] private Vector2Int regionSize = new Vector2Int(8, 8);
        [SerializeField] private string stampName = "FantasyKingdom_House";
        [SerializeField] private FantasyKingdomStampPurpose stampPurpose = FantasyKingdomStampPurpose.Structure;
        [SerializeField] private string outputFolder = DefaultOutputFolder;

        private FantasyKingdomAnalysisResult analysis;
        private FantasyKingdomStructureStamp lastCreatedStamp;
        private Vector2 windowScroll;
        private Vector2 layerScroll;
        private Vector2 candidateScroll;
        private bool showLayers = true;
        private bool showCandidates = true;
        private string status = "Reference Scene analizi bekleniyor.";
        private MessageType statusType = MessageType.Info;

        [MenuItem("Window/DeadWalls/Fantasy Kingdom Scene Painter")]
        public static void ShowWindow()
        {
            var window = GetWindow<FantasyKingdomScenePainterWindow>("Fantasy Kingdom Painter");
            window.minSize = new Vector2(520f, 680f);
        }

        private void OnEnable()
        {
            if (referenceScene == null)
                referenceScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(DefaultReferenceScenePath);
            if (string.IsNullOrWhiteSpace(outputFolder))
                outputFolder = DefaultOutputFolder;
            InitializePreviewState();
        }

        private void OnDisable()
        {
            DisposePreviewState();
        }

        private void OnGUI()
        {
            windowScroll = EditorGUILayout.BeginScrollView(windowScroll);
            DrawHeader();
            DrawReferenceSection();

            if (analysis != null)
            {
                DrawAnalysisSummary();
                DrawLayerSelection();
                DrawCandidateSelection();
                DrawExtractionSection();
            }

            DrawPreviewSection();
            DrawApplySection();

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(status, statusType);

            if (lastCreatedStamp != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField("Last Stamp", lastCreatedStamp, typeof(FantasyKingdomStructureStamp), false);
                    if (GUILayout.Button("Ping", GUILayout.Width(54f)))
                        EditorGUIUtility.PingObject(lastCreatedStamp);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Fantasy Kingdom Scene Painter", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "FAZ 1 + 2 + 3 — Analyzer, Multi-Layer Stamp, Dry-Run ve Safe Apply\n" +
                "Preview DontSave tilemap'lerinde gosterilir. Safe Apply yalniz tool-owned " +
                "katmanlara, tek Undo grubuyla kalici tile yazar; sahneyi otomatik kaydetmez.",
                MessageType.Info);
        }

        private void DrawReferenceSection()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("1. Reference Scene", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            referenceScene = (SceneAsset)EditorGUILayout.ObjectField(
                "Example Scene",
                referenceScene,
                typeof(SceneAsset),
                false);
            minimumRoofTiles = EditorGUILayout.IntSlider("Min Roof Component", minimumRoofTiles, 1, 30);
            candidateMergeDistance = EditorGUILayout.IntSlider("Candidate Merge", candidateMergeDistance, 0, 4);
            if (EditorGUI.EndChangeCheck())
                analysis = null;

            EditorGUI.BeginDisabledGroup(referenceScene == null || EditorApplication.isPlayingOrWillChangePlaymode);
            GUI.backgroundColor = new Color(0.55f, 0.85f, 1f);
            if (GUILayout.Button("ANALYZE REFERENCE SCENE", GUILayout.Height(34f)))
                AnalyzeReferenceScene();
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();
        }

        private void DrawAnalysisSummary()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("2. Analysis", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                string.Format(
                    "Grid: {0}\nLayout: {1} / {2}   Cell: {3}\n" +
                    "Layers: {4}   Occupied cells: {5:N0}   Structure candidates: {6}",
                    analysis.GridPath,
                    analysis.CellLayout,
                    analysis.CellSwizzle,
                    analysis.CellSize,
                    analysis.Layers.Count,
                    analysis.TotalOccupiedCells,
                    analysis.Candidates.Count),
                MessageType.None);
        }

        private void DrawLayerSelection()
        {
            showLayers = EditorGUILayout.BeginFoldoutHeaderGroup(showLayers, "3. Extraction Layers");
            if (showLayers)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Recommended"))
                        ApplyRecommendedLayerSelection();
                    if (GUILayout.Button("All Visual"))
                        ApplyVisualLayerSelection();
                    if (GUILayout.Button("Clear"))
                    {
                        for (int i = 0; i < analysis.Layers.Count; i++)
                            analysis.Layers[i].Selected = false;
                    }
                }

                layerScroll = EditorGUILayout.BeginScrollView(layerScroll, GUILayout.Height(190f));
                for (int i = 0; i < analysis.Layers.Count; i++)
                {
                    FantasyKingdomLayerAnalysis layer = analysis.Layers[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        layer.Selected = EditorGUILayout.Toggle(layer.Selected, GUILayout.Width(18f));
                        EditorGUILayout.LabelField(layer.Name, GUILayout.Width(150f));
                        EditorGUILayout.LabelField(
                            string.Format(
                                "cells:{0:N0} unique:{1} order:{2} {3}",
                                layer.OccupiedCellCount,
                                layer.UniqueTileCount,
                                layer.SortingOrder,
                                layer.RendererMode));
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawCandidateSelection()
        {
            showCandidates = EditorGUILayout.BeginFoldoutHeaderGroup(showCandidates, "4. Roof-Based Structure Candidates");
            if (showCandidates)
            {
                extractionPadding = EditorGUILayout.IntSlider("Extraction Padding", extractionPadding, 0, 8);

                candidateScroll = EditorGUILayout.BeginScrollView(candidateScroll, GUILayout.Height(220f));
                int visibleCount = Mathf.Min(analysis.Candidates.Count, 120);
                for (int i = 0; i < visibleCount; i++)
                {
                    FantasyKingdomStructureCandidate candidate = analysis.Candidates[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(candidate.DisplayName(i));
                        if (GUILayout.Button("Use", GUILayout.Width(48f)))
                            UseCandidate(candidate, i);
                    }
                }

                if (analysis.Candidates.Count > visibleCount)
                    EditorGUILayout.LabelField(
                        string.Format("First {0} / {1} candidates shown.", visibleCount, analysis.Candidates.Count),
                        EditorStyles.miniLabel);
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawExtractionSection()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("5. Extract Stamp Asset", EditorStyles.boldLabel);

            regionMin = EditorGUILayout.Vector2IntField("Region Min", regionMin);
            regionSize = EditorGUILayout.Vector2IntField("Region Size", regionSize);
            stampName = EditorGUILayout.TextField("Stamp Name", stampName);
            stampPurpose = (FantasyKingdomStampPurpose)EditorGUILayout.EnumPopup(
                "Stamp Purpose",
                stampPurpose);

            using (new EditorGUILayout.HorizontalScope())
            {
                outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
                if (GUILayout.Button("...", GUILayout.Width(32f)))
                    BrowseOutputFolder();
            }

            int selectedLayerCount = analysis.Layers.Count(layer => layer.Selected);
            bool valid = regionSize.x > 0 &&
                         regionSize.y > 0 &&
                         selectedLayerCount > 0 &&
                         !string.IsNullOrWhiteSpace(stampName) &&
                         !EditorApplication.isPlayingOrWillChangePlaymode;

            EditorGUILayout.LabelField(
                string.Format(
                    "Region: [{0},{1}] {2}x{3}   Selected layers: {4}",
                    regionMin.x,
                    regionMin.y,
                    regionSize.x,
                    regionSize.y,
                    selectedLayerCount),
                EditorStyles.miniLabel);

            EditorGUI.BeginDisabledGroup(!valid);
            GUI.backgroundColor = new Color(0.55f, 0.95f, 0.65f);
            if (GUILayout.Button("EXTRACT MULTI-LAYER STAMP", GUILayout.Height(36f)))
                ExtractStamp();
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();
        }

        private void AnalyzeReferenceScene()
        {
            string scenePath = AssetDatabase.GetAssetPath(referenceScene);
            try
            {
                EditorUtility.DisplayProgressBar(
                    "Fantasy Kingdom Painter",
                    "Reference Scene analiz ediliyor...",
                    0.35f);

                analysis = FantasyKingdomReferenceAnalyzer.Analyze(
                    scenePath,
                    minimumRoofTiles,
                    candidateMergeDistance);

                status = string.Format(
                    "Analiz tamamlandi: {0} layer, {1:N0} occupied cell, {2} structure candidate.",
                    analysis.Layers.Count,
                    analysis.TotalOccupiedCells,
                    analysis.Candidates.Count);
                statusType = MessageType.Info;
            }
            catch (Exception exception)
            {
                analysis = null;
                status = "Analiz basarisiz: " + exception.Message;
                statusType = MessageType.Error;
                Debug.LogException(exception);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void UseCandidate(FantasyKingdomStructureCandidate candidate, int index)
        {
            RectInt bounds = candidate.RoofBounds;
            regionMin = new Vector2Int(
                bounds.xMin - extractionPadding,
                bounds.yMin - extractionPadding);
            regionSize = new Vector2Int(
                bounds.width + extractionPadding * 2,
                bounds.height + extractionPadding * 2);
            stampName = string.Format(
                "FK_Structure_{0:00}_{1}_{2}",
                index + 1,
                bounds.xMin,
                bounds.yMin);
            status = "Candidate extraction region'e aktarildi. Layer secimini kontrol edip stamp cikarabilirsin.";
            statusType = MessageType.Info;
        }

        private void ExtractStamp()
        {
            try
            {
                EditorUtility.DisplayProgressBar(
                    "Fantasy Kingdom Painter",
                    "Multi-layer stamp cikariliyor...",
                    0.65f);

                var region = new RectInt(regionMin, regionSize);
                string assetPath = FantasyKingdomReferenceAnalyzer.ExtractStamp(
                    analysis,
                    region,
                    stampName,
                    stampPurpose,
                    outputFolder);

                lastCreatedStamp = AssetDatabase.LoadAssetAtPath<FantasyKingdomStructureStamp>(assetPath);
                status = string.Format(
                    "Stamp olusturuldu: {0} ({1} layer / {2} tile)",
                    assetPath,
                    lastCreatedStamp != null ? lastCreatedStamp.Layers.Count : 0,
                    lastCreatedStamp != null ? lastCreatedStamp.TotalTileCount : 0);
                statusType = MessageType.Info;
                if (lastCreatedStamp != null)
                    EditorGUIUtility.PingObject(lastCreatedStamp);
            }
            catch (Exception exception)
            {
                status = "Extraction basarisiz: " + exception.Message;
                statusType = MessageType.Error;
                Debug.LogException(exception);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void ApplyRecommendedLayerSelection()
        {
            string[] names =
            {
                "Walls",
                "Roof1",
                "Roof2",
                "Roof3",
                "WallDetail1",
                "WallDetail2",
                "Objects",
                "BrokenObjects",
                "Shadows1",
                "Shadows2",
                "LowerShadows",
                "Ground 2",
                "Ground 3"
            };

            for (int i = 0; i < analysis.Layers.Count; i++)
                analysis.Layers[i].Selected = names.Contains(
                    analysis.Layers[i].Name,
                    StringComparer.OrdinalIgnoreCase);
        }

        private void ApplyVisualLayerSelection()
        {
            for (int i = 0; i < analysis.Layers.Count; i++)
            {
                string name = analysis.Layers[i].Name;
                bool technical = name.IndexOf("Collider", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("TileCheck", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("BuildPreview", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("indestructible", StringComparison.OrdinalIgnoreCase) >= 0;
                analysis.Layers[i].Selected = !technical;
            }
        }

        private void BrowseOutputFolder()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/');
            string currentAbsolute = Path.Combine(projectRoot, outputFolder).Replace('\\', '/');
            string selected = EditorUtility.OpenFolderPanel(
                "Fantasy Kingdom Stamp Output",
                Directory.Exists(currentAbsolute) ? currentAbsolute : Application.dataPath,
                string.Empty);

            if (string.IsNullOrEmpty(selected))
                return;

            selected = selected.Replace('\\', '/');
            if (!selected.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                status = "Output folder proje kokunun icinde olmalidir.";
                statusType = MessageType.Warning;
                return;
            }

            string relative = selected.Substring(projectRoot.Length + 1);
            if (!relative.StartsWith("Assets", StringComparison.Ordinal))
            {
                status = "Stamp assetleri Assets/ altinda tutulmalidir.";
                statusType = MessageType.Warning;
                return;
            }

            outputFolder = relative;
        }
    }
}
#endif
