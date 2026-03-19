# Building Tile Composer — Mimari Dokuman

## Amac
Her `BuildingConfigSO` icin izometrik tile layout compose eden Editor araci. Binalar 2 katmanli tile'lardan olusur: **Base** (duvar, kapi, zemin) ve **Top** (cati, bayrak, detay). Bu tool ile her hucreye tile atanir ve SO'ya kaydedilir.

## Veri Akisi

```
BuildingConfigSO
├── TileLayoutBase[]    → base tilemap layer (Order=5)
├── TileLayoutTop[]     → top tilemap layer (Order=6)
├── GridWidth / GridHeight
└── Index mapping: x + y * GridWidth (sol-alttan baslayarak)

Composer Window                        Runtime (BuildingGridManager)
┌───────────────┐                     ┌──────────────────────────┐
│ SO yukle      │──→ _baseLayout[]    │ PlaceVisualTile():       │
│ Diamond grid  │    _topLayout[]     │   TileLayoutBase → base  │
│ Tile ata/sil  │                     │   TileLayoutTop  → top   │
│ Save to SO    │──→ SO.TileLayoutX[] │   fallback: GhostSprite  │
└───────────────┘                     └──────────────────────────┘
```

## Dosya Yapisi

| Dosya | Rol |
|-------|-----|
| `Editor/BuildingTileComposerWindow.cs` | EditorWindow — tile composer UI |
| `ScriptableObject/BuildingConfigSO.cs` | `TileLayoutBase[]` + `TileLayoutTop[]` field'lari |
| `MonoBehaviour/BuildingGridManager.cs` | Runtime tile yerlestirme (2-layer) |

## Diamond Grid Cizim Mantigi

Izometrik diamond hesabi:
```
cellW = 64px, cellH = 32px (2:1 oran)

Hucre (x,y) icin merkez:
  cx = origin.x + (x - y) * cellW * 0.5
  cy = origin.y + (x + y) * cellH * 0.5

Diamond 4 kose: ust, sag, alt, sol
  ust:  (cx, cy - cellH/2)
  sag:  (cx + cellW/2, cy)
  alt:  (cx, cy + cellH/2)
  sol:  (cx - cellW/2, cy)
```

Hit testi: Cross product sign kontrolu ile 4 kenar testi.

## 2-Layer Tilemap Sistemi

| Layer | Tilemap | Order in Layer | Icerik |
|-------|---------|----------------|--------|
| Base | `BuildingVisualTilemap` | 5 | Duvar, kapi, zemin |
| Top | `BuildingTopTilemap` | 6 | Cati, bayrak, detay |

- `PlaceVisualTile`: TileLayout varsa → her hucreye base+top tile koyar. Yoksa → eski GhostSprite fallback.
- `RemoveVisualTile`: Ayni logic ile her iki layer'dan tile siler.
- `ResetGrid`: Her iki tilemap'i `ClearAllTiles()` ile temizler.

## Array Boyut Kurali
- `TileLayoutBase.Length == GridWidth * GridHeight` olmali. Aksi halde fallback'e duser.
- `TileLayoutTop` null olabilir veya bos olabilir — sadece base yeterli.
- null slot = o hucrede tile yok.

## Preview RenderTexture Sistemi

Preview bolumu, Unity'nin kendi Tilemap renderer'ini kullanarak gercek izometrik render uretir. Manuel `GUI.DrawTextureWithTexCoords` yerine gecti.

### Akis
```
Tile degisir (_previewDirty = true)
  → RenderPreview() cagrilir
    → Gecici Grid + 2x Tilemap + Camera olustur (HideFlags.HideAndDontSave)
    → Grid: cellSize=(1, 0.5, 1), cellLayout=IsometricZAsY
    → Base tilemap: sortOrder=TopRight, sortingOrder=0, mode=Individual
    → Top tilemap:  sortOrder=TopRight, sortingOrder=1, mode=Individual
    → Tile'lari SetTile() ile yerlestir
    → Camera: orthographic, Render() → RenderTexture (512x512)
    → ReadPixels() ile GPU→CPU kopyala → _previewTexture (Texture2D)
    → Gecici objeleri DestroyImmediate()
  → GUI.DrawTexture() ile preview goster
```

### Dirty Flag Tetikleme
- `PaintCell()` — tile atama/silme
- `ClearAll()` — tum tile'lari temizleme
- `LoadFromConfig()` — yeni SO yukleme

### Kamera Pozisyon Hesabi
- 4 grid kosesinin world-space pozisyonu alinir (`CellToWorld`)
- Sprite tasmasi hesaplanir (pivot Y=0.19, spriteWorldHeight=256/127)
- orthographicSize: tum binayi kapsayacak sekilde margin'li hesaplanir

### Cleanup
- `OnDisable()`: `_previewTexture` varsa `DestroyImmediate` ile yok edilir
- Render sonrasi: gecici GameObject'ler `DestroyImmediate` ile temizlenir
- `HideFlags.HideAndDontSave`: scene'de gorunmez, save edilmez

## Undo Destegi
- `Undo.RecordObject(config, ...)` ile SO degisiklikleri geri alinabilir.
- `EditorUtility.SetDirty()` + `AssetDatabase.SaveAssets()` ile kayit.
