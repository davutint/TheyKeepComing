# Meta Progression (Roguelite) - Mimari

## Amaç

Wall `0 HP` olduğunda koşu tamamen biter; koşu içi state Continue edilemez. O koşunun kill ve day sonucu kalıcı Souls ilerlemesine bir kez aktarılır. Meta state yeni koşularda başlangıç ivmesi ve hafif kalıcı güç sağlar.

Player-facing bütün metinler İngilizcedir. Kod tarafındaki para birimi otoritesi `MetaProgression.CurrencyName` sabitidir.

## Kalıcı state

`MetaProgression.cs` içindeki `MetaProgressState v2`, `persistentDataPath/meta_progress.json` dosyasına yazılır:

- Souls ve TotalSoulsEarned,
- BestDay, TotalRuns ve TotalKillsAllTime,
- `MetaUpgradeLevel` listesi,
- Aynı koşunun iki kez ödüllendirilmesini önleyen sınırlı `RewardedRunIds` listesi.

JsonUtility dictionary serialize etmediği için upgrade state list olarak tutulur. `RewardedRunIds` son 128 run identity'siyle sınırlıdır.

## Koşu sonucu transaction'ı

Otoriter API `AddRunResult(string runId, int day, int kills)` metodudur. Boş RunId kabul edilmez. Daha önce ödüllendirilen RunId yeniden gelirse sonuç `AlreadyRewarded` olarak döner ve Souls/istatistik değişmez.

Game Over akışı journal-first ilerler:

1. `GameManager.ProcessRunDeath()`, run identity ile `RunDeathReceipt` üretir.
2. Receipt atomik ve durable yazılır; bundan sonra matching run save Continue edilemez.
3. Canlı run save fiziksel olarak temizlenir.
4. `AddRunResult()` ödülü idempotent uygular ve `RewardedRunIds` dahil meta state'i atomik yazar.
5. Yalnız meta write başarılı ve RunId ödüllendirilmiş olarak doğrulanmışsa receipt temizlenir.

Uygulama bu adımların arasında kapanırsa `GameManager.Awake` ve ana menü başlangıcı
`RunPersistence.RecoverPendingDeathReward()` çağırır. Receipt'teki aynı RunId meta state'te
varsa ikinci ödül verilmeden yalnız cleanup yapılır; yoksa ödül bir kez uygulanır. Meta write
başarısızsa receipt silinmez ve sonraki açılış yeniden dener. Ayrıntılı çökme matrisi
`DEATH_RECEIPT_ARCHITECTURE.md` içindedir.

## Ödül hesabı

- Her kill: `1 Soul`.
- Yeni day rekoru: `day x 50` ek Soul.
- Sonuç `MetaRunResult` üzerinden Game Over UI tarafından okunur.

## Upgrade kataloğu ve koşu başı uygulama

`MetaUpgradeCatalogSO` kalıcı upgrade tanımlarının sahibidir. Mevcut effect yolları:

- StartingResource -> `AddResources`
- StartingArchers -> `SpawnArcher`; Basic/Rapid/Frost ortak `1000` cap'i bypass edilmez
- StartingTechLevel -> `GrantTechNodeLevelsFromMeta`
- WallHpPercent -> defense aggregate
- ArcherDamagePercent -> archer stat scaling
- ProductionPercent -> economy aggregate

`GameManager.ApplyMetaProgressionAtRunStart()` her yeni koşuda state'i mevcut gameplay kanallarından uygular. `_metaAppliedThisRun` aynı runtime başlangıcında çift uygulamayı önler.

## Kurallar

- Meta yüzdeleri tech/Council ile aynı aggregate kanallarından geçer; runtime component'e ayrı bir sürekli override yazılmaz.
- Run sonucu yalnız kesin Game Over geçişinde toplanır; frame polling ile ödül verilmez.
- `MetaProgression.ResetAll` yalnız debug içindir ve oyuncu yüzeyine bağlanmaz.
- Run save ile meta save ayrı otoritelerdir. Meta state hiçbir zaman canlı koşunun phase/timer/combat snapshot'ını taşımaz.
- Meta write `AtomicJsonFile` üzerinden temp + flush + replace sözleşmesiyle yapılır; durable
  sonuç alınmadan death receipt temizlenmez.
