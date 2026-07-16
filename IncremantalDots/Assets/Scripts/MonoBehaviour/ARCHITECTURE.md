# MonoBehaviour Hybrid Layer - Mimari

## Genel Yapi

MonoBehaviour'lar ECS ile Unity UI arasinda kopru gorevi gorur. `World.DefaultGameObjectInjectionWorld.EntityManager` uzerinden ECS verilerine erisir.

## Dosyalar

### GameManager.cs

- Singleton pattern
- Her frame ECS singleton'larini okur: `GameStateData`, `WaveStateData`, `ContinuousSiegeCycleData`, `WallSegment`, resource ve population verileri
- Event'ler: `OnGameOver`, legacy `OnLevelUp`, `OnWaveChanged`, `OnGameStateChanged`
- `OnWaveCompleted` legacy wave cleared / market bekleme fazini UI katmanina bildirebilir; continuous siege varsayilaninda tetiklenmez
- Mobile ilk play'de baked aktif wave state'ini legacy DayPrep baslangicina normalize edebilir; continuous siege system bir sonraki frame aktif cycle'a ceker
- `StartNextWave()` debug/public API olarak kalir; mobile player-facing akis continuous day/dusk/night cycle ile ilerler
- `RepairDefenseFull()`, `BuyFortify()` ve `BuyRally()` legacy/debug API olarak kalir
- Finite Arrow API'leri: +1/+5/Buy Max Wood refill quote/transaction'ı, Wood+Iron Capacity/Efficiency quote/transaction'ı ve data-driven capacity/verim okuması
- `GetDefensePercent()` wall/gate/castle toplam HP yuzdesini HUD'a verir
- Mobile archer economy API'leri: `ArcherDefinitionSO` catalog'undan type-count scaled buy/retrain cost ve base stat okuma, buy, Basic -> Rapid/Frost in-place retrain, type count/DPS okuma; `GetTotalArcherCount`, `GetRemainingArcherCapacity` ve `CanAddArchers` Basic/Rapid/Frost ortak `1000` cap'ini sunar. Legacy unlock/upgrade API'leri kodda kalir ama sag drawer player-facing kullanmaz
- Legacy Tech Tree state/API (`_techNodeLevels`, `_revealedTechNodes`, `TryBuyTechNode`) migration/debug uyumlulugu icin kodda kalir; aktif `NewGameScene` HUD'inda `TechTreeUI` yoktur ve legacy catalog player-facing progression owner'i degildir
- Castle Heart runtime'i `GameManager.HeartRuntime.cs` partial'inda generated graph/reveal/presentation, Grave Essence-only quote/purchase ve actual effect adapter'larini birlestirir. Production `heartCatalog` null ise acik hata verir; legacy `TechTreeCatalogSO`'ya fallback yapmaz
- Run-only `GraveEssence` bakiyesi `GrantGraveEssence` ile artar ve yalniz `TrySpendGraveEssenceAtHeart` kapisindan azalir; exact save v11'de generated Heart graph ile birlikte korunur, Restart/Game Over'da silinir
- Continue saved Heart graph'i `CatalogVersion`/structural/runtime-state preflight'inden gecirir ve purchased level'lari deferred `HeartEffectPipeline` replay'iyle canli owner'lara uygular; v9 null-graph migration'i yeni graph uretmez
- Heart effect'leri Heart'siz baseline uzerine uygulanir: Basic/Rapid/Frost damage/fire-rate/range/Frost slow, tek Wall HP/repair, resource-specific worker capacity/production, population growth, Arrow capacity/efficiency ve Fireball damage/radius/cooldown. Arrow Heart bonuslari paid Arrow level'larindan ayri ECS alanlarinda tutulur
- Worker economy API'leri: `OpenCastleEconomy()`, `CloseCastleEconomy()`, `SetResourceWorkers()`, `ChooseEconomyEvent()`
- Worker bina yatırım API'leri: `GetWorkerBuildingUpgradeLevel()`, `GetWorkerBuildingUpgradeCost()`, `CanBuyWorkerBuildingUpgrade()` ve `TryBuyWorkerBuildingUpgrade()`; dört hazır binanın bağımsız Capacity/Efficiency seviyelerini baked `MobileEconomyPriceTuning` fiyatıyla Wood + Iron transaction'ı üzerinden büyütür. `ApplyTechEconomyAggregates()` base + Heart + Council + Meta + bina etkilerini tek owner'da birleştirir
- House bed API'leri: `GetTotalBedCapacity()`, `GetPurchasedBedCapacity()`, `GetBedCapacityPurchaseCost()`, `CanBuyBedCapacity()` ve `TryBuyBedCapacity()`; run-scoped `MobileBedCapacityState`, baked `MobileEconomyPriceTuning` base/interval değerleriyle toplam sahipliği `60` tabanından sonra quadratic büyüten ardışık Wood transaction'ıyla büyür ve güncel exact save içinde korunur. Mobile Dawn bed + Food kabul bütçesi bu state'i kapasite owner'ı olarak kullanır
- Dawn survivor görsel köprüsü: yeni persistent growth marker'ını ve gerçek accepted count'u gözler; mevcut `VillagerWorker` prefabından en fazla `15` transient arrival entity'si üretir, resource worker/logistics component'lerini kaldırır ve hareketi `SurvivorArrivalVisualSystem`'a bırakır. Population/Food transaction'ını tekrar yazmaz
- Economy focus API'leri legacy olarak kalir; worker economy aktifken setup tool focus UI'yi gizler
- Legacy level-up API'leri durur, fakat mobile castle loop'ta XP level-up pause tetiklemez
- Mobile castle mode'da drawer economy tarafindan satin alinan Basic/Rapid/Frost okculari `Grid/outside` tilemap hucrelerine spawn eder ve `1` idle population kullanir
- Mevcut `ArcherUnit` entity'lerinden Basic/Rapid/Frost sayilarini okur
- Bütün aktif spawn yollarini `SpawnArcher` merkezinde `ArcherCapacityUtility` ile sınırlar; 1001. entity oluşmaz
- Spawn edilen okcuya type-specific `SpriteTint` yazar
- Spawn edilen okculari varsayilan East facing idle state'iyle baslatir
- Type upgrade'leri mevcut ve gelecekte spawn olacak ayni tip okculara damage/fire-rate scaling uygular; bu akisin player-facing sahibi sag drawer degil, ileride full-screen Tech Tree olacaktir
- `RestartGame()` ile oyunu sifirlar

