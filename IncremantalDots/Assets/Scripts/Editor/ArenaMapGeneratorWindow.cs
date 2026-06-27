#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeadWalls
{
    /// <summary>
    /// Arena Map Generator — seed-tabanli tek-tik izometrik arena haritasi uretir.
    /// Hedef: NewGameScene / WorldVisualRoot / MobileArenaGrid tilemap katmanlari (TAMAMEN GORSEL, ECS etkilenmez).
    /// Onizleme = canli sahne: GENERATE aninda gercek arenaya boyar, Game view'da gorunur. Tek-undo.
    /// Tile'lar Fantasy kingdom Tileset (Environment/Tiles) duz UnityEngine.Tilemaps.Tile asset'leri.
    /// Geciler marching-squares kose tile'i ile DEGIL, asset'in scatter-overlay decal'leriyle yapilir (A3/A6/A12).
    /// </summary>
    public class ArenaMapGeneratorWindow : EditorWindow
    {
        // ─── Sabit yollar ───────────────────────────────
        private const string TilesRoot = "Assets/SmallScaleInt/Fantasy kingdom Tileset/Environment/Tiles";
        private const string RootName = "WorldVisualRoot";
        private const string GridName = "MobileArenaGrid";

        // ─── Generator-owned katman isimleri + sorting ──
        // GroundTilemap mevcut setup tool tarafindan -50'de olusturuluyor; onu yeniden kullaniyoruz.
        private const string GroundLayer = "GroundTilemap";          // -50  biome taban zemin
        private const string PathLayer = "ArenaPathTilemap";         // -49  yollar (doseme)
        private const string OverlayLayer = "ArenaOverlayTilemap";   // -48  cim gecis decal'leri + golge
        private const string DecorLayer = "ArenaDecorTilemap";       // -10  agac/tas/flora/misc + harabe
        private const string RoofLayer = "ArenaRoofTilemap";         //  +2  kule catilari
        private const int GroundOrder = -50;
        private const int PathOrder = -49;
        private const int OverlayOrder = -48;
        private const int DecorOrder = -10;
        private const int RoofOrder = 2;

        // ─── Verified biome FILL tile'lari (kendi gozumle dogrulandi) ──
        private const string TileGrass = "Ground A2";   // tam cim blogu
        private const string TileDirtA = "Ground A1";   // toprak blogu
        private const string TileDirtB = "Ground I1";   // toprak varyanti
        private const string TileRocky = "Ground B1";   // cakilli/tasli toprak
        private const string TileYellow = "Ground J1";  // sari/kuru cim
        // cim gecis overlay decal'leri (kademeli kaplama: yarim -> yama -> seyrek -> cicek)
        private static readonly string[] GrassOverlays = { "Ground A3", "Ground A6", "Ground A12", "Ground A24" };
        // yol/doseme tile'lari
        private static readonly string[] PavingTiles = { "Ground D1", "Ground E1", "Ground H1" };
        // kucuk kaya scatter -- GORSEL DOGRULANMIS kucuk kayalar (monolith Stone A1 / mezar A4-A5 / kuyu A15 HARIC)
        private static readonly string[] RockTiles = { "Stone A2", "Stone A3", "Stone A8", "Stone A9" };

        // ─── Arena geometrisi (mevcut setup ile ayni footprint) ──
        private int _arenaHalf = 13;       // x,y in [-half, half]
        private int _arenaRadius = 22;     // |x|+|y| <= radius (diamond)
        private int _keepClearRadius = 5;  // |x|+|y| <= bu deger: dekor/yapi konmaz (kale + savas ring)

        // ─── Seed ───────────────────────────────────────
        private int _seed = 42;

        // ─── Biome agirliklari (goreli) ─────────────────
        private float _wGrass = 6f;
        private float _wDirt = 2f;
        private float _wRocky = 0.5f;          // dusuk: cok gri/tasli arena cirkin duruyor
        private bool _yellowPatches = true;   // bazi cim alanlarini sari cime cevir
        private float _biomeNoiseScale = 0.10f;
        private float _warpStrength = 1.4f;

        // ─── Dekor yogunluklari (0..1 hucre basina sans) ─
        private float _treeDensity = 0.045f;
        private float _rockDensity = 0.03f;    // kucuk kaya (curated RockTiles)
        private float _floraDensity = 0.05f;   // yesil cali/bitki
        private bool _treeShadows = true;
        // NOT: "Misc" kategorisi (comlek/sandik/kuyu/iskelet) DOGAL DEKOR DEGIL -> kullanilmiyor

        // ─── Yapilar ────────────────────────────────────
        // Varsayilan KAPALI: once temiz cim+agac+cali+kaya tabanini gor, yapilari ayri ac/degerlendir.
        private bool _ruins = false;
        private float _ruinDensity = 0.012f;
        private bool _towers = false;
        private int _towerCount = 4;

        // ─── Yollar ─────────────────────────────────────
        private bool _roads = false;   // varsayilan kapali: doseme spoke'lari gri/dagitik duruyor

        // ─── Durum ──────────────────────────────────────
        private string _status = "Hazir. NewGameScene acik olmali.";
        private Vector2 _scroll;

        // ─── Tile katalogu (GENERATE sirasinda doldurulur) ──
        private Dictionary<string, TileBase> _byName;
        private Dictionary<string, List<TileBase>> _byCategory;

        [MenuItem("Window/DeadWalls/Arena Map Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<ArenaMapGeneratorWindow>("Arena Map Gen");
            window.minSize = new Vector2(340f, 420f);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Arena Map Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Tek tik izometrik arena uretir (gorsel). Onizleme = Scene/Game view. Tek Undo ile geri alinir.\n" +
                "NewGameScene acik + WorldVisualRoot/MobileArenaGrid mevcut olmali (Mobile Castle Scene Setup ile kurulur).",
                MessageType.Info);

            // Seed satiri
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                _seed = EditorGUILayout.IntField("Seed", _seed);
                if (GUILayout.Button("Rastgele", GUILayout.Width(80f)))
                    _seed = UnityEngine.Random.Range(0, 999999);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("< onceki"))
                {
                    _seed = Mathf.Max(0, _seed - 1);
                    Generate();
                }
                if (GUILayout.Button("sonraki >"))
                {
                    _seed++;
                    Generate();
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Biome agirliklari", EditorStyles.boldLabel);
            _wGrass = EditorGUILayout.Slider("Cim", _wGrass, 0f, 10f);
            _wDirt = EditorGUILayout.Slider("Toprak", _wDirt, 0f, 10f);
            _wRocky = EditorGUILayout.Slider("Kayalik", _wRocky, 0f, 10f);
            _yellowPatches = EditorGUILayout.Toggle("Sari cim yamalari", _yellowPatches);
            _biomeNoiseScale = EditorGUILayout.Slider("Biome olcek", _biomeNoiseScale, 0.02f, 0.4f);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Dekor yogunlugu", EditorStyles.boldLabel);
            _treeDensity = EditorGUILayout.Slider("Agac", _treeDensity, 0f, 0.3f);
            _rockDensity = EditorGUILayout.Slider("Kaya", _rockDensity, 0f, 0.3f);
            _floraDensity = EditorGUILayout.Slider("Cali/Flora", _floraDensity, 0f, 0.3f);
            _treeShadows = EditorGUILayout.Toggle("Agac golgesi", _treeShadows);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Yapilar", EditorStyles.boldLabel);
            _ruins = EditorGUILayout.Toggle("Harabe (duvar/molozt)", _ruins);
            _towers = EditorGUILayout.Toggle("Kule", _towers);
            if (_towers)
                _towerCount = EditorGUILayout.IntSlider("Kule sayisi", _towerCount, 0, 12);
            _roads = EditorGUILayout.Toggle("Yol", _roads);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Arena", EditorStyles.boldLabel);
            _arenaRadius = EditorGUILayout.IntSlider("Arena yaricap", _arenaRadius, 8, 26);
            _keepClearRadius = EditorGUILayout.IntSlider("Merkez temiz yaricap", _keepClearRadius, 3, 12);

            EditorGUILayout.Space(10f);
            GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
            if (GUILayout.Button("GENERATE", GUILayout.Height(36f)))
                Generate();
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Temizle (generator katmanlari)", GUILayout.Height(22f)))
                ClearGeneratedLayers();

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(_status, MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        // ════════════════════════════════════════════════
        //  GENERATE pipeline
        // ════════════════════════════════════════════════
        private void Generate()
        {
            try
            {
                Transform gridTransform = FindArenaGrid();
                if (gridTransform == null)
                {
                    _status = "HATA: WorldVisualRoot/MobileArenaGrid bulunamadi. Once 'Mobile Castle Scene Setup' calistir.";
                    return;
                }

                if (_byName == null)
                    BuildCatalog();

                Tilemap ground = EnsureLayer(gridTransform, GroundLayer, GroundOrder, TilemapRenderer.Mode.Individual);
                Tilemap path = EnsureLayer(gridTransform, PathLayer, PathOrder, TilemapRenderer.Mode.Chunk);
                Tilemap overlay = EnsureLayer(gridTransform, OverlayLayer, OverlayOrder, TilemapRenderer.Mode.Chunk);
                Tilemap decor = EnsureLayer(gridTransform, DecorLayer, DecorOrder, TilemapRenderer.Mode.Individual);
                Tilemap roof = EnsureLayer(gridTransform, RoofLayer, RoofOrder, TilemapRenderer.Mode.Individual);

                int undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Arena Map Generate");
                Undo.RegisterCompleteObjectUndo(
                    new UnityEngine.Object[] { ground, path, overlay, decor, roof },
                    "Arena Map Generate");

                ground.ClearAllTiles();
                path.ClearAllTiles();
                overlay.ClearAllTiles();
                decor.ClearAllTiles();
                roof.ClearAllTiles();

                // Seed -> deterministik RNG + noise offsetleri
                var rng = new System.Random(_seed);
                Vector2 noiseOff = new Vector2(_seed * 137.5f % 9173f, _seed * 259.3f % 7919f);
                Vector2 yellowOff = new Vector2(_seed * 71.7f % 4099f, _seed * 53.1f % 3571f);

                // Biome alani onceden hesapla (komsu-cim gecisleri icin lazim)
                var biome = new Dictionary<Vector2Int, int>(); // 0=cim 1=toprak 2=kayalik 3=sari cim

                // 1) ZEMIN (biome fill, yon cesitliligiyle)
                int painted = 0;
                for (int x = -_arenaHalf; x <= _arenaHalf; x++)
                {
                    for (int y = -_arenaHalf; y <= _arenaHalf; y++)
                    {
                        if (Mathf.Abs(x) + Mathf.Abs(y) > _arenaRadius)
                            continue;

                        int b = DecideBiome(x, y, noiseOff, yellowOff);
                        biome[new Vector2Int(x, y)] = b;
                        TileBase fill = BiomeFillTile(b, rng);
                        if (fill != null)
                        {
                            ground.SetTile(new Vector3Int(x, y, 0), fill);
                            painted++;
                        }
                    }
                }

                // 2) CIM GECIS OVERLAY'leri (komsu cim oranina gore kademeli decal)
                foreach (var kv in biome)
                {
                    Vector2Int c = kv.Key;
                    int b = kv.Value;
                    if (b == 0)
                        continue; // zaten tam cim, overlay gereksiz

                    float grassFrac = GrassNeighborFraction(c, biome);
                    if (grassFrac <= 0f)
                        continue;

                    // kaplama kademesi: cok komsu -> yarim decal, az komsu -> seyrek decal
                    string overlayName;
                    if (grassFrac >= 0.6f) overlayName = "Ground A3";   // yarim kaplama
                    else if (grassFrac >= 0.35f) overlayName = "Ground A6";  // yama
                    else overlayName = "Ground A12";                    // seyrek
                    TileBase ov = PickVariant(overlayName, rng);
                    if (ov != null)
                        overlay.SetTile(new Vector3Int(c.x, c.y, 0), ov);
                }

                // 3) YOLLAR (doseme spokes, kale kenarindan disa)
                if (_roads)
                    PaintRoads(path, rng);

                // 4) DEKOR (agac/tas/flora/misc) + golge
                PaintDecor(decor, overlay, rng);

                // 5) YAPILAR (harabe + kule)
                if (_ruins)
                    PaintRuins(decor, rng);
                if (_towers)
                    PaintTowers(decor, roof, rng);

                ground.CompressBounds();
                path.CompressBounds();
                overlay.CompressBounds();
                decor.CompressBounds();
                roof.CompressBounds();

                Undo.CollapseUndoOperations(undoGroup);
                MarkDirty(ground, path, overlay, decor, roof);

                _status = $"Uretildi. Seed={_seed}, {painted} zemin hucresi. (Game view'da gor; Ctrl+Z geri alir)";
            }
            catch (Exception e)
            {
                _status = "HATA: " + e.Message;
                Debug.LogException(e);
            }
        }

        // ─── Biome karari ───────────────────────────────
        private int DecideBiome(int x, int y, Vector2 noiseOff, Vector2 yellowOff)
        {
            // domain-warp + fbm
            float wx = x * _biomeNoiseScale + noiseOff.x;
            float wy = y * _biomeNoiseScale + noiseOff.y;
            float warpX = (Fbm(wx + 11.3f, wy + 5.7f, 2) - 0.5f) * _warpStrength;
            float warpY = (Fbm(wx - 7.1f, wy + 9.9f, 2) - 0.5f) * _warpStrength;
            float n = Fbm(wx + warpX, wy + warpY, 4);

            float total = Mathf.Max(0.0001f, _wGrass + _wDirt + _wRocky);
            float tGrass = _wGrass / total;
            float tDirt = (_wGrass + _wDirt) / total;

            int b;
            if (n < tGrass) b = 0;        // cim
            else if (n < tDirt) b = 1;    // toprak
            else b = 2;                   // kayalik

            // sari cim yamalari: cim alaninda ayri dusuk-frekans noise
            if (b == 0 && _yellowPatches)
            {
                float yn = Fbm(x * 0.07f + yellowOff.x, y * 0.07f + yellowOff.y, 3);
                if (yn > 0.62f) b = 3;
            }
            return b;
        }

        private TileBase BiomeFillTile(int b, System.Random rng)
        {
            switch (b)
            {
                case 0: return PickVariant(TileGrass, rng);
                case 1: return PickVariant(rng.Next(2) == 0 ? TileDirtA : TileDirtB, rng);
                case 2: return PickVariant(TileRocky, rng);
                case 3: return PickVariant(TileYellow, rng);
                default: return PickVariant(TileDirtA, rng);
            }
        }

        private float GrassNeighborFraction(Vector2Int c, Dictionary<Vector2Int, int> biome)
        {
            int grass = 0, total = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;
                    if (biome.TryGetValue(new Vector2Int(c.x + dx, c.y + dy), out int nb))
                    {
                        total++;
                        if (nb == 0 || nb == 3)
                            grass++;
                    }
                }
            }
            return total == 0 ? 0f : (float)grass / total;
        }

        // ─── Yollar ─────────────────────────────────────
        private void PaintRoads(Tilemap path, System.Random rng)
        {
            for (int x = -_arenaHalf; x <= _arenaHalf; x++)
            {
                for (int y = -_arenaHalf; y <= _arenaHalf; y++)
                {
                    if (Mathf.Abs(x) + Mathf.Abs(y) > _arenaRadius - 2)
                        continue;
                    bool outsideCastle = Mathf.Abs(x) > 2 || Mathf.Abs(y) > 2;
                    if (!outsideCastle)
                        continue;
                    // kardinal + capraz spoke (hafif kalinlik)
                    bool spoke = Mathf.Abs(x) <= 1 || Mathf.Abs(y) <= 1 ||
                                 Mathf.Abs(x - y) <= 1 || Mathf.Abs(x + y) <= 1;
                    if (!spoke)
                        continue;
                    TileBase paving = PickVariant(PavingTiles[rng.Next(PavingTiles.Length)], rng);
                    if (paving != null)
                        path.SetTile(new Vector3Int(x, y, 0), paving);
                }
            }
        }

        // ─── Dekor ──────────────────────────────────────
        private void PaintDecor(Tilemap decor, Tilemap overlay, System.Random rng)
        {
            // Trees + Flora kategorileri gorsel olarak temiz (yesil agac/cali). Kayalar ise
            // KATEGORI-RASTGELE DEGIL -> curated RockTiles (monolith/mezar/kuyu disarida).
            List<TileBase> trees = Category("Tree");
            List<TileBase> flora = Category("Flora");
            var rocks = new List<TileBase>();
            foreach (string r in RockTiles)
                foreach (string d in new[] { "_S", "_N", "_E", "_W" })
                    if (_byName.TryGetValue(r + d, out var rt) && rt != null)
                        rocks.Add(rt);

            TileBase shadow = FindFirst("Tree Shadow") ?? FindFirstCategory("Shadow");

            for (int x = -_arenaHalf; x <= _arenaHalf; x++)
            {
                for (int y = -_arenaHalf; y <= _arenaHalf; y++)
                {
                    if (!InOuterArena(x, y))
                        continue;

                    double roll = rng.NextDouble();
                    var cell = new Vector3Int(x, y, 0);

                    if (roll < _treeDensity && trees.Count > 0)
                    {
                        if (_treeShadows && shadow != null)
                            overlay.SetTile(cell, shadow);
                        decor.SetTile(cell, trees[rng.Next(trees.Count)]);
                    }
                    else if (roll < _treeDensity + _rockDensity && rocks.Count > 0)
                    {
                        decor.SetTile(cell, rocks[rng.Next(rocks.Count)]);
                    }
                    else if (roll < _treeDensity + _rockDensity + _floraDensity && flora.Count > 0)
                    {
                        decor.SetTile(cell, flora[rng.Next(flora.Count)]);
                    }
                }
            }
        }

        // ─── Harabe (duvar/moloz set parcalari) ─────────
        private void PaintRuins(Tilemap decor, System.Random rng)
        {
            var ruinPool = new List<TileBase>();
            ruinPool.AddRange(Category("BrokenWall"));
            ruinPool.AddRange(Category("BrokenStone"));
            AddIfExists(ruinPool, "Wall A1");
            AddIfExists(ruinPool, "Door C1");
            if (ruinPool.Count == 0)
                return;

            for (int x = -_arenaHalf; x <= _arenaHalf; x++)
            {
                for (int y = -_arenaHalf; y <= _arenaHalf; y++)
                {
                    if (!InOuterArena(x, y))
                        continue;
                    if (rng.NextDouble() < _ruinDensity)
                        decor.SetTile(new Vector3Int(x, y, 0), ruinPool[rng.Next(ruinPool.Count)]);
                }
            }
        }

        // ─── Kuleler (duvar tabani + cati, +(2,2) offset) ─
        private void PaintTowers(Tilemap decor, Tilemap roof, System.Random rng)
        {
            List<TileBase> walls = Category("Wall");
            List<TileBase> roofs = Category("Roof");
            if (walls.Count == 0)
                return;

            int placed = 0, attempts = 0;
            while (placed < _towerCount && attempts < _towerCount * 30)
            {
                attempts++;
                int x = rng.Next(-_arenaHalf, _arenaHalf + 1);
                int y = rng.Next(-_arenaHalf, _arenaHalf + 1);
                if (!InOuterArena(x, y))
                    continue;
                if (Mathf.Abs(x) + Mathf.Abs(y) > _arenaRadius - 3)
                    continue;

                decor.SetTile(new Vector3Int(x, y, 0), walls[rng.Next(walls.Count)]);
                // cati: izometrikte "yukari" = +(2,2) hucre, daha yuksek sorting katmaninda
                if (roofs.Count > 0)
                    roof.SetTile(new Vector3Int(x + 2, y + 2, 0), roofs[rng.Next(roofs.Count)]);
                placed++;
            }
        }

        // ════════════════════════════════════════════════
        //  Yardimcilar
        // ════════════════════════════════════════════════
        private bool InOuterArena(int x, int y)
        {
            int d = Mathf.Abs(x) + Mathf.Abs(y);
            if (d > _arenaRadius)
                return false;
            if (d <= _keepClearRadius)
                return false; // merkez savas ring + kale temiz
            return true;
        }

        // fbm Perlin (MapImporter ile ayni stil)
        private static float Fbm(float x, float y, int octaves)
        {
            float sum = 0f, amp = 1f, freq = 1f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += amp * Mathf.PerlinNoise(x * freq, y * freq);
                norm += amp;
                amp *= 0.5f;
                freq *= 2f;
            }
            return norm > 0f ? sum / norm : 0f;
        }

        // ─── Katalog ────────────────────────────────────
        private void BuildCatalog()
        {
            _byName = new Dictionary<string, TileBase>();
            _byCategory = new Dictionary<string, List<TileBase>>();

            string[] guids = AssetDatabase.FindAssets("t:TileBase", new[] { TilesRoot });
            foreach (string guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var tile = AssetDatabase.LoadAssetAtPath<TileBase>(p);
                if (tile == null)
                    continue;
                _byName[tile.name] = tile;

                // kategori = isim basindan ilk rakam veya '_' oncesi kelime grubu
                string cat = ExtractCategory(tile.name);
                if (!_byCategory.TryGetValue(cat, out var list))
                {
                    list = new List<TileBase>();
                    _byCategory[cat] = list;
                }
                list.Add(tile);
            }
        }

        // "Tree A2_W" -> "Tree", "WallFlora A1_E" -> "WallFlora", "BrokenStone small1" -> "BrokenStone",
        // "Tree Shadow" -> "Tree Shadow" (family token degil), "BrokenWallStone1" -> "BrokenWallStone"
        private static string ExtractCategory(string name)
        {
            // Once "<Category> <Letter(ler)><Rakam>..." desenini ara: family token'in oncesindeki space'de kes
            int sp = name.IndexOf(' ');
            while (sp >= 0)
            {
                int j = sp + 1;
                int letters = 0;
                while (j < name.Length && char.IsLetter(name[j])) { j++; letters++; }
                if (letters >= 1 && j < name.Length && char.IsDigit(name[j]))
                    return name.Substring(0, sp);
                sp = name.IndexOf(' ', sp + 1);
            }
            // Family token yok (cok kelimeli kategori veya space'siz): ilk rakam/underscore oncesi
            int cut = name.Length;
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsDigit(c) || c == '_')
                {
                    cut = i;
                    break;
                }
            }
            return name.Substring(0, cut).TrimEnd(' ');
        }

        // Kategori adina gore tum tile'lar (ornek: "Tree", "Stone", "Flora", "Misc", "Wall", "Roof")
        private List<TileBase> Category(string cat)
        {
            return _byCategory != null && _byCategory.TryGetValue(cat, out var list)
                ? list
                : new List<TileBase>();
        }

        // Bir mantiksal tile'in (ornek "Ground A2") rastgele yon varyanti
        private TileBase PickVariant(string baseName, System.Random rng)
        {
            var found = new List<TileBase>();
            foreach (string d in new[] { "_S", "_N", "_E", "_W" })
                if (_byName.TryGetValue(baseName + d, out var t) && t != null)
                    found.Add(t);
            if (_byName.TryGetValue(baseName, out var raw) && raw != null)
                found.Add(raw);
            if (found.Count == 0)
                return null;
            return found[rng.Next(found.Count)];
        }

        private TileBase FindFirst(string baseName)
        {
            if (_byName.TryGetValue(baseName, out var t))
                return t;
            foreach (string d in new[] { "_S", "_N", "_E", "_W" })
                if (_byName.TryGetValue(baseName + d, out var t2))
                    return t2;
            return null;
        }

        private TileBase FindFirstCategory(string cat)
        {
            var list = Category(cat);
            return list.Count > 0 ? list[0] : null;
        }

        private void AddIfExists(List<TileBase> pool, string baseName)
        {
            foreach (string d in new[] { "_S", "_N", "_E", "_W" })
                if (_byName.TryGetValue(baseName + d, out var t) && t != null)
                    pool.Add(t);
        }

        // ─── Sahne / katman ─────────────────────────────
        private static Transform FindArenaGrid()
        {
            GameObject root = GameObject.Find(RootName);
            if (root == null)
                return null;
            Transform grid = root.transform.Find(GridName);
            return grid;
        }

        private static Tilemap EnsureLayer(Transform gridTransform, string name, int sortingOrder, TilemapRenderer.Mode mode)
        {
            Transform existing = gridTransform.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
                go.transform.SetParent(gridTransform, false);
            }
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var tilemap = go.GetComponent<Tilemap>();
            if (tilemap == null)
                tilemap = go.AddComponent<Tilemap>();
            var renderer = go.GetComponent<TilemapRenderer>();
            if (renderer == null)
                renderer = go.AddComponent<TilemapRenderer>();
            renderer.mode = mode;
            renderer.sortOrder = TilemapRenderer.SortOrder.TopRight;
            renderer.sortingOrder = sortingOrder;
            return tilemap;
        }

        private void ClearGeneratedLayers()
        {
            Transform gridTransform = FindArenaGrid();
            if (gridTransform == null)
            {
                _status = "HATA: MobileArenaGrid bulunamadi.";
                return;
            }
            foreach (string layer in new[] { GroundLayer, PathLayer, OverlayLayer, DecorLayer, RoofLayer })
            {
                Transform t = gridTransform.Find(layer);
                var tm = t != null ? t.GetComponent<Tilemap>() : null;
                if (tm != null)
                {
                    Undo.RegisterCompleteObjectUndo(tm, "Clear Arena Layers");
                    tm.ClearAllTiles();
                    EditorUtility.SetDirty(tm);
                }
            }
            _status = "Generator katmanlari temizlendi.";
        }

        private static void MarkDirty(params Tilemap[] tilemaps)
        {
            foreach (var t in tilemaps)
                if (t != null)
                    EditorUtility.SetDirty(t);
        }
    }
}
#endif
