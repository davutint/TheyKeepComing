# ECS Component Haritasi

## Genel Yapi

Tum component'lar unmanaged ECS struct olarak tutulur. Davranis sistemlerde, veri component'larda kalir.

## ZombieComponents.cs

- `ZombieTag`: zombi entity'lerini isaretler.
- `ZombieStats`: HP, hareket hizi, saldiri hasari, cooldown ve XP odulu.
- `ZombieSlow`: Frost ok etkisi icin enableable slow duration + speed multiplier.
- `ZombieState`: zombi durumu: `Moving`, `Attacking`, `Dead`, `Queued`.
- `DeathTimer`: olum animasyonu bitene kadar entity silmeyi geciktirir.

## SpriteComponents.cs

- `SpriteAnimation`: sprite sheet grid, aktif row/frame ve frame timer verisi.
- `SpriteUVRect`: `[MaterialProperty("_UVRect")]` ile shader'a per-instance frame UV rect yollar.
- `SpriteTint`: `[MaterialProperty("_Color")]` ile shader'a per-instance tint yollar.

## CastleComponents.cs

- `WallSegment`: duvar HP bilgisi.
- `GateComponent`: kapi HP bilgisi.
- `CastleHP`: kale ana HP bilgisi.
- `WallXPosition`: eski mode'da duvarin X koordinati.
- `CastleUpgradeData`: kale upgrade seviyesi ve maliyetleri.

## ArcherComponents.cs

- `ArcherType`: `Basic`, `Rapid`, `Frost`.
- `ArcherUnit`: okcu tipi, atis hizi, hasar, menzil, opsiyonel slow bilgileri, facing direction ve attack anim timer.
- `ArrowProjectile`: okun hizi, hasari, hedef entity referansi ve projectile effect datasini tasir.
- `ArrowTag`: ok entity'lerini isaretler.
- `ArcherVisualStyle`: Basic/Rapid/Frost ve slow tint renklerini merkezi tutar.
- `CombatVfxEvent` / `CombatSfxEvent`: DOTS combat sistemlerinden MonoBehaviour feedback bridge'e giden tek frame'lik VFX/SFX event'leridir. Normal arrow/frost hit event'leri bridge tarafinda sprite flipbook impact olarak oynatilir.

## GameStateComponents.cs

- `GameStateData`: XP, level, level-up pending ve game-over state.
- `RunPhaseType`: legacy mobile run phase enum'u. Continuous siege aktifken uyumluluk icin `NightCombat` tutulur.
- `WaveStateData`: dalga/spawn durumu, spawn timer, zombi sayilari ve wave stat'leri. Continuous siege aktifken `WaveActive` true kalir; `StressTestMode` normal mobile akislarini atlar.

## MobileCastleCombatComponents.cs

- `MobileCastleCombatConfig`: `NewGameScene` mobile castle mode singleton'i. Kale merkezi, spawn radius, attack radius, mobile wave/siege sayilari, spawn batch, zombie scale/speed, continuous siege tuning, worker production/cap tuning, reward/focus tuning, legacy day/night prep tuning, unlimited arrow flag'i ve stress test limitlerini tutar.
- `ContinuousSiegeCycleData`: player-facing `DAY / DUSK / NIGHT` fazini, 60s cycle progress'ini, horde pressure ve spawn intensity degerlerini tutar.
- `EconomyFocusState`: aktif mobile ekonomi focus'unu tutar. `Balanced` default'tur; Wood/Stone/Iron/Food secimleri passive income, kill reward ve wave clear bonus'u yonlendirir.
- `WaveClearRewardData`: son wave clear bonusunu HUD feedback'i icin saklar.
- `CastleYardPrepState`: `Fortify` ve `Rally` tek-gecelik prep buff state'ini tutar.
- `MobilePopulationAllocation`: Wood/Stone/Iron/Food worker sayilarini, legacy wave growth checkpoint'ini ve continuous cycle growth checkpoint'ini tutar.
- `ArcherSlotPosition`: legacy/manual pozisyon buffer'i. NewGameScene mobile tilemap spawn akisi bunu kullanmaz.

