# MonoBehaviour Hybrid Layer - Mimari

## Genel Yapi

MonoBehaviour'lar ECS ile Unity UI arasinda kopru gorevi gorur. `World.DefaultGameObjectInjectionWorld.EntityManager` uzerinden ECS verilerine erisir.

## Dosyalar

### GameManager.cs

- Singleton pattern
- Her frame ECS singleton'larini okur: `GameStateData`, `WaveStateData`, `ContinuousSiegeCycleData`, `CastleHP`, resource ve population verileri
- Event'ler: `OnGameOver`, legacy `OnLevelUp`, `OnWaveChanged`, `OnGameStateChanged`
- `OnWaveCompleted` legacy wave cleared / market bekleme fazini UI katmanina bildirebilir; continuous siege varsayilaninda tetiklenmez
- Mobile ilk play'de baked aktif wave state'ini legacy DayPrep baslangicina normalize edebilir; continuous siege system bir sonraki frame aktif cycle'a ceker
- `StartNextWave()` debug/public API olarak kalir; mobile player-facing akis continuous day/dusk/night cycle ile ilerler
- `RepairDefenseFull()`, `BuyFortify()`, `BuyRally()` ve `RefillArrows()` legacy/debug API olarak kalir
- `GetDefensePercent()` wall/gate/castle toplam HP yuzdesini HUD'a verir
- Mobile archer economy API'leri: buy, cost ve type count/DPS okuma; unlock/upgrade API'leri ileride Tech Tree icin kodda kalir ama sag drawer player-facing kullanmaz
- Worker economy API'leri: `OpenCastleEconomy()`, `CloseCastleEconomy()`, `SetResourceWorkers()`, `ChooseEconomyEvent()`
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
- Mobile HUD'da `CastleDefensePanel` varsa `DefenseWallFill`, `DefenseGateFill`, `DefenseCoreFill` ve yuzde text'leriyle gercek defense modulu guncellenir; eski `DefenseText` sadece fallback'tir
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
- Basic/Rapid/Frost row'larinda yalnizca `Buy` aksiyonunu `GameManager.BuyArcher()` API'sine baglar
- Upgrade butonlari, Rapid/Frost tech unlock butonlari ve `ArrowTechPanel` player-facing olarak gizlenir
- Basic baslangicta aciktir; Rapid/Frost ileride Tech Tree tarafindan unlock edilecek kilitli satirlar olarak kalir
- Row `CostText` alanlarinda mevcut cost ile beraber eksik kaynak varsa `NEED ...`, idle population yoksa `NEED POP` yazar
- `GameManager.Free Economy Test Mode` acikken cost satirlari `FREE` gosterir; kaynak ve population yetersizligi player-facing aksiyonlari bloklamaz
- Worker economy aktifken `Repair`, `Fortify` ve `Rally` player-facing drawer'da gizlenir; drawer archer recruitment paneli olarak kalir
- Mobile unlimited arrow modunda `Arrow Refill` gizlenir
- Mobile continuous siege loop'ta `Start Next Wave` player-facing UI'da gizlenir; oyun durmadan `DAY / DUSK / NIGHT` cycle'i akar
- Runtime davranisi UI Importer JSON'una gomulmez; controller ve scene setup tool tarafinda baglanir

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
- Wood/Stone/Iron/Food `+ WORKER` butonlarini `GameManager.AssignResourceWorker()` API'sine baglar.
- DayPrep sartina bagli degildir; worker assignment her zaman denenebilir.

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
Worker Drawer Input -> GameManager.AssignResourceWorker() -> MobilePopulationAllocation -> DOTS VillagerWorker route visual sync -> MobilePopulationEconomySystem
Legacy Castle Click -> CastleEconomyUI.OpenFromCastle() -> MobilePrepPauseState
Legacy Worker Slider Input -> GameManager.SetResourceWorkers() -> MobilePopulationAllocation -> DOTS VillagerWorker visual sync
Economy Event Input -> GameManager.ChooseEconomyEvent() -> Resources/Population/MobileEconomyEventState
Castle Interior Repair -> GameManager.RepairDefenseFull() -> EntityManager.SetComponentData -> ECS
DayNightOverlayController -> GameManager.WaveState + MobileCastleCombatConfig -> Overlay alpha
Mouse Click -> ClickDamageHandler -> EntityManager.SetComponentData -> ECS
```
