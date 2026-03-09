# ECS Systems - Mimari

## Calisma Sirasi (UpdateOrder)
```
SimulationSystemGroup icinde:
 1. WaveSpawnSystem              — Zombi spawn + PhysicsBody set
 2. ZombieNavigationSystem       — AgentBody.Destination sync
 3. ApplyMovementForceSystem  *  — Hedefe dogru kuvvet → PhysicsBody.Force
 4. BuildSpatialHashSystem    *  — Pozisyonlari hash grid'e yaz
 5. PhysicsCollisionSystem    *  — Circle-circle carpisma + momentum transfer
 6. IntegrateSystem           *  — velocity += force*dt, pos += vel*dt, damping
 7. BoundarySystem            *  — Duvar bariyeri, state transition, Y siniri
 8. ZombieAttackSystem           — Duvar/kapi/kale hasar
 9. ArcherShootSystem            — Spatial hash ile en yakin zombi, ok firlat
10. ArrowMoveSystem              — Ok hareket
11. ArrowHitSystem               — Ok isabet + hasar
12. ClickDamageSystem         *  — Spatial hash ile click damage
13. ZombieDeathSystem            — HP<=0 → Dead state
14. ZombieAnimationStateSystem   — Sprite animasyon guncelle
15. DamageCleanupSystem          — DeathTimer, gold/XP, entity sil

* = Yeni fizik sistemleri
```

## System Detaylari

### WaveSpawnSystem
- WaveStateData singleton'dan dalga bilgilerini okur
- SpawnTimer ile periyodik zombi spawn (batch 20)
- AgentBody.IsStopped = true (PD locomotion devre disi)
- PhysicsBody + CollisionRadius component'lari eklenir

### ZombieNavigationSystem
- AgentBody.Destination'i guncel tutar (CrowdSteering Force hesabi icin)
- IsStopped her zaman true — PD pozisyon yazmaz
- State transition ve duvar bariyeri BoundarySystem'e tasindi

### ApplyMovementForceSystem (FIZIK)
- Moving zombilere hedefe dogru kuvvet uygular
- Oncelik: PD Force > Destination yonu > fallback (-1,0)
- Attacking/Dead → kuvvet sifir

### BuildSpatialHashSystem (FIZIK)
- NativeParallelMultiHashMap<int, Entity> rebuild eder
- Paralel HashJob ile O(n) performans
- Static field uzerinden diger sistemler erisiyor

### PhysicsCollisionSystem (FIZIK)
- Spatial hash ile broadphase (3x3 komsu hucre)
- Circle-circle overlap test + pozisyon duzeltme + velocity impulse
- Paralel: her entity sadece kendini gunceller

### IntegrateSystem (FIZIK)
- Semi-implicit Euler: velocity += force/mass*dt, pos += vel*dt
- Damping: velocity *= (1 - damping*dt)
- Force sifirlanir (sonraki frame icin)

### BoundarySystem (FIZIK)
- Moving → Attacking: pos.x <= wallX + stopOffset
- Duvar bariyeri: pos.x clamp
- Dead: velocity + force sifir
- Y siniri: -15 ile +15 arasi

### ZombieAttackSystem
- Attacking state'deki zombiler duvar/kapi/kale'ye hasar verir
- Oncelik: Wall → Gate → Castle
- CastleHP 0 olunca GameOver isaretler

### ArcherShootSystem
- Spatial hash ile en yakin zombiyi bulur (brute-force fallback var)
- Fire timer'a gore ok spawn eder

### ArrowMoveSystem
- Oklari hedeflerine dogru hareket ettirir
- Hedef olmusse oku yok eder

### ArrowHitSystem
- ComponentLookup ile hedef kontrolu (Burst-uyumlu)
- Mesafe < 0.5 → hasar uygula, oku sil

### ClickDamageSystem (YENI)
- ClickDamageRequest entity'lerini isler
- Spatial hash ile en yakin zombiyi bulur
- Hasar uygular, request entity'sini siler

### ZombieDeathSystem
- HP <= 0 olan zombileri Dead state'e gecirir

### ZombieAnimationStateSystem
- State'e gore sprite animasyon satirini degistirir
- Dead → DeathTimer ekler

### DamageCleanupSystem
- DeathTimer geri sayar
- Timer bitince: gold/XP + entity sil
- Level up kontrolu
