# Combined Load Player Profiler - Mimari

`CombinedLoadPlayerProfilerRunner`, mevcut explicit `HordeScaleProfilerCapturePlayModeTests` testini Unity Test Framework üzerinden `StandaloneWindows64` Development Player'da çalıştıran Editor-only orchestration sahibidir. Gameplay runtime'ına, release tuning asset'lerine veya production save'e yeni owner eklemez.

Ölçüm zinciri:

1. Player-targeted test gerçek `NewGameScene` ve production pool/catalog owner'larıyla 10.000 enemy kurar.
2. 1.000 Basic Archer, doğrudan stress entity'si yerine production `GameManager.RestoreArcherCountsWithinCapacity` yoluyla canonical 40 x 25 formasyona yerleştirilir.
3. Finite Arrow state'i yalnız test koşusunda 10.200 kapasiteye çıkarılır; 1K archer hedefleme, projectile pool, hareket, collision ve return pipeline'ı capture boyunca aktiftir.
4. Development Player, allocation callstack açık şekilde 30 warmup + 120 steady frame'i `DW_V1_PLAYER_COMBINED_*.raw` dosyasına yazar.
5. Mevcut `ProfilerDataAnalyzer`, raw dosyayı otomatik yükler; frame average/P95/max, root GC/frame, user allocator, ECS system average/max/GC ve max-spike system özetini üretir.

Raw capture, `Application.persistentDataPath/DeadWallsProfilerCaptures` altında kalır. İnsan-okur raporu `Logs/DW_V1_PLAYER_COMBINED_REPORT.txt`, makine-okur özeti `Logs/DW_V1_PLAYER_COMBINED_SUMMARY.json`, orchestration sonucu `Logs/DW_V1_PLAYER_COMBINED_STATUS.json` dosyasına yazılır. `Logs` ve persistent capture çıktıları repoya alınmaz.

Runner, normal `ICallbacks` yanında Test Framework `IErrorCallbacks` kanalını da sahiplenir. Player build test başlamadan hata verirse durum dosyası `running` olarak asılı kalmaz; run ID, başlangıç/bitiş zamanı ve build failure mesajı `failed` sonucu olarak korunur.

`ProfilerDataAnalyzer` hem legacy `Assembly-CSharp.dll!` hem aktif `DeadWalls.dll!`/`DeadWalls.` marker biçimlerini user code kabul eder. Bilinen ECS system/job isimleri marker'ın tamamında aranır; böylece Development Player'ın namespace ve `OnUpdate()` suffix'i system eşleşmesini bozmaz.

Bu ölçüm yalnız aynı makine ve aynı Unity sürümünde regresyon karşılaştırmasıdır. Editor ölçümü yerine gerçek Standalone Player raw'ı kullanır; buna rağmen düşük donanım/release build QA yerine geçmez.
