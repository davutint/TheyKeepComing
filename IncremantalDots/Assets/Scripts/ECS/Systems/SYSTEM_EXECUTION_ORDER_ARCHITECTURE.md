# System Calisma Sirasi ve Sync Point Stratejisi

## Calisma Sirasi (UpdateOrder)
```
SimulationSystemGroup icinde:
-3. BuildingProductionSystem    — Bina uretim hizlarini topla → singleton'a yaz
-2½.BuildingPopulationSystem   — Kapasite + bina yemek tuketimi → singleton'a yaz
-2. ArrowProductionSystem    *  — Fletcher ok uretimi + WoodPerMin'e ekleme
-1. PopulationTickSystem     *  — Nufus hesapla + FoodPerMin += nufus kismi
-½. BarracksTrainingSystem      — Kisla okcu egitimi (idle nufus + kaynak → okcu spawn)
 0. ResourceTickSystem        *  — Kaynak uretim/tuketim tick (net hiz * dt → accumulator → int)
 1. WaveSpawnSystem              — Zombi spawn + wave stats uygula (ZombieStats set)
 2. ArcherShootSystem         *  — Burst + brute-force query, physics oncesi (1-frame-old pozisyon)
 2b.CatapultShootSystem       *  — Mancinik atis (brute-force hedefleme + tas tuketimi + mermi spawn)
 3. ApplyMovementForceSystem  *  — Hedefe dogru kuvvet → PhysicsBody.Force (WallX singleton ile yon)
 4. BuildSpatialHashSystem    *  — Double-buffered spatial hash (ReadMap/WriteMap)
 5. PhysicsCollisionSystem    *  — Circle-circle carpisma + momentum transfer
 6. IntegrateSystem           *  — velocity += force*dt, pos += vel*dt, damping
 7. BoundarySystem            *  — Duvar bariyeri, state transition, Y siniri
 8. ZombieAttackTimerSystem      — IJobEntity: attack timer + NativeQueue'ya hasar yaz
 9. ArrowMoveSystem           *  — IJobEntity: ok hareket
 9b.CatapultProjectileMoveSystem * — IJobEntity: parabolik mermi hareketi
10. ArrowHitSystem            *  — IJobEntity + ECB: ok isabet + hasar
10b.CatapultProjectileHitSystem  — Main thread: AoE hasar (spatial hash + static field erisimi)
11. ZombieDeathSystem         *  — IJobEntity: HP<=0 → Dead state
12. ZombieAnimationStateSystem*  — IJobEntity + ECB: sprite animasyon guncelle
13. DamageApplySystem            — TEK SYNC POINT: damage queue drain + singleton yazma
14. DamageCleanupSystem          — DeathTimer, XP, entity sil

PresentationSystemGroup icinde:
15. SpriteAnimationSystem     *  — IJobEntity: UV rect hesapla

* = IJobEntity (parallel job olarak calisir, main thread bloklamaz)
```

## Sync Point Stratejisi
- Sistem 2-12 arasi main thread sadece job dispatch yapar (~0.5ms)
- **Tek sync point: DamageApplySystem** (sistem 13) — tum physics + attack job'lari tamamlanir
- WaveSpawnSystem (sistem 1) sequential ama frame basinda → onceki frame'in job'lari zaten bitmis

## System Detaylari

### BuildingProductionSystem (M1.4)
→ Detay: `BUILDING_PRODUCTION_SYSTEM_ARCHITECTURE.md`

### BuildingPopulationSystem (M1.5)
→ Detay: `BUILDING_POPULATION_SYSTEM_ARCHITECTURE.md`

### ArrowProductionSystem (M1.6)
- Fletcher binalarindaki isciler ok uretir
- ArrowSupply singleton uzerinde accumulator pattern
- WoodPerMin'e Fletcher ahsap tuketimini ekler (BuildingPopulationSystem sifirladiktan sonra)
- `[BurstCompile]` destekli
- `[UpdateAfter(typeof(BuildingPopulationSystem))]`
- `[UpdateBefore(typeof(PopulationTickSystem))]`
→ Detay: `ARROW_PRODUCTION_SYSTEM_ARCHITECTURE.md`

