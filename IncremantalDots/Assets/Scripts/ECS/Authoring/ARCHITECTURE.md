# Authoring + Baker - Mimari

## Genel Yapi
Her authoring MonoBehaviour, ECS entity'ye donusturulmek uzere Sub Scene icine yerlestirilen GameObject'lere eklenir.
Baker class'lari authoring degerlerini okuyarak entity'lere IComponentData ekler.

## Dosyalar

### ZombieAuthoring.cs
- Zombi prefab'ina eklenir
- ZombieTag, ZombieStats, ZombieState ve disabled ZombieSlow component'larini bake eder

### CastleAuthoring.cs
- Kale/duvar GameObject'ine eklenir (Sub Scene icinde tek instance)
- Aktif savunma icin yalniz `WallSegment` ve `WallXPosition` bake eder; legacy Gate/Core alanlari serialize uyumlulugu icin gizli tutulur

### ArcherAuthoring.cs
- Okcu prefab'ina eklenir
- ArcherUnit component'ini bake eder
- Type, fire rate, damage, range ve slow effect degerlerini Inspector'dan tasir
- Tint alanini Inspector'da tasir; SpriteSheetAuthoring ayni objede varsa SpriteTint bake ederken bu degeri kaynak olarak kullanir

### ArrowAuthoring.cs
- Ok prefab'ina eklenir
- ArrowTag, ArrowProjectile bake eder
- Projectile effect datasini varsayilan prefab degeri olarak tutar; runtime'da ArcherShootSystem okcu tipine gore override eder
- Tint alanini Inspector'da tasir; SpriteSheetAuthoring ayni objede varsa SpriteTint bake ederken bu degeri kaynak olarak kullanir

### GameStateAuthoring.cs
- Oyun durumu singleton entity'si olusturur
- GameStateData, WaveStateData, resource, population ve ArrowSupply singleton verilerini bake eder
- Initial wave sayisi, HP, damage, spawn interval ve initial arrow degerleri Inspector'dan ayarlanabilir
- NewGameScene setup tool mobile default kaynaklari yazar: Wood `150`, Stone `80`, Iron `45`, Food `150`, Arrows `200`
- Mobile passive income degerleri ayni authoring uzerinden bake edilir: Wood `+90/min`, Stone `+50/min`, Iron `+30/min`, Food `+75/min`

### WaveConfigAuthoring.cs
- `EnemyCatalogSO` ile aktif enemy prefab/stat/pool metadata kaynagini tutar; `ZombiePrefab` yalniz eski sahneler icin migration fallback'idir
- `EnemyCatalogRuntimeData` ve `EnemyCatalogEntryData` buffer'ini bake eder
- Aktif catalog kaydindan compatibility `ZombiePrefabData`; ayrica ArrowPrefabData ve ArcherPrefabData bake eder
- Sub Scene icinde GameStateAuthoring ile ayni GameObject'e eklenebilir

### MobileCastleCombatAuthoring.cs
- NewGameScene mobile castle mode switch'idir
- MobileCastleCombatConfig singleton'ini bake eder
- Aktif `EnemyDefinitionSO` base HP/damage/speed/scale degerlerini runtime config'e son owner olarak uygular
- ArcherSlotPosition buffer'ina SubScene slot transform pozisyonlarini yazar
- Kill reward, wave clear bonus ve economy focus tuning degerlerini Inspector'dan ECS config'e tasir
- `EconomyFocusState` singleton'ini `Balanced` default'u ile bake eder
- `WaveClearRewardData` mobile reward feedback state'ini bake eder
- `CastleYardPrepState` tek-gecelik Fortify/Rally state'ini bake eder
- Day/night, wave director, unlimited arrow ve Castle Yard prep defaultlarini Inspector'dan ECS config'e tasir
- Config yoksa runtime sistemleri eski WallX mode'a doner

## Prefab Referans Akisi
Mobile castle icin `WaveConfigAuthoring.ArcherPrefab` bake edilir ve `GameManager` tarafindan drawer economy uzerinden satin alinan Basic/Rapid/Frost okcu spawn'inda kullanilir.

WaveConfigAuthoring.EnemyCatalog → EnemyDefinitionSO → EnemyCatalogEntryData buffer → WaveSpawnSystem
EnemyCatalogEntryData(active).Prefab → ZombiePrefabData.ZombiePrefab (legacy compatibility output)
WaveConfigAuthoring.ArrowPrefab → Baker.GetEntity() → ArrowPrefabData.ArrowPrefab (Entity)
