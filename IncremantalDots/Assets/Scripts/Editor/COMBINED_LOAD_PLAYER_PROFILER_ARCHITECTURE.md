# Combined Load Player Profiler - Mimari

`CombinedLoadPlayerProfilerRunner`, mevcut explicit `HordeScaleProfilerCapturePlayModeTests` testini Unity Test Framework üzerinden `StandaloneWindows64` Development Player'da çalıştıran Editor-only orchestration sahibidir. Gameplay runtime'ına, release tuning asset'lerine veya production save'e yeni owner eklemez.

Ölçüm zinciri:

1. Player-targeted test gerçek `NewGameScene` ve production pool/catalog owner'larıyla 10.000 enemy kurar.
2. 1.000 Basic Archer, doğrudan stress entity'si yerine production `GameManager.RestoreArcherCountsWithinCapacity` yoluyla canonical 40 x 25 formasyona yerleştirilir.
3. Arrow state'i yalnız fixture içinde efektif sınırsız kapasiteye çıkarılır; böylece 1K archer ölçümü normal run ammo depletion'ı tarafından erken kesilmez. Projectile örnekleri combat hattının gerçekten çalıştığını kanıtlar.
4. Binary Profiler ve allocation callstack kapalıyken 180 warmup + 600 sample frame ölçülür. `Time.unscaledDeltaTime` örneklerinden average, P95, P99, maximum, bütçe-aşım sayısı ve en uzun ardışık aşım hesaplanır; hardware/quality/resolution ve exact entity sayılarıyla `DW_V1_TARGET_HARDWARE_FRAME_PACING_*.json` yazılır.
5. Kabul kapısı `average <= 16,67 ms`, `P95 <= 16,67 ms`, `P99 <= 33,33 ms`, exact `10.000 enemy / 1.000 archer` ve ölçüm penceresinde gerçek projectile üretimidir.
6. Bu kabul penceresinden sonra Development Player, allocation callstack açık şekilde ayrı 120 frame'i `DW_V1_PLAYER_COMBINED_*.raw` dosyasına yazar.
7. Mevcut `ProfilerDataAnalyzer`, raw dosyayı otomatik yükler; frame average/P95/max, root GC/frame, user allocator, ECS system average/max/GC ve max-spike system özetini üretir. Instrumented raw, kabul penceresinin yerine geçmez; owner analizi içindir.

Frame-pacing JSON ile raw capture, `Application.persistentDataPath/DeadWallsProfilerCaptures` altında kalır. İnsan-okur raw raporu `Logs/DW_V1_PLAYER_COMBINED_REPORT.txt`, makine-okur raw özeti `Logs/DW_V1_PLAYER_COMBINED_SUMMARY.json`, orchestration sonucu ve iki capture yolu `Logs/DW_V1_PLAYER_COMBINED_STATUS.json` dosyasına yazılır. `Logs` ve persistent capture çıktıları repoya alınmaz.

Runner, normal `ICallbacks` yanında Test Framework `IErrorCallbacks` kanalını da sahiplenir. Player build test başlamadan hata verirse durum dosyası `running` olarak asılı kalmaz; run ID, başlangıç/bitiş zamanı ve build failure mesajı `failed` sonucu olarak korunur.

`ProfilerDataAnalyzer` hem legacy `Assembly-CSharp.dll!` hem aktif `DeadWalls.dll!`/`DeadWalls.` marker biçimlerini user code kabul eder. Bilinen ECS system/job isimleri marker'ın tamamında aranır; böylece Development Player'ın namespace ve `OnUpdate()` suffix'i system eşleşmesini bozmaz.

Frame-pacing penceresi gerçek StandaloneWindows64 Player'da ve profiler instrumentation kapalıyken hedef makinenin 60 FPS DoD kapısını ölçer. Development Player test assembly'si taşıdığı için geniş düşük-donanım/release-build matrisi yerine geçmez. Instrumented raw ise yalnız aynı makine ve aynı Unity sürümünde owner/regresyon karşılaştırmasıdır.
