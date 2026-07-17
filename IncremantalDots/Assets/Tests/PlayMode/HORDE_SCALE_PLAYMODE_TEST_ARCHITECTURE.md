# Horde Scale PlayMode Test - Mimari

`HordeScalePlayModeTests`, release tuning assetlerini değiştirmeden gerçek `NewGameScene` içinde 10.000 enemy + canonical 1.000 archer correctness ve ölçüm kapısını çalıştırır.

Test runtime-only cap override kullanır; pool rent/expand, HUD/feedback varlığı, ProfilerRecorder frame/GC/render sayaçları, 10K snapshot, Fireball toplu return ve Continue restore aynı senaryodadır. 1.000 Basic Archer doğrudan stress entity'si olarak üretilmez: üretim restore owner'ı `GameManager.RestoreArcherCountsWithinCapacity` kullanılarak canonical formasyon ve type-count cache'i kurulur; `PopulationState`, `MobilePopulationAllocation` ve `MobileBedCapacityState` aynı exact run state'ine eşitlenir.

Save kabulü `BasicArchers = 1000`, diğer iki type count `0`, güncel `ArcherFormationVersion`, 10.000 aggregate combat rebuild ve backlog `777` değerlerini assert eder. Test bütün okçuları kaldırdıktan sonra aynı snapshot'ı iki kez Continue eder; her iki turda 10.000 enemy ile 1.000 canonical archer yeniden kurulmalı, enemy position multiset'i ve archer type/formation fingerprint'i birebir aynı kalmalıdır.

Performans değerleri assert threshold değildir; donanım/Editor bağımlı telemetry olarak `[DW-B-SCALE]` satırına yazılır. `saved_archers`, `rebuild_deterministic`, `archer_rebuild_deterministic`, `save_ms`, `save_bytes`, `restore_ms` ve `second_restore_ms` canonical Continue kanıtının parçasıdır. Contract correctness değerleri assert edilir; test runtime gameplay davranışını değiştirmez.

Otoriter ölçüm çıktısı: `Assets/Docs/DEAD_WALLS_10K_RUNTIME_REPORT.md`.
