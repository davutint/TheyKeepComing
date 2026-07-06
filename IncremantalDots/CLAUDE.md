# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> Yazim kurali: bu dosya SADECE ASCII karakter kullanir (Turkce prose diakritiksiz: "Klasor", "Yapisi";
> noktalama duz ASCII: `->`, `-`, `<->`). Tipografik karakter (em-dash, ok, vb.) KULLANMA -- diger
> tool'larda mojibake olur. Operasyonel/surec kurallari (build/test/git/Turkce iletisim/derleme) icin
> `AGENTS.md`'ye bak; bu dosya MIMARI + AKTIF YON pusulasidir.

---

## >>> Current Active Direction: NewGameScene Mobile Castle Defense <<<

Proje ilk GDD'lerdeki grid town-building / RTS vizyonundan CIKTI. Su an aktif gelistirilen tek sey:
**NewGameScene "Mobile Continuous Siege" dongusu + dunya gorsel temeli (world-visual foundation) + ekonomi/savas polish.**
(Eski RTS/town tarafi icin asagidaki "Legacy / Broader Repo Context" bolumune bak -- silinmedi, sadece geri cekildi.)

### Aktif sahne yapisi (iki katman)
- **`Assets/Scenes/NewGameScene.unity`** = AKTIF ana sahne -- MonoBehaviour/UI + gorsel kabuk.
  Icerik: `Main Camera` (ortho, size 8), `Global Light 2D`, `WorldVisualRoot`, `Canvas`
  (`DayNightOverlay` + `MobileCastleHudRoot` prefab + level/restart), `GameManager`,
  `CastleClickTarget`, ve bir SubScene referansi.
