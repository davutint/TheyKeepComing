# Mobile Castle Combat v2 - Editor Setup

## Tuning owner kuralı

- Difficulty alanlarını doğrudan shadow Inspector değerlerinden değiştirme. Aktif profile atanmışsa `Window > DeadWalls > Difficulty Tuner` veya `DefaultDifficulty.asset` kullanılır.
- Enemy prefabı ile base HP/damage/speed/scale için `Assets/ScriptableObject/MobileCastle/Enemies/BasicZombie.asset` düzenlenir; Profile/Inspector shadow alanları owner değildir.
- Geometri, mode, cycle süreleri ve ekonomi baseline alanları aktif SubScene'deki `MobileCastleCombatAuthoring` üzerinden düzenlenir.
- `MobileCastleCombatConfig` runtime çıktısıdır; Play Mode Inspector/debug değişikliği kalıcı tuning değildir.
- Setup penceresi yalnız açıkça çalıştırılan initializer/repair aracıdır; günlük tuning owner'ı değildir.
- Ayrıntılı alan listesi: `Assets/Scripts/ECS/Authoring/MOBILE_CASTLE_TUNING_ARCHITECTURE.md`.

## Kurulum

1. Unity Editor'de projeyi ac.
2. `Window -> DeadWalls -> Mobile Castle Scene Setup` penceresini ac.
3. `Setup NewGameScene` butonuna bas.
4. Tool `NewGameScene` ana sahnesini ve `MobileCastleCombatSubScene.unity` SubScene'ini gunceller.

Script eklendikten sonra disaridan manuel compile komutu calistirma. Unity refresh sonrasi derlemeyi kendisi yapar.

## Beklenen SubScene Hierarchy

- `GameState`
  - `GameStateAuthoring`
  - `WaveConfigAuthoring`
- `CastleCore`
  - `CastleAuthoring`
- `MobileCastleConfig`
  - `MobileCastleCombatAuthoring`
- `BasicArcher_01`
  - `ArcherAuthoring`

## Inspector Kontrolleri

- `MobileCastleCombatAuthoring`
  - Enemy Catalog: `Assets/ScriptableObject/MobileCastle/Enemies/EnemyCatalog.asset`
  - Castle Center: `(0, 0)`
  - Spawn Radius: `11`
  - Attack Radius: `1.35`
  - Base Wave Enemy Count: `30`
  - Extra Enemies Per Wave: `10`
  - Spawn Batch Size: `2` (Difficulty Profile owner)
  - Zombie Scale: `1.4`
  - Base Zombie Speed: `0.85`
  - Zombie Speed Per Wave: `0.04`
  - Stress Spawn Batch Size: `25`
  - Stress Spawn Interval: `0.1`
  - Stress Max Alive Zombies: `1500`
  - Kill Reward Wood/Food/Stone/Iron: `1 / 0.6 / 0.25 / 0.15`
  - Kill Reward Wave Scale: `0`
  - Wave Clear Bonus Wood/Food/Stone/Iron Base: `20 / 15 / 10 / 6`
  - Wave Clear Bonus Wood/Food/Stone/Iron Per Wave: `6 / 5 / 4 / 3`
  - Initial Day Prep Duration: `12`
  - Day Prep Duration: `15`
  - Day/Night Overlay Alpha: `0 / 0.50`
  - Unlimited Arrows: enabled
  - Wave Director Base Spawn Interval: `0.95` (Difficulty Profile owner)
  - Spawn Interval Wave Multiplier: `0.96`
  - Min Spawn Interval: `0.35`
  - Opening/Final Enemy Ratio: `0.20 / 0.20`
  - Opening/Final Interval Multiplier: `1.35 / 0.65`
  - Opening/Final Batch Delta: `-1 / +1`
  - Fortify Damage Multiplier: `0.70`
  - Rally Duration: `10`
  - Rally Fire Rate Multiplier: `1.25`
  - Archer Slots: mobile tilemap spawn akisi tarafindan kullanilmaz; bos kalabilir.
- Main scene `Grid`
  - `MobileCastleArcherTilePlacement` component'i `outside` tilemapini ve `ArcherFormationV1.asset` tanimini spawn kaynagi olarak kullanir.
  - Scene view'da Gizmos acikken 40 tile x 25, toplam 1000 seeded formation noktasi gorunur.
- `GameStateAuthoring`
  - Initial Zombies To Spawn: `30`
  - Spawn Interval: `0.8`
  - Base Zombie Speed: `0.85`
  - Initial Wood/Stone/Iron/Food: `280 / 120 / 70 / 220`
  - Initial Population: `60`
  - Initial Workers/Archers: `53 / 4`
  - Initial Arrows: `200`
- `WaveConfigAuthoring`
  - Enemy Catalog, Arrow ve Archer prefab referanslari dolu olmali.
  - Enemy Catalog V1'de yalniz `zombie_basic` / `Zombie.prefab` kaydini icermeli.
  - Zombie Prefab alani yalniz catalog'u olmayan legacy sahneler icin fallback'tir.
- `BasicArcher_01`
  - Type: `Basic`
  - Fire Rate: `1.5`
  - Arrow Damage: `10`
  - Range: `15`
  - Tint: beyaz
  - Local Scale: `1`
