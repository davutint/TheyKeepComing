# Map Importer — Mimari Dokuman

## Amac
Harita uretim ve tilemap boyama araci. Iki mod destekler:
1. **JSON Import**: `dead_wall_map.json` dosyasini tilemap'lere basar
2. **Prosedural Uretim**: Perlin noise + fBM ile tek tikla harita uretir, seed degistirerek sonsuz iterasyon yapilabilir

## Dosya
- `MapImporterWindow.cs` — tek Editor window dosyasi

## JSON Formati
```json
{
  "version": 1,
  "size": 40,
  "tileSize": { "width": 128, "height": 256, "ppu": 128 },
  "layers": {
    "ground":    [[ "grass", "dirt", "dark_grass", "rocky", ... ], ...],
    "buildable": [[ 0, 1, 1, 0, ... ], ...],
    "resources": [[ "", "forest", "stone", "iron", ... ], ...],
    "strategic": [[ "", "", ... ], ...]
  },
  "castlePosition": { "r": 20, "c": 6 },
  "zombieSpawn": { "colMin": 35, "rowMin": 10, "rowMax": 30 }
}
```

## Katmanlar

| Layer | Tilemap | Tile Slot'lari | Aciklama |
|-------|---------|---------------|----------|
| Ground Base | ground_base (sorting order -1) | A1_E (otomatik yuklenir) | HER hucreye opak toprak blok |
| Ground Overlay | ground (sorting order 0) | IsometricRuleTile (kullanici atar) | Cimen hucrelerine RuleTile, toprak hucrelerine null |
| Buildable | buildable_zone | tek TileBase | Sadece `1` olan hucreler boyanir |
| Resources | resources_zones | forest, stone, iron | Bos olmayan hucreler boyanir |
| Strategic | — | — | Tilemap'e basilMAZ, sadece bilgi gosterilir |

## Koordinat Donusumu
```
tilemapCell.x = col + offset.x
tilemapCell.y = (mapSize - 1 - row) + offset.y
```
- JSON row 0 = haritanin ustu → Unity tilemap y=39'a karsilik gelir (Y-flip)
- `_cellOffset` field ile kullanici kaydirma yapabilir (varsayilan 0,0)

## Persistence (EditorPrefs)
- **Tile asset'ler**: GUID ile kaydedilir → proje reimport'a dayanikli
- **Tilemap referanslar**: GlobalObjectId ile kaydedilir → scene objesi
- **Key prefix**: `"DeadWalls_MapImporter_"`
- Window kapatilip acilinca son atamalar korunur

## Undo Stratejisi
- Her layer boyama: `Undo.RecordObject(tilemap, label)` — tek seferde tum tilemap state'i
- "Tumunu Boya": `Undo.CollapseUndoOperations()` ile tek undo grubuna sarilir
- Clear: ayni pattern + `ClearAllTiles()`

## JSON Parsing
- `Newtonsoft.Json.Linq` (JObject + JArray) — projede `com.unity.nuget.newtonsoft-json` v3.2.2
- `JsonUtility` nested `string[][]` desteklemez, bu yuzden JObject tercih edildi
- Parse sonucu cached array'lere yazilir (`_groundLayer`, `_buildableLayer`, `_resourcesLayer`)

## Buton Validation
- Paint butonlari: JSON parsed + tilemap atanmis + en az 1 tile slot dolu olmalir
- Clear butonlari: sadece tilemap atanmis olmali
- "Tumunu Boya": 3 layer icin tum kosullar saglanmali
- Kosul saglanmazsa buton `GUI.enabled = false` ile devre disi

## Prosedural Uretim Mimarisi

### Noise Fonksiyonu: Domain-Warped fBM
- `Mathf.PerlinNoise(x, y)` built-in kullanilir
- `SampleFBM()`: scale, octaves, lacunarity (2.0), persistence (0.5) parametreleri
- `SampleDomainWarpedFBM()`: koordinatlara noise offset uygular → organik yapilar
  - Warp: `wx = x + noise1(x,y) * warpStrength`, `wy = y + noise2(x,y) * warpStrength`
  - Warp noise daha dusuk scale (0.7x) ve oktav (octaves-1) kullanir
- `GenerateNoiseGrid()`: float[,] grid uretir + `SmoothGrid()` ile post-processing
- `SmoothGrid()`: 3x3 box blur, kenar gecislerini yumusatir
- Seed → koordinat offset'e cevrilir (`seed * 137.5f`, `seed * 259.3f`)
- Her kaynak tipi farkli seed offset (+1111, +2222, +3333, +7777)