- **`Assets/Scenes/NewGameScene/MobileCastleCombatSubScene.unity`** = DOTS authoring tarafi (ECS'e bake).
  Icerik: `GameState` (GameState+WaveConfig authoring), `CastleCore` (CastleAuthoring),
  `MobileCastleConfig` (MobileCastleCombatAuthoring), legacy/seed `BasicArcher_01`.
- Okcu spawn noktasi artik subscene slot'lari degil, ana scene `Grid/outside` tilemapindeki dolu hucrelerdir.
- Iki katman BILINCLI ayrik: Mono/UI ana sahnede, simulasyon datasi subscene'de.

### Gorsel temel: `WorldVisualRoot` + SmallScaleInt
- `WorldVisualRoot` = duz sahne GameObject (NewGameScene.unity:132, scale 0.35) -- prefab/ECS DEGIL,
  gameplay'i ETKILEMEZ. `MobileCastleSceneSetupWindow.EnsureWorldVisuals` (MobileCastleSceneSetupWindow.cs:375)
  idempotent kurar/normalize eder.
- Altinda `MobileArenaGrid` (Unity `Grid`, Isometric, cellSize 4/2/4) -> 4 `Tilemap` katmani
  (sortingOrder ile derinlik): `GroundTilemap` -50, `CastleGroundTilemap` -40, `CastleWallTilemap` -30,
  `CastlePropsTilemap` -20. Yani: izometrik arena zemini + kale silueti + savas alani dekoru, salt okunabilirlik.
- Tile'lar `Assets/SmallScaleInt/Fantasy kingdom Tileset/Environment/Tiles`'ten isimle yuklenir
  (`SmallScaleInt/` = gorsel temel art: Fantasy kingdom Tileset + Character creator - Fantasy).

### Aktif oyun dongusu (Mobile Continuous Siege -- otoriter dok: `Systems/MOBILE_CASTLE_COMBAT_ARCHITECTURE.md` + `Systems/CONTINUOUS_SIEGE_CYCLE_SYSTEM_ARCHITECTURE.md`)
360-derece merkezi kale savunmasi, SUREKLI akan kusatma. Player-facing "Start Next Wave" / "Wave Cleared" YOK; oyun durmaz.
Otorite `ContinuousSiegeCycleData` (eski `WaveStateData.Phase` DEGIL):
1. **Cycle (`ContinuousSiegeCycleSystem`)** -- 60s dongu 4 FAZ: DAY 22s -> DUSK 8s -> NIGHT 22s -> DAWN 8s.
   `ContinuousSiegeCycleData` `Phase` + progress + `SpawnIntensityMultiplier` + `HordePressure01` + `CycleIndex` yazar.
   UI `DAY/DUSK/NIGHT/DAWN` + `CycleDayCounterText` ("DAY n") gosterir.
2. **Spawn (kutle eskalasyonu)** -- zombiler rastgele 360-ring'den; interval/batch intensity'ye gore akar
   (Day 0.55, Dusk 1.00 -> 1.35, Night 1.65, Dawn 0.15). Cycle ile: batch buyur (`SpawnBatchGrowthPerCycle`,
   `MaxSpawnBatch` cap), HP LINEER buyur (`ZombieBaseHP*(1+(w-1)*ZombieHpGrowthPerCycle)` -- ustel DEGIL,
   sunger degil kalabalik), `MaxAliveZombies` performans tavani. Gunduz overlay alpha 0 -> gece 0.50 -> Dawn'da acilir.
3. **Reward (DAWN)** -- population growth (+15) DAWN fazinda verilir + `DawnRewardToastUI` toast'u.
   Kill reward SABIT kalir (`KillRewardWaveScale=0`, gelir/zorluk ayrisik); ana gelir sol worker ekonomisi.
   Repair artik MALIYETLI (kayip-orantili, `DefenseRepairUI` butonu) -- ana sink'lerden biri.
4. **Council events (safak meclisi)** -- DAWN'da rollanir (sans 0.30 + 4 gun pity), kart DAY boyunca yasar,
   DUSK'ta expire. `CouncilComposer` uretir (SO atom/sablon havuzu `ScriptableObject/MobileCastle/Council/`);
   risk atomu `NextNightSpawnMultiplier` ile sonraki geceyi buker. Legacy `MobileEconomyEventState` roll'u
   (DayPrep) continuous'ta kosmaz; temp-bonus/expire alanlari yeni sistemce KULLANILIR.
- **LEGACY (kodda durur, `ContinuousSiegeCycleData.Enabled` true iken DEVRE DISI):** `WaveStateData.Phase` DayPrep/NightCombat,
  Wave-clear reward (`WaveClearRewardData`), `Start Next Wave`, `Repair/Fortify/Rally`, intra-wave director pacing.
- Anahtarlar `MobileCastleCombatConfig`'te: CastleCenter (0,0), SpawnRadius 11, AttackRadius 1.35,
  `UnlimitedArrows=true`, continuous cycle 60s (22/8/22/8) + intensity + kutle eskalasyonu + repair maliyeti,
  worker uretim/cap/odul carpani (detay icin doc).
- BIRAKILAN GDD ozellikleri: grid town-building, lane/telegraph wave, manuel RTS okcu yerlestirme, manuel Start Next Wave /
  wave-clear ekrani, XP level-up kartlari, ok stogu yonetimi -- hepsi mobile loop'ta YOK.

### Aktif UI yuzeyleri (NewGameScene'de bagli; setup tool baglar)
| Yuzey | Controller | Not |
|---|---|---|
| Mobile HUD (kaynak/cycle/HP) | `HUDController.cs` | `MobileCastleHudRoot` prefab; Cycle paneli `DAY/DUSK/NIGHT` + 60s progress |
| Defense panel | `HUDController.cs` | AYRI controller YOK -- HUD alt modulu (`CastleDefensePanel`) Wall/Gate/Core |
| Worker economy drawer (sol) | `WorkerEconomyDrawerUI.cs` | resource bar alti; Wood/Stone/Iron/Food `+ WORKER` assignment -- AKTIF ana ekonomi |
| Archer recruitment drawer (sag) | `MarketUI.cs` | SO-driven `ArcherDefinitionSO` catalog'dan row basar; SADECE okcu satin alma (Buy); upgrade/ArrowTech/Repair/Fortify/Rally/StartNextWave player-facing GIZLI |
| Tech Tree (fullscreen) | `TechTreeUI.cs` | SO-driven dinamik reveal grafi: `TechNodeDefinitionSO`+`TechTreeCatalogSO`, root `castle_heart` sahipli baslar, satin alma cocuklari acar; Rapid/Frost unlock BURADAN gelir; panel acikken oyun DURMAZ; kategori/tier YOK |
| Gun/Gece overlay | `DayNightOverlayController.cs` | faz alpha (Day 0 -> Night 0.50 -> Dawn'da geri acilir) |
| Defense repair butonu | `DefenseRepairUI.cs` | CastleDefensePanel'de REPAIR + kayip-orantili maliyet etiketi; her zaman denenebilir (DayPrep sarti YOK) |
| Dawn odul toast'u | `DawnRewardToastUI.cs` | faz Dawn'a gecince "DAWN - DAY n SURVIVED +15 POP" (SiegeToastText) |
| Council event karti | `CouncilEventUI.cs` + `CouncilComposer.cs` | safak meclisi: kart DAWN'da belirir, DAY boyunca yasar, DUSK'ta expire; event'ler asset DEGIL, sablon x atom x baglam x olcekten uretilir (director + zincir/flag hafizasi + butce dengeleme); pause YOK |
| Castle Interior ekonomi paneli | `CastleEconomyUI.cs` | LEGACY/debug (`PlayerFacingPanelEnabled=false`); ana ekonomi sol drawer'a tasindi |
| Kaleye tikla-ac tetikleyici | `CastleInteriorClickTarget.cs` | LEGACY; player-facing worker yonetimi sol drawer'da |

### Su anki pusula (takip kaynaklari) -- ROADMAP DEGIL
- **Guncel tasarim GDD'si:** `Assets/Docs/DEAD_WALLS_GDD_v5.0.md` (Mobile Continuous Siege + Castle Interior Economy). GDD v2-v4 = legacy.
- **Canli takip:** ozellik-bazli mobile docs:
  `Systems/MOBILE_CASTLE_COMBAT_ARCHITECTURE.md`, `Systems/CONTINUOUS_SIEGE_CYCLE_SYSTEM_ARCHITECTURE.md`,
  `Systems/MOBILE_POPULATION_ECONOMY_SYSTEM_ARCHITECTURE.md`, `Systems/SYSTEM_EXECUTION_ORDER_ARCHITECTURE.md`,
  `MonoBehaviour/ARCHITECTURE.md`, `MonoBehaviour/WORKER_ECONOMY_DRAWER_UI_ARCHITECTURE.md`,
  `MonoBehaviour/TECH_TREE_UI_ARCHITECTURE.md`, `ScriptableObject/TECH_TREE_SO_ARCHITECTURE.md`,
  `MonoBehaviour/CASTLE_ECONOMY_UI_ARCHITECTURE.md`, `Editor/MOBILE_CASTLE_SCENE_SETUP_ARCHITECTURE.md`.
- **Aktif yon:** mobile continuous siege + sol castle interior worker economy + combat/UI polish. **M-ISO (izometrik/perimeter) ARTIK AKTIF DEGIL.**
- `Assets/Docs/ROADMAP.md` (son guncelleme 2026-03-18) = LEGACY milestone gecmisi, guncel yon DEGIL.

---

## Proje Hakkinda
- **Motor:** Unity `6000.3.10f1` (Unity 6 LTS) + DOTS/ECS (`com.unity.entities` 1.3.9, Entities Graphics, URP 2D).
- **Namespace:** `DeadWalls` (tek asmdef: `Assets/Scripts/DeadWalls.asmdef`, `allowUnsafeCode: true`).
- **Tween:** DOTween Pro (`Assets/Plugins/Demigiant`); UI juice icin. `DeadWalls.asmdef` -> `DOTween.Modules`
  referansi SART (DOTween Utility Panel > Create ASMDEF ile uretilir); Modules asmdef'i silinirse DOFade/DOColor derlenmez.
- **Tur:** Zombie castle-defense (360-derece, kaynak/nufus ekonomisi).

## Komutlar (build / test) -- detay: `AGENTS.md`
- **Ac:** Unity Hub -> `IncremantalDots`. Aktif sahne: `Assets/Scenes/NewGameScene.unity`.
- **EditMode:** `Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -quit -logFile Logs/EditModeTests.log`
- **PlayMode:** `Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -quit -logFile Logs/PlayModeTests.log`
- Checked-in build script YOK (Unity Build Settings).
- **MANUEL DERLEME YAPMA** -- Unity editor refresh'te otomatik derler. "Derleniyor mu?" diye harici komut calistirma.

## Klasor Yapisi
```
Assets/Scripts/
  ECS/{Components, Authoring, Systems, Physics}  - ECS data/baker/sistem/custom physics
  MonoBehaviour/                                 - manager + UI controller (ECS world'e kopru)
  Editor/                                        - tool/analyzer
  ScriptableObject/                              - BuildingConfigSO (LEGACY) + MobileCastle SO'lari (ArcherDefinition/TechTree/Council)
Assets/Scenes/NewGameScene*                      - AKTIF sahne + DOTS subscene
Assets/SmallScaleInt/                            - gorsel temel art (tileset + character creator)
Assets/Prefabs/UI/Generated/                     - aktif UI prefab'lari (MobileCastleHudRoot = TEK dogruluk kaynagi)
Assets/Tests/EditMode/                           - EditMode testleri (DeadWalls.EditMode.Tests)
CodexPreviews/                                   - eski UI tasarim onizlemeleri (HTML; tarihsel referans)
Assets/Docs/                                     - GDD + ROADMAP (LEGACY/baglam)
```

## UI Uretim Is Akisi (ZORUNLU KURAL)
UI dogrudan Unity prefab'i uzerinde uretilir/duzenlenir. TEK dogruluk kaynagi:
`Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab`.

> Eski "Codex export -> UIImporter" pipeline'i 2026-07-06'da KALDIRILDI (`Assets/UIExports/` +
> `Editor/UIBuilder/` silindi): iki-yazici senkron borcu/riski kokten bitirildi. Gerekirse git
> gecmisinden geri getirilebilir. `CodexPreviews/` tarihsel referans olarak durur.

Kurallar:
1. Yeni UI yuzeyi = prefab stage'de kur + controller'i `MonoBehaviour/`'da yaz + setup tool'a
   binding ekle (`MobileCastleSceneSetupWindow` isimle bulur/baglar).
2. **Binding isimleri SOZLESMEDIR** — yeniden adlandirma setup tool re-run'ini kirar.
3. Prefab polish'te mevcut HUD komple yeniden uretilmez; C# runtime davranisi ayri karardir.
4. Gorsel standart **plain Unity UI / text-first**: custom fantasy icon uretmeye calisma;
   panel/row/button'larda duz yari-opak renkler, builtin/simple Image yaklasimi, net text
   label'lar ve sabit satir olculeri. Ic ice gecme, tasan text, dekoratif icon zorlamasi kabul edilmez.
5. `EconomyFocusPanel` retired durumdadir: prefabda yoktur; setup tool bunu geri uretmez.

## Mimari Ozet
> Otorite: gercek frame sirasi `[UpdateBefore]`/`[UpdateAfter]`/`OrderFirst` ozniteliklerinden gelir.
> Kanonik kaynak: `Systems/SYSTEM_EXECUTION_ORDER_ARCHITECTURE.md`.

### Iki dunya: ECS <-> MonoBehaviour koprusu
- Simulasyon ECS'te (`World.DefaultGameObjectInjectionWorld`); Mono katmani UI + input.
- **Kopru:** Mono manager `EntityManager.CreateEntityQuery(typeof(T))` -> `GetSingletonEntity()`.
  `TryInitialize()` singleton'lar bake edilene kadar her frame `false` (lazy init).
- **`GameManager`** (singleton) merkez: her `Update()`'te `ReadECSData()` ECS singleton'larini public C#
  property'lere kopyalar (UI cache snapshot) + degisimde C# event firlatir; UI'in cagirdigi TUM mutation metotlari burada.
- **`UIManager`** panel orkestratoru (event'lere abone, panel ac/kapat).
- **3 singleton hub:** (1) GameState entity (`GameStateData`+`WaveStateData`+`ContinuousSiegeCycleData`+`Resource*`+`PopulationState`+`ArrowSupply`),
  (2) Castle entity (`WallSegment`+`GateComponent`+`CastleHP`), (3) `MobileCastleCombatConfig` entity
  (+`MobilePopulationAllocation`/`MobilePrepPauseState`/`MobileEconomyEventState`/`CastleYardPrepState`/`EconomyFocusState`).

### Mod anahtari: Mobile/Kale vs Legacy WallX
- `MobileCastleCombatConfig` singleton VARSA -> **mobil/kale modu** (AKTIF): CastleCenter'a yon, 360 spawn,
  AttackRadius, continuous DAY/DUSK/NIGHT dongusu. `GameManager.TryGetMobileConfigEntity()` tum ekonomi/wave mantigini dallandirir.
- Config YOKSA -> **legacy WallX modu** (fallback): zombiler sagdan, yon `WallXPosition` ile.

### Iki entity-olusturma pattern'i
1. **Authoring + Baker:** Castle/GameState/WaveConfig/MobileCastleCombat = sahne singleton'lari (subscene'de bake).
   Zombie/Arrow/Archer = bake edilmis PREFAB'lar (`*PrefabData` ref'i), runtime `Instantiate`.
2. **Runtime `CreateEntity`** (prefab/baker YOK): bina entity'leri (`BuildingGridManager`, `BuildingConfigSO`) -- **LEGACY**.

### Sistem yurutme sirasi (SimulationSystemGroup, ASCII)
```
# Frame basi (hepsi UpdateBefore WaveSpawn)
ContinuousSiegeCycleSystem   [OrderFirst, UpdateBefore WaveSpawn] continuous DAY/DUSK/NIGHT 60s cycle datasi
DayNightPrepSystem           [UpdateBefore WaveSpawn] LEGACY gun/gece state (ContinuousSiegeCycleData.Enabled true iken erken cikar)
BuildingProductionSystem  ->  BuildingPopulationSystem  ->  ArrowProductionSystem
  ->  MobilePopulationEconomySystem  ->  PopulationTickSystem  ->  BarracksTrainingSystem  ->  ResourceTickSystem
WaveSpawnSystem              [OrderFirst] wave spawn (stats her zombiye)
# Combat prep (UpdateAfter WaveSpawn, UpdateBefore ApplyMovementForce)
CastleYardPrepSystem -> ArcherShootSystem -> ArcherAnimationStateSystem ; ZombieSlowTimerSystem (frost)
# Physics pipeline (kati lineer)
ApplyMovementForceSystem -> BuildSpatialHashSystem -> PhysicsCollisionSystem -> IntegrateSystem -> BoundarySystem
# Post-physics (kati lineer, IJobEntity)
ZombieAttackTimerSystem(*) -> ArrowMoveSystem -> ArrowHitSystem -> ZombieDeathSystem
  -> ZombieAnimationStateSystem -> DamageApplySystem [*** TEK SYNC POINT ***] -> DamageCleanupSystem
# PresentationSystemGroup
SpriteAnimationSystem
```
- (*) `ZombieAttackTimerSystem` struct'i `Systems/ZombieAttackSystem.cs` icindedir (dosya/tip ismi farkli).
- **Pause guard:** `GameStateData.IsLevelUpPending` veya `IsGameOver` true iken combat durur (mobil modda XP esigi `IsLevelUpPending` TETIKLEMEZ).
- **Helper'lar (ISystem DEGIL, `public static class`):** `Systems/EconomyFocusUtility`, `Systems/MobileWaveUtility`, `Physics/SpatialHashGrid`.

### Sync point + spatial hash
- Fizik/post-physics sistemlerinin HICBIRI `CompleteDependency()` cagirmaz (hepsi `ScheduleParallel`).
  **TEK sync point** `DamageApplySystem` (sequential drain, Wall->Gate->Castle, GameOver).
- `BuildSpatialHashSystem` double-buffer: `ReadMap` (consumer okur, 1-frame-eski) + `WriteMap` (bu frame yazilan),
  her frame swap + `.Complete()` YOK. Consumer'lar (`PhysicsCollisionSystem`, `BoundarySystem`)
  `BuildSpatialHashSystem.ReadMap` static field'ini `[ReadOnly]` alir. Bedel: 1-frame-eski uzaysal veri.

### Zombie state akisi (`ZombieStateType`)
```
Moving --> Attacking   (kale/AttackRadius'a ulasti)
Moving --> Queued      (domino: durmus komsuya cakisti)
Queued --> Moving      (blocker gitti) ; Queued --> Attacking (kaleye ulasti)
  *    --> Dead         (HP<=0, ZombieDeathSystem)
```
- Gecislerin sahibi `BoundarySystem` (->Dead haric). **Frost/slow:** `ZombieSlow` enableable + `ZombieSlowTimerSystem` -> speed multiplier + mavi tint.
- **Domino queuing:** durmus komsuya cakisan Moving -> Queued; her frame 1 katman, dalga kaleden geri yayilir/cozulur (3x3 hash taramasi).

## Kurallar
### Kod Stili
- Namespace her zaman `DeadWalls`. ECS Systems: `partial struct` + `ISystem`, job'lar `[BurstCompile]`.
- **Static field erisimi olan ISystem struct'tan `[BurstCompile]` KALDIRILIR** (job'da kalir):
  `BuildSpatialHashSystem`, `PhysicsCollisionSystem`, `BoundarySystem`.
