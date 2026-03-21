#if UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeadWalls
{
    /// <summary>
    /// Harita uretim ve boyama Editor araci.
    /// 1) JSON import: dead_wall_map.json → tilemap boyama
    /// 2) Prosedural uretim: Perlin noise + fBM → tek tikla harita uretimi
    /// Seed degistirerek sonsuz iterasyon yapilabilir.
    /// Slot atamalari EditorPrefs ile kalici saklanir.
    /// </summary>
    public class MapImporterWindow : EditorWindow
    {
        // ─── JSON kaynak ────────────────────────────────
        private TextAsset _jsonFile;
        private string _parseStatus = "";

        // ─── Parsed data (cached) ───────────────────────
        private int _mapWidth;   // sutun sayisi (cols)
        private int _mapHeight;  // satir sayisi (rows)
        private string[][] _groundLayer;    // [row][col]
        private int[][] _buildableLayer;    // [row][col] — 0 veya 1
        private string[][] _resourcesLayer; // [row][col]

        // ─── Stratejik bilgi ────────────────────────────
        private int _castleRow, _castleCol;
        private int _zombieColMin, _zombieRowMin, _zombieRowMax;

        // ─── Tilemap referanslari ───────────────────────
        private Tilemap _groundTilemap;
        private Tilemap _buildableTilemap;
        private Tilemap _resourcesTilemap;

        // ─── Ground tile slot'lari ──────────────────────
        private TileBase _tileGrass;
        private TileBase _tileDarkGrass;
        private TileBase _tileDirt;
        private TileBase _tileRocky;

        // ─── Buildable tile slot ────────────────────────
        private TileBase _tileBuildable;

        // ─── Resource tile slot'lari ────────────────────
        private TileBase _tileForest;
        private TileBase _tileStone;
        private TileBase _tileIron;

        // ─── Koordinat offset ───────────────────────────
        private Vector2Int _cellOffset = Vector2Int.zero;

        // ─── Scroll ─────────────────────────────────────
        private Vector2 _scrollPos;

        // ─── Foldout state'leri ─────────────────────────
        private bool _foldJson = true;
        private bool _foldTilemaps = true;
        private bool _foldGround = true;
        private bool _foldBuildable = true;
        private bool _foldResources = true;
        private bool _foldOffset = true;
        private bool _foldStrategic = true;
        private bool _foldActions = true;

        // ─── Prosedural uretim parametreleri ────────────
        private bool _foldProcedural = true;
        private int _procWidth = 150;
        private int _procHeight = 170;
        private int _procSeed = 42;

        // Ground noise
        private float _groundNoiseScale = 0.03f;
        private int _groundOctaves = 4;
        private float _threshRocky = 0.005f;
        private float _threshDirt = 0.19f;
        private float _threshDarkGrass = 0.61f;

        // Domain warp + smoothing
        private float _warpStrength = 30f;
        private int _smoothingPasses = 1;

        // Buildable zone
        private int _castleCenterRow = 85;
        private int _castleCenterCol = 23;
        private float _buildableRadius = 69f;
        private int _zombieBorderCol = 131;
        private float _boundaryNoiseScale = 0.05f;
        private float _boundaryNoiseAmp = 12f;

        // Kaynaklar
        private float _resourceNoiseScale = 0.06f;
        private float _forestDensity = 0.35f;
        private float _stoneDensity = 0.10f;
        private float _ironDensity = 0.055f;
        private float _forestEdgeBias = 0.3f;
        private float _rockyStoneBonus = 0.15f;

        // ─── EditorPrefs key prefix ─────────────────────
        private const string KeyPrefix = "DeadWalls_MapImporter_";

        [MenuItem("Window/DeadWalls/Map Importer")]
        public static void ShowWindow()
        {
            var window = GetWindow<MapImporterWindow>("Map Importer");
            window.minSize = new Vector2(380, 500);
        }

        // ═══════════════════════════════════════════════
        // EDITORPREFS PERSISTENCE
        // ═══════════════════════════════════════════════

        private void OnEnable()
        {
            LoadAllPrefs();
        }

        private void OnDisable()
        {
            SaveAllPrefs();
        }

        /// <summary>TileBase asset'i GUID ile kaydeder (proje reimport'a dayanikli).</summary>
        private static void SaveTileRef(string key, TileBase tile)
        {
            if (tile == null)
            {
                EditorPrefs.DeleteKey(KeyPrefix + key);
                return;
            }
            string path = AssetDatabase.GetAssetPath(tile);
            string guid = AssetDatabase.AssetPathToGUID(path);
            EditorPrefs.SetString(KeyPrefix + key, guid);
        }

        /// <summary>GUID'den TileBase asset yukler.</summary>
        private static TileBase LoadTileRef(string key)
        {
            string guid = EditorPrefs.GetString(KeyPrefix + key, "");
            if (string.IsNullOrEmpty(guid)) return null;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadAssetAtPath<TileBase>(path);
        }

        /// <summary>Scene objesi (Tilemap) GlobalObjectId ile kaydeder.</summary>
        private static void SaveTilemapRef(string key, Tilemap tilemap)
        {
            if (tilemap == null)
            {
                EditorPrefs.DeleteKey(KeyPrefix + key);
                return;
            }
            var goid = GlobalObjectId.GetGlobalObjectIdSlow(tilemap);
            EditorPrefs.SetString(KeyPrefix + key, goid.ToString());
        }

        /// <summary>GlobalObjectId'den Tilemap yukler.</summary>
        private static Tilemap LoadTilemapRef(string key)
        {
            string str = EditorPrefs.GetString(KeyPrefix + key, "");
            if (string.IsNullOrEmpty(str)) return null;
            if (!GlobalObjectId.TryParse(str, out var goid)) return null;
            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(goid);
            return obj as Tilemap;
        }

        private void SaveAllPrefs()
        {
            // Tile asset'ler (GUID)
            SaveTileRef("Grass", _tileGrass);
            SaveTileRef("DarkGrass", _tileDarkGrass);
            SaveTileRef("Dirt", _tileDirt);
            SaveTileRef("Rocky", _tileRocky);
            SaveTileRef("Buildable", _tileBuildable);
            SaveTileRef("Forest", _tileForest);
            SaveTileRef("Stone", _tileStone);
            SaveTileRef("Iron", _tileIron);

            // Tilemap referanslar (GlobalObjectId)
            SaveTilemapRef("GroundTilemap", _groundTilemap);
            SaveTilemapRef("BuildableTilemap", _buildableTilemap);
            SaveTilemapRef("ResourcesTilemap", _resourcesTilemap);

            // Offset
            EditorPrefs.SetInt(KeyPrefix + "OffsetX", _cellOffset.x);
            EditorPrefs.SetInt(KeyPrefix + "OffsetY", _cellOffset.y);

            // Prosedural parametreler
            EditorPrefs.SetInt(KeyPrefix + "ProcWidth", _procWidth);
            EditorPrefs.SetInt(KeyPrefix + "ProcHeight", _procHeight);
            EditorPrefs.SetInt(KeyPrefix + "ProcSeed", _procSeed);
            EditorPrefs.SetFloat(KeyPrefix + "GroundNoiseScale", _groundNoiseScale);
            EditorPrefs.SetInt(KeyPrefix + "GroundOctaves", _groundOctaves);
            EditorPrefs.SetFloat(KeyPrefix + "ThreshRocky", _threshRocky);
            EditorPrefs.SetFloat(KeyPrefix + "ThreshDirt", _threshDirt);
            EditorPrefs.SetFloat(KeyPrefix + "ThreshDarkGrass", _threshDarkGrass);
            EditorPrefs.SetInt(KeyPrefix + "CastleCenterRow", _castleCenterRow);
            EditorPrefs.SetInt(KeyPrefix + "CastleCenterCol", _castleCenterCol);
            EditorPrefs.SetFloat(KeyPrefix + "BuildableRadius", _buildableRadius);
            EditorPrefs.SetInt(KeyPrefix + "ZombieBorderCol", _zombieBorderCol);
            EditorPrefs.SetFloat(KeyPrefix + "BoundaryNoiseScale", _boundaryNoiseScale);
            EditorPrefs.SetFloat(KeyPrefix + "BoundaryNoiseAmp", _boundaryNoiseAmp);
            EditorPrefs.SetFloat(KeyPrefix + "ResourceNoiseScale", _resourceNoiseScale);
            EditorPrefs.SetFloat(KeyPrefix + "ForestDensity", _forestDensity);
            EditorPrefs.SetFloat(KeyPrefix + "StoneDensity", _stoneDensity);
            EditorPrefs.SetFloat(KeyPrefix + "IronDensity", _ironDensity);
            EditorPrefs.SetFloat(KeyPrefix + "ForestEdgeBias", _forestEdgeBias);
            EditorPrefs.SetFloat(KeyPrefix + "RockyStoneBonus", _rockyStoneBonus);
            EditorPrefs.SetFloat(KeyPrefix + "WarpStrength", _warpStrength);
            EditorPrefs.SetInt(KeyPrefix + "SmoothingPasses", _smoothingPasses);
        }

        private void LoadAllPrefs()
        {
            // Tile asset'ler
            _tileGrass = LoadTileRef("Grass");
            _tileDarkGrass = LoadTileRef("DarkGrass");
            _tileDirt = LoadTileRef("Dirt");
            _tileRocky = LoadTileRef("Rocky");
            _tileBuildable = LoadTileRef("Buildable");
            _tileForest = LoadTileRef("Forest");
            _tileStone = LoadTileRef("Stone");
            _tileIron = LoadTileRef("Iron");

            // Tilemap referanslar
            _groundTilemap = LoadTilemapRef("GroundTilemap");
            _buildableTilemap = LoadTilemapRef("BuildableTilemap");
            _resourcesTilemap = LoadTilemapRef("ResourcesTilemap");

            // Offset
            _cellOffset.x = EditorPrefs.GetInt(KeyPrefix + "OffsetX", 0);
            _cellOffset.y = EditorPrefs.GetInt(KeyPrefix + "OffsetY", 0);

            // Prosedural parametreler
            _procWidth = EditorPrefs.GetInt(KeyPrefix + "ProcWidth", 150);
            _procHeight = EditorPrefs.GetInt(KeyPrefix + "ProcHeight", 170);
            _procSeed = EditorPrefs.GetInt(KeyPrefix + "ProcSeed", 42);
            _groundNoiseScale = EditorPrefs.GetFloat(KeyPrefix + "GroundNoiseScale", 0.03f);
            _groundOctaves = EditorPrefs.GetInt(KeyPrefix + "GroundOctaves", 4);
            _threshRocky = EditorPrefs.GetFloat(KeyPrefix + "ThreshRocky", 0.20f);
            _threshDirt = EditorPrefs.GetFloat(KeyPrefix + "ThreshDirt", 0.35f);
            _threshDarkGrass = EditorPrefs.GetFloat(KeyPrefix + "ThreshDarkGrass", 0.62f);
            _castleCenterRow = EditorPrefs.GetInt(KeyPrefix + "CastleCenterRow", 85);
            _castleCenterCol = EditorPrefs.GetInt(KeyPrefix + "CastleCenterCol", 23);
            _buildableRadius = EditorPrefs.GetFloat(KeyPrefix + "BuildableRadius", 69f);
            _zombieBorderCol = EditorPrefs.GetInt(KeyPrefix + "ZombieBorderCol", 131);
            _boundaryNoiseScale = EditorPrefs.GetFloat(KeyPrefix + "BoundaryNoiseScale", 0.05f);
            _boundaryNoiseAmp = EditorPrefs.GetFloat(KeyPrefix + "BoundaryNoiseAmp", 12f);
            _resourceNoiseScale = EditorPrefs.GetFloat(KeyPrefix + "ResourceNoiseScale", 0.06f);
            _forestDensity = EditorPrefs.GetFloat(KeyPrefix + "ForestDensity", 0.22f);
            _stoneDensity = EditorPrefs.GetFloat(KeyPrefix + "StoneDensity", 0.10f);
            _ironDensity = EditorPrefs.GetFloat(KeyPrefix + "IronDensity", 0.055f);
            _forestEdgeBias = EditorPrefs.GetFloat(KeyPrefix + "ForestEdgeBias", 0.3f);
            _rockyStoneBonus = EditorPrefs.GetFloat(KeyPrefix + "RockyStoneBonus", 0.15f);
            _warpStrength = EditorPrefs.GetFloat(KeyPrefix + "WarpStrength", 30f);
            _smoothingPasses = EditorPrefs.GetInt(KeyPrefix + "SmoothingPasses", 1);
        }

        // ═══════════════════════════════════════════════
        // OnGUI
        // ═══════════════════════════════════════════════

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawHeader();
            DrawJsonSection();
            DrawTilemapSection();
            DrawGroundTilesSection();
            DrawBuildableTileSection();
            DrawResourceTilesSection();
            DrawOffsetSection();
            DrawStrategicInfoSection();
            DrawActionsSection();
            DrawProceduralSection();

            EditorGUILayout.EndScrollView();
        }

        // ═══════════════════════════════════════════════
        // HEADER
        // ═══════════════════════════════════════════════

        private void DrawHeader()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("\u25c6 MAP IMPORTER", EditorStyles.boldLabel);
            DrawSeparator();
        }

        // ═══════════════════════════════════════════════
        // BOLUM 1: JSON DOSYASI
        // ═══════════════════════════════════════════════

        private void DrawJsonSection()
        {
            _foldJson = EditorGUILayout.BeginFoldoutHeaderGroup(_foldJson, "JSON DOSYASI");
            if (_foldJson)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                _jsonFile = (TextAsset)EditorGUILayout.ObjectField(
                    "JSON File", _jsonFile, typeof(TextAsset), false);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Parse JSON", GUILayout.Height(24)))
                    ParseJson();

                if (!string.IsNullOrEmpty(_parseStatus))
                    EditorGUILayout.LabelField(_parseStatus, EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ═══════════════════════════════════════════════
        // BOLUM 2: TILEMAP REFERANSLARI
        // ═══════════════════════════════════════════════

        private void DrawTilemapSection()
        {
            _foldTilemaps = EditorGUILayout.BeginFoldoutHeaderGroup(_foldTilemaps, "TILEMAP REFERANSLARI");
            if (_foldTilemaps)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUI.BeginChangeCheck();
                _groundTilemap = (Tilemap)EditorGUILayout.ObjectField(
                    "Ground", _groundTilemap, typeof(Tilemap), true);
                _buildableTilemap = (Tilemap)EditorGUILayout.ObjectField(
                    "Buildable", _buildableTilemap, typeof(Tilemap), true);
                _resourcesTilemap = (Tilemap)EditorGUILayout.ObjectField(
                    "Resources", _resourcesTilemap, typeof(Tilemap), true);
                if (EditorGUI.EndChangeCheck())
                    SaveAllPrefs();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ═══════════════════════════════════════════════
        // BOLUM 3: GROUND TILE'LARI
        // ═══════════════════════════════════════════════

        private void DrawGroundTilesSection()
        {
            _foldGround = EditorGUILayout.BeginFoldoutHeaderGroup(_foldGround, "GROUND TILE'LARI");
            if (_foldGround)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUI.BeginChangeCheck();
                _tileGrass = (TileBase)EditorGUILayout.ObjectField(
                    "Grass", _tileGrass, typeof(TileBase), false);
                _tileDarkGrass = (TileBase)EditorGUILayout.ObjectField(
                    "Dark Grass", _tileDarkGrass, typeof(TileBase), false);
                _tileDirt = (TileBase)EditorGUILayout.ObjectField(
                    "Dirt", _tileDirt, typeof(TileBase), false);
                _tileRocky = (TileBase)EditorGUILayout.ObjectField(
                    "Rocky", _tileRocky, typeof(TileBase), false);
                if (EditorGUI.EndChangeCheck())
                    SaveAllPrefs();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ═══════════════════════════════════════════════
        // BOLUM 4: BUILDABLE ZONE TILE
        // ═══════════════════════════════════════════════

        private void DrawBuildableTileSection()
        {
            _foldBuildable = EditorGUILayout.BeginFoldoutHeaderGroup(_foldBuildable, "BUILDABLE ZONE TILE");
            if (_foldBuildable)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUI.BeginChangeCheck();
                _tileBuildable = (TileBase)EditorGUILayout.ObjectField(
                    "Buildable", _tileBuildable, typeof(TileBase), false);
                if (EditorGUI.EndChangeCheck())
                    SaveAllPrefs();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ═══════════════════════════════════════════════
        // BOLUM 5: RESOURCE TILE'LARI
        // ═══════════════════════════════════════════════

        private void DrawResourceTilesSection()
        {
            _foldResources = EditorGUILayout.BeginFoldoutHeaderGroup(_foldResources, "RESOURCE TILE'LARI");
            if (_foldResources)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUI.BeginChangeCheck();
                _tileForest = (TileBase)EditorGUILayout.ObjectField(
                    "Forest", _tileForest, typeof(TileBase), false);
                _tileStone = (TileBase)EditorGUILayout.ObjectField(
                    "Stone", _tileStone, typeof(TileBase), false);
                _tileIron = (TileBase)EditorGUILayout.ObjectField(
                    "Iron", _tileIron, typeof(TileBase), false);
                if (EditorGUI.EndChangeCheck())
                    SaveAllPrefs();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ═══════════════════════════════════════════════
        // BOLUM 6: KOORDINAT OFFSET
        // ═══════════════════════════════════════════════

        private void DrawOffsetSection()
        {
            _foldOffset = EditorGUILayout.BeginFoldoutHeaderGroup(_foldOffset, "KOORDINAT OFFSET");
            if (_foldOffset)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUI.BeginChangeCheck();
                _cellOffset = EditorGUILayout.Vector2IntField("Cell Offset", _cellOffset);
                if (EditorGUI.EndChangeCheck())
                    SaveAllPrefs();

                string yInfo = _mapHeight > 0 ? (_mapHeight - 1).ToString() : "?";
                EditorGUILayout.HelpBox(
                    $"Tilemap hucrelerini kaydirmak icin kullan.\n" +
                    $"(0,0) → JSON row=0 tilemap y={yInfo}'a eslenecek.",
                    MessageType.Info);

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ═══════════════════════════════════════════════
        // BOLUM 7: STRATEJIK BILGI (read-only)
        // ═══════════════════════════════════════════════

        private void DrawStrategicInfoSection()
        {
            _foldStrategic = EditorGUILayout.BeginFoldoutHeaderGroup(_foldStrategic,
                "STRATEJIK BILGI (read-only)");
            if (_foldStrategic)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (_mapWidth > 0)
                {
                    EditorGUILayout.LabelField($"Harita boyutu: {_mapWidth}x{_mapHeight}");
                    EditorGUILayout.LabelField($"Castle: row={_castleRow}, col={_castleCol}");
                    EditorGUILayout.LabelField(
                        $"Zombie Spawn: cols>={_zombieColMin}, rows {_zombieRowMin}-{_zombieRowMax}");
                }
                else
                {
                    EditorGUILayout.LabelField("JSON parse edilmedi.", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ═══════════════════════════════════════════════
        // BOLUM 8: ISLEMLER
        // ═══════════════════════════════════════════════

        private void DrawActionsSection()
        {
            _foldActions = EditorGUILayout.BeginFoldoutHeaderGroup(_foldActions, "ISLEMLER");
            if (_foldActions)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                bool parsed = _groundLayer != null;

                // --- Ground ---
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = parsed && _groundTilemap != null && HasAnyGroundTile();
                if (GUILayout.Button("Paint Ground", GUILayout.Height(24)))
                    PaintGround();
                GUI.enabled = _groundTilemap != null;
                if (GUILayout.Button("Clear Ground", GUILayout.Height(24)))
                    ClearLayer(_groundTilemap, "Clear Ground");
                EditorGUILayout.EndHorizontal();

                // --- Buildable ---
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = parsed && _buildableTilemap != null && _tileBuildable != null;
                if (GUILayout.Button("Paint Buildable", GUILayout.Height(24)))
                    PaintBuildable();
                GUI.enabled = _buildableTilemap != null;
                if (GUILayout.Button("Clear Buildable", GUILayout.Height(24)))
                    ClearLayer(_buildableTilemap, "Clear Buildable");
                EditorGUILayout.EndHorizontal();

                // --- Resources ---
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = parsed && _resourcesTilemap != null && HasAnyResourceTile();
                if (GUILayout.Button("Paint Resources", GUILayout.Height(24)))
                    PaintResources();
                GUI.enabled = _resourcesTilemap != null;
                if (GUILayout.Button("Clear Resources", GUILayout.Height(24)))
                    ClearLayer(_resourcesTilemap, "Clear Resources");
                EditorGUILayout.EndHorizontal();

                GUI.enabled = true;
                DrawSeparator();

                // --- Tumunu Boya ---
                bool canPaintAll = parsed
                    && _groundTilemap != null && HasAnyGroundTile()
                    && _buildableTilemap != null && _tileBuildable != null
                    && _resourcesTilemap != null && HasAnyResourceTile();

                Color oldBg = GUI.backgroundColor;
                GUI.backgroundColor = canPaintAll ? new Color(0.3f, 0.8f, 0.3f) : Color.gray;
                GUI.enabled = canPaintAll;
                if (GUILayout.Button("TUMUNU BOYA", GUILayout.Height(32)))
                    PaintAll();
                GUI.backgroundColor = oldBg;
                GUI.enabled = true;

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ═══════════════════════════════════════════════
        // JSON PARSING
        // ═══════════════════════════════════════════════

        private void ParseJson()
        {
            if (_jsonFile == null)
            {
                _parseStatus = "JSON dosyasi secilmedi!";
                return;
            }

            try
            {
                var root = JObject.Parse(_jsonFile.text);

                // Kare harita ("size") veya dikdortgen ("width"/"height") destegi
                if (root["width"] != null && root["height"] != null)
                {
                    _mapWidth = root["width"].Value<int>();
                    _mapHeight = root["height"].Value<int>();
                }
                else
                {
                    int size = root["size"]?.Value<int>() ?? 0;
                    _mapWidth = size;
                    _mapHeight = size;
                }

                // Stratejik bilgi
                var castle = root["castlePosition"];
                if (castle != null)
                {
                    _castleRow = castle["r"]?.Value<int>() ?? 0;
                    _castleCol = castle["c"]?.Value<int>() ?? 0;
                }

                var zombie = root["zombieSpawn"];
                if (zombie != null)
                {
                    _zombieColMin = zombie["colMin"]?.Value<int>() ?? 0;
                    _zombieRowMin = zombie["rowMin"]?.Value<int>() ?? 0;
                    _zombieRowMax = zombie["rowMax"]?.Value<int>() ?? 0;
                }

                var layers = root["layers"];
                if (layers == null)
                {
                    _parseStatus = "JSON'da 'layers' bulunamadi!";
                    return;
                }

                // Her layer'i parse et
                _groundLayer = ParseStringLayer(layers["ground"] as JArray);
                _buildableLayer = ParseIntLayer(layers["buildable"] as JArray);
                _resourcesLayer = ParseStringLayer(layers["resources"] as JArray);

                _parseStatus = $"{_mapWidth}x{_mapHeight}, 3 layer yuklendi";
            }
            catch (Exception e)
            {
                _parseStatus = $"Parse hatasi: {e.Message}";
                _groundLayer = null;
                _buildableLayer = null;
                _resourcesLayer = null;
            }
        }

        private static string[][] ParseStringLayer(JArray layerArray)
        {
            if (layerArray == null) return null;

            int rows = layerArray.Count;
            var result = new string[rows][];

            for (int r = 0; r < rows; r++)
            {
                var rowArray = layerArray[r] as JArray;
                if (rowArray == null) { result[r] = Array.Empty<string>(); continue; }

                result[r] = new string[rowArray.Count];
                for (int c = 0; c < rowArray.Count; c++)
                    result[r][c] = rowArray[c]?.Value<string>() ?? "";
            }

            return result;
        }

        private static int[][] ParseIntLayer(JArray layerArray)
        {
            if (layerArray == null) return null;

            int rows = layerArray.Count;
            var result = new int[rows][];

            for (int r = 0; r < rows; r++)
            {
                var rowArray = layerArray[r] as JArray;
                if (rowArray == null) { result[r] = Array.Empty<int>(); continue; }

                result[r] = new int[rowArray.Count];
                for (int c = 0; c < rowArray.Count; c++)
                    result[r][c] = rowArray[c]?.Value<int>() ?? 0;
            }

            return result;
        }

        // ═══════════════════════════════════════════════
        // KOORDINAT DONUSUMU
        // ═══════════════════════════════════════════════

        /// <summary>
        /// JSON (row, col) → Tilemap cell pozisyonu.
        /// JSON row 0 = haritanin ustu → Unity tilemap y = mapHeight-1 (Y-flip).
        /// </summary>
        private Vector3Int JsonCellToTilemapCell(int row, int col)
        {
            int x = col + _cellOffset.x;
            int y = (_mapHeight - 1 - row) + _cellOffset.y;
            return new Vector3Int(x, y, 0);
        }

        // ═══════════════════════════════════════════════
        // PAINT METOTLARI
        // ═══════════════════════════════════════════════

        private void PaintGround()
        {
            Undo.RecordObject(_groundTilemap, "Paint Ground Layer");
            int total = _mapHeight * _mapWidth;
            int count = 0;

            try
            {
                for (int r = 0; r < _mapHeight; r++)
                {
                    for (int c = 0; c < _mapWidth; c++)
                    {
                        var tile = GetGroundTile(_groundLayer[r][c]);
                        if (tile != null)
                        {
                            var cell = JsonCellToTilemapCell(r, c);
                            _groundTilemap.SetTile(cell, tile);
                        }

                        if (++count % 500 == 0)
                            EditorUtility.DisplayProgressBar("Ground Boyaniyor",
                                $"{count}/{total}", (float)count / total);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.SetDirty(_groundTilemap);
            Debug.Log($"[MapImporter] Ground layer boyandi: {total} hucre");
        }

        private void PaintBuildable()
        {
            Undo.RecordObject(_buildableTilemap, "Paint Buildable Layer");
            int total = _mapHeight * _mapWidth;
            int count = 0;
            int painted = 0;

            try
            {
                for (int r = 0; r < _mapHeight; r++)
                {
                    for (int c = 0; c < _mapWidth; c++)
                    {
                        if (_buildableLayer[r][c] == 1)
                        {
                            var cell = JsonCellToTilemapCell(r, c);
                            _buildableTilemap.SetTile(cell, _tileBuildable);
                            painted++;
                        }

                        if (++count % 500 == 0)
                            EditorUtility.DisplayProgressBar("Buildable Boyaniyor",
                                $"{count}/{total}", (float)count / total);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.SetDirty(_buildableTilemap);
            Debug.Log($"[MapImporter] Buildable layer boyandi: {painted}/{total} hucre");
        }

        private void PaintResources()
        {
            Undo.RecordObject(_resourcesTilemap, "Paint Resources Layer");
            int total = _mapHeight * _mapWidth;
            int count = 0;
            int painted = 0;

            try
            {
                for (int r = 0; r < _mapHeight; r++)
                {
                    for (int c = 0; c < _mapWidth; c++)
                    {
                        var tile = GetResourceTile(_resourcesLayer[r][c]);
                        if (tile != null)
                        {
                            var cell = JsonCellToTilemapCell(r, c);
                            _resourcesTilemap.SetTile(cell, tile);
                            painted++;
                        }

                        if (++count % 500 == 0)
                            EditorUtility.DisplayProgressBar("Resources Boyaniyor",
                                $"{count}/{total}", (float)count / total);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.SetDirty(_resourcesTilemap);
            Debug.Log($"[MapImporter] Resources layer boyandi: {painted}/{total} hucre");
        }

        /// <summary>
        /// 3 layer'i tek seferde boyar. Onceki tile'lari temizler (repaint destegi).
        /// Undo.CollapseUndoOperations ile tek Ctrl+Z adimina sarilir.
        /// </summary>
        private void PaintAll()
        {
            int undoGroup = Undo.GetCurrentGroup();

            // Onceki boyamayi temizle — farkli seed/parametrelerle
            // degisen hucreler eski tile'lari birakmasin
            _groundTilemap.ClearAllTiles();
            _buildableTilemap.ClearAllTiles();
            _resourcesTilemap.ClearAllTiles();

            PaintGround();
            PaintBuildable();
            PaintResources();

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("[MapImporter] Tum layer'lar boyandi!");
        }

        // ═══════════════════════════════════════════════
        // CLEAR
        // ═══════════════════════════════════════════════

        private void ClearLayer(Tilemap tilemap, string label)
        {
            Undo.RecordObject(tilemap, label);
            tilemap.ClearAllTiles();
            EditorUtility.SetDirty(tilemap);
            Debug.Log($"[MapImporter] {label} tamamlandi.");
        }

        // ═══════════════════════════════════════════════
        // TILE MAPPING
        // ═══════════════════════════════════════════════

        private TileBase GetGroundTile(string key)
        {
            return key switch
            {
                "grass"      => _tileGrass,
                "dark_grass" => _tileDarkGrass,
                "dirt"       => _tileDirt,
                "rocky"      => _tileRocky,
                _            => null
            };
        }

        private TileBase GetResourceTile(string key)
        {
            return key switch
            {
                "forest" => _tileForest,
                "stone"  => _tileStone,
                "iron"   => _tileIron,
                _        => null
            };
        }

        // ═══════════════════════════════════════════════
        // VALIDATION HELPERS
        // ═══════════════════════════════════════════════

        private bool HasAnyGroundTile()
        {
            return _tileGrass != null || _tileDarkGrass != null
                || _tileDirt != null || _tileRocky != null;
        }

        private bool HasAnyResourceTile()
        {
            return _tileForest != null || _tileStone != null || _tileIron != null;
        }

        // ═══════════════════════════════════════════════
        // PROSEDURAL URETIM — Domain-Warped fBM
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Fractal Brownian Motion — coklu oktav Perlin noise ornekleme.
        /// </summary>
        private static float SampleFBM(float x, float y, float scale, int octaves,
            float lacunarity = 2f, float persistence = 0.5f)
        {
            float amplitude = 1f;
            float frequency = 1f;
            float total = 0f;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float sx = x * scale * frequency;
                float sy = y * scale * frequency;
                total += Mathf.PerlinNoise(sx, sy) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return total / maxValue;
        }

        /// <summary>
        /// Domain-warped fBM — koordinatlara noise offset uygulayarak
        /// organik, bulutsu yapilar olusturur. Web versiyonundaki
        /// akiskan arazi yapilarinin temel kaynagi.
        /// </summary>
        private static float SampleDomainWarpedFBM(float x, float y, float scale,
            int octaves, float warpStrength, float lacunarity = 2f, float persistence = 0.5f)
        {
            // Warp koordinatlarini ayri noise orneklerinden hesapla
            int warpOctaves = Mathf.Max(octaves - 1, 1);
            float warpScale = scale * 0.7f;
            float warpX = SampleFBM(x, y, warpScale, warpOctaves, lacunarity, persistence);
            float warpY = SampleFBM(x + 5.2f, y + 1.3f, warpScale, warpOctaves, lacunarity, persistence);

            // Warp uygula — koordinatlar kaydirildigi icin
            // nehir yatagi, dag sirasi gibi organik yapilar olusur
            float wx = x + warpX * warpStrength;
            float wy = y + warpY * warpStrength;

            return SampleFBM(wx, wy, scale, octaves, lacunarity, persistence);
        }

        /// <summary>3x3 box blur smoothing. Her pass kenar gecislerini yumusatir.</summary>
        private static float[,] SmoothGrid(float[,] grid, int width, int height)
        {
            var result = new float[height, width];
            for (int r = 0; r < height; r++)
            {
                for (int c = 0; c < width; c++)
                {
                    float sum = 0f;
                    int count = 0;
                    for (int dr = -1; dr <= 1; dr++)
                    {
                        for (int dc = -1; dc <= 1; dc++)
                        {
                            int nr = r + dr, nc = c + dc;
                            if (nr >= 0 && nr < height && nc >= 0 && nc < width)
                            {
                                sum += grid[nr, nc];
                                count++;
                            }
                        }
                    }
                    result[r, c] = sum / count;
                }
            }
            return result;
        }

        /// <summary>Domain-warped noise grid uretir + smoothing uygular.</summary>
        private float[,] GenerateNoiseGrid(int seedOffset, float scale, int octaves)
        {
            var grid = new float[_procHeight, _procWidth];
            float ox = (_procSeed + seedOffset) * 137.5f;
            float oy = (_procSeed + seedOffset) * 259.3f;

            for (int r = 0; r < _procHeight; r++)
                for (int c = 0; c < _procWidth; c++)
                    grid[r, c] = SampleDomainWarpedFBM(c + ox, r + oy,
                        scale, octaves, _warpStrength);

            for (int pass = 0; pass < _smoothingPasses; pass++)
                grid = SmoothGrid(grid, _procWidth, _procHeight);

            return grid;
        }

        /// <summary>Noise grid → esik tabanlı terrain tipi esleme.</summary>
        private void ProceduralGenerateGround()
        {
            var noiseGrid = GenerateNoiseGrid(0, _groundNoiseScale, _groundOctaves);

            _groundLayer = new string[_procHeight][];
            for (int r = 0; r < _procHeight; r++)
            {
                _groundLayer[r] = new string[_procWidth];
                for (int c = 0; c < _procWidth; c++)
                {
                    float n = noiseGrid[r, c];
                    if (n < _threshRocky)
                        _groundLayer[r][c] = "rocky";
                    else if (n < _threshDirt)
                        _groundLayer[r][c] = "dirt";
                    else if (n < _threshDarkGrass)
                        _groundLayer[r][c] = "dark_grass";
                    else
                        _groundLayer[r][c] = "grass";
                }
            }
        }

        /// <summary>Kale merkezinden mesafe + domain-warped noise perturbasyonu → buildable zone.</summary>
        private void ProceduralGenerateBuildable()
        {
            var noiseGrid = GenerateNoiseGrid(7777, _boundaryNoiseScale, 3);

            _buildableLayer = new int[_procHeight][];
            for (int r = 0; r < _procHeight; r++)
            {
                _buildableLayer[r] = new int[_procWidth];
                for (int c = 0; c < _procWidth; c++)
                {
                    float dr = r - _castleCenterRow;
                    float dc = c - _castleCenterCol;
                    float dist = Mathf.Sqrt(dr * dr + dc * dc);

                    float noise = noiseGrid[r, c];
                    float effectiveRadius = _buildableRadius + (noise - 0.5f) * 2f * _boundaryNoiseAmp;

                    _buildableLayer[r][c] = (dist <= effectiveRadius && c < _zombieBorderCol) ? 1 : 0;
                }
            }
        }

        /// <summary>Her kaynak tipi domain-warped noise + kurallar (edgeBias, rockyBonus, winner-take-all).</summary>
        private void ProceduralGenerateResources()
        {
            var forestGrid = GenerateNoiseGrid(1111, _resourceNoiseScale, _groundOctaves);
            var stoneGrid = GenerateNoiseGrid(2222, _resourceNoiseScale, _groundOctaves);
            var ironGrid = GenerateNoiseGrid(3333, _resourceNoiseScale * 1.5f, 2);

            float maxEdgeDist = Mathf.Min(_procHeight, _procWidth) * 0.25f;

            _resourcesLayer = new string[_procHeight][];
            for (int r = 0; r < _procHeight; r++)
            {
                _resourcesLayer[r] = new string[_procWidth];
                for (int c = 0; c < _procWidth; c++)
                {
                    // Buildable zone icinde kaynak yok
                    if (_buildableLayer[r][c] == 1)
                    {
                        _resourcesLayer[r][c] = "";
                        continue;
                    }

                    // Forest noise + kenar yanliligi
                    float distToEdge = Mathf.Min(
                        Mathf.Min(r, _procHeight - 1 - r),
                        Mathf.Min(c, _procWidth - 1 - c));
                    float edgeFactor = 1f;
                    if (distToEdge < maxEdgeDist)
                        edgeFactor = 1f + _forestEdgeBias * (1f - distToEdge / maxEdgeDist);
                    float forestScore = forestGrid[r, c] * edgeFactor;

                    // Stone noise + rocky zemin bonusu
                    float stoneScore = stoneGrid[r, c];
                    if (_groundLayer[r][c] == "rocky")
                        stoneScore += _rockyStoneBonus;

                    // Iron noise — daha siki scale, 2 oktav → kucuk izole kumeler
                    float ironScore = ironGrid[r, c];

                    // Threshold kontrol
                    bool hasForest = forestScore > (1f - _forestDensity);
                    bool hasStone = stoneScore > (1f - _stoneDensity);
                    bool hasIron = ironScore > (1f - _ironDensity);

                    if (!hasForest && !hasStone && !hasIron)
                    {
                        _resourcesLayer[r][c] = "";
                        continue;
                    }

                    // Winner-take-all: en yuksek score kazanir
                    string winner = "";
                    float bestScore = 0f;
                    if (hasForest && forestScore > bestScore) { bestScore = forestScore; winner = "forest"; }
                    if (hasStone && stoneScore > bestScore) { bestScore = stoneScore; winner = "stone"; }
                    if (hasIron && ironScore > bestScore) { winner = "iron"; }

                    _resourcesLayer[r][c] = winner;
                }
            }
        }

        /// <summary>3 layer'i prosedural uretir, stratejik bilgiyi gunceller.</summary>
        private void ProceduralGenerateAll()
        {
            _mapWidth = _procWidth;
            _mapHeight = _procHeight;

            ProceduralGenerateGround();
            ProceduralGenerateBuildable();
            ProceduralGenerateResources();

            // Stratejik bilgi guncelle
            _castleRow = _castleCenterRow;
            _castleCol = _castleCenterCol;
            _zombieColMin = _zombieBorderCol;
            _zombieRowMin = 0;
            _zombieRowMax = _procHeight - 1;

            _parseStatus = $"Prosedural {_procWidth}x{_procHeight}, seed={_procSeed}";
            Debug.Log($"[MapImporter] Prosedural harita uretildi: {_parseStatus}");
        }

        /// <summary>Tum prosedural parametreleri varsayilana dondurur.</summary>
        private void ResetProceduralDefaults()
        {
            _procWidth = 150; _procHeight = 170; _procSeed = 42;
            _groundNoiseScale = 0.03f; _groundOctaves = 4;
            _threshRocky = 0.005f; _threshDirt = 0.19f; _threshDarkGrass = 0.61f;
            _warpStrength = 30f; _smoothingPasses = 1;
            _castleCenterRow = 85; _castleCenterCol = 23;
            _buildableRadius = 69f; _zombieBorderCol = 131;
            _boundaryNoiseScale = 0.05f; _boundaryNoiseAmp = 12f;
            _resourceNoiseScale = 0.06f;
            _forestDensity = 0.35f; _stoneDensity = 0.10f; _ironDensity = 0.055f;
            _forestEdgeBias = 0.3f; _rockyStoneBonus = 0.15f;
        }

        // ═══════════════════════════════════════════════
        // BOLUM 9: PROSEDURAL URETIM UI
        // ═══════════════════════════════════════════════

        private void DrawProceduralSection()
        {
            _foldProcedural = EditorGUILayout.BeginFoldoutHeaderGroup(
                _foldProcedural, "PROSEDÜREL ÜRETIM");
            if (_foldProcedural)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // ── Harita Boyutu ──
                EditorGUILayout.LabelField("Harita Boyutu", EditorStyles.boldLabel);
                _procWidth = EditorGUILayout.IntField("Genislik", _procWidth);
                _procHeight = EditorGUILayout.IntField("Yukseklik", _procHeight);

                DrawSeparator();

                // ── Seed ──
                EditorGUILayout.LabelField("Seed", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                _procSeed = EditorGUILayout.IntField("Seed", _procSeed);
                if (GUILayout.Button("Rastgele Seed", GUILayout.Width(110)))
                    _procSeed = UnityEngine.Random.Range(0, 99999);
                EditorGUILayout.EndHorizontal();

                DrawSeparator();

                // ── Ground Noise ──
                EditorGUILayout.LabelField("Ground Noise", EditorStyles.boldLabel);
                _groundNoiseScale = EditorGUILayout.Slider("Noise Scale", _groundNoiseScale, 0.005f, 0.15f);
                _groundOctaves = EditorGUILayout.IntSlider("Octave Sayisi", _groundOctaves, 1, 8);
                _warpStrength = EditorGUILayout.Slider("Domain Warp", _warpStrength, 0f, 80f);
                _smoothingPasses = EditorGUILayout.IntSlider("Smoothing", _smoothingPasses, 0, 3);

                // Threshold slider'lari — sirali zorunluluk uygula
                _threshRocky = EditorGUILayout.Slider("Rocky Esik", _threshRocky, 0.01f, 0.98f);
                float dirtMin = _threshRocky + 0.01f;
                _threshDirt = Mathf.Max(_threshDirt, dirtMin);
                _threshDirt = EditorGUILayout.Slider("Dirt Esik", _threshDirt, dirtMin, 0.99f);
                float darkGrassMin = _threshDirt + 0.01f;
                _threshDarkGrass = Mathf.Max(_threshDarkGrass, darkGrassMin);
                _threshDarkGrass = EditorGUILayout.Slider("Dark Grass Esik", _threshDarkGrass, darkGrassMin, 1f);

                DrawSeparator();

                // ── Buildable Zone ──
                EditorGUILayout.LabelField("Buildable Zone", EditorStyles.boldLabel);
                _castleCenterRow = EditorGUILayout.IntField("Kale Satiri", _castleCenterRow);
                _castleCenterCol = EditorGUILayout.IntField("Kale Sutunu", _castleCenterCol);
                _buildableRadius = EditorGUILayout.Slider("Yaricap", _buildableRadius, 10f, 200f);
                _zombieBorderCol = EditorGUILayout.IntField("Zombie Sinir", _zombieBorderCol);
                _boundaryNoiseScale = EditorGUILayout.Slider("Sinir Noise", _boundaryNoiseScale, 0.005f, 0.15f);
                _boundaryNoiseAmp = EditorGUILayout.Slider("Sinir Genlik", _boundaryNoiseAmp, 0f, 50f);

                DrawSeparator();

                // ── Kaynaklar ──
                EditorGUILayout.LabelField("Kaynaklar", EditorStyles.boldLabel);
                _resourceNoiseScale = EditorGUILayout.Slider("Kaynak Noise", _resourceNoiseScale, 0.005f, 0.15f);
                _forestDensity = EditorGUILayout.Slider("Orman", _forestDensity, 0.01f, 0.5f);
                _stoneDensity = EditorGUILayout.Slider("Tas", _stoneDensity, 0.01f, 0.4f);
                _ironDensity = EditorGUILayout.Slider("Demir", _ironDensity, 0.01f, 0.3f);
                _forestEdgeBias = EditorGUILayout.Slider("Kenar Yanliligi", _forestEdgeBias, 0f, 1f);
                _rockyStoneBonus = EditorGUILayout.Slider("Kayalik Bonus", _rockyStoneBonus, 0f, 0.5f);

                DrawSeparator();

                // ── Butonlar ──
                Color oldBg = GUI.backgroundColor;

                GUI.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
                if (GUILayout.Button("SADECE GENERATE", GUILayout.Height(28)))
                    ProceduralGenerateAll();

                bool hasTilemaps = _groundTilemap != null && HasAnyGroundTile()
                    && _buildableTilemap != null && _tileBuildable != null
                    && _resourcesTilemap != null && HasAnyResourceTile();

                GUI.backgroundColor = hasTilemaps ? new Color(0.3f, 0.8f, 0.3f) : Color.gray;
                if (GUILayout.Button("GENERATE + PAINT", GUILayout.Height(32)))
                {
                    ProceduralGenerateAll();
                    if (hasTilemaps)
                        PaintAll();
                    else
                        Debug.LogWarning("[MapImporter] Generate tamamlandi ama paint icin tilemap/tile slot'lari eksik!");
                }

                GUI.backgroundColor = oldBg;
                GUI.enabled = true;

                DrawSeparator();

                // ── Varsayilanlara Don ──
                if (GUILayout.Button("Varsayilanlara Don"))
                    ResetProceduralDefaults();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ═══════════════════════════════════════════════
        // UTILITY
        // ═══════════════════════════════════════════════

        private static void DrawSeparator()
        {
            EditorGUILayout.Space(2);
            Rect r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(2);
        }
    }
}
#endif