### Ground Layer — Esik Tabanlı Terrain
```
Noise:  0.0 ──────────────────────────── 1.0
        ┃ rocky ┃  dirt  ┃ dark_grass ┃ grass ┃
        0     0.20    0.35          0.62     1.0
```
- Esikler slider ile ayarlanabilir, sirali zorunluluk uygulanir (dirt min = rocky + 0.01)

### Buildable Zone — Mesafe + Noise Perturbasyonu
```
effectiveRadius = buildableRadius + (noise - 0.5) * 2 * boundaryNoiseAmp
isBuildable = distFromCastle <= effectiveRadius && col < zombieBorderCol
```
- Kale merkezinden uzaklik tabanlı, kenar noise'u ile organik sinir
- Sag sinir hard cutoff (zombie spawn bolgesi)

### Resources — Ayri Noise + Kurallar
- **Forest**: kenar yanliligi (edgeBias) — harita kenarlarinda daha yogun
- **Stone**: rocky zemin bonusu (+0.15 noise)
- **Iron**: daha siki noise scale (1.5x), 2 oktav → kucuk izole kumeler
- Buildable zone icinde kaynak konmaz
- Ayni hucrede cakisma → winner-take-all (en yuksek score kazanir)

### 2-Katman Ground Sistemi
Fantasy Kingdom tileset'inin Ground A serisi 2 katmanli calisir:
- **A1** = opak 3D toprak blok (base zemin)
- **A2+** = seffaf cimen overlay'leri (ust katman)

Tek tilemap'te A2 koyulursa alti seffaf kalir — Unity arka plani gorunur. Cozum:
```
ground_base tilemap (sorting order -1)
  └── A1_E tile HER hucreye → opak toprak, asla seffaf alan yok

ground tilemap (sorting order 0 — overlay)
  └── Cimen hucrelerine: kullanicinin IsometricRuleTile'i
  └── Toprak hucrelerine: null (base'den A1 gorunur)
  └── RuleTile komsulara bakarak otomatik dogru sprite secer
```

- `PaintGround2Layer()`: base + overlay ayri ayri boyanir
- `_groundBaseTile` (A1_E): asset path'ten otomatik yuklenir (ilk seferde)
- `_grassOverlayTile`: fallback tile (Inspector'da atanir)

#### GroundTileMapper Entegrasyonu
`PaintGround2Layer()` overlay tile seciminde 3 katmanli fallback zinciri kullanir:
```
1. GroundTileMapper esleme var mi?
   EVET → ComputeGrassNeighborMask(r,c) ile 4-bit mask hesapla → mapperTiles[mask]
   2. mapperTiles[mask] null mi? → _grassOverlayTile fallback
   3. _grassOverlayTile da null mi? → null (base A1 gorunur)
```
- `ComputeGrassNeighborMask()`: K(r-1)=bit3, D(c+1)=bit2, G(r+1)=bit1, B(c-1)=bit0
- `IsGrassAt()`: sinir disi = true (kenar temizligi)
- Mapper esleme: `GroundTileMapperWindow` EditorPrefs uzerinden 16 mask→tile GUID saklar
- Ayri editor window: `Window > DeadWalls > Ground Tile Mapper`

### Veri Kontrati
`ProceduralGenerateAll()` ayni `_groundLayer`, `_buildableLayer`, `_resourcesLayer` dizilerini doldurur.
Mevcut `PaintGround()`, `PaintBuildable()`, `PaintResources()` aynen calisir — JSON parse ile ayni format.

### Varsayilan Parametreler
| Parametre | Deger | Aciklama |
|-----------|-------|----------|
| procWidth/Height | 150x170 | Harita boyutu |
| procSeed | 42 | Degistirilebilir seed |
| groundNoiseScale | 0.03 | ~33 hucrelik terrain yamalari |
| groundOctaves | 4 | fBM oktav sayisi |
| warpStrength | 30 | Domain warp yogunlugu |
| smoothingPasses | 1 | Box blur pass sayisi |
| threshRocky/Dirt/DarkGrass | 0.005/0.19/0.61 | Terrain esikleri |
| buildableRadius | 69 | Kale etrafinda buildable alan yaricapi |
| zombieBorderCol | 131 | Sag sinir (zombie spawn bolgesi) |
| boundaryNoiseAmp | 12 | ±12 hucre kenar dalgalanmasi |
| forestDensity | 0.35 | ~35% orman |
| stoneDensity | 0.10 | ~10% tas |
| ironDensity | 0.055 | ~6% demir (en nadir) |
