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

`Window -> DeadWalls -> Mobile Castle Scene Setup` tool'u `Canvas/MobileCastleHudRoot` objesini, `HUDController`, `MarketUI` ve `EconomyFocusUI` controller'larini idempotent olarak baglar.

Beklenen referanslar:

- Economy: `WoodText`, `StoneText`, `IronText`, `FoodText`, `PopulationText`, `ArrowText`
- Economy Focus: `EconomyFocusText`, `EconomyBalancedButton`, `EconomyWoodButton`, `EconomyStoneButton`, `EconomyIronButton`, `EconomyFoodButton`
- Optional Focus Highlight: `EconomyBalancedSelected`, `EconomyWoodSelected`, `EconomyStoneSelected`, `EconomyIronSelected`, `EconomyFoodSelected`
- Top center: `WaveText`, `KillsText`, `WaveRewardText`
- Defense module: `CastleDefensePanel`, `DefensePercentText`, `DefenseWallFill`, `DefenseGateFill`, `DefenseCoreFill`, `DefenseWallText`, `DefenseGateText`, `DefenseCoreText`
- Optional defense feedback: `DefenseDamageGlow`, `DefenseWarningIcon`, fallback `DefenseText`
- Feedback: `DamageFlashOverlay` full-screen red `Image`, `HUDController.DamageFlashImage` alanina baglanir
- Combat feedback: main scene'de `CombatFeedbackRoot` + `CombatFeedbackBridge` bulunur; setup tool arrow hit/frost VFX prefablarini, opsiyonel muzzle referansini ve arrow/castle SFX clip'lerini otomatik baglar. Shoot muzzle VFX V1'de oynatilmaz.
- Drawer: `ArcherDrawerPanel`, `DrawerToggleButton`
- Rows: `BasicArcherRow`, `RapidArcherRow`, `FrostArcherRow`
- Row fields: `BasicCountText`, `BasicDpsText`, `BasicLevelText`, `BasicCostText`, `BasicBuyButton`, `BasicUpgradeButton`; ayni pattern `Rapid` ve `Frost` icin
- Tech: `RapidTechUnlockButton`, `FrostTechUnlockButton`
- Prep: `RepairButton`, optional `RepairCostText`, `RepairStatusText`
- Castle Yard: `FortifyButton`, `FortifyCostText`, `FortifyStatusText`, `RallyButton`, `RallyCostText`, `RallyStatusText`
- `RefillArrowsButton` varsa mobile unlimited arrow modda gizlenir
- `StartNextWaveButton` varsa mobile otomatik day/night loop'ta gizlenir
- `Canvas/DayNightOverlay`: full-screen black `Image` + `DayNightOverlayController`

Generated UI kullanilacaksa owner tarafindan onaylanan UI Importer export'u `Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab` olarak import edilir. Sonra Mobile Castle Scene Setup tekrar calistirilir; prefab varsa fallback HUD yerine onu kullanir. Implementer kendi basina UI JSON/export uretmez.

Drawer gameplay'i pause etmez. Mobile castle loop'ta level-up paneli kullanilmaz; oyuncu surekli oldurur, kaynak toplar, drawer'dan okcu alir veya upgrade eder. Wave arasi `DayPrep` sayaci otomatik akar, ekran kararir ve sure bitince gece wave'i baslar. Kaynak yetmiyorsa mevcut row `CostText` alaninda `NEED ...` gosterilir; bunun icin yeni UI binding gerekmez.

`Repair`, `Fortify` ve `Rally` sadece `DayPrep` sirasinda aktif olur. Archer buy/upgrade, tech unlock ve economy focus combat sirasinda kullanilmaya devam eder; sag panel komple kilitlenmez. Castle Yard aksiyonlari polish prefabda yoksa tool yeni Fortify/Rally gorseli uretmez, sadece mevcut isimleri baglar.

Economy focus butonlari combat sirasinda kullanilabilir. Balanced seciliyken tum passive/reward akisi hafif boost alir; Wood/Stone/Iron/Food seciliyken secili kaynak pasif uretim, kill reward ve wave clear bonus tarafinda daha guclu akar.

Mobile unlimited arrow modunda HUD `ArrowText` degeri `INF` gosterir. Refill butonu player-facing UI'da kullanilmaz; `GameManager.RefillArrows()` legacy/debug API olarak kalabilir.

## Onemli
- Bu MonoBehaviour'lar ana scene'de bulunur (Sub Scene'de degil!)
- ECS entity'lerine erisim icin Sub Scene'in yuklu olmasi gerekir
- GameManager ilk birkac frame'de initialization bekler
- Script eklendikten sonra disaridan manuel compile komutu calistirma; Unity refresh/compile eder