- `MobileCastleHudRoot`
  - `HUDController`: economy text'leri, `WaveText`, `KillsText`, `WaveRewardText`, `DamageFlashImage` ve varsa defense module alanlari bagli olmali.
  - Defense module: `DefensePercentText`, `DefenseWallFill`, `DefenseWallText`. Legacy Gate/Core referanslari serialize uyumlulugu icin kalabilir ama runtime'da gizlenir.
  - `MarketUI`: `ArcherDrawerPanel`, `DrawerToggleButton`, Basic/Rapid/Frost row buy alanlari ve dynamic template `ArcherRetrainButton` bagli olmali.
  - `MarketUI`: `Basic/Rapid/FrostUpgradeButton`, `ArrowTechPanel`, `RapidTechUnlockButton`, `FrostTechUnlockButton` prefabda varsa player-facing olarak gizlenmelidir.
  - Castle Yard: `RepairButton`; polish prefabda varsa `FortifyButton`, `RallyButton` ve cost/status text'leri.
- `Canvas/DayNightOverlay`
  - Full-screen black `Image`, raycast target kapali.
  - `DayNightOverlayController.OverlayImage` ayni image'a bagli.
  - Canvas'in ilk child'i olmali; world'u karartir, HUD/drawer ustte kalir.

## Play Kontrolu

`NewGameScene` Play modunda:

- Basic okcu `Grid/outside` tilemapindeki bir spawn hucresinden ates eder.
- Zombiler kalenin etrafindaki farkli acilardan gelir.
- Zombi oldukce mobile resource reward artar; mobile loop'ta XP level-up pause tetiklemez.
- Zombi oldukce Wood/Food/Stone/Iron reward'i accumulator uzerinden artar.
- `MaxAliveZombies` doluyken spawn talebi kaybolmaz; `ContinuousSpawnBudgetData.PendingEnemies` artar ve kapasite açılınca `MaxSpawnBatch` sınırıyla azalır.
- V1 runtime config'te Moat gameplay kapalı, slow `1`, damage `0` olmalıdır. `moat_dig`, `moat_flame` ve `start_moat` aktif tech/meta catalog'larında görünmemelidir; görsel hendek ground detail olarak kalabilir.
- Runtime `EnemyCatalogEntryData` buffer'i tek kayit olmali; spawn edilen zombi prefab/stat/scale degerleri `BasicZombie.asset` ile eslesmelidir.
- XP threshold oyunu durdurmaz; level-up paneli acilmaz.
- Basic/Rapid/Frost okcular farkli tint ile okunur.
- Basic/Rapid/Frost oklari okcu tipinin tint'ini miras alir.
- Frost isabet eden zombi slow suresince mavi/soguk gorunur, sonra normale doner.
- Okcular yalnizca `Grid/outside` tilemapinde Formation V1 ile sabitlenen 40 hucrenin 25'er seeded diamond noktasina yerlestirilir; eski tile-center tekrar/stack davranisi kullanilmaz.
- Drawer toggle ile sag `Archer Recruitment` paneli acilip kapanir ve oyun pause olmaz.
- Basic buy kaynak dusurup yeni Basic okcu spawn eder.
- Rapid/Frost locked baslar; sag panelde unlock/upgrade butonu gosterilmez, unlock ileride Tech Tree tarafindan yapilacaktir.
- Rapid/Frost unlock olduktan sonra `RETRAIN` bir Basic'i yerinde donusturur; toplam archer/population ayni kalir.
- Prefab retrain binding'i kaybolursa `Window > DeadWalls > Repair Archer Retrain Control` calistirilir.
- Kaynak yetmiyorsa ilgili row `CostText` alaninda `NEED ...` gorunur.
- Sag panelde upgrade butonu gorunmez; panel sadece okcu satin alma icindir.
- Frost oklar hedef zombiyi yavaslatir.
- `Repair` tek Wall HP'sini onarir; Wall 0/Game Over sonrasi diriltme yapmaz.
- Oyun kisa `DAY 01` hazirligi ile baslar, sayac bitince `NIGHT 01` otomatik baslar.
- Wave basi daha sakin, wave sonu daha baskili akar.
- Spawn yonleri tam random 360 kalir.
- Wave bitince 15 sn day prep baslar; `Start Next Wave` gerekmez ve button player-facing UI'da gizlidir.
- Day prep boyunca overlay alpha `0 -> 0.50` artar, night combat'ta `0.50` sabit kalir.
- Ok sayisi HUD'da `INF` gorunur ve mobile modda atis ok stogu dusurmez.
- `Repair`, `Fortify` ve `Rally` sadece day prep sirasinda aktif olur.
- HUD'da tek Wall bari gorunur; wave clear bonusu kisa `Wave Cleared +...` feedback'i verir.
- Wave son `20%` bolumunde wave/kills text'i threat rengine gecer; savunma hasarinda kisa red flash gorunur.
- Wave bitince clear bonus tek sefer eklenir.

## Stress Test

1. SubScene'de `GameState` objesini sec.
2. `GameStateAuthoring.StressTestMode` degerini `true` yap.
3. `MobileCastleConfig` uzerindeki stress degerleriyle batch, interval ve alive cap'i ayarla.
4. Play modunda HUD'daki `Zombies: alive (max X)` degerini takip et.

Stress mode'da zombi hasari uygulanmaz; castle HP dusmeden max gorunen zombi sayisi olculebilir.
Stress mode'da kill reward ve wave clear bonus verilmez.

Stress test bitince `StressTestMode` tekrar `false` yapilmali.

## Otomatik 10K Runtime Gate

Release scene/asset cap degerlerini degistirmeden 10K senaryosunu calistirmak icin Unity Test Runner'da:

`DeadWalls.Tests.HordeScalePlayModeTests.HordeScale_10K_WithHudFeedbackPoolFireballAndContinue_ProducesTelemetry`

Test runtime-only cap override kullanir. Son olcum ve blocker raporu `Assets/Docs/DEAD_WALLS_10K_RUNTIME_REPORT.md` dosyasindadir.
