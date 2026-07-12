# Horde Scale PlayMode Test - Mimari

`HordeScalePlayModeTests`, release tuning assetlerini değiştirmeden gerçek `NewGameScene` içinde 10.000 enemy correctness ve ölçüm kapısını çalıştırır.

Test runtime-only cap override kullanır; pool rent/expand, HUD/feedback varlığı, ProfilerRecorder frame/GC/render sayaçları, 10K snapshot, Fireball toplu return ve Continue restore aynı senaryodadır. Performans değerleri assert threshold değildir; donanım/Editor bağımlı telemetry olarak loglanır. Contract correctness değerleri ise assert edilir.

Otoriter ölçüm çıktısı: `Assets/Docs/DEAD_WALLS_10K_RUNTIME_REPORT.md`.
