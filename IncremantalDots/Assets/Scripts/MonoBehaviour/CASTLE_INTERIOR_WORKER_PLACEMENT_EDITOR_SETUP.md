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

Tool gorsel dekor uretmez. `VisualRoot` altindaki resource props, tilemap veya dekor owner tarafindan yerlestirilir.

## Gizmo Kontrolu

Game View'da `Gizmos` acikken `WoodSite`, `StoneSite`, `IronSite` veya `FoodSite` secilirse ilgili resource site radius'u, pickup marker'lari ve hub'a giden rota cizgileri gorunur. Daha kalici preview istenirse ilgili `CastleInteriorWorkerSiteGizmo.DrawAlways` acilabilir.

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
5. UI assign butonlari varsa Wood/Stone/Iron/Food'a tikla.
6. Idle population varsa ilgili resource worker sayisi artmali ve villager ilgili site ile hub arasinda yuruyerek kaynak tasiyor gibi gorunmelidir.

Idle population yoksa button `NEED POP` veya disabled state'e dusmelidir. Free economy test mode aciksa assignment debug amacli devam edebilir.
