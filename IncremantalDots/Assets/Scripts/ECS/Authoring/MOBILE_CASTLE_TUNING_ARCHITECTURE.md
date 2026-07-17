# Mobile Castle Tuning Ownership - Mimari

## Amaç

Her tuning alanının tek bir baseline owner'ı olmalıdır. Inspector, Difficulty Profile, setup tool ve runtime ECS aynı alan için bağımsız doğruluk kaynakları değildir.

## Öncelik sözleşmesi

1. V1 Blueprint ürün davranışının tasarım otoritesidir.
2. Aktif `EnemyDefinitionSO`, enemy prefabı ile base HP/damage/speed/scale değerlerinin içerik owner'ıdır.
3. `DifficultyProfileSO`, enemy base statları dışındaki quantity/difficulty baseline değerlerinin içerik owner'ıdır.
4. Aktif SubScene'deki `MobileCastleCombatAuthoring`, profile taşınmamış geometri, mode,
   cycle süresi, initial bed, worker cap ve feedback baseline değerlerinin owner'ıdır.
5. `MobileCastleTuningResolver` Profile/Authoring değerlerini birleştirir; Baker aktif EnemyDefinition base statlarını son adımda runtime config'e uygular.
6. `MobileCastleCombatConfig`, runtime çıktısıdır; editlenecek içerik kaynağı değildir.
7. Tech, meta progression ve Council etkileri baseline config üzerine runtime aggregate uygular. Bu effective değerler yeni baseline sayılmaz.

## EnemyDefinition-owned alanlar

- Enemy id ve prefab
- Zombie base HP ve damage
- Zombie base movement speed ve scale
- XP reward, spawn weight ve pool metadata

## DifficultyProfile-owned alanlar

