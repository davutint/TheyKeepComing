# Wall Slot Manager — Mimari

## Genel Bakis
Sur uzerindeki onceden tanimlanmis slotlara savunma birimleri (mancinik) yerlestirme sistemi. Grid tabanli bina yerlestirmeden BAGIMSIZ — slot pozisyonlari Inspector'dan sabit tanimlanir.

## WallSlotManager
- **Tip:** MonoBehaviour singleton (Instance pattern)
- **Konum:** `Assets/Scripts/MonoBehaviour/WallSlotManager.cs`

### WallSlot Struct
| Alan | Tip | Aciklama |
|------|-----|----------|
| Position | Vector3 | Slot'un dunya pozisyonu |
| IsOccupied | bool | Slot dolu mu (HideInInspector) |
| OccupantEntity | Entity | Slottaki ECS entity (HideInInspector) |

### Default Slot Pozisyonlari
```
Slot 0: (2.5, -4, -1)  — sur alt
Slot 1: (2.5,  0, -1)  — sur orta
Slot 2: (2.5,  4, -1)  — sur ust
```

### Maliyet
| Kaynak | Miktar |
|--------|--------|
| Wood | 30 |
| Stone | 50 |
| Iron | 20 |

### Temel Metodlar
- `PlaceCatapult(slotIndex)` — Kaynak + Demirci kontrol → prefab instantiate → slot isaretle
- `RemoveCatapult(slotIndex)` — Entity destroy → %50 kaynak iade → slot bosalt
- `ResetSlots()` — Tum slotlari bosalt (RestartGame icin)
- `GetNearestEmptySlot(worldPos)` — En yakin bos slot indeksi (-1 yoksa)
- `HasEmptySlot()` — Bos slot var mi

### Unlock Sistemi
- `RequireBlacksmith` toggle (default true)
- true iken `BuildingGridManager.HasBuildingOfType(Blacksmith)` kontrol edilir
- Test icin false yapilabilir

## Akis
```
BuildingPlacementUI  ←  IsWallSlotBuilding = true
    ↓
WallSlotManager.GetNearestEmptySlot()  →  ghost snap
    ↓
WallSlotManager.PlaceCatapult()
    ↓
CatapultPrefabData singleton → Instantiate → LocalTransform set
    ↓
CatapultShootSystem isler
```

## BuildingPlacementUI Entegrasyonu
- `BuildingConfigSO.IsWallSlotBuilding = true` ise Update()'de wall slot branch calisir
- Ghost preview en yakin bos slot'a snap eder (grid yerine)
- Sol tikla → WallSlotManager.PlaceCatapult()

## GameManager Entegrasyonu
- RestartGame() icinde:
  - CatapultUnit + CatapultProjectileTag entity'leri silinir
  - WallSlotManager.ResetSlots() cagrilir
