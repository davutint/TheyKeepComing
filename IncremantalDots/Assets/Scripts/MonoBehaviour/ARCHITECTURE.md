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
- `RepairDefenseFull()`, `BuyFortify()`, `BuyRally()` ve `RefillArrows()` legacy/debug API olarak kalir
- `GetDefensePercent()` wall/gate/castle toplam HP yuzdesini HUD'a verir
- Mobile archer economy API'leri: `ArcherDefinitionSO` catalog'undan buy cost/base stat okuma, buy, type count/DPS okuma; legacy unlock/upgrade API'leri kodda kalir ama sag drawer player-facing kullanmaz
- Tech Tree runtime state'i (run-scoped, persistence yok): `_techNodeLevels` + `_revealedTechNodes`; katalog `techTreeCatalog` (`TechTreeCatalogSO`, setup tool baglar). API: `IsTechNodeRevealed`, `GetTechNodeLevel`, `CanBuyTechNode(node, out reason)`, `TryBuyTechNode`, `GetRevealedTechNodes`. Root (`castle_heart`) otomatik sahipli baslar; satin alma `RevealChildNodeIds`'i gorunur yapar
- Tech effect'leri: `UnlockArcherType` (maliyetsiz icsel unlock — `UnlockArcherType()` cagrilmaz, cift harcamayi onler), damage/firerate carpanlari (`GetScaledArcherStats` + `ApplyScaledStatsToArchers`), worker cap / production / population growth (`MobileCastleCombatConfig`'e base'ten yeniden hesaplanarak yazilir), tek Wall MaxHP (CurrentHP orani korunur). Base degerler ilk dokunusta cache'lenir; `RestartGame()` -> `ResetTechTreeState()` hepsini base'e dondurur
- Worker economy API'leri: `OpenCastleEconomy()`, `CloseCastleEconomy()`, `SetResourceWorkers()`, `ChooseEconomyEvent()`
- House bed API'leri: `GetTotalBedCapacity()`, `GetPurchasedBedCapacity()`, `GetBedCapacityPurchaseCost()`, `CanBuyBedCapacity()` ve `TryBuyBedCapacity()`; run-scoped `MobileBedCapacityState`, toplam sahipliği `60` tabanından sonra quadratic büyüten ardışık Wood transaction'ıyla büyür ve exact save `v5` içinde korunur. Dawn arrival bağlantısı sonraki Package C işidir
- Economy focus API'leri legacy olarak kalir; worker economy aktifken setup tool focus UI'yi gizler
- Legacy level-up API'leri durur, fakat mobile castle loop'ta XP level-up pause tetiklemez
- Mobile castle mode'da drawer economy tarafindan satin alinan Basic/Rapid/Frost okculari `Grid/outside` tilemap hucrelerine spawn eder ve `1` idle population kullanir
- Mevcut `ArcherUnit` entity'lerinden Basic/Rapid/Frost sayilarini okur
- Spawn edilen okcuya type-specific `SpriteTint` yazar
- Spawn edilen okculari varsayilan East facing idle state'iyle baslatir
- Type upgrade'leri mevcut ve gelecekte spawn olacak ayni tip okculara damage/fire-rate scaling uygular; bu akisin player-facing sahibi sag drawer degil, ileride full-screen Tech Tree olacaktir
- `RestartGame()` ile oyunu sifirlar

### HUDController.cs

- HP, XP, continuous cycle, zombie alive/max, resource, population ve arrow text'lerini gunceller
- Mobile HUD'da resource text'leri label tekrar etmez; sadece kompakt value/rate yazar
- Resource rate gosteriminde base production yerine effective production'i kullanir; mobile worker economy aktifken bu deger worker allocation'dan gelir
- `CyclePanel` varsa `CyclePhaseText` degerini sadece `DAY / DUSK / NIGHT` olarak yazar
- `CycleProgressFill` ve `CycleProgressMarker` ile 60s cycle progress'ini gosterir
- `HordePressurePanel` imported prefabda bulunsa bile player-facing olarak kapali tutulur
- `CyclePanel` yoksa legacy wave fallback text'lerini kullanir
- Mobile unlimited arrow modunda `ArrowText` degeri `INF` olur
- Mobile HUD'da yalniz `DefenseWallFill` ve Wall yuzdesi guncellenir; legacy Gate/Core alanlari runtime'da gizlenir
- `WaveRewardText`, wave clear bonusunu kisa sure `Wave Cleared +...` olarak gosterir
- Night/high pressure baskisinda threat rengi kullanabilir; savunma hasarinda `DamageFlashImage` kisa red flash verir
- Archer count bilgisi sag drawer row'larinda okunur; mobile setup eski `ArcherTypeText` placeholder'ini kullanmaz
- Text alanlarini sadece deger degisince guncelleyerek gereksiz string allocation'i azaltir

### MobileCastleArcherTilePlacement.cs

