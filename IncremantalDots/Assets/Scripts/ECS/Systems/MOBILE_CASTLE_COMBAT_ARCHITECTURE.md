# Mobile Castle Combat v2 - Mimari

## Tuning sahipliği

Baseline tuning tek bir precedence hattından geçer: quantity/difficulty alanları `DifficultyProfileSO`, enemy prefab ve base statları aktif `EnemyDefinitionSO`, diğer alanlar aktif SubScene `MobileCastleCombatAuthoring`, birleştirme kodu `MobileCastleTuningResolver` + Baker, runtime çıktı `MobileCastleCombatConfig`. Tech/meta/Council değişimleri baseline üzerine effective aggregate'dir. Ayrıntılı sözleşme `Assets/Scripts/ECS/Authoring/MOBILE_CASTLE_TUNING_ARCHITECTURE.md` dosyasındadır.

## Amac

`NewGameScene` icin merkezi kale combat akisini, continuous day/dusk/night kusatma dongusunu ve mobile archer/worker economy drawer davranisini tasir. Mobil davranis sadece sahnede `MobileCastleCombatConfig` singleton'i bake edildiginde aktif olur.

## Mode Switch

- `MobileCastleCombatConfig` varsa oyun mobile castle mode'dadir.
- `MobileCastleCombatConfig` yoksa sistemler eski `WallXPosition` akisina doner.
- `NewGameScene` TEK CEPHE duzeni kullanir (asagidaki bolum); eski sahneler config yoksa kendi akisinda kalir.

## Tek Cephe (K4 pivotu, M-0 — 2026-07-06)

`SingleFrontEnabled=true` (default) iken 360-ring TERK EDILIR; dusmanlar YALNIZ SAGDAN gelir:

- **Spawn:** gizli sag serit — x = `SpawnLineX`(27) + 0..2 jitter, y =
  +-`SpawnBandYHalf`(6.5). Sonuc `X 27..29`; Android max aspect 2.4, kamera sarsintisi
  ve zombi quad yaricapi dahil ekran disindadir.
  (WaveSpawnSystem; batch/intensity/eskalasyon mantigi DEGISMEDI, yalniz dogum yeri degisti).
- **Hareket:** hedef `(FrontlineX, kendi y)` — duz sola akis (ApplyMovementForceSystem).
- **Saldiri gecisi:** `pos.x <= FrontlineX + AttackRadius` esiginde Attacking; duvar bariyeri
  yalniz x'i sabitler, y'de yigilma serbest (BoundarySystem). Domino kuyruk fizigi aynen calisir.
- **Arena siniri:** x = [FrontlineX, SpawnLineX+4], y = +-(SpawnBandYHalf+2).
- **Gorsel giris bantlari:** battlefield `X 4..18`, far-right frame `X 18..27`, hidden
  spawn ground `X 27..29`. Frame, zombinin dogumunu degil cepheye girisini gosterir.
- **Tempo notu:** `SpawnLineX` 13 -> 27 degisimi base hiz 0.85'te ilk duvar temasini
  yaklasik 16.5 saniye geciktirir. Bu degisiklikte hiz/intensity telafisi yapilmadi;
  offscreen birikim ve ilk baski ayri Play Mode denge kontrolu ister.
- **Hendek:** world-art bandı ve legacy `MoatSystem` kodu korunur fakat V1 gameplay'de dormant'tır.
  Baker/runtime aggregate `MoatGameplayEnabled=false`, slow `1`, damage `0` yazar.
  `moat_dig`, `moat_flame` ve `start_moat` assetleri aktif tech/meta catalog'larında bulunmaz.
  Ayrıntılı sınır `MOAT_DORMANCY_ARCHITECTURE.md` dosyasındadır.
- **Okcu yerlesimi:** oncelik tilemap hucresi AMA yalniz `x <= FrontlineX+1` bolgesindekiler
  gecerli (eski 360 hucreleri elenir); yoksa duvar kolonu fallback'i (x = FrontlineX-0.8,
  ortadan disa dikey dizilim). Owner duvar/kule tile'larini boyayinca tilemap oncelik kazanir.