## CastleInteriorWorkerComponents.cs

- `WorkerPrefabData`: SubScene `WaveConfigAuthoring` tarafindan bake edilen DOTS villager worker prefab referansi.
- `ResourceWorkerVisual`: sahnede gorunen DOTS villager entity'sinin Wood/Stone/Iron/Food kaynagini ve site icindeki index'ini tutar.
- `WorkerLogisticsRoute`: DOTS villager'in kaynak pickup noktasi ile CastleWorkerHub delivery noktasi arasindaki rota state'ini tutar.
- `ResourceWorkerVisualStyle`: kaynak tipine gore hafif worker tint degerlerini merkezi tutar.

`MobileCastleCombatConfig` sahnede yoksa sistemler eski `WallXPosition` tabanli davranisi kullanir.

## PhysicsComponents.cs

- `PhysicsBody`: velocity, force, mass ve damping.
- `CollisionRadius`: circle-circle collision yaricapi.

## Mobile Castle Mode Akisi

```
MobileCastleCombatAuthoring -> MobileCastleCombatConfig + ContinuousSiegeCycleData + EconomyFocusState + WaveClearRewardData + CastleYardPrepState + ArcherSlotPosition buffer bake eder
ContinuousSiegeCycleSystem -> 60s DAY/DUSK/NIGHT cycle, horde pressure ve spawn intensity yazar
DayNightPrepSystem -> Continuous siege kapaliysa legacy DayPrep sayacini azaltir
WaveSpawnSystem -> Config varsa kale etrafindaki random 360 spawn cemberini ve continuous intensity ritmini kullanir
ApplyMovementForceSystem -> Config varsa zombiyi CastleCenter'a yonlendirir, ZombieSlow varsa hiz carpanini uygular
BoundarySystem -> Config varsa AttackRadius icinde Attacking state'e gecirir
GameManager.BuyArcher(type) -> main scene `Grid/outside` tilemap hucrelerine okcu spawn eder
GameManager.AssignResourceWorker(resource) -> MobilePopulationAllocation artirir + DOTS villager worker route visual sync eder
GameManager.BuyFortify()/BuyRally() -> CastleYardPrepState uzerine tek-gecelik buff yazar
CastleYardPrepSystem -> Rally timer'i NightCombat sirasinda azaltir
ArcherShootSystem -> Okcu tipine gore projectile effect datasini oka yazar, facing direction + attack timer set eder
ArrowHitSystem -> Frost ok isabetinde ZombieSlow refresh eder
ZombieSlowTimerSystem -> Slow suresini dusurur, slow tint'ini yonetir ve bitince pasifler
ZombieAnimationStateSystem -> Velocity/center yonunden sprite direction row hesaplar
ArcherAnimationStateSystem -> Okculari hedef yonunde idle/attack row'larina ceker
```

## Veri Akisi

```
PopulationTickSystem -> PopulationState.Idle hesaplar + ResourceConsumptionRate.FoodPerMin gunceller
ResourceTickSystem -> EconomyFocusState varsa effective production hesaplar, ResourceAccumulator + ResourceData gunceller
WaveSpawnSystem -> ZombieStats/ZombieState/PhysicsBody/CollisionRadius olusturur
ApplyMovementForceSystem -> PhysicsBody.Force yazar
PhysicsCollisionSystem -> PhysicsBody.Velocity + LocalTransform yazar
IntegrateSystem -> PhysicsBody.Force -> Velocity -> LocalTransform.Position
BoundarySystem -> ZombieState gecirir
ZombieAttackSystem -> hasar queue yazar
DamageApplySystem -> Wall/Gate/Castle HP yazar
ZombieDeathSystem -> ZombieState.Dead isaretler
DamageCleanupSystem -> Dead entity'leri siler, GameStateData.XP ve mobile kill reward gunceller
```
