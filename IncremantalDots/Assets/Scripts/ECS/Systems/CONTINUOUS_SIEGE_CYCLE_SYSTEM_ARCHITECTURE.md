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
