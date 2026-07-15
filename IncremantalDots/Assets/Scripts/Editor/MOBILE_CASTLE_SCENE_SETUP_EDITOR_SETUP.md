# Mobile Castle Scene Setup - Editor Kurulum

## Kullanma

1. Unity Editor'de projeyi ac.
2. Ust menuden `Window -> DeadWalls -> Mobile Castle Scene Setup` penceresini ac.
3. `Setup NewGameScene` butonuna bas.
4. Tool gerekirse `Assets/Scenes/NewGameScene.unity` sahnesini acar.
5. Kurulum bittiginde `NewGameScene` kaydedilir ve `Assets/Scenes/NewGameScene/MobileCastleCombatSubScene.unity` olusur.

## Beklenen Hierarchy

Ana scene:

- `Main Camera`
- `Global Light 2D`
- `EventSystem`
- `Canvas`
  - `DayNightOverlay`
  - `MobileCastleHudRoot`
- `GameManager`
- `CombatFeedbackRoot`
- `CastleClickTarget`
- `CastleInteriorEconomyArea`
  - `CastleWorkerHub/DeliveryPoints`
  - `WoodSite/WorkerSpawnPoints`
  - `StoneSite/WorkerSpawnPoints`
  - `IronSite/WorkerSpawnPoints`
  - `FoodSite/WorkerSpawnPoints`
- `Grid` owner tarafindan kurulan world visual root'u olabilir
  - `inside`
  - `outside0`
  - `outside`
  - `outside2`
  - `Grass`
  - `Tree`
- `MobileCastleCombatSubScene`

SubScene:

- `GameState`
- `CastleCore`
- `MobileCastleConfig`
  - `MobileCastleCombatAuthoring`
- `BasicArcher_01`

## Inspector Kontrolu

- `Main Camera`: Orthographic, size `8`, position `(0, 0, -10)`
- `Canvas`: Scale With Screen Size, reference `1920 x 1080`, match `0.5`
- `WaveConfigAuthoring`: `Zombie`, `Arrow`, `Archer`, `Worker` prefab referanslari dolu
- `VillagerWorker.prefab`: `Villager.mat` + `Character_villager/Idle.png`, `SpriteSheetAuthoring` rows `8`, columns `15`, `VillagerWorkerAuthoring` ekli
- `MobileCastleCombatAuthoring`: castle center `(0, 0)`, spawn radius `11`, attack radius `1.35`, wave enemy count `30`, wave basi `+10`, spawn batch `3`
- `MobileCastleCombatAuthoring`: zombie scale `1.4`, base speed `0.85`, speed per wave `0.04`, stress batch `25`, stress interval `0.1`, stress max alive `1500`
- `MobileCastleCombatAuthoring`: kill reward Wood/Food/Stone/Iron `1 / 0.6 / 0.25 / 0.15`, wave scale `0.05`
- `MobileCastleCombatAuthoring`: wave clear bonus base Wood/Food/Stone/Iron `20 / 15 / 10 / 6`, per wave `6 / 5 / 4 / 3`
- `MobileCastleCombatAuthoring`: worker economy population growth `15`, initial workers Wood/Stone/Iron/Food `20 / 10 / 8 / 15`
- `MobileCastleCombatAuthoring`: worker caps Wood/Stone/Iron/Food `40 / 30 / 24 / 40`
- `MobileCastleCombatAuthoring`: worker production Wood/Stone/Iron/Food `8 / 5.5 / 3.8 / 7` per minute, reward multiplier `0.25`
- `MobileCastleCombatAuthoring`: economy event chance `0.15`, cooldown `2` waves
- `MobileCastleCombatAuthoring`: continuous siege enabled, total cycle `60`, day/dusk/night/dawn `30 / 5 / 20 / 5`, intensity `0.55 / 1.00->1.35 / 1.65`
- `MobileCastleCombatAuthoring`: legacy initial day prep `12`, day prep `15`, day/night overlay alpha `0 / 0.50`; unlimited Arrow alani yoktur
- `MobileCastleCombatAuthoring`: wave director base interval `0.8`, wave multiplier `0.96`, min interval `0.35`
- `MobileCastleCombatAuthoring`: opening/final ratio `0.20 / 0.20`, interval multiplier `1.35 / 0.65`, batch delta `-1 / +1`
- `MobileCastleCombatAuthoring`: Castle Yard defaults Fortify damage multiplier `0.70`, Rally duration `10`, Rally fire-rate multiplier `1.25`
- `MobileCastleCombatAuthoring.ArcherSlots`: mobile tilemap spawn akisi tarafindan kullanilmaz; bos kalabilir
- `MobileCastleArcherTilePlacement`: main scene `Grid` uzerinde bulunur; `outside` tilemapini ve `ArcherFormationV1.asset` tanimini kullanir, `40` hucre x `25` seeded nokta ile tam `1000` kapasite kurar
- `LevelUpUI`: legacy paneldir; mobile loop'ta acilmaz
- `MobileCastleHudRoot`: generated prefab varsa `Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab` instancelanir; yoksa fallback HUD/drawer kurulur
- `HUDController`: `WoodText`, `StoneText`, `IronText`, `FoodText`, `PopulationText`, `ArrowText`, `WaveRewardText`, `DamageFlashImage` ve varsa cycle/defense module alanlari bagli
- `ResourceBar`: üst solda `560 x 48`; altı resource/population/Arrow chip'i `84 x 42`, value/rate tek satır ve `ArrowChip` finite ammo toggle olmaya devam eder
- `HUDController` cycle module: `CyclePanel`, `CyclePhaseText`, `CycleDayLabelText`, `CycleDuskLabelText`, `CycleNightLabelText`, `CycleProgressFill`, `CycleProgressMarker`
- `CyclePanel`: top-center anchor, `340 x 78`; `280 x 10` progress track ve butun mevcut phase binding'leri korunur. Ham label visual polish'i ayri Package I isidir
- `HordePressurePanel` prefabda varsa player-facing olarak kapali tutulur
- `HUDController` defense module: `DefensePercentText`, `DefenseWallFill`, `DefenseWallText`, opsiyonel `DefenseDamageGlow`; legacy Gate/Core alanlari prefabda ve controller binding'inde bulunmaz
- `MarketUI`: `ArcherDrawerPanel`, `DrawerToggleButton`, Basic/Rapid/Frost row text, buy ve dynamic `ArcherRetrainButton` alanlari bagli
- `MarketUI`: Upgrade butonlari, `ArrowTechPanel`, tech unlock butonlari ve repair/prep butonlari prefabda varsa gizli
- `HeartScreenUI`: `CastleHeartPanel`, open/close, viewport/content, node/connection template,
  Grave Essence, status, compass, `+1/+10/MAX`, badge ve toast alanlari bagli; `TechTreeUI` yok
