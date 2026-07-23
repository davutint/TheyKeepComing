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
- Mobile archer economy API'leri: `ArcherDefinitionSO` catalog'undan type-count scaled buy/retrain cost ve base stat okuma, buy, Basic -> Rapid/Frost in-place retrain, type count/DPS okuma; `GetTotalArcherCount`, `GetRemainingArcherCapacity` ve `CanAddArchers` Basic/Rapid/Frost ortak `1000` cap'ini sunar. Direct type-level cost/upgrade API'si yoktur; stat progression exact Heart effect pipeline'ina aittir
- Legacy Tech Tree state/API (`_techNodeLevels`, `_revealedTechNodes`, `TryBuyTechNode`) migration/debug uyumlulugu icin kodda kalir; aktif `NewGameScene` HUD'inda `TechTreeUI` yoktur ve legacy catalog player-facing progression owner'i degildir
- Castle Heart runtime'i `GameManager.HeartRuntime.cs` partial'inda generated graph/reveal/presentation, Grave Essence-only quote/purchase ve actual effect adapter'larini birlestirir. Production `heartCatalog` null ise acik hata verir; legacy `TechTreeCatalogSO`'ya fallback yapmaz
- Run-only `GraveEssence` bakiyesi, `ZombieDeathSystem`in `%10` / `1` production drop event'lerini tuketen `GrantGraveEssence` ile artar ve yalniz `TrySpendGraveEssenceAtHeart` kapisindan azalir; stress-test olumleri drop uretmez, exact save v17'de generated Heart graph ile birlikte korunur, Restart/Game Over'da silinir
- Continue saved Heart graph'i `CatalogVersion`/structural/runtime-state preflight'inden gecirir ve purchased level'lari deferred `HeartEffectPipeline` replay'iyle canli owner'lara uygular; v9 null-graph migration'i yeni graph uretmez
- Heart effect'leri Heart'siz baseline uzerine uygulanir: Basic/Rapid/Frost damage/fire-rate/range/Frost slow, tek Wall HP/repair, resource-specific worker capacity/production, population growth, Arrow capacity/efficiency ve Fireball damage/radius/cooldown. Arrow Heart bonuslari paid Arrow level'larindan ayri ECS alanlarinda tutulur
- `GetHeartGraphSettingsSnapshot()` gelecekte uretilecek graph ayarlarini kopya olarak verir; `GetHeartRuntimeTuningTelemetry()` ise hidden node kimliklerini acmadan wallet, meta remainder ve aggregate graph sayaclarini Difficulty Tuner'a sunar. Bu yuzey aktif veya Continue ile restore edilen exact graph'i reroll etmez
- Worker economy API'leri: `OpenCastleEconomy()`, `CloseCastleEconomy()`, `SetResourceWorkers()`, `ChooseEconomyEvent()`
- Worker bina yatırım API'leri: `GetWorkerBuildingUpgradeLevel()`, `GetWorkerBuildingUpgradeCost()`, `CanBuyWorkerBuildingUpgrade()` ve `TryBuyWorkerBuildingUpgrade()`; dört hazır binanın bağımsız Capacity/Efficiency seviyelerini baked `MobileEconomyPriceTuning` fiyatıyla Wood + Iron transaction'ı üzerinden büyütür. `ApplyTechEconomyAggregates()` profile base + Tech + Heart + Meta + bina etkilerini tek owner'da birleştirir; `ApplyWorkerEconomyTuning()` live profile değişikliğinde aynı katmanları yeni base rate'lere yeniden fold eder
- House bed API'leri: `GetTotalBedCapacity()`, `GetPurchasedBedCapacity()`, `GetBedCapacityPurchaseCost()`, `CanBuyBedCapacity()` ve `TryBuyBedCapacity()`; run-scoped `MobileBedCapacityState`, baked `MobileEconomyPriceTuning` base/interval değerleriyle toplam sahipliği `60` tabanından sonra quadratic büyüten ardışık Wood transaction'ıyla büyür ve güncel exact save içinde korunur. Mobile Dawn bed + Food kabul bütçesi bu state'i kapasite owner'ı olarak kullanır
- Population tuning: Dawn request ve Food/arrival `DifficultyProfileSO -> MobileCastleTuningResolver -> MobileCastleCombatConfig` zincirindedir; `Difficulty Tuner > Population Runtime Contract` live Apply mevcut run yatak state'ini sifirlamadan bir sonraki Dawn butcesini ve bed fiyat egrisini gunceller
- Dawn survivor görsel köprüsü: yeni persistent growth marker'ını ve gerçek accepted count'u gözler; mevcut `VillagerWorker` prefabından en fazla `15` transient arrival entity'si üretir, resource worker/logistics component'lerini kaldırır ve hareketi `SurvivorArrivalVisualSystem`'a bırakır. Population/Food transaction'ını tekrar yazmaz
- Economy focus API'leri legacy olarak kalir; worker economy aktifken setup tool focus UI'yi gizler
- Legacy level-up API'leri durur, fakat mobile castle loop'ta XP level-up pause tetiklemez
- Mobile castle mode'da drawer economy tarafindan satin alinan Basic/Rapid/Frost okculari `Grid/outside` tilemap hucrelerine spawn eder ve Wood -> Stone -> Iron -> Food sirasiyla `1` resource worker'i Archer havuzuna tasir
- Mevcut `ArcherUnit` entity'lerinden Basic/Rapid/Frost sayilarini okur
- Bütün aktif spawn yollarini `SpawnArcher` merkezinde `ArcherCapacityUtility` ile sınırlar; 1001. entity oluşmaz
- Spawn edilen okcuya type-specific `SpriteTint` yazar
- Spawn edilen okculari varsayilan East facing idle state'iyle baslatir
- Heart damage/fire-rate/range/Frost slow effect'leri mevcut okculari aninda rebase eder; daha sonra spawn/retrain edilen okcular ayni effective state'i alir. Continue saved Heart level'larini replay eder ve bonusu compound etmez
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
- Bir frame'deki shoot event'lerini tek salvo cue'ya aggregate eder; sabit `16` AudioSource pool,
  frame basi `4` cue budget'i ve Night `0.12s` shoot rate-limit'i ile kalabalik mix'i sinirlar.
