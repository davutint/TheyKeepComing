# Meta Progression (Roguelite) - Mimari

## Amaç

Wall `0 HP` olduğunda koşu tamamen biter; koşu içi state Continue edilemez. O koşunun kill ve day sonucu kalıcı Souls ilerlemesine bir kez aktarılır. Meta state yeni koşularda başlangıç ivmesi ve hafif kalıcı güç sağlar.

Player-facing bütün metinler İngilizcedir. Kod tarafındaki para birimi otoritesi `MetaProgression.CurrencyName` sabitidir.

## Kalıcı state

`MetaProgression.cs` içindeki `MetaProgressState v3`, `persistentDataPath/meta_progress.json` dosyasına yazılır:

- Souls ve TotalSoulsEarned,
- BestDay, TotalRuns ve TotalKillsAllTime,
- `MetaUpgradeLevel` listesi,
- Gelecekteki Heart/ability content olasılık havuzlarını açacak stable `UnlockedPoolIds`,
- Package I onboarding sahibinin kullandığı stable `TutorialFlags`,
- Aynı koşunun iki kez ödüllendirilmesini önleyen sınırlı `RewardedRunIds` listesi.

JsonUtility dictionary serialize etmediği için upgrade ve kimlik state'leri list olarak tutulur.
Load/Save normalizasyonu boş kimlikleri temizler, duplicate kimlikleri tekilleştirir, duplicate
upgrade seviyelerinde en yüksek değeri korur ve negatif sayısal state'i sıfıra çeker. Bilinmeyen
upgrade Id'leri silinmez; gelecekteki veya geçici catalog içeriğinin kalıcı seviyesini kaybetmez.
`RewardedRunIds` son 128 run identity'siyle sınırlıdır.

## Sürüm ve migration sözleşmesi

- Güncel schema `v3`, desteklenen en eski schema `v1`dir.
- `v1 -> v2`: Souls, istatistik ve upgrade seviyeleri korunur; bulunmayan reward receipt
  geçmişi boş başlar.
- `v2 -> v3`: mevcut bütün state korunur; pool unlock ve tutorial flag listeleri boş başlar.
- Güncel sürümden büyük, minimumdan küçük veya bozuk JSON otomatik sıfırlanmaz.
  `LoadStatus` `UnsupportedVersion/Corrupt` olur ve `CanPersist = false` ile bütün meta write
  yolları fail-closed kilitlenir.
- Fail-closed durumda in-memory temiz state yalnız UI/runtime'ın açık kalmasını sağlar; orijinal
  dosyanın üzerine yazılmaz. Death receipt meta durable olana kadar beklemede kalır.
- `ResetAll()` yalnız explicit debug sahibidir; bilinmeyen dosyayı bilinçsiz migration yerine
  sessizce silemez.

Migration başarılıysa v3 state atomik biçimde hemen durable yazılır. `TryDeserializeState()`
saf schema/migration test sahibidir; Player-facing kod doğrudan çağırmaz.

## Pool unlock ve tutorial flag API'si

- `HasPoolUnlock` / `TryUnlockPoolContent`: yalnız stable content-pool Id'sini saklar. Bu state
  mevcut run'ın generated graph node/edge/Keystone sonucuna zorla içerik enjekte etmez.
- `HasTutorialFlag` / `SetTutorialFlag`: onboarding tamamlanma/reset state'inin canonical disk
  sınırıdır; tutorial davranışının kendisi Package I sahibinde kalır.
- Her iki mutation API'si atomik save başarısızsa in-memory değişikliği geri alır.

Ilk aktif consumer `FirstRunOnboardingUI`, `tutorial.v1.worker_ratio` flag'ini yalnız başarılı
gerçek player ratio action'ından; `tutorial.v1.basic_archer` flag'ini yalnız başarılı gerçek
Basic Archer satın alımından sonra yazar. Kalan tutorial adımları ve final complete/reset
flag'leri sonraki Package I tracker işlerinde kalır. Pool unlock consumer'ı da owner onaylı
content işini bekler. Satın alma ve graph izolasyon sınırı aşağıdaki runtime sözleşmesiyle
tamamlanmıştır.

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

`MetaUpgradeCatalogSO` Blueprint v1.0 sabit sırasındaki 11 definition'ın sahibidir:

1. `start_wood`, `start_stone`, `start_iron`, `start_food`
2. `start_archers`
3. `start_beds`
4. `wall_hp`
5. `production`
6. `arrow_efficiency`
7. `essence_gain`
8. `node_pool_unlock`

Runtime effect yolları açıkça ayrıdır:

- `StartingResource` -> saturating `AddResources`; Heart node açmaz.
- `StartingArchers` -> yalnız Basic `SpawnArcher`; ortak `1000` cap'i bypass etmez ve Rapid/Frost açmaz.
- `StartingBeds` -> `MobileBedCapacityState.BaseCapacity`; run satın alım fiyatı toplam sahip olunan
  kapasiteden büyümeye devam eder.
