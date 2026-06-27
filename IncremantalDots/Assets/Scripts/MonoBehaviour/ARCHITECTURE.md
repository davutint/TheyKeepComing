# MonoBehaviour Hybrid Layer - Mimari

## Genel Yapi

MonoBehaviour'lar ECS ile Unity UI arasinda kopru gorevi gorur. `World.DefaultGameObjectInjectionWorld.EntityManager` uzerinden ECS verilerine erisir.

## Dosyalar

### GameManager.cs

- Singleton pattern
- Her frame ECS singleton'larini okur: `GameStateData`, `WaveStateData`, `CastleHP`, resource ve population verileri
- Event'ler: `OnGameOver`, legacy `OnLevelUp`, `OnWaveChanged`, `OnGameStateChanged`
- `OnWaveCompleted` ile wave cleared / market bekleme fazini UI katmanina bildirir
- Mobile ilk play'de baked aktif wave state'ini `DayPrep` baslangicina normalize eder
- `StartNextWave()` debug/public API olarak kalir; mobile player-facing akis otomatik day/night sayaciyla ilerler
- `RepairDefenseFull()` day prep aksiyonudur; `BuyFortify()`, `BuyRally()` ve `RefillArrows()` legacy/debug API olarak kalir
- `GetDefensePercent()` wall/gate/castle toplam HP yuzdesini HUD'a verir
- Mobile archer economy API'leri: unlock, buy, upgrade, cost ve type count/DPS okuma
- Worker economy API'leri: `OpenCastleEconomy()`, `CloseCastleEconomy()`, `SetResourceWorkers()`, `ChooseEconomyEvent()`
- Economy focus API'leri legacy olarak kalir; worker economy aktifken setup tool focus UI'yi gizler
- Legacy level-up API'leri durur, fakat mobile castle loop'ta XP level-up pause tetiklemez
- Mobile castle mode'da drawer economy tarafindan satin alinan Basic/Rapid/Frost okculari `Grid/outside` tilemap hucrelerine spawn eder ve `1` idle population kullanir
- Mevcut `ArcherUnit` entity'lerinden Basic/Rapid/Frost sayilarini okur
- Spawn edilen okcuya type-specific `SpriteTint` yazar
- Spawn edilen okculari varsayilan East facing idle state'iyle baslatir
- Type upgrade'leri mevcut ve gelecekte spawn olacak ayni tip okculara damage/fire-rate scaling uygular
- `RestartGame()` ile oyunu sifirlar

### HUDController.cs

- HP, XP, wave, level, zombie alive/max, resource, population ve arrow text'lerini gunceller
- Mobile HUD'da resource text'leri label tekrar etmez; sadece kompakt value/rate yazar
- Resource rate gosteriminde base production yerine effective production'i kullanir; mobile worker economy aktifken bu deger worker allocation'dan gelir
- Wave state'ini `WAVE 01`, `WAVE 01 STARTING`, `WAVE 01 CLEARED` formatinda yazar
- Mobile mode'da wave state'ini `DAY 01 - 12s` ve `NIGHT 01` formatinda yazar
- Day prep sirasinda kills text'i `PREPARE DEFENSE`, night combat sirasinda `KILLS x / y` olur
- Mobile unlimited arrow modunda `ArrowText` degeri `INF` olur
- Mobile HUD'da `CastleDefensePanel` varsa `DefenseWallFill`, `DefenseGateFill`, `DefenseCoreFill` ve yuzde text'leriyle gercek defense modulu guncellenir; eski `DefenseText` sadece fallback'tir
- `WaveRewardText`, wave clear bonusunu kisa sure `Wave Cleared +...` olarak gosterir
- Wave final baski fazinda wave/kills text'i sicak threat rengine gecer; savunma hasarinda `DamageFlashImage` kisa red flash verir
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
- Mobile castle loop'ta kullanilmaz; okcu alma/upgrade sag drawer economy uzerinden ilerler.

### MarketUI.cs

