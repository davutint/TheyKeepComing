# Worker Economy Drawer UI - Editor Setup

## Required UI Names

`MobileCastleHudRoot` altinda su isimler varsa `Mobile Castle Scene Setup` otomatik baglar:

```text
WorkerDrawerToggleButton
WorkerEconomyDrawerPanel
WorkerDrawerTitleText
WorkerIdlePopulationText
WorkerTotalText
WorkerArcherPopulationText

WoodWorkerCountText
WoodWorkerRateText
WoodWorkerAddButton
WoodWorkerStatusText

StoneWorkerCountText
StoneWorkerRateText
StoneWorkerAddButton
StoneWorkerStatusText

IronWorkerCountText
IronWorkerRateText
IronWorkerAddButton
IronWorkerStatusText

FoodWorkerCountText
FoodWorkerRateText
FoodWorkerAddButton
FoodWorkerStatusText
```

## Setup Tool

`Window > DeadWalls > Mobile Castle Scene Setup`:

- `WorkerEconomyDrawerUI` component'ini `MobileCastleHudRoot` uzerine ekler veya yeniden kullanir.
- Yukaridaki isimleri component alanlarina baglar.
- `WorkerEconomyDrawerPanel` baslangicta kapali tutulur.
- Eski `CastleEconomyPanel` ve `CastleTapHint` player-facing olarak kapali kalir.

## Test

1. UI Importer ile guncel `MobileCastleHudRoot` prefabini import et.
2. `Mobile Castle Scene Setup` calistir.
3. Play modunda sol ust `Workers` toggle'ina bas.
4. `+ WORKER` butonlari idle population varsa worker sayisini artirmali.
5. Resource cap dolunca row `CAP FULL` gostermeli ve buton disabled olmalidir.
6. Sahnede ilgili resource site ile `CastleWorkerHub` arasinda yeni DOTS villager hareketi gorunmelidir.
