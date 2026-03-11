# ECS Components - Mimari

## Genel Yapi
Tum component'lar `IComponentData` interface'ini implement eder (unmanaged struct).

## Component Gruplari

### ZombieComponents.cs
- **ZombieTag** — Zombi entity'lerini isaretler (tag component)
- **ZombieStats** — HP, hareket hizi, saldiri hasari, odul bilgileri
- **ZombieState** — Zombi durumu: Moving / Attacking / Dead
- **ReachedTarget** — Duvara ulasan zombilere eklenir
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
- **GameStateData** — Gold, XP, Level, click damage (singleton)
- **WaveStateData** — Dalga durumu, spawn timer, zombi sayilari (singleton)
- **ClickDamageRequest** — Tiklanma istegi (gecici component, ClickDamageHandler olusturur)

### PhysicsComponents.cs
- **PhysicsBody** — Velocity (float2), Force (float2), Mass, Damping. Her fizik cismi icin.
- **CollisionRadius** — Carpisma yaricapi (float). Circle-circle collision icin.

## Singleton Pattern
GameStateData ve WaveStateData tek entity uzerinde tutulur. SystemAPI.GetSingletonRW ile erisim saglanir.

## Veri Akisi
```
WaveSpawnSystem → ZombieStats/ZombieState/PhysicsBody/CollisionRadius olusturur
ApplyMovementForceSystem → PhysicsBody.Force yazar (hedefe dogru kuvvet)
PhysicsCollisionSystem → PhysicsBody.Velocity + LocalTransform yazar (carpisma)
IntegrateSystem → PhysicsBody.Force → Velocity → LocalTransform.Position
BoundarySystem → ZombieState gecirir (Moving→Attacking), pozisyon clamp
ZombieAttackSystem → WallSegment/GateComponent/CastleHP yazar
ZombieDeathSystem → ZombieState.Dead isaretler
DamageCleanupSystem → Dead entity'leri siler, GameStateData gunceller
```