- Stress mode'da event'leri temizleyip playback'i kapatabilir; bu sayede performans testleri VFX/SFX yukunden etkilenmez.

### LevelUpUI.cs

- Legacy kart panelidir.
- Mobile castle loop'ta kullanilmaz; okcu alma sag drawer recruitment uzerinden, unlock ve stat progression Castle Heart uzerinden ilerler.

### MarketUI.cs

- `MobileCastleHudRoot` uzerindeki alt-sag `ArcherDrawerPanel` controller'idir
- HUD ve yeni run acilisinda drawer kapali baslar; sabit `ARCHERS` butonu kayan panelin disindadir
- Drawer combat sirasinda acilip kapanir; oyun pause olmaz. `OpenOnWaveCompleted` legacy wave-complete acilisini korur
- `ArcherRecruitmentListRoot` + inactive `ArcherRecruitmentRowTemplate` varsa satirlari `ArcherRecruitmentCatalogSO` definition listesinden runtime'da uretir
- Template yoksa legacy Basic/Rapid/Frost row'larinda yalnizca `Buy` aksiyonunu `GameManager.BuyArcher()` API'sine baglar
- Upgrade butonlari, Rapid/Frost tech unlock butonlari ve `ArrowTechPanel` player-facing olarak gizlenir
- Basic baslangicta aciktir; Rapid/Frost Castle Heart unlock node'larina kadar kilitli satirlar olarak kalir
- Row `CostText` alanlarinda mevcut cost ile beraber eksik kaynak varsa `NEED ...`, sivil worker yoksa `NEED WORKER` yazar
- `GameManager.Free Economy Test Mode` acikken cost satirlari `FREE` gosterir; kaynak ve population yetersizligi player-facing aksiyonlari bloklamaz
- Free Economy Test Mode ortak `1000` cap'i bypass etmez; cap'te row `ARMY CAP 1000/1000` ve `MAX` gosterir
- Rapid/Frost unlock olduktan sonra `RETRAIN`, bir Basic entity'yi yerinde dönüştürür; toplam garnizon/population değişmez ve cap doluyken de çalışır
- Buy ve retrain maliyetleri hedef tür sayısına göre definition tuning'inden büyür; ayrı archer upgrade/level UI açılmaz. Row level alanı unlocked türde `HEART`, kilitli türde `TECH` yazar
- Basarili player-facing buy action'i `ArcherPurchasedByPlayer` event'ini yayar; onboarding gibi presentation consumer'lari transaction'i tekrar etmeden bu event'i dinler
- Worker economy aktifken `Repair`, `Fortify` ve `Rally` player-facing drawer'da gizlenir; drawer archer recruitment paneli olarak kalir
- Legacy `Arrow Refill` kontrolü gizlenir; resource chip'leri pasif kalır ve ayrı alt-sağ
  `ARROW SUPPLY` butonu güncel UI Toolkit supply drawer'ını açar; `GameManager.ArrowDelivery`
  ödemesi alınan refill'i 3 simulation saniyesi kullanılamaz tutar ve süre sonunda gerçek
  ECS stokuna atomik olarak ekler