- Turkce yorum, Ingilizce identifier. Mimari kararda **once performansi** degerlendir.

### ECS Pattern
- Component sade data. System tek sorumluluk, mumkunse `IJobEntity`. Komsu okuma `ComponentLookup` +
  `[ReadOnly] [NativeDisableContainerSafetyRestriction]` (her entity yalniz KENDI verisini yazar).

### MD Dokumantasyonu (ZORUNLU)
- Yeni sistem/klasor -> o klasore SPESIFIK `*_ARCHITECTURE.md` + `*_EDITOR_SETUP.md`. Mevcut klasore ekleme -> mevcut MD'leri GUNCELLE.
- ASLA generic isim (`README.md`/`NOTES.md`) yok. Mevcutlari kesfet: `glob **/*_ARCHITECTURE.md`.

## Editor Tool'lari (`Assets/Scripts/Editor`)
- **MobileCastleSceneSetupWindow** -- NewGameScene + subscene + WorldVisualRoot iskeletini tek tikla (idempotent) kurar. (AKTIF is akisinin merkezi)
- **ArenaMapGeneratorWindow** -- seed-tabanli tek-tik izometrik arena uretici (gorsel). WorldVisualRoot tilemap'lerine biome zemin + noise-blend gecis + dekor + dekoratif yapi boyar; canli-sahne onizleme, tek-undo. Tile = Fantasy kingdom Tileset (duz Tile asset). Bkz. `ARENA_MAP_GENERATOR_ARCHITECTURE.md`.
- **MapImporterWindow / GroundTileMapperWindow / BuildingTileComposerWindow** -- harita/zemin/bina tile araclari (cogu LEGACY town tarafi).
- **SpriteAtlasGenerator** -- 4 karakter animasyon PNG'sini tek atlasa birlestir.
- **ProfilerDataAnalyzer** -- `.raw` profiler dosyasini A/B karsilastirmali rapora donustur.