- **Kamera:** sabit tek ekran — pozisyon (4.5, 0, -10), ortho 8 (setup tool normalize eder).
- **Animasyon/feedback:** yon hedefi ve kale-vurus VFX konumu duvar hattina baglanir.
- **Geri alma:** `SingleFrontEnabled=false` -> eski 360-ring davranisi aynen doner (karsilastirma/test icin).

2026-07-06 tarihli hendek slow/damage doğrulaması yalnız legacy implementasyonun tarihsel
kanıtıdır; V1 ürün davranışı değildir. 2026-07-12 runtime regresyonu stale Moat tuning'inin
zombie HP/hız/slow state'ini değiştiremediğini kanıtlar.

## ECS Verisi

Savunma sonucu için tek runtime owner `WallSegment`tir. Gate/Core component ve HUD referansları yalnız legacy serialization uyumluluğudur; ayrıntılı sınır `SINGLE_WALL_DEFENSE_ARCHITECTURE.md` dosyasındadır.

- `MobileCastleCombatConfig`: kale merkezi, spawn radius, attack radius, wave/siege sayilari, spawn batch, zombie scale/speed, continuous siege tuning, reward tuning, worker economy tuning, event tuning ve stress test limitlerini tutar.
- `EnemyCatalogRuntimeData` + `EnemyCatalogEntryData`: aktif enemy index'ini; prefab, base stats, scale, XP ve pool metadata'sini tutar. V1'de buffer tek kayittir.
- `EnemyPoolRuntimeData` + `EnemyPoolAvailable`: prewarm/expand rezervini ve rent/return telemetry'sini tutar.
- `ArrowPoolRuntimeData` + `ArrowPoolAvailable`: ok prewarm/expand rezervini ve rent/return telemetry'sini tutar.
- `ContinuousSiegeCycleData`: player-facing `DAY / DUSK / NIGHT` fazini, 60s cycle progress'ini, spawn intensity multiplier'i ve horde pressure degerini tutar.
- `ContinuousSpawnBudgetData`: day tabanı ile phase multiplier'ını ayrı tutar; pending enemy backlog'u ve demanded/spawned runtime telemetry sayaçlarını taşır.
- `WaveStateData.Phase`: mobile continuous modda uyumluluk icin `NightCombat` aktif tutulur. Eski DayPrep akisi component seviyesinde kalir ama `ContinuousSiegeCycleData.Enabled` true iken player-facing akisi yonetmez.
- `EconomyFocusState`: eski focus akisi icin korunur. Worker economy aktifken player-facing UI bunu kullanmaz.
- `WaveClearRewardData`: son wave clear bonusunu HUD toast'i icin saklar.
- `CastleYardPrepState`: day prep'te alinan `Fortify` ve `Rally` tek-gecelik buff state'ini tutar.
- `MobilePopulationAllocation`: Wood/Stone/Iron/Food worker dagilimini ve DayPrep growth/event roll checkpoint'lerini tutar.
- `MobilePrepPauseState`: Castle Interior paneli acikken prep timer ve resource tick durdurmak icin kullanilir.
- `MobileEconomyEventState`: pending event, cooldown ve secilmis temporary production bonusunu tutar.
- `ArcherSlotPosition`: legacy/manual pozisyon buffer'idir; mobile NewGameScene tilemap spawn akisi bunu kullanmaz.
- `ArcherUnit`: okcu tipi, fire rate, hasar, range ve opsiyonel slow degerlerini tutar.
- `ArrowProjectile`: hedef entity, hasar, projectile effect datasini ve kalan lifetime'i tasir.
- `ArrowSupply`: finite stok ile run ici Capacity/Efficiency seviyelerini tasir.
- `ZombieSlow`: Frost ok etkisini enableable component olarak tasir.

Varsayilan mobile degerleri:

