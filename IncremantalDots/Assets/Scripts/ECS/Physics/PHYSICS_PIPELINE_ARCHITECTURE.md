# Custom 2D Circle Physics - Pipeline Mimarisi

## Amac
100K+ zombi icin gercek zamanli fizik simulasyonu. Her zombi pozisyon + hiz + kuvvet tutan bir fizik cismi. Carpisma momentum transfer eder, kuvvet zincir halinde crowd boyunca yayilir.

## Pipeline Sirasi (Her Frame)
```
ApplyMovementForceSystem    → Zombiye hedefe dogru kuvvet uygula
        |
BuildSpatialHashSystem      → Tum pozisyonlari hash grid'e yaz
        |
PhysicsCollisionSystem      → Circle-circle carpisma + momentum transfer
        |
IntegrateSystem             → velocity += force*dt, position += velocity*dt
        |
BoundarySystem              → Duvar bariyeri, domino attacking, state transitions, Y siniri
```

## Carpisma Response
Iki daire cakistiginda:
1. **Pozisyon duzeltme**: Her iki cisim overlap/2 kadar birbirinden itilir
2. **Velocity impulse (momentum)**: Momentum transfer — arkadaki ondekine hizini aktarir
3. **Velocity impulse (overlap)**: `body.Velocity += normal * overlap * 2.0f` — overlap kaliciligi azaltir, cisimleri daha hizli ayirir
4. **Zincir reaksiyon**: Kuvvet crowd boyunca yayilir (her frame bir hop)

## ProjectDawn Entegrasyonu
- CrowdSteering → AgentBody.Force (yon hesabi, AYNI kaliyor)
- AgentBody.IsStopped = true → PD locomotion devre disi
- Pozisyonu tamamen IntegrateSystem yaziyor
- ApplyMovementForceSystem, PD Force varsa onu kullanir, yoksa Destination'dan yon hesaplar

## Domino Queuing (BoundarySystem)
Moving zombi, Attacking/Queued bir komsusuna cakisiyorsa **Queued** state'e gecer (saldirmaz, yuruyus animasyonu oynar, sadece bekler).
- Spatial hash uzerinden 3x3 hucre taranir (ayni grid PhysicsCollisionSystem ile paylasiliyor)
- Her frame sadece bir katman gecer → zincir halinde yayilir
- Queued zombi her frame komsu kontrol eder: blocker gitti → Moving'e doner
- Duvar onunde ic ice girme problemi cozulur — ondeki durdu, arkadaki de durur
- State akisi: Moving → Queued (domino) → Attacking (duvara ulasinca) veya Moving (blocker gidince)
- ZombieStopOffset kaldirildi — duvar kontrolu dogrudan `pos.x <= WallX` olarak basitlesti
- HasComponent kontrolleri kaldirildi (spatial hash sadece gecerli entity icerir, gereksiz guard'lar performans dusuruyordu)

## Performans (100K zombi tahmini)
| Sistem | Karmasiklik | Tahmini |
|--------|-------------|---------|
| ApplyMovementForce | O(n) parallel | ~0.1ms |
| BuildSpatialHash | O(n) parallel | ~0.3ms |
| PhysicsCollision | O(n*k) parallel, k~26 | ~3-5ms |
| Integrate | O(n) parallel | ~0.1ms |
| Boundary | O(n) parallel | ~0.1ms |
| **Toplam** | | **~4-6ms** |
