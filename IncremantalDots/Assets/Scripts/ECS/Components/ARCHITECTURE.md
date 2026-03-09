# ECS Components - Mimari

## Genel Yapi
Tum component'lar `IComponentData` interface'ini implement eder (unmanaged struct).

## Component Gruplari

### ZombieComponents.cs
- **ZombieTag** — Zombi entity'lerini isaretler (tag component)
- **ZombieStats** — HP, hareket hizi, saldiri hasari, odul bilgileri
- **ZombieState** — Zombi durumu: Moving / Attacking / Dead
- **ReachedTarget** — Duvara ulasan zombilere eklenir

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
- **ClickDamageRequest** — Tiklanma istegi (gecici component)

## Singleton Pattern
GameStateData ve WaveStateData tek entity uzerinde tutulur. SystemAPI.GetSingletonRW ile erisim saglanir.

## Veri Akisi
```
WaveSpawnSystem → ZombieStats/ZombieState olusturur
ZombieMoveSystem → ZombieStats.MoveSpeed okur, LocalTransform yazar
ZombieAttackSystem → ReachedTarget ekler, WallSegment/GateComponent/CastleHP yazar
ZombieDeathSystem → ZombieState.Dead isaretler
DamageCleanupSystem → Dead entity'leri siler, GameStateData gunceller
```
