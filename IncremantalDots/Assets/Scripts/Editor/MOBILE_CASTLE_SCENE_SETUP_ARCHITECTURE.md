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

World visual tilemap'leri owner tarafindan yonetilir. Tool `Grid/outside` tilemapini bulursa sadece `MobileCastleArcherTilePlacement` controller'ina baglar; tile icerigine dokunmaz. Kale occlusion icin renderer sorting ve z-depth band'larini normalize eder: `inside` Wall/1/z0, `outside0` ve `outside` Wall/2/z0, `outside2` Wall/4/z-2.

`Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab` varsa `MobileCastleHudRoot` bu prefabdan instancelanir. Prefab yoksa fallback HUD ayni runtime isimleriyle kurulur: economy text'leri, fallback `WaveText`, fallback `KillsText`, fallback `DefenseText`, `WaveRewardText`, `DamageFlashOverlay`, `ArcherDrawerPanel`, Basic/Rapid/Frost row buy alanlari ve `RepairButton`. Onayli prefabda `CyclePanel` varsa fallback `WaveText/KillsText` uretilmez ve varsa kapatilir. Castle Interior economy paneli icin fallback polish UI uretilmez; panel gerekirse dogrudan prefabda kurulur.

Onayli polish prefab gelirse tool `CastleDefensePanel` altindaki `DefensePercentText`, `DefenseWallFill`, `DefenseGateFill`, `DefenseCoreFill`, `DefenseWallText`, `DefenseGateText`, `DefenseCoreText` ve opsiyonel `DefenseDamageGlow` alanlarini baglar. Fill image'lari setup sirasinda horizontal left fill moduna alinir.

## ECS Notu

DOTS authoring objeleri SubScene icinde tutulur. `GameStateAuthoring`, `WaveConfigAuthoring` ve `CastleAuthoring` bake edilerek ECS singleton/component verisini olusturur. Ana scene MonoBehaviour/UI katmani ile SubScene combat verisi ayrik kalir.

`MobileCastleCombatAuthoring`, mobile combat mode switch'idir. Bu component bake edilince `MobileCastleCombatConfig` singleton'i ve `ArcherSlotPosition` buffer'i olusur. Runtime sistemleri config varsa merkezi kale davranisini, config yoksa eski `WallX` davranisini kullanir.

`BasicArcher_01` legacy/seed authoring objesi olarak kalabilir; runtime mobile ilk acilista mevcut okculari `Grid/outside` tilemap spawn hucrelerine yeniden yerlestirir. Sonraki okcular da ayni tilemap hucre listesini kullanir.

Tool mobile combat degerlerini de normalize eder: zombi scale/speed eski prefab degerlerinden ayrilir, continuous siege cycle tuning, legacy wave director tuning, kill/wave reward tuning, worker economy tuning, economy event tuning, unlimited arrow flag'i ve stress test batch/interval/cap degerleri `MobileCastleCombatAuthoring` uzerinden tutulur. LevelUpPanel legacy olarak durabilir, fakat mobile loop'ta kullanilmaz.

World visual foundation main scene MonoBehaviour/Tilemap tarafinda ve owner kontrolundedir. Gorsel kale `(0,0)` gameplay center ile hizalanir. `inside/outside/outside2` tilemap'leri gorsel katmandir; yalniz `outside` tilemap'indeki dolu hucreler okcu spawn kaynagi olarak okunur. `outside2` front-wall/occluder katmanidir ve okculardan onde cizilmek icin z `-2` bandinda tutulur. Bir tilemap hem ust yurume yuzeyi hem on duvar yuzu icerirse partial occlusion beklenmez; front-wall pikselleri ayri ondeki tilemapte tutulmalidir.

Readability polish icin `MobileCastleArcherTilePlacement` Scene view'da `outside` spawn hucrelerini ve tekrar kullanim preview noktalarini cizer. `BasicArcher_01` beyaz tint ile normalize edilir; Basic/Rapid/Frost runtime tint'leri ECS tarafinda `SpriteTint` ile uygulanir. Archer count bilgisi sag drawer row'larinda okunur; eski `ArcherTypeText` placeholder'i mobile HUD'da kullanilmaz.

Wave/run loop icin drawer oyun akisi controller'i degil, yalnizca gorsel ve referans root'udur. Davranis `MarketUI`, `UIManager`, `GameManager` ve ECS `DayNightPrepSystem` tarafindan uygulanir. Prefab runtime event barindirmaz. Yeni UI yuzeyi dogrudan `MobileCastleHudRoot.prefab` uzerinde kurulur (eski UIImporter/export pipeline'i 2026-07-06'da kaldirildi).

Castle Yard prep aksiyonlari sag drawer'da player-facing degildir. Tool `RepairButton`, `FortifyButton`, `RallyButton`, `RefillArrowsButton` ve `StartNextWaveButton` bulursa gizler. Tool ayrica `Basic/Rapid/FrostUpgradeButton`, `ArrowTechPanel`, `RapidTechUnlockButton` ve `FrostTechUnlockButton` bulursa gizler; sag drawer yalnizca archer recruitment icin kullanilir. `CastleRepairButton` legacy Castle Interior akisi icin bagli kalabilir, fakat continuous siege varsayilaninda Castle Interior player-facing kapali tutulur.

