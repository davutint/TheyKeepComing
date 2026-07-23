# Castle Economy UI Editor Setup

Bu panel mobile continuous worker drawer akisi icin legacy/debug kabul edilir. Player-facing worker assignment UI'i `WorkerEconomyDrawerUI` tarafindadir.

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

`Tools/Dead Walls/Setup & Repair/Mobile Castle Scene Setup` calistirilinca:

- `CastleEconomyUI` component'i `MobileCastleHudRoot` uzerine eklenir.
- Yukaridaki alanlar isimle aranip baglanir.
- `CastleEconomyPanel`, `CastleTapHint`, `EconomyEventPanel`, `EconomyEventBadge` ve `EconomyEventGlow` baslangicta kapali tutulur.
- `CastleEconomyUI.PlayerFacingPanelEnabled` varsayilan olarak kapali tutulur.
- Main scene'e `CastleClickTarget` root objesi eklenir ve `CastleInteriorClickTarget` baglanir.
- Eski `Economy Focus` butonlari gizlenir.
- Sag drawer'daki eski `RepairButton` player-facing olarak gizlenir.

Setup tool polish fallback full-screen panel uretmez. Yeni worker assignment gorseli prefabdaki sol `WorkerEconomyDrawerPanel` uzerindedir.
