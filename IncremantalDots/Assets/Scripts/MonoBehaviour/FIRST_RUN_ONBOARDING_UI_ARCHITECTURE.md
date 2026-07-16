# First Run Onboarding UI - Mimari

## Sorumluluk

`FirstRunOnboardingUI`, Package I ilk-kosu ogretiminin scene-owned sunum sahibidir. Gameplay
transaction'i, otomatik drawer acma, resource harcama veya worker dagitma yapmaz. Gercek UI
owner'larinin player-action event'lerini dinler; yalniz non-modal hint ve pulse gosterir.

## Ilk Day Worker Ratio Adimi

- Stable meta flag: `tutorial.v1.worker_ratio`.
- Gorunme kapisi: mobile worker economy aktif, oyun bitmemis, continuous cycle Day 1 / Day.
- Drawer kapaliyken `WorkerDrawerToggleButton`, acikken `WoodWorkerTargetPlus10Button` pulse olur.
- Tek player-facing metin English'tir: `ADJUST A WORKER TARGET RATIO.`
- Herhangi bir resource target ratio islemi `GameManager` tarafinda basarili oldugunda
  `WorkerEconomyDrawerUI.WorkerTargetRatioChangedByPlayer` event'i yayilir.
- Event prompt gorunmeden once gelse bile adim tamamlanir. Durable flag yazimi basarisizsa
  hint kapanmaz ve state fail-closed kalir.

## Sunum Siniri

`OnboardingHintPanel` ve `OnboardingPulseFrame`, aktif generated HUD prefabinin responsive
gorsel root'u altindadir. Raycast kapali oldugu icin combat ve management input'unu engellemez.
Pulse `Time.unscaledTime` kullanir; simulation pause state'ini degistirmez.

Sonraki Basic Archer, ammo, Heart, Council, repair ve ability onboarding adimlari ayni
scene-owned controller ve stable meta flag siniri uzerinden eklenir; bu adim final tutorial
complete flag'i yerine gecmez.
