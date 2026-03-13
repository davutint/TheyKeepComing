# BuildingDetailUI — Mimari Dokumani

## Amac
Binaya tiklaninca acilan detay paneli. Isci atama (+/-), bina yikma, uretim bilgisi gosterir.

## Dosya
`Assets/Scripts/MonoBehaviour/BuildingDetailUI.cs`

## UI Yapisi
```
+-----------------------+
|  Oduncu  (Sv.1)       |  <- BuildingData.Type + Level
|                       |
|  Uretim: Ahsap        |  <- ResourceType
|  Hiz: 5.0/dk          |  <- AssignedWorkers * RatePerWorkerPerMin
|                       |
|  Isci: 1 / 5          |  <- AssignedWorkers / MaxWorkers
|  [ - ]  [ + ]         |  <- Butonlar
|                       |
|  Bos Isci: 10         |  <- PopulationState.Idle
|                       |
|  [ Yik ]  [ Kapat ]   |  <- Yikma + kapatma butonlari
+-----------------------+
```

## Tiklama Akisi
```
BuildingPlacementUI.Update()
  ├─ Placement modu degil + sol tikla
  ├─ UI uzerine tiklanmadiysa (EventSystem kontrolu)
  ├─ Mouse → WorldToGrid → GetBuildingEntity
  └─ Entity != Null → BuildingDetailUI.ShowDetail(entity)
```

## Isci Atama Mantigi
- **OnAddWorker:** `ResourceProducer.AssignedWorkers++` → `SetComponentData`
  - Kosul: `Idle > 0 && AssignedWorkers < MaxWorkers`
- **OnRemoveWorker:** `ResourceProducer.AssignedWorkers--` → `SetComponentData`
  - Kosul: `AssignedWorkers > 0`
- `BuildingProductionSystem` sonraki frame'de otomatik yeniden hesaplar

## Yikma Akisi
```
OnDemolish()
  → CloseDetail()
  → BuildingGridManager.RemoveBuilding(gridX, gridY)
    → Grid hucreleri = 0, entityGrid = Null
    → Gorsel tile kaldir
    → %50 kaynak iade
    → EntityManager.DestroyEntity
```

## Bina Tipine Gore Gosterim
| Bina | Isci Bolumu | Kapasite Bolumu | Uretim Bilgisi |
|------|:-----------:|:---------------:|:--------------:|
| Kaynak binalari | Gorunur | Gizli | Gorunur |
| Ev | Gizli | Gorunur | Gizli |
| Diger | Gizli | Gizli | Gizli |

## Performans
- Sadece panel acikken Update calisir (`_hasEntity` null check)
- `EntityManager.GetComponentData/SetComponentData` — tek entity, O(1)
- Sync point YOK, structural change YOK (yikma harici)

## Singleton Pattern
- `BuildingDetailUI.Instance` — Awake'te set edilir
- `GameManager.RestartGame()` icerisinde `CloseDetail()` cagirilir
