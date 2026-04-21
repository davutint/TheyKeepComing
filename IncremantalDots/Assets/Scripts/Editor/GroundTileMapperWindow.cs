#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeadWalls
{
    /// <summary>
    /// Ground tile esleme araci.
    /// 16 olasi 4-bit komsu pattern'i (N/E/S/W) icin
    /// hangi Ground A-serisi tile kullanilacagini gorsel olarak belirlemeye yarar.
    /// Esleme EditorPrefs'e kaydedilir, MapImporterWindow.PaintGround2Layer() tarafindan okunur.
    /// </summary>
    public class GroundTileMapperWindow : EditorWindow
    {
        // ─── EditorPrefs key prefix ───────────────────
        private const string KeyPrefix = "DeadWalls_GroundMapper_";

        // ─── 16 mask slot (index = 4-bit mask) ───────
        // Bit 3=Kuzey, Bit 2=Dogu, Bit 1=Guney, Bit 0=Bati
        private readonly TileBase[] _maskTiles = new TileBase[16];

        // ─── Referans paleti ──────────────────────────
        private List<TileBase> _paletteTiles;
        private readonly Dictionary<int, Sprite> _spriteCache = new();

        // ─── UI state ─────────────────────────────────
        private Vector2 _scrollPos;
        private bool _foldMapping = true;
        private bool _foldPalette = true;

        // ─── Sabitler ─────────────────────────────────
        private const float ThumbSize = 52f;
        private const string TileFolder =
            "Assets/SmallScaleInt/Fantasy kingdom Tileset/Environment/Tiles";


        [MenuItem("Window/DeadWalls/Ground Tile Mapper")]
        public static void ShowWindow()
        {
            var window = GetWindow<GroundTileMapperWindow>("Ground Tile Mapper");
            window.minSize = new Vector2(420, 500);
        }

        // ═══════════════════════════════════════════════
        // PERSISTENCE
        // ═══════════════════════════════════════════════

        private void OnEnable()
        {
            LoadMappings();
            ScanPaletteTiles();
        }

        private void OnDisable()
        {
            SaveMappings();
        }

        private void LoadMappings()
        {
            for (int i = 0; i < 16; i++)
            {
                string guid = EditorPrefs.GetString(KeyPrefix + "Mask" + i, "");
                if (string.IsNullOrEmpty(guid)) { _maskTiles[i] = null; continue; }
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) { _maskTiles[i] = null; continue; }
                _maskTiles[i] = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            }
        }

        private void SaveMappings()
        {
            for (int i = 0; i < 16; i++)
            {
                if (_maskTiles[i] == null)
                {
                    EditorPrefs.DeleteKey(KeyPrefix + "Mask" + i);
                    continue;
                }
                string path = AssetDatabase.GetAssetPath(_maskTiles[i]);
                string guid = AssetDatabase.AssetPathToGUID(path);
                EditorPrefs.SetString(KeyPrefix + "Mask" + i, guid);
            }
        }

        /// <summary>Mapper'da en az 1 esleme var mi? MapImporterWindow tarafindan da kullanilir.</summary>
        public static bool HasAnyMapping()
        {
            for (int i = 0; i < 16; i++)
            {
                if (!string.IsNullOrEmpty(EditorPrefs.GetString(KeyPrefix + "Mask" + i, "")))
                    return true;
            }
            return false;
        }

        /// <summary>16 mask→tile eslesmesini EditorPrefs'ten yukler. MapImporterWindow tarafindan cagirilir.</summary>
        public static TileBase[] LoadMapperTiles()
        {
            var tiles = new TileBase[16];
            for (int i = 0; i < 16; i++)
            {
                string guid = EditorPrefs.GetString(KeyPrefix + "Mask" + i, "");
                if (string.IsNullOrEmpty(guid)) continue;
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;
                tiles[i] = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            }
            return tiles;
        }

        // ═══════════════════════════════════════════════
        // PALET TARAMA
        // ═══════════════════════════════════════════════

        /// <summary>Ground A serisi tile asset'lerini tarar (tum yonler: _E, _N, _S, _W).</summary>
        private void ScanPaletteTiles()
        {
            _paletteTiles = new List<TileBase>();
            string[] guids = AssetDatabase.FindAssets("Ground A t:TileBase",
                new[] { TileFolder });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                // _0 varyantlarini atla (duplikat)
                if (path.Contains("_0.")) continue;

                var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                if (tile != null)
                    _paletteTiles.Add(tile);
            }

            // Isme gore sirala
            _paletteTiles.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        }

        // ═══════════════════════════════════════════════
        // OnGUI
        // ═══════════════════════════════════════════════

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawHeader();
            DrawMappingSection();
            DrawPaletteSection();
            DrawActionsSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("\u25c6 GROUND TILE MAPPER", EditorStyles.boldLabel);

            // Doluluk durumu
            int filled = 0;
            for (int i = 0; i < 16; i++)
                if (_maskTiles[i] != null) filled++;
            EditorGUILayout.LabelField($"Atanan: {filled}/16 pattern", EditorStyles.miniLabel);

            DrawSeparator();
        }

        // ═══════════════════════════════════════════════
        // BOLUM 1: MASK ESLEME (16 satir)
        // ═══════════════════════════════════════════════

        private void DrawMappingSection()
        {
            _foldMapping = EditorGUILayout.BeginFoldoutHeaderGroup(_foldMapping, "MASK ESLEME (16 pattern)");
            if (_foldMapping)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.HelpBox(
                    "Her satirdaki diamond 4 komsunu gosterir (ust=K, sag=D, alt=G, sol=B).\n" +
                    "Yesil = cimen komsu, Kahverengi = toprak komsu.\n" +
                    "Diamond'a bakarak uygun tile'i slot'a surukle.",
                    MessageType.Info);

                EditorGUI.BeginChangeCheck();

                for (int mask = 0; mask < 16; mask++)
                {
                    DrawMaskRow(mask);
                }

                if (EditorGUI.EndChangeCheck())
                    SaveMappings();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawMaskRow(int mask)
        {
            EditorGUILayout.BeginHorizontal();

            // 1. Mask numarasi
            EditorGUILayout.LabelField($"{mask:D2}", GUILayout.Width(22));

            // 2. Gorsel komsu gosterimi (K D G B — renkli)
            DrawNeighborIndicator(mask);

            // 3. Sprite thumbnail (mevcut atama)
            Rect thumbRect = GUILayoutUtility.GetRect(ThumbSize, ThumbSize,
                GUILayout.Width(ThumbSize), GUILayout.Height(ThumbSize));
            if (_maskTiles[mask] != null)
                DrawSpriteThumb(thumbRect, _maskTiles[mask]);
            else
                EditorGUI.DrawRect(thumbRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));

            // 4. ObjectField slot
            _maskTiles[mask] = (TileBase)EditorGUILayout.ObjectField(
                _maskTiles[mask], typeof(TileBase), false);

            // 5. Kisa aciklama
            EditorGUILayout.LabelField(MaskDescription(mask),
                EditorStyles.miniLabel, GUILayout.Width(160));

            EditorGUILayout.EndHorizontal();
        }

        // ─── Diamond gorsel sabitleri ─────────────────
        private static readonly Color ColorGrass = new Color(0.30f, 0.69f, 0.31f); // yesil
        private static readonly Color ColorDirt = new Color(0.55f, 0.27f, 0.07f);  // kahverengi
        private static readonly Color ColorCenter = new Color(0.85f, 0.85f, 0.85f); // acik gri
        private const float DiamondSize = 40f; // toplam alan
        private const float CellSize = 12f;    // her hucre boyutu
        private const float CellGap = 2f;      // hucre arasi bosluk

        /// <summary>
        /// Mask icin diamond seklinde gorsel komsu gosterimi cizer.
        ///       [K]
        ///    [B] ● [D]
        ///       [G]
        /// Yesil = cimen komsu, Kahverengi = toprak komsu.
        /// </summary>
        private static void DrawNeighborIndicator(int mask)
        {
            Rect area = GUILayoutUtility.GetRect(DiamondSize, DiamondSize,
                GUILayout.Width(DiamondSize), GUILayout.Height(DiamondSize));

            float cx = area.x + DiamondSize * 0.5f;
            float cy = area.y + DiamondSize * 0.5f;
            float half = CellSize * 0.5f;
            float offset = half + CellGap + half; // merkez → komsu arasi mesafe

            // Merkez hucre (●)
            EditorGUI.DrawRect(new Rect(cx - half, cy - half, CellSize, CellSize), ColorCenter);

            // Kuzey (ust) — bit 3
            bool nGrass = (mask & 8) != 0;
            EditorGUI.DrawRect(new Rect(cx - half, cy - offset - half, CellSize, CellSize),
                nGrass ? ColorGrass : ColorDirt);

            // Dogu (sag) — bit 2
            bool eGrass = (mask & 4) != 0;
            EditorGUI.DrawRect(new Rect(cx + offset - half, cy - half, CellSize, CellSize),
                eGrass ? ColorGrass : ColorDirt);

            // Guney (alt) — bit 1
            bool sGrass = (mask & 2) != 0;
            EditorGUI.DrawRect(new Rect(cx - half, cy + offset - half, CellSize, CellSize),
                sGrass ? ColorGrass : ColorDirt);

            // Bati (sol) — bit 0
            bool wGrass = (mask & 1) != 0;
            EditorGUI.DrawRect(new Rect(cx - offset - half, cy - half, CellSize, CellSize),
                wGrass ? ColorGrass : ColorDirt);
        }

        /// <summary>
        /// Mask icin cimen perspektifinden aciklama uretir.
        /// Kahverengi taraflar = gecis kenari, yesil = devam eden cimen.
        /// </summary>
        private static string MaskDescription(int mask)
        {
            // Ozel durumlar
            if (mask == 15) return "Tam cimen";
            if (mask == 0) return "Izole cimen";

            // Hangi taraflar acik (kahverengi = gecis kenari)?
            bool kAcik = (mask & 8) == 0; // ust
            bool dAcik = (mask & 4) == 0; // sag
            bool gAcik = (mask & 2) == 0; // alt
            bool bAcik = (mask & 1) == 0; // sol

            int kenarSayisi = (kAcik ? 1 : 0) + (dAcik ? 1 : 0)
                + (gAcik ? 1 : 0) + (bAcik ? 1 : 0);

            if (kenarSayisi == 1)
            {
                // Tek kenar gecisi
                if (kAcik) return "Ust kenar";
                if (dAcik) return "Sag kenar";
                if (gAcik) return "Alt kenar";
                return "Sol kenar";
            }

            if (kenarSayisi == 2)
            {
                // Karsilikli mi yoksa komsumu?
                if (kAcik && gAcik) return "Yatay serit";
                if (dAcik && bAcik) return "Dikey serit";
                // Kose
                if (kAcik && dAcik) return "Sag-ust kose";
                if (dAcik && gAcik) return "Sag-alt kose";
                if (gAcik && bAcik) return "Sol-alt kose";
                return "Sol-ust kose";
            }

            // 3 kenar acik — sadece 1 yon cimen
            if (!kAcik) return "Sadece ust cimen";
            if (!dAcik) return "Sadece sag cimen";
            if (!gAcik) return "Sadece alt cimen";
            return "Sadece sol cimen";
        }

        // ═══════════════════════════════════════════════
        // BOLUM 2: REFERANS PALETI
        // ═══════════════════════════════════════════════

        private void DrawPaletteSection()
        {
            _foldPalette = EditorGUILayout.BeginFoldoutHeaderGroup(_foldPalette,
                $"REFERANS PALETI ({(_paletteTiles?.Count ?? 0)} tile)");
            if (_foldPalette)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.HelpBox(
                    "Tile'i buradan yukardaki slot'a surukle-birak.",
                    MessageType.Info);

                if (_paletteTiles == null || _paletteTiles.Count == 0)
                {
                    EditorGUILayout.LabelField("Tile bulunamadi!", EditorStyles.miniLabel);
                }
                else
                {
                    // Grid seklinde thumbnail'leri goster
                    float windowWidth = position.width - 40;
                    int cols = Mathf.Max(1, (int)(windowWidth / (ThumbSize + 30)));

                    int col = 0;
                    EditorGUILayout.BeginHorizontal();

                    foreach (var tile in _paletteTiles)
                    {
                        if (col >= cols)
                        {
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                            col = 0;
                        }

                        EditorGUILayout.BeginVertical(GUILayout.Width(ThumbSize + 6));

                        Rect thumbRect = GUILayoutUtility.GetRect(ThumbSize, ThumbSize,
                            GUILayout.Width(ThumbSize), GUILayout.Height(ThumbSize));

                        // Arka plan + sprite
                        EditorGUI.DrawRect(thumbRect, new Color(0.15f, 0.15f, 0.15f, 1f));
                        DrawSpriteThumb(thumbRect, tile);

                        // Drag desteği — tiklayip surukleyince ObjectField'a birakilabilir
                        if (Event.current.type == EventType.MouseDrag
                            && thumbRect.Contains(Event.current.mousePosition))
                        {
                            DragAndDrop.PrepareStartDrag();
                            DragAndDrop.objectReferences = new Object[] { tile };
                            DragAndDrop.StartDrag(tile.name);
                            Event.current.Use();
                        }

                        // Kisa isim (Ground A3_E → A3_E)
                        string shortName = tile.name.Replace("Ground ", "");
                        EditorGUILayout.LabelField(shortName,
                            EditorStyles.centeredGreyMiniLabel,
                            GUILayout.Width(ThumbSize + 6));

                        EditorGUILayout.EndVertical();
                        col++;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ═══════════════════════════════════════════════
        // BOLUM 3: ISLEMLER
        // ═══════════════════════════════════════════════

        private void DrawActionsSection()
        {
            DrawSeparator();

            // Otomatik doldur butonu
            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.7f, 0.9f);
            if (GUILayout.Button("Otomatik Doldur (9 emin esleme)", GUILayout.Height(32)))
            {
                AutoFillMappings();
            }
            GUI.backgroundColor = oldBg;

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Tum Atamalari Temizle", GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog("Temizle",
                    "16 mask atamasinin tamami silinecek. Emin misin?",
                    "Evet", "Iptal"))
                {
                    for (int i = 0; i < 16; i++)
                        _maskTiles[i] = null;
                    SaveMappings();
                }
            }

            if (GUILayout.Button("Paleti Yenile", GUILayout.Height(28)))
            {
                _spriteCache.Clear();
                ScanPaletteTiles();
            }

            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════
        // OTOMATIK DOLDURMA
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Gorsel analiz sonucu emin olunan 9 eslemeyi otomatik doldurur.
        /// Geri kalan 7 mask icin mantikli fallback atar.
        /// Mapping tablosu:
        ///   mask 15 (tam cimen)        → A2_E
        ///   mask 6  (sol-ust kose)     → A3_E
        ///   mask 9  (sag-alt kose)     → A3_W
        ///   mask 3  (sag-ust kose)     → A3_N
        ///   mask 12 (sol-alt kose)     → A3_S
        ///   mask 4  (sadece sag cimen) → A4_E
        ///   mask 1  (sadece sol cimen) → A4_N
        ///   mask 2  (sadece alt cimen) → A4_S
        ///   mask 8  (sadece ust cimen) → A4_W
        ///   mask 7,11,13,14 (tek kenar) → A2_E (tileset'te %75 tile yok)
        ///   mask 5,10 (serit)           → A2_E (fallback)
        ///   mask 0 (izole)              → null
        /// </summary>
        private void AutoFillMappings()
        {
            string basePath = "Assets/SmallScaleInt/Fantasy kingdom Tileset/Environment/Tiles/";

            // Tile'lari yukle
            var a2E = AssetDatabase.LoadAssetAtPath<TileBase>(basePath + "Ground A2_E.asset");
            var a3E = AssetDatabase.LoadAssetAtPath<TileBase>(basePath + "Ground A3_E.asset");
            var a3N = AssetDatabase.LoadAssetAtPath<TileBase>(basePath + "Ground A3_N.asset");
            var a3S = AssetDatabase.LoadAssetAtPath<TileBase>(basePath + "Ground A3_S.asset");
            var a3W = AssetDatabase.LoadAssetAtPath<TileBase>(basePath + "Ground A3_W.asset");
            var a4E = AssetDatabase.LoadAssetAtPath<TileBase>(basePath + "Ground A4_E.asset");
            var a4N = AssetDatabase.LoadAssetAtPath<TileBase>(basePath + "Ground A4_N.asset");
            var a4S = AssetDatabase.LoadAssetAtPath<TileBase>(basePath + "Ground A4_S.asset");
            var a4W = AssetDatabase.LoadAssetAtPath<TileBase>(basePath + "Ground A4_W.asset");

            if (a2E == null)
            {
                Debug.LogError("[GroundTileMapper] Ground A2_E.asset bulunamadi! Path: " + basePath);
                return;
            }

            // ── Emin olunan 9 esleme ──

            // Tam cimen (4 komsu cimen)
            _maskTiles[15] = a2E;

            // Kose esleme (2 bitisik komsu toprak)
            _maskTiles[6]  = a3E;  // sol-ust kose (K+B toprak)
            _maskTiles[9]  = a3W;  // sag-alt kose (D+G toprak)
            _maskTiles[3]  = a3N;  // sag-ust kose (K+D toprak)
            _maskTiles[12] = a3S;  // sol-alt kose (G+B toprak)

            // Tek komsu cimen (3 komsu toprak)
            _maskTiles[4]  = a4E;  // sadece sag (dogu) cimen
            _maskTiles[1]  = a4N;  // sadece sol (bati) cimen
            _maskTiles[2]  = a4S;  // sadece alt (guney) cimen
            _maskTiles[8]  = a4W;  // sadece ust (kuzey) cimen

            // ── Fallback eslemeler (emin degil) ──

            // Tek kenar toprak (3 komsu cimen) — tileset'te ozel tile yok, A2 kullan
            _maskTiles[7]  = a2E;  // ust kenar
            _maskTiles[11] = a2E;  // sag kenar
            _maskTiles[13] = a2E;  // alt kenar
            _maskTiles[14] = a2E;  // sol kenar

            // Karsilikli serit — A2 fallback
            _maskTiles[5]  = a2E;  // yatay serit
            _maskTiles[10] = a2E;  // dikey serit

            // Izole (4 komsu toprak) — null (base A1 gorunur)
            _maskTiles[0]  = null;

            SaveMappings();
            _spriteCache.Clear();

            int loaded = 0;
            for (int i = 0; i < 16; i++)
                if (_maskTiles[i] != null) loaded++;

            Debug.Log($"[GroundTileMapper] Otomatik dolduruldu: {loaded}/16 slot " +
                "(9 emin + 6 fallback A2 + 1 null)");
        }

        // ═══════════════════════════════════════════════
        // SPRITE YARDIMCILARI
        // ═══════════════════════════════════════════════

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
