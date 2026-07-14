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
WoodWorkerTargetPlus10Button
WoodWorkerTargetPlus100Button
WoodWorkerTargetInput
WoodWorkerStatusText
WoodCapacityUpgradeButton
WoodEfficiencyUpgradeButton

StoneWorkerCountText
StoneWorkerRateText
StoneWorkerAddButton
StoneWorkerTargetPlus10Button
StoneWorkerTargetPlus100Button
StoneWorkerTargetInput
StoneWorkerStatusText
StoneCapacityUpgradeButton
StoneEfficiencyUpgradeButton

IronWorkerCountText
IronWorkerRateText
IronWorkerAddButton
IronWorkerTargetPlus10Button
IronWorkerTargetPlus100Button
IronWorkerTargetInput
IronWorkerStatusText
IronCapacityUpgradeButton
IronEfficiencyUpgradeButton

FoodWorkerCountText
FoodWorkerRateText
FoodWorkerAddButton
FoodWorkerTargetPlus10Button
FoodWorkerTargetPlus100Button
FoodWorkerTargetInput
FoodWorkerStatusText
FoodCapacityUpgradeButton
FoodEfficiencyUpgradeButton
```

## Setup Tool

`Window > DeadWalls > Mobile Castle Scene Setup`:

- `WorkerEconomyDrawerUI` component'ini `MobileCastleHudRoot` uzerine ekler veya yeniden kullanir.
- Yukaridaki isimleri component alanlarina baglar.
- `WorkerEconomyDrawerPanel` baslangicta kapali tutulur.
- Eski `CastleEconomyPanel` ve `CastleTapHint` player-facing olarak kapali kalir.

`Window > DeadWalls > Repair Worker Drawer Target Controls`, hedef ve bina yatirimi
kontrollerini generated HUD prefabinda idempotent olarak kurar. Satirlari `980 px`
drawer duzenine getirir; eski sol kenari koruyup paneli saga genisletir. Aktif sahne
`NewGameScene` ise sahnedeki otoriter `WorkerEconomyDrawerUI` referanslarini baglayip
sahneyi kaydeder. Prefabda ikinci bir runtime controller birakmaz. Ana scene setup da
prefab instantiate edilmeden once ayni repair adimini otomatik calistirir.

## Test

1. `Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab` guncel olmali (UI dogrudan prefabda duzenlenir).
2. `Mobile Castle Scene Setup` calistir.
3. Play modunda sol ust `Workers` toggle'ina bas.
4. `+1%` ve `+10%` secilen hedefi yuzde puan olarak artirmali; dort hedef toplami `%100` kalmalidir.
5. `+100%` secilen hedefi `%100`'e tasimali, diger hedefleri sifirlamalidir.
6. Direct input `0-100` araliginda exact hedefi uygulamalidir.
7. Hedef kontrolleri mevcut actual worker sayilarini aninda degistirmemelidir.
8. Sonradan gelen yeni population, hedef acigi ve resource cap kuralina gore dagilmalidir.
9. Her satirdaki `CAP` ve `EFF` butonlari level + sonraki Wood/Iron maliyetini gostermelidir.
10. CAP alimi ilgili cap'i `10`, EFF alimi baz kisi uretimini additive `%10` artirmalidir.
11. Iki alimin da Wood ve Iron'i ayni transaction'da harcadigi dogrulanmalidir.