## Onemli Surec Kurallari (tam liste: `AGENTS.md`)
- **Iletisim her zaman Turkce.** ONCE PLAN sonra EXECUTE (sorma, otomatik yap). Soruya once CEVAP ver, sonra tool.
- **GitHub/remote islem YOK** (push, PR, issue, release, tag, remote branch, upload, `gh`) -- sahip izin vermedikce.
- **Yikici git YOK** (reset/checkout/clean/rebase/branch silme) -- acik istek olmadan. Read-only git serbest.
- Gorsel/tile mapping'de TAHMIN YAPMA -- her tile'i dogrula.

---

## Legacy / Broader Repo Context (silinmedi -- guncel yon DEGIL)
Asagidakiler tarihsel/genis baglamdir. Kod hala derde ama AKTIF mobile loop'ta KULLANILMAZ:
- **RTS / town-building sistemi:** `BuildingGridManager` (hibrit `int[,]` truth source + Tilemap + runtime ECS entity),
  `BuildingConfigSO`, `BuildingPlacementUI`, `BuildingDetailUI`, `EconomyFocusUI` -- bunlar **NewGameScene'de YOK**
  (setup tool bazilarini destroy eder). Mobile ekonomi sol `WorkerEconomyDrawerUI` drawer'ina gecti; `CastleEconomyUI` legacy/debug.
- **`Assets/Docs/ROADMAP.md`** -- 2026-03-18'e kadar milestone gecmisi. "TEK takip kaynagi" ve "AKTIF M-ISO"
  iddialari ARTIK GECERSIZ. Guncel yon icin yukaridaki "Current Active Direction"a guven.
- **GDD v4.0/v3.0/v2.0** (`Assets/Docs/`) -- tasarim vizyonu/genis scope referansi, gorev takibi DEGIL.
- **M-ISO (izometrik grid + perimeter wall)** -- terk edildi. M-ISO.5 (8-yonlu sprite atlas + `ZombieAnimationStateSystem`) gibi
  bazi ciktilari mobile loop'ta kullaniliyor, ama izometrik gecis hedefi aktif degil.
