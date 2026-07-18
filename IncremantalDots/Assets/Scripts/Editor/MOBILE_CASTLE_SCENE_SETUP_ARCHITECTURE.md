# Mobile Castle Scene Setup - Mimari

## Amac

`MobileCastleSceneSetupWindow`, mobil castle-defense GDD icin `NewGameScene` sahne omurgasini kurar. Tool, Unity scene YAML'ini elle duzenlemek yerine Editor API kullanir.

## Kurulan Yapi

- Ana scene: `Assets/Scenes/NewGameScene.unity`
- Combat SubScene: `Assets/Scenes/NewGameScene/MobileCastleCombatSubScene.unity`
- Ana scene objeleri: `Main Camera`, `Global Light 2D`, `EventSystem`, `Canvas`, `GameManager`, `CastleClickTarget`, `MobileCastleCombatSubScene`
- Combat feedback objesi: `CombatFeedbackRoot`
- User-owned world visual objeleri: ana scene'de `Grid` altinda `inside`, `outside`, `outside2`, `Grass`, `Tree` gibi tilemap'ler bulunabilir; setup tool bu tilemap'leri uretmez, boyamaz veya tasimaz
- Canvas panelleri: `DayNightOverlay`, `MobileCastleHudRoot`, `LevelUpPanel`, `GameOverPanel`
- SubScene authoring objeleri: `GameState`, `CastleCore`, `MobileCastleConfig`, `BasicArcher_01`

## Sorumluluk Sinirlari

Bu tool sahne altyapisini ve mobile HUD/drawer icin gerekli UI/SubScene baglantilarini kurar. `MarketUI` artik popup market degil, `MobileCastleHudRoot` uzerindeki sag `ArcherDrawerPanel` controller'idir. Sag drawer'in player-facing rolu yalnizca okcu satin alma/recruitment'tir. Upgrade ve tech unlock API'leri runtime'da ileride Tech Tree icin kalabilir, fakat bu panelde gosterilmez.

Mevcut `GameScene` ve `GameScene/TestSubScene` referans olarak kullanilir, ama yeni mobil sahneye town-building/grid/resource UI kopyalanmaz.

## Idempotent Davranis

Tool ayni isimli root ve child objeleri yeniden kullanir. Tekrar calistirildiginda duplicate `Canvas`, `GameManager`, `MobileCastleHudRoot`, `LevelUpPanel` veya SubScene root objesi uretmemelidir.

World visual tilemap'leri owner tarafindan yonetilir. Tool `Grid/outside` tilemapini bulursa `MobileCastleArcherTilePlacement` controller'ina ve version'li `ArcherFormationV1.asset` tanimina baglar; tile icerigine dokunmaz. Kale occlusion icin renderer sorting ve z-depth band'larini normalize eder: `inside` Wall/1/z0, `outside0` ve `outside` Wall/2/z0, `outside2` Wall/4/z-2.

`Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab` varsa `MobileCastleHudRoot` bu prefabdan instancelanir. Prefab yoksa fallback HUD ayni runtime isimleriyle kurulur: economy text'leri, fallback `WaveText`, fallback `KillsText`, fallback `DefenseText`, `WaveRewardText`, `DamageFlashOverlay`, `ArcherDrawerPanel`, Basic/Rapid/Frost row buy alanlari ve `RepairButton`. Onayli prefabda `CyclePanel` varsa fallback `WaveText/KillsText` uretilmez ve varsa kapatilir. Castle Interior economy paneli icin fallback polish UI uretilmez; panel gerekirse dogrudan prefabda kurulur.

`GameOverPanel` scene-owned kalır; HUD prefabının parçası değildir. Meta presentation v2 için
`1120 x 880` rounded frame, Last Embers reward/balance ikonları, açıklamalı `68px` shop satırları,
maskeli vertical `ScrollRect` ve ayrı `BEGIN NEXT RUN` CTA kullanır. Eski `680 x 640` sıkışık
SOULS listesi `Window > DeadWalls > Repair Meta Identity Presentation` ile idempotent migrate
edilir. Migration `MetaUpgradeCatalogSO.Presentation` alanını yeniler; legacy save bakiyesi ve
upgrade Id/level state'ini değiştirmez.

Generated prefabdaki dis root, `CanvasScaler` ile `1920 x 1080` referans ve `0.5`
width/height match kullanir. Onun dogrudan altindaki ayni isimli gorsel
`MobileCastleHudRoot` sabit piksel boyutu tasimaz; parent sanal canvas'ina dort yonden stretch
olur. Setup tool bu sozlesmeyi prefabda ve scene instance'inda idempotent onarir. Boylece
16:9 yerlesimi korunurken `3440 x 1440` ultrawide sanal alaninda ust/alt kritik HUD
yuzeyleri kirpilmaz. `CastleDefensePanel` top-center anchor ve `-205` dikey offset ile
16:9 konumunu korur; daha kisa ultrawide sanal yukseklikte Celestial Dial'a binmez.