- Castle center: `(0, 0)`
- Spawn radius: `11`
- Attack radius: `1.35`
- Base wave enemy count: `30`
- Extra enemies per wave: `10`
- Spawn batch size: `2` (Difficulty Profile)
- Zombie scale: `1.4`
- Base zombie speed: `0.85`
- Zombie speed per wave: `0.04`
- Stress spawn batch size: `25`
- Stress spawn interval: `0.1`
- Stress max alive zombies: `1500`
- Kill reward: Wood `1.0`, Food `0.6`, Stone `0.25`, Iron `0.15`, wave scale `0`
- Wave clear bonus: Wood `20 + 6 per wave`, Food `15 + 5 per wave`, Stone `10 + 4 per wave`, Iron `6 + 3 per wave`
- Worker economy: profile-owned Dawn request/Food `15 / 1`; authoring initial beds `60`, initial workers Wood/Stone/Iron/Food `20 / 10 / 8 / 15`
- Worker caps: Wood/Stone/Iron/Food `40 / 30 / 24 / 40`
- Worker production baseline: Wood/Stone/Iron/Food `8 / 5.5 / 4.9 / 7` per minute
- Worker economy reward multiplier: `0.25`
- Economy event chance `15%`, cooldown `2` waves
- Continuous siege cycle: total `60s`, Day `30s`, Dusk `5s`, Night `20s`, Dawn `5s`
- Continuous siege intensity: Day `0.55`, Dusk `1.00 -> 1.35`, Night `1.65`
- Initial/day prep duration fields legacy/debug akis icin korunur
- Day overlay alpha: `0`
- Night overlay alpha: `0.50`
- Initial finite Arrow supply: `200`
- Wave director: base interval `0.95`, wave multiplier `0.96`, min interval `0.35`
- Wave director phases: opening `20%` at interval `x1.35` and batch `-1`, final `20%` at interval `x0.65` and batch `+1`
- Castle Yard: Fortify/Rally runtime state korunur, player-facing drawer'da gizlenir

## Continuous Day/Dusk/Night Run Loop

Mobile normal mode artik player-facing wave clear veya `Start Next Wave` beklemez:

1. `ContinuousSiegeCycleSystem`, 60 saniyelik cycle timer'i ilerletir.
2. UI fazi sadece `DAY`, `DUSK`, `NIGHT` olarak gosterilir; `DAY 03` gibi wave numarasi yazilmaz.
3. Day fazinda spawn dusuk tempo akar, Dusk fazinda kararir ve tempo yukselir, Night fazinda baski yuksek kalir.
4. `WaveStateData.WaveActive = true` tutulur; eski market/prep dur-kalk akisi tetiklenmez.
5. `WaveSpawnSystem`, continuous cycle intensity degerine gore interval ve batch size ayarlar.

Stress mode bu akisi atlar; stress spawn davranisi korunur.

## Render Depth Bands

Mobile castle render sirasi shader degistirilerek degil, world z bandlariyla cozulur. Kamera z `-10` oldugu icin daha negatif z daha onde kabul edilir: back tilemap `0`, unit band `-1`, front occluder `-2`, projectile `-2.5`. `inside/outside/outside0` arka gorsel katmanlaridir; `outside2` front-wall/occluder katmanidir. `DeadWalls/SpriteSheet` shader'i `Opaque/Geometry` kalir; `Transparent`, `AlphaTest`, `TransparentCutout` veya `ZWrite Off` kullanilmaz.

## Sistem Davranisi

### WaveSpawnSystem

Continuous siege aktifken `WaveSpawnSystem`, wave clear kontrolüne girmez. `ContinuousSpawnBudgetUtility` günlük count/batch/interval tabanını phase multiplier'dan ayrı hesaplar. Alive cap doluysa geçen interval talebi `ContinuousSpawnBudgetData.PendingEnemies` içinde korunur; kapasite açılınca frame başına `MaxSpawnBatch` sınırıyla sahaya aktarılır. Spawn prefabı ve entity base statları aktif `EnemyCatalogEntryData` kaydından gelir; entity `EnemyPoolRuntimeUtility.TryRent` ile alınır ve rezerv boşsa definition batch değeriyle genişler. Enemy tipine özel dal yoktur. İç tarafta `CurrentWave` cycle index olarak tutulur; `MobileWaveUtility.ConfigureMobileWave()` count ve base interval'i günceller, enemy HP/damage/speed'i sabit tutar. UI wave veya backlog sayısını player-facing olarak göstermez.

