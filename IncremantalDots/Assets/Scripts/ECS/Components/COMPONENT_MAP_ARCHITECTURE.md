# ECS Component Haritasi

## Genel Yapi

Tum component'lar unmanaged ECS struct olarak tutulur. Davranis sistemlerde, veri component'larda kalir.

## ZombieComponents.cs

- `ZombieTag`: enableable aktif-zombi isaretidir; pool rezervinde disabled tutulur.
- `ZombieStats`: HP, hareket hizi, saldiri hasari, cooldown ve XP odulu.
- `ZombieSlow`: Frost ok etkisi icin enableable slow duration + speed multiplier.
- `ZombieState`: zombi durumu: `Moving`, `Attacking`, `Dead`, `Queued`.
- `DeathTimer`: enableable olum animasyonu sayacidir; bitince entity pool'a doner.

## SpriteComponents.cs

- `SpriteAnimation`: sprite sheet grid, aktif row/frame ve frame timer verisi.
- `SpriteUVRect`: `[MaterialProperty("_UVRect")]` ile shader'a per-instance frame UV rect yollar.
- `SpriteTint`: `[MaterialProperty("_Color")]` ile shader'a per-instance tint yollar.

## CastleComponents.cs

- `WallSegment`: tek runtime savunma HP bilgisi ve Game Over otoritesi.
- `GateComponent`: yalniz eski Entity Scene verisiyle uyumluluk icin tutulan legacy tip; aktif bake/runtime zincirinde kullanilmaz.
- `CastleHP`: yalniz legacy uyumluluk tipi; sonuc otoritesi degildir.
- `WallXPosition`: eski mode'da duvarin X koordinati.
- `CastleUpgradeData`: kale upgrade seviyesi ve maliyetleri.

## ArcherComponents.cs

- `ArcherType`: `Basic`, `Rapid`, `Frost`.
- `ArcherUnit`: okcu tipi, atis hizi, hasar, menzil, opsiyonel slow bilgileri, facing direction ve attack anim timer.
- `ArcherCapacityUtility`: Basic/Rapid/Frost için tek `1000` toplam entity cap'i; kalan kapasite ve bulk izin matematiği.
- `ArrowProjectile`: okun hizi, hasari, hedef entity referansi, hedef pool generation'i ve projectile effect datasini tasir.
- `ArrowTag`: ok entity'lerini isaretler.
- `ArcherVisualStyle`: Basic/Rapid/Frost ve slow tint renklerini merkezi tutar.
- `CombatVfxEvent` / `CombatSfxEvent`: DOTS combat sistemlerinden MonoBehaviour feedback bridge'e giden tek frame'lik VFX/SFX event'leridir. Normal arrow/frost hit event'leri bridge tarafinda sprite flipbook impact olarak oynatilir.

## GameStateComponents.cs

- `GameStateData`: XP, level, level-up pending ve game-over state.
- `RunPhaseType`: legacy mobile run phase enum'u. Continuous siege aktifken uyumluluk icin `NightCombat` tutulur.
- `WaveStateData`: dalga/spawn durumu, spawn timer, zombi sayilari ve wave stat'leri. Continuous siege aktifken `WaveActive` true kalir; `StressTestMode` normal mobile akislarini atlar.

## MobileCastleCombatComponents.cs