- Spawn batch size ve batch/cycle growth
- Max spawn batch ve max alive zombie
- Base/min spawn interval
- Day, Dusk start/end, Night ve Dawn intensity
- Wall base HP, normal repair heal paketi, Stone/HP, Day fiyat carpani
- Emergency Repair heal yuzdesi ve cooldown
- Repair base Wood/Stone legacy serialized uyumluluk degerleri (aktif fiyat owner'i degil)
- Dawn/cycle basina istenen survivor sayisi ve kabul edilen kisi basi tek seferlik Food
- House bed başlangıç Wood maliyeti ve owned-bed büyüme interval'i
- Worker CAP/EFF ayrı Wood/Iron başlangıç maliyetleri ve ortak bina büyüme çarpanı
- Wood/Stone/Iron/Food worker başına üretim baseline'ları
- Worker Efficiency seviye başına additive üretim yüzdesi
- Gün eğrileri; SpecialNights schema/content V1'de dormant ve multiplier her zaman 1

Not: Normal V1 repair yalnız Stone kullanir. `RepairBaseWoodCost` ve
`RepairBaseStoneCost` serialize uyumlulugu disinda gameplay tarafindan okunmaz; aktif fiyat
`RepairStonePerMissingHp x RepairDayPriceMultiplier x gercek heal x discounts` formuludur.
Profile varsa `WallBaseHp` runtime baseline'i olur; profile yoksa `CastleAuthoring.WallHP`
bake degeri fallback kalir.

## RunDifficultyProfile contract (V1)

Tracker'daki `RunDifficultyProfile` yeni veya paralel bir ScriptableObject değildir. Canlı
contract, mevcut owner'ların aşağıdaki tek zinciridir:

1. **BaseSpawn curve:** `DifficultyProfileSO.SpawnBatchMultByDay`, Baker/live apply sırasında
   `DifficultyDaySample.SpawnBatchMult` olarak örneklenir. `BaseSpawnInterval`,
   `MinSpawnInterval`, `SpawnBatchSize` ve `SpawnBatchGrowthPerCycle` aynı profile baseline'ına
   aittir. `WaveSpawnSystem` gün tabanını phase temposundan ayrı çözer.
2. **Phase multipliers:** Day, Dusk start/end, Night ve Dawn değerleri
   `MobileCastleTuningResolver` üzerinden `MobileCastleCombatConfig` runtime çıktısına yazılır.
3. **Active cap:** Normal run tavanının içerik owner'ı `DifficultyProfileSO.MaxAliveZombies`dır.
   Editor/Development 2K/5K/10K harness'i yalnız transient test oturumunda bu tavana kontrollü
   istisna uygular; production asset'i değiştirmez.
4. **Backlog policy:** V1 politikası sabit `PreserveDemand`dır. Active cap doluyken geçen
   interval talepleri düşürülmez veya görünmezce sıkıştırılmaz; saturating `long`
   `ContinuousSpawnBudgetData.PendingEnemies` içinde exact save/Continue state'i olarak tutulur.
   Kapasite açıldığında `ContinuousSpawnBudgetUtility.ResolveDrainCount`, hem boş active
   kapasiteyi hem `MaxSpawnBatch` frame sınırını aşmadan backlog'u azaltır.

Backlog policy designer tarafından değiştirilebilir bir enum değildir. Talebi düşüren alternatif
bir mod, oyuncunun göremediği difficulty kaybı ve Continue farkı üreteceği için V1 contract'ının
dışındadır. İçerik değerleri `DifficultyProfileSO`, politika/matematik
`ContinuousSpawnBudgetUtility`, runtime telemetry/save state'i ise
`ContinuousSpawnBudgetData` sahibindedir; bu üçü birbirinin paralel baseline'ı değildir.

## MobileCastleCombatAuthoring-owned alan örnekleri

- Castle/spawn geometrisi, single-front ve moat baseline
- Continuous siege enable ve cycle süreleri
- Zombie scale/speed ve stress-test alanları
- Reward ve worker economy baseline değerleri
- Initial bed ve worker cap değerleri
- Population growth/Food alanlari yalniz profile yokken fallback'tir
- Worker production alanları yalnız profile yokken fallback'tir
- Unlimited arrows gibi mode flag'leri
- Overlay, wave director phase oranları, Fortify/Rally baseline

Profile atanmışken aynı isimli shadow Inspector difficulty alanları fallback'tir; aktif runtime'a yazılmaz. Profile kaldırılırsa geriye uyumlu authoring fallback devreye girer. Legacy Profile/Authoring enemy base stat alanları mapping uyumluluğu için kalabilir; catalog atanmış aktif sahnede EnemyDefinition bunların üzerine yazar.

## Editor araçları

`DifficultyTunerWindow` profile asset'i düzenler. Play Mode canlı uygulama ve Baker
difficulty config için aynı `MobileCastleTuningResolver.ApplyDifficultyProfile` metodunu,
ekonomi fiyatları için aynı `ResolveEconomyPriceTuning` metodunu kullanır. Gün eğrisi/
SpecialNight sample üretimi de aynı `ResolveDaySample` metodundadır; iki ayrı formül tutulmaz.
`Spawn Runtime Contract` paneli bu owner zincirini day preview + live ECS telemetry olarak tek
yüzeyde toplar. `PendingEnemies` read-only gösterilir; backlog policy tune edilmez. Designer yalnız
BaseSpawn curve/interval, phase multipliers, `MaxSpawnBatch` drain hızı ve `MaxAliveZombies`
active cap değerlerini profile üzerinden değiştirir.

`Wall Runtime Contract` paneli profile-owned `WallBaseHp`, normal repair heal paketi,
Stone/HP, Day fiyat carpani ve Emergency yuzdesini tek yuzeyde duzenler. Baseline paket
preview'u `SingleWallDefenseRules` ile runtime formulunu paylasir. Play Mode telemetry,
baseline/effective MaxHP, mevcut HP, gercek Stone quote ve phase gate'i gosterir; live Apply
HP oranini koruyarak tech/meta/Heart aggregate'lerini yeniden fold eder.

`Economy Runtime Contract` paneli profile-owned dort kisi basi production baseline'ini,
CAP/EFF Wood+Iron ilk maliyetlerini, ortak fiyat buyumesini ve additive EFF yuzdesini tek
yuzeyde toplar. Preview runtime utility'lerini kullanir. Play Mode Apply
`GameManager.ApplyWorkerEconomyTuning` ile mevcut tech/meta/Heart ve bina katmanlarini yeni
base rate'lere yeniden fold eder; effective config yeni baseline sayilmaz.

`Population Runtime Contract` paneli profile-owned Dawn request ile Food/arrival degerini ve
profile-owned House bed fiyat egrisini tek yuzeyde toplar. Preview/live telemetry ayni
`MobilePopulationArrivalUtility` ve `MobileBedCapacityUtility` owner'larini kullanir. Baslangic
yatagi SubScene Authoring state'idir; live Apply mevcut `MobileBedCapacityState` degerini
sifirlamadan yalniz config ve fiyat tuning baseline'larini gunceller.

`MobileCastleSceneSetupWindow` yalnız owner tarafından açıkça çalıştırılan initializer/repair aracıdır. Runtime owner değildir. Tool'un yazdığı değerler scene/profile asset'e kaydedildikten sonra yukarıdaki sahiplik kuralına girer.

## Aktif proje kanıtı (2026-07-12)

Aktif SubScene `DefaultDifficulty.asset` profilini kullanır. Bilinçli olarak farklı serialize değerler resolver testidir:

| Alan | Authoring fallback | Profile | Runtime baseline |
|---|---:|---:|---:|
| Zombie base HP/damage/speed/scale | Legacy shadow fields | Legacy base HP/damage | `BasicZombie.asset`: 20/5/0.85/1.4 |
| Zombie HP growth/cycle | 0 | 0 | 0; utility ayrıca growth'ü okumaz |
| Spawn batch growth/cycle | 0.10 | 0.15 | 0.15 |
| Max spawn batch | 12 | 16 | 16 |
| Wall base HP | `CastleAuthoring` 350 fallback | 350 | 350 baseline + tech/meta/Heart aggregate |
| Legacy repair base Stone | 80 | 50 | serialized only; V1 fiyatinda okunmaz |
| Normal repair | Authoring fallback | %25, 0.10 Stone/HP, x1 Day | ayni profile baseline |
| Emergency repair | Authoring fallback | %20, 120s | ayni profile baseline |
| Dawn request / Food each | Authoring fallback 15 / 1 | 15 / 1 | Profile baseline: 15 / 1 |
| Initial bed capacity | 60 | Profile'da yok | `MobileBedCapacityState.BaseCapacity`: 60 |
| Bed base / interval | Profile owner | 100 / 25 | `MobileEconomyPriceTuning`: 100 / 25 |
| Worker CAP / EFF base | Profile owner | 100W+25I / 150W+50I | `MobileEconomyPriceTuning`: aynı |
| Worker bina growth | Profile owner | 1.35 | `MobileEconomyPriceTuning`: 1.35 |
| Worker production/min | Authoring fallback 8/5.5/4.9/7 | 8/5.5/4.9/7 | Profile baseline + aggregate |
| Worker EFF effect/level | Legacy code default %10 | %10 | `MobileEconomyPriceTuning`: additive %10 |
| Cycle Day/Dusk/Night/Dawn | 30/5/20/5 | Profile'da yok | 30/5/20/5 |

Runtime production gibi bazı alanlar tech/meta aggregate sonrasında baseline'dan farklı görünebilir. Örneğin `IronWorkerProductionPerMin` meta production bonusuyla artabilir; bu owner çakışması değildir.

## Doğrulama

- `MobileCastleTuningResolverTests.DifficultyProfile_OverridesOnlyProfileOwnedFields`
- `MobileCastleTuningResolverTests.ActiveSubScene_AssignsDefaultProfile_AndResolvesItsDivergentValues`
- `MobileCastleTuningResolverTests.DaySample_UsesSameCurveAndSpecialNightRulesForBakeAndLiveApply`
- `MobileCastleTuningResolverTests.RunDifficultyProfile_ClosesSpawnCurvePhaseCapAndPreservedBacklogContract`
- `MobileCastleTuningResolverTests.EconomyPriceTuning_UsesProfileValuesAndSanitizesInvalidInputs`
- `MobileCastleTuningResolverTests.PopulationRuntimeTuning_UsesProfileValuesAndSanitizesInvalidInputs`
- `MobilePopulationArrivalUtilityTests.DefaultsAndSanitizers_ClosePopulationRuntimeContract`
- `ExactRunContinuePlayModeTests.RuntimeTuning_UsesProfileDifficulty_AndAuthoringCycleDurations`
- `EnemyCatalogContractTests.Definition_OwnsBaseStatsAndFuturePoolMetadata`
- `ExactRunContinuePlayModeTests.EnemyCatalog_SpawnsRegisteredPrefabWithDefinitionStats`
