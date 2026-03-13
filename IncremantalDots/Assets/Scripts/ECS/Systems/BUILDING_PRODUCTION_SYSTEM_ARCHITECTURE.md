# BuildingProductionSystem — Mimari

## Amac
Yerlestirilen binalarin kaynak uretimini ECS singleton'larina yansitir. Her frame tum `ResourceProducer` entity'lerini tarar, toplam uretim hizini `ResourceProductionRate` singleton'ina ve toplam isci sayisini `PopulationState.Workers`'a yazar.

## Sistem Sirasi
```
BuildingProductionSystem → PopulationTickSystem → ResourceTickSystem → ...
```
- `[UpdateBefore(typeof(PopulationTickSystem))]` — Workers guncel olur, PopulationTickSystem Idle'i dogru hesaplar
- PopulationTickSystem FoodPerMin tuketimini Workers'a gore hesaplar
- ResourceTickSystem net hizi (uretim - tuketim) uygular

## Calisma Mantigi (OnUpdate)
1. GameOver kontrolu — oyun bittiyse calis**MA**
2. `SystemAPI.Query<RefRO<ResourceProducer>>()` ile tum bina entity'lerini tara
3. Her bina icin: `rate = RatePerWorkerPerMin * AssignedWorkers`
4. ResourceType'a gore toplam Wood/Stone/Iron/Food rate biriktir
5. Toplam isci sayisini biriktir
6. `ResourceProductionRate` singleton'ina toplam rate'leri yaz
7. `PopulationState.Workers`'a toplam isci sayisini yaz

## Performans
- **Main thread** — bina sayisi 10-50 max, job overhead'i gereksiz
- `[BurstCompile]` struct + OnUpdate — tam Burst uyumlu
- Sync point YOK, structural change YOK, ECB YOK
- Allocation YOK — sadece local degiskenler + singleton yazma

## Bagimliliklar
- **Okur:** `ResourceProducer` (bina entity'leri), `GameStateData` (GameOver kontrolu)
- **Yazar:** `ResourceProductionRate` (uretim hizlari), `PopulationState` (Workers)
- **Okunur by:** `ResourceTickSystem` (uretim hizini kullanir), `PopulationTickSystem` (Workers'i kullanir)

## Bina Yoksa
- Query bos donerse tum rate'ler 0f olur, Workers 0 olur
- Restart sonrasi bina entity'leri silinir → uretim otomatik durur

## Isci Atama (M1.7)
- `AssignedWorkers` varsayilan 0 — oyuncu BuildingDetailUI uzerinden +/- ile atar
- `BuildingDetailUI.OnAddWorker/OnRemoveWorker` → `SetComponentData<ResourceProducer>`
- BuildingProductionSystem sonraki frame'de toplam Workers'i otomatik yeniden hesaplar