Legacy mobile wave director akisi `ContinuousSiegeCycleData.Enabled` false yapilirsa hala calisabilir: opening/mid/final fazlari `ZombiesSpawned / ZombiesToSpawn` oranindan hesaplanir ve wave temizlenince DayPrep'e doner. Varsayilan NewGameScene akisi continuous siege'dir. Stress mode aciksa mobile config'teki stress batch/interval/cap kullanilir, reward verilmez ve continuous/legacy wave director fazlari calismaz.

Wave clear bonus normal mobile modda bir kez verilir. Worker economy aktifken bonus `WorkerEconomyRewardMultiplier` ile azaltilir. Bonus miktari `WaveClearRewardData` uzerine de yazilir; HUD bu veriyi kisa `Wave Cleared +...` feedback'i icin kullanir.

### ResourceTickSystem

Worker economy aktifken `MobilePopulationEconomySystem` `ResourceProductionRate` degerlerini worker allocation'a gore yazar. Continuous siege aktifken her tamamlanan 60 saniyelik cycle, population'a `PopulationGrowthPerDayPrep` kadar yeni nufus ekler. `MobilePrepPauseState.IsPaused` true iken `ResourceTickSystem` resource accumulator ilerletmez. `MobilePopulationAllocation` yoksa eski economy focus multiplier akisi korunur.

### MobilePopulationEconomySystem

Mobile mode'da worker allocation'i resource cap ve population'a gore clamp eder, `PopulationState.Workers/Idle` degerlerini gunceller ve resource production rate'lerini yazar. Continuous siege varsayilaninda her tamamlanan cycle basina population growth uygular. Legacy DayPrep akisi acilirse completed wave sonrasi DayPrep basinda ayni growth degerini kullanir. Event cooldown/sans roll'u legacy DayPrep akisinda burada yapilir; pending event secilmezse gece baslarken expire eder.

### ApplyMovementForceSystem

Mobile mode'da moving zombiler tek-cephe modunda `(FrontlineX, kendi y)` hedefine (duz sola), 360 modunda `CastleCenter` noktasina dogru kuvvet alir. V1'de aktif `ZombieSlow` kaynağı Frost oklarıdır; dormant Moat sistemi bu state'i değiştiremez.

### BoundarySystem

Mobile mode'da zombi `AttackRadius` icine girince `Attacking` state'e gecer ve kale etrafindaki attack ring'e sabitlenir. `NewGameScene` setup default'u `1.35` olarak SmallScaleInt kale footprint'iyle hizalanmistir; bu sadece durma/saldirma cemberini etkiler, spawn yonu ve pathfinding davranisini degistirmez.

### ArcherShootSystem

Okcu hedefleme sistemi persistent `2.0` cell coarse spatial map'i Burst-parallel kurar;
ateşe hazır okçular read-only map üzerinde range içindeki yaşayan en yakın hedefi
seçer. Uçuşta olan ve aynı frame oluşturulan okların incoming damage'i target HP'yi
karşılıyorsa sonraki okçu o hedefi atlar; bütün range lethal olarak rezerve edilmişse
o frame bekler. Basic, Rapid ve Frost aynı policy'yi kullanır.
`CastleYardPrepState.RallyTimer > 0` ise fire-rate hesabına
`RallyFireRateMultiplier` uygulanır.

- Basic: fire rate `1.5`, damage `10`, range `15`
- Rapid: fire rate `3.0`, damage `6`, range `14`
- Frost: fire rate `1.2`, damage `5`, range `14`, slow `2s`, multiplier `0.55`

Okcu ve ok projectile gorunurlugu `SpriteTint` ile okunur:

- Basic: beyaz/notr
- Rapid: sicak sari
- Frost: soguk mavi

