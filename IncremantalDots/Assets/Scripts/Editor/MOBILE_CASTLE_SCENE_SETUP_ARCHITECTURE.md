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

Bu tool sahne altyapisini ve mobile HUD/drawer icin gerekli UI/SubScene baglantilarini kurar. `MarketUI` artik popup market degil, `MobileCastleHudRoot` uzerindeki sag `ArcherDrawerPanel` controller'idir. Satin alma, upgrade ve tech unlock davranisi runtime `GameManager` API'lerindedir; polish gorsel export owner onayindan sonra gelir.

Mevcut `GameScene` ve `GameScene/TestSubScene` referans olarak kullanilir, ama yeni mobil sahneye town-building/grid/resource UI kopyalanmaz.

## Idempotent Davranis

Tool ayni isimli root ve child objeleri yeniden kullanir. Tekrar calistirildiginda duplicate `Canvas`, `GameManager`, `MobileCastleHudRoot`, `LevelUpPanel` veya SubScene root objesi uretmemelidir.

World visual tilemap'leri owner tarafindan yonetilir. Tool `Grid/outside` tilemapini bulursa sadece `MobileCastleArcherTilePlacement` controller'ina baglar; tile icerigine dokunmaz. Kale occlusion icin renderer sorting ve z-depth band'larini normalize eder: `inside` Wall/1/z0, `outside0` ve `outside` Wall/2/z0, `outside2` Wall/4/z-2.

`Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab` varsa `MobileCastleHudRoot` bu prefabdan instancelanir. Prefab yoksa fallback HUD ayni runtime isimleriyle kurulur: economy text'leri, `WaveText`, `KillsText`, fallback `DefenseText`, `WaveRewardText`, `DamageFlashOverlay`, `ArcherDrawerPanel`, Basic/Rapid/Frost row alanlari, tech unlock butonlari ve `RepairButton`. Castle Interior economy paneli icin fallback polish UI uretilmez; owner onayli UI Importer export'u beklenir.

Onayli polish prefab gelirse tool `CastleDefensePanel` altindaki `DefensePercentText`, `DefenseWallFill`, `DefenseGateFill`, `DefenseCoreFill`, `DefenseWallText`, `DefenseGateText`, `DefenseCoreText` ve opsiyonel `DefenseDamageGlow` alanlarini baglar. Fill image'lari setup sirasinda horizontal left fill moduna alinir.

## ECS Notu

DOTS authoring objeleri SubScene icinde tutulur. `GameStateAuthoring`, `WaveConfigAuthoring` ve `CastleAuthoring` bake edilerek ECS singleton/component verisini olusturur. Ana scene MonoBehaviour/UI katmani ile SubScene combat verisi ayrik kalir.

`MobileCastleCombatAuthoring`, mobile combat mode switch'idir. Bu component bake edilince `MobileCastleCombatConfig` singleton'i ve `ArcherSlotPosition` buffer'i olusur. Runtime sistemleri config varsa merkezi kale davranisini, config yoksa eski `WallX` davranisini kullanir.

`BasicArcher_01` legacy/seed authoring objesi olarak kalabilir; runtime mobile ilk acilista mevcut okculari `Grid/outside` tilemap spawn hucrelerine yeniden yerlestirir. Sonraki okcular da ayni tilemap hucre listesini kullanir.

Tool mobile combat degerlerini de normalize eder: zombi scale/speed eski prefab degerlerinden ayrilir, wave director tuning, kill/wave reward tuning, worker economy tuning, economy event tuning, day/night prep tuning, unlimited arrow flag'i ve stress test batch/interval/cap degerleri `MobileCastleCombatAuthoring` uzerinden tutulur. LevelUpPanel legacy olarak durabilir, fakat mobile loop'ta kullanilmaz.