### HUDController.cs

- HP, XP, continuous cycle, zombie alive/max, resource, population ve arrow text'lerini gunceller
- Aktif generated HUD prefabinin dogrudan alt gorsel root'u `CanvasScaler` sanal alanina stretch olur; kritik HUD anchor'lari 16:9 ve `3440 x 1440` ultrawide'da ekran icinde kalir
- Mobile HUD'da resource text'leri label tekrar etmez; amount ve signed `/m` rate'i tek satırlı kompakt value olarak yazar
- `ResourceBar`, üst solda `560 x 48` tek şerittir; Wood/Stone/Iron/Food/Population/Arrow altı adet `84 x 42` chip içinde kalır ve label renkleri hızlı taramayı destekler
- Resource rate gosteriminde base production yerine effective production'i kullanir; mobile worker economy aktifken bu deger worker allocation'dan gelir
- Owner tarafindan secilen `B - Celestial Dial`, top-center anchor'li `290 x 68` gercek pill siluetli `CyclePanel` icinde yalniz `DAY N` sayacini player-facing tutar
- `CycleProgressMarker`, `CycleProgress01` ile `178 x 44` ve 44 segmentli sig yay uzerinde hareket eder; faz degisiminde marker/halo rengi `250 ms` crossfade yapar
- Sahne instance'inda yeni serialized referanslar yoksa `HUDController`, `CycleProgressTrack` ve `CycleCelestialGlow` isimlerini aktif prefab hiyerarsisinden bir kez bulup cache'ler; per-frame hiyerarsi taramasi yapmaz
- `CyclePhaseText`, uc ham `DAY / DUSK / NIGHT` label'i ve linear `CycleProgressFill` serialized uyumluluk icin korunur fakat Celestial Dial'da player-facing kapali kalir
- A alternatifi ve B karar sozlesmesi `Assets/Docs/DW_I_PHASE_HUD_PRESENTATION_DECISION.md` dosyasinda korunur
- Aktif HUD prefabinda ve `HUDController` sozlesmesinde `HordePressurePanel` ya da child binding'i bulunmaz; gameplay `HordePressure01` yogunluk verisi korunur fakat player-facing forecast uretilmez
- `CyclePanel` yoksa legacy wave fallback text'lerini kullanir
- `ArrowText`, finite stoku `Current / Capacity` biçiminde gösterir; `INF` modu yoktur
- Mobile HUD'da yalniz `DefenseWallFill` ve Wall yuzdesi guncellenir; aktif prefab ve `HUDController` legacy Gate/Core gorseli ya da binding'i tasimaz
- `WaveRewardText`, wave clear bonusunu kisa sure `Wave Cleared +...` olarak gosterir
- Night/high pressure baskisinda threat rengi kullanabilir; savunma hasarinda `DamageFlashImage` kisa red flash verir
- Archer count bilgisi sag drawer row'larinda okunur; mobile setup eski `ArcherTypeText` placeholder'ini kullanmaz
- Text alanlarini sadece deger degisince guncelleyerek gereksiz string allocation'i azaltir