- `NewGameScene` icindeki `Grid/outside` tilemapini okcu spawn kaynagi olarak kullanir.
- Tilemap'teki dolu hucreleri deterministic siraya dizer; hucre sayisi cap degildir, ayni hucre tekrar kullanilir.
- Tekrar kullanilan hucrelerde kucuk mini-offset uygular ve Scene view'da outside spawn noktalarini gizmo ile gosterir.
- Okcu spawn Z degeri `MobileCastleRenderDepth.UnitZ` (`-1`) tutulur. Kale tilemap on/arka iliskisi world z bandlariyla cozulur: back tilemap `0`, unit `-1`, front occluder `-2`, projectile `-2.5`. `DeadWalls/SpriteSheet` shader'i Entities Graphics uyumlulugu icin `Opaque/Geometry` kalir; transparent queue veya depth yazimini kapatma bu Entities hattinda entity gorunurlugunu bozabilir.

### CombatFeedbackBridge.cs

- ECS `CombatVfxEvent` ve `CombatSfxEvent` entity'lerini okur, hit flipbook, pooled ParticleSystem ve AudioSource ile oynatir.
- Arrow/Frost hit feedback'i hafif sprite flipbook pool ile, castle hit ParticleSystem ile, shoot feedback'i random AudioSource pool ile yonetilir; shoot particle V1'de kapali tutulur.
- Stress mode'da event'leri temizleyip playback'i kapatabilir; bu sayede performans testleri VFX/SFX yukunden etkilenmez.

### LevelUpUI.cs

- Legacy kart panelidir.
- Mobile castle loop'ta kullanilmaz; okcu alma sag drawer recruitment uzerinden ilerler, upgrade/unlock ileride Tech Tree'ye tasinacaktir.

### MarketUI.cs

- `MobileCastleHudRoot` uzerindeki sag `ArcherDrawerPanel` controller'idir
- Drawer combat sirasinda acilip kapanir; oyun pause olmaz
- `ArcherRecruitmentListRoot` + inactive `ArcherRecruitmentRowTemplate` varsa satirlari `ArcherRecruitmentCatalogSO` definition listesinden runtime'da uretir
- Template yoksa legacy Basic/Rapid/Frost row'larinda yalnizca `Buy` aksiyonunu `GameManager.BuyArcher()` API'sine baglar
- Upgrade butonlari, Rapid/Frost tech unlock butonlari ve `ArrowTechPanel` player-facing olarak gizlenir
- Basic baslangicta aciktir; Rapid/Frost ileride Tech Tree tarafindan unlock edilecek kilitli satirlar olarak kalir
- Row `CostText` alanlarinda mevcut cost ile beraber eksik kaynak varsa `NEED ...`, idle population yoksa `NEED POP` yazar
- `GameManager.Free Economy Test Mode` acikken cost satirlari `FREE` gosterir; kaynak ve population yetersizligi player-facing aksiyonlari bloklamaz
- Worker economy aktifken `Repair`, `Fortify` ve `Rally` player-facing drawer'da gizlenir; drawer archer recruitment paneli olarak kalir
- Mobile unlimited arrow modunda `Arrow Refill` gizlenir
- Mobile continuous siege loop'ta `Start Next Wave` player-facing UI'da gizlenir; oyun durmadan `DAY / DUSK / NIGHT` cycle'i akar
- Runtime davranisi prefaba gomulmez; controller ve scene setup tool tarafinda baglanir
  (UI dogrudan prefab uzerinde uretilir; eski UIImporter/export pipeline'i 2026-07-06'da kaldirildi)

### TechTreeUI.cs

