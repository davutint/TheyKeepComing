# Mobile Castle Combat v2 - Mimari

## Amac

`NewGameScene` icin merkezi kale combat akisini, continuous day/dusk/night kusatma dongusunu ve mobile archer/worker economy drawer davranisini tasir. Mobil davranis sadece sahnede `MobileCastleCombatConfig` singleton'i bake edildiginde aktif olur.

## Mode Switch

- `MobileCastleCombatConfig` varsa oyun mobile castle mode'dadir.
- `MobileCastleCombatConfig` yoksa sistemler eski `WallXPosition` akisina doner.
- `NewGameScene` 360 derece kale savunmasi kullanir; eski sahneler config yoksa kendi akisinda kalir.

## ECS Verisi

- `MobileCastleCombatConfig`: kale merkezi, spawn radius, attack radius, wave/siege sayilari, spawn batch, zombie scale/speed, continuous siege tuning, reward tuning, worker economy tuning, event tuning, unlimited arrow flag'i ve stress test limitlerini tutar.
- `ContinuousSiegeCycleData`: player-facing `DAY / DUSK / NIGHT` fazini, 60s cycle progress'ini, spawn intensity multiplier'i ve horde pressure degerini tutar.
- `WaveStateData.Phase`: mobile continuous modda uyumluluk icin `NightCombat` aktif tutulur. Eski DayPrep akisi component seviyesinde kalir ama `ContinuousSiegeCycleData.Enabled` true iken player-facing akisi yonetmez.
- `EconomyFocusState`: eski focus akisi icin korunur. Worker economy aktifken player-facing UI bunu kullanmaz.
- `WaveClearRewardData`: son wave clear bonusunu HUD toast'i icin saklar.
- `CastleYardPrepState`: day prep'te alinan `Fortify` ve `Rally` tek-gecelik buff state'ini tutar.
- `MobilePopulationAllocation`: Wood/Stone/Iron/Food worker dagilimini ve DayPrep growth/event roll checkpoint'lerini tutar.
- `MobilePrepPauseState`: Castle Interior paneli acikken prep timer ve resource tick durdurmak icin kullanilir.
- `MobileEconomyEventState`: pending event, cooldown ve secilmis temporary production bonusunu tutar.
- `ArcherSlotPosition`: legacy/manual pozisyon buffer'idir; mobile NewGameScene tilemap spawn akisi bunu kullanmaz.
- `ArcherUnit`: okcu tipi, fire rate, hasar, range ve opsiyonel slow degerlerini tutar.
- `ArrowProjectile`: hedef entity, hasar ve projectile effect datasini tasir.
- `ZombieSlow`: Frost ok etkisini enableable component olarak tasir.

Varsayilan mobile degerleri:

- Castle center: `(0, 0)`
- Spawn radius: `11`
- Attack radius: `1.35`
- Base wave enemy count: `30`
- Extra enemies per wave: `10`
- Spawn batch size: `3`
- Zombie scale: `1.4`
- Base zombie speed: `0.85`
- Zombie speed per wave: `0.04`
- Stress spawn batch size: `25`
- Stress spawn interval: `0.1`
- Stress max alive zombies: `1500`
- Kill reward: Wood `1.0`, Food `0.6`, Stone `0.25`, Iron `0.15`, wave scale `+5%`
- Wave clear bonus: Wood `20 + 6 per wave`, Food `15 + 5 per wave`, Stone `10 + 4 per wave`, Iron `6 + 3 per wave`
- Worker economy: population growth continuous siege cycle basina `15`, initial workers Wood/Stone/Iron/Food `20 / 10 / 8 / 15`
- Worker caps: Wood/Stone/Iron/Food `40 / 30 / 24 / 40`
- Worker production: Wood/Stone/Iron/Food `8 / 5.5 / 3.8 / 7` per minute
- Worker economy reward multiplier: `0.25`
- Economy event chance `15%`, cooldown `2` waves
- Continuous siege cycle: total `60s`, Day `25s`, Dusk `10s`, Night `25s`
- Continuous siege intensity: Day `0.55`, Dusk `1.00 -> 1.35`, Night `1.65`
- Initial/day prep duration fields legacy/debug akis icin korunur
- Day overlay alpha: `0`
- Night overlay alpha: `0.50`
- Unlimited arrows: `true`
- Wave director: base interval `0.8`, wave multiplier `0.96`, min interval `0.35`
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