### MobileCastleArcherTilePlacement.cs

- `NewGameScene` icindeki `Grid/outside` tilemapini okcu spawn kaynagi olarak kullanir.
- `ArcherFormationV1.asset` icindeki version'li tam 40 canonical hucreyi dogrular; rastgele ek dolu hucreleri formasyona katmaz.
- Her hucrede seeded blue-noise ile uretilen 25 diamond-inset noktayi layer sirasi ile duzler; toplam kapasite tam `1000` olur.
- Scene view'da sinirlanmis preview yerine formasyonun butun 1000 noktasini gizmo ile gosterir.
- Save world position yazmaz; `ArcherFormationVersion` ile ayni deterministik cache'i Continue sirasinda yeniden kurar.
- Okcu spawn Z degeri `MobileCastleRenderDepth.UnitZ` (`-1`) tutulur. Kale tilemap on/arka iliskisi world z bandlariyla cozulur: back tilemap `0`, unit `-1`, front occluder `-2`, projectile `-2.5`. `DeadWalls/SpriteSheet` shader'i Entities Graphics uyumlulugu icin `Opaque/Geometry` kalir; transparent queue veya depth yazimini kapatma bu Entities hattinda entity gorunurlugunu bozabilir.

### CombatFeedbackBridge.cs

- ECS `CombatVfxEvent` ve `CombatSfxEvent` entity'lerini okur, hit flipbook, pooled ParticleSystem ve AudioSource ile oynatir.
- Arrow/Frost hit feedback'i hafif sprite flipbook pool ile, castle hit ParticleSystem ile, shoot feedback'i random AudioSource pool ile yonetilir; shoot particle V1'de kapali tutulur.
- Stress mode'da event'leri temizleyip playback'i kapatabilir; bu sayede performans testleri VFX/SFX yukunden etkilenmez.

### LevelUpUI.cs

- Legacy kart panelidir.
- Mobile castle loop'ta kullanilmaz; okcu alma sag drawer recruitment uzerinden ilerler, upgrade/unlock ileride Tech Tree'ye tasinacaktir.

### MarketUI.cs

- `MobileCastleHudRoot` uzerindeki alt-sag `ArcherDrawerPanel` controller'idir
- HUD ve yeni run acilisinda drawer kapali baslar; sabit `ARCHERS` butonu kayan panelin disindadir
- Drawer combat sirasinda acilip kapanir; oyun pause olmaz. `OpenOnWaveCompleted` legacy wave-complete acilisini korur
- `ArcherRecruitmentListRoot` + inactive `ArcherRecruitmentRowTemplate` varsa satirlari `ArcherRecruitmentCatalogSO` definition listesinden runtime'da uretir
- Template yoksa legacy Basic/Rapid/Frost row'larinda yalnizca `Buy` aksiyonunu `GameManager.BuyArcher()` API'sine baglar
- Upgrade butonlari, Rapid/Frost tech unlock butonlari ve `ArrowTechPanel` player-facing olarak gizlenir
- Basic baslangicta aciktir; Rapid/Frost ileride Tech Tree tarafindan unlock edilecek kilitli satirlar olarak kalir
- Row `CostText` alanlarinda mevcut cost ile beraber eksik kaynak varsa `NEED ...`, idle population yoksa `NEED POP` yazar
- `GameManager.Free Economy Test Mode` acikken cost satirlari `FREE` gosterir; kaynak ve population yetersizligi player-facing aksiyonlari bloklamaz
- Free Economy Test Mode ortak `1000` cap'i bypass etmez; cap'te row `ARMY CAP 1000/1000` ve `MAX` gosterir
- Rapid/Frost unlock olduktan sonra `RETRAIN`, bir Basic entity'yi yerinde dönüştürür; toplam garnizon/population değişmez ve cap doluyken de çalışır
- Buy ve retrain maliyetleri hedef tür sayısına göre definition tuning'inden büyür; ayrı archer upgrade/level UI açılmaz
- Basarili player-facing buy action'i `ArcherPurchasedByPlayer` event'ini yayar; onboarding gibi presentation consumer'lari transaction'i tekrar etmeden bu event'i dinler
- Worker economy aktifken `Repair`, `Fortify` ve `Rally` player-facing drawer'da gizlenir; drawer archer recruitment paneli olarak kalir
- Legacy `Arrow Refill` kontrolü gizlenir; Arrow chip'i scene-owned `ArrowSupplyUI` tek satır panelini açar
- Mobile continuous siege loop'ta `Start Next Wave` player-facing UI'da gizlenir; oyun durmadan `DAY / DUSK / NIGHT` cycle'i akar
- Runtime davranisi prefaba gomulmez; controller ve scene setup tool tarafinda baglanir
  (UI dogrudan prefab uzerinde uretilir; eski UIImporter/export pipeline'i 2026-07-06'da kaldirildi)