Onayli polish prefab gelirse tool `CastleDefensePanel` altindaki `DefensePercentText`, `DefenseWallFill`, `DefenseWallText` ve opsiyonel `DefenseDamageGlow` alanlarini baglar. Legacy Gate/Core gorseli veya binding'i kurulmaz. Wall fill image'i setup sirasinda horizontal left fill moduna alinir. Ust-orta `CyclePanel`, owner-secili `B - Celestial Dial` olarak `290 x 68` gercek pill siluetinde kalir; setup tool `CycleProgressTrack`i `178 x 44` `CycleCelestialArc`, `CycleCelestialGlow`u halo binding'i yapar ve dikey ayirici ile ham phase label/linear fill objelerini player-facing kapatir.

## ECS Notu

DOTS authoring objeleri SubScene icinde tutulur. `GameStateAuthoring`, `WaveConfigAuthoring` ve `CastleAuthoring` bake edilerek ECS singleton/component verisini olusturur. Ana scene MonoBehaviour/UI katmani ile SubScene combat verisi ayrik kalir.

`MobileCastleCombatAuthoring`, mobile combat mode switch'idir. Bu component bake edilince `MobileCastleCombatConfig` singleton'i ve `ArcherSlotPosition` buffer'i olusur. Runtime sistemleri config varsa merkezi kale davranisini, config yoksa eski `WallX` davranisini kullanir.

`BasicArcher_01` legacy/seed authoring objesi olarak kalabilir; runtime mobile ilk acilista mevcut okculari Formation V1'in ilk slotlarina yeniden yerlestirir. Sonraki okcular ayni 40 x 25 layer-fill sirasini kullanir.

Tool mobile combat degerlerini de normalize eder: zombi scale/speed eski prefab degerlerinden ayrilir, continuous siege cycle tuning, legacy wave director tuning, kill/wave reward tuning, worker economy tuning, economy event tuning ve stress test batch/interval/cap degerleri `MobileCastleCombatAuthoring` uzerinden tutulur. Finite Arrow fiyat baseline'i `DifficultyProfileSO -> MobileEconomyPriceTuning`, run state'i `ArrowSupply` owner'ındadır. LevelUpPanel legacy olarak durabilir, fakat mobile loop'ta kullanilmaz.

World visual foundation main scene MonoBehaviour/Tilemap tarafinda ve owner kontrolundedir. Gorsel kale `(0,0)` gameplay center ile hizalanir. `inside/outside/outside2` tilemap'leri gorsel katmandir; `outside`, Formation V1 asset'indeki tam 40 canonical hucrenin world-space yuzeyidir. `outside2` front-wall/occluder katmanidir ve okculardan onde cizilmek icin z `-2` bandinda tutulur. Bir tilemap hem ust yurume yuzeyi hem on duvar yuzu icerirse partial occlusion beklenmez; front-wall pikselleri ayri ondeki tilemapte tutulmalidir.

Readability polish icin `MobileCastleArcherTilePlacement` Scene view'da 40 tile x 25 seeded noktanin tamamini cizer. `BasicArcher_01` beyaz tint ile normalize edilir; Basic/Rapid/Frost runtime tint'leri ECS tarafinda `SpriteTint` ile uygulanir. Archer count bilgisi sag drawer row'larinda okunur; eski `ArcherTypeText` placeholder'i mobile HUD'da kullanilmaz.

Wave/run loop icin drawer oyun akisi controller'i degil, yalnizca gorsel ve referans root'udur. Davranis `MarketUI`, `UIManager`, `GameManager` ve ECS `DayNightPrepSystem` tarafindan uygulanir. Prefab runtime event barindirmaz. Yeni UI yuzeyi dogrudan `MobileCastleHudRoot.prefab` uzerinde kurulur (eski UIImporter/export pipeline'i 2026-07-06'da kaldirildi).

Castle Yard prep aksiyonlari sag drawer'da player-facing degildir. Tool `RepairButton`, `FortifyButton`, `RallyButton`, legacy `RefillArrowsButton` ve `StartNextWaveButton` bulursa gizler. Finite refill için Arrow chip'ine bağlı tek satırlık `AmmoPurchasePanel` ve scene-owned `ArrowSupplyUI` kurar. Tool ayrica `Basic/Rapid/FrostUpgradeButton`, `ArrowTechPanel`, `RapidTechUnlockButton` ve `FrostTechUnlockButton` bulursa gizler; sag drawer yalnizca archer recruitment icin kullanilir. `CastleRepairButton` legacy Castle Interior akisi icin bagli kalabilir, fakat continuous siege varsayilaninda Castle Interior player-facing kapali tutulur.

