#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DeadWalls
{
    public enum FantasyKingdomMapZone
    {
        Settlement = 0,
        Battlefield = 1,
        MoatGround = 2,
        SpawnGround = 3,
        FarRightFrame = 4
    }

    public enum FantasyKingdomGameplayAnchor
    {
        None = 0,
        CastleKeep = 1,
        Wood = 2,
        Stone = 3,
        Food = 4,
        Iron = 5
    }

    public enum FantasyKingdomRenderBand
    {
        LegacyAuto = 0,
        Ground = 1,
        BehindUnits = 2,
        InFrontOfUnits = 3
    }

    [Serializable]
    public sealed class FantasyKingdomMapPlacement
    {
        [SerializeField] private string id;
        [SerializeField] private string label;
        [SerializeField] private bool enabled = true;
        [SerializeField] private FantasyKingdomStructureStamp stamp;
        [SerializeField] private Vector3Int targetAnchorCell;
        [SerializeField] private FantasyKingdomMapZone zone;
        [SerializeField] private FantasyKingdomGameplayAnchor gameplayAnchor;
        [SerializeField] private FantasyKingdomRenderBand renderBand;

        public string Id => id;
        public string Label => label;
        public bool Enabled => enabled;
        public FantasyKingdomStructureStamp Stamp => stamp;
        public Vector3Int TargetAnchorCell => targetAnchorCell;
        public FantasyKingdomMapZone Zone => zone;
        public FantasyKingdomGameplayAnchor GameplayAnchor => gameplayAnchor;
        public FantasyKingdomRenderBand RenderBand => renderBand;

        internal FantasyKingdomMapPlacement(
            string stableId,
            string displayLabel,
            FantasyKingdomStructureStamp sourceStamp,
            Vector3Int anchorCell,
            FantasyKingdomMapZone mapZone,
            FantasyKingdomGameplayAnchor anchor,
            FantasyKingdomRenderBand placementRenderBand = FantasyKingdomRenderBand.LegacyAuto)
        {
            id = stableId;
            label = displayLabel;
            enabled = true;
            stamp = sourceStamp;
            targetAnchorCell = anchorCell;
            zone = mapZone;
            gameplayAnchor = anchor;
            renderBand = placementRenderBand;
        }
    }

    /// <summary>
    /// Tam harita gorsel taslaginin editor-only, tekrar uretilebilir yerlesim recetesi.
    /// Sahne tilemap verisi tasimaz ve kendi basina sahneyi degistirmez.
    /// </summary>
    [CreateAssetMenu(
        fileName = "FantasyKingdomMapLayout",
        menuName = "DeadWalls/Editor/Fantasy Kingdom Map Layout")]
    public sealed class FantasyKingdomMapLayout : ScriptableObject
    {
        public const int MinimumSupportedSchemaVersion = 1;
        public const int CurrentSchemaVersion = 2;

        [SerializeField] private int schemaVersion = 1;
        [SerializeField] private SceneAsset targetScene;
        [SerializeField] private string targetGridPath = "Grid";
        [SerializeField] private string profileId = "NewGameScene-VisualRebuild-v2";
        [SerializeField] private int seed = 1072026;
        [SerializeField] private List<FantasyKingdomMapPlacement> placements =
            new List<FantasyKingdomMapPlacement>();

        public int SchemaVersion => schemaVersion;
        public SceneAsset TargetScene => targetScene;
        public string TargetScenePath => targetScene != null
            ? AssetDatabase.GetAssetPath(targetScene)
            : string.Empty;
        public string TargetGridPath => targetGridPath;
        public string ProfileId => profileId;
        public int Seed => seed;
        public IReadOnlyList<FantasyKingdomMapPlacement> Placements => placements;

        internal void Initialize(
            SceneAsset scene,
            string gridPath,
            string profile,
            int layoutSeed,
            List<FantasyKingdomMapPlacement> layoutPlacements,
            int layoutSchemaVersion = MinimumSupportedSchemaVersion)
        {
            schemaVersion = layoutSchemaVersion;
            targetScene = scene;
            targetGridPath = gridPath;
            profileId = profile;
            seed = layoutSeed;
            placements = layoutPlacements ?? new List<FantasyKingdomMapPlacement>();
        }
    }

    internal static class FantasyKingdomMapLayoutFactory
    {
        public const string LegacyV2LayoutPath =
            "Assets/Editor/FantasyKingdomPainter/Layouts/FK_NewGameScene_FullMap_Draft.asset";
        public const string DefaultLayoutPath =
            "Assets/Editor/FantasyKingdomPainter/Layouts/FK_NewGameScene_FullMap_V3_Draft.asset";

        private const string TargetScenePath = "Assets/Scenes/NewGameScene.unity";
        private const string StampFolder = "Assets/Editor/FantasyKingdomPainter/Stamps/";

        public static FantasyKingdomMapLayout LoadDefault()
        {
            return AssetDatabase.LoadAssetAtPath<FantasyKingdomMapLayout>(DefaultLayoutPath);
        }

        public static FantasyKingdomMapLayout CreateOrLoadDefault()
        {
            FantasyKingdomMapLayout existing = LoadDefault();
            if (existing != null)
                return existing;

            return FantasyKingdomV3MapDraftBuilder.CreateOrRefreshDraft();
        }

        public static FantasyKingdomMapLayout CreateOrLoadLegacyV2()
        {
            FantasyKingdomMapLayout existing =
                AssetDatabase.LoadAssetAtPath<FantasyKingdomMapLayout>(LegacyV2LayoutPath);
            if (existing != null)
                return existing;

            SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath);
            if (scene == null)
                throw new InvalidOperationException("NewGameScene asset'i bulunamadi: " + TargetScenePath);

            FantasyKingdomStructureStamp keep = LoadStamp("FK_PreservedVillageKeep_A.asset");
            FantasyKingdomStructureStamp house = LoadStamp("FK_LogCabin_House_A.asset");
            FantasyKingdomStructureStamp workshop = LoadStamp("FK_StoneEntry_Workshop_A.asset");
            FantasyKingdomStructureStamp lowRuin = LoadStamp("FK_Battlefield_LowRuin_A.asset");
            FantasyKingdomStructureStamp brokenCart = LoadStamp("FK_Battlefield_BrokenCart_A.asset");
            FantasyKingdomStructureStamp dryBranch = LoadStamp("FK_Battlefield_DryBranch_A.asset");
            FantasyKingdomStructureStamp wornScuff = LoadStamp("FK_Battlefield_WornScuff_A.asset");
            FantasyKingdomStructureStamp craterNorth = LoadStamp("FK_Battlefield_Crater_N.asset");
            FantasyKingdomStructureStamp craterSouth = LoadStamp("FK_Battlefield_Crater_S.asset");
            FantasyKingdomStructureStamp rubbleDense =
                LoadStamp("FK_Battlefield_Rubble_Dense_A.asset");
            FantasyKingdomStructureStamp rubbleLight =
                LoadStamp("FK_Battlefield_Rubble_Light_A.asset");

            var placements = new List<FantasyKingdomMapPlacement>
            {
                new FantasyKingdomMapPlacement(
                    "left.keep", "Preserved Village Keep", keep, new Vector3Int(4, 15, 0),
                    FantasyKingdomMapZone.Settlement, FantasyKingdomGameplayAnchor.CastleKeep),
                new FantasyKingdomMapPlacement(
                    "left.house", "Complete Log Cabin", house, new Vector3Int(-5, 5, 0),
                    FantasyKingdomMapZone.Settlement, FantasyKingdomGameplayAnchor.Wood),
                new FantasyKingdomMapPlacement(
                    "left.workshop", "Stone Entry Workshop", workshop, new Vector3Int(-16, -4, 0),
                    FantasyKingdomMapZone.Settlement, FantasyKingdomGameplayAnchor.Stone),
                new FantasyKingdomMapPlacement(
                    "right.north.low_ruin", "Northern Low Ruin", lowRuin,
                    new Vector3Int(27, -3, 0),
                    FantasyKingdomMapZone.Battlefield, FantasyKingdomGameplayAnchor.None),
                new FantasyKingdomMapPlacement(
                    "right.north.rubble_dense", "Northern Dense Rubble", rubbleDense,
                    new Vector3Int(26, -8, 0),
                    FantasyKingdomMapZone.Battlefield, FantasyKingdomGameplayAnchor.None),
                new FantasyKingdomMapPlacement(
                    "right.south.broken_cart", "Southern Broken Cart", brokenCart,
                    new Vector3Int(2, -26, 0),
                    FantasyKingdomMapZone.Battlefield, FantasyKingdomGameplayAnchor.None),
                new FantasyKingdomMapPlacement(
                    "right.south.dry_branch", "Southern Dry Branch", dryBranch,
                    new Vector3Int(6, -28, 0),
                    FantasyKingdomMapZone.Battlefield, FantasyKingdomGameplayAnchor.None),
                new FantasyKingdomMapPlacement(
                    "right.edge.crater_north", "Northern Edge Crater", craterNorth,
                    new Vector3Int(20, -10, 0),
                    FantasyKingdomMapZone.Battlefield, FantasyKingdomGameplayAnchor.None),
                new FantasyKingdomMapPlacement(
                    "right.edge.crater_south", "Southern Edge Crater", craterSouth,
                    new Vector3Int(13, -19, 0),
                    FantasyKingdomMapZone.Battlefield, FantasyKingdomGameplayAnchor.None),
                new FantasyKingdomMapPlacement(
                    "right.far.worn_scuff", "Far Right Worn Scuff", wornScuff,
                    new Vector3Int(28, -12, 0),
                    FantasyKingdomMapZone.FarRightFrame, FantasyKingdomGameplayAnchor.None),
                new FantasyKingdomMapPlacement(
                    "right.far.rubble_light", "Far Right Light Rubble", rubbleLight,
                    new Vector3Int(14, -30, 0),
                    FantasyKingdomMapZone.FarRightFrame, FantasyKingdomGameplayAnchor.None)
            };

            EnsureAssetFolder("Assets/Editor/FantasyKingdomPainter/Layouts");
            var layout = ScriptableObject.CreateInstance<FantasyKingdomMapLayout>();
            layout.name = "FK_NewGameScene_FullMap_Draft";
            layout.Initialize(scene, "Grid", "NewGameScene-VisualRebuild-v2", 1072026, placements);
            AssetDatabase.CreateAsset(layout, LegacyV2LayoutPath);
            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssetIfDirty(layout);
            AssetDatabase.ImportAsset(LegacyV2LayoutPath);
            return layout;
        }

        private static FantasyKingdomStructureStamp LoadStamp(string fileName)
        {
            string path = StampFolder + fileName;
            FantasyKingdomStructureStamp stamp =
                AssetDatabase.LoadAssetAtPath<FantasyKingdomStructureStamp>(path);
            if (stamp == null)
                throw new InvalidOperationException("Default layout stamp'i bulunamadi: " + path);
            return stamp;
        }

        private static void EnsureAssetFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
