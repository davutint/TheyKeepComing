# Arrow Pool + Burst-Safe Lifetime - Editor Setup

## WaveConfigAuthoring

`NewGameScene` SubScene içindeki `GameState / WaveConfigAuthoring` üzerinde:

- `Arrow Prefab`: `Assets/Prefabs/Arrow.prefab`
- `Arrow Pool Prewarm`: `1024`
- `Arrow Pool Expand Batch`: `256`

Prewarm aktif 1.000 okçunun ilk salvo kapasitesini karşılar. Expand batch gameplay cap
değildir; yalnız rezerv tükendiğinde sonraki Initialization turunda ek entity hazırlar.

## Arrow.prefab

Prefab üzerinde şu componentler korunmalıdır:

- `ArrowAuthoring`
  - `Speed`: `12`
  - `Lifetime`: `5`
  - `Slow Multiplier`: `1`
- `SpriteSheetAuthoring`

`ArrowAuthoring` bake sırasında `ArrowTag`, `ArrowProjectile` ve `ArrowPoolMember`
ekler. Pool entity'lerine elle `ArrowTag` enable/disable uygulama; debug/test cleanup
için `ArrowPoolRuntimeUtility.ReturnAllActive` kullan.

## Runtime Kontrolü

Play Mode'da pool owner entity üzerinde:

- `ArrowPoolRuntimeData.Initialized = 1`
- başlangıç `TotalCreated = 1024`
- normal owner yollarında `AvailableCount + ActiveCount = TotalCreated`
- atışta `TotalRentCount`, dönüşte `TotalReturnCount` artar
- rezerv tükendiğinde `ExpansionCount` artar ve `TotalCreated` değeri `256` yükselir

Inactive oklar `ArrowTag` disabled, `LocalTransform.Scale = 0` ve target `Entity.Null`
durumunda olmalıdır.

## Hata Ayıklama

- Oklar hiç çıkmıyorsa `ArrowPoolRuntimeData`, `ArrowPoolAvailable` ve
  `ArrowPrefabData` singleton'larının bake edildiğini kontrol et.
- Pool boş kalıyorsa `ExpandRequested` sonraki frame'de maintenance tarafından
  temizlenmeli ve batch genişleme yapılmalıdır.
- Ok yeni rent edilen zombiye yanlış gidiyorsa target generation ile
  `TargetPoolGeneration` eşleşmesini kontrol et; sistem retarget yapmamalıdır.
- Continue sonrasında eski oklar çoğalıyorsa cleanup'ın destroy yerine
  `ReturnAllActive` kullandığını kontrol et.
