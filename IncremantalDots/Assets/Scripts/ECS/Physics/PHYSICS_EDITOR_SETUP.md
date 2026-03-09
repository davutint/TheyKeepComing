# Custom 2D Circle Physics - Editor Kurulum

## Gereksinimler
- Unity 6 LTS
- com.unity.entities paketi
- com.projectdawn.navigation paketi (CrowdSteering icin)
- DeadWalls.asmdef referanslari dogru olmali

## Authoring Ayarlari (ZombieAuthoring Inspector)
| Alan | Varsayilan | Aciklama |
|------|-----------|----------|
| CollisionRadius | 0.15 | Carpisma yaricapi (zombi scale 0.3 icin uygun) |
| PhysicsDamping | 3.0 | Surturme katsayisi (yuksek = hizli yavaslar) |

## Tuning Rehberi
- **Damping cok yuksek** (>5): Zombiler agir hareket eder, itme zayif
- **Damping cok dusuk** (<1): Zombiler kayar, duramaz
- **CollisionRadius cok buyuk** (>0.2): Zombiler birbirinden cok uzak durur
- **CollisionRadius cok kucuk** (<0.1): Zombiler iç ice gecer
- **MoveForce carpani** (ApplyMovementForceSystem icinde `speed * 10`): Degistirmek icin kodu duzenle

## Spatial Hash Cell Size
- `SpatialHashGrid.cs` icinde `DefaultCellSize = 0.5f`
- 100K'da hucre basina ~26 zombi, 9 hucre kontrolu = ~234 karsilastirma
- Sikissa olursa 0.75'e cikar (daha az hucre ama daha fazla karsilastirma)

## Debug
- Entity Debugger'da PhysicsBody component'ini incele:
  - Velocity: anlik hiz (float2)
  - Force: bu frame biriken kuvvet (her frame sifirlanir)
- Zombiler duvardan geciyorsa: BoundarySystem WallX degerini kontrol et
- Zombiler birbirine yapisiyor: Damping veya CollisionRadius ayarla

## Play Mode Test
1. ZombieAuthoring prefab'inda CollisionRadius ve PhysicsDamping degerlerinin gorundugunden emin ol
2. Play'e bas — zombiler fiziksel olarak birbirini itmeli
3. Duvar onunde organik yigilma olusmali
4. Zombi olunce bosluk dogal dolmali
