# Continuous Spawn Budget Architecture

## Amaç

Continuous horde akışında teknik alive cap doluyken geçen spawn talebi kaybolmaz. Her geçen interval explicit backlog üretir; kapasite açılınca backlog güvenli bir frame batch sınırıyla sahaya geri akar.

## Runtime Owner

`ContinuousSpawnBudgetData`, aktif config entity üzerindeki tek spawn budget ve runtime telemetry state'idir.

- `PendingEnemies`: henüz sahaya çıkmamış exact backlog.
- `TotalDemandedEnemies`: koşu boyunca budget'ın ürettiği toplam talep.
- `TotalSpawnedEnemies`: budget'tan gerçekten sahaya aktarılan toplam sayı.
- `DemandPerInterval`, `LastDemandedEnemies`, `LastSpawnedEnemies`: canlı tanılama değerleri.
- `DayQuantityMultiplier`, `DayBaseSpawnInterval`: güne ait taban.
- `PhaseIntensityMultiplier`, `EffectiveSpawnInterval`: Day/Dusk/Night/Dawn temposu.

`ContinuousSpawnBudgetUtility` count/batch/interval matematiğinin saf owner'ıdır. `WaveSpawnSystem` yalnız girdileri toplar, utility sonucunu state'e yazar ve spawn emrini uygular.

## Hesap Sırası

1. `ContinuousSiegeCycleSystem`, gün/cycle index'ini ve phase intensity değerini üretir.
2. `MobileWaveUtility`, günün base count ve base interval değerini kurar. Enemy HP/damage/speed sabit kalır.
3. `WaveSpawnSystem`, `DifficultyDaySample.SpawnBatchMult` değerini day quantity multiplier olarak okur.
4. Day tabanı ve phase multiplier ayrı state alanlarına yazılır.
5. Frame içinde geçen her effective interval `PendingEnemies` değerine demand ekler.
6. Alive cap doluysa pending state korunur. Yer varsa frame başına en fazla `MaxSpawnBatch` ve mevcut alive kapasitesi kadar spawn edilir.

Bu ayrım sayesinde Dawn'ın düşük phase intensity değeri yalnız anlık tempo hesabını etkiler; sonraki günün `DayBaseSpawnInterval` veya day curve tabanına geri yazılmaz.

## Save ve Telemetry

Exact run snapshot aşağıdaki state'i saklar ve Continue sırasında aynı config entity'ye geri yükler:

- pending backlog,
- total demanded/spawned sayaçları,
- son demand/spawn değerleri,
- day ve phase ayrıştırılmış multiplier/interval değerleri.

Bu alanlar aynı zamanda runtime telemetry yüzeyidir. `GameManager.TryGetContinuousSpawnBudget` ile debug, simulator veya ilerideki telemetry bridge tarafından okunabilir. Player-facing HUD'a backlog sayısı eklenmez.

## Performans Sınırı

Backlog'un tamamı tek frame'de boşaltılmaz. Drain miktarı:

`min(PendingEnemies, MaxAliveZombies - ZombiesAlive, MaxSpawnBatch)`

olarak sınırlıdır. `MaxAliveZombies = 0` yalnız legacy “alive cap yok” sentineli olarak korunur. Entity pooling bu sistemin işi değildir; Package B içindeki sonraki pool işi instantiate/destroy churn'ünü ayrıca çözecektir.

## Doğrulama

- `ContinuousSpawnBudgetUtilityTests`: day/phase ayrımı, çoklu elapsed interval birikimi ve drain limitleri.
- `RunPersistenceTests`: JSON round-trip backlog ve telemetry alanlarını korur.
- `ExactRunContinuePlayModeTests.Continue_RestoresSameCyclePhaseTimerResourcesAndSpawnRng`: Continue exact backlog'u geri getirir.
- `ExactRunContinuePlayModeTests.ContinuousSpawnBudget_AccumulatesAtCap_AndDrainsWhenCapacityOpens`: gerçek `NewGameScene` içinde cap altında birikme ve kapasite açılınca spawn kanıtı.