- Basarili player-facing `+1`, `+5` veya `Buy Max` refill
  `ArrowRefillPurchasedByPlayer` event'ini yayar; CAP/EFF yatirimi bu event'i yaymaz
- Mobile continuous siege loop'ta `Start Next Wave` player-facing UI'da gizlenir; oyun durmadan `DAY / DUSK / NIGHT` cycle'i akar
- Runtime davranisi prefaba gomulmez; controller ve scene setup tool tarafinda baglanir
  (UI dogrudan prefab uzerinde uretilir; eski UIImporter/export pipeline'i 2026-07-06'da kaldirildi)

### HeartScreenUI.cs

- Aktif fullscreen Castle Heart controller'idir; `HeartGraphPresentation` hidden-safe modelini cizer
- Alt-sag dock'taki sabit `CASTLE HEART` butonu fullscreen paneli acar; button Archer drawer'in hareketinden bagimsizdir
- Army/Defense/Production/Heart-Magic dallarini sag/sol/yukari/asagi compass layout ile yerlestirir
- `+1/+10/MAX`, exact Grave Essence quote, actual current/after/delta ve Keystone conflict bilgisini sunar
- Acilista `SimulationPauseService` lease'i alir; cycle/spawn/movement/combat/worker ve scaled cooldown'lar durur. UI refresh/animasyonlari unscaled zamanda calisir
- Basarili gercek open/close UI aksiyonlari `HeartOpenedByPlayer` / `HeartClosedByPlayer` event'lerini yayar; programmatic panel cagrilari player action sayilmaz
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
- Finite Arrow stoku effective kapasitenin `%25` veya altina ilk kez indiginde panel kapaliyken
  gercek `ARROW SUPPLY` dock butonunu, panel oyuncu tarafindan acilinca gercek paket butonunu
  pulse eder; ammo panelini otomatik acmaz
- Ilk pozitif Grave Essence bakiyesinde gercek `CASTLE HEART` butonunu pulse eder; oyuncu paneli acinca mevcut full pause'u English hint ile ogretir ve flag'i player close sonrasinda yazar
- Gameplay transaction'i, otomatik drawer acma, resource harcama veya worker dagitma yapmaz
- Basarili gercek player ratio, Basic Archer, Arrow refill ve Heart open/close action'larini ilgili
  gameplay event'lerinden alir; stable `tutorial.v1.*` flag'lerini yalniz mevcut Play oturumunda
  `TutorialSessionProgress` uzerinden kaydeder
- Tutorial flag'leri meta veya run save'e yazilmaz; her yeni Play oturumu butun adimlari temizler
- Gercek Heart open/close event'i Essence drop miktari uretmez
- Otoriter dok: `FIRST_RUN_ONBOARDING_UI_ARCHITECTURE.md`

### TechTreeUI.cs

- Legacy sabit catalog UI controller'idir; aktif `NewGameScene` HUD instance'inda bulunmaz
- Save/migration ve eski scene uyumlulugu icin kodda kalir; yeni progression veya UI degisikligi burada yapilmaz

### TechTreeViewController.cs

