# Castle Economy UI Editor Setup

## Beklenen UI Isimleri

`MobileCastleHudRoot` altinda imported prefab su isimleri saglarsa setup tool otomatik baglar:

- `CastleEconomyPanel`
- `CastleTapHint`
- `CastleTapHintText`
- `CastleTapHintPulse` (opsiyonel)
- `CloseCastleEconomyButton`
- `ConfirmCastleEconomyButton`
- `PopulationTotalText`
- `PopulationIdleText`
- `PopulationArchersText`
- `PopulationGrowthText`
- `WorkerBudgetText`
- `WoodWorkerSlider`, `StoneWorkerSlider`, `IronWorkerSlider`, `FoodWorkerSlider`
- `WoodWorkerText`, `StoneWorkerText`, `IronWorkerText`, `FoodWorkerText`
- `WoodRateText`, `StoneRateText`, `IronRateText`, `FoodRateText`
- `ProjectedIncomeText`
- `ProjectedWoodText`, `ProjectedStoneText`, `ProjectedIronText`, `ProjectedFoodText`
- `CastleRepairButton`
- `CastleRepairStatusText`
- `CastleRepairCostText` (opsiyonel)
- `EconomyEventPanel`
- `EconomyEventTitleText`
- `EconomyEventDescriptionText`
- `EconomyEventChoiceAButton`
- `EconomyEventChoiceBButton`
- `EconomyEventChoiceAText`
- `EconomyEventChoiceBText`
- `EconomyEventBadge`
- `EconomyEventBadgeText`
- `EconomyEventGlow` (opsiyonel)

## Scene Setup

`Window/DeadWalls/Mobile Castle Scene Setup` calistirilinca:

- `CastleEconomyUI` component'i `MobileCastleHudRoot` uzerine eklenir.
- Yukaridaki alanlar isimle aranip baglanir.
- `CastleEconomyPanel`, `CastleTapHint`, `EconomyEventPanel`, `EconomyEventBadge` ve `EconomyEventGlow` baslangicta kapali tutulur.
- Main scene'e `CastleClickTarget` root objesi eklenir ve `CastleInteriorClickTarget` baglanir.
- Eski `Economy Focus` butonlari gizlenir.
- Sag drawer'daki eski `RepairButton` player-facing olarak gizlenir; repair artik Castle Interior panelindedir.

Setup tool polish fallback full-screen panel uretmez. Castle Interior gorseli owner onayli UI Importer export'u ile gelmelidir.