Sag drawer recruitment data'si `Assets/ScriptableObject/MobileCastle/Archers` altindaki `ArcherDefinitionSO` asset'lerinden gelir. Tool Basic/Rapid/Frost default definition asset'lerini ve `ArcherRecruitmentCatalogSO` catalog'unu idempotent olusturur, sonra catalog'u `GameManager` ve `MarketUI` alanlarina baglar. Catalog'daki ekstra definition asset'leri korunur; setup tool sadece eksik defaultlari tamamlar. `ArcherRecruitmentListRoot` + inactive `ArcherRecruitmentRowTemplate` varsa `MarketUI` runtime satirlari template'ten basar; legacy Basic/Rapid/Frost row'lari sadece fallback'tir.

`DayNightOverlay` Canvas'in ilk child'i olarak kurulur. Full-screen siyah Image sadece world'u karartir; `MobileCastleHudRoot` sonradan geldigi icin HUD/drawer overlay'in ustunde kalir. Overlay alpha runtime'da `DayNightOverlayController` tarafindan mobile config'teki day/night alpha ve `WaveStateData.PrepTimer` degerlerine gore guncellenir.

`CombatFeedbackRoot`, `CombatFeedbackBridge` ile ECS feedback event'lerini hit flipbook, pooled ParticleSystem ve AudioSource playback'e cevirir. Tool `fanfx2_cure_small_red/spritesheet.png` flipbook frame'lerini, opsiyonel particle fallback'lerini, `Arrow & Bow*.wav` random shoot clip listesini ve `Rock Impact 37.wav` referansini baglar. Demo `FX_Shoot_Arrow.prefab` root'u kullanilmaz.

Mobile HUD economy varsayilanlari NewGameScene setup tarafindan GameStateAuthoring'e yazilir:

- Wood `280`, Stone `120`, Iron `70`, Food `220`, Population `60`, Arrows `200`
- Initial workers: Wood `20`, Stone `10`, Iron `8`, Food `15`
- Initial archers: `4`
- Worker caps: Wood `40`, Stone `30`, Iron `24`, Food `40`
- Worker production: Wood `8/min`, Stone `5.5/min`, Iron `3.8/min`, Food `7/min`
- Continuous siege cycle tamamlandikca population growth `+15` uygulanir.

Economy focus UI mobile worker economy ile kullanilmaz. Tool eski `EconomyFocusPanel`, `EconomyFocusText` ve `EconomyBalanced/Wood/Stone/Iron/FoodButton` objelerini root'tan soker; bunlar re-run ile geri uretilmez. Yeni player-facing worker kontrolu `WorkerEconomyDrawerUI` uzerinden sol ust resource bar altindaki drawer ile yapilir; target ratio ile hazir binalarin Capacity/Efficiency yatirimlari ayni panelde sunulur. Prefab yalniz gorsel kontrolleri tasir, runtime controller sahne instance'inda tek owner olarak kalir. `CastleEconomyUI` legacy full-screen panel olarak bagli kalabilir ama `PlayerFacingPanelEnabled = false` ile kapali tutulur. Runtime davranis UI JSON icine gomulmez.

## Tech Tree Kurulumu

Tool `Assets/ScriptableObject/MobileCastle/TechTree/` altinda 13 default `TechNodeDefinitionSO`
asset'ini ve `TechTreeCatalog.asset`'i idempotent seed eder (`EnsureDefaultTechTreeCatalog`):
mevcut asset degerlerine DOKUNMAZ, katalogdaki kullanici-eklenmis ekstra node'lar KORUNUR
(merge-only), `RootNodeId` bossa `castle_heart` yazilir, `ValidateCatalog()` sorunlari
Console'a warning basilir. Catalog `GameManager.techTreeCatalog` alanina baglanir.

`ConfigureTechTree` HUD root'a `TechTreeUI` component'ini ekler ve prefabdaki
`TechTreePanel/TechTreeOpenButton/TechTreeCloseButton/TechTreeViewport/TechTreeContent/
TechNodeTemplate/TechConnectionTemplate` objelerini isimle baglar; template'ler inactive,
panel kapali garanti edilir. Prefabda panel yoksa `EnsureFallbackTechTreePanel` minimal
iskeleti kurar (normal akista devreye girmez — objeler `MobileCastleHudRoot.prefab` icindedir).
`ConfigureHudRoot` ayrica HUD root'taki missing-script kalintilarini temizler
(`GameObjectUtility.RemoveMonoBehavioursWithMissingScript`; eski `CastleTechTreeUI` kalintisi).
Detay: `MonoBehaviour/TECH_TREE_UI_ARCHITECTURE.md` + `ScriptableObject/TECH_TREE_SO_ARCHITECTURE.md`.