Mobile mode'da zombileri kale merkezi etrafindaki cemberden random aciyla spawn eder. Spawn yonu tam random 360 kalir; lane, cephe veya telegraph yoktur. Continuous siege aktifken `WaveSpawnSystem`, wave clear kontrolune girmez; `ContinuousSiegeCycleData.SpawnIntensityMultiplier` ile interval'i kisa/uzun, batch'i kucuk/buyuk yapar. Ic tarafta `CurrentWave` cycle index olarak tutulur ve `MobileWaveUtility.ConfigureMobileWave()` zombi HP/speed gibi scaling degerlerini hesaplamaya devam eder, fakat UI wave numarasi gostermez.

Legacy mobile wave director akisi `ContinuousSiegeCycleData.Enabled` false yapilirsa hala calisabilir: opening/mid/final fazlari `ZombiesSpawned / ZombiesToSpawn` oranindan hesaplanir ve wave temizlenince DayPrep'e doner. Varsayilan NewGameScene akisi continuous siege'dir. Stress mode aciksa mobile config'teki stress batch/interval/cap kullanilir, reward verilmez ve continuous/legacy wave director fazlari calismaz.

Wave clear bonus normal mobile modda bir kez verilir. Worker economy aktifken bonus `WorkerEconomyRewardMultiplier` ile azaltilir. Bonus miktari `WaveClearRewardData` uzerine de yazilir; HUD bu veriyi kisa `Wave Cleared +...` feedback'i icin kullanir.

### ResourceTickSystem

Worker economy aktifken `MobilePopulationEconomySystem` `ResourceProductionRate` degerlerini worker allocation'a gore yazar. Continuous siege aktifken her tamamlanan 60 saniyelik cycle, population'a `PopulationGrowthPerDayPrep` kadar yeni nufus ekler. `MobilePrepPauseState.IsPaused` true iken `ResourceTickSystem` resource accumulator ilerletmez. `MobilePopulationAllocation` yoksa eski economy focus multiplier akisi korunur.

### MobilePopulationEconomySystem

Mobile mode'da worker allocation'i resource cap ve population'a gore clamp eder, `PopulationState.Workers/Idle` degerlerini gunceller ve resource production rate'lerini yazar. Continuous siege varsayilaninda her tamamlanan cycle basina population growth uygular. Legacy DayPrep akisi acilirse completed wave sonrasi DayPrep basinda ayni growth degerini kullanir. Event cooldown/sans roll'u legacy DayPrep akisinda burada yapilir; pending event secilmezse gece baslarken expire eder.

### ApplyMovementForceSystem

Mobile mode'da moving zombiler `CastleCenter` noktasina dogru kuvvet alir. `ZombieSlow` enabled ise hareket kuvveti slow multiplier ile carpilir.

### BoundarySystem

Mobile mode'da zombi `AttackRadius` icine girince `Attacking` state'e gecer ve kale etrafindaki attack ring'e sabitlenir. `NewGameScene` setup default'u `1.35` olarak SmallScaleInt kale footprint'iyle hizalanmistir; bu sadece durma/saldirma cemberini etkiler, spawn yonu ve pathfinding davranisini degistirmez.

### ArcherShootSystem

Okcu hedefleme sistemi range icindeki en yakin zombiyi secer. Basic, Rapid ve Frost ayni `Archer.prefab` uzerinden farkli `ArcherUnit` stat'leriyle calisir. `CastleYardPrepState.RallyTimer > 0` ise fire-rate hesabina `RallyFireRateMultiplier` uygulanir.

- Basic: fire rate `1.5`, damage `10`, range `15`
- Rapid: fire rate `3.0`, damage `6`, range `14`
- Frost: fire rate `1.2`, damage `5`, range `14`, slow `2s`, multiplier `0.55`

Okcu ve ok projectile gorunurlugu `SpriteTint` ile okunur:

- Basic: beyaz/notr
- Rapid: sicak sari
- Frost: soguk mavi

`ArcherShootSystem`, instantiate edilen oka okcu tipinden gelen tint'i yazar.
Ayni `Arrow.prefab` ve `ArrowMat` kullanilmaya devam eder.
Mobile config ve `UnlimitedArrows = true` iken `ArrowSupply.Current` kontrolu/decrement yapilmaz; legacy config olmayan sahnelerde ok stogu tuketimi korunur.
Atis aninda `CombatSfxEvent.ArrowShoot` uretilir; shoot particle V1'de kapali tutulur. Playback main scene `CombatFeedbackBridge` tarafindadir.

Atis aninda okcunun `FacingDirection` degeri hedef zombiye gore guncellenir ve
`AttackAnimTimer` baslatilir. `ArcherAnimationStateSystem`, timer aktifken attack
row'unu, timer bitince ayni yonde idle row'unu oynatir.