- `MobileCastleCombatConfig`: `NewGameScene` mobile castle mode singleton'i. Kale merkezi, spawn radius, attack radius, mobile wave/siege sayilari, spawn batch, zombie scale/speed, continuous siege tuning, başlangıç yatak kapasitesi, Dawn survivor isteği ve kişi başına Food maliyeti, worker production/cap tuning, reward/focus tuning, legacy day/night prep tuning, unlimited arrow flag'i ve stress test limitlerini tutar.
- `ContinuousSiegeCycleData`: player-facing `DAY / DUSK / NIGHT` fazini, 60s cycle progress'ini, horde pressure ve spawn intensity degerlerini tutar.
- `EconomyFocusState`: aktif mobile ekonomi focus'unu tutar. `Balanced` default'tur; Wood/Stone/Iron/Food secimleri passive income, kill reward ve wave clear bonus'u yonlendirir.
- `WaveClearRewardData`: son wave clear bonusunu HUD feedback'i icin saklar.
- `CastleYardPrepState`: `Fortify` ve `Rally` tek-gecelik prep buff state'ini tutar.
- `MobilePopulationAllocation`: Wood/Stone/Iron/Food actual worker sayilarini, `10.000` basis-point target ratio'larini, etkin cap ve idle aynalarini, population auto-allocation/growth checkpoint'lerini ve son Dawn için requested/accepted/Food budget sonucunu tutar.
- `MobileBedCapacityState`: Run başlangıç yatak kapasitesi ile satın alınmış ek yatak sayısını ayrı tutar; toplam sahiplik `60` tabanından sonra quadratic Wood maliyetini büyütür, gameplay hard max yoktur ve güncel exact save `v7` kapsamındadır.
- `MobileWorkerBuildingUpgradeState`: Hazır Wood/Stone/Iron/Food worker binalarının bağımsız Capacity/Efficiency seviyelerini tutar. Capacity seviye başına `+10` slot, Efficiency baz kişi üretimine additive `+10%` verir; güncel exact save `v7` kapsamındadır.
- `MobileEconomyPriceTuning`: `DifficultyProfileSO` kaynaklı House bed ve worker CAP/EFF başlangıç maliyetleriyle ortak worker bina büyüme çarpanını config entity'sinde taşır. Runtime içerik baseline'ıdır; satın alınmış state değildir ve save'e yazılmaz.
- `ArcherSlotPosition`: legacy/manual pozisyon buffer'i. NewGameScene mobile tilemap spawn akisi bunu kullanmaz.

## CastleInteriorWorkerComponents.cs

- `WorkerPrefabData`: SubScene `WaveConfigAuthoring` tarafindan bake edilen DOTS villager worker prefab referansi.
- `ResourceWorkerVisual`: sahnede gorunen temsili DOTS villager entity'sinin Wood/Stone/Iron/Food kaynagini, site index'ini ve o visual'in temsil ettigi exact actual worker sayisini tutar; gameplay truth degildir.
- `WorkerLogisticsRoute`: DOTS villager'in pickup, site approach, ortak koridor approach ve
  CastleWorkerHub delivery noktasi arasindaki segmentli rota state'ini tutar.
- `WorkerLogisticsFeedbackState`: working/carrying/delivering/returning activity, cargo, lantern ve delivery pulse state'ini tutar.
- `SurvivorArrivalVisual`: Dawn'da kabul edilmiş nüfusun geçici world-space temsilinde hedef, hız, başlangıç gecikmesi, varış mesafesi ve exact represented survivor sayısını tutar; gameplay population truth'u değildir.
- `WorkerAnimationMaterialProperty`, `WorkerFeedbackMaterialProperty` ve `WorkerCargoColorMaterialProperty`: Idle/Walk/Work/Celebrate atlas secimini, cargo/fener/teslimat shader feedback'ini DOTS instancing ile yollar.
- `ResourceWorkerVisualStyle`: kaynak tipine gore worker ve tasinan paket tint degerlerini merkezi tutar.
- `WorkerVisualRepresentationUtility`: actual worker sayisini Low/Medium/High egriyle resource basina en fazla `32` temsili visual'a cevirir; actual sayiyi visual'lara exact weight olarak dagitir ve feedback siddetini cozer.
- `SurvivorArrivalVisualUtility`: accepted nüfusu en fazla `15` görsele exact weight ile dağıtır; Wall'ın sağındaki lane spawn'larını, Wall arkası hedeflerini, hız/gecikme varyasyonunu ve arrival tint'ini deterministik üretir.

`MobileCastleCombatConfig` sahnede yoksa sistemler eski `WallXPosition` tabanli davranisi kullanir.

## PhysicsComponents.cs

- `PhysicsBody`: velocity, force, mass ve damping.
- `CollisionRadius`: circle-circle collision yaricapi.

## Mobile Castle Mode Akisi