- Tech tree viewport'unun pan/zoom controller'i; ScrollRect'in ustune eklenir (sol drag ScrollRect'te kalir)
- `TechTreeInputMode` enum (`Auto/Desktop/Mobile`): Desktop = tekerlek imlec-merkezli zoom + orta tus pan; Mobile = pinch zoom (orta-nokta merkezli) + tek parmak pan; Auto platforma gore secer
- Zoom `content.localScale` ile (layout sabit); alt sinir icerik viewport'a sigiyorsa 1'e clamp; pinch sirasinda ScrollRect gecici kapatilir

### CouncilComposer.cs + CouncilEventUI.cs

- Safak meclisi event'leri: kart DAWN'da belirir ve production UI Toolkit `CouncilDecision`
  lease'iyle simulation'i durdurur; basarili secim onceki `1X/2X/3X` hizini geri yukler
- Event'ler asset degil — `CouncilComposer` (pure static, EditMode testli) sablon x atom x baglam x olcek carpimindan uretir; deterministik (seed = hash(ECS RandomSeed, gun))
- Director: kit kaynak/dusuk savunma/bolluk baglamina gore atom-sablon agirliklari; hafiza: flag'ler + zincir sablonlari (RequiredFlags/ChainDelayDays/OneShot); butce: A/B secenekleri "dakika-degeri" cinsinden dengelenir
- Regular schedule tek owner'i `CouncilRegularSchedule`: Day 1'den itibaren her Dawn'da tam bir kez; chance/pity/cooldown regular akis disinda. Council regular-only'dir ve ikinci emergency meeting yolu yoktur. GameManager API: `TryOpenRegularCouncilEvent`, `ChooseCouncilOption`, `ExpireCouncilEvent`, `CanAffordCouncilOption`
- `CouncilOptionPresentationUtility` iki secenegi canli state'ten exact quote eder;
  player-facing karar seridi pause altinda dolu kalir ve secim yapilmasini ister
- Exact save v17 `LastRegularCouncilDay`, non-zero Council run salt, `HasActiveCouncilEvent` ve resolved effect state'ini korur; v10 chance fail'i migration'da scheduled gunu tuketmez
- Otoriter dok: `COUNCIL_EVENTS_ARCHITECTURE.md`

### SimulationPauseService.cs + GameplayToastService.cs

- `SimulationPauseService`, player-facing `1X/2X/3X` kosu hizinin ve modal pause lease'lerinin
  tek runtime owner'idir; son lease kapaninca yakalanan ECS state'i ve kosu hizi exact doner
- `GameplayToastService`, mevcut HUD feedback cagrilarini bounded FIFO kuyrugunda tasir; yeni
  gameplay toast trigger'i owner onayi olmadan eklenmez
- Otoriter doklar: `SIMULATION_SPEED_AND_PAUSE_ARCHITECTURE.md` ve
  `GAMEPLAY_TOAST_SERVICE_ARCHITECTURE.md`

### DefenseRepairUI.cs

