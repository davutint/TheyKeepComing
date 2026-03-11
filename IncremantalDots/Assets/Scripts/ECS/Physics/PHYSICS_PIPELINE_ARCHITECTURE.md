# Custom 2D Circle Physics - Pipeline Mimarisi

## Amac
100K+ zombi icin gercek zamanli fizik simulasyonu. Her zombi pozisyon + hiz + kuvvet tutan bir fizik cismi. Carpisma momentum transfer eder, kuvvet zincir halinde crowd boyunca yayilir.

## Pipeline Sirasi (Her Frame)
```
ApplyMovementForceSystem    → Zombiye hedefe dogru kuvvet uygula
        |
BuildSpatialHashSystem      → Double-buffered: WriteMap'e hash yaz, consumer'lar ReadMap okur
        |
PhysicsCollisionSystem      → Circle-circle carpisma + momentum transfer (ReadMap kullanir)
        |
IntegrateSystem             → velocity += force*dt, position += velocity*dt
        |
BoundarySystem              → Duvar bariyeri, domino attacking, state transitions (ReadMap kullanir)
```

## Double Buffer (BuildSpatialHashSystem)
```
ReadMap  ← onceki frame'in verisi, consumer'lar (Collision, Boundary, ClickDamage) okur
WriteMap ← bu frame'de ClearMapJob + HashJob doldurur
Her frame: swap(ReadMap, WriteMap) → .Complete() YOK → main thread bloklanmaz
```
- `state.Dependency = hashJobHandle` → ECS component dependency zinciri korunur
- 1-frame-old spatial data: max pozisyon hatasi %14 radius, soft correction halleder
- Nadir capacity resize: count > Capacity/2 ise rebuild (oyun boyunca 5-10 kez)

## Carpisma Response
Iki daire cakistiginda:
1. **Pozisyon duzeltme**: Her iki cisim overlap/2 kadar birbirinden itilir
2. **Velocity impulse (momentum)**: Momentum transfer — arkadaki ondekine hizini aktarir
3. **Velocity impulse (overlap)**: `body.Velocity += normal * overlap * 2.0f` — overlap kaliciligi azaltir
4. **Zincir reaksiyon**: Kuvvet crowd boyunca yayilir (her frame bir hop)

## ProjectDawn Entegrasyonu
- CrowdSteering → AgentBody.Force (yon hesabi, AYNI kaliyor)
- AgentBody.IsStopped = true → PD locomotion devre disi
- Pozisyonu tamamen IntegrateSystem yaziyor
- ApplyMovementForceSystem, PD Force varsa onu kullanir, yoksa Destination'dan yon hesaplar

## Domino Queuing (BoundarySystem)
Moving zombi, Attacking/Queued bir komsusuna cakisiyorsa **Queued** state'e gecer (saldirmaz, yuruyus animasyonu oynar, sadece bekler).
- Spatial hash (ReadMap) uzerinden 3x3 hucre taranir
- Her frame sadece bir katman gecer → zincir halinde yayilir
- Queued zombi her frame komsu kontrol eder: blocker gitti → Moving'e doner
- Duvar onunde ic ice girme problemi cozulur

## Sync Point Stratejisi
- BuildSpatialHashSystem: .Complete() YOK (double buffer sayesinde)
- Physics pipeline tamamen job chain olarak calisir
- Tek sync point: DamageApplySystem (SimulationSystemGroup sonunda)
- Main thread sadece job dispatch yapar → ~0.5ms

## Performans (100K zombi tahmini)
| Sistem | Karmasiklik | Tahmini |
|--------|-------------|---------|
| ApplyMovementForce | O(n) parallel | ~0.1ms |
| BuildSpatialHash | O(n) parallel + ClearMapJob | ~0.3ms |
| PhysicsCollision | O(n*k) parallel, k~26 | ~3-5ms |
| Integrate | O(n) parallel | ~0.1ms |
| Boundary | O(n) parallel | ~0.1ms |
| **Toplam** | | **~4-6ms** |