- `MobileCastleHudRoot` uzerindeki sag `ArcherDrawerPanel` controller'idir
- Drawer combat sirasinda acilip kapanir; oyun pause olmaz
- Basic/Rapid/Frost row'larinda `Buy` ve `Upgrade` aksiyonlarini `GameManager` API'lerine baglar
- Rapid/Frost tech unlock butonlarini yonetir; Basic baslangicta aciktir
- Row `CostText` alanlarinda mevcut cost ile beraber eksik kaynak varsa `NEED ...`, idle population yoksa `NEED POP` yazar
- `GameManager.Free Economy Test Mode` acikken cost satirlari `FREE` gosterir; kaynak ve population yetersizligi player-facing aksiyonlari bloklamaz
- Worker economy aktifken `Repair`, `Fortify` ve `Rally` player-facing drawer'da gizlenir; drawer archer progression paneli olarak kalir
- Mobile unlimited arrow modunda `Arrow Refill` gizlenir
- Mobile day/night loop'ta `Start Next Wave` player-facing UI'da gizlenir; otomatik sayac yeni wave'i baslatir
- Runtime davranisi UI Importer JSON'una gomulmez; controller ve scene setup tool tarafinda baglanir

### DayNightOverlayController.cs

- `Canvas/DayNightOverlay` full-screen black `Image` alpha degerini yonetir.
- `DayPrep` sirasinda alpha'yi config'teki day/night degerleri arasinda sayac progress'ine gore artirir.
- `NightCombat` sirasinda night alpha sabit kalir.
- Stress veya non-mobile mode'da alpha `0` olur.

### EconomyFocusUI.cs

- Legacy controller'dir.
- Mobile worker economy aktifken setup tool economy focus objelerini gizler ve bu controller'i kullanmaz.
- Eski focus akisi, `MobilePopulationAllocation` bulunmayan mobile/legacy denemeler icin korunur.

### CastleEconomyUI.cs

- `CastleEconomyPanel` full-screen ekonomi panelini yonetir.
- `CastleTapHint` DayPrep sirasinda gorunur; panel acikken, combat sirasinda ve stress mode'da gizlenir.
- Panel acikken `GameManager.OpenCastleEconomy()` ile prep timer ve resource tick pause olur.
- Worker slider'larini `GameManager.SetResourceWorkers()` API'sine baglar.
- Population total, idle, archers, growth ve worker budget text'lerini gunceller.
- Kalan DayPrep suresine gore `net/min * remaining prep / 60` projected resource gain text'lerini gunceller.
- `CastleRepairButton` ile repair aksiyonunu panel icinde yonetir.
- Pending economy event varsa 2 secenekli event alanini ve badge/glow feedback'ini gosterir.

### CastleInteriorClickTarget.cs

- Main scene'deki `CastleClickTarget` objesi uzerindedir.
- Sadece mobile normal `DayPrep` sirasinda kale/world merkezi tiklamasini kabul eder.
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
UI Input -> GameManager.CanApplyUpgrade()/ApplyUpgrade() -> EntityManager.SetComponentData -> ECS
Drawer Input -> GameManager.BuyArcher()/UpgradeArcherType()/UnlockArcherType() -> EntityManager.SetComponentData -> ECS
Castle Click -> CastleEconomyUI.OpenFromCastle() -> MobilePrepPauseState
Worker Assign Button -> GameManager.AssignResourceWorker() -> MobilePopulationAllocation -> DOTS VillagerWorker route visual sync -> MobilePopulationEconomySystem
Legacy Worker Slider Input -> GameManager.SetResourceWorkers() -> MobilePopulationAllocation -> DOTS VillagerWorker visual sync
Economy Event Input -> GameManager.ChooseEconomyEvent() -> Resources/Population/MobileEconomyEventState
Castle Interior Repair -> GameManager.RepairDefenseFull() -> EntityManager.SetComponentData -> ECS
DayNightOverlayController -> GameManager.WaveState + MobileCastleCombatConfig -> Overlay alpha
Mouse Click -> ClickDamageHandler -> EntityManager.SetComponentData -> ECS
```
