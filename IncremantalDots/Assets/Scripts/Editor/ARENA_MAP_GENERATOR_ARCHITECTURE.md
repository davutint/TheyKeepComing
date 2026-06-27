# Arena Map Generator — Architecture

Seed-tabanli tek-tik izometrik arena haritasi ureten Editor araci.
Dosya: `Assets/Scripts/Editor/ArenaMapGeneratorWindow.cs` (menu: `Window > DeadWalls > Arena Map Generator`).

## Amac ve kapsam
- Owner'in elle tile koymak yerine **seed cevirip tek tik** arena uretmesi.
- Cikti TAMAMEN GORSEL: `NewGameScene / WorldVisualRoot / MobileArenaGrid` tilemap katmanlarina boyar.
  ECS gameplay (zombi fizigi, kale HP) ETKILENMEZ. Gameplay-etkili yapilar ayri/sonraki is.
- Onizleme = canli sahne. GENERATE aninda gercek arenaya boyar; Game view'da gorunur. Tek Undo (Ctrl+Z) geri alir.

## Hedef ve onkosul
- Aktif sahne `NewGameScene.unity` olmali; `WorldVisualRoot/MobileArenaGrid` mevcut olmali
  (`MobileCastleSceneSetupWindow` ile kurulur). Grid yoksa tool hata verir, once setup'i ister.
- Grid: Isometric, cellSwizzle XYZ, cellSize (4,2,4); WorldVisualRoot scale 0.35 (bunlari tool DEGISTIRMEZ, var olani kullanir).

## Katman plani (generator-owned, idempotent ensure)
Mevcut `GroundTilemap` (-50) yeniden kullanilir; digerleri yoksa olusturulur. Hepsi `TopRight` sort order.

| Katman | sortingOrder | Mode | Icerik |
|---|--:|---|---|
| `GroundTilemap` | -50 | Individual | biome taban zemin (tam-kaplama fill) |
| `ArenaPathTilemap` | -49 | Chunk | yollar (doseme) |
| `ArenaOverlayTilemap` | -48 | Chunk | cim gecis decal'leri + agac golgesi |
| `ArenaDecorTilemap` | -10 | Individual | agac/tas/flora/misc + harabe + kule tabani |
| `ArenaRoofTilemap` | +2 | Individual | kule catilari (+(2,2) hucre offset) |

Kale katmanlari (`CastleGroundTilemap`/`CastleWallTilemap`/`CastlePropsTilemap`) TUTULMAZ -- merkez kale olduğu gibi kalir.

## Uretim hatti (GENERATE)
1. **Biome zemin:** domain-warped fBM noise (`Mathf.PerlinNoise`, seed-offset) -> agirlikli esik ile cim/toprak/kayalik;
   opsiyonel sari-cim yamasi ikinci dusuk-frekans noise ile. Her hucreye **rastgele yon varyanti** (tiling tekrarini kirar).
2. **Cim gecis overlay'leri:** marching-squares kose tile'i YOK. Asset'in scatter-overlay decal'leri kullanilir:
   bir dirt/rocky hucresinin 3x3 komsusundaki cim orani -> kademeli decal (`Ground A3` yarim / `A6` yama / `A12` seyrek).
   Bu, tileset'in tasarlandigi organik gecis yontemidir.
3. **Yollar (opsiyonel):** kale kenarindan disa kardinal+capraz spoke'lar, doseme tile'lariyla (`Ground D1/E1/H1`).
4. **Dekor:** merkez "temiz ring" disinda yogunluk-bazli sacim: agac/tas/flora/misc. Agac altina opsiyonel golge.
5. **Yapilar:** harabe (BrokenWall/BrokenStone/Wall/Door set parcalari) + kule (duvar tabani + cati, izometrik +(2,2) offset).

Cikti tek Undo grubunda toplanir (`Undo.CollapseUndoOperations`).

## Tile katalogu
- **Biome FILL'leri** sabit, GORSEL DOGRULANMIS isimler (tahmin yok): cim `Ground A2`, toprak `Ground A1/I1`,
  kayalik `Ground B1`, sari cim `Ground J1`. Cim overlay decal'leri `Ground A3/A6/A12/A24`. Doseme `Ground D1/E1/H1`.
- **Dekor/yapi** sabit isimle DEGIL, asset'in kendi kategori-ismiyle dinamik yuklenir:
  `AssetDatabase.FindAssets("t:TileBase", TilesRoot)` -> isim prefix'ine gore gruplanir (`Tree`, `Stone`, `Flora`,
  `Misc`, `Wall`, `Roof`, `BrokenWall`, `BrokenStone`, `Tree Shadow`, `Shadow`). Katalog pencere acikken cache'lenir.
- Tile'lar duz `UnityEngine.Tilemaps.Tile` asset'i; `tilemap.SetTile` ile dogrudan atanir.
- Isim deseni `<Kategori> <Harf><Numara>_<Yon>`; yon SADECE N/E/S/W (4 izo yuz). 1 mantiksal tile ≈ 4 asset.

## Izometrik geometri / footprint
- Arena diamond: `x,y in [-arenaHalf, arenaHalf]`, `|x|+|y| <= arenaRadius` (varsayilan half=13, radius=22 -- mevcut setup ile ayni).
- Merkez temiz ring: `|x|+|y| <= keepClearRadius` (varsayilan 5) -> dekor/yapi konmaz (kale 5x5 + savas alani).
- Yukseklik Z ile DEGIL sahte: dusuk sprite pivot + sortingOrder painter-order + `TopRight`. z=0'da SetTile.

## Bilinen v1 sinirlari
- Cross-tilemap izo sorting tek katmanli: `ArenaDecorTilemap` (-10) kalenin onunde render eder; merkez temiz ring bunu maskeler.
  Kalenin arkasindaki dekor teknik olarak onde gorunebilir (kucuk arena + temiz ring ile nadir). v2: tek birlesik prop katmani + per-tile sort.
- Kule cati offset (+(2,2)) gorsel varsayim; preview'da ayarlanabilir.
- Dekor kategorileri asset'in kendi isimlendirmesinden gelir; preview'da gozle teyit edilir (canli-sahne secildi).

## Iliskili dosyalar
- `Assets/Scripts/Editor/ArenaMapGeneratorWindow.cs`
- `Assets/Scripts/Editor/ARENA_MAP_GENERATOR_EDITOR_SETUP.md`
- Mevcut arena kurulumu: `Assets/Scripts/Editor/MobileCastleSceneSetupWindow.cs` (+ `MOBILE_CASTLE_SCENE_SETUP_ARCHITECTURE.md`)
- Tile kaynagi: `Assets/SmallScaleInt/Fantasy kingdom Tileset/Environment/Tiles`
- Mevcut noise motoru referansi: `Assets/Scripts/Editor/MapImporterWindow.cs`