`ArcherShootSystem`, pool'dan rent edilen oka okcu tipinden gelen tint'i yazar.
Ayni `Arrow.prefab` ve `ArrowMat` kullanilmaya devam eder.
Stok `0` iken atış durur. Pool rent'i başarılı olduktan sonra tam `1 Arrow` düşer;
pool rezervi tükenmişse stok ve fire timer değişmez. Refill ve run içi capacity/efficiency
yatırımları `ARROW_AMMO_ARCHITECTURE.md` sözleşmesinde tanımlanır.
Atis aninda `CombatSfxEvent.ArrowShoot` uretilir; shoot particle V1'de kapali tutulur. Playback main scene `CombatFeedbackBridge` tarafindadir.

Atis aninda okcunun `FacingDirection` degeri hedef zombiye gore guncellenir ve
`AttackAnimTimer` baslatilir. `ArcherAnimationStateSystem`, timer aktifken attack
row'unu, timer bitince ayni yonde idle row'unu oynatir.

Target tie-break entity index/version ile stabildir. Pool generation uyuşmazlığında
uçuşta olan ok kendi pool'una döner; retarget yapılmaz. Ayrıntı:
`ARCHER_TARGETING_ARCHITECTURE.md` ve `ARROW_POOL_ARCHITECTURE.md`.

### ArrowHitSystem + ZombieSlowTimerSystem

Frost ok hedefe vurunca `ZombieSlow` enable edilir veya yenilenir. Slow stack yapmaz; ayni hedefe tekrar vurmak duration'i refresh eder. Timer bitince slow pasiflenir ve multiplier tekrar `1` olur.

`ZombieSlowTimerSystem`, slow aktifken zombi `SpriteTint` degerini soguk/mavi yapar.
Slow biterse veya zombi Dead state'e gecerse tint normal beyaza doner; death akisi
ayri kalir.
Isabet aninda Basic/Rapid ve Frost ham hit basina feedback entity'si uretmez.
`ArrowHitSystem`, ayni `0.75` world-unit hucredeki ayni hit turunu tek spatial candidate'a
indirir. Ardindan frame basi en fazla `24` `CombatVfxEvent` uretir; iki tur ayni
frame'de mevcutsa ikisi de slot alir. Arrow/Frost icin ayri ayri yalniz birer
`CombatSfxEvent` uretilir ve `Multiplicity` temsil edilen candidate sayisini tasir.
Hit VFX hedef pozisyonunda kisa sprite flipbook impact olarak oynar; normal arrow/frost
hit ParticleSystem prefab'i kullanmaz. Bridge, ikinci guvenlik kati olarak `24` hit
flipbook/frame ve `0.04s` burst rate-limit uygular; ilgili ses clip'i atanmadiginda cue
sessizce atlanir. Bu budget yalniz feedback'i azaltir; damage, slow ve pool return
islemleri her isabet icin aynen uygulanir.

Presentation hierarchy ordinary hit'i `Wall/12`, sampled Frost ring/hit'i `Wall/47-48`,
Fireball projectile'i `Wall/219-220` ve blast katmanlarını `Wall/230-232` bandında tutar.
Frost ve Fireball world pozisyonu `MobileCastleRenderDepth.ProjectileZ` bandına normalize edilir;
opaque/depth-write enemy renderer'larında sorting order tek başına görünürlük garantisi değildir.
Frost ring mevcut hit pool slotunda, Fireball aura/core/ring ise sabit yeniden kullanılan
renderer'larda yaşar; 10K enemy sayısı yeni feedback nesnesi üretmez. Ayrıntı:
`Assets/Scripts/MonoBehaviour/SPELL_FEEDBACK_HIERARCHY_ARCHITECTURE.md`.

## HUD Readability

`GameManager`, mevcut `ArcherUnit` entity'lerinden Basic/Rapid/Frost sayilarini okur.
Archer count bilgisi sag drawer row'larinda okunur. `ArcherTypeText` eski HUD placeholder'i imported drawer UI ile cakisabildigi icin mobile setup tarafindan kullanilmaz.

