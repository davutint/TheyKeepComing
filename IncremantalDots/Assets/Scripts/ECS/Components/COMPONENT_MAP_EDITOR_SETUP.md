# ECS Component Haritasi - Editor Kurulum

## Gereksinimler
- Unity 6 LTS
- com.unity.entities paketi kurulu olmali
- DeadWalls.asmdef referanslari dogru olmali

## Notlar
- Component'lar dogrudan kullanilmaz, Authoring + Baker uzerinden entity'lere eklenir.
- Singleton component'lar (GameStateData, WaveStateData, Resource*) icin GameStateAuthoring kullanilir.
- Component degerlerini degistirmek icin ilgili Authoring component'inin Inspector degerlerini duzenleyin.

## Spesifik Editor Setup Dosyalari
- Kaynak component'lari → `RESOURCE_COMPONENTS_EDITOR_SETUP.md`
- Fizik component'lari → `Physics/PHYSICS_EDITOR_SETUP.md`

## Fizik Component'lari (PhysicsComponents.cs)
- PhysicsBody ve CollisionRadius, ZombieAuthoring Baker'i tarafindan zombi prefab'ina eklenir.
- ZombieAuthoring Inspector'inda yeni alanlar:
  - **CollisionRadius**: 0.15 (varsayilan)
  - **PhysicsDamping**: 3.0 (varsayilan)
- Bu component'lar zombi-spesifik degil — gelecekte catapult, patlama parcasi vb. icin de kullanilabilir.
