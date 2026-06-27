# Worker Logistics Movement System - Editor Setup

## Required Scene Hierarchy

`Mobile Castle Scene Setup` su hiyerarsiyi idempotent kurar:

```text
CastleInteriorEconomyArea
  CastleWorkerHub
    VisualRoot
    DeliveryPoints
      Delivery_00
      Delivery_01
  WoodSite
    VisualRoot
    WorkerSpawnPoints
      Spawn_00
  StoneSite
  IronSite
  FoodSite
```

## Owner Workflow

- `WorkerSpawnPoints` markerlari kaynak pickup noktalaridir.
- `CastleWorkerHub/DeliveryPoints` markerlari teslim noktalaridir.
- `VisualRoot` altina owner dekor koyar; setup tool bu gorselleri boyamaz, silmez veya tasimaz.
- Site objeleri secilince `CastleInteriorWorkerSiteGizmo` pickup -> delivery rotalarini gosterir.

## Test

1. `Window > DeadWalls > Mobile Castle Scene Setup` calistir.
2. `CastleWorkerHub` ve site markerlarini sol ekonomi alanina yerlestir.
3. Play'de worker assignment yap.
4. Villagerlar ilgili site ile hub arasinda gidip gelmelidir.