Yeni imported HUD varsa cycle paneli player-facing zaman bilgisini gosterir:

- `CyclePhaseText`: `DAY`, `DUSK` veya `NIGHT`
- `CycleDayLabelText`, `CycleDuskLabelText`, `CycleNightLabelText`: segment label'lari
- `CycleProgressFill` ve `CycleProgressMarker`: 60s dongu progress'i
- Forecast/pressure surface: aktif prefab ve `HUDController` binding sozlesmesinde bulunmaz; `HordePressure01` spawn/gameplay sinyali olarak korunur
- Fallback eski HUD varsa wave text sadece `DAY/DUSK/NIGHT`, kills text ise hedef sayi olmadan `KILLS x` yazar.

HUD varsa `CastleDefensePanel` uzerindeki tek `DefenseWallFill` ve Wall yuzdesini gunceller. Legacy Gate/Core track ve text'leri runtime'da gizlenir. Night/high pressure durumunda threat rengi kullanilabilir; Wall hasar aldiginda kisa red flash feedback'i verilir.

Sol ust economy HUD mevcut kaynaklari gosterir: Wood, Stone, Iron, Food, Population, Arrows. Runtime text'ler label tekrar etmez; kutu basligi UI'da, value/rate text'i kod tarafindadir. HUD rate degeri mobile population allocation tarafindan yazilan worker production'dir. NewGameScene mobile default'lari:

- Wood `280`, Stone `120`, Iron `70`, Food `220`, Population `60`, Arrows `200 / 200`
- Initial workers: Wood `20`, Stone `10`, Iron `8`, Food `15`
- Worker caps: Wood `40`, Stone `30`, Iron `24`, Food `40`
- Worker income: active profile base Wood `8/min`, Stone `5.5/min`, Iron `4.9/min`,
  Food `7/min` per assigned worker

## Continuous Recruitment Loop

Mobile castle mode'da XP level-up pause veya kart paneli tetiklemez. Oyun dongusu surekli devam eder: zombi oldur, kaynak biriktir, sol worker drawer'dan ekonomi buyut, sag recruitment drawer'dan okcu satin al.

### Sabit oyun hızı

Player-facing run hızı `1x` sabittir; `2x/4x`, fast-forward veya benzeri bir hızlandırma
kontrolü yoktur. `SimulationPauseService` yalnız modal/pause lease'leri için `0` kullanır ve
önceki değeri exact geri yükler. `UIManager` Game Over sunumunda kısa süreli `0.25x` yavaşlatma
kullanabilir; bu oyuncu kontrollü hızlandırma değildir. Production runtime'da `1` üstü
`Time.timeScale` ve `Time.fixedDeltaTime` yazıcısı yasaktır.

`LongRunSimulatorWindow` içindeki `1-5x` yalnız Editor test aracıdır; player build/runtime
sözleşmesine girmez. Production scene pause kontrolü taşıyabilir fakat speed control taşıyamaz.
Bu sahiplik `NoSpeedOfflineProgressContractTests` ile kilitlidir.

Legacy level-up kart API'si eski akis icin kodda durabilir, ama `MobileCastleCombatConfig` varken `DamageCleanupSystem` XP threshold'u `IsLevelUpPending` yapmaz.

Mobile normal mode'da `DamageCleanupSystem`, death timer biten zombiler icin kill reward'i `ResourceAccumulator` uzerine yazar ve entity'yi pool rezervine dondurur. Worker economy aktifken kill reward `WorkerEconomyRewardMultiplier` ile azaltilir; ana gelir kaynagi worker allocation'dir. Continuous siege varsayilaninda wave clear bonus/player-facing clear akisi tetiklenmez, fakat cycle tamamlandikca population growth uygulanir. Stress mode'da reward verilmez.

## Castle Interior Economy

`EconomyFocusUI` mobile loop'ta artik kullanilmaz; setup tool eski `EconomyFocusPanel` ve focus child objelerini root'tan soker. Ekonomi yonu sol worker drawer ve kale ici worker site gorselleriyle belirlenir.