- Fullscreen dinamik Tech Tree paneli controller'idir (`MobileCastleHudRoot` uzerinde, MarketUI gibi ayri component)
- Sabit kategori/tier/elle-yerlestirilmis agac YOKTUR; gorunur node'lar `GameManager.GetRevealedTechNodes()`'tan gelir, `TechNodeTemplate` runtime'da klonlanir
- Baglanti cizgileri `RevealChildNodeIds` iliskisinden `TechConnectionTemplate` klonuyla cizilir; layout deterministik agac algoritmasi (derinlik -> x, gorunur yaprak sayisi -> y dagilimi)
- Panel default kapali; `TechTreeOpenButton` acar, `TechTreeCloseButton` kapatir; PANEL ACIKKEN OYUN DURMAZ (drawer emsali; `MobilePrepPauseState` continuous siege'de olu, `Time.timeScale=0` "oyun durmaz" ilkesiyle catisir)
- Satin alma `GameManager.TryBuyTechNode()` uzerinden; basarida graf yeniden kurulur (yeni reveal'lar gorunur)
- Durum etiketleri: `AVAILABLE` / `BOUGHT` / `LOCKED` / `MAX` / `NEED ...` — duz renklerle ayrisir
- `Icon` null ise `TechNodeIconImage` kapanir, `TechNodeIconFallbackText` baslik bas-harflerini gosterir (art uretilmez)
- 0.2s unscaled poll ile refresh; gorunur node sayisi degisince INCREMENTAL sync (mevcut view'lar korunur, yeniler cizgi-cizilme + scale-pop animasyonuyla eklenir, yeri degisenler kayar — DOTween, unscaled)
- Juice: satin alma punch + chip flash + toast, reddetmede shake + kilit SFX'i, TECH butonunda alinabilir-tech badge'i (pulse), satin alinmis yollarin cizgileri yesil, LV pip'leri (MaxLevel 2-4), panel fade acilisi + son alinan node'a odak; SFX'ler Fantasy UI SFX Lite'tan setup tool ile baglanir
- Otoriter dok: `TECH_TREE_UI_ARCHITECTURE.md`

### TechTreeViewController.cs

- Tech tree viewport'unun pan/zoom controller'i; ScrollRect'in ustune eklenir (sol drag ScrollRect'te kalir)
- `TechTreeInputMode` enum (`Auto/Desktop/Mobile`): Desktop = tekerlek imlec-merkezli zoom + orta tus pan; Mobile = pinch zoom (orta-nokta merkezli) + tek parmak pan; Auto platforma gore secer
- Zoom `content.localScale` ile (layout sabit); alt sinir icerik viewport'a sigiyorsa 1'e clamp; pinch sirasinda ScrollRect gecici kapatilir

### CouncilComposer.cs + CouncilEventUI.cs

- Safak meclisi event'leri: kart DAWN'da belirir, DAY boyunca yasar, DUSK'ta expire; oyun durmaz
- Event'ler asset degil — `CouncilComposer` (pure static, EditMode testli) sablon x atom x baglam x olcek carpimindan uretir; deterministik (seed = hash(ECS RandomSeed, gun))
- Director: kit kaynak/dusuk savunma/bolluk baglamina gore atom-sablon agirliklari; hafiza: flag'ler + zincir sablonlari (RequiredFlags/ChainDelayDays/OneShot); butce: A/B secenekleri "dakika-degeri" cinsinden dengelenir
- GameManager API: `TryRollCouncilEvent` (sans 0.30 + pity 4 gun + cooldown), `ChooseCouncilOption`, `ExpireCouncilEvent`, `CanAffordCouncilOption`; efektler mevcut yollara akar (AddResources/AddPopulation/SpawnArcher/config cap aggregate/temp production slotu/NextNightSpawnMultiplier)
- Otoriter dok: `COUNCIL_EVENTS_ARCHITECTURE.md`

### DefenseRepairUI.cs

- CastleDefensePanel'deki player-facing REPAIR butonunun controller'i (HUD root'ta ayri component)
- Tamir continuous siege sirasinda HER ZAMAN denenebilir (eski DayPrep sarti kaldirildi — continuous'ta olu yoldu)
- Maliyet kayip-orantili: `GameManager.GetRepairCost()` = `ceil(RepairBase * kayipOrani * techCarpani)`; taban config'te (120W/80S tam kayipta)
- `repair_efficiency` tech node'u (`ReduceRepairCostPercent`) maliyeti dusurur
- Basarida punch, reddetmede shake (DOTween); 0.25s poll ile cost etiketi/interactable

### DawnRewardToastUI.cs

- Faz DAWN'a gectiginde bir kez "DAWN — DAY n SURVIVED  +N POP" toast'u (SiegeToastText, DOTween fade)
- Nufus odulunu MobilePopulationEconomySystem verir; bu controller yalnizca ani GORUNUR kilar (GDD 4-faz odul vurusu)

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
Archer Drawer Input -> GameManager.BuyArcher() -> EntityManager.SetComponentData -> ECS
Tech Tree Input -> GameManager.TryBuyTechNode() -> reveal/unlock state + MobileCastleCombatConfig/WallSegment/ArcherUnit yazimi -> ECS
Worker Drawer Input -> GameManager.Set/AdjustWorkerTargetRatioPercent() -> WorkerAllocationUtility -> MobilePopulationAllocation target -> sonraki population auto-allocation -> WorkerVisualRepresentationUtility -> temsili DOTS VillagerWorker count + exact weight -> animation/cargo/fener/delivery feedback
House Bed Purchase -> GameManager.TryBuyBedCapacity() -> MobileBedCapacityUtility owned-capacity sıralı fiyatı -> Wood transaction -> MobileBedCapacityState.PurchasedCapacity -> exact save v5
Legacy Castle Click -> CastleEconomyUI.OpenFromCastle() -> MobilePrepPauseState
Legacy Worker Slider Input -> GameManager.SetResourceWorkers() -> MobilePopulationAllocation -> WorkerVisualRepresentationUtility -> temsili DOTS VillagerWorker count + exact weight sync
Economy Event Input -> GameManager.ChooseEconomyEvent() -> Resources/Population/MobileEconomyEventState
Castle Interior Repair -> GameManager.RepairDefenseFull() -> EntityManager.SetComponentData -> ECS
DayNightOverlayController -> GameManager.WaveState + MobileCastleCombatConfig -> Overlay alpha
Mouse Click -> ClickDamageHandler -> EntityManager.SetComponentData -> ECS
```
