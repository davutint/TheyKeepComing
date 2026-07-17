# Quantity-only Difficulty - Mimari

## V1 sözleşmesi

Enemy tipi değişmediği sürece zombie HP, attack damage ve movement speed bütün gün/cycle'larda sabittir. Zorluk yalnız sahadaki miktar ve akış baskısıyla artar.

## Sabit stat owner'ları

Aktif `EnemyDefinitionSO`, prefab ile birlikte base HP, damage, movement speed ve scale değerlerinin içerik owner'ıdır. `MobileCastleCombatAuthoring` Baker'ı bu değerleri `MobileCastleCombatConfig` runtime çıktısına yazar. `MobileWaveUtility.ConfigureMobileWave` her cycle'da şu sabit runtime baseline değerlerini kullanır:

- `ZombieHP = MobileCastleCombatConfig.ZombieBaseHP`
- `ZombieDamage = MobileCastleCombatConfig.ZombieBaseDamage`
- `ZombieSpeed = MobileCastleCombatConfig.BaseZombieSpeed`

`ZombieHpGrowthPerCycle`, `ZombieDamagePerCycle`, `ZombieSpeedPerWave` ve `DifficultyDaySample.ZombieHpMult` eski serialized içerik uyumluluğu için kalabilir fakat V1 stat hesabında okunmaz. Aktif Authoring ve DefaultDifficulty değerleri ayrıca `0` tutulur.

`DifficultyProfileSO` içindeki legacy base HP/damage alanları fallback mapping sırasında okunabilir; aktif catalog varken `EnemyDefinitionSO` bunların üzerine yazılır. Böylece miktar eğrileri Profile'da, düşman kimliği ve base statları catalog'da kalır.

## Artan baskı

Quantity pressure kanalları korunur:

- `ZombiesToSpawn = BaseWaveEnemyCount + cycle * ExtraEnemiesPerWave`
- Spawn interval wave multiplier ile minimum interval'e doğru düşer.
- Continuous phase intensity batch/interval davranışını değiştirir.
- `SpawnBatchGrowthPerCycle`, `SpawnBatchMultByDay` ve max batch quantity tuning'idir.

Bu ayrım, ilerleyen günlerde düşmanı süngerleştirmeden kalabalık hissini büyütür.

## Güvenlik

Utility stat growth alanlarını tamamen görmezden gelir. Böylece eski bir scene/profile değeri yanlışlıkla sıfırdan büyük kalsa bile HP/damage/speed progression geri dönmez.

## Çıkış profile guard'ı

Production difficulty içeriği `Assets/ScriptableObject/MobileCastle/Difficulty/DefaultDifficulty.asset` dosyasındaki tek `DifficultyProfileSO` ile sınırlıdır. `MobileCastleCombatSubScene` içindeki tek `MobileCastleCombatAuthoring` bu profile ve geçerli production enemy catalog'una bağlı olmak zorundadır. Production profile ve authoring üzerindeki HP/damage/speed growth alanları neutral kalır; buna karşılık extra enemy count, cycle batch growth, day/night intensity ve daralan spawn interval kanalları aktif tutulur. EditMode release guard'ı gerçek profile + SubScene authoring + enemy definition bileşimini `MobileWaveUtility` üzerinden Day 1 ve ileri cycle karşılaştırmasına sokar.

## Doğrulama

- `MobileWaveUtilityTests.ConfigureMobileWave_IgnoresStatGrowthFields_ButIncreasesQuantityPressure`
- `MobileWaveUtilityTests.ProductionProfileAndSubScene_KeepStatsFixedAndQuantityPressureActive`
- `ExactRunContinuePlayModeTests.AdvancedCycle_IncreasesQuantityButKeepsEnemyStatsFixed`
- `ExactRunContinuePlayModeTests.EnemyCatalog_SpawnsRegisteredPrefabWithDefinitionStats`

Testler Day 1 ile ileri cycle'ı karşılaştırır: HP/damage/speed eşit kalırken enemy count artar ve spawn interval daralır.
