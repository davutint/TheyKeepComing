# ECS Systems - Mimari

## Calisma Sirasi (UpdateOrder)
```
SimulationSystemGroup icinde:
 1. WaveSpawnSystem              — Zombi spawn + PhysicsBody set
 2. ArcherShootSystem         *  — Burst + brute-force query, physics oncesi (1-frame-old pozisyon)
 3. ApplyMovementForceSystem  *  — Hedefe dogru kuvvet → PhysicsBody.Force (WallX singleton ile yon)
 4. BuildSpatialHashSystem    *  — Double-buffered spatial hash (ReadMap/WriteMap)
 5. PhysicsCollisionSystem    *  — Circle-circle carpisma + momentum transfer
 6. IntegrateSystem           *  — velocity += force*dt, pos += vel*dt, damping
 7. BoundarySystem            *  — Duvar bariyeri, state transition, Y siniri
 8. ZombieAttackTimerSystem      — IJobEntity: attack timer + NativeQueue'ya hasar yaz
 9. ArrowMoveSystem           *  — IJobEntity: ok hareket
10. ArrowHitSystem            *  — IJobEntity + ECB: ok isabet + hasar
11. ClickDamageSystem         *  — Spatial hash ile click damage (RequireForUpdate ile skip)
12. ZombieDeathSystem         *  — IJobEntity: HP<=0 → Dead state
13. ZombieAnimationStateSystem*  — IJobEntity + ECB: sprite animasyon guncelle
14. DamageApplySystem            — TEK SYNC POINT: damage queue drain + singleton yazma
15. DamageCleanupSystem          — DeathTimer, gold/XP, entity sil

PresentationSystemGroup icinde:
16. SpriteAnimationSystem     *  — IJobEntity: UV rect hesapla

* = IJobEntity (parallel job olarak calisir, main thread bloklamaz)
```

## Sync Point Stratejisi
- Sistem 2-13 arasi main thread sadece job dispatch yapar (~0.5ms)
- **Tek sync point: DamageApplySystem** (sistem 14) — tum physics + attack job'lari tamamlanir
- ClickDamageSystem: RequireForUpdate<ClickDamageRequest> ile %98 frame skip → pratik sync yok
- WaveSpawnSystem (sistem 1) sequential ama frame basinda → onceki frame'in job'lari zaten bitmis

## System Detaylari

### WaveSpawnSystem
- WaveStateData singleton'dan dalga bilgilerini okur
- SpawnTimer ile periyodik zombi spawn (batch 20)
- PhysicsBody + CollisionRadius component'lari eklenir

### ArcherShootSystem
- **Physics oncesine tasindi** — `[UpdateBefore(typeof(ApplyMovementForceSystem))]`
- 1-frame-old zombie pozisyonlari ile hedefleme (ok ucus suresi >> 1 frame)
- `[BurstCompile]` ile struct ve OnUpdate
- Brute-force SystemAPI.Query ile en yakin zombiyi bulur (~60K mesafe kontrolu, Burst ile spatial hash'ten hizli)
- `math.distancesq` kullanir (sqrt maliyeti yok)
- `EndSimulationEntityCommandBufferSystem` ECB kullanir
- Fire timer'a gore ok spawn eder

### ApplyMovementForceSystem (FIZIK)
- Moving zombilere hedefe dogru kuvvet uygular
- WallX singleton'dan duvar pozisyonunu okur, zombiyi duvara dogru yonlendirir
- Attacking/Dead/Queued → kuvvet sifir

### BuildSpatialHashSystem (FIZIK — DOUBLE BUFFER)
- **ReadMap**: onceki frame'in verisi, consumer'lar okur
- **WriteMap**: bu frame'de hash job doldurur
- Her frame swap yapilir, .Complete() YOKU — main thread bloklanmaz
- ClearMapJob + HashJob dependency chain ile schedule edilir
- Static field uzerinden diger sistemler ReadMap'e erisiyor

### PhysicsCollisionSystem (FIZIK)
- Spatial hash (ReadMap) ile broadphase (3x3 komsu hucre)
- Circle-circle overlap test + pozisyon duzeltme + velocity impulse
- Paralel: her entity sadece kendini gunceller

### IntegrateSystem (FIZIK)
- Semi-implicit Euler: velocity += force/mass*dt, pos += vel*dt
- Damping: velocity *= (1 - damping*dt)
- Force sifirlanir (sonraki frame icin)

### BoundarySystem (FIZIK)
- Moving → Attacking: pos.x <= wallX
- Domino queuing: Moving → Queued (komsuda Attacking/Queued varsa)
- Queued → Moving: blocker gidince
- Duvar bariyeri + Y siniri

### ZombieAttackTimerSystem
- **IJobEntity**: Attacking state'deki zombilerin timer'ini isler
- Timer dolunca hasar `NativeQueue<float>.ParallelWriter`'a yazilir
- Main thread beklemez — hasar DamageApplySystem'de uygulanir
- Static field `DamageQueue` uzerinden DamageApplySystem erisir

### ArrowMoveSystem
- **IJobEntity**: Oklari hedeflerine dogru hareket ettirir
- ComponentLookup<LocalTransform> ile hedef pozisyon okur
- ECB.ParallelWriter ile hedefi olmayan oklari siler

### ArrowHitSystem
- **IJobEntity + ECB.ParallelWriter**: Ok isabet kontrolu
- ComponentLookup ile hedef kontrolu (Burst-uyumlu)
- Mesafe < 0.5 → hasar uygula, oku sil

### ClickDamageSystem
- **RequireForUpdate<ClickDamageRequest>**: Click yoksa OnUpdate cagrilmaz → sync yok
- Spatial hash (ReadMap) ile en yakin zombiyi bulur
- Hasar uygular, request entity'sini siler

### ZombieDeathSystem
- **IJobEntity**: HP <= 0 olan zombileri Dead state'e gecirir

### ZombieAnimationStateSystem
- **IJobEntity + ECB.ParallelWriter**: State'e gore sprite animasyon satirini degistirir
- Dead → DeathTimer ekler (ECB ile)

### DamageApplySystem (TEK SYNC POINT)
- `state.CompleteDependency()` cagrilir — tum pending job'lar tamamlanir
- ZombieAttackTimerSystem'in DamageQueue'sunu drain eder
- Hasar: Wall → Gate → Castle onceligi
- CastleHP <= 0 → GameOver

### DamageCleanupSystem
- DeathTimer geri sayar
- Timer bitince: gold/XP + entity sil
- Level up kontrolu

### SpriteAnimationSystem (PRESENTATION)
- **IJobEntity**: Sprite sheet UV rect hesaplar
- Timer ilerletir, frame degistirir
- PresentationSystemGroup'ta calisir