- CastleDefensePanel'deki player-facing REPAIR butonunun controller'i (HUD root'ta ayri component)
- Tamir continuous siege sirasinda HER ZAMAN denenebilir (eski DayPrep sarti kaldirildi — continuous'ta olu yoldu)
- Maliyet gercek heal paketine bagli: `ceil(actualHealHP x StonePerHP x DayPrice x discounts)`; yalniz Stone harcanir
- `repair_efficiency` tech node'u (`ReduceRepairCostPercent`) maliyeti dusurur
- Basarida punch, reddetmede shake (DOTween); 0.25s poll ile cost etiketi/interactable

### DawnRewardToastUI.cs

- Faz DAWN'a gectiginde bir kez "DAWN — DAY n SURVIVED  +N POP" toast'u (SiegeToastText, DOTween fade)
- Nufus odulunu `MobilePopulationEconomySystem` verir; bu controller `GameManager.GetLastAcceptedPopulationArrivalCount()` ile config isteği yerine gerçek kabul edilen `N` değerini gösterir
- Accepted `N > 0` ise tek ana `outside2/Door C5_E` tile'ini survivor yaklaşımında
  `Door C6_E` ile açar, tek `DawnGateGlow` envelope'u sürer ve geçiş sonunda kapatır.
- İlk scene/Continue gözlemi faz kenarı sayılmaz; toast ve kapı yalnız gerçek Dawn geçişinde oynar.

### DayNightOverlayController.cs

- `Canvas/DayNightOverlay` full-screen black `Image` alpha degerini yonetir.
- Sahnedeki tek Global Light 2D'yi warm Day, amber-indigo Dusk, cold-moon Night ve
  cyan-altın-Day Dawn hedefleriyle surer.
- Tam dort canonical kale pencere sprite'ina bagli Point Light 2D, Dusk'ta yumusak yanar,
  Night boyunca sicak kalir ve Dawn'da soner; yeni cycle veya gameplay owner'i olusturmaz.
- Continuous siege aktifken Day alpha acik kalir, Dusk boyunca day/night alpha arasinda kararir, Night alpha sabit kalir.
- Legacy `DayPrep` sirasinda alpha'yi config'teki day/night degerleri arasinda sayac progress'ine gore artirir.
- Legacy `NightCombat` sirasinda night alpha sabit kalir.
- Stress veya non-mobile mode'da alpha `0` olur.

### AmbientAudioController.cs

- Tek phase-transition 2D AudioSource'u Dusk riser ve Dawn nefes/yeni-gün cue'sunu taşır.
- Dawn cue yalnız gerçek faz kenarında bir kez oynar; ilk scene/Continue gözleminde tekrar etmez.
- Night drone/horde bed ile Day worker foley mevcut bounded owner'larında kalır.

### MomentVignetteUI.cs

- Serialized adı korunarak phase sky ve tek bounded atmosfer ParticleSystem owner'ına genişletildi.
- Main Camera background rengi Day/Dusk/Night/Dawn paletini izler; aynı `ContinuousSiegeCycleData`
  grading ve audio owner'larıyla ortak truth kaynağıdır.
- Tek `PhaseAtmosphereParticles`, `72` cap ve faza bağlı bounded emission/burst kullanır; stress ve
  Game Over yeni parçacık üretmez.
- İlk scene/Continue gözlemi transition sayılmaz. Canonical Dawn generic full-screen flash değeri
  sıfırdır; büyük phase text/label objeleri player-facing inactive kalır.
- Ayrıntı: `PHASE_WORLD_READABILITY_ARCHITECTURE.md`.

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
- Unassigned, total worker, archer count ve resource worker rate alanlarini gunceller.
- Wood/Stone/Iron/Food `0-100%` slider kontrollerini target ratio API'lerine baglar.
- Basarili target-ratio player action'inda `WorkerTargetRatioChangedByPlayer` event'i yayar; onboarding bu event'i dinler, drawer transaction sahibi degismez.
- Her resource satirindaki `CAP` ve `EFF` butonlarini bagimsiz worker bina yatirim API'lerine baglar; level ve bir sonraki Wood + Iron maliyetini butonda gosterir.
- Secilen exact hedef korunurken diger uc hedef deterministik yeniden dagilir; toplam `%100` kalir.
- Mevcut archer olmayan bütün nüfusu anında kapasite-aware yeniden dağıtır; hedef cap'e çarparsa overflow sıradaki resource'a gider.
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
Dawn accepted marker -> GameManager.SyncSurvivorArrivalVisualsIfNeeded() -> VillagerWorker tabanlı transient survivor entity'leri -> SurvivorArrivalVisualSystem -> DawnRewardToastUI ana gate tile sunumu -> Wall arkası varışta destroy/kapı kapanışı
Legacy Castle Click -> CastleEconomyUI.OpenFromCastle() -> MobilePrepPauseState
Legacy Worker Slider Input -> GameManager.SetResourceWorkers() -> MobilePopulationAllocation -> WorkerVisualRepresentationUtility -> temsili DOTS VillagerWorker count + exact weight sync
Economy Event Input -> GameManager.ChooseEconomyEvent() -> Resources/Population/MobileEconomyEventState
Castle Interior Repair -> GameManager.RepairDefenseFull() -> EntityManager.SetComponentData -> ECS
DayNightOverlayController -> ContinuousSiegeCycleData + MobileCastleCombatConfig -> Global Light + cyan/altın overlay grading
Mouse Click -> ClickDamageHandler -> EntityManager.SetComponentData -> ECS
```