- `CastleEconomyUI`: legacy full-screen panel bindingleri bagli kalabilir ama `PlayerFacingPanelEnabled = false`; `CastleEconomyPanel` ve `CastleTapHint` player-facing kapali tutulur
- `WorkerEconomyDrawerUI`: sol ust worker drawer toggle/panel, summary alanlari, Wood/Stone/Iron/Food target kontrolleri ve `CAP / EFF` bina yatirim butonlari bagli
- `CastleInteriorWorkerPlacement`: Wood/Stone/Iron/Food pickup root'lari ve `CastleWorkerHub/DeliveryPoints` delivery root'u bagli
- `GameManager`: test icin `Free Economy Test Mode` acilirsa archer buy ve legacy/debug upgrade/unlock/prep API'leri kaynak/population harcamadan calisir; sag drawer player-facing yalnizca buy kullanir
- `CastleTapHint`, `EconomyEventBadge` ve opsiyonel glow objelerinin raycast target'lari kapatilir; eski castle tap akisi player-facing kullanilmaz
- `CombatFeedbackBridge`: `fanfx2_cure_small_red/spritesheet.png` hit flipbook frame'leri, opsiyonel particle fallback referanslari, `Arrow & Bow*.wav` random shoot clip listesi, `Rock Impact 37.wav`, pool/rate limit defaultlari ve `DisableInStressMode` bagli. Shoot muzzle VFX V1'de event uretmedigi icin oynatilmaz.
- `CastleClickTarget`: position `(0,0,0)`, `CastleInteriorClickTarget.ClickRadius` `2.0`
- `Grid/outside`: Formation V1'de data olarak sabitlenen tam 40 canonical hucreyi tasir; her hucrenin 25 diamond-inset noktasi algoritmayla uretilir, `inside` ve `outside2` sadece kale gorsel katmanidir
- Castle tilemap render bandlari: `inside` Wall/1/z0, `outside0` Wall/2/z0, `outside` Wall/2/z0, `outside2` Wall/4/z-2; `Archer.prefab` Wall/3 ve runtime z `-1` bandinda olmalidir
- Economy focus objeleri varsa gizli kalir; yeni ekonomi kontrolu sol ust `WorkerEconomyDrawerUI` panelindedir
- `DayNightOverlay`: Canvas'in ilk child'i, full-screen siyah `Image`, raycast target kapali, `DayNightOverlayController.OverlayImage` bagli
- `GameStateAuthoring`: mobile kaynak baslangici `280/120/70/220`, initial population `60`, initial workers `53`, initial archers `4`, initial arrows `200`
- `BasicArcher_01`: legacy/seed basic okcudur; Play modunda runtime initial basic archer sayisi 4'e tamamlanir ve `Grid/outside` tilemapindeki ilk spawn noktalarina yerlestirilir
- `BasicArcher_01`: `ArcherAuthoring.Tint` ve `SpriteSheetAuthoring.Tint` beyaz
- `MobileCastleCombatSubScene`: Scene Asset alani `MobileCastleCombatSubScene.unity`

