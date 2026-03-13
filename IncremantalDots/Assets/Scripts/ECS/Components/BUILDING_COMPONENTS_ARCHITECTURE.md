# Building Components — Mimari Dokumani

## Genel Bakis
Bina sistemi icin ECS component'lari. Her bina bir ECS entity olarak olusturulur — gorsel Tilemap uzerinden, data ECS uzerinden yonetilir.

## Dosya
`Assets/Scripts/ECS/Components/BuildingComponents.cs`

## Enum'lar

### BuildingType (byte)
Tum bina tipleri tek enum'da:
- `Lumberjack` — Oduncu (ahsap uretir)
- `Quarry` — Tas Ocagi (tas uretir)
- `Mine` — Maden (demir uretir)
- `Farm` — Ciftlik (yemek uretir)
- `House` — Ev (nufus kapasitesi + yemek tuketir)
- `Barracks` — Kisla (okcu egitir)
- `Fletcher` — Ok Atolyesi (ok uretir)
- `Blacksmith` — Demirci (upgrade saglar)
- `WizardTower` — Buyucu Kulesi (buyu kartlari acar)

> Castle enum'da YOK — kale zaten sahnede, bina olarak yerlestirilmiyor.

### ResourceType (byte)
Kaynak binalarinin urettigi kaynak tipi:
- `Wood`, `Stone`, `Iron`, `Food`

## Component'lar

### BuildingData (her binada VAR)
| Alan | Tip | Aciklama |
|------|-----|----------|
| Type | BuildingType | Bina tipi |
| Level | int | Bina seviyesi (baslangic 1) |
| GridX | int | Grid pozisyonu X (sol-alt kose) |
| GridY | int | Grid pozisyonu Y (sol-alt kose) |

### ResourceProducer (sadece kaynak binalari)
| Alan | Tip | Aciklama |
|------|-----|----------|
| ResourceType | ResourceType | Uretilen kaynak |
| RatePerWorkerPerMin | float | Isci basina dakika/kaynak |
| AssignedWorkers | int | Atanmis isci sayisi (M1.7: UI'dan degistirilir, varsayilan 0) |
| MaxWorkers | int | Maksimum isci kapasitesi |

### PopulationProvider (sadece Ev)
| Alan | Tip | Aciklama |
|------|-----|----------|
| CapacityAmount | int | Sagladigi nufus kapasitesi |

### BuildingFoodCost (sadece Ev)
| Alan | Tip | Aciklama |
|------|-----|----------|
| FoodPerMin | float | Dakika basina yemek tuketimi |

## Bina Tipi → Component Matrisi

| Bina | BuildingData | ResourceProducer | PopulationProvider | BuildingFoodCost |
|------|:---:|:---:|:---:|:---:|
| Lumberjack | ✅ | ✅ | - | - |
| Quarry | ✅ | ✅ | - | - |
| Mine | ✅ | ✅ | - | - |
| Farm | ✅ | ✅ | - | - |
| House | ✅ | - | ✅ | ✅ |
| Barracks | ✅ | - | - | - |
| Fletcher | ✅ | ✅ | - | - |
| Blacksmith | ✅ | - | - | - |
| WizardTower | ✅ | - | - | - |

## Veri Akisi
```
BuildingGridManager.PlaceBuilding() → EntityManager.CreateEntity + AddComponentData
  → BuildingData (her zaman)
  → ResourceProducer (config.MaxWorkers > 0 ise)
  → PopulationProvider + BuildingFoodCost (config.PopulationCapacity > 0 ise)
```

## CastleUpgradeData (Castle entity uzerinde)
| Alan | Tip | Aciklama |
|------|-----|----------|
| Level | int | Su anki yukseltme seviyesi (0 = yukseltilmemis) |
| MaxLevel | int | Maksimum seviye (5) |
| CapacityPerLevel | int | Her seviye basina ek nufus kapasitesi (+10) |
| WoodCostPerLevel | int | Her yukseltme icin ahsap maliyeti (20) |
| StoneCostPerLevel | int | Her yukseltme icin tas maliyeti (30) |

> Castle entity bina entity degil — sahnedeki CastleAuthoring baker'i ekler.

## ECS Entity Notu
Bina entity'leri prefab'dan olusturulmuyor — runtime'da `EntityManager.CreateEntity()` ile olusturuluyor. Gorseller Tilemap uzerinden yonetildigi icin Transform/Renderer component'i yok.
