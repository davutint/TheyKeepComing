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
BoundarySystem              → Duvar bariyeri, state transitions, Y siniri
```

## Carpisma Response
Iki daire cakistiginda:
1. **Pozisyon duzeltme**: Her iki cisim overlap/2 kadar birbirinden itilir
2. **Velocity impulse**: Momentum transfer — arkadaki ondekine hizini aktarir
3. **Zincir reaksiyon**: Kuvvet crowd boyunca yayilir (her frame bir hop)

## ProjectDawn Entegrasyonu
- CrowdSteering → AgentBody.Force (yon hesabi, AYNI kaliyor)
- AgentBody.IsStopped = true → PD locomotion devre disi
- Pozisyonu tamamen IntegrateSystem yaziyor
- ApplyMovementForceSystem, PD Force varsa onu kullanir, yoksa Destination'dan yon hesaplar

## Performans (100K zombi tahmini)
| Sistem | Karmasiklik | Tahmini |
|--------|-------------|---------|
| ApplyMovementForce | O(n) parallel | ~0.1ms |
| BuildSpatialHash | O(n) parallel | ~0.3ms |
| PhysicsCollision | O(n*k) parallel, k~26 | ~3-5ms |
| Integrate | O(n) parallel | ~0.1ms |
| Boundary | O(n) parallel | ~0.1ms |
| **Toplam** | | **~4-6ms** |