### ArrowHitSystem + ZombieSlowTimerSystem

Frost ok hedefe vurunca `ZombieSlow` enable edilir veya yenilenir. Slow stack yapmaz; ayni hedefe tekrar vurmak duration'i refresh eder. Timer bitince slow pasiflenir ve multiplier tekrar `1` olur.

`ZombieSlowTimerSystem`, slow aktifken zombi `SpriteTint` degerini soguk/mavi yapar.
Slow biterse veya zombi Dead state'e gecerse tint normal beyaza doner; death akisi
ayri kalir.
Isabet aninda Basic/Rapid icin `CombatVfxEvent.ArrowHit`, Frost icin `CombatVfxEvent.FrostHit` uretilir. Hit VFX hedef pozisyonunda kisa sprite flipbook impact olarak oynar; normal arrow/frost hit artik ParticleSystem prefab'i kullanmaz. Hit SFX event'i de uretilir; ilgili clip bridge uzerinde atanmadiginda sessizce atlanir.

## HUD Readability

`GameManager`, mevcut `ArcherUnit` entity'lerinden Basic/Rapid/Frost sayilarini okur.
Archer count bilgisi sag drawer row'larinda okunur. `ArcherTypeText` eski HUD placeholder'i imported drawer UI ile cakisabildigi icin mobile setup tarafindan kullanilmaz.

Yeni imported HUD varsa cycle paneli player-facing zaman bilgisini gosterir:

- `CyclePhaseText`: `DAY`, `DUSK` veya `NIGHT`
- `CycleDayLabelText`, `CycleDuskLabelText`, `CycleNightLabelText`: segment label'lari
- `CycleProgressFill` ve `CycleProgressMarker`: 60s dongu progress'i
- `HordePressurePanel`: prefabda bulunsa bile player-facing olarak kapali tutulur
- Fallback eski HUD varsa wave text sadece `DAY/DUSK/NIGHT`, kills text ise hedef sayi olmadan `KILLS x` yazar.

HUD varsa `CastleDefensePanel` uzerindeki `DefenseWallFill`, `DefenseGateFill`, `DefenseCoreFill` ve yuzde text'lerini gunceller. Toplam defense yuzdesi wall, gate ve castle current/max HP toplamindan hesaplanir; eski `DefenseText` sadece fallback olarak kalir. Night/high pressure durumunda threat rengi kullanilabilir; savunma hasar aldiginda kisa red flash feedback'i verilir.

Sol ust economy HUD mevcut kaynaklari gosterir: Wood, Stone, Iron, Food, Population, Arrows. Runtime text'ler label tekrar etmez; kutu basligi UI'da, value/rate text'i kod tarafindadir. HUD rate degeri mobile population allocation tarafindan yazilan worker production'dir. NewGameScene mobile default'lari:

- Wood `280`, Stone `120`, Iron `70`, Food `220`, Population `60`, Arrows `INF`
- Initial workers: Wood `20`, Stone `10`, Iron `8`, Food `15`
- Worker caps: Wood `40`, Stone `30`, Iron `24`, Food `40`
- Worker income: Wood `8/min`, Stone `5.5/min`, Iron `3.8/min`, Food `7/min` per assigned worker

## Continuous Recruitment Loop

Mobile castle mode'da XP level-up pause veya kart paneli tetiklemez. Oyun dongusu surekli devam eder: zombi oldur, kaynak biriktir, sol worker drawer'dan ekonomi buyut, sag recruitment drawer'dan okcu satin al.

Legacy level-up kart API'si eski akis icin kodda durabilir, ama `MobileCastleCombatConfig` varken `DamageCleanupSystem` XP threshold'u `IsLevelUpPending` yapmaz.

Mobile normal mode'da `DamageCleanupSystem`, death timer biten zombiler icin kill reward'i `ResourceAccumulator` uzerine yazar. Worker economy aktifken kill reward `WorkerEconomyRewardMultiplier` ile azaltilir; ana gelir kaynagi worker allocation'dir. Continuous siege varsayilaninda wave clear bonus/player-facing clear akisi tetiklenmez, fakat cycle tamamlandikca population growth uygulanir. Stress mode'da reward verilmez.

## Castle Interior Economy

`EconomyFocusUI` mobile loop'ta artik kullanilmaz; setup tool eski focus objelerini gizler. Ekonomi yonu sol worker drawer ve kale ici worker site gorselleriyle belirlenir.