### HeartScreenUI.cs

- Aktif fullscreen Castle Heart controller'idir; `HeartGraphPresentation` hidden-safe modelini cizer
- Alt-sag dock'taki sabit `CASTLE HEART` butonu fullscreen paneli acar; button Archer drawer'in hareketinden bagimsizdir
- Army/Defense/Production/Heart-Magic dallarini sag/sol/yukari/asagi compass layout ile yerlestirir
- `+1/+10/MAX`, exact Grave Essence quote, actual current/after/delta ve Keystone conflict bilgisini sunar
- Acilista `SimulationPauseService` lease'i alir; cycle/spawn/movement/combat/worker ve scaled cooldown'lar durur. UI refresh/animasyonlari unscaled zamanda calisir
- Aktif prefab `CastleHeart...`/`Heart...` isimlerini kullanir; node template lookup'i migration icin eski `TechNode...` child isimlerini de taniyabilir
- Otoriter dok: `HEART_SCREEN_ARCHITECTURE.md`

### ManagementDrawerCoordinatorUI.cs

- Workers/Housing, Archer Recruitment ve Arrow Supply yuzeylerinin tek exclusive owner'idir
- Yeni drawer claim edildiginde diger iki yuzeyi aninda kapatir; gameplay transaction veya presentation verisi yazmaz
- Castle Heart, Council, Pause ve Game Over modal akislarina sahip olmaz
- Scene-owned tek component'tir; aktif generated prefab runtime controller tasimaz
- Otoriter dok: `MANAGEMENT_DRAWER_COORDINATOR_ARCHITECTURE.md`

### FirstRunOnboardingUI.cs

- Package I ilk-kosu ogretiminin scene-owned, non-modal presentation sahibidir
- Ilk Day worker ratio adiminda drawer kapaliyken Workers/Housing toggle'ini, acikken ilk ratio kontrolunu pulse eder; tek satir English hint gosterir
- Basic Archer ilk kez gercekten satin alinabilir oldugunda drawer kapaliyken ARCHERS toggle'ini, acikken runtime Basic BUY kontrolunu pulse eder
- Gameplay transaction'i, otomatik drawer acma, resource harcama veya worker dagitma yapmaz
- Basarili gercek player ratio action'ini `WorkerEconomyDrawerUI` event'inden alir ve `tutorial.v1.worker_ratio` stable flag'ini canonical `MetaProgression` API'siyle durable yazar
- Basarili gercek Basic Archer satin alimini `MarketUI` event'inden alir ve `tutorial.v1.basic_archer` stable flag'ini durable yazar
- Otoriter dok: `FIRST_RUN_ONBOARDING_UI_ARCHITECTURE.md`

### TechTreeUI.cs

- Legacy sabit catalog UI controller'idir; aktif `NewGameScene` HUD instance'inda bulunmaz
- Save/migration ve eski scene uyumlulugu icin kodda kalir; yeni progression veya UI degisikligi burada yapilmaz

### TechTreeViewController.cs

- Tech tree viewport'unun pan/zoom controller'i; ScrollRect'in ustune eklenir (sol drag ScrollRect'te kalir)
- `TechTreeInputMode` enum (`Auto/Desktop/Mobile`): Desktop = tekerlek imlec-merkezli zoom + orta tus pan; Mobile = pinch zoom (orta-nokta merkezli) + tek parmak pan; Auto platforma gore secer
- Zoom `content.localScale` ile (layout sabit); alt sinir icerik viewport'a sigiyorsa 1'e clamp; pinch sirasinda ScrollRect gecici kapatilir

