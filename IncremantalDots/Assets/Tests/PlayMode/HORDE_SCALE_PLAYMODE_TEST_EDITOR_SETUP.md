# Horde Scale PlayMode Test - Editor Setup

Ek scene veya Inspector kurulumu gerekmez.

Unity Test Runner'da şu testi hedefli çalıştır:

`DeadWalls.Tests.HordeScalePlayModeTests.HordeScale_10K_WithHudFeedbackPoolFireballAndContinue_ProducesTelemetry`

Test başlamadan `NewGameScene` build settings içinde olmalı ve Unity Console temizlenmelidir. Test, mevcut `run_save.json` dosyasını geçici olarak korur ve bitince geri yükler. 1.000 Basic Archer üretim restore/spawn owner'ı üzerinden canonical 40 x 25 formasyona yerleştirilir; save alındıktan sonra okçular tamamen silinir ve iki ayrı Continue turunda yeniden kurulmaları doğrulanır.

Başarılı koşuda Test Runner output'undaki `[DW-B-SCALE]` satırı en az `enemy=10000`, `archer=1000`, `saved_archers=1000`, `rebuild_deterministic=True`, `archer_rebuild_deterministic=True` ve `backlog=777` taşımalıdır. `save_ms`, `save_bytes`, `restore_ms` ve `second_restore_ms` aynı satırdan rapora aktarılır.

Editor ölçümünü standalone build sonucu olarak kullanma. Karşılaştırma yaparken aynı Unity sürümü, aynı makine ve aynı test senaryosunu kullan.
