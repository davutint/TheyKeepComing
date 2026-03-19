# Building Grid (Isometric) — Mimari Dokumani

## Genel Bakis
Bina yerlestirme icin **izometrik** grid altyapisi. Hibrit yaklasim kullanir:
- **MonoBehaviour `int[,]`** — truth source (grid durumu, layout-agnostic)
- **Tilemap (CellLayout=Isometric)** — gorsel (buildable zone + bina sprite)
- **ECS entity** — data (BuildingData + kosullu component'lar)
- **Unity Grid component** — koordinat donusumu (WorldToCell / CellToWorld)

## Dosyalar
- `Assets/Scripts/MonoBehaviour/BuildingGridManager.cs` — Grid truth source + yerlestirme mantigi
- `Assets/Scripts/MonoBehaviour/BuildingPlacementUI.cs` — UI + ghost preview + input + bina secim tiklama
- `Assets/Scripts/MonoBehaviour/BuildingDetailUI.cs` — Bina detay paneli + isci atama + yikma
- `Assets/Scripts/ScriptableObject/BuildingConfigSO.cs` — Bina config SO

## Grid Yapisi

### Hucre Degerleri
| Deger | Anlam |
|-------|-------|
| -1 | Yerlestirilemez (buildable_zone disinda) |
| 0 | Bos, yerlestirilebilir |
| 1 | Dolu (bina var) |

### Izometrik Koordinat Sistemi
- **Grid component**: `CellLayout.Isometric`, `cellSize = (1, 0.5, 1)`
- **Grid origin**: `GridOrigin` (Vector3Int) — tilemap cellBounds.min'den otomatik hesaplanir
- **Grid boyutu**: `GridWidth x GridHeight` — cellBounds'tan otomatik
- **Hucre sekli**: Diamond (baklava) — world space'te 45 derece donuk
- **`_grid[,]`, `_entityGrid[,]`, `_zoneGrid[,]`**: Layout-agnostic integer array'ler, iso/rect farketmez

### Koordinat Donusumu
| Metod | Aciklama |
|-------|----------|
| `WorldToGrid(worldPos)` | `Grid.WorldToCell()` ile world → cell → grid offset |
| `GridToWorld(x, y, config)` | `Grid.GetCellCenterWorld()` ile grid → cell merkez world pos |
| `CellToWorld(x, y)` | Config'siz tek hucre donusumu (gizmo, debug icin) |

**Onemli:** Custom matrix math YOK. Unity'nin `Grid` component'i `CellLayout=Isometric` secildiginde tum trigonometriyi dahili olarak halleder.

### Tilemap Layer'lar
| Layer | CellLayout | Order in Layer | Amac | Runtime Gorunurluk |
|-------|-----------|----------------|------|-------------------|
| `buildable_zone` | Isometric (inherit) | — | Yerlestirilebilir alan tanimlar | Gorunmez |
| `building_visuals` | Isometric (inherit) | 5 | Bina base tile'lari (duvar/zemin) | Gorunur |
| `building_top` | Isometric (inherit) | 6 | Bina top tile'lari (cati/detay) | Gorunur |
| `resource_zones` | Isometric (inherit) | — | Dogal kaynak zone'lari (Orman/Tas/Demir) | Sadece placement modunda |

> Tum tilemap'ler parent Grid objesinden CellLayout'u inherit eder.
> 2-layer sistem: `BuildingConfigSO.TileLayoutBase[]` → base, `TileLayoutTop[]` → top. Layout yoksa eski GhostSprite fallback kullanilir.

## BuildingGridManager (Singleton)

### Yasam Dongusu
```
Awake() → Singleton instance set
Start() → InitializeGrid()
  → _isoGrid = BuildableZoneTilemap.layoutGrid  (Grid component cache)
  → cellBounds'tan GridOrigin, Width, Height hesapla
  → buildable_zone tilemap'ten _grid[,] doldur
  → resource_zones tilemap'ten _zoneGrid[,] doldur
```

### Ana API
| Metod | Aciklama |
|-------|----------|
| `CanPlace(config, x, y)` | NxN alan bos mu + kaynak yeterli mi + zone yakin mi |
| `IsNearZone(zoneType, x, y, w, h, radius)` | Bina footprint'i etrafinda zone tile var mi |
| `PlaceBuilding(config, x, y)` | Kaynak dus + grid isaretle + ECS entity olustur + gorsel koy |
| `RemoveBuilding(x, y)` | Grid serbest + gorsel sil + %50 kaynak iade + entity sil |
| `GetConfigByType(type)` | BuildingType'a gore config bul |
| `WorldToGrid(worldPos)` | World → grid (izometrik Grid.WorldToCell) |
| `GridToWorld(x, y, config)` | Grid → world merkez (izometrik Grid.GetCellCenterWorld) |
| `CellToWorld(x, y)` | Grid → world merkez, config'siz |
| `IsoGrid` | Unity Grid component referansi (readonly) |
| `ShowResourceZones()` / `HideResourceZones()` | Resource zone overlay toggle |
| `ResetGrid()` | Grid sifirla, entity'ler GameManager tarafindan silinir |

### Yerlestirme Akisi
```
1. CanPlace() kontrolu
   ├─ Grid sinir kontrolu
   ├─ NxN hucre bos mu? (_grid[x,y] == 0)
   ├─ Kaynak yeterli mi? (GameManager.Resources)
   └─ Zone yakin mi? (IsNearZone — RequiredZone != None ise)
2. Kaynak dusur (ResourceData ECS singleton)
3. _grid hucreleri = 1
4. ECS entity olustur (CreateEntity + AddComponentData)
5. Tilemap'e gorsel tile koy
6. _entityGrid'e entity referansi yaz (TUM hucrelere)
```

## BuildingPlacementUI

### Yerlestirme Modu Akisi
```
Panel acik → bina butonlari gosterilir (SO listesinden)
  ↓
Butona tikla → StartPlacement(config)
  ↓
Mouse hareket → Ghost snap (izometrik diamond hucreye kilitle)
  ↓
Ghost renk: CanPlace() → yesil, degilse → kirmizi
  ↓
Sol tikla + CanPlace → PlaceBuilding(), mod biter
Sag tikla / Escape → iptal, mod biter
```

### Ghost Preview (Izometrik)
- `SpriteRenderer` ile yari saydam gosterim
- Pozisyon: `GridToWorld()` ile diamond hucre merkezine snap
- Scale: `config.GridWidth * cellSize.x / spriteWorldWidth` (izometrik cell boyutlarina gore)
- Valid: yesil (0, 1, 0, 0.5) / Invalid: kirmizi (1, 0, 0, 0.5)

### Gizmo Cizimi (Diamond)
- Her hucre icin 4 kose: `Grid.CellToWorld()` ile diamond outline
- Dis ceper: footprint siniri sari wireframe
- Dikdortgen wireframe DEGIL, baklava (diamond) sekli

## Yikma (RemoveBuilding) Akisi
```
RemoveBuilding(gridX, gridY)
  → GetBuildingEntity → Entity al
  → BuildingData'dan sol-alt kose + config bul
  → Grid hucreleri = 0, _entityGrid = Null
  → RemoveVisualTile (tilemap'ten sil)
  → RefundResources (%50 kaynak iade)
  → EntityManager.DestroyEntity
```

## Restart Davranisi
```
GameManager.RestartGame()
  → EntityManager.DestroyEntity(BuildingData query)
  → BuildingGridManager.ResetGrid()
  → BuildingDetailUI.CloseDetail()
```

> **Not:** `_zoneGrid` ve `ResourceZoneTilemap` restart'ta temizlenmez — zone'lar kalici harita ozelligi.