- `CastleInteriorClickTarget` ve `CastleEconomyUI` legacy/debug akisi olarak kalir; player-facing ana worker kontrolu sol drawer'dadir.
- `WorkerEconomyDrawerUI` her zaman acilip kapanabilir; DayPrep sartina bagli degildir.
- Wood/Stone/Iron/Food `+ WORKER` butonlari her basarili tiklamada ilgili worker sayisini +1 yapar; tap progress yoktur.
- Eski worker slider'lari debug/legacy olarak kalabilir ve ayni `MobilePopulationAllocation` verisini degistirir.
- Worker assignment toplam worker sayisini `Population.Total - Population.Archers` ustune cikaramaz.
- `GameManager`, worker allocation degisince `WorkerPrefabData.WorkerPrefab` uzerinden DOTS villager entity'leri spawn/destroy eder.
- Villager worker pickup pozisyonlari main scene `CastleInteriorEconomyArea/*Site/WorkerSpawnPoints` marker'larindan gelir.
- Delivery pozisyonlari `CastleInteriorEconomyArea/CastleWorkerHub/DeliveryPoints` marker'larindan gelir.
- `WorkerLogisticsMovementSystem`, villagerlari pickup ile hub arasinda yuruturek kaynak tasima feedback'i verir.
- `PopulationGrowthPerDayPrep` legacy DayPrep akisi icin isimlendirilmis eski alan olarak kalir; continuous siege varsayilaninda her cycle tamamlandiginda population growth miktari olarak kullanilir.
- Okcu satin almak `1` idle population kullanir; idle yoksa buy disabled olur ve drawer `NEED POP` yazar.
- Editor testleri icin `GameManager.Free Economy Test Mode` acilirsa okcu satin alma population harcamaz ve resource/population eksigi aksiyonlari bloklamaz.
- Nadir eventler `MobileEconomyEventState` ile tutulur; secilmezse gece baslarken expire eder.

## Archer Economy Drawer

`MarketUI`, `MobileCastleHudRoot` altindaki `ArcherDrawerPanel` controller'idir. Drawer combat sirasinda acilip kapanir ve `Time.timeScale` degistirmez. Sag drawer'in player-facing rolu yalnizca archer recruitment'tir; upgrade ve tech unlock aksiyonlari burada gosterilmez.

- Basic baslangicta unlocked.
- Rapid/Frost kilitli baslar; unlock ileride full-screen Tech Tree tarafindan yapilacaktir.
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
- `Arrow Refill`: unlimited arrow mobile akista oyuncuya gosterilmez.
- `Start Next Wave`: debug/public API olarak kalir, player-facing UI'da gizlidir.

## Okcu Yerlesimi

Mobile mode'da okcular `NewGameScene` main scene'indeki `Grid/outside` tilemap'inin dolu hucrelerine yerlestirilir. `inside` ve `outside2` yalnizca kale gorsel katmanidir.

- Spawn tilemap: `outside`
- Spawn Z: `-1`
- Hucre sayisi hard cap degildir; dolu hucreler tekrar kullanilir
- Ayni hucreye donen okcular kucuk deterministic mini-offset alir
- Okcu render'i Entities Graphics uyumlulugu icin `Opaque/Geometry` shader ile calisir. `outside2` Wall/4 ve okcu prefab Wall/3 sirasi korunur, fakat shader/depth kisitlari nedeniyle front-wall occlusion icin sonraki cozum shader'a dokunmadan kurulmalidir.

Bu milestone'da okcu hard cap yoktur. Gelecekte limit nufus/kaynak ekonomisinden gelecek.

## World Visuals

World visual tilemap'leri main scene tarafinda owner kontrolundedir. Setup tool artik world visuals uretmez veya boyamaz.

Wall/gate/core cani hala `CastleAuthoring` bake edilen ECS verisinden gelir; zombi spawn'i tam random 360 kalir. Gorsel katmandan okunan tek runtime veri `outside` tilemap okcu spawn hucreleridir.

## Stress Test

- `StressTestMode = true`: wave/gameover beklemeden surekli zombi spawn eder.
- Stress mode'da zombi hasari uygulanmaz; wall/gate/castle HP dusmez ve game over beklenmez.
- HUD enemy text'i `Zombies: alive (max X)` formatinda o oturumda gorulen max alive sayisini gosterir.

## Bilerek Yapilmayanlar

- Enemy variety yok; tek tip zombi kalir.
- RTS-style manuel okcu kontrolu yok.
- Nufus hard cap bu milestone'da okcu limitine baglanmaz.
- Yeni coin yok; mevcut kaynak sistemi kullanilir.
