# Combined Load Player Profiler - Editor Setup

Ek scene, prefab veya Inspector kurulumu gerekmez. Aktif build target `StandaloneWindows64` olmalı ve `NewGameScene` build settings içinde bulunmalıdır.

Unity Editor menüsünden:

`Tools > DeadWalls > Run Combined 10K + 1K Player Profile`

komutunu çalıştır. Araç exact explicit testi seçer, Development Player'ı test assembly'leriyle build eder, önce instrumentation kapalı target-hardware frame-pacing penceresini çalıştırır, ardından ayrı raw capture'ı otomatik analiz eder.

İlk Standalone build shader ve asset importları nedeniyle birkaç dakika sürebilir. Build devam ederken Unity MCP/Editor ana thread'i geçici olarak yanıt vermeyebilir; build penceresi ilerliyorsa süreci kesme. Test callback'i tamamlandığında sahne `NewGameScene`'e döner ve durum dosyası `passed` veya `failed` olarak kapanır.

Başarılı koşuda:

- Console: `[DW-V1-PLAYER-PROFILE] status=passed`;
- frame pacing: `%USERPROFILE%/AppData/LocalLow/DefaultCompany/IncremantalDots/DeadWallsProfilerCaptures/DW_V1_TARGET_HARDWARE_FRAME_PACING_*.json`;
- capture: `%USERPROFILE%/AppData/LocalLow/DefaultCompany/IncremantalDots/DeadWallsProfilerCaptures/DW_V1_PLAYER_COMBINED_*.raw`;
- durum: `Logs/DW_V1_PLAYER_COMBINED_STATUS.json`;
- özet: `Logs/DW_V1_PLAYER_COMBINED_SUMMARY.json`;
- tam rapor: `Logs/DW_V1_PLAYER_COMBINED_REPORT.txt`.

Player testi geçtiği halde otomatik analiz yarıda kalırsa aynı raw için:

`Tools > DeadWalls > Analyze Latest Combined Player Profile`

komutu yalnız analiz aşamasını tekrarlar. Raw dosyayı Profiler penceresinde yeniden export etmeye gerek yoktur.

Player build test başlamadan hata verirse `IErrorCallbacks` sonucu aynı durum dosyasına yazılır. Önce `error` alanındaki gerçek build girdisini düzelt; eski raw dosyayı yeni koşu kanıtı olarak kullanma.

Kabulde frame-pacing JSON `enemyCount=10000`, `archerCount=1000`, `projectilePositiveSamples>0` ve `accepted=true` taşımalıdır. Canonical eşikler average/P95 için `16,67 ms`, P99 için `33,33 ms`'dir. Tracker'a DoD kanıtı olarak instrumentation kapalı 600-frame JSON yazılır; ayrı 120-frame raw özetindeki GC/system owner değerleri yalnız teşhis kanıtıdır.