### CouncilComposer.cs + CouncilEventUI.cs

- Safak meclisi event'leri: kart DAWN'da belirir, DAY boyunca yasar, DUSK'ta expire; oyun durmaz
- Event'ler asset degil — `CouncilComposer` (pure static, EditMode testli) sablon x atom x baglam x olcek carpimindan uretir; deterministik (seed = hash(ECS RandomSeed, gun))
- Director: kit kaynak/dusuk savunma/bolluk baglamina gore atom-sablon agirliklari; hafiza: flag'ler + zincir sablonlari (RequiredFlags/ChainDelayDays/OneShot); butce: A/B secenekleri "dakika-degeri" cinsinden dengelenir
- Regular schedule tek owner'i `CouncilRegularSchedule`: Day `3,6,9,12...`; chance/pity/cooldown regular akis disinda. V1 regular-only'dir ve ikinci emergency meeting yolu yoktur. GameManager API: `TryOpenRegularCouncilEvent`, `ChooseCouncilOption`, `ExpireCouncilEvent`, `CanAffordCouncilOption`
- `CouncilOptionPresentationUtility` iki secenegi canli state'ten exact quote eder; `CouncilDecisionWindowUtility` kalan Dawn + Day suresini `DECIDE Ns` ve azalan Filled/Horizontal seride verir
- Exact save v13 `LastRegularCouncilDay`, `HasActiveCouncilEvent` ve Essence meta remainder'ını korur; v10 chance fail'i migration'da scheduled gunu tuketmez
- Otoriter dok: `COUNCIL_EVENTS_ARCHITECTURE.md`

### DefenseRepairUI.cs