- `WallHpPercent` ve `ProductionPercent` -> mevcut defense/economy aggregate katmanları.
- `ArrowEfficiency` -> `ArrowSupply.MetaEfficiencyBonus`; paid run `EfficiencyLevel` ve Heart bonusundan ayrıdır.
- `EssenceGainPercent` -> `GrantGraveEssence`; kesirli kazanç `MetaGainAccumulator` ile birikir ve
  exact Continue'da korunur.
- `NodePoolUnlock` -> yalnız stable `UnlockedPoolIds`; aktif koşuya uygulanmaz.

`MetaUpgradePolicy.IsRunGraphIsolatedEffect()` bu sekiz effect yolunun fail-closed allowlist'idir.
`None`, legacy numeric `3`, Blueprint dışı eski numeric `5` ve gelecekte eklenecek tanımsız
effect'ler catalog validation'da reddedilir. `StartingTechLevel`, `ArcherDamagePercent`,
`TechNodeId`, `Meta_start_moat.asset` ve `Meta_archer_damage.asset` üretim modelinden kaldırılmıştır;
boşalan serialized değerler bilerek yeniden kullanılmaz.

Katalogdaki kaynak ve yatak sink'lerinde `MaxLevel=0` limitsiz anlamına gelir. Maliyet doğrusal
değil, `ceil(BaseCost * (1 + growth)^currentLevel)` formülüyle üstel büyür ve temsil sınırında
`int.MaxValue` değerine saturate olur. Basic Archer `1000` hard cap'ine kadar tanımlıdır; güç
yüzdeleri sınırlı level taşır; node-pool unlock tam bir kez alınabilir.

`GameManager.ApplyMetaProgressionAtRunStart()` her yeni koşuda state'i mevcut gameplay
kanallarından uygular. `_metaAppliedThisRun` aynı runtime başlangıcında çift uygulamayı önler.
Exact Continue başlangıç kaynak/yatak/okçu değerlerini snapshot'tan restore ettiği için bu
etkileri ikinci kez eklemez. Derived Arrow efficiency ve Essence gain yüzdesi kalıcı seviyelerden
yeniden kurulur; v13'te eklenen Essence kesirli remainder'ı güncel `RunSaveState v14` içinde run state olarak taşınır.
Saved legacy tech level replay'i `RestoreSavedTechNodeLevels()` sahibinde kalır; bu metottan meta
catalog'a açılan bir çağrı yolu yoktur.

## Death-only satın alma sınırı

Game Over panelinde görünmek tek başına yetki değildir. Otoriter satın alma API'si
`GameManager.TryBuyMetaUpgrade()` metodudur ve dört koşulu birlikte zorunlu tutar:

1. ECS/Mono cache Game Over durumundadır.
2. O koşunun death transaction'ı `GameManager` tarafından toplanmıştır.
3. `LastRunResult.Persisted`, reward receipt dahil meta state'in durable olduğunu doğrular.
4. `MetaProgression.CanPersist` true'dur.

`CanBuyMetaUpgrade()` ayrıca verilen definition'ın aktif `MetaUpgradeCatalogSO` içindeki aynı
canonical asset olduğunu doğrular. Aynı Id'yi taşıyan katalog dışı bir asset farklı fiyat veya
effect ile satın alma yapamaz. `MetaProgression.TryBuyUpgrade()` persistence katmanında internal
kaldığı için Player-facing kod bu ölüm sınırını doğrudan bypass edemez. `MetaProgressionUI`
button interactability ve click transaction'ını aynı `GameManager` kapısından geçirir.

Satın alınan seviye ölmüş koşuyu veya o koşunun generated Heart graph'ını değiştirmez; etkiler
yalnız sonraki `RestartGame()` / yeni koşu başlangıcında uygulanır. `UnlockedPoolIds` yalnız
gelecekte üretilecek graph için olası content havuzunu genişletebilir; mevcut graph node, edge,
Keystone lock veya result state'ine retroaktif yazamaz.

## Kurallar

- Wall HP ve worker production yüzdeleri tech/Council aggregate kanallarından geçer; Arrow meta
  verimi paid/Heart level'dan ayrı derived alandır, Essence gain ise yalnız grant kapısında uygulanır.
- Run sonucu yalnız kesin Game Over geçişinde toplanır; frame polling ile ödül verilmez.
- `MetaProgression.ResetAll` yalnız debug içindir ve oyuncu yüzeyine bağlanmaz.
- Run save ile meta save ayrı otoritelerdir. Meta state hiçbir zaman canlı koşunun phase/timer/combat snapshot'ını taşımaz.
- Meta write `AtomicJsonFile` üzerinden temp + flush + replace sözleşmesiyle yapılır; durable
  sonuç alınmadan death receipt temizlenmez.
- Meta satın alımı disk write başarısızsa Souls/seviye/pool unlock değişikliğini birlikte geri alır.
- Aktif koşuda, reward durable değilken veya meta persistence fail-closed iken satın alma yoktur.
- Pause Restart gizlidir; yaşayan save varken Main Menu New Run açılamaz. Game Over Restart yeni
  koşudur ve gönüllü prestige/reset sayılmaz.
- Canonical schema alanları run save'e kopyalanmaz; run-only Grave Essence, Heart graph,
  phase/timer ve combat snapshot yalnız `RunSaveState` sahibindedir.