`ConfigureHudRoot`, scene root'a tek `ManagementDrawerCoordinatorUI` ekler. Bu owner
Workers/Housing, Archer Recruitment ve Arrow Supply acilislarini exclusive tutar; prefab
presentation kaynagi olarak runtime component tasimaz.

Council karti generated prefabda `CouncilEventPanel` presentation'i olarak kalir. Setup tool,
scene root'taki tek `CouncilEventUI` owner'ini iki option rich-text yuzeyi, `DECIDE Ns` sayaci ve
Filled/Horizontal/Left azalan sure seridine baglar; V1'de yalniz regular Day `3/6/9...` akisi vardir.

First-run onboarding sunumu generated prefabda raycast kapali
`OnboardingHintPanel` ve `OnboardingPulseFrame` olarak kalir. Runtime state prefab assetine
gomulmez; setup tool scene HUD root'una tek `FirstRunOnboardingUI` ekler ve mevcut
`WorkerEconomyDrawerUI`, `MarketUI`, `ArrowSupplyUI`, `HeartScreenUI`, `CouncilEventUI` ve
`DefenseRepairUI` player-action event'lerine; `SpellCastUI` accepted-hotkey event'ine baglar.
Worker ratio, ilk affordable Basic Archer,
ilk `%25` low-ammo, ilk pozitif Grave Essence Heart girisi, ilk regular Council ve ilk Daytime
Wall repair ile ilk Night ability-key cue'su ayni presentation'i paylasir. Low-ammo hedefi gercek
ust HUD `ArrowChip`, Heart hedefi gercek alt-sag `CastleHeartOpenButton`, repair hedefi gercek
`DefenseRepairButton`, ability hedefi ilk hazir gercek `AbilityBarPanel` slotudur.
Controller drawer/paneli
otomatik acmaz, ekonomi state'i yazmaz veya yeni pause lease'i uretmez; Heart acikken yalniz
mevcut full-pause davranisini unscaled hint ile aciklar.

Sabit kamera framing sozlesmesi `Main Camera` position `(6,0,-10)`, orthographic size `8`
ve desteklenen `1920 x 1080` / `3440 x 1440` oranlaridir. Bu araliklarda savunma hatti ile
kale merkezi gorunur, `SpawnLineX = 27` ise ekranin saginda en az bir world unit gizli kalir.
`HudAspectRatioPresentationTests` hem kritik HUD rect sinirlarini hem bu battlefield/spawn
sozlesmesini asset ve scene truth'u uzerinden kilitler.

Sag drawer buy ve Basic -> Rapid/Frost retrain data'si `Assets/ScriptableObject/MobileCastle/Archers` altindaki `ArcherDefinitionSO` asset'lerinden gelir. Tool Basic/Rapid/Frost default definition asset'lerini ve `ArcherRecruitmentCatalogSO` catalog'unu idempotent olusturur, sonra catalog'u `GameManager` ve `MarketUI` alanlarina baglar. Catalog'daki ekstra definition asset'leri korunur; setup tool sadece eksik defaultlari tamamlar. `ArcherRecruitmentListRoot` + inactive `ArcherRecruitmentRowTemplate` varsa `MarketUI` runtime satirlari template'ten basar; template icindeki `ArcherRetrainButton` eksikse idempotent prefab repair ekler. Legacy Basic/Rapid/Frost row'lari sadece fallback'tir.

`DayNightOverlay` Canvas'in ilk child'i olarak kurulur. Full-screen siyah Image sadece world'u karartir; `MobileCastleHudRoot` sonradan geldigi icin HUD/drawer overlay'in ustunde kalir. Overlay alpha runtime'da `DayNightOverlayController` tarafindan mobile config'teki day/night alpha ve `WaveStateData.PrepTimer` degerlerine gore guncellenir.

`AmbientAudioRoot`, audio owner'ıyla birlikte serialized adı korunan `MomentVignetteUI` phase-world
owner'ını taşır. Tool tek `PhaseAtmosphereParticles` child'ını, radial mote texture'ını ve URP
transparent materialini idempotent üretir. Main Camera sky rengi ile `72` cap'li particle field
Day/Dusk/Night/Dawn boyunca aynı authoritative cycle verisini izler; HUD faz yazısı üretmez.

