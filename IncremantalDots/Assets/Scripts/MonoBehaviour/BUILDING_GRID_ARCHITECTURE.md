# Building Grid — Mimari Dokumani

## Genel Bakis
Bina yerlestirme icin grid altyapisi. Hibrit yaklasim kullanir:
- **MonoBehaviour `int[,]`** — truth source (grid durumu)
- **Tilemap** — gorsel (buildable zone + bina sprite)
- **ECS entity** — data (BuildingData + kosullu component'lar)

## Dosyalar
- `Assets/Scripts/MonoBehaviour/BuildingGridManager.cs` — Grid truth source + yerlestirme mantigi
- `Assets/Scripts/MonoBehaviour/BuildingPlacementUI.cs` — UI + ghost preview + input
- `Assets/Scripts/ScriptableObject/BuildingConfigSO.cs` — Bina config SO

## Grid Yapisi

### Hucre Degerleri
| Deger | Anlam |
|-------|-------|
| -1 | Yerlestirilemez (buildable_zone disinda) |
| 0 | Bos, yerlestirilebilir |
| 1 | Dolu (bina var) |

### Koordinat Sistemi
- Grid origin: `GridOrigin` (Vector3Int) — tilemap world space'den grid space'e offset
- Grid boyutu: `GridWidth x GridHeight` (varsayilan 32x32)
- Her bina 3x3 hucre kaplar (GridWidth/GridHeight SO'dan)
- Sol-alt kose referans noktasi

### Tilemap Layer'lar
| Layer | Amac | Runtime Gorunurluk |
|-------|------|-------------------|
| `buildable_zone` | Yerlestirilebilir alan tanimlar | Gorunmez |
| `building_visuals` | Bina sprite'lari gosterir | Gorunur |

## BuildingGridManager (Singleton)

### Yasam Dongusu
```
Awake() → Singleton instance set
Start() → InitializeGrid()
  → buildable_zone tilemap'ten _grid[,] doldr
  → tile olan yerler = 0, olmayan = -1
```

### Ana API
| Metod | Aciklama |
|-------|----------|
| `CanPlace(config, x, y)` | 3x3 alan bos mu + kaynak yeterli mi |
| `PlaceBuilding(config, x, y)` | Kaynak dus + grid isaretle + ECS entity olustur + gorsel koy |
| `RemoveBuilding(x, y)` | Stub — M1.7'de implement edilecek |
| `WorldToGrid(worldPos)` | World → grid koordinat cevrimi |
| `GridToWorld(x, y, config)` | Grid → world (bina merkezi) |
| `ResetGrid()` | Grid sifirla, entity'ler GameManager tarafindan silinir |

### Yerlestirme Akisi
```
1. CanPlace() kontrolu
   ├─ Grid sinir kontrolu
   ├─ 3x3 hucre bos mu? (_grid[x,y] == 0)
   └─ Kaynak yeterli mi? (GameManager.Resources)
2. Kaynak dusur (ResourceData ECS singleton)
3. _grid hucreleri = 1
4. ECS entity olustur (CreateEntity + AddComponentData)
5. Tilemap'e gorsel tile koy
6. _entityGrid'e entity referansi yaz
```

## BuildingPlacementUI

### Yerlestirme Modu Akisi
```
Panel acik → bina butonlari gosterilir (SO listesinden)
  ↓
Butona tikla → StartPlacement(config)
  ↓
Mouse hareket → Ghost snap (grid'e kilitle)
  ↓
Ghost renk: CanPlace() → yesil, degilse → kirmizi
  ↓
Sol tikla + CanPlace → PlaceBuilding(), mod biter
Sag tikla / Escape → iptal, mod biter
```

### Ghost Preview
- `SpriteRenderer` ile yari saydam gosterim
- Valid: yesil (0, 1, 0, 0.5)
- Invalid: kirmizi (1, 0, 0, 0.5)
- Grid'e snap: `GridToWorld()` ile pozisyon hesaplanir

## BuildingConfigSO

Her bina tipi icin 1 ScriptableObject asset:
- Tip, isim, ikon, ghost sprite
- Grid boyutu (3x3)
- Maliyet (Wood, Stone, Iron, Food)
- Uretim (ResourceType, RatePerWorkerPerMin, MaxWorkers)
- Nufus (PopulationCapacity, FoodCostPerMin)

## Restart Davranisi
```
GameManager.RestartGame()
  → EntityManager.DestroyEntity(BuildingData query)  // Tum bina entity'leri sil
  → BuildingGridManager.ResetGrid()                   // Grid sifirla + tilemap temizle
```
