# MonoBehaviour - Editor Kurulum

## Scene Kurulumu (GameScene)

### GameManager
1. Bos GameObject olustur -> "GameManager"
2. GameManager component'ini ekle
3. DontDestroyOnLoad degil, scene'e bagli

### ClickDamageHandler
1. GameManager objesine veya ayri bir GameObject'e ekle
2. Ek ayar gerekmiyor

### Camera
1. Main Camera objesine CameraSetup ekle
2. Otomatik olarak orthographic ayarlari yapar
3. Manuel ayar: Orthographic Size = 6, Position = (0, 0, -10)

## NewGameScene Mobile HUD

`Window -> DeadWalls -> Mobile Castle Scene Setup` tool'u `Canvas/MobileCastleHudRoot` objesini, `HUDController`, `MarketUI` ve `WorkerEconomyDrawerUI` controller'larini idempotent olarak baglar. Legacy `EconomyFocusUI` mobile continuous loop'ta player-facing degildir ve setup tarafindan root'tan sokulur.

Beklenen referanslar:

- Economy: `WoodText`, `StoneText`, `IronText`, `FoodText`, `PopulationText`, `ArrowText`
- Compact top strip: `ResourceBar` (`560 x 48`) içinde `WoodChip`, `StoneChip`, `IronChip`, `FoodChip`, `PopulationChip`, `ArrowChip`; value/rate metni tek satırdır
- Worker drawer: `WorkerEconomyDrawerPanel`, `WorkerDrawerToggleButton`, `WorkerIdlePopulationText`, `WorkerTotalText`, `WorkerArcherPopulationText`
- Worker rows: `WoodWorkerCountText`, `WoodWorkerRateText`, `WoodWorkerAddButton`, `WoodWorkerStatusText`; ayni pattern `Stone`, `Iron` ve `Food` icin
- Top center: `WaveText`, `KillsText`, `WaveRewardText`
- Phase area: owner-secili Celestial Dial; top-center gercek pill `CyclePanel` (`290 x 68`), `CycleProgressTrack`/`CycleCelestialArc` (`178 x 44`), `CycleProgressMarker`, `CycleCelestialGlow`, crescent moon ve horizon-dawn glyph'leri. Legacy phase/label/fill objeleri player-facing kapali
- Pill kapaklari, hazir `Knob` sprite'inin gradient/seam artefact'ini tasimayan `Assets/UI/CelestialPillCircle.asset` flat shape'ini kullanir
- Defense module: `CastleDefensePanel`, `DefensePercentText`, `DefenseWallFill`, `DefenseWallText`; legacy Gate/Core gorseli ve binding'i yoktur
- Optional defense feedback: `DefenseDamageGlow`, `DefenseWarningIcon`, fallback `DefenseText`
- Feedback: `DamageFlashOverlay` full-screen red `Image`, `HUDController.DamageFlashImage` alanina baglanir
- Combat feedback: main scene'de `CombatFeedbackRoot` + `CombatFeedbackBridge` bulunur; setup tool arrow hit/frost VFX prefablarini, opsiyonel muzzle referansini ve arrow/castle SFX clip'lerini otomatik baglar. Shoot muzzle VFX V1'de oynatilmaz.
- Bottom-right Archer surface: `ArcherDrawerPanel` (`540 x 350`, `(-24,160)`), sabit `DrawerToggleButton` (`156 x 56`, `(-190,28)`, label `ARCHERS`)
- Rows: `BasicArcherRow`, `RapidArcherRow`, `FrostArcherRow`
- Row fields: `BasicCountText`, `BasicDpsText`, `BasicLevelText`, `BasicCostText`, `BasicBuyButton`; ayni pattern `Rapid` ve `Frost` icin
- Legacy/hidden row fields: `BasicUpgradeButton`, `RapidUpgradeButton`, `FrostUpgradeButton`
- Legacy/hidden tech: `ArrowTechPanel`, `RapidTechUnlockButton`, `FrostTechUnlockButton`
- Castle Heart: sabit alt-sag `CastleHeartOpenButton` (`156 x 56`, `(-24,28)`, label `CASTLE HEART`), `CastleHeartPanel`, `CastleHeartCloseButton`,
  `HeartViewport`, `HeartContent`, `HeartNodeTemplate`, `HeartConnectionTemplate`,
  `GraveEssenceText`, `HeartScreenStatusText`, `HeartBranchCompassText`,
  `HeartQuantityOneButton`, `HeartQuantityTenButton`, `HeartQuantityMaxButton`,
  `CastleHeartBadge`, `CastleHeartToastText`