- `CastleInteriorClickTarget` ve `CastleEconomyUI` legacy/debug akisi olarak kalir; player-facing ana worker kontrolu sol drawer'dadir.
- `WorkerEconomyDrawerUI` her zaman acilip kapanabilir; DayPrep sartina bagli degildir.
- Wood/Stone/Iron/Food `+1% / +10% / +100% / direct input` kontrolleri target ratio'yu aninda ve ucretsiz degistirir; tap progress yoktur.
- Secilen hedef exact kalir, diger uc hedef deterministik yeniden olceklenir ve toplam `%100` olur.
- Target degisikligi mevcut worker'lari yeniden atamaz; yalniz sonra gelen population hedef acigina gore dagilir.
- Eski worker slider'lari debug/legacy olarak kalabilir ve ayni `MobilePopulationAllocation` verisini degistirir.
- Worker assignment toplam worker sayisini `Population.Total - Population.Archers` ustune cikaramaz.
- `GameManager`, actual worker allocation'i `WorkerVisualRepresentationUtility` ile Low/Medium/High temsili sayiya cevirir; resource basina en fazla `32` `WorkerPrefabData.WorkerPrefab` entity'si spawn/destroy eder.
- Villager worker pickup pozisyonlari main scene `CastleInteriorEconomyArea/*Site/WorkerSpawnPoints` marker'larindan gelir.
- Delivery pozisyonlari `CastleInteriorEconomyArea/CastleWorkerHub/DeliveryPoints` marker'larindan gelir.
- `WorkerLogisticsMovementSystem`, villagerlari pickup ile hub arasinda yuruturek kaynak tasima feedback'i verir.
- `PopulationGrowthPerDayPrep` legacy DayPrep akisi icin isimlendirilmis eski config alanidir;
  continuous siege varsayilaninda her Dawn request miktari olarak kullanilir. Aktif baseline
  `DifficultyProfileSO`dan resolver ile gelir; authoring alani profile yoksa fallback'tir.
- Okcu satin almak `1` idle population kullanir; idle yoksa buy disabled olur ve drawer `NEED POP` yazar.
- Editor testleri icin `GameManager.Free Economy Test Mode` acilirsa okcu satin alma population harcamaz ve resource/population eksigi aksiyonlari bloklamaz.
- Nadir eventler `MobileEconomyEventState` ile tutulur; secilmezse gece baslarken expire eder.

## Archer Economy Drawer

`MarketUI`, `MobileCastleHudRoot` altindaki `ArcherDrawerPanel` controller'idir. Drawer combat sirasinda acilip kapanir ve `Time.timeScale` degistirmez. Sag drawer'in player-facing rolu archer recruitment ve Basic -> Rapid/Frost retrain'dir; ayrı upgrade/level ve tech unlock aksiyonlari burada gosterilmez.

- Basic baslangicta unlocked.
- Rapid/Frost kilitli baslar; unlock ileride full-screen Tech Tree tarafindan yapilacaktir.
- Unlock sonrasi retrain mevcut bir Basic entity'yi yerinde hedef type/stat/tint'e cevirir; population, toplam archer ve cap kullanimi degismez.
- Buy ve retrain maliyeti hedef türün mevcut sayısına göre `ArcherDefinitionSO` tuning'iyle büyür.
- Basic Buy: `45W + 20F`.
- Rapid Buy: `55W + 35I + 20F`.
- Frost Buy: `45W + 55S + 25I`.
- Yetersiz kaynak varsa row `CostText` mevcut cost yanina `NEED ...` ekler; idle population yoksa buy cost yanina `NEED POP` ekler. Locked tiplerde row `LOCKED BY TECH` gosterir. Yeni UI elemani gerekmez.
- `Free Economy Test Mode` sadece lokal test kolayligi icindir; acikken UI costlari `FREE` gosterir ve satin alma kaynak/population eksiginden bloklanmaz.

Type level ve upgrade API'leri kodda korunur, fakat player-facing sahibi sag drawer degildir. Ileride Tech Tree node'lari ayni tip mevcut ve future okculara su scaling'i uygulayabilir:

