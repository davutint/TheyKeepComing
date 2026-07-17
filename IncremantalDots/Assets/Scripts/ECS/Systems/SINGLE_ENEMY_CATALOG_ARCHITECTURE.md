# Single Enemy Catalog - Mimari

## V1 sözleşmesi

V1 çıkışı yalnız tek düşman içerir: `zombie_basic`. Boss, elite, varyant seçimi veya enemy-type özel runtime dalı yoktur. Tek kayıtlı içerik `Assets/ScriptableObject/MobileCastle/Enemies/EnemyCatalog.asset`, tanım ise `BasicZombie.asset` dosyasıdır.

## Veri sahipliği

`EnemyDefinitionSO` aşağıdaki içerik alanlarının tek sahibidir:

- Kalıcı düşman kimliği ve prefab referansı
- Base HP, damage, movement speed ve görsel scale
- XP reward ve spawn weight
- Pool prewarm ve ihtiyaç halinde genişleme batch metadata'sı

`EnemyCatalogSO`, aktif düşman kimliğini ve tanım listesini tutar. V1 validation sözleşmesi catalog'da tam olarak bir geçerli tanım bulunmasını ve `ActiveEnemyId` değerinin bu tanıma çözülmesini zorunlu kılar.

`MobileCastleCombatConfig` base statların içerik kaynağı değildir. Baker önce legacy Profile/Authoring mapping'ini uygular, ardından aktif `EnemyDefinitionSO` değerlerini config'e yazar. Böylece runtime sistemleri için tek sonuç üretilirken içerik doğruluğu catalog'da kalır.

## Bake ve runtime akışı

1. `WaveConfigAuthoring.EnemyCatalog`, tanımları `EnemyCatalogEntryData` buffer'ına bake eder.
2. `EnemyCatalogRuntimeData`, kayıt sayısını ve aktif kayıt index'ini taşır.
3. `WaveSpawnSystem`, aktif index'i çözer ve o kaydın entity prefabını instantiate eder.
4. Mobile castle mode'da yeni entity'nin HP, damage, speed, scale ve XP değerleri aynı catalog kaydından yazılır.
5. `ZombiePrefabData`, eski sistemlerle serialized/runtime uyumluluk için aktif catalog prefabından üretilen compatibility output'tur; bağımsız owner değildir.

Seçim index üzerinden yapılır. Spawn veya UI kodunda `zombie_basic`, enum ya da prefab adına göre özel bir switch/if zinciri bulunmaz.

## Legacy fallback

Catalog atanmamış eski sahnelerde `WaveConfigAuthoring.ZombiePrefab`, `legacy_zombie` adlı tek geçici runtime kaydına çevrilir. Bu yol migration uyumluluğudur; aktif `NewGameScene` catalog kullanır.

## Pool sınırı

`PoolPrewarm` ve `PoolExpandBatch` alanları bake edilir ve `EnemyPoolInitializationSystem` tarafından gerçek runtime rezervine çevrilir. Spawn pool rent, ölüm pool return kullanır; ayrıntılı yaşam döngüsü `ENEMY_POOL_ARCHITECTURE.md` dosyasındadır.

## Yeni düşman ekleme sınırı

V1 ürün kararı değişmeden ikinci prefab catalog'a eklenmez. İleride kapsam onaylanırsa yeni içerik prefab adına göre kod dalı eklemek yerine yeni `EnemyDefinitionSO` ile catalog'a kaydedilir; catalog validation sözleşmesi de bilinçli olarak V1 tek-kayıt kuralından genişletilir.

## Çıkış catalog guard'ı

Production düşman içeriği `Assets/ScriptableObject/MobileCastle/Enemies` klasöründe tam bir `EnemyCatalogSO` ve tam bir `EnemyDefinitionSO` ile sınırlıdır. `MobileCastleCombatSubScene` içindeki tek `WaveConfigAuthoring` ve tek `MobileCastleCombatAuthoring` aynı production catalog'a bağlı olmak zorundadır; legacy `ZombiePrefab` compatibility alanı da catalog tanımındaki aynı `Zombie.prefab` referansını taşır. Bu sınırlar EditMode release guard'ında birlikte denetlenir. Böylece ikinci bir asset eklemek, iki authoring sahibini farklı catalog'lara bağlamak veya legacy prefab referansını catalog'dan ayırmak V1 çıkış testini doğrudan kırar.

## Doğrulama

- `EnemyCatalogContractTests`: production klasöründe tek catalog/tanım, iki SubScene authoring sahibinde aynı catalog/prefab bağlantısı, tek aktif kayıt, prefab/stat/pool metadata ve type branch gerektirmeyen index çözümü.
- `ExactRunContinuePlayModeTests.RuntimeTuning_UsesProfileDifficulty_AndAuthoringCycleDurations`: bake edilmiş runtime catalog/config/prefab eşleşmesi.
- `ExactRunContinuePlayModeTests.EnemyCatalog_SpawnsRegisteredPrefabWithDefinitionStats`: gerçek spawn edilen entity'nin catalog prefabı ve tanım statlarıyla oluşması.
