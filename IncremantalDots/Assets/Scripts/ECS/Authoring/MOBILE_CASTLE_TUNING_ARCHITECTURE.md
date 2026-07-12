# Mobile Castle Tuning Ownership - Mimari

## Amaç

Her tuning alanının tek bir baseline owner'ı olmalıdır. Inspector, Difficulty Profile, setup tool ve runtime ECS aynı alan için bağımsız doğruluk kaynakları değildir.

## Öncelik sözleşmesi

1. V1 Blueprint ürün davranışının tasarım otoritesidir.
2. `DifficultyProfileSO`, yalnız difficulty grubundaki baseline değerlerin içerik owner'ıdır.
3. Aktif SubScene'deki `MobileCastleCombatAuthoring`, profile taşınmamış geometri, mode, cycle süresi, ekonomi ve feedback baseline değerlerinin owner'ıdır.
4. `MobileCastleTuningResolver`, bu iki kaynağı `MobileCastleCombatConfig` içine birleştiren tek kod owner'ıdır.
5. `MobileCastleCombatConfig`, runtime çıktısıdır; editlenecek içerik kaynağı değildir.
6. Tech, meta progression ve Council etkileri baseline config üzerine runtime aggregate uygular. Bu effective değerler yeni baseline sayılmaz.

## DifficultyProfile-owned alanlar

- Zombie base HP ve HP/cycle growth
- Zombie base damage ve damage/cycle growth
- Spawn batch size ve batch/cycle growth
- Max spawn batch ve max alive zombie
- Base/min spawn interval
- Day, Dusk start/end, Night ve Dawn intensity
- Repair base Wood/Stone serialized tuning değerleri
- Gün eğrileri; SpecialNights schema/content V1'de dormant ve multiplier her zaman 1

Not: Normal V1 repair artık yalnız Stone kullandığı için `RepairBaseWoodCost` serialize uyumluluğu dışında gameplay tarafından okunmaz.

## MobileCastleCombatAuthoring-owned alan örnekleri

- Castle/spawn geometrisi, single-front ve moat baseline
- Continuous siege enable ve cycle süreleri
- Zombie scale/speed ve stress-test alanları
- Reward ve worker economy baseline değerleri
- Population growth, worker cap ve production değerleri
- Unlimited arrows gibi mode flag'leri
- Overlay, wave director phase oranları, Fortify/Rally baseline

Profile atanmışken aynı isimli shadow Inspector difficulty alanları fallback'tir; aktif runtime'a yazılmaz. Profile kaldırılırsa geriye uyumlu authoring fallback devreye girer.

## Editor araçları

`DifficultyTunerWindow` profile asset'i düzenler. Play Mode canlı uygulama ve Baker aynı `MobileCastleTuningResolver.ApplyDifficultyProfile` metodunu kullanır. Gün eğrisi/SpecialNight sample üretimi de aynı `ResolveDaySample` metodundadır; iki ayrı formül tutulmaz.

`MobileCastleSceneSetupWindow` yalnız owner tarafından açıkça çalıştırılan initializer/repair aracıdır. Runtime owner değildir. Tool'un yazdığı değerler scene/profile asset'e kaydedildikten sonra yukarıdaki sahiplik kuralına girer.

## Aktif proje kanıtı (2026-07-12)

Aktif SubScene `DefaultDifficulty.asset` profilini kullanır. Bilinçli olarak farklı serialize değerler resolver testidir:

| Alan | Authoring fallback | Profile | Runtime baseline |
|---|---:|---:|---:|
| Zombie HP growth/cycle | 0 | 0 | 0; utility ayrıca growth'ü okumaz |
| Spawn batch growth/cycle | 0.10 | 0.15 | 0.15 |
| Max spawn batch | 12 | 16 | 16 |
| Repair base Stone | 80 | 50 | 50 |
| Cycle Day/Dusk/Night/Dawn | 30/5/20/5 | Profile'da yok | 30/5/20/5 |

Runtime production gibi bazı alanlar tech/meta aggregate sonrasında baseline'dan farklı görünebilir. Örneğin `IronWorkerProductionPerMin` meta production bonusuyla artabilir; bu owner çakışması değildir.

## Doğrulama

- `MobileCastleTuningResolverTests.DifficultyProfile_OverridesOnlyProfileOwnedFields`
- `MobileCastleTuningResolverTests.ActiveSubScene_AssignsDefaultProfile_AndResolvesItsDivergentValues`
- `MobileCastleTuningResolverTests.DaySample_UsesSameCurveAndSpecialNightRulesForBakeAndLiveApply`
- `ExactRunContinuePlayModeTests.RuntimeTuning_UsesProfileDifficulty_AndAuthoringCycleDurations`