- Damage `+12%`
- FireRate `+8%`
- Frost SlowDuration `+0.15s / level`
- Frost SlowMultiplier her level `-0.02`, minimum `0.40`

## Legacy Prep API'leri

Continuous siege varsayilaninda wave bittiginde oyun durmaz, `GameManager.OnWaveCompleted` tetiklenmez ve player-facing `Start Next Wave` yoktur. Asagidaki API'ler legacy/debug veya ileride farkli mode icin kodda kalabilir:

- `Repair`: legacy DayPrep sirasinda aktiftir ve `GameManager.RepairDefenseFull()` ile wall/gate/castle HP full olur.
- `Fortify` ve `Rally`: runtime API olarak kalir, fakat Castle Interior economy ekrani geldikten sonra player-facing drawer'da gizlenir.
- `Arrow Refill`: Arrow chip'inden açılan tek satırlık panelde +1/+5 paket, Buy Max ve Capacity/Efficiency yatırımları gösterilir.
- `Start Next Wave`: debug/public API olarak kalir, player-facing UI'da gizlidir.

## Okcu Yerlesimi

Mobile mode'da okcular `NewGameScene` main scene'indeki `Grid/outside` tilemap'i uzerinde Formation V1'in deterministik 40 x 25 noktalarina yerlestirilir. `inside` ve `outside2` yalnizca kale gorsel katmanidir.

- Spawn tilemap: `outside`
- Spawn Z: `-1`
- Formation asset'i: `Assets/ScriptableObject/MobileCastle/Archers/ArcherFormationV1.asset`
- Tam 40 canonical hucre data/version ile sabittir; tilemap contract'i bu hucrelerin hepsini icermelidir
- Her hucrede tile coordinate + local slot seed'iyle 25 diamond-inset nokta uretilir ve minimum local mesafe korunur
- Fill order once butun 40 hucrenin slot 0'i, sonra slot 1'i olacak bicimde layer mantigiyla kurulur
- Save world position yerine formation version + type count tutar; Continue ayni 1000 noktayi yeniden uretir
- Okcu render'i Entities Graphics uyumlulugu icin `Opaque/Geometry` shader ile calisir. `outside2` Wall/4 ve okcu prefab Wall/3 sirasi korunur, fakat shader/depth kisitlari nedeniyle front-wall occlusion icin sonraki cozum shader'a dokunmadan kurulmalidir.

Basic, Rapid ve Frost toplamı `ArcherCapacityUtility.MaxTotalArchers = 1000` ortak
hard cap'ini paylaşır. Formation V1 de tam `1000` pozisyon üretir; 1001. spawn satın alma,
Council, meta, restore ve başlangıç yollarının ortak kapısında reddedilir.

## World Visuals

World visual tilemap'leri main scene tarafinda owner kontrolundedir. Setup tool artik world visuals uretmez veya boyamaz.

Tek Wall cani `CastleAuthoring` tarafindan bake edilen `WallSegment` verisinden gelir; Gate/Core sonuc zincirinde yoktur. Zombi spawn'i tam random 360 kalir. Gorsel katmandan okunan tek runtime veri `outside` tilemap okcu spawn hucreleridir.

## Stress Test

- `StressTestMode = true`: wave/gameover beklemeden surekli zombi spawn eder.
- Stress mode'da zombi hasari uygulanmaz; wall/gate/castle HP dusmez ve game over beklenmez.
- HUD enemy text'i `Zombies: alive (max X)` formatinda o oturumda gorulen max alive sayisini gosterir.

## Bilerek Yapilmayanlar

- Enemy variety yok; tek tip zombi kalir.
- Enemy pool prewarm/expand/rent/return aktiftir; 10k urun senaryosu ve profiling `DW-B-SCALE` isidir.
- RTS-style manuel okcu kontrolu yok.
- Nufus hard cap bu milestone'da okcu limitine baglanmaz.
- Yeni coin yok; mevcut kaynak sistemi kullanilir.
