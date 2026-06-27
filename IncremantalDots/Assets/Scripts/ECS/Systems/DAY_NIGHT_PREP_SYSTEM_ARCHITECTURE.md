# DayNightPrepSystem - Mimari

## Amac

`DayNightPrepSystem`, mobile castle normal modda manuel `Start Next Wave` akisini otomatik gunduz/gece dongusune cevirir.

## Calisma Kosullari

- `GameStateData`, `WaveStateData` ve `MobileCastleCombatConfig` singleton'lari gerekir.
- Sadece `WaveStateData.Phase == DayPrep` ve `WaveActive == false` iken calisir.
- `IsGameOver`, `IsLevelUpPending` veya `StressTestMode` aktifse is yapmaz.

## Davranis

1. Baked scene ilk frame'de `WaveActive = true`, `NightCombat`, `CurrentWave = 1` ve baslangic `WaveStartTimer > 0` ile gelirse spawn'dan once `CurrentWave = 0` day prep baslangicina normalize eder.
2. `PrepTimer` her frame `DeltaTime` kadar azalir.
3. Timer sifira inince `MobileWaveUtility.StartNightWave()` cagrilir.
4. `CurrentWave` artar, mobile wave stat'leri configure edilir.
5. `Phase = NightCombat`, `WaveActive = true`, `PrepTimer = 0` olur.

`WaveSpawnSystem` wave temizlenince tekrar `DayPrep` fazini ve `DayPrepDuration` sayacini yazar.

## Bilerek Yapilmayanlar

- Gercek isik veya skybox degistirmez; gorsel kararma `DayNightOverlayController` tarafindadir.
- Stress mode'u etkilemez.
- UI butonlarini yonetmez; drawer/HUD davranisi MonoBehaviour katmanindadir.
