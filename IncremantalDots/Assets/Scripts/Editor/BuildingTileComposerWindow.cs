using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeadWalls
{
    /// <summary>
    /// Her BuildingConfigSO icin izometrik tile layout compose eden Editor araci.
    /// Performans: AssetPreview yerine sprite texture dogrudan cizilir (aninda render).
    /// Lazy load: Scan sirasinda sadece metadata saklanir, tile gorunur olunca yuklenir.
    /// Yon filtresi: _N/_S/_E/_W suffix'lerinden yon bilgisi cikarilir.
    /// </summary>
    public class BuildingTileComposerWindow : EditorWindow
    {
        // ─── SO referans ──────────────────────────────
        private BuildingConfigSO _config;

        // ─── Layer & tile secimi ──────────────────────
        private enum Layer { Base, Top }
        private Layer _selectedLayer = Layer.Base;
        private TileBase _selectedTile;

        // ─── Calisma array'leri ───────────────────────
        private TileBase[] _baseLayout;
        private TileBase[] _topLayout;

        // ─── Tile Palette (lazy load) ─────────────────
        private DefaultAsset _tileFolder;

        private struct TileEntry
        {
            public string Guid;
            public string Path;
            public string Name;
            public string Category;
            public string Direction; // "N", "S", "E", "W" veya ""
        }

        private List<TileEntry> _tileEntries = new List<TileEntry>();
        private Dictionary<string, List<int>> _categoryIndices = new Dictionary<string, List<int>>();
        private List<string> _categoryNames = new List<string>();
        private Dictionary<string, TileBase> _loadedTiles = new Dictionary<string, TileBase>();

        private string _selectedCategory = "All";
        private string _selectedDirection = "All"; // "All", "N", "S", "E", "W"
        private string _searchFilter = "";
        private Vector2 _paletteScrollPos;
        private const int ThumbnailSize = 48;
        private const int ThumbnailPadding = 2;
        private const int PaletteCellSize = ThumbnailSize + ThumbnailPadding * 2;

        // ─── Son kullanilan tile'lar ──────────────────
        private List<TileBase> _recentTiles = new List<TileBase>();
        private const int MaxRecentTiles = 10;

        // ─── Grid cizim parametreleri ─────────────────
        private const float CellW = 64f;
        private const float CellH = 32f;

        // ─── Scroll pozisyonu ─────────────────────────
        private Vector2 _scrollPos;

        // ─── Hover & drag state ───────────────────────
        private int _hoverX = -1;
        private int _hoverY = -1;
        private bool _isDragging;
        private int _dragButton = -1;
        private int _lastPaintedX = -1;
        private int _lastPaintedY = -1;

        // ─── Foldout state'leri ───────────────────────
        private bool _foldoutConfig = true;
        private bool _foldoutPalette = true;
        private bool _foldoutGrid = true;
        private bool _foldoutPreview = true;

        // ─── Sprite cache (AssetPreview yerine) ───────
        private Dictionary<int, Sprite> _spriteCache = new Dictionary<int, Sprite>();

        // ─── Filtered index cache ─────────────────────
        private List<int> _filteredIndices;
        private string _lastCategory;
        private string _lastDirection;
        private string _lastSearch;

        // ─── Preview RenderTexture cache ────────────
        private Texture2D _previewTexture;
        private bool _previewDirty = true;

        // ─── Cached GUIStyle'lar ──────────────────────
        private GUIStyle _cellLabelStyle;
        private GUIStyle _dirHintStyle;

        [MenuItem("Window/DeadWalls/Building Tile Composer")]
        public static void ShowWindow()
        {
            var window = GetWindow<BuildingTileComposerWindow>("Building Tile Composer");
            window.minSize = new Vector2(420, 600);
        }

        private void OnEnable()
        {
            string defaultPath = "Assets/SmallScaleInt/Fantasy kingdom Tileset/Environment/Tiles";
            if (AssetDatabase.IsValidFolder(defaultPath))
            {
                _tileFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(defaultPath);
                ScanTileFolder();
            }
        }

        private void OnDisable()
        {
            if (_previewTexture != null)
            {
                DestroyImmediate(_previewTexture);
                _previewTexture = null;
            }
        }

        private void OnGUI()
        {
            // GUIStyle cache (bir kez olustur)
            if (_cellLabelStyle == null)
            {
                _cellLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 1f, 1f, 0.3f) }
                };
                _dirHintStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 9,
                    fontStyle = FontStyle.Bold
                };
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawHeader();
            DrawBuildingConfigSection();
            DrawTilePaletteSection();

            if (_config != null)
            {
                DrawGridEditorSection();
                DrawPreviewSection();
                DrawActionButtons();
            }

            EditorGUILayout.EndScrollView();
        }

        // ═══════════════════════════════════════════════
        // HEADER
        // ═══════════════════════════════════════════════
        private void DrawHeader()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("\u25c6 BUILDING TILE COMPOSER", EditorStyles.boldLabel);
            DrawSeparator();
        }

        // ═══════════════════════════════════════════════
        // BOLUM 1: BUILDING CONFIG
        // ═══════════════════════════════════════════════
        private void DrawBuildingConfigSection()
        {
            _foldoutConfig = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutConfig, "BUILDING CONFIG");
            if (_foldoutConfig)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUI.BeginChangeCheck();
                _config = (BuildingConfigSO)EditorGUILayout.ObjectField(
                    "Config SO", _config, typeof(BuildingConfigSO), false);
                if (EditorGUI.EndChangeCheck() && _config != null)
                    LoadFromConfig();

                if (_config != null)
                    EditorGUILayout.LabelField(
                        $"{_config.GridWidth}x{_config.GridHeight}  {_config.DisplayName}",
                        EditorStyles.miniLabel);

                // Layer toggle
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Layer:", GUILayout.Width(50));

                Color oldBg = GUI.backgroundColor;
                GUI.backgroundColor = _selectedLayer == Layer.Base ? new Color(0.5f, 0.8f, 1f) : Color.white;
                if (GUILayout.Toggle(_selectedLayer == Layer.Base, "Base (duvar/zemin)",
                    EditorStyles.miniButtonLeft, GUILayout.Height(22)))
                    _selectedLayer = Layer.Base;
                GUI.backgroundColor = _selectedLayer == Layer.Top ? new Color(1f, 0.8f, 0.5f) : Color.white;
                if (GUILayout.Toggle(_selectedLayer == Layer.Top, "Top (cati/detay)",
                    EditorStyles.miniButtonRight, GUILayout.Height(22)))
                    _selectedLayer = Layer.Top;
                GUI.backgroundColor = oldBg;
                EditorGUILayout.EndHorizontal();

                // Secili tile
                if (_selectedTile != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Secili:", GUILayout.Width(50));
                    Rect iconRect = GUILayoutUtility.GetRect(24, 24, GUILayout.Width(24));
                    DrawSpriteThumb(iconRect, _selectedTile);
                    EditorGUILayout.LabelField(_selectedTile.name, EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ═══════════════════════════════════════════════
        // BOLUM 2: TILE PALETTE
        // ═══════════════════════════════════════════════
        private void DrawTilePaletteSection()
        {
            _foldoutPalette = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutPalette, "TILE PALETTE");
            if (_foldoutPalette)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUI.BeginChangeCheck();
                _tileFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                    "Tile Klasoru", _tileFolder, typeof(DefaultAsset), false);
                if (EditorGUI.EndChangeCheck())
                    ScanTileFolder();

                if (_tileEntries.Count == 0)
                {
                    EditorGUILayout.HelpBox("Tile klasoru secin veya klasorde Tile asset'i yok.",
                        MessageType.Info);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndFoldoutHeaderGroup();
                    return;
                }

                // Arama
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Ara:", GUILayout.Width(30));
                _searchFilter = EditorGUILayout.TextField(_searchFilter);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                    _searchFilter = "";
                EditorGUILayout.EndHorizontal();

                // Kategori butonlari
                DrawCategoryButtons();

                // Yon filtresi
                DrawDirectionFilter();

                // Son kullanilan
                if (_recentTiles.Count > 0)
                {
                    EditorGUILayout.LabelField("Son Kullanilan:", EditorStyles.miniLabel);
                    DrawRecentTiles();
                    DrawSeparator();
                }

                // Tile grid
                var indices = GetFilteredIndices();
                EditorGUILayout.LabelField($"{indices.Count} tile", EditorStyles.miniLabel);
                DrawVirtualizedTileGrid(indices);

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        /// <summary>
        /// Yon filtresi — N/S/E/W butonlari.
        /// Izometrik tileset'te yon, tile'in baktigi yonu belirler.
        /// </summary>
        private void DrawDirectionFilter()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Yon:", GUILayout.Width(30));

            string[] dirs = { "All", "N", "S", "E", "W" };
            foreach (var dir in dirs)
            {
                Color oldBg = GUI.backgroundColor;
                if (_selectedDirection == dir)
                    GUI.backgroundColor = new Color(1f, 0.85f, 0.5f);

                if (GUILayout.Toggle(_selectedDirection == dir, dir,
                    EditorStyles.miniButton, GUILayout.Width(32)))
                    _selectedDirection = dir;

                GUI.backgroundColor = oldBg;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCategoryButtons()
        {
            float catAvailWidth = Mathf.Max(200f, EditorGUIUtility.currentViewWidth - 50);
            float catX = 0f;
            EditorGUILayout.BeginHorizontal();
            DrawCatBtn("All", _tileEntries.Count, ref catX, catAvailWidth);
            foreach (var cat in _categoryNames)
            {
                int count = _categoryIndices.ContainsKey(cat) ? _categoryIndices[cat].Count : 0;
                DrawCatBtn(cat, count, ref catX, catAvailWidth);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCatBtn(string cat, int count, ref float catX, float availWidth)
        {
            string label = $"{cat} ({count})";
            float btnWidth = label.Length * 7f + 16f;
            if (catX + btnWidth > availWidth && catX > 0f)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                catX = 0f;
            }

            Color oldBg = GUI.backgroundColor;
            if (_selectedCategory == cat)
                GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
            if (GUILayout.Button(label, EditorStyles.miniButton))
                _selectedCategory = cat;
            GUI.backgroundColor = oldBg;
            catX += btnWidth;
        }

        private void DrawRecentTiles()
        {
            float availWidth = Mathf.Max(200f, EditorGUIUtility.currentViewWidth - 40);
            int cols = Mathf.Max(1, Mathf.FloorToInt(availWidth / PaletteCellSize));
            int rows = Mathf.CeilToInt((float)_recentTiles.Count / cols);
            Rect gridRect = GUILayoutUtility.GetRect(availWidth, rows * PaletteCellSize);

            for (int i = 0; i < _recentTiles.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                Rect tileRect = new Rect(
                    gridRect.x + col * PaletteCellSize,
                    gridRect.y + row * PaletteCellSize,
                    PaletteCellSize, PaletteCellSize);

                var tile = _recentTiles[i];
                DrawPaletteCell(tileRect, tile, tile == _selectedTile, tile.name);

                if (Event.current.type == EventType.MouseDown &&
                    Event.current.button == 0 && tileRect.Contains(Event.current.mousePosition))
                {
                    _selectedTile = tile;
                    Event.current.Use();
                    Repaint();
                }
            }
        }

        private void DrawVirtualizedTileGrid(List<int> indices)
        {
            if (indices.Count == 0) return;

            float availWidth = Mathf.Max(200f, EditorGUIUtility.currentViewWidth - 40);
            int cols = Mathf.Max(1, Mathf.FloorToInt(availWidth / PaletteCellSize));
            int totalRows = Mathf.CeilToInt((float)indices.Count / cols);

            float totalHeight = totalRows * PaletteCellSize;
            float viewHeight = Mathf.Clamp(totalHeight, 60f, 250f);

            _paletteScrollPos = EditorGUILayout.BeginScrollView(
                _paletteScrollPos, GUILayout.Height(viewHeight));

            Rect gridRect = GUILayoutUtility.GetRect(availWidth, totalHeight);

            // Gorunen satir araligi
            float scrollY = _paletteScrollPos.y;
            int firstRow = Mathf.Max(0, Mathf.FloorToInt(scrollY / PaletteCellSize) - 1);
            int lastRow = Mathf.Min(totalRows - 1,
                Mathf.CeilToInt((scrollY + viewHeight) / PaletteCellSize) + 1);

            int firstIdx = firstRow * cols;
            int lastIdx = Mathf.Min(indices.Count - 1, (lastRow + 1) * cols - 1);

            for (int i = firstIdx; i <= lastIdx; i++)
            {
                var entry = _tileEntries[indices[i]];

                int col = i % cols;
                int row = i / cols;
                Rect tileRect = new Rect(
                    gridRect.x + col * PaletteCellSize,
                    gridRect.y + row * PaletteCellSize,
                    PaletteCellSize, PaletteCellSize);

                var tile = LazyLoadTile(entry.Guid, entry.Path);
                bool isSelected = tile != null && tile == _selectedTile;

                DrawPaletteCell(tileRect, tile, isSelected, entry.Name);

                if (Event.current.type == EventType.MouseDown &&
                    Event.current.button == 0 && tileRect.Contains(Event.current.mousePosition))
                {
                    if (tile != null)
                    {
                        _selectedTile = tile;
                        AddToRecent(tile);
                    }
                    Event.current.Use();
                    Repaint();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Tek palette hucresini ciz. Sprite texture dogrudan render edilir.
        /// </summary>
        private void DrawPaletteCell(Rect tileRect, TileBase tile, bool isSelected, string name)
        {
            if (isSelected)
                EditorGUI.DrawRect(tileRect, new Color(0.3f, 0.7f, 1f, 0.4f));

            if (tileRect.Contains(Event.current.mousePosition))
            {
                EditorGUI.DrawRect(tileRect, new Color(1f, 1f, 1f, 0.15f));
                GUI.Label(tileRect, new GUIContent("", name));
            }

            Rect thumbRect = new Rect(
                tileRect.x + ThumbnailPadding,
                tileRect.y + ThumbnailPadding,
                ThumbnailSize, ThumbnailSize);

            if (tile != null)
                DrawSpriteThumb(thumbRect, tile);
            else
                EditorGUI.DrawRect(thumbRect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
        }

        // ═══════════════════════════════════════════════
        // BOLUM 3: GRID EDITOR
        // ═══════════════════════════════════════════════
        private void DrawGridEditorSection()
        {
            _foldoutGrid = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutGrid,
                $"GRID EDITOR — {(_selectedLayer == Layer.Base ? "BASE" : "TOP")} Layer");
            if (_foldoutGrid)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(
                    "Sol tik: tile ata | Sag tik: tile sil | Surukle: boyama",
                    EditorStyles.miniLabel);

                int gw = _config.GridWidth;
                int gh = _config.GridHeight;

                float totalW = (gw + gh) * CellW * 0.5f + CellW;
                float totalH = (gw + gh) * CellH * 0.5f + CellH;
                Rect gridRect = GUILayoutUtility.GetRect(totalW, totalH + 10);
                EditorGUI.DrawRect(gridRect, new Color(0.15f, 0.15f, 0.15f, 1f));

                Vector2 origin = new Vector2(
                    gridRect.x + gridRect.width * 0.5f,
                    gridRect.y + CellH * 1.5f);

                Event e = Event.current;
                _hoverX = -1;
                _hoverY = -1;

                for (int y = 0; y < gh; y++)
                {
                    for (int x = 0; x < gw; x++)
                    {
                        Vector2 center = GetDiamondCenter(origin, x, y);
                        Vector2[] diamond = GetDiamondVerts(center, CellW, CellH);

                        if (IsPointInDiamond(e.mousePosition, diamond))
                        {
                            _hoverX = x;
                            _hoverY = y;
                        }

                        int idx = x + y * gw;

                        // Her iki layer'i goster — aktif layer tam opak, diger yari saydam
                        bool hasBase = _baseLayout != null && idx < _baseLayout.Length && _baseLayout[idx] != null;
                        bool hasTop = _topLayout != null && idx < _topLayout.Length && _topLayout[idx] != null;

                        if (hasBase)
                        {
                            if (_selectedLayer != Layer.Base && hasTop)
                                GUI.color = new Color(1f, 1f, 1f, 0.35f); // pasif layer soluk
                            DrawTileInDiamond(center, _baseLayout[idx], CellW, CellH);
                            GUI.color = Color.white;
                        }
                        if (hasTop)
                        {
                            if (_selectedLayer != Layer.Top && hasBase)
                                GUI.color = new Color(1f, 1f, 1f, 0.35f);
                            DrawTileInDiamond(center, _topLayout[idx], CellW, CellH);
                            GUI.color = Color.white;
                        }

                        // Hucre icerik gostergesi — hangi layer'larda tile var
                        if (hasBase || hasTop)
                        {
                            string marker = (hasBase && hasTop) ? "B+T"
                                : hasBase ? "B" : "T";
                            Color markerCol = (hasBase && hasTop) ? new Color(0.5f, 1f, 0.5f, 0.6f)
                                : hasBase ? new Color(0.5f, 0.8f, 1f, 0.5f)
                                : new Color(1f, 0.8f, 0.5f, 0.5f);
                            _cellLabelStyle.normal.textColor = markerCol;
                            GUI.Label(new Rect(center.x - 12, center.y + 4, 24, 10),
                                marker, _cellLabelStyle);
                            _cellLabelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.3f);
                        }

                        // Yon ipucu (kenar hucrelerinde)
                        string dirHint = GetCellDirectionHint(x, y, gw, gh);
                        Color outlineColor;
                        if (_hoverX == x && _hoverY == y)
                            outlineColor = new Color(0.3f, 0.7f, 1f, 1f);
                        else if (!string.IsNullOrEmpty(dirHint))
                            outlineColor = GetDirectionColor(dirHint);
                        else
                            outlineColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
                        DrawDiamondOutline(diamond, outlineColor);

                        // Yon etiketi (kenar hucrelerinde)
                        if (!string.IsNullOrEmpty(dirHint))
                        {
                            _dirHintStyle.normal.textColor = GetDirectionColor(dirHint);
                            GUI.Label(new Rect(center.x - 10, center.y - 12, 20, 12),
                                dirHint, _dirHintStyle);
                        }
                    }
                }

                // Input
                if (gridRect.Contains(e.mousePosition))
                {
                    if (e.type == EventType.MouseDown && _hoverX >= 0 && _hoverY >= 0)
                    {
                        _dragButton = e.button;
                        PaintCell(_hoverX, _hoverY, gw, _dragButton);
                        _isDragging = true;
                        _lastPaintedX = _hoverX;
                        _lastPaintedY = _hoverY;
                        e.Use();
                        Repaint();
                    }

                    if (e.type == EventType.MouseDrag && _isDragging &&
                        _hoverX >= 0 && _hoverY >= 0 &&
                        (_hoverX != _lastPaintedX || _hoverY != _lastPaintedY))
                    {
                        PaintCell(_hoverX, _hoverY, gw, _dragButton);
                        _lastPaintedX = _hoverX;
                        _lastPaintedY = _hoverY;
                        e.Use();
                        Repaint();
                    }

                    if (e.type == EventType.MouseMove)
                        Repaint();
                }

                if (e.type == EventType.MouseUp)
                {
                    _isDragging = false;
                    _dragButton = -1;
                    _lastPaintedX = -1;
                    _lastPaintedY = -1;
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void PaintCell(int x, int y, int gridWidth, int mouseButton)
        {
            TileBase[] target = _selectedLayer == Layer.Base ? _baseLayout : _topLayout;
            if (target == null) return;
            int idx = x + y * gridWidth;
            if (idx < 0 || idx >= target.Length) return;

            if (mouseButton == 0)
            {
                target[idx] = _selectedTile;
                if (_selectedTile != null) AddToRecent(_selectedTile);
                _previewDirty = true;
            }
            else if (mouseButton == 1)
            {
                target[idx] = null;
                _previewDirty = true;
            }
        }

        // ═══════════════════════════════════════════════
        // BOLUM 4: PREVIEW — RenderTexture ile gercek izometrik render
        // Gecici Grid+Tilemap+Camera olusturup Unity'nin kendi renderer'ina cizdirir.
        // ═══════════════════════════════════════════════
        private void DrawPreviewSection()
        {
            _foldoutPreview = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutPreview, "PREVIEW (Base + Top)");
            if (_foldoutPreview)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (_previewDirty && _baseLayout != null)
                    RenderPreview();

                if (_previewTexture != null)
                {
                    float aspect = (float)_previewTexture.width / _previewTexture.height;
                    float previewWidth = Mathf.Min(400f, EditorGUIUtility.currentViewWidth - 40f);
                    float previewHeight = previewWidth / aspect;
                    Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
                    EditorGUI.DrawRect(previewRect, new Color(0.12f, 0.12f, 0.12f, 1f));
                    GUI.DrawTexture(previewRect, _previewTexture, ScaleMode.ScaleToFit);
                }
                else
                {
                    EditorGUILayout.HelpBox("Tile atayarak preview goruntusu olusturun.", MessageType.Info);
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        /// <summary>
        /// Gecici Grid + Tilemap + Camera olusturup RenderTexture'a cizer.
        /// Unity'nin Tilemap renderer'i sorting, pivot, overlap'i otomatik halleder.
        /// </summary>
        private void RenderPreview()
        {
            _previewDirty = false;

            if (_config == null || _baseLayout == null) return;
            int gw = _config.GridWidth;
            int gh = _config.GridHeight;

            // Tum tile'lar bos mu kontrol et
            bool anyTile = false;
            for (int i = 0; i < _baseLayout.Length && !anyTile; i++)
                anyTile = _baseLayout[i] != null;
            if (_topLayout != null)
                for (int i = 0; i < _topLayout.Length && !anyTile; i++)
                    anyTile = _topLayout[i] != null;
            if (!anyTile)
            {
                if (_previewTexture != null)
                {
                    DestroyImmediate(_previewTexture);
                    _previewTexture = null;
                }
                return;
            }

            // ── 1. Gecici objeler olustur ──
            var rootGO = new GameObject("_TileComposerPreview");
            rootGO.hideFlags = HideFlags.HideAndDontSave;

            var grid = rootGO.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 0.5f, 1f);
            grid.cellLayout = GridLayout.CellLayout.IsometricZAsY;

            // Base layer tilemap
            var baseGO = new GameObject("base");
            baseGO.transform.SetParent(rootGO.transform);
            baseGO.hideFlags = HideFlags.HideAndDontSave;
            var baseMap = baseGO.AddComponent<Tilemap>();
            var baseRenderer = baseGO.AddComponent<TilemapRenderer>();
            baseRenderer.sortOrder = TilemapRenderer.SortOrder.TopRight;
            baseRenderer.sortingOrder = 0;
            baseRenderer.mode = TilemapRenderer.Mode.Individual;

            // Top layer tilemap
            var topGO = new GameObject("top");
            topGO.transform.SetParent(rootGO.transform);
            topGO.hideFlags = HideFlags.HideAndDontSave;
            var topMap = topGO.AddComponent<Tilemap>();
            var topRenderer = topGO.AddComponent<TilemapRenderer>();
            topRenderer.sortOrder = TilemapRenderer.SortOrder.TopRight;
            topRenderer.sortingOrder = 1;
            topRenderer.mode = TilemapRenderer.Mode.Individual;

            // ── 2. Tile'lari yerlestir ──
            // Grid editor: yatay ∝ (x-y), dikey ∝ (x+y) asagi
            // Tilemap:     yatay ∝ (tx-ty), dikey ∝ (tx+ty) yukari
            // Eslestirme: tx = gh-1-y, ty = gw-1-x (swap + flip)
            for (int y = 0; y < gh; y++)
            {
                for (int x = 0; x < gw; x++)
                {
                    int idx = x + y * gw;
                    var pos = new Vector3Int(gh - 1 - y, gw - 1 - x, 0);
                    if (idx < _baseLayout.Length && _baseLayout[idx] != null)
                        baseMap.SetTile(pos, _baseLayout[idx]);
                    if (_topLayout != null && idx < _topLayout.Length && _topLayout[idx] != null)
                        topMap.SetTile(pos, _topLayout[idx]);
                }
            }

            // ── 3. Kamera olustur ve pozisyonla ──
            var camGO = new GameObject("cam");
            camGO.hideFlags = HideFlags.HideAndDontSave;
            var camera = camGO.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);
            camera.cullingMask = ~0; // Everything
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.enabled = false; // manuel Render() cagrilacak

            // Sprite yukseklik tasmasi (pivot ~0.19 → sprite'in %81'i yukariya tasar)
            float spriteWorldHeight = 256f / 127f; // ≈ 2.016 unit
            float pivotY = 0.19f;
            float topOverflow = spriteWorldHeight * (1f - pivotY); // ≈ 1.633

            // Tile'lar (0,0)-(gh-1, gw-1) araliginda — 4 kose ile sinirlari bul
            Vector3 c0 = grid.CellToWorld(new Vector3Int(0, 0, 0));
            Vector3 c1 = grid.CellToWorld(new Vector3Int(gh - 1, 0, 0));
            Vector3 c2 = grid.CellToWorld(new Vector3Int(0, gw - 1, 0));
            Vector3 c3 = grid.CellToWorld(new Vector3Int(gh - 1, gw - 1, 0));

            float minX = Mathf.Min(c0.x, c1.x, c2.x, c3.x) - spriteWorldHeight * 0.5f;
            float maxX = Mathf.Max(c0.x, c1.x, c2.x, c3.x) + spriteWorldHeight * 0.5f;
            float minY = Mathf.Min(c0.y, c1.y, c2.y, c3.y) - spriteWorldHeight * pivotY;
            float maxY = Mathf.Max(c0.y, c1.y, c2.y, c3.y) + topOverflow;

            float worldW = maxX - minX;
            float worldH = maxY - minY;
            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;

            camera.transform.position = new Vector3(centerX, centerY, -10f);
            float aspect = 1f; // kare render texture
            camera.orthographicSize = Mathf.Max(worldH * 0.5f, worldW * 0.5f / aspect) + 0.3f;

            // ── 4. Render al ──
            int texSize = 512;
            RenderTexture rt = RenderTexture.GetTemporary(texSize, texSize, 16, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;
            camera.Render();

            // GPU → CPU kopyala
            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            if (_previewTexture == null || _previewTexture.width != texSize || _previewTexture.height != texSize)
            {
                if (_previewTexture != null) DestroyImmediate(_previewTexture);
                _previewTexture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
                _previewTexture.hideFlags = HideFlags.HideAndDontSave;
            }
            _previewTexture.ReadPixels(new Rect(0, 0, texSize, texSize), 0, 0);
            _previewTexture.Apply();
            RenderTexture.active = prevActive;

            // ── 5. Temizle ──
            RenderTexture.ReleaseTemporary(rt);
            camera.targetTexture = null;
            DestroyImmediate(camGO);
            DestroyImmediate(rootGO);
        }

        // ═══════════════════════════════════════════════
        // BOLUM 5: AKSIYONLAR
        // ═══════════════════════════════════════════════
        private void DrawActionButtons()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save to SO", GUILayout.Height(30)))
                SaveToConfig();
            if (GUILayout.Button("Clear All", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Clear All",
                    "Tum tile atamalari silinecek. Emin misiniz?", "Evet", "Iptal"))
                    ClearAll();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }

        // ═══════════════════════════════════════════════
        // SCAN & FILTER
        // ═══════════════════════════════════════════════
        private void ScanTileFolder()
        {
            _tileEntries.Clear();
            _categoryIndices.Clear();
            _categoryNames.Clear();
            _loadedTiles.Clear();
            _spriteCache.Clear();
            _filteredIndices = null;

            if (_tileFolder == null) return;
            string folderPath = AssetDatabase.GetAssetPath(_tileFolder);
            if (!AssetDatabase.IsValidFolder(folderPath)) return;

            string[] guids = AssetDatabase.FindAssets("t:TileBase", new[] { folderPath });

            for (int i = 0; i < guids.Length; i++)
            {
                string guid = guids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

                _tileEntries.Add(new TileEntry
                {
                    Guid = guid,
                    Path = path,
                    Name = fileName,
                    Category = ExtractCategory(fileName),
                    Direction = ExtractDirection(fileName)
                });
            }

            // Isme gore sirala
            _tileEntries = _tileEntries
                .OrderBy(e => e.Name, System.StringComparer.Ordinal).ToList();

            // Kategori index'leri
            for (int i = 0; i < _tileEntries.Count; i++)
            {
                string cat = _tileEntries[i].Category;
                if (!_categoryIndices.ContainsKey(cat))
                    _categoryIndices[cat] = new List<int>();
                _categoryIndices[cat].Add(i);
            }
            _categoryNames = _categoryIndices.Keys.OrderBy(k => k).ToList();
        }

        private string ExtractCategory(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Other";
            int sp = name.IndexOf(' ');
            if (sp > 0) return name.Substring(0, sp);
            for (int i = 1; i < name.Length; i++)
                if (char.IsUpper(name[i]) && char.IsLower(name[i - 1]))
                    return name.Substring(0, i);
            return "Other";
        }

        /// <summary>
        /// Tile isminden yon cikar. "Wall D14_S" → "S", "Roof B1_N" → "N".
        /// Son '_' karakterinden sonraki tek harf.
        /// </summary>
        private string ExtractDirection(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            int lastUnderscore = name.LastIndexOf('_');
            if (lastUnderscore >= 0 && lastUnderscore == name.Length - 2)
            {
                char dir = char.ToUpper(name[name.Length - 1]);
                if (dir == 'N' || dir == 'S' || dir == 'E' || dir == 'W')
                    return dir.ToString();
            }
            return "";
        }

        private TileBase LazyLoadTile(string guid, string path)
        {
            if (_loadedTiles.TryGetValue(guid, out var cached))
                return cached;
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            _loadedTiles[guid] = tile;
            return tile;
        }

        private List<int> GetFilteredIndices()
        {
            if (_filteredIndices != null &&
                _lastCategory == _selectedCategory &&
                _lastDirection == _selectedDirection &&
                _lastSearch == _searchFilter)
                return _filteredIndices;

            _lastCategory = _selectedCategory;
            _lastDirection = _selectedDirection;
            _lastSearch = _searchFilter;

            IEnumerable<int> source;
            if (_selectedCategory == "All" || !_categoryIndices.ContainsKey(_selectedCategory))
                source = Enumerable.Range(0, _tileEntries.Count);
            else
                source = _categoryIndices[_selectedCategory];

            // Yon filtresi
            if (_selectedDirection != "All")
                source = source.Where(i => _tileEntries[i].Direction == _selectedDirection);

            // Arama filtresi
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                string filter = _searchFilter.ToLowerInvariant();
                source = source.Where(i => _tileEntries[i].Name.ToLowerInvariant().Contains(filter));
            }

            _filteredIndices = source.ToList();
            return _filteredIndices;
        }

        private void AddToRecent(TileBase tile)
        {
            if (tile == null) return;
            _recentTiles.Remove(tile);
            _recentTiles.Insert(0, tile);
            if (_recentTiles.Count > MaxRecentTiles)
                _recentTiles.RemoveAt(_recentTiles.Count - 1);
        }

        // ═══════════════════════════════════════════════
        // VERI ISLEMLERI
        // ═══════════════════════════════════════════════
        private void LoadFromConfig()
        {
            if (_config == null) return;
            int size = _config.GridWidth * _config.GridHeight;
            _baseLayout = (_config.TileLayoutBase != null && _config.TileLayoutBase.Length == size)
                ? (TileBase[])_config.TileLayoutBase.Clone() : new TileBase[size];
            _topLayout = (_config.TileLayoutTop != null && _config.TileLayoutTop.Length == size)
                ? (TileBase[])_config.TileLayoutTop.Clone() : new TileBase[size];
            _previewDirty = true;
        }

        private void SaveToConfig()
        {
            if (_config == null) return;
            Undo.RecordObject(_config, "Building Tile Composer - Save Layout");
            _config.TileLayoutBase = (TileBase[])_baseLayout.Clone();
            _config.TileLayoutTop = (TileBase[])_topLayout.Clone();
            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
            Debug.Log($"[TileComposer] {_config.DisplayName} layout kaydedildi.");
        }

        private void ClearAll()
        {
            if (_baseLayout != null) System.Array.Clear(_baseLayout, 0, _baseLayout.Length);
            if (_topLayout != null) System.Array.Clear(_topLayout, 0, _topLayout.Length);
            _previewDirty = true;
            Repaint();
        }

        // ═══════════════════════════════════════════════
        // SPRITE RENDERING (AssetPreview YERINE)
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Sprite texture'ini dogrudan ciz — AssetPreview'dan 100x hizli.
        /// Spritesheet'ten dogru UV rect ile keser.
        /// </summary>
        private void DrawSpriteThumb(Rect rect, TileBase tile)
        {
            Sprite sprite = GetSprite(tile);
            if (sprite == null || sprite.texture == null) return;

            Texture2D tex = sprite.texture;
            Rect texRect = sprite.textureRect;
            Rect uv = new Rect(
                texRect.x / tex.width,
                texRect.y / tex.height,
                texRect.width / tex.width,
                texRect.height / tex.height);
            GUI.DrawTextureWithTexCoords(rect, tex, uv);
        }

        /// <summary>
        /// TileBase'den Sprite al (cache'li).
        /// </summary>
        private Sprite GetSprite(TileBase tile)
        {
            if (tile == null) return null;

            int id = tile.GetInstanceID();
            if (_spriteCache.TryGetValue(id, out var cached))
                return cached;

            Sprite sprite = null;
            if (tile is Tile t)
                sprite = t.sprite;

            _spriteCache[id] = sprite;
            return sprite;
        }

        // ═══════════════════════════════════════════════
        // DIAMOND YARDIMCILARI
        // ═══════════════════════════════════════════════
        private Vector2 GetDiamondCenter(Vector2 origin, int x, int y,
            float cellW = CellW, float cellH = CellH)
        {
            return new Vector2(
                origin.x + (x - y) * cellW * 0.5f,
                origin.y + (x + y) * cellH * 0.5f);
        }

        private Vector2[] GetDiamondVerts(Vector2 c, float cellW, float cellH)
        {
            float hw = cellW * 0.5f, hh = cellH * 0.5f;
            return new[]
            {
                new Vector2(c.x, c.y - hh),
                new Vector2(c.x + hw, c.y),
                new Vector2(c.x, c.y + hh),
                new Vector2(c.x - hw, c.y)
            };
        }

        private bool IsPointInDiamond(Vector2 p, Vector2[] v)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector2 a = v[i], b = v[(i + 1) % 4];
                if ((b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x) < 0) return false;
            }
            return true;
        }

        private void DrawDiamondOutline(Vector2[] v, Color color)
        {
            Handles.color = color;
            for (int i = 0; i < 4; i++)
                Handles.DrawLine(
                    new Vector3(v[i].x, v[i].y, 0),
                    new Vector3(v[(i + 1) % 4].x, v[(i + 1) % 4].y, 0));
        }

        private void DrawTileInDiamond(Vector2 center, TileBase tile, float cellW, float cellH)
        {
            Sprite sprite = GetSprite(tile);
            if (sprite == null || sprite.texture == null) return;

            float size = Mathf.Min(cellW, cellH) * 0.8f;
            Rect drawRect = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
            DrawSpriteRect(drawRect, sprite);
        }

        private void DrawSpriteRect(Rect drawRect, Sprite sprite)
        {
            Texture2D tex = sprite.texture;
            Rect texRect = sprite.textureRect;
            Rect uv = new Rect(
                texRect.x / tex.width,
                texRect.y / tex.height,
                texRect.width / tex.width,
                texRect.height / tex.height);
            GUI.DrawTextureWithTexCoords(drawRect, tex, uv);
        }

        /// <summary>
        /// Izometrik diamond grid'de hucre kenarinin onerilen tile yonu.
        /// Diamond gorunumde:
        ///   - Sol ust kenar (x=0)     → _W tile kullan
        ///   - Sag ust kenar (y=0)     → _N tile kullan
        ///   - Sag alt kenar (x=max)   → _E tile kullan
        ///   - Sol alt kenar (y=max)   → _S tile kullan
        /// Kose hucreleri iki yon alir. Ic hucreler bos doner.
        /// </summary>
        private string GetCellDirectionHint(int x, int y, int gw, int gh)
        {
            bool isXMin = (x == 0);
            bool isXMax = (x == gw - 1);
            bool isYMin = (y == 0);
            bool isYMax = (y == gh - 1);

            // Tek kenar
            if (isXMin && !isYMin && !isYMax) return "W";
            if (isXMax && !isYMin && !isYMax) return "E";
            if (isYMin && !isXMin && !isXMax) return "N";
            if (isYMax && !isXMin && !isXMax) return "S";

            // Kose hucreleri
            if (isXMin && isYMin) return "NW";
            if (isXMax && isYMin) return "NE";
            if (isXMin && isYMax) return "SW";
            if (isXMax && isYMax) return "SE";

            return "";
        }

        private Color GetDirectionColor(string dir)
        {
            // Her yon farkli renk — grid'de bakinca hangi kenar hangi yon aninda anlasilir
            if (dir.Contains("N")) return new Color(0.4f, 1f, 0.4f, 0.9f);   // yesil
            if (dir.Contains("S")) return new Color(1f, 0.5f, 0.3f, 0.9f);   // turuncu
            if (dir.Contains("E")) return new Color(0.5f, 0.8f, 1f, 0.9f);   // mavi
            if (dir.Contains("W")) return new Color(1f, 1f, 0.4f, 0.9f);     // sari
            return new Color(1f, 1f, 1f, 0.5f);
        }

        private void DrawSeparator()
        {
            EditorGUILayout.Space(2);
            Rect r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(2);
        }
    }
}
