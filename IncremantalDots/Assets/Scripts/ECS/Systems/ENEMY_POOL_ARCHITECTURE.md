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
5. Rent, generation değerini artırır ve state/slow/death timer/physics/tint/animation transient verilerini sıfırlar.
6. `DamageCleanupSystem`, ölüm animasyonu bittiğinde ödülü bir kez yazar ve entity'yi destroy etmek yerine pool'a döndürür.

`ZombieTag` ve `DeathTimer` enableable component'tir. Normal rent/return structural archetype değişikliği yapmaz; yalnız gerçek pool genişlemesi entity instantiate eder.

## Projectile güvenliği

`ArrowProjectile.TargetPoolGeneration`, okun atıldığı anda hedefteki `EnemyPoolMember.Generation` değerini kaydeder. `ArrowMoveSystem` ve `ArrowHitSystem` şu durumlarda oku deterministik olarak siler:

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
- `ExactRunContinuePlayModeTests.EnemyPool_DeathReturnsEntityAndRejectsStaleArrowGeneration`
- Tam regresyon: EditMode `34/34`, PlayMode `12/12`.

## Kapsam dışı

- Arrow entity pooling bu işte yoktur.
- VFX/SFX pool'ları `CombatFeedbackBridge` sorumluluğunda kalır.
- 10.000 aktif enemy frame pacing ve ürün senaryosu `DW-B-SCALE` işidir.
