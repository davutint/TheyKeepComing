# Spatial Hash Grid - Mimari

## Amac
O(n^2) brute-force yerine O(n*k) broadphase carpisma tespiti. k = ortalama komsu sayisi (~26).

## Nasil Calisiyor
1. Dunya 2D grid'e bolunur (cell size = 0.6, zombi capi 0.30 ile daha iyi eslesir)
2. Her zombinin pozisyonu hash fonksiyonu ile bir hucreye eslesir
3. Carpisma tespitinde sadece ayni + 8 komsu hucre kontrol edilir

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
| ArcherShootSystem | Artik spatial hash kullanmiyor — brute-force SystemAPI.Query ile Burst derlemeli |
| ClickDamageSystem | Click pozisyonu etrafindaki hucreleri tara |

## Static Erisim
`BuildSpatialHashSystem.SpatialMap` static field uzerinden tum sistemler erisebirlir.
Bu field job'lara [ReadOnly] olarak kopyalanir — thread-safe.
