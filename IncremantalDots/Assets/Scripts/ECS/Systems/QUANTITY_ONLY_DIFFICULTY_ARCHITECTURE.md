# Quantity-only Difficulty - Mimari

## V1 sözleşmesi

Enemy tipi değişmediği sürece zombie HP, attack damage ve movement speed bütün gün/cycle'larda sabittir. Zorluk yalnız sahadaki miktar ve akış baskısıyla artar.

## Sabit stat owner'ları

`MobileWaveUtility.ConfigureMobileWave` şu baseline değerleri doğrudan yazar:

- `ZombieHP = MobileCastleCombatConfig.ZombieBaseHP`
- `ZombieDamage = MobileCastleCombatConfig.ZombieBaseDamage`
- `ZombieSpeed = MobileCastleCombatConfig.BaseZombieSpeed`

`ZombieHpGrowthPerCycle`, `ZombieDamagePerCycle`, `ZombieSpeedPerWave` ve `DifficultyDaySample.ZombieHpMult` eski serialized içerik uyumluluğu için kalabilir fakat V1 stat hesabında okunmaz. Aktif Authoring ve DefaultDifficulty değerleri ayrıca `0` tutulur.

## Artan baskı

Quantity pressure kanalları korunur:

- `ZombiesToSpawn = BaseWaveEnemyCount + cycle * ExtraEnemiesPerWave`
- Spawn interval wave multiplier ile minimum interval'e doğru düşer.
- Continuous phase intensity batch/interval davranışını değiştirir.
- `SpawnBatchGrowthPerCycle`, `SpawnBatchMultByDay` ve max batch quantity tuning'idir.

Bu ayrım, ilerleyen günlerde düşmanı süngerleştirmeden kalabalık hissini büyütür.

## Güvenlik

Utility stat growth alanlarını tamamen görmezden gelir. Böylece eski bir scene/profile değeri yanlışlıkla sıfırdan büyük kalsa bile HP/damage/speed progression geri dönmez.

## Doğrulama

- `MobileWaveUtilityTests.ConfigureMobileWave_IgnoresStatGrowthFields_ButIncreasesQuantityPressure`
- `ExactRunContinuePlayModeTests.AdvancedCycle_IncreasesQuantityButKeepsEnemyStatsFixed`

Testler Day 1 ile ileri cycle'ı karşılaştırır: HP/damage/speed eşit kalırken enemy count artar ve spawn interval daralır.
