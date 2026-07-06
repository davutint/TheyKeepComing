# ContinuousSiegeCycleSystem - Mimari

`ContinuousSiegeCycleSystem`, mobile castle modda player-facing wave clear / start next wave akisini devre disi birakir ve oyunu 60 saniyelik kesintisiz kusatma dongusune tasir.

## Akis

- Sistem sadece `MobileCastleCombatConfig` ve `ContinuousSiegeCycleData` varsa calisir.
- Stress mode, game over ve level-up pending durumlarinda update yapmaz.
- Varsayilan cycle:
  - Day: 25 saniye
  - Dusk: 10 saniye
  - Night: 25 saniye
- `CyclePhaseText` icin faz sadece `DAY`, `DUSK`, `NIGHT` olarak tutulur; wave numarasi UI'a yazilmaz.
- `WaveStateData` uyumluluk icin aktif combat halinde tutulur:
  - `WaveActive = true`
  - `Phase = NightCombat`
  - `PrepTimer = 0`

## Spawn Pressure

Sistem `SpawnIntensityMultiplier` ve `HordePressure01` uretir. `WaveSpawnSystem` continuous siege aktifken eski wave clear kontrolunu calistirmaz; bu multiplier'a gore spawn interval ve batch size ayarlar.

Varsayilan yogunluklar:

- Day: `0.55`
- Dusk: `1.00 -> 1.35`
- Night: `1.65`

## Eski Akisle Iliski

`DayNightPrepSystem`, `ContinuousSiegeCycleData.Enabled` true oldugunda erken cikar. Boylece eski day prep sistemi dosyada kalir ama mobile continuous siege varsayilaninda oyunu durdurmaz.

## v5.1: DAWN Fazi + Kutle Eskalasyonu (2026-07-06)

Dongu artik 4 FAZ: DAY 22s -> DUSK 8s -> NIGHT 22s -> DAWN 8s (60s toplam;
sureler `SiegeCycleDuration`'a oranla olceklenir). Dawn intensity 0.15 —
odul/nefes fazi. `SiegeDawnDuration=0` bake'lerde legacy 3-faz davranis korunur.

- Population growth (+`PopulationGrowthPerDayPrep`) artik DAWN BASINDA verilir
  (`MobilePopulationEconomySystem.ApplyContinuousCycleGrowth`; isaret degeri
  `LastPopulationGrowthCycle = CycleIndex + 1`, ilk gunun Dawn'i dahil).
  `DawnRewardToastUI` bu ani toast ile gorunur kilar.
- Kutle eskalasyonu (MobileWaveUtility + WaveSpawnSystem.HandleContinuousSiegeSpawn):
  HP LINEER `ZombieBaseHP*(1+(w-1)*ZombieHpGrowthPerCycle)` (eski ustel 20*w^1.2
  KALDIRILDI — sunger degil kalabalik); batch = `SpawnBatchSize * intensity *
  (1+(w-1)*SpawnBatchGrowthPerCycle)` cap `MaxSpawnBatch`; `MaxAliveZombies`
  performans tavani (spawn atlanir). Tabanlar config'e tasindi (hardcoded 20/5 yok).
- `KillRewardWaveScale=0` default: kill odulu cycle ile buyumez (gelir/zorluk ayrisik).
- DayNightOverlayController Dawn'da night->day alpha lerp'i yapar;
  HUDController `DAWN` yazar ve `CycleDayCounterText` ("DAY n" = CycleIndex+1) gunceller.
