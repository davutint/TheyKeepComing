# BarracksTrainingSystem — Mimari Dokumani (M1.6)

## Genel Bakis
Kisla (Barracks) binalarinda okcu egitimi yapan sistem. Idle nufus ve yeterli kaynak varsa egitim baslatir, timer bitince okcu entity'si spawn eder.

## Dosya
`Assets/Scripts/ECS/Systems/BarracksTrainingSystem.cs`

## Namespace
`DeadWalls`

## Sistem Ozellikleri
- `partial struct BarracksTrainingSystem : ISystem`
- **BurstCompile YOK** — ECB + singleton RW erisimi nedeniyle struct'tan kaldirildi
- `[UpdateInGroup(typeof(SimulationSystemGroup))]`
- `[UpdateAfter(typeof(PopulationTickSystem))]`
- `[UpdateBefore(typeof(ResourceTickSystem))]`

## Akis

### 1. Egitim Baslama Kosullari
Bir Kisla entity'si egitim baslatabilmek icin:
- `ArcherTrainer.IsTraining == false` (su an egitim yapmiyor)
- `PopulationState.IdlePopulation >= 1` (bos isci var)
- `ResourceData.Food >= ArcherTrainer.FoodCostPerArcher` (yeterli yemek)
- `ResourceData.Wood >= ArcherTrainer.WoodCostPerArcher` (yeterli ahsap)

### 2. Kaynak Kesimi
Egitim basladiginda kaynaklar **anlik** kesilir:
```
ResourceData.Food -= FoodCostPerArcher
ResourceData.Wood -= WoodCostPerArcher
```
> Kaynak kesimi ResourceTickSystem'den bagimsiz — accumulator uzerinden degil, direkt int eksiltme.

### 3. Timer Isleme
- `ArcherTrainer.TrainingTimer += SystemAPI.Time.DeltaTime`
- Timer >= `ArcherTrainer.TrainingDuration` → egitim tamamlandi

### 4. Okcu Spawn
Timer bittiginde:
- `EntityCommandBuffer` ile okcu entity'si spawn edilir (ArcherPrefabData singleton)
- `PopulationState.Archers += 1` — toplam okcu sayaci guncellenir
- `ArcherTrainer.IsTraining = false` — sonraki frame'de yeni egitim baslatilabilir
- `ArcherTrainer.TrainingTimer = 0` — timer sifirlanir

## Component'lar

### ArcherTrainer (Kisla entity'sinde)
| Alan | Tip | Aciklama |
|------|-----|----------|
| TrainingDuration | float | Egitim suresi (saniye) |
| FoodCostPerArcher | int | Okcu basina yemek maliyeti |
| WoodCostPerArcher | int | Okcu basina ahsap maliyeti |
| TrainingTimer | float | Su anki egitim zamanlayicisi |
| IsTraining | bool | Egitim devam ediyor mu |

### ArcherPrefabData (singleton, GameState entity)
| Alan | Tip | Aciklama |
|------|-----|----------|
| Prefab | Entity | Okcu prefab entity referansi |

## Singleton Erisim
- `SystemAPI.GetSingletonRW<ResourceData>()` — kaynak kesimi
- `SystemAPI.GetSingletonRW<PopulationState>()` — okcu sayaci + idle nufus kontrolu
- `SystemAPI.GetSingleton<ArcherPrefabData>()` — prefab referansi

## Execution Order Baglami
```
PopulationTickSystem        → idle nufus hesaplandi
  ↓
BarracksTrainingSystem      → idle nufus + kaynak kontrolu, egitim isleme
  ↓
ResourceTickSystem          → normal kaynak tick (Barracks'in anlik kesimi bunu etkilemez)
```

## Notlar
- Her Kisla ayni anda sadece 1 okcu egitebilir (kuyruk sistemi yok)
- Birden fazla Kisla binasi varsa her biri bagimsiz calisir
- Idle nufus yetersizse egitim baslatilmaz, kaynaklar harcanmaz
- ECB kullanimi: `EndSimulationEntityCommandBufferSystem` ile okcu spawn
