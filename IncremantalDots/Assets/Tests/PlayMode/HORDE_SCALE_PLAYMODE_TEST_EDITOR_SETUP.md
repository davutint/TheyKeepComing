# Horde Scale PlayMode Test - Editor Setup

Ek scene veya Inspector kurulumu gerekmez.

Unity Test Runner'da şu testi hedefli çalıştır:

`DeadWalls.Tests.HordeScalePlayModeTests.HordeScale_10K_WithHudFeedbackPoolFireballAndContinue_ProducesTelemetry`

Test başlamadan `NewGameScene` build settings içinde olmalı ve Unity Console temizlenmelidir. Test, mevcut `run_save.json` dosyasını geçici olarak korur ve bitince geri yükler. Sonuç satırı `[DW-B-SCALE]` prefix'iyle Test Runner output'una yazılır.

Editor ölçümünü standalone build sonucu olarak kullanma. Karşılaştırma yaparken aynı Unity sürümü, aynı makine ve aynı test senaryosunu kullan.
