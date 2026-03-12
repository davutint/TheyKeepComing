# ECS Component Haritasi

## Genel Yapi
Tum component'lar `IComponentData` interface'ini implement eder (unmanaged struct).

## Component Gruplari

### ZombieComponents.cs
- **ZombieTag** — Zombi entity'lerini isaretler (tag component)
- **ZombieStats** — HP, hareket hizi, saldiri hasari, XP odulu
- **ZombieState** — Zombi durumu: Moving / Attacking / Dead / Queued
- ~~**ReachedTarget**~~ — Kaldirildi (olu kod, hicbir yerde kullanilmiyordu)
- ~~**ZombieStopOffset**~~ — Kaldirildi (her zaman 0'di, gereksiz bulundu)

### CastleComponents.cs
- **WallSegment** — Duvar HP bilgisi
- **GateComponent** — Kapi HP bilgisi
- **CastleHP** — Kale ana HP (singleton)
- **WallXPosition** — Duvarin X koordinati (zombiler buraya yurur)

### ArcherComponents.cs
- **ArcherUnit** — Okcunun atis hizi, hasar, menzil bilgileri
- **ArrowProjectile** — Okun hizi, hasari, hedef entity referansi
- **ArrowTag** — Ok entity'lerini isaretler

### GameStateComponents.cs
- **GameStateData** — XP, Level (singleton)
- **WaveStateData** — Dalga durumu, spawn timer, zombi sayilari (singleton)
- ~~**Gold**~~ — Kaldirildi (GDD v3.0'da yok)
- ~~**ClickDamage**~~ — Kaldirildi (GDD v3.0'da yok)
- ~~**ClickDamageRequest**~~ — Kaldirildi (ClickDamageSystem ile birlikte silindi)

### ResourceComponents.cs (M1.1)
→ Detay: `RESOURCE_COMPONENTS_ARCHITECTURE.md`

### PopulationComponents.cs (M1.2)
→ Detay: `POPULATION_STATE_ARCHITECTURE.md`

### BuildingComponents.cs (M1.3)
→ Detay: `BUILDING_COMPONENTS_ARCHITECTURE.md`

### PhysicsComponents.cs
- **PhysicsBody** — Velocity (float2), Force (float2), Mass, Damping. Her fizik cismi icin.
- **CollisionRadius** — Carpisma yaricapi (float). Circle-circle collision icin.

## Singleton Pattern
GameStateData, WaveStateData, Resource* ve PopulationState component'lari tek entity uzerinde tutulur (GameStateAuthoring). SystemAPI.GetSingletonRW ile erisim saglanir.

## Per-Entity Component'lar (Binalar)
- **BuildingData** — Bina tipi, seviyesi, grid pozisyonu. Her bina entity'sinde bulunur.
- **ResourceProducer** — Kaynak ureten binalar (Oduncu, Ciftlik vs.) — tip, hiz, isci sayisi.
- **PopulationProvider** — Nufus kapasitesi artiran binalar (Ev).
- **BuildingFoodCost** — Yemek tuketen binalar (Ev).

## Veri Akisi
```
PopulationTickSystem → PopulationState.Idle hesaplar + ResourceConsumptionRate.FoodPerMin gunceller
ResourceTickSystem → ResourceAccumulator + ResourceData gunceller (net hiz * dt → accumulator → int)
WaveSpawnSystem → ZombieStats/ZombieState/PhysicsBody/CollisionRadius olusturur (wave stats uygulanir)
ApplyMovementForceSystem → PhysicsBody.Force yazar (hedefe dogru kuvvet)
PhysicsCollisionSystem → PhysicsBody.Velocity + LocalTransform yazar (carpisma)
IntegrateSystem → PhysicsBody.Force → Velocity → LocalTransform.Position
BoundarySystem → ZombieState gecirir (Moving→Attacking/Queued), pozisyon clamp
ZombieAttackTimerSystem → NativeQueue'ya hasar yazar
DamageApplySystem → Wall/Gate/Castle HP yazar (tek sync point)
ZombieDeathSystem → ZombieState.Dead isaretler
DamageCleanupSystem → Dead entity'leri siler, GameStateData.XP gunceller
```