- CastleDefensePanel'deki player-facing REPAIR butonunun controller'i (HUD root'ta ayri component)
- Tamir continuous siege sirasinda HER ZAMAN denenebilir (eski DayPrep sarti kaldirildi — continuous'ta olu yoldu)
- Maliyet kayip-orantili: `GameManager.GetRepairCost()` = `ceil(RepairBase * kayipOrani * techCarpani)`; taban config'te (120W/80S tam kayipta)
- `repair_efficiency` tech node'u (`ReduceRepairCostPercent`) maliyeti dusurur
- Basarida punch, reddetmede shake (DOTween); 0.25s poll ile cost etiketi/interactable

### DawnRewardToastUI.cs

- Faz DAWN'a gectiginde bir kez "DAWN — DAY n SURVIVED  +N POP" toast'u (SiegeToastText, DOTween fade)
- Nufus odulunu `MobilePopulationEconomySystem` verir; bu controller `GameManager.GetLastAcceptedPopulationArrivalCount()` ile config isteği yerine gerçek kabul edilen `N` değerini gösterir

### DayNightOverlayController.cs

- `Canvas/DayNightOverlay` full-screen black `Image` alpha degerini yonetir.
- Continuous siege aktifken Day alpha acik kalir, Dusk boyunca day/night alpha arasinda kararir, Night alpha sabit kalir.
- Legacy `DayPrep` sirasinda alpha'yi config'teki day/night degerleri arasinda sayac progress'ine gore artirir.
- Legacy `NightCombat` sirasinda night alpha sabit kalir.
- Stress veya non-mobile mode'da alpha `0` olur.

### EconomyFocusUI.cs

- Legacy controller'dir.
- Mobile worker economy aktifken setup tool economy focus panel/objelerini root'tan soker ve bu controller'i kullanmaz.
- Eski focus akisi, `MobilePopulationAllocation` bulunmayan mobile/legacy denemeler icin korunur.

### CastleEconomyUI.cs

- Legacy full-screen ekonomi panelidir.
- Mobile continuous worker drawer akisi aktifken `PlayerFacingPanelEnabled = false` kalir.
- `CastleEconomyPanel` ve `CastleTapHint` player-facing ana ekonomi akisi degildir.
- Slider/debug bindingleri korunabilir, fakat ana worker assignment UI'i `WorkerEconomyDrawerUI` tarafindadir.

### WorkerEconomyDrawerUI.cs

- Sol ust resource bar altindaki worker drawer'i yonetir.
- `WorkerDrawerToggleButton` ile drawer panelini acip kapatir.
- Idle pop, total worker, archer count ve resource worker rate alanlarini gunceller.
- Wood/Stone/Iron/Food `+1% / +10% / +100% / direct input` kontrollerini target ratio API'lerine baglar.
- Basarili target-ratio player action'inda `WorkerTargetRatioChangedByPlayer` event'i yayar; onboarding bu event'i dinler, drawer transaction sahibi degismez.
- Her resource satirindaki `CAP` ve `EFF` butonlarini bagimsiz worker bina yatirim API'lerine baglar; level ve bir sonraki Wood + Iron maliyetini butonda gosterir.
- Secilen exact hedef korunurken diger uc hedef deterministik yeniden dagilir; toplam `%100` kalir.
- Mevcut actual worker'lari aninda tasimaz; hedef yalniz sonraki yeni population dagitimini yonlendirir.
- DayPrep sartina bagli degildir; worker hedefi her zaman degistirilebilir.

### CastleInteriorClickTarget.cs

- Main scene'deki `CastleClickTarget` objesi uzerindedir.
- Legacy Castle Interior panel akisi icindir; yeni player-facing worker yonetimi sol drawer'dadir.
- Default click radius `2.0`; setup tool bunu gorsel kale footprint'ine gore normalize eder.
- UI ustune tiklamalari ignore eder ve `CastleEconomyUI.OpenFromCastle()` cagirir.

### ClickDamageHandler.cs

- Mouse click'i alir
- Dunya koordinatina cevirir
- En yakin zombi entity'sini bulur
- Dogrudan `ZombieStats.CurrentHP` degerini dusurur

### CameraSetup.cs

- Orthographic kamera ayari
- Size: `6`, Position: `(0, 0, -10)`

## Veri Akisi

```
ECS Systems -> Entity Data -> GameManager.ReadECSData() -> Events -> UI Controllers
Legacy UI Input -> GameManager.CanApplyUpgrade()/ApplyUpgrade() -> EntityManager.SetComponentData -> ECS
Archer Drawer Input -> GameManager.CanBuyArcher() -> ArcherCapacityUtility ortak 1000 cap -> resource/population transaction -> GameManager.SpawnArcher() son cap kontrolu -> ECS
Archer Retrain Input -> GameManager.CanRetrainBasicArcher() -> target-type scaled cost -> mevcut Basic ArcherUnit type/stat/tint in-place degisimi -> count refresh
Tech Tree Input -> GameManager.TryBuyTechNode() -> reveal/unlock state + MobileCastleCombatConfig/WallSegment/ArcherUnit yazimi -> ECS
Worker Drawer Input -> GameManager.Set/AdjustWorkerTargetRatioPercent() -> WorkerAllocationUtility -> MobilePopulationAllocation target -> sonraki population auto-allocation -> WorkerVisualRepresentationUtility -> temsili DOTS VillagerWorker count + exact weight -> animation/cargo/fener/delivery feedback
House Bed Purchase -> GameManager.TryBuyBedCapacity() -> MobileEconomyPriceTuning + MobileBedCapacityUtility owned-capacity sıralı fiyatı -> Wood transaction -> MobileBedCapacityState.PurchasedCapacity -> güncel exact save
Worker Building Purchase -> GameManager.TryBuyWorkerBuildingUpgrade() -> MobileEconomyPriceTuning fiyatı -> Wood + Iron transaction -> MobileWorkerBuildingUpgradeState -> base + Heart + Council + Meta + bina aggregate'i -> güncel exact save
Dawn accepted marker -> GameManager.SyncSurvivorArrivalVisualsIfNeeded() -> VillagerWorker tabanlı transient survivor entity'leri -> SurvivorArrivalVisualSystem -> Wall arkası varışta destroy
Legacy Castle Click -> CastleEconomyUI.OpenFromCastle() -> MobilePrepPauseState
Legacy Worker Slider Input -> GameManager.SetResourceWorkers() -> MobilePopulationAllocation -> WorkerVisualRepresentationUtility -> temsili DOTS VillagerWorker count + exact weight sync
Economy Event Input -> GameManager.ChooseEconomyEvent() -> Resources/Population/MobileEconomyEventState
Castle Interior Repair -> GameManager.RepairDefenseFull() -> EntityManager.SetComponentData -> ECS
DayNightOverlayController -> GameManager.WaveState + MobileCastleCombatConfig -> Overlay alpha
Mouse Click -> ClickDamageHandler -> EntityManager.SetComponentData -> ECS
```
