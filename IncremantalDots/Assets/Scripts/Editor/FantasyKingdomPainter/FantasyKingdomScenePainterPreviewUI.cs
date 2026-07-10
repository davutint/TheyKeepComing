#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadWalls
{
    public sealed partial class FantasyKingdomScenePainterWindow
    {
        private const string DefaultPreviewStampPath =
            "Assets/Editor/FantasyKingdomPainter/Stamps/FK_Reference_StoneHouse_A.asset";

        [SerializeField] private FantasyKingdomStructureStamp previewStamp;
        [SerializeField] private Grid previewTargetGrid;
        [SerializeField] private Vector3Int previewOrigin;
        [SerializeField] private Color previewTint = new Color(1f, 1f, 1f, 0.92f);
        [SerializeField] private string previewSortingLayer = "Objects";
        [SerializeField] private int previewBaseSortingOrder = 1000;

        private FantasyKingdomPreviewReport previewReport;
        private FantasyKingdomApplyReport lastApplyReport;
        private FantasyKingdomStructureStamp previewedStamp;
        private Grid previewedGrid;
        private Vector3Int previewedOrigin;
        private bool showPreview = true;
        private bool showApply = true;

        private void InitializePreviewState()
        {
            if (previewStamp == null)
                previewStamp = AssetDatabase.LoadAssetAtPath<FantasyKingdomStructureStamp>(DefaultPreviewStampPath);
            RefreshDefaultPreviewGrid();
        }

        private void DisposePreviewState()
        {
            InvalidatePreview(previewTargetGrid);
        }

        private void DrawPreviewSection()
        {
            EditorGUILayout.Space(10f);
            showPreview = EditorGUILayout.BeginFoldoutHeaderGroup(
                showPreview,
                "6. Dry-Run Preview (Phase 2)");

            if (showPreview)
            {
                EditorGUILayout.HelpBox(
                    "Stamp yalniz __FKPreviewRoot altindaki DontSave tilemap'lerine yazilir. " +
                    "Gercek Grid katmanlari ve scene dosyasi degismez.",
                    MessageType.Info);

                EditorGUI.BeginChangeCheck();
                FantasyKingdomStructureStamp nextStamp =
                    (FantasyKingdomStructureStamp)EditorGUILayout.ObjectField(
                        "Stamp",
                        previewStamp,
                        typeof(FantasyKingdomStructureStamp),
                        false);
                if (EditorGUI.EndChangeCheck())
                {
                    InvalidatePreview(previewTargetGrid);
                    previewStamp = nextStamp;
                }
                else
                {
                    previewStamp = nextStamp;
                }

                if (previewStamp != null)
                    EditorGUILayout.LabelField("Purpose", previewStamp.Purpose.ToString(), EditorStyles.miniLabel);

                Grid previousGrid = previewTargetGrid;
                Grid nextGrid;
                bool gridChanged;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    nextGrid = (Grid)EditorGUILayout.ObjectField(
                        "Target Grid",
                        previewTargetGrid,
                        typeof(Grid),
                        true);
                    gridChanged = EditorGUI.EndChangeCheck();
                    if (GUILayout.Button("Active Grid", GUILayout.Width(86f)))
                        RefreshDefaultPreviewGrid();
                }
                if (gridChanged)
                {
                    InvalidatePreview(previousGrid);
                    previewTargetGrid = nextGrid;
                }

                EditorGUI.BeginChangeCheck();
                Vector3Int nextOrigin = EditorGUILayout.Vector3IntField("Target Origin", previewOrigin);
                if (EditorGUI.EndChangeCheck())
                {
                    InvalidatePreview(previewTargetGrid);
                    previewOrigin = nextOrigin;
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Origin From Scene View"))
                        UseSceneViewPivotAsOrigin();
                    if (GUILayout.Button("Origin From Selection"))
                        UseSelectionAsOrigin();
                }

                DrawPreviewNudgeButtons();
                EditorGUI.BeginChangeCheck();
                previewTint = EditorGUILayout.ColorField("Preview Tint", previewTint);
                previewSortingLayer = EditorGUILayout.TextField("Preview Sorting Layer", previewSortingLayer);
                previewBaseSortingOrder = EditorGUILayout.IntField("Preview Base Order", previewBaseSortingOrder);
                if (EditorGUI.EndChangeCheck())
                    InvalidatePreview(previewTargetGrid);

                bool canPreview = previewStamp != null &&
                                  previewTargetGrid != null &&
                                  !EditorApplication.isPlayingOrWillChangePlaymode;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginDisabledGroup(!canPreview);
                    GUI.backgroundColor = new Color(0.55f, 0.85f, 1f);
                    if (GUILayout.Button("CREATE / UPDATE PREVIEW", GUILayout.Height(34f)))
                        CreateOrUpdatePreview();
                    GUI.backgroundColor = Color.white;
                    EditorGUI.EndDisabledGroup();

                    EditorGUI.BeginDisabledGroup(previewTargetGrid == null);
                    if (GUILayout.Button("CLEAR PREVIEW", GUILayout.Height(34f), GUILayout.Width(130f)))
                        ClearPreview();
                    EditorGUI.EndDisabledGroup();
                }

                if (previewReport != null)
                {
                    MessageType reportType = previewReport.HasProtectedConflict
                        ? MessageType.Warning
                        : previewReport.BlockingOverlapCellCount > 0
                            ? MessageType.Info
                            : MessageType.None;
                    EditorGUILayout.HelpBox(previewReport.BuildSummary(), reportType);
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawApplySection()
        {
            EditorGUILayout.Space(8f);
            showApply = EditorGUILayout.BeginFoldoutHeaderGroup(
                showApply,
                "7. Safe Apply (Phase 3)");

            if (showApply)
            {
                EditorGUILayout.HelpBox(
                    "Purpose'a gore yalniz Grid/FK_PaintedStructures veya FK_PaintedBattlefield " +
                    "altindaki tool-owned katmanlara yazar. " +
                    "Mevcut Grass/Structures/outside katmanlarini silmez. Islem tek Undo grubudur; " +
                    "scene otomatik kaydedilmez.",
                    MessageType.Info);

                bool previewCurrent = IsPreviewCurrent();
                bool hasReport = previewReport != null;
                bool hasProtectedConflict = hasReport && previewReport.HasProtectedConflict;
                bool hasBlockingConflict = hasReport && previewReport.HasBlockingConflict;
                bool canApply = previewCurrent &&
                                !hasProtectedConflict &&
                                !hasBlockingConflict &&
                                !EditorApplication.isPlayingOrWillChangePlaymode;

                if (!hasReport)
                {
                    EditorGUILayout.HelpBox(
                        "Safe Apply icin once guncel bir dry-run preview olustur.",
                        MessageType.None);
                }
                else if (!previewCurrent)
                {
                    EditorGUILayout.HelpBox(
                        "Stamp, Grid veya origin degisti. Apply oncesi preview'u yenile.",
                        MessageType.Warning);
                }
                else if (hasProtectedConflict)
                {
                    EditorGUILayout.HelpBox(
                        "Apply kilitli: outside/VillageMarkers, sol-sag bolge veya zemin destegi " +
                        "kurali ihlali var.",
                        MessageType.Warning);
                }
                else if (hasBlockingConflict)
                {
                    EditorGUILayout.HelpBox(
                        "Apply kilitli: mevcut yapisal tile'larla cakisma var. Origin'i tasi.",
                        MessageType.Warning);
                }

                EditorGUI.BeginDisabledGroup(!canApply);
                GUI.backgroundColor = new Color(0.55f, 0.95f, 0.65f);
                if (GUILayout.Button("SAFE APPLY STAMP (UNDOABLE)", GUILayout.Height(38f)))
                    ApplyStampSafely();
                GUI.backgroundColor = Color.white;
                EditorGUI.EndDisabledGroup();

                if (lastApplyReport != null)
                {
                    EditorGUILayout.HelpBox(
                        lastApplyReport.BuildSummary() +
                        "\nScene dirty; kaydetme karari kullanicida. Ctrl+Z ile geri alinabilir.",
                        MessageType.Info);
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawPreviewNudgeButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Nudge", GUILayout.Width(70f));
                if (GUILayout.Button("X-")) NudgePreview(Vector3Int.left);
                if (GUILayout.Button("X+")) NudgePreview(Vector3Int.right);
                if (GUILayout.Button("Y-")) NudgePreview(Vector3Int.down);
                if (GUILayout.Button("Y+")) NudgePreview(Vector3Int.up);
            }
        }

        private void RefreshDefaultPreviewGrid()
        {
            Grid nextGrid = FantasyKingdomStampPreviewService.FindDefaultTargetGrid(
                SceneManager.GetActiveScene());
            if (previewTargetGrid != null && previewTargetGrid != nextGrid)
                FantasyKingdomStampPreviewService.ClearPreview(previewTargetGrid);
            previewTargetGrid = nextGrid;
            ResetPreviewSignature();
        }

        private void UseSceneViewPivotAsOrigin()
        {
            if (previewTargetGrid == null)
            {
                status = "Aktif sahnede target Grid bulunamadi.";
                statusType = MessageType.Warning;
                return;
            }

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                status = "Acik bir Scene View bulunamadi.";
                statusType = MessageType.Warning;
                return;
            }

            SetPreviewOrigin(
                previewTargetGrid.WorldToCell(sceneView.pivot),
                "Preview origin Scene View pivot'undan alindi: ");
        }

        private void UseSelectionAsOrigin()
        {
            if (previewTargetGrid == null || Selection.activeTransform == null)
            {
                status = "Target Grid ve sahnede secili bir Transform gerekli.";
                statusType = MessageType.Warning;
                return;
            }

            SetPreviewOrigin(
                previewTargetGrid.WorldToCell(Selection.activeTransform.position),
                "Preview origin secili objeden alindi: ");
        }

        private void NudgePreview(Vector3Int delta)
        {
            previewOrigin += delta;
            if (previewReport != null && previewStamp != null && previewTargetGrid != null)
                CreateOrUpdatePreview();
        }

        private void SetPreviewOrigin(Vector3Int nextOrigin, string statusPrefix)
        {
            bool refreshPreview = previewReport != null;
            previewOrigin = nextOrigin;
            if (refreshPreview && previewStamp != null && previewTargetGrid != null)
            {
                CreateOrUpdatePreview();
                return;
            }

            InvalidatePreview(previewTargetGrid);
            status = statusPrefix + previewOrigin;
            statusType = MessageType.Info;
        }

        private void CreateOrUpdatePreview()
        {
            try
            {
                previewReport = FantasyKingdomStampPreviewService.CreateOrUpdatePreview(
                    previewStamp,
                    previewTargetGrid,
                    previewOrigin,
                    previewTint,
                    previewSortingLayer,
                    previewBaseSortingOrder);
                previewedStamp = previewStamp;
                previewedGrid = previewTargetGrid;
                previewedOrigin = previewOrigin;

                status = "Dry-run preview guncellendi. Scene dosyasina kalici tile yazilmadi.";
                statusType = previewReport.HasProtectedConflict
                    ? MessageType.Warning
                    : MessageType.Info;
                SceneView.RepaintAll();
            }
            catch (Exception exception)
            {
                ResetPreviewSignature();
                status = "Preview basarisiz: " + exception.Message;
                statusType = MessageType.Error;
                Debug.LogException(exception);
            }
        }

        private void ClearPreview()
        {
            InvalidatePreview(previewTargetGrid);
            status = "Dry-run preview temizlendi.";
            statusType = MessageType.Info;
            SceneView.RepaintAll();
        }

        private void ApplyStampSafely()
        {
            try
            {
                lastApplyReport = FantasyKingdomStampApplyService.ApplySafely(
                    previewStamp,
                    previewTargetGrid,
                    previewOrigin);
                ResetPreviewSignature();
                status = "Stamp tool-owned katmanlara uygulandi. Scene kaydedilmedi; Ctrl+Z aktif.";
                statusType = MessageType.Info;
                SceneView.RepaintAll();
            }
            catch (Exception exception)
            {
                status = "Safe Apply basarisiz: " + exception.Message;
                statusType = MessageType.Error;
                Debug.LogException(exception);
            }
        }

        private bool IsPreviewCurrent()
        {
            return previewReport != null &&
                   previewedStamp == previewStamp &&
                   previewedGrid == previewTargetGrid &&
                   previewedOrigin == previewOrigin;
        }

        private void InvalidatePreview(Grid gridToClear)
        {
            FantasyKingdomStampPreviewService.ClearPreview(gridToClear);
            ResetPreviewSignature();
        }

        private void ResetPreviewSignature()
        {
            previewReport = null;
            previewedStamp = null;
            previewedGrid = null;
            previewedOrigin = default;
        }
    }
}
#endif
