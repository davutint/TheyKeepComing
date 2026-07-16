# Expandable Enemy Pool - Mimari

## Amaç

V1 tek düşman akışında sürekli `Instantiate/DestroyEntity` churn'ü yerine küçük bir başlangıç rezervi, ihtiyaç halinde batch genişleme ve ölümde tekrar kullanım uygulanır. Pool kapasitesi gameplay active cap veya spawn backlog değildir; yalnız entity yaşam döngüsü altyapısıdır.

## Veri sözleşmesi

- `EnemyDefinitionSO.PoolPrewarm`: world açıldığında hazırlanacak inactive entity sayısı. Aktif `zombie_basic` değeri `128`.
- `EnemyDefinitionSO.PoolExpandBatch`: rezerv tükendiğinde tek seferde üretilecek entity sayısı. Aktif değer `128`.
- `EnemyPoolRuntimeData`: initialized, total/available/active, expansion ve toplam rent/return telemetry değerlerini taşır.
- `EnemyPoolAvailable`: inactive entity referanslarının LIFO rezerv buffer'ıdır.
- `EnemyPoolMember`: catalog entry index ve her rent'te artan generation değerini taşır.

## Yaşam döngüsü

1. `EnemyPoolInitializationSystem`, catalog bake edildikten sonra aktif entry'nin `PoolPrewarm` sayısını hazırlar.
2. Inactive entity'de enableable `ZombieTag` kapalıdır ve `LocalTransform.Scale = 0` olduğu için gameplay query'lerine girmez ve görünmez.
3. `WaveSpawnSystem`, `EnemyPoolRuntimeUtility.TryRent` ile rezervden entity alır.
4. Rezerv boşsa utility tam `PoolExpandBatch` kadar yeni entity üretip rezervi genişletir.
5. Rent, generation değerini artırır; state/slow/death timer/physics/tint transient verilerini sıfırlar ve `HordeMotionCadenceUtility` ile animation frame/timer fazını entity index + generation üzerinden deterministik dağıtır.
6. `DamageCleanupSystem`, ölüm animasyonu bittiğinde ödülü bir kez yazar; dönecek pool üyelerini toplar, transient component reset'ini Burst-parallel job ile yapar ve bütün entity'leri tek `CommitBulkReturn` buffer/state yazımıyla rezerve ekler.

`ZombieTag` ve `DeathTimer` enableable component'tir. Ölüm animasyonu `DeathTimer` verisini ve enabled state'ini job içinde doğrudan yazar; 10K ölümde entity başına ECB komutu üretmez. Normal rent/return structural archetype değişikliği yapmaz; yalnız gerçek pool genişlemesi entity instantiate eder.

## Projectile güvenliği

`ArrowProjectile.TargetPoolGeneration`, okun atıldığı anda hedefteki `EnemyPoolMember.Generation` değerini kaydeder. `ArrowMoveSystem` ve `ArrowHitSystem` şu durumlarda oku deterministik olarak kendi pool'una döndürür:

- Hedef entity artık yoksa
- Hedef `ZombieTag` disabled olduğu için pool rezervindeyse
- Aynı entity yeniden rent edilmiş fakat generation değişmişse

Bu sözleşme, eski okun yeniden kullanılan aynı entity kimliğindeki yeni zombiye yanlışlıkla taşınmasını veya hasar vermesini engeller. Retarget yapılmaz.

## Save, Continue ve restart

- Inactive pool rezervi `ZombieTag` disabled olduğu için combat snapshot query'sine girmez.
- Continue yalnız kaydedilmiş aktif zombileri pool'dan rent eder ve exact stat/state/slow/death timer değerlerini geri yazar.
- Restore öncesindeki aktif pool üyeleri rezerve döndürülür.
- Restart aktif pool üyelerini destroy etmez; rezerv buffer'ına geri koyar. Legacy/non-pool zombie kalırsa eski cleanup fallback'iyle silinir.
- Pool kapasitesi veya telemetry save schema'ya yazılmaz; catalog metadata'dan deterministik olarak yeniden kurulur. Spawn backlog snapshot'ı bundan bağımsızdır.

## Doğrulama

- `EnemyPoolRuntimeUtilityTests.Pool_PrewarmExpandsRentsReturnsAndResetsTransientState`
- `HordeReadabilityTests.MotionCadence_SeedDistributesFramesAndTimerSlicesDeterministically`
- `ExactRunContinuePlayModeTests.EnemyPool_DeathReturnsEntityAndRejectsStaleArrowGeneration`
- Tam regresyon: EditMode `34/34`; PlayMode `13/13`, hedefli profiler capture normal sette explicit skip.
- 10K runtime ölçümü: `Assets/Docs/DEAD_WALLS_10K_RUNTIME_REPORT.md`.

## Kapsam dışı

- Arrow entity pooling ayrı `ARROW_POOL_ARCHITECTURE.md` işinde tamamlanmıştır; bu doküman yalnız enemy pool owner'ını tarif eder.
- VFX/SFX pool'ları `CombatFeedbackBridge` sorumluluğunda kalır.
- 10.000 aktif enemy death/allocation optimizasyonu ölçüldü; GPU draw-call ve build save/restore bütçesi `DW-B-SCALE-OPT` altında açık kalır.