- Prep: `RepairButton`, optional `RepairCostText`, `RepairStatusText`
- Castle Yard: `FortifyButton`, `FortifyCostText`, `FortifyStatusText`, `RallyButton`, `RallyCostText`, `RallyStatusText`
- Legacy `RefillArrowsButton` varsa gizlenir; `AmmoPurchasePanel` ve `ArrowSupplyUI` binding'leri `ARROW_AMMO_EDITOR_SETUP.md` ile kurulur
- `StartNextWaveButton` varsa mobile otomatik day/night loop'ta gizlenir
- `Canvas/DayNightOverlay`: full-screen black `Image` + `DayNightOverlayController`

`Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab` UI'nin TEK dogruluk kaynagidir; UI dogrudan bu prefab uzerinde (prefab stage'de) uretilir/duzenlenir. Mobile Castle Scene Setup prefab varsa fallback HUD yerine onu kullanir. (Eski UIImporter/export pipeline'i 2026-07-06'da kaldirildi.)

Aktif scene HUD owner'i `HeartScreenUI`dir; `TechTreeUI` bulunmamalidir. Heart acilinca
`SimulationPauseService` DOTS SimulationSystemGroup ve time scale'i lease ile durdurur.
`GameManager.heartCatalog` owner-onayli production catalog gelene kadar null kalabilir; bu
durumda panel acik hata gosterir ve legacy catalog'a fallback yapmaz. Ayrintili binding/QA:
`HEART_SCREEN_EDITOR_SETUP.md`.

Drawer gameplay'i pause etmez ve HUD acilisinda kapali baslar. Mobile castle loop'ta level-up paneli kullanilmaz; oyuncu surekli oldurur, kaynak toplar ve sag drawer'dan okcu satin alir veya acilmis Rapid/Frost'a Basic retrain eder. Archer recruitment row'lari `ArcherDefinitionSO` asset'lerini iceren `ArcherRecruitmentCatalogSO` catalog'undan uretilir; template yoksa eski Basic/Rapid/Frost row'lari fallback olarak calisir. Upgrade/unlock aksiyonlari sag drawer'da player-facing degildir; Castle Heart tek progression owner'idir. Dynamic template `ArcherRetrainButton/ArcherRetrainButtonText` binding'ini tasir; eksikse `Window -> DeadWalls -> Repair Archer Retrain Control` prefabi idempotent onarir. Kaynak yetmiyorsa row `CostText` alaninda `NEED ...`, idle population yoksa buy icin `NEED POP` gosterilir; Basic/Rapid/Frost toplam ortak cap'i `1000` olduğunda buy `MAX` olur, fakat toplamı değiştirmeyen retrain açık kalabilir.

`Repair`, `Fortify` ve `Rally` sag drawer'da player-facing olarak gizlenir. Archer buy combat sirasinda kullanilmaya devam eder; sag panel komple kilitlenmez. Castle Yard aksiyonlari polish prefabda yoksa tool yeni Fortify/Rally gorseli uretmez, sadece mevcut isimleri baglar.

Economy focus butonlari mobile continuous HUD'dan tamamen kaldirildi. Kaynak yonlendirme player-facing olarak sol worker drawer uzerinden yapilir; `EconomyFocusState` ve `EconomyFocusUI` sadece legacy/debug akislar icin kodda kalabilir.

HUD `ArrowText` finite stoku `Current / Capacity` gösterir. Refill player-facing olarak
Arrow chip'inden açılan `ArrowSupplyUI` panelindedir; legacy `RefillArrowsButton` ve
Fletcher/queue akışı kullanılmaz.

## Onemli
- Bu MonoBehaviour'lar ana scene'de bulunur (Sub Scene'de degil!)
- ECS entity'lerine erisim icin Sub Scene'in yuklu olmasi gerekir
- GameManager ilk birkac frame'de initialization bekler
- Script eklendikten sonra disaridan manuel compile komutu calistirma; Unity refresh/compile eder
