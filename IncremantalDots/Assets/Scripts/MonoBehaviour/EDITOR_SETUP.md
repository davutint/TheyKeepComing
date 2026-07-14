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
- Worker drawer: `WorkerEconomyDrawerPanel`, `WorkerDrawerToggleButton`, `WorkerIdlePopulationText`, `WorkerTotalText`, `WorkerArcherPopulationText`
- Worker rows: `WoodWorkerCountText`, `WoodWorkerRateText`, `WoodWorkerAddButton`, `WoodWorkerStatusText`; ayni pattern `Stone`, `Iron` ve `Food` icin
- Top center: `WaveText`, `KillsText`, `WaveRewardText`
- Defense module: `CastleDefensePanel`, `DefensePercentText`, `DefenseWallFill`, `DefenseGateFill`, `DefenseCoreFill`, `DefenseWallText`, `DefenseGateText`, `DefenseCoreText`
- Optional defense feedback: `DefenseDamageGlow`, `DefenseWarningIcon`, fallback `DefenseText`
- Feedback: `DamageFlashOverlay` full-screen red `Image`, `HUDController.DamageFlashImage` alanina baglanir
- Combat feedback: main scene'de `CombatFeedbackRoot` + `CombatFeedbackBridge` bulunur; setup tool arrow hit/frost VFX prefablarini, opsiyonel muzzle referansini ve arrow/castle SFX clip'lerini otomatik baglar. Shoot muzzle VFX V1'de oynatilmaz.
- Drawer: `ArcherDrawerPanel`, `DrawerToggleButton`
- Rows: `BasicArcherRow`, `RapidArcherRow`, `FrostArcherRow`
- Row fields: `BasicCountText`, `BasicDpsText`, `BasicLevelText`, `BasicCostText`, `BasicBuyButton`; ayni pattern `Rapid` ve `Frost` icin
- Legacy/hidden row fields: `BasicUpgradeButton`, `RapidUpgradeButton`, `FrostUpgradeButton`
- Legacy/hidden tech: `ArrowTechPanel`, `RapidTechUnlockButton`, `FrostTechUnlockButton`
- Prep: `RepairButton`, optional `RepairCostText`, `RepairStatusText`
- Castle Yard: `FortifyButton`, `FortifyCostText`, `FortifyStatusText`, `RallyButton`, `RallyCostText`, `RallyStatusText`
- `RefillArrowsButton` varsa mobile unlimited arrow modda gizlenir
- `StartNextWaveButton` varsa mobile otomatik day/night loop'ta gizlenir
- `Canvas/DayNightOverlay`: full-screen black `Image` + `DayNightOverlayController`

`Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab` UI'nin TEK dogruluk kaynagidir; UI dogrudan bu prefab uzerinde (prefab stage'de) uretilir/duzenlenir. Mobile Castle Scene Setup prefab varsa fallback HUD yerine onu kullanir. (Eski UIImporter/export pipeline'i 2026-07-06'da kaldirildi.)

Drawer gameplay'i pause etmez. Mobile castle loop'ta level-up paneli kullanilmaz; oyuncu surekli oldurur, kaynak toplar ve sag drawer'dan okcu satin alir. Archer recruitment row'lari `ArcherDefinitionSO` asset'lerini iceren `ArcherRecruitmentCatalogSO` catalog'undan uretilir; template yoksa eski Basic/Rapid/Frost row'lari fallback olarak calisir. Upgrade/unlock aksiyonlari sag drawer'da player-facing degildir; ileride full-screen Tech Tree tarafina tasinacaktir. Wave arasi `DayPrep` sayaci otomatik akar, ekran kararir ve sure bitince gece wave'i baslar. Kaynak yetmiyorsa mevcut row `CostText` alaninda `NEED ...`, idle population yoksa `NEED POP` gosterilir; Basic/Rapid/Frost toplam ortak cap'i `1000` olduğunda `ARMY CAP 1000/1000` ve `MAX` gösterilir. Bunun için yeni UI binding gerekmez.

`Repair`, `Fortify` ve `Rally` sag drawer'da player-facing olarak gizlenir. Archer buy combat sirasinda kullanilmaya devam eder; sag panel komple kilitlenmez. Castle Yard aksiyonlari polish prefabda yoksa tool yeni Fortify/Rally gorseli uretmez, sadece mevcut isimleri baglar.

Economy focus butonlari mobile continuous HUD'dan tamamen kaldirildi. Kaynak yonlendirme player-facing olarak sol worker drawer uzerinden yapilir; `EconomyFocusState` ve `EconomyFocusUI` sadece legacy/debug akislar icin kodda kalabilir.

Mobile unlimited arrow modunda HUD `ArrowText` degeri `INF` gosterir. Refill butonu player-facing UI'da kullanilmaz; `GameManager.RefillArrows()` legacy/debug API olarak kalabilir.

## Onemli
- Bu MonoBehaviour'lar ana scene'de bulunur (Sub Scene'de degil!)
- ECS entity'lerine erisim icin Sub Scene'in yuklu olmasi gerekir
- GameManager ilk birkac frame'de initialization bekler
- Script eklendikten sonra disaridan manuel compile komutu calistirma; Unity refresh/compile eder
