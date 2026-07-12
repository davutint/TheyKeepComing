# Expandable Enemy Pool - Editor Kurulum

Ek GameObject veya MonoBehaviour kurulumu gerekmez.

## Asset kontrolü

`Assets/ScriptableObject/MobileCastle/Enemies/BasicZombie.asset`:

- `Pool Prewarm`: `128`
- `Pool Expand Batch`: `128`

`EnemyCatalog.asset` yalnız `zombie_basic` tanımını aktif tutmalıdır. `WaveConfigAuthoring.EnemyCatalog` bu catalog'a bağlı olmalıdır. `Window > DeadWalls > Mobile Castle Scene Setup` aracı eksik bağlantıları kurar.

## Runtime kontrolü

Play Mode'da catalog entity üzerinde:

- `EnemyPoolRuntimeData.Initialized = 1`
- Başlangıçta `TotalCreated = 128`
- `AvailableCount + ActiveCount = TotalCreated` normal owner yollarında korunur
- Rezerv biterse `TotalCreated` değeri `128` katlarıyla artar ve `ExpansionCount` yükselir
- Ölüm sonrası `TotalReturnCount` artar; entity yok edilmez

Inactive pool üyeleri `ZombieTag` disabled ve scale `0` durumundadır. Bunları manuel olarak enable etme veya buffer dışından destroy etme; test/debug cleanup için `EnemyPoolRuntimeUtility.ReturnAllActive` kullan.
