#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeadWalls
{
    public enum FantasyKingdomStampPurpose
    {
        Structure = 0,
        ResourceSite = 1,
        BattlefieldDecoration = 2,
        GroundDetail = 3
    }

    /// <summary>
    /// Fantasy Kingdom referans sahnesinden cikarilan, katmanlar-arasi tile recetesi.
    /// Bu asset yalniz Editor pipeline'i icindir; runtime gameplay verisi tasimaz.
    /// </summary>
    [CreateAssetMenu(
        fileName = "FantasyKingdomStructureStamp",
        menuName = "DeadWalls/Editor/Fantasy Kingdom Structure Stamp")]
    public sealed class FantasyKingdomStructureStamp : ScriptableObject
    {
        [SerializeField] private string sourceScenePath;
        [SerializeField] private string sourceGridPath;
        [SerializeField] private Vector2Int sourceRegionMin;
        [SerializeField] private Vector2Int sourceRegionSize;
        [SerializeField] private GridLayout.CellLayout sourceCellLayout;
        [SerializeField] private GridLayout.CellSwizzle sourceCellSwizzle;
        [SerializeField] private Vector3 sourceCellSize;
        [SerializeField] private Vector3 sourceCellGap;
        [SerializeField] private Vector3Int anchorLocalCell;
        [SerializeField] private bool hasExplicitAnchor;
        [SerializeField] private FantasyKingdomStampPurpose purpose = FantasyKingdomStampPurpose.Structure;
        [SerializeField] private List<FantasyKingdomStampLayer> layers = new List<FantasyKingdomStampLayer>();

        public string SourceScenePath => sourceScenePath;
        public string SourceGridPath => sourceGridPath;
        public Vector2Int SourceRegionMin => sourceRegionMin;
        public Vector2Int SourceRegionSize => sourceRegionSize;
        public GridLayout.CellLayout SourceCellLayout => sourceCellLayout;
        public GridLayout.CellSwizzle SourceCellSwizzle => sourceCellSwizzle;
        public Vector3 SourceCellSize => sourceCellSize;
        public Vector3 SourceCellGap => sourceCellGap;
        public Vector3Int AnchorLocalCell => hasExplicitAnchor
            ? anchorLocalCell
            : new Vector3Int(
                Mathf.Max(0, sourceRegionSize.x / 2),
                Mathf.Max(0, sourceRegionSize.y / 2),
                0);
        public FantasyKingdomStampPurpose Purpose => purpose;
        public IReadOnlyList<FantasyKingdomStampLayer> Layers => layers;

        public int TotalTileCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < layers.Count; i++)
                    total += layers[i].Cells.Count;
                return total;
            }
        }

        internal void Initialize(
            string scenePath,
            string gridPath,
            RectInt sourceRegion,
            Grid grid,
            FantasyKingdomStampPurpose stampPurpose,
            List<FantasyKingdomStampLayer> extractedLayers)
        {
            sourceScenePath = scenePath;
            sourceGridPath = gridPath;
            sourceRegionMin = sourceRegion.min;
            sourceRegionSize = sourceRegion.size;
            sourceCellLayout = grid.cellLayout;
            sourceCellSwizzle = grid.cellSwizzle;
            sourceCellSize = grid.cellSize;
            sourceCellGap = grid.cellGap;
            anchorLocalCell = new Vector3Int(sourceRegion.width / 2, sourceRegion.height / 2, 0);
            hasExplicitAnchor = true;
            purpose = stampPurpose;
            layers = extractedLayers ?? new List<FantasyKingdomStampLayer>();
        }

        internal void SetAnchorLocalCell(Vector3Int localCell)
        {
            if (localCell.x < 0 || localCell.y < 0 || localCell.z != 0 ||
                localCell.x >= sourceRegionSize.x || localCell.y >= sourceRegionSize.y)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(localCell),
                    "Stamp anchor'i source region sinirlari icinde ve z=0 olmalidir.");
            }

            anchorLocalCell = localCell;
            hasExplicitAnchor = true;
        }
    }

    [Serializable]
    public sealed class FantasyKingdomStampLayer
    {
        [SerializeField] private string sourceHierarchyPath;
        [SerializeField] private string sourceName;
        [SerializeField] private string sortingLayerName;
        [SerializeField] private int sortingOrder;
        [SerializeField] private TilemapRenderer.Mode rendererMode;
        [SerializeField] private TilemapRenderer.SortOrder sortOrder;
        [SerializeField] private Vector3 tileAnchor;
        [SerializeField] private Color layerColor = Color.white;
        [SerializeField] private Tilemap.Orientation orientation;
        [SerializeField] private Matrix4x4 orientationMatrix = Matrix4x4.identity;
        [SerializeField] private List<FantasyKingdomStampCell> cells = new List<FantasyKingdomStampCell>();

        public string SourceHierarchyPath => sourceHierarchyPath;
        public string SourceName => sourceName;
        public string SortingLayerName => sortingLayerName;
        public int SortingOrder => sortingOrder;
        public TilemapRenderer.Mode RendererMode => rendererMode;
        public TilemapRenderer.SortOrder SortOrder => sortOrder;
        public Vector3 TileAnchor => tileAnchor;
        public Color LayerColor => layerColor;
        public Tilemap.Orientation Orientation => orientation;
        public Matrix4x4 OrientationMatrix => orientationMatrix;
        public IReadOnlyList<FantasyKingdomStampCell> Cells => cells;

        internal FantasyKingdomStampLayer(
            string hierarchyPath,
            Tilemap tilemap,
            TilemapRenderer renderer,
            List<FantasyKingdomStampCell> extractedCells)
        {
            sourceHierarchyPath = hierarchyPath;
            sourceName = tilemap.name;
            sortingLayerName = renderer != null ? renderer.sortingLayerName : "Default";
            sortingOrder = renderer != null ? renderer.sortingOrder : 0;
            rendererMode = renderer != null ? renderer.mode : TilemapRenderer.Mode.Chunk;
            sortOrder = renderer != null ? renderer.sortOrder : TilemapRenderer.SortOrder.BottomLeft;
            tileAnchor = tilemap.tileAnchor;
            layerColor = tilemap.color;
            orientation = tilemap.orientation;
            orientationMatrix = tilemap.orientationMatrix;
            cells = extractedCells ?? new List<FantasyKingdomStampCell>();
        }

        internal FantasyKingdomStampLayer(
            string hierarchyPath,
            string layerName,
            string layerSortingName,
            int layerSortingOrder,
            TilemapRenderer.Mode layerRendererMode,
            TilemapRenderer.SortOrder layerSortOrder,
            Vector3 layerTileAnchor,
            Color tint,
            Tilemap.Orientation layerOrientation,
            Matrix4x4 layerOrientationMatrix,
            List<FantasyKingdomStampCell> generatedCells)
        {
            sourceHierarchyPath = hierarchyPath;
            sourceName = layerName;
            sortingLayerName = layerSortingName;
            sortingOrder = layerSortingOrder;
            rendererMode = layerRendererMode;
            sortOrder = layerSortOrder;
            tileAnchor = layerTileAnchor;
            layerColor = tint;
            orientation = layerOrientation;
            orientationMatrix = layerOrientationMatrix;
            cells = generatedCells ?? new List<FantasyKingdomStampCell>();
        }
    }

    [Serializable]
    public sealed class FantasyKingdomStampCell
    {
        [SerializeField] private Vector3Int localPosition;
        [SerializeField] private TileBase tile;
        [SerializeField] private Matrix4x4 transformMatrix = Matrix4x4.identity;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private TileFlags flags;

        public Vector3Int LocalPosition => localPosition;
        public TileBase Tile => tile;
        public Matrix4x4 TransformMatrix => transformMatrix;
        public Color Color => color;
        public TileFlags Flags => flags;

        internal FantasyKingdomStampCell(
            Vector3Int relativePosition,
            TileBase sourceTile,
            Matrix4x4 sourceTransform,
            Color sourceColor,
            TileFlags sourceFlags)
        {
            localPosition = relativePosition;
            tile = sourceTile;
            transformMatrix = sourceTransform;
            color = sourceColor;
            flags = sourceFlags;
        }
    }
}
#endif
