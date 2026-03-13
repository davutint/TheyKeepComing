# ArrowProductionSystem — Mimari Dokumani (M1.6)

## Genel Bakis
Fletcher (Ok Atolyesi) binalarindaki iscilerin ok uretimini yoneten sistem. ArrowSupply singleton uzerinde accumulator pattern ile calisir, ahsap tuketimini ResourceConsumptionRate'e ekler.

## Dosya
`Assets/Scripts/ECS/Systems/ArrowProductionSystem.cs`

## Namespace
`DeadWalls`

## Sistem Ozellikleri
- `partial struct ArrowProductionSystem : ISystem`
- **`[BurstCompile]`** destekli — singleton RW erisimi var ama ECB yok, static field yok
- `[UpdateInGroup(typeof(SimulationSystemGroup))]`
- `[UpdateAfter(typeof(BuildingPopulationSystem))]`
- `[UpdateBefore(typeof(PopulationTickSystem))]`

## Akis

### 1. Fletcher Entity Tarama
Tum `ResourceProducer` + `BuildingData` component'li entity'ler taranir:
- `BuildingData.Type == BuildingType.Fletcher` filtresi
- `ResourceProducer.AssignedWorkers > 0` kontrolu

### 2. Ok Uretim Hesabi
Her Fletcher entity icin:
```
arrowsThisFrame = AssignedWorkers * ArrowsPerWorkerPerMin * (dt / 60f)
```
- Uretilen ok miktari `ArrowSupply.Accumulator`'a eklenir
- Accumulator >= 1.0 olunca int'e transfer: `ArrowSupply.Current += (int)Accumulator`

### 3. Ahsap Tuketimi
Fletcher'larin ahsap tuketimi `ResourceConsumptionRate.WoodPerMin`'e eklenir:
```
WoodPerMin += AssignedWorkers * WoodCostPerBatchPerMin
```
> Bu ekleme BuildingPopulationSystem WoodPerMin'i sifirladiktan SONRA yapilir (UpdateAfter BuildingPopulationSystem).
> ResourceTickSystem bu toplam WoodPerMin'i kullanarak ahsap stogunu azaltir.

### 4. Accumulator Pattern
ArrowSupply singleton uzerinde ResourceData ile ayni pattern:
```
ArrowSupply.Accumulator += arrowsThisFrame
if (Accumulator >= 1.0f)
    int transfer = (int)Accumulator
    Current += transfer
    Accumulator -= transfer
```

## Component'lar

### ArrowSupply (singleton, GameState entity)
| Alan | Tip | Aciklama |
|------|-----|----------|
| Current | int | Mevcut ok stoku |
| Accumulator | float | Kesirli birikim tamponu |

### ArrowProducer (Fletcher entity'sinde)
| Alan | Tip | Aciklama |
|------|-----|----------|
| ArrowsPerWorkerPerMin | float | Isci basina dakikada uretilen ok |
| WoodCostPerBatchPerMin | float | Isci basina dakikada tuketilen ahsap |

> Fletcher entity'sinde ayrica `ResourceProducer` component'i bulunur — `AssignedWorkers` ve `MaxWorkers` icin kullanilir.

## Singleton Erisim
- `SystemAPI.GetSingletonRW<ArrowSupply>()` — ok stoku + accumulator
- `SystemAPI.GetSingletonRW<ResourceConsumptionRate>()` — WoodPerMin ekleme

## Execution Order Baglami
```
BuildingProductionSystem    → kaynak uretim hizlarini topla
  ↓
BuildingPopulationSystem    → WoodPerMin dahil consumption rate'leri sifirla + bina yemek gideri yaz
  ↓
ArrowProductionSystem       → Fletcher ok uretimi + WoodPerMin'e ekleme
  ↓
PopulationTickSystem        → nufus hesapla
  ↓
BarracksTrainingSystem      → okcu egitimi
  ↓
ResourceTickSystem          → tum rate'leri isleme (WoodPerMin Fletcher tuketimini de icerir)
```

## Notlar
- ArrowSupply.Current ArcherShootSystem tarafindan tuketilir (ok atilinca -1)
- Current <= 0 ise ArcherShootSystem ok atamaz
- Birden fazla Fletcher varsa uretim toplanir
- BurstCompile desteklenir — tum erisimler SystemAPI uzerinden, static field yok
- Blacksmith binasi bu sistemi KULLANMAZ — Blacksmith sadece BuildingData component'i tasir, uretim/ok islemi yapmaz
