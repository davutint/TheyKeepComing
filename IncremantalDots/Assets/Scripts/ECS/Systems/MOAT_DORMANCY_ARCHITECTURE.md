# Moat Dormancy Architecture

## V1 Sözleşmesi

Dead Walls V1 Blueprint core loop'unda Moat slow, damage veya satın alınabilir teknoloji değildir. Eski implementasyon gelecekteki içerik ihtimali ve serialization uyumluluğu için silinmez; aktif oyuna bağlanamaz.

## Runtime Guard

`MobileCastleCombatConfig.MoatGameplayEnabled` V1'de her zaman `false` bake edilir. `MoatSystem`, slow/damage değerlerini okumadan önce bu flag'i kontrol eder.

`MoatDormancyRules.ApplyV1` stale runtime değerlerini tek noktada neutral hale getirir:

- `MoatGameplayEnabled = false`
- `MoatSlowMultiplier = 1`
- `MoatDamagePerSecond = 0`

Bu kural Baker, tech aggregate ve runtime reset yollarında uygulanır. Böylece eski SubScene değeri, save state veya tech effect'i sistemi yeniden açamaz.

## Content Sınırı

Assetler fiziksel olarak korunur fakat aktif ürün catalog'larında yer almaz:

- `DeeperMoat.asset` (`moat_dig`)
- `BurningMoat.asset` (`moat_flame`)
- `Meta_start_moat.asset` (`start_moat`)

`TechTreeCatalog.asset`, `MetaUpgradeCatalog.asset` ve `WallReinforcement.asset` reveal bağlantısı bu içerikleri dışarıda bırakır. Setup tool aynı dormant id'leri yeniden seed veya merge etmez. `GameManager`, stale bir catalog/save üzerinden Moat effect'i gelirse effect'i yok sayar; meta restore da Moat node level'i grant etmez.

Hendek görseli ve `MoatXMin/MoatXMax` geometri alanları world-art/migration verisi olarak kalabilir. Görsel hendek gameplay etkisi anlamına gelmez.

## Doğrulama

- `MoatDormancyRulesTests.ApplyV1_NeutralizesStaleMoatRuntimeValues`
- `MoatDormancyRulesTests.ActiveCatalogs_ExcludeMoatTechAndMetaContent`
- `ExactRunContinuePlayModeTests.RuntimeTuning_UsesProfileDifficulty_AndAuthoringCycleDurations`
- `ExactRunContinuePlayModeTests.StaleMoatTuning_CannotSlowOrDamageZombieInV1Runtime`

Runtime testi aktif `NewGameScene` config'ine `0.05` slow ve `100000 DPS` enjekte eder; V1 flag kapalıyken zombie HP, MoveSpeed ve `ZombieSlow` enabled state'i değişmez.
