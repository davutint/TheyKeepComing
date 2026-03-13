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

### ResourcePointType (byte)
Dogal kaynak zone tipi — bina yerlestirmede zone yakinlik kontrolu icin:
- `None` — Zone gereksinimi yok (Farm, House, Barracks vs.)
- `Forest` — Orman zone'u (Lumberjack gerektirir)
- `Stone` — Tas zone'u (Quarry gerektirir)
- `Iron` — Demir zone'u (Mine gerektirir)

> ECS component degil, sadece enum — zone verisi MonoBehaviour `_zoneGrid[,]` cache'inde yasar.

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

### ArcherTrainer (sadece Kisla — M1.6)
| Alan | Tip | Aciklama |
|------|-----|----------|
| TrainingDuration | float | Egitim suresi (saniye) |
| FoodCostPerArcher | int | Okcu basina yemek maliyeti |
| WoodCostPerArcher | int | Okcu basina ahsap maliyeti |
| TrainingTimer | float | Su anki egitim zamanlayicisi |
| IsTraining | bool | Egitim devam ediyor mu |

> BarracksTrainingSystem tarafindan islenir. Idle nufus + yeterli kaynak varsa egitim baslatilir, timer bitince okcu spawn edilir.

### ArrowProducer (sadece Fletcher — M1.6)
| Alan | Tip | Aciklama |
|------|-----|----------|
| ArrowsPerWorkerPerMin | float | Isci basina dakikada uretilen ok |
| WoodCostPerBatchPerMin | float | Isci basina dakikada tuketilen ahsap |

> ArrowProductionSystem tarafindan islenir. Fletcher entity'sinde `ResourceProducer` de bulunur — `AssignedWorkers` / `MaxWorkers` icin.

## Bina Tipi → Component Matrisi

| Bina | BuildingData | ResourceProducer | PopulationProvider | BuildingFoodCost | ArcherTrainer | ArrowProducer | RequiredZone |
|------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Lumberjack | ✅ | ✅ | - | - | - | - | Forest |
| Quarry | ✅ | ✅ | - | - | - | - | Stone |
| Mine | ✅ | ✅ | - | - | - | - | Iron |
| Farm | ✅ | ✅ | - | - | - | - | None |
| House | ✅ | - | ✅ | ✅ | - | - | None |
| Barracks | ✅ | - | - | - | ✅ | - | None |
| Fletcher | ✅ | ✅ | - | - | - | ✅ | None |
| Blacksmith | ✅ | - | - | - | - | - | None |
| WizardTower | ✅ | - | - | - | - | - | None |

## Veri Akisi
```
BuildingGridManager.PlaceBuilding() → EntityManager.CreateEntity + AddComponentData
  → BuildingData (her zaman)
  → ResourceProducer (config.MaxWorkers > 0 ise)
  → PopulationProvider + BuildingFoodCost (config.PopulationCapacity > 0 ise)
  → ArcherTrainer (config.Type == Barracks ise)
  → ArrowProducer (config.Type == Fletcher ise)
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