### PopulationTickSystem (M1.2)
→ Detay: `POPULATION_TICK_SYSTEM_ARCHITECTURE.md`

### BarracksTrainingSystem (M1.6)
- Kisla okcu egitim sistemi
- Idle nufus + yeterli kaynak varsa egitim baslatir
- Timer bitince okcu spawn eder (ArcherPrefabData + ECB)
- PopulationState.Archers sayacini arttirir
- BurstCompile YOK (ECB + singleton RW)
- `[UpdateAfter(typeof(PopulationTickSystem))]`
- `[UpdateBefore(typeof(ResourceTickSystem))]`
→ Detay: `BARRACKS_TRAINING_SYSTEM_ARCHITECTURE.md`

### ResourceTickSystem (M1.1)
→ Detay: `RESOURCE_TICK_SYSTEM_ARCHITECTURE.md`

### WaveSpawnSystem
- WaveStateData singleton'dan dalga bilgilerini okur
- SpawnTimer ile periyodik zombi spawn (batch 20)
- **Wave stats uygulanir:** Her zombiye wave'e ozel HP, Speed, Damage degerleri yazilir
- PhysicsBody + CollisionRadius component'lari eklenir
- StressTestMode: `false` default (Inspector'dan degistirilebilir)

### ArcherShootSystem
- **Physics oncesine tasindi** — `[UpdateBefore(typeof(ApplyMovementForceSystem))]`
- 1-frame-old zombie pozisyonlari ile hedefleme (ok ucus suresi >> 1 frame)
- `[BurstCompile]` ile struct ve OnUpdate
- Brute-force SystemAPI.Query ile en yakin zombiyi bulur (~60K mesafe kontrolu, Burst ile spatial hash'ten hizli)
- `math.distancesq` kullanir (sqrt maliyeti yok)
- `EndSimulationEntityCommandBufferSystem` ECB kullanir
- Fire timer'a gore ok spawn eder

### CatapultShootSystem (M2.1)
- Mancinik atis sistemi — ArcherShootSystem pattern'i
- Brute-force en yakin zombie bulur, parabolik mermi spawn eder
- Tas tuketimi (ResourceData.Stone -= StoneCostPerShot)
- `[BurstCompile]` destekli
- `[UpdateAfter(typeof(WaveSpawnSystem))]`
- `[UpdateBefore(typeof(ApplyMovementForceSystem))]`
→ Detay: `CATAPULT_SYSTEM_ARCHITECTURE.md`

### CatapultProjectileMoveSystem (M2.1)
- IJobEntity: parabolik mermi hareketi
- `lerp(Start,Target,t) + ArcHeight*4*t*(1-t)` ile parabol
- `[BurstCompile]` destekli
- `[UpdateAfter(typeof(ZombieAttackTimerSystem))]`
→ Detay: `CATAPULT_SYSTEM_ARCHITECTURE.md`

### CatapultProjectileHitSystem (M2.1)
- Main thread (BurstCompile YOK — static field erisimi)
- Spatial hash (ReadMap) uzerinden AoE tarama
- Yaricap icindeki zombilere direkt HP dusurme
- `[UpdateAfter(typeof(CatapultProjectileMoveSystem))]`
→ Detay: `CATAPULT_SYSTEM_ARCHITECTURE.md`

### ApplyMovementForceSystem (FIZIK)
- Moving zombilere hedefe dogru kuvvet uygular
- WallX singleton'dan duvar pozisyonunu okur, zombiyi duvara dogru yonlendirir
- Attacking/Dead/Queued → kuvvet sifir

### BuildSpatialHashSystem (FIZIK — DOUBLE BUFFER)
- **ReadMap**: onceki frame'in verisi, consumer'lar (Collision, Boundary) okur
- **WriteMap**: bu frame'de hash job doldurur
- Her frame swap yapilir, .Complete() YOK — main thread bloklanmaz
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
- Timer bitince: XP + entity sil
- Level up kontrolu

### SpriteAnimationSystem (PRESENTATION)
→ Detay: `SPRITE_ANIMATION_ARCHITECTURE.md`

## Kaldirilan Sistemler
- ~~**ClickDamageSystem**~~ — GDD v3.0'da yok, tamamen kaldirildi (M0 Bug Fix)
