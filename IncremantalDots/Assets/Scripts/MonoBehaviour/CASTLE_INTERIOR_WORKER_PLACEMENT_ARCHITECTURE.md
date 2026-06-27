# Castle Interior Worker Placement - Architecture

`CastleInteriorWorkerPlacement`, mobile castle loop'ta sol taraftaki kale ici ekonomi alaninda DOTS villager worker entity'lerinin hangi kaynak pickup noktasi ile hangi merkezi hub delivery noktasi arasinda yuruyecigini belirleyen main-scene controller'dir.

## Sorumluluk

- `CastleInteriorEconomyArea` root'unu, resource site pickup marker'larini ve `CastleWorkerHub` delivery marker'larini okur.
- Wood/Stone/Iron/Food icin siradaki worker pickup/delivery world pozisyonlarini verir.
- Spawn point sayisi gameplay cap degildir; markerlar biterse ayni noktalara kucuk deterministic offset uygular.
- Worker visual spawn etmez; sadece route pozisyonlarini saglar. Spawn islemini `GameManager` ECS prefab instantiate ile yapar.

## Hedef Hierarchy

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
      Spawn_01
  StoneSite
    VisualRoot
    WorkerSpawnPoints
      Spawn_00
  IronSite
    VisualRoot
    WorkerSpawnPoints
      Spawn_00
  FoodSite
    VisualRoot
    WorkerSpawnPoints
      Spawn_00
```

`VisualRoot` owner tarafindan kurulan dekor/gorsel alanidir. Runtime kod bu objeleri boyamaz veya tasimaz.

`WorkerSpawnPoints` artik villagerlarin durdugu yer degil, kaynak pickup marker'laridir. `CastleWorkerHub/DeliveryPoints` ise kaynak teslim merkezidir.

## DOTS Worker Akisi

```text
CastleEconomyUI Assign Button
-> GameManager.AssignResourceWorker(resource)
-> MobilePopulationAllocation worker count artar
-> GameManager worker visual sync
-> WorkerPrefabData.WorkerPrefab instantiate
-> ResourceWorkerVisual + WorkerLogisticsRoute + LocalTransform set edilir
-> WorkerLogisticsMovementSystem villager'i pickup/hub arasinda yurutur
```

Worker entity'leri `VillagerWorker.prefab` uzerinden bake edilir. Prefab `SpriteSheetAuthoring` ve `VillagerWorkerAuthoring` tasir.

## Render

Workerlar unit bandinda spawn olur:

```text
MobileCastleRenderDepth.UnitZ = -1
```

`Villager.mat`, `DeadWalls/SpriteSheet` shader'i ve `Character_villager/Idle.png` spritesheet'i kullanir.

## Gizmo Preview

`CastleInteriorWorkerSiteGizmo`, `WoodSite`, `StoneSite`, `IronSite` ve `FoodSite` objelerine takilir. Site objesi seciliyken Game View veya Scene View gizmos aciksa:

- Resource site radius'u gorunur.
- `WorkerSpawnPoints/Spawn_XX` marker noktalari gorunur.
- Hub delivery marker'larina rota cizgileri gorunur.
- Markerlar isim sirasina gore cizilir; runtime worker logistics ile ayni sirayi temsil eder.
