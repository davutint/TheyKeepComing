# Spatial Hash Grid - Mimari

## Amac
O(n^2) brute-force yerine yoğunluğa göre ayrılmış spatial broadphase/query sağlamak.
Collision ve archer targeting aynı cell size'a zorlanmaz.

## Nasil Calisiyor
1. Collision grid `BuildSpatialHashSystem` tarafından `0.35` cell size ile çift buffer kurulur.
2. Archer target grid `ArcherShootSystem` tarafından `2.0` cell size ile her frame kurulur.
3. Her zombinin pozisyonu aynı hash fonksiyonu ile ilgili grid hücresine eşlenir.
4. Collision yalnız aynı + 8 komşu hücreyi; targeting range halkalarını ve cell AABB prune'u kullanır.

## Hash Fonksiyonu
```
cell = (floor(x/cellSize), floor(y/cellSize))
key = cell.x * 73856093 XOR cell.y * 19349663
```
Prime-based hash — uniform dagilim saglar.

## Veri Yapisi
- `NativeParallelMultiHashMap<int, Entity>` — Allocator.Persistent
- Her frame Clear() + paralel HashJob ile rebuild
- Kapasite yetersizse ceilpow2(count*2) ile buyutulur

## Kullanan Sistemler
| Sistem | Kullanim |
|--------|----------|
| PhysicsCollisionSystem | 3x3 komsu hucre tarasi, circle-circle test |
| BoundarySystem | Durmuş komşu overlap sorgusu için collision read map |
| ArcherShootSystem | Persistent coarse target map + read-only nearest query + incoming load |

## Static Erisim
Collision/Boundary `BuildSpatialHashSystem.ReadMap` snapshot'ını `[ReadOnly]` kullanır.
Archer target map sistem-local owner'da kalır ve yalnız
`NativeParallelMultiHashMap.ReadOnly` alias olarak target job'a geçirilir.

Ayrıntılı hedefleme sözleşmesi:
`Assets/Scripts/ECS/Systems/ARCHER_TARGETING_ARCHITECTURE.md`.