World visual foundation main scene MonoBehaviour/Tilemap tarafinda ve owner kontrolundedir. Gorsel kale `(0,0)` gameplay center ile hizalanir. `inside/outside/outside2` tilemap'leri gorsel katmandir; yalniz `outside` tilemap'indeki dolu hucreler okcu spawn kaynagi olarak okunur. `outside2` front-wall/occluder katmanidir ve okculardan onde cizilmek icin z `-2` bandinda tutulur. Bir tilemap hem ust yurume yuzeyi hem on duvar yuzu icerirse partial occlusion beklenmez; front-wall pikselleri ayri ondeki tilemapte tutulmalidir.

Readability polish icin `MobileCastleArcherTilePlacement` Scene view'da `outside` spawn hucrelerini ve tekrar kullanim preview noktalarini cizer. `BasicArcher_01` beyaz tint ile normalize edilir; Basic/Rapid/Frost runtime tint'leri ECS tarafinda `SpriteTint` ile uygulanir. Archer count bilgisi sag drawer row'larinda okunur; eski `ArcherTypeText` placeholder'i mobile HUD'da kullanilmaz.

Wave/run loop icin drawer oyun akisi controller'i degil, yalnizca gorsel ve referans root'udur. Davranis `MarketUI`, `UIManager`, `GameManager` ve ECS `DayNightPrepSystem` tarafindan uygulanir. UI Importer JSON'u runtime event barindirmaz. Yeni UI ihtiyaci dogarsa implementer final JSON uretmez; owner'a ayri Codex tabinda mockup/preview icin prompt verilir.

Castle Yard prep aksiyonlari sag drawer'da player-facing degildir. Tool `RepairButton`, `FortifyButton`, `RallyButton`, `RefillArrowsButton` ve `StartNextWaveButton` bulursa gizler; sag drawer archer buy/upgrade ve tech unlock icin kullanilir. Repair aksiyonu `CastleRepairButton` ile Castle Interior paneline tasinmistir ve sadece `DayPrep` sirasinda aktif olur.

`DayNightOverlay` Canvas'in ilk child'i olarak kurulur. Full-screen siyah Image sadece world'u karartir; `MobileCastleHudRoot` sonradan geldigi icin HUD/drawer overlay'in ustunde kalir. Overlay alpha runtime'da `DayNightOverlayController` tarafindan mobile config'teki day/night alpha ve `WaveStateData.PrepTimer` degerlerine gore guncellenir.

`CombatFeedbackRoot`, `CombatFeedbackBridge` ile ECS feedback event'lerini hit flipbook, pooled ParticleSystem ve AudioSource playback'e cevirir. Tool `fanfx2_cure_small_red/spritesheet.png` flipbook frame'lerini, opsiyonel particle fallback'lerini, `Arrow & Bow*.wav` random shoot clip listesini ve `Rock Impact 37.wav` referansini baglar. Demo `FX_Shoot_Arrow.prefab` root'u kullanilmaz.

Mobile HUD economy varsayilanlari NewGameScene setup tarafindan GameStateAuthoring'e yazilir:

- Wood `150`, Stone `80`, Iron `45`, Food `150`, Population `60`, Arrows `200`
- Initial workers: Wood `20`, Stone `10`, Iron `8`, Food `15`
- Worker production: Wood `4.5/min`, Stone `3/min`, Iron `2/min`, Food `4/min`

Economy focus UI mobile worker economy ile kullanilmaz. Tool eski `EconomyFocusText` ve `EconomyBalanced/Wood/Stone/Iron/FoodButton` objelerini gizler. `CastleEconomyUI` isim tabanli olarak `CastleEconomyPanel`, `CastleTapHint`, worker slider/text alanlari, `WorkerBudgetText`, projected gain text'leri, `CastleRepairButton`, event secim butonlari ve `EconomyEventBadge` alanlarini baglar. Hint/badge feedback objelerinin raycast target'lari kapatilir; kaleye tiklamayi bloklamazlar. Runtime davranis UI JSON icine gomulmez.
