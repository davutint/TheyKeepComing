# Authoring + Baker - Mimari

## Genel Yapi
Her authoring MonoBehaviour, ECS entity'ye donusturulmek uzere Sub Scene icine yerlestirilen GameObject'lere eklenir.
Baker class'lari authoring degerlerini okuyarak entity'lere IComponentData ekler.

## Dosyalar

### ZombieAuthoring.cs
- Zombi prefab'ina eklenir
- ZombieTag, ZombieStats, ZombieState component'larini bake eder

### CastleAuthoring.cs
- Kale/duvar/kapi GameObject'ine eklenir (Sub Scene icinde tek instance)
- WallSegment, GateComponent, CastleHP, WallXPosition bake eder

### ArcherAuthoring.cs
- Okcu prefab'ina eklenir
- ArcherUnit component'ini bake eder

### ArrowAuthoring.cs
- Ok prefab'ina eklenir
- ArrowTag, ArrowProjectile bake eder

### GameStateAuthoring.cs
- Oyun durumu singleton entity'si olusturur
- GameStateData, WaveStateData bake eder

### WaveConfigAuthoring.cs
- Prefab referanslarini tutar (Zombie, Arrow)
- ZombiePrefabData, ArrowPrefabData bake eder
- Sub Scene icinde GameStateAuthoring ile ayni GameObject'e eklenebilir

## Prefab Referans Akisi
WaveConfigAuthoring.ZombiePrefab → Baker.GetEntity() → ZombiePrefabData.ZombiePrefab (Entity)
WaveConfigAuthoring.ArrowPrefab → Baker.GetEntity() → ArrowPrefabData.ArrowPrefab (Entity)
