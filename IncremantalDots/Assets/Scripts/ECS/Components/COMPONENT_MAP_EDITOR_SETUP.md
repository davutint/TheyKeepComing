# ECS Component Haritasi - Editor Kurulum

## Gereksinimler
- Unity 6 LTS
- com.unity.entities paketi kurulu olmali
- DeadWalls.asmdef referanslari dogru olmali

## Notlar
- Component'lar dogrudan kullanilmaz, Authoring + Baker uzerinden entity'lere eklenir.
- Singleton component'lar (GameStateData, WaveStateData, Resource*) icin GameStateAuthoring kullanilir.
- Component degerlerini degistirmek icin ilgili Authoring component'inin Inspector degerlerini duzenleyin.
- ZombieSlow editor'de ayri kurulum istemez; ZombieAuthoring tarafindan disabled olarak bake edilir ve Frost ok isabetinde runtime'da enable edilir.
- SpriteTint editor'de SpriteSheetAuthoring, ArcherAuthoring veya ArrowAuthoring tint alanindan bake edilir. Mobile combat'ta runtime Basic/Rapid/Frost tint'leri bu degeri override edebilir.
- MobileCastleCombatConfig, ContinuousSiegeCycleData, EconomyFocusState, WaveClearRewardData, CastleYardPrepState ve ArcherSlotPosition `MobileCastleCombatAuthoring` baker'i tarafindan olusturulur. Continuous siege ve Fortify/Rally degerleri Inspector'daki ilgili alanlardan tune edilir.

## Spesifik Editor Setup Dosyalari
- Kaynak component'lari → `RESOURCE_COMPONENTS_EDITOR_SETUP.md`
- Fizik component'lari → `Physics/PHYSICS_EDITOR_SETUP.md`

## Fizik Component'lari (PhysicsComponents.cs)
- PhysicsBody ve CollisionRadius, ZombieAuthoring Baker'i tarafindan zombi prefab'ina eklenir.
- ZombieAuthoring Inspector'inda yeni alanlar:
  - **CollisionRadius**: 0.15 (varsayilan)
  - **PhysicsDamping**: 3.0 (varsayilan)
- Bu component'lar zombi-spesifik degil — gelecekte catapult, patlama parcasi vb. icin de kullanilabilir.
