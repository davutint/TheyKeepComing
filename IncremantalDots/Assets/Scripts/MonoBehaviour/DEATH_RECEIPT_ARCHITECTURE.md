# Death Receipt ve Kalıcı Ödül Transaction'ı - Mimari

## Oyuncu sözleşmesi

Wall `0 HP` olduğunda koşu kesin olarak biter. Ölümden önceki `run_save.json` artık
Continue edilemez ve aynı koşunun meta ödülü process kapanması, Editor kapanması veya
tekrar açılış sırasında yalnız bir kez yazılabilir.

Bu sözleşme üç ayrı disk sahibine bölünür:

| Dosya | Sahip | İçerik |
|---|---|---|
| `run_save.json` | `RunPersistence` | Yalnız yaşayan koşunun exact-critical + deterministic-combat v14 snapshot'ı |
| `run_death_receipt.json` | `RunPersistence` | Kapanmakta olan koşunun `{ RunId, Day, Kills }` transaction journal'ı |
| `meta_progress.json` | `MetaProgression` | v3 Souls/istatistik, upgrade seviyeleri, pool unlocks, tutorial flags ve son 128 `RewardedRunIds` |

Run save ile meta save birbirinin alanlarını taşımaz. Death receipt gameplay state değildir;
iki ayrı save otoritesi arasındaki işlemi tamamlayan küçük ve geçici bir journal'dır.

## Journal-first işlem sırası

Otoriter ölüm yolu `GameManager.ProcessRunDeath()` metodudur:

1. `GameManager`, güncel `RunId`, day ve kill değerleriyle receipt üretir.
2. `RunPersistence.CommitDeath()` receipt'i durable ve atomik biçimde diske yazar.
3. Receipt authoritative olduktan sonra run snapshot fiziksel olarak silinir. Silme başarısız
   olsa bile matching receipt bulunan snapshot `TryLoad()` tarafından reddedilir.
4. `MetaProgression.AddRunResult()` aynı `RunId` için ödülü idempotent uygular.
5. `meta_progress.json`, `RewardedRunIds` dahil durable ve atomik biçimde yazılır.
6. Yalnız meta write başarılıysa ve `HasRewardedRun(runId)` doğrulanıyorsa run snapshot ile
   death receipt temizlenir.

Meta write başarısız olursa receipt korunur. `GameManager.Awake` ve `MainMenuSceneUI`
başlangıcı `RecoverPendingDeathReward()` çağırarak aynı transaction'ı tamamlar.

## Atomik dosya sözleşmesi

`AtomicJsonFile` JSON'u aynı klasördeki `.tmp` dosyasına `WriteThrough` ile yazar ve
`Flush(true)` sonrasında authoritative dosyaya `File.Replace` veya `File.Move` uygular.
İlk yazımın rename adımından önce process kapanırsa, sonraki load authoritative dosya yokken
tam `.tmp` dosyasını sahiplenir. Authoritative dosya zaten varsa stale `.tmp` silinir.

Bir death receipt dosyası veya `.tmp` marker'ı bulunduğu halde payload parse edilemiyorsa
Continue fail-closed davranır. Bozuk/yarım ölüm kanıtı, oyuncuya ölüm öncesi snapshot'ı geri
vermek için kullanılamaz.

## Çökme noktaları

| Kapanma anı | Sonraki açılış davranışı |
|---|---|
| Receipt durable olmadan önce | Yaşayan run snapshot hâlâ geçerlidir |
| Receipt yazıldı, run snapshot silinemedi | Matching snapshot Continue listesine alınmaz |
| Reward bellekte uygulandı, meta write başarısız | Receipt kalır; açılışta aynı state yeniden durable yazılır |
| Meta write başarılı, receipt silinemedi | `RewardedRunIds` ikinci ödülü engeller; retry yalnız cleanup yapar |
| Receipt `.tmp` olarak kaldı | Temp recover edilir ve transaction tamamlanır |

## Taze ECS ölüm kontrolü

`GameManager.SaveRunSnapshot()` cached MonoBehaviour state'ine güvenmez. Önce
`ReadECSData()` ile ECS truth'ini yeniler; Game Over veya Wall destruction bu çağrıda tespit
edildiyse ölüm transaction'ı snapshot capture'dan önce kazanır. Böylece application quit aynı
frame içinde ölmüş koşuyu yeni bir yaşayan snapshot olarak tekrar yazamaz.

## İdempotency sınırı

`RunId` transaction kimliğidir. Aynı `RunId` ikinci kez `AddRunResult()` metoduna gelirse
Souls, TotalRuns, kills veya record tekrar artırılmaz. Duplicate state yine de diske yazılmayı
dener; bunun nedeni ilk ödül uygulamasından sonraki meta write'ın başarısız kalmış olabilmesidir.

`RewardedRunIds` son 128 kimlikle sınırlıdır. Bu liste transaction tekrarını engeller; run
snapshot restore veya generated Heart graph seçimi için kullanılmaz.

## Test sahipleri

- `RunPersistenceTests`: atomik receipt round-trip, matching snapshot invalidation, corrupt
  marker fail-closed, orphan temp recovery ve process-reload idempotency.
- `ExactRunContinuePlayModeTests.SaveRunSnapshot_LethalEcsState_CannotRewriteContinueAfterDeath`:
  stale Mono cache'e rağmen lethal ECS state'in yaşayan snapshot yazmasını engeller.
- `ExactRunContinuePlayModeTests.Continue_RestoresSameCyclePhaseTimerResourcesAndSpawnRng`:
  yaşayan exact Continue sözleşmesinin ölüm guard'ından etkilenmediğini doğrular.
- `ExactRunContinuePlayModeTests.RuntimeDefense_IgnoresInjectedGateCore_AndEndsOnlyWhenWallDies`:
  ölüm otoritesinin yalnız tek Wall olduğunu doğrular.