```
DifficultyProfileSO -> MobileCastleTuningResolver -> MobileEconomyPriceTuning
MobileCastleCombatAuthoring -> MobileCastleCombatConfig + MobileEconomyPriceTuning + MobileBedCapacityState + MobileWorkerBuildingUpgradeState + ContinuousSiegeCycleData + EconomyFocusState + WaveClearRewardData + CastleYardPrepState + ArcherSlotPosition buffer bake eder
ContinuousSiegeCycleSystem -> 60s DAY/DUSK/NIGHT cycle, horde pressure ve spawn intensity yazar
DayNightPrepSystem -> Continuous siege kapaliysa legacy DayPrep sayacini azaltir
WaveSpawnSystem -> Config varsa kale etrafindaki random 360 spawn cemberini ve continuous intensity ritmini kullanir
ApplyMovementForceSystem -> Config varsa zombiyi CastleCenter'a yonlendirir, ZombieSlow varsa hiz carpanini uygular
BoundarySystem -> Config varsa AttackRadius icinde Attacking state'e gecirir
GameManager.BuyArcher(type) -> ArcherCapacityUtility ortak 1000 cap -> MobileCastleArcherTilePlacement + ArcherFormationV1 -> main scene `Grid/outside` uzerindeki 40 x 25 formasyona okcu spawn eder
MobilePopulationAllocation actual count -> WorkerVisualRepresentationUtility -> GameManager temsili DOTS villager count + exact weight sync -> WorkerLogisticsMovementSystem animation/cargo/fener/teslimat feedback
GameManager.TryBuyBedCapacity -> MobileEconomyPriceTuning + MobileBedCapacityUtility owned-capacity sıralı fiyatı -> Wood transaction -> MobileBedCapacityState.PurchasedCapacity
GameManager.TryBuyWorkerBuildingUpgrade -> MobileEconomyPriceTuning fiyatı -> Wood + Iron transaction -> bağımsız bina seviyesi -> base + Heart + Council + Meta + bina config aggregate'i
MobilePopulationEconomySystem -> MobileBedCapacityState kapasite aynası -> MobilePopulationArrivalUtility bed + Food kabul bütçesi -> accepted population growth + tek seferlik ResourceData.Food transaction
MobilePopulationAllocation yeni growth marker + accepted count -> GameManager VillagerWorker arrival spawn -> SurvivorArrivalVisualSystem sağdan Wall arkasına yürüyüş + varışta destroy
GameManager.BuyFortify()/BuyRally() -> CastleYardPrepState uzerine tek-gecelik buff yazar
CastleYardPrepSystem -> Rally timer'i NightCombat sirasinda azaltir
ArcherShootSystem -> Coarse target grid + incoming damage reservation ile nearest yaşayan hedefi seçer; okçu tipine göre projectile effect datasını oka yazar, facing direction + attack timer set eder
ArrowHitSystem -> Frost ok isabetinde ZombieSlow refresh eder
ZombieSlowTimerSystem -> Slow suresini dusurur, slow tint'ini yonetir ve bitince pasifler
ZombieAnimationStateSystem -> Velocity/center yonunden sprite direction row hesaplar
ArcherAnimationStateSystem -> Okculari hedef yonunde idle/attack row'larina ceker
```

## Veri Akisi

```
MobilePopulationEconomySystem -> bed kapasitesini aynalar; Dawn arrival'ını boş yatak + Food bütçesiyle sınırlar; kabul edilen yeni population'i target ratio + cap ile worker/idle state'e dagitir
GameManager + SurvivorArrivalVisualSystem -> committed accepted count'u yalnız sunum için geçici villager entity'lerine çevirir; population/resource state yazmaz
PopulationTickSystem -> PopulationState.Idle aggregate'ini hesaplar; V1'de pasif Food consumption yazmaz
ResourceTickSystem -> EconomyFocusState varsa effective production hesaplar, ResourceAccumulator + ResourceData gunceller
WaveSpawnSystem -> ZombieStats/ZombieState/PhysicsBody/CollisionRadius olusturur
ApplyMovementForceSystem -> PhysicsBody.Force yazar
PhysicsCollisionSystem -> PhysicsBody.Velocity + LocalTransform yazar
IntegrateSystem -> PhysicsBody.Force -> Velocity -> LocalTransform.Position
BoundarySystem -> ZombieState gecirir
ZombieAttackSystem -> hasar queue yazar
DamageApplySystem -> yalniz WallSegment HP yazar; Wall 0 ise GameStateData.IsGameOver
ZombieDeathSystem -> ZombieState.Dead isaretler
DamageCleanupSystem -> Dead entity'leri pool'a dondurur, GameStateData.XP ve mobile kill reward gunceller
```