## HUD Prefab Isim Sozlesmesi

UI dogrudan `Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab` uzerinde (prefab stage'de)
uretilir/duzenlenir; bu prefab TEK dogruluk kaynagidir. (Eski Codex export -> UI Importer
pipeline'i 2026-07-06'da kaldirildi.) Setup tool asagidaki isimleri exact-match ile bulur/baglar:

- Hedef prefab: `Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab`
- Beklenen root: `MobileCastleHudRoot`
- Beklenen drawer: `ArcherDrawerPanel`
- Beklenen drawer title: `DrawerTitleText` (`ARCHER RECRUITMENT` olarak normalize edilir)
- Economy focus objeleri artik opsiyoneldir ve setup tool tarafindan gizlenir
- Beklenen Worker Drawer: `WorkerDrawerToggleButton`, `WorkerEconomyDrawerPanel`, `WorkerDrawerTitleText`, `WorkerIdlePopulationText`, `WorkerTotalText`, `WorkerArcherPopulationText`
- Beklenen Worker Drawer row alanlari: `WoodWorkerCountText`, `WoodWorkerRateText`, `WoodWorkerAddButton`, `WoodWorkerTargetPlus10Button`, `WoodWorkerTargetPlus100Button`, `WoodWorkerTargetInput`, `WoodWorkerStatusText`, `WoodCapacityUpgradeButton`, `WoodEfficiencyUpgradeButton`; ayni pattern `Stone`, `Iron`, `Food`
- `Window > DeadWalls > Repair Worker Drawer Target Controls`, generated prefabdaki sekiz bina yatirim butonunu idempotent kurar ve aktif `NewGameScene` runtime component referanslarini baglar.
- Legacy Castle Interior paneli varsa baglanabilir ama player-facing ana ekonomi UI'i degildir: `CastleEconomyPanel`, `CastleInteriorImage`, `CloseCastleEconomyButton`, `ConfirmCastleEconomyButton`, `CastleTapHint`, `CastleTapHintText`
- Beklenen population alanlari: `PopulationTotalText`, `PopulationIdleText`, `PopulationArchersText`, `PopulationGrowthText`, `WorkerBudgetText`
- Beklenen worker alanlari: `WoodWorkerSlider`, `StoneWorkerSlider`, `IronWorkerSlider`, `FoodWorkerSlider`, `WoodWorkerText`, `StoneWorkerText`, `IronWorkerText`, `FoodWorkerText`, `WoodRateText`, `StoneRateText`, `IronRateText`, `FoodRateText`
- Beklenen projected gain alanlari: `ProjectedIncomeText`, `ProjectedWoodText`, `ProjectedStoneText`, `ProjectedIronText`, `ProjectedFoodText`
- Beklenen Castle Interior repair alanlari: `CastleRepairButton`, `CastleRepairStatusText`, opsiyonel `CastleRepairCostText`
- Beklenen event alanlari: `EconomyEventPanel`, `EconomyEventTitleText`, `EconomyEventDescriptionText`, `EconomyEventChoiceAButton`, `EconomyEventChoiceBButton`, `EconomyEventChoiceAText`, `EconomyEventChoiceBText`, `EconomyEventBadge`, `EconomyEventBadgeText`
- Beklenen readability alanlari: `WaveRewardText`, `DefensePercentText`, `DefenseWallFill`, `DefenseWallText`
- Beklenen continuous cycle alanlari: `CyclePanel`, `CyclePhaseText`, `CycleDayLabelText`, `CycleDuskLabelText`, `CycleNightLabelText`, `CycleProgressFill`, `CycleProgressMarker`
- Opsiyonel defense feedback: `DefenseDamageGlow`, `DefenseWarningIcon`, fallback `DefenseText`
- Opsiyonel Castle Interior feedback: `CastleTapHintPulse`, `ProjectedIncomeFrame`, `CastleRepairFrame`, `EconomyEventGlow`
- Sag drawer archer buy ve Basic -> Rapid/Frost retrain icindir. Dynamic satirlarda `ArcherRetrainButton` kullanilir; legacy `Basic/Rapid/FrostUpgradeButton` kontrolleri setup sirasinda player-facing olarak gizli kalir ve yalniz dynamic template bulunamazsa Rapid/Frost retrain fallback'i olabilir. `ArrowTechPanel`, `RapidTechUnlockButton`, `FrostTechUnlockButton`, `RepairButton`, `FortifyButton`, `RallyButton`, `RefillArrowsButton` ve `StartNextWaveButton` prefabda varsa setup tool bunlari player-facing olarak gizler.
- Beklenen Castle Heart alanlari: `CastleHeartOpenButton`, `CastleHeartPanel`,
  `CastleHeartCloseButton`, `HeartViewport`, `HeartContent`, `HeartNodeTemplate`,
  `HeartConnectionTemplate`, `GraveEssenceText`, `HeartScreenStatusText`,
  `HeartBranchCompassText`, `HeartQuantityOneButton`, `HeartQuantityTenButton`,
  `HeartQuantityMaxButton`, `CastleHeartBadge`, `CastleHeartToastText`.

Runtime davranisi prefab icinde degildir; `MarketUI` ve scene setup tool baglar.

Mobile continuous siege loop'ta player-facing `StartNextWaveButton` yoktur. Legacy
`RefillArrowsButton` gizli kalır; finite refill, Arrow chip'inden açılan
`AmmoPurchasePanel` + scene-owned `ArrowSupplyUI` üzerinden çalışır.

`CastleRepairButton` legacy Castle Interior akisi icindir. Continuous siege varsayilaninda Castle Interior panel player-facing kapali kalir; sag drawer'in archer buy ve Basic -> Rapid/Frost retrain aksiyonlari combat sirasinda kullanilmaya devam eder. Stat upgrade ve tech unlock aktif full-screen Castle Heart owner'indadir. Production Heart catalog owner onayi bekler; setup tool icerik uydurmaz. Castle Interior paneli yoksa setup tool polish fallback uretmez; panel gerekirse dogrudan prefabda kurulur.

## World Visuals

World visuals owner tarafindan `NewGameScene` icinde kurulur. Setup tool artik world visual tilemap uretmez, boyamaz veya tasimaz; yalniz `Grid/outside` tilemapini bulursa `MobileCastleArcherTilePlacement` controller'ina baglar. Kale icin bilinen tilemap renderer sorting order'larini ve world z bandlarini normalize edebilir, tile icerigine dokunmaz.

Onerilen tile aileleri:

- Arena/avlu zeminleri: `Ground A...`, `Ground B...`, `Ground G...`
- Kale footprint'i: `Wall A...`, `Wall D...`
- Gate gorseli: `Door C1_S`
- Hafif dekor: `BrokenStone...`, `BrokenWallStone1`, `Tree Shadow`

Bu gorsel katman wall/gate/core HP verisi degildir; HP hala ECS `CastleAuthoring` ve runtime component'lerinden gelir. Okcu spawn icin tek istisna `outside` tilemapidir: Formation V1 asset'indeki 40 hucre, layer sirali 25'er local noktanin world-space yuzeyidir. Tile merkezini sinirsiz tekrar kullanan eski stack davranisi yoktur.

## Bilerek Yapilmayanlar

- Yeni coin eklenmez; Wood/Stone/Iron/Food/Population/Arrows mevcut resource akisini kullanir.
- Eski town-building/grid UI bu sahneye tasinmaz.

Scene setup tool slot objesi veya world visual tilemap uretmez; mobile HUD/drawer controller referanslarini baglar ve main scene `Grid/outside` tilemapini version'li `ArcherFormationV1.asset` ile `MobileCastleArcherTilePlacement` uzerinde birlestirir. Yalniz formation onarimi icin `Window -> DeadWalls -> Repair Archer Formation V1` kullanilabilir. Mobile gameplay artik level-up paneliyle durmaz.

Script eklendikten sonra disaridan manuel compile komutu calistirma. Unity refresh sonrasi scriptleri kendisi derler.
