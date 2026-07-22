# Castle Interior Worker Placement - Editor Setup

## Setup Tool

`Window > DeadWalls > Mobile Castle Scene Setup` calistirildiginda:

- `Assets/Prefabs/VillagerWorker.prefab` yoksa olusturulur.
- Prefab `Assets/Materials/Villager.mat` kullanir.
- `Villager.mat` ana texture olarak `Assets/SmallScaleInt/Character creator - Fantasy/Created Spritesheets/Character_villager/Idle.png` kullanacak sekilde normalize edilir.
- SubScene icindeki `WaveConfigAuthoring.WorkerPrefab` bu prefab'a baglanir.
- Main scene'de `CastleInteriorEconomyArea` root'u yoksa olusturulur.
- `CastleWorkerHub/DeliveryPoints/Delivery_XX` marker'lari eksikse eklenir.
- Wood/Stone/Iron/Food site root'lari ve bos `WorkerSpawnPoints/Spawn_XX` pickup marker'lari eksikse eklenir.
- `WoodSite`, `StoneSite`, `IronSite` ve `FoodSite` objelerine `CastleInteriorWorkerSiteGizmo` baglanir.
- `CastleInteriorWorkerPlacement` acik worker koridorunu `RouteCorridorX=-0.9`,
  `HubApproachY=0.6`, bes lane ve `0.1` lane araligi ile normalize eder.

Tool gorsel dekor uretmez. `VisualRoot` altindaki resource props, tilemap veya dekor owner tarafindan yerlestirilir.

## Gizmo Kontrolu

Game View'da `Gizmos` acikken `WoodSite`, `StoneSite`, `IronSite` veya `FoodSite`
secilirse ilgili resource site radius'u, pickup marker'lari ve pickup/site approach/hub
approach/delivery rota segmentleri gorunur. Daha kalici preview istenirse ilgili
`CastleInteriorWorkerSiteGizmo.DrawAlways` acilabilir.

## UI Binding

Castle economy UI export'unda su optional buton isimleri varsa otomatik baglanir:

```text
WoodAssignButton
StoneAssignButton
IronAssignButton
FoodAssignButton
```

Butonlar `GameManager.AssignResourceWorker(resource)` API'sini kullanir. Tap progress yoktur; her basarili tap bir worker assignment yapar.

## Play Test

1. `Mobile Castle Scene Setup` calistir.
2. `CastleWorkerHub` delivery marker'larini kale merkezi/depo alanina yerlestir.
3. Resource site pickup marker'larini sahnede istedigin sol ekonomi alanina tasi.
4. Play'e bas.
5. Economy target slider'ını değiştir.
6. Archer olmayan bütün population boş kapasitelere otomatik dağılmalı; villager kaynak yapısından
   çıkıp sağdaki açık koridoru kullanarak hub'a gitmeli ve yüksek yapı/bitki piksellerinin
   üstüne çizilmemelidir.

Unassigned yalnız bütün resource cap'leri doluysa görünmelidir. Yeni kapasite açıldığında kişi
aynı frame otomatik işe dönmelidir. Legacy assignment button normal durumda `AUTO ASSIGNED` veya
disabled state'tedir; Free Economy Test Mode yalnız debug akışıdır.