`CombatFeedbackRoot`, `CombatFeedbackBridge` ile ECS feedback event'lerini hit flipbook, pooled ParticleSystem ve AudioSource playback'e cevirir. Tool `fanfx2_cure_small_red/spritesheet.png` flipbook frame'lerini, opsiyonel particle fallback'lerini, `Arrow & Bow*.wav` random shoot clip listesini ve `Rock Impact 37.wav` referansini baglar. Demo `FX_Shoot_Arrow.prefab` root'u kullanilmaz.

Mobile HUD economy varsayilanlari NewGameScene setup tarafindan GameStateAuthoring'e yazilir:

- Wood `280`, Stone `120`, Iron `70`, Food `220`, Population `60`, Arrows `200`
- Initial workers: Wood `20`, Stone `10`, Iron `8`, Food `15`
- Initial archers: `4`
- Worker caps: Wood `40`, Stone `30`, Iron `24`, Food `40`
- Worker production authoring fallback: Wood `8/min`, Stone `5.5/min`, Iron `4.9/min`,
  Food `7/min`; aktif `DefaultDifficulty.asset` ayni degerlerin content owner'idir
- Continuous siege cycle tamamlandikca aktif `DefaultDifficulty.asset` kaynakli Dawn request
  `+15` ve Food/arrival `1` uygulanir; initial bed `60` authoring state'idir.

Economy focus UI mobile worker economy ile kullanilmaz. Tool eski `EconomyFocusPanel`, `EconomyFocusText` ve `EconomyBalanced/Wood/Stone/Iron/FoodButton` objelerini root'tan soker; bunlar re-run ile geri uretilmez. Yeni player-facing worker kontrolu `WorkerEconomyDrawerUI` uzerinden sol ust resource bar altindaki drawer ile yapilir; target ratio ile hazir binalarin Capacity/Efficiency yatirimlari ayni panelde sunulur. Prefab yalniz gorsel kontrolleri tasir, runtime controller sahne instance'inda tek owner olarak kalir. `CastleEconomyUI` legacy full-screen panel olarak bagli kalabilir ama `PlayerFacingPanelEnabled = false` ile kapali tutulur. Runtime davranis UI JSON icine gomulmez.

## Castle Heart Kurulumu

Tool `Assets/ScriptableObject/MobileCastle/TechTree/` altinda 13 default `TechNodeDefinitionSO`
asset'ini ve `TechTreeCatalog.asset`'i idempotent seed eder (`EnsureDefaultTechTreeCatalog`):
mevcut asset degerlerine DOKUNMAZ, katalogdaki kullanici-eklenmis ekstra node'lar KORUNUR
(merge-only), `RootNodeId` bossa `castle_heart` yazilir, `ValidateCatalog()` sorunlari
Console'a warning basilir. Catalog `GameManager.techTreeCatalog` alanina baglanir.

Legacy catalog seed'i save/migration uyumlulugu icin korunur; aktif production progression
owner'i degildir. Production `HeartNodeCatalogSO` setup tool tarafindan seed edilmez.

`ConfigureTechTree` once HUD root'taki `TechTreeUI` component'ini kaldirir, sonra
`HeartScreenUI` ekler ve prefabdaki `CastleHeartPanel/CastleHeartOpenButton/
CastleHeartCloseButton/HeartViewport/HeartContent/HeartNodeTemplate/
HeartConnectionTemplate` objelerini isimle baglar. Grave Essence, status, branch compass,
`+1/+10/MAX`, badge ve toast alanlari da ayni configurator tarafindan bind edilir; template'ler
inactive ve panel kapali garanti edilir. Eski `Tech...` isimleri yalniz migration fallback'i
olarak taninir. Prefabda panel yoksa legacy isimli minimal fallback iskelet kurulabilir;
normal akista kaynak `MobileCastleHudRoot.prefab`dir.
`ConfigureHudRoot` ayrica HUD root'taki missing-script kalintilarini temizler
(`GameObjectUtility.RemoveMonoBehavioursWithMissingScript`; eski `CastleTechTreeUI` kalintisi).
`EnsureArcherHeartDockLayout`, `ArcherDrawerPanel` ile `ARCHERS` / `CASTLE HEART`
girislerini alt-sag layout'a normalize eder; Archer toggle kayan panelin child'i olarak
birakilmaz. Runtime owner'lar prefabda degil scene root'taki `MarketUI` ve `HeartScreenUI`dir.
Detay: `MonoBehaviour/HEART_SCREEN_ARCHITECTURE.md` +
`ScriptableObject/HEART_PURCHASE_EFFECT_ARCHITECTURE.md`.
