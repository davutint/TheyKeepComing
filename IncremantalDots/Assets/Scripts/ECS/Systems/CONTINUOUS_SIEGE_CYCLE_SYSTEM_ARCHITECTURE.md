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

Sistem `SpawnIntensityMultiplier` ve `HordePressure01` uretir. `WaveSpawnSystem` continuous siege aktifken eski wave clear kontrolunu calistirmaz. Günlük count/batch/interval tabanı ile phase multiplier ayrı `ContinuousSpawnBudgetData` alanlarında tutulur; ayrıntılı sözleşme `CONTINUOUS_SPAWN_BUDGET_ARCHITECTURE.md` dosyasındadır.

Varsayilan yogunluklar:

- Day: `0.55`
- Dusk: `1.00 -> 1.35`
- Night: `1.65`

## Eski Akisle Iliski

`DayNightPrepSystem`, `ContinuousSiegeCycleData.Enabled` true oldugunda erken cikar. Boylece eski day prep sistemi dosyada kalir ama mobile continuous siege varsayilaninda oyunu durdurmaz.

## v5.1: DAWN Fazi + Kutle Eskalasyonu (2026-07-06)

Dongu 4 FAZ: DAY 30s -> DUSK 5s -> NIGHT 20s -> DAWN 5s (60s toplam;
sureler `SiegeCycleDuration`'a oranla olceklenir). Dawn intensity 0.15 —
odul/nefes fazi. `SiegeDawnDuration=0` bake'lerde legacy 3-faz davranis korunur.

- Population growth (+`PopulationGrowthPerDayPrep`) artik DAWN BASINDA verilir
  (`MobilePopulationEconomySystem.ApplyContinuousCycleGrowth`; isaret degeri
  `LastPopulationGrowthCycle = CycleIndex + 1`, ilk gunun Dawn'i dahil).
  `DawnRewardToastUI` bu ani toast ile gorunur kilar.
- Kutle eskalasyonu (MobileWaveUtility + ContinuousSpawnBudgetUtility):
  HP/damage/speed cycle ile buyumez; demand batch day curve, cycle growth ve phase
  intensity ile hesaplanıp `MaxSpawnBatch` ile sınırlanır. `MaxAliveZombies` doluyken
  talep atlanmaz; explicit backlog'a eklenir ve kapasite açılınca kontrollü boşalır.
- `KillRewardWaveScale=0` default: kill odulu cycle ile buyumez (gelir/zorluk ayrisik).
- DayNightOverlayController Dawn'da night->day alpha lerp'i yapar;
  HUDController `DAWN` yazar ve `CycleDayCounterText` ("DAY n" = CycleIndex+1) gunceller.

## V1: SpecialNights dormant

V1 Blueprint'te special night, Blood Moon veya boss gecesi yoktur. `SpecialNightEntry`,
`BloodMoonIntensityMult` ve `IsBloodMoonNight` yalniz save/config geriye uyumlulugu ve gelecekteki
content icin dormant veri olarak kalir; aktif davranis veya presentation uretmez.

- DefaultDifficulty `SpecialNights` listesi bostur; setup tool seed eklemez.
- `MobileCastleTuningResolver` her sample icin `BloodMoonIntensityMult = 1` yazar.
- `ContinuousSiegeCycleSystem` stale buffer degeri olsa bile multiplier'i okumaz ve
  `IsBloodMoonNight = false` yazar.
- Save/Continue legacy bayragi diske geri yazmaz veya runtime'a restore etmez.
- `BloodMoonWarningUI`, warning scene root'u, HUD label/color, overlay tint, vignette flash ve
  audio loop/sting dallari V1 runtime ve setup akisindan tamamen kaldirilmistir.
