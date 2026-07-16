# Hit Feedback Budget - Editor Setup

## Sahne Ayarları

`NewGameScene/CombatFeedbackRoot/CombatFeedbackBridge` üzerinde:

- `Hit Flipbook Pool Size`: `128`
- `Max Hit Vfx Played Per Frame`: `24`
- `Hit Vfx Min Interval`: `0.04`
- `Hit Flipbook Frame Rate`: `90`
- `Hit Flipbook Scale`: `0.35`

`Window -> DeadWalls -> Mobile Castle Scene Setup` aynı değerleri tekrar üretir.

## Doğrulama

1. `CombatHitFeedbackBudgetTests` EditMode paketini çalıştır.
2. `DenseArrowHits_EmitSpatiallySampledVfxAndAggregatedSfx` PlayMode testini çalıştır.
3. `HitFeedbackBridge_EnforcesPlaybackBudgetAndRateLimit` PlayMode testini çalıştır.
4. Game View QA'da yoğun hit burst'ünde aktif flipbook sayısının `24` üstüne
   çıkmadığını ve event query'nin aynı frame temizlendiğini doğrula.
5. Console'da error olmamalı; scene validation temiz olmalı.
