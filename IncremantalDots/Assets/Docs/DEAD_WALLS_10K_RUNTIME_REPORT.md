# Dead Walls - 10K Horde Runtime Report

## Ölçüm kimliği

- Tarih: `2026-07-12`
- Unity: `6000.3.10f1`
- Ortam: Windows Unity Editor PlayMode
- Scene: `Assets/Scenes/NewGameScene.unity`
- Test: `HordeScalePlayModeTests.HordeScale_10K_WithHudFeedbackPoolFireballAndContinue_ProducesTelemetry`
- Release tuning değişikliği: Yok; normal `MaxAliveZombies = 900` korundu.

Bu rapor aynı makinede regresyon karşılaştırması içindir. Editor memory ve frame değerleri standalone PC build veya hedef donanım sonucu olarak yorumlanmamalıdır.

## Senaryo

- Gerçek NewGameScene, HUD, day/night, feedback bridge ve ECS sistemleri açık.
- Test runtime'ında active cap `10.000`.
- Tek `zombie_basic` catalog entry'si pool üzerinden 10.000 kez rent edildi.
- 30 frame warmup ve 120 frame steady-state örnekleme yapıldı.
- Spawn backlog `777` olarak snapshot'a yazıldı.
- 10.000 aktif zombi exact run snapshot'ına kaydedildi.
- Tek Fireball strike bütün zombilere aynı frame lethal damage verdi.
- Ölüm animasyonu sonrası 10.000 entity pool'a döndü.
- Aynı snapshot Continue yoluyla tekrar 10.000 aktif zombi olarak restore edildi.

## Sonuçlar

### Hedefli temiz koşu

| Metrik | Ölçüm |
|---|---:|
| Aktif enemy | 10.000 |
| Pool total / available | 10.112 / 112 |
| Runtime expansion | 78 x 128 |
| 10K activation | 96,69 ms |
| Frame average | 9,00 ms |
| Frame P95 | 10,22 ms |
| Frame maximum | 12,62 ms |
| Main thread average | 8,91 ms |
| Main thread maximum | 12,83 ms |
| GC allocation average | 20.817 byte/frame |
| GC allocation maximum | 74.363 byte/frame |
| Draw call average | 532 |
| Editor total used memory counter | 4.451.437.340 byte |
| Snapshot save | 75,37 ms |
| Snapshot size | 7.365.908 byte |
| Fireball death-to-return | 213 frame |
| Death/return peak | 126,42 ms |
| 10K Continue restore | 146,58 ms |
| Restore edilen backlog | 777 |

### Tam PlayMode seti içindeki doğrulama koşusu

| Metrik | Ölçüm |
|---|---:|
| Frame average / P95 / max | 10,08 / 11,30 / 12,40 ms |
| Main thread average / max | 9,98 / 12,20 ms |
| GC average / max | 20.658 / 57.139 byte/frame |
| Draw call average | 532 |
| Save / restore | 90,46 / 131,16 ms |
| Snapshot size | 7.365.662 byte |
| Death/return peak | 131,95 ms |

İki koşuda da correctness sonucu aynıdır. Steady-state P95 `10,22-11,30 ms`, GC yaklaşık `20,7 KB/frame`, death/return peak `126,42-131,95 ms` aralığında tekrarlandı.

## Karar

10K correctness kapısı geçti:

- Pool 10.000 aktif entity'yi taşıdı ve kapasite invariant'ı korundu.
- Steady-state örneklemede aktif enemy kaybolmadı.
- Fireball toplu ölümü entity destroy/yeniden instantiate üretmeden 10.000 return yaptı.
- Continue aynı pool kapasitesiyle 10.000 aktif zombiyi ve backlog'u geri yükledi.
- HUD ve feedback bridge gerçek scene içinde aktifti.

Package B performans kapısı henüz kapanmadı. Ölçülmüş blockerlar:

1. `126,42-131,95 ms` death/return peak görünür tek-frame hitch'tir. İlk owner adayı `DamageCleanupSystem` içindeki 10.000 adet tekil main-thread return çağrısıdır.
2. Steady-state yaklaşık `20,7 KB/frame` GC allocation kaynağı marker/source audit ile bulunmalıdır.
3. `7,37 MB` snapshot, `75,37 ms` save ve `146,58 ms` restore değerleri hedef PC/build bütçesi belirlenerek tekrar değerlendirilmelidir.
4. `532` draw call ve Editor memory sayısı GPU/build profiling olmadan tek başına kabul veya red kanıtı değildir.

## Sonraki iş

`DW-B-SCALE-OPT`:

- Death return'ü bulk işlemle ve tek buffer/state yazımıyla optimize et.
- Steady-state GC owner'larını profiler marker/source audit ile çıkar.
- Aynı benchmark'ı değişiklik öncesi/sonrası karşılaştırmalı çalıştır.
- Release cap'i yalnız ölçüm ve owner onayı sonrası değiştir.

1.000 archer + 10.000 enemy kombinasyonu bu raporun kapsamında değildir; Package D hedefidir.

---

## DW-B-SCALE-OPT takip ölçümü - 2026-07-13

Bu bölüm yukarıdaki ilk baseline kararının death spike ve allocation maddelerini günceller. Release `MaxAliveZombies = 900` değeri değiştirilmedi.

### Uygulanan optimizasyonlar

- `DamageCleanupSystem`: entity başına main-thread return yerine Burst-parallel transient reset ve tek `CommitBulkReturn` buffer/state yazımı.
- `ZombieDeathSystem`: aynı frame'deki toplu ölüm için 10.000 geçici SFX event entity'si yerine tek temsilci event.
- `ZombieAnimationStateSystem`: death timer geçişinde entity başına ECB komutları yerine doğrudan enableable component yazımı.
- `GameManager` ve `HUDController`: değişmeyen ECS/UI verileri için entity, sayım ve label cache'leri.
- `MarketUI` ve `WorkerEconomyDrawerUI`: sabit aralıkla koşulsuz string/hiyerarşi üretimi yerine allocation-free state fingerprint ve yalnız değişiklikte refresh.
- PlayMode içinde domain reload sonrasında başlayan, yüklenebilir binary RAW üreten explicit 10K profiler capture testi.

### İki temiz 10K doğrulama koşusu

| Metrik | Koşu 1 | Koşu 2 |
|---|---:|---:|
| Frame average | 8,88 ms | 9,01 ms |
| Frame P95 | 10,55 ms | 11,16 ms |
| Main thread average | 8,79 ms | 8,90 ms |
| Editor GC average | 17.943 B/frame | 17.868 B/frame |
| Death peak | 83,72 ms | 79,13 ms |
| Death peak frame / active | 0 / 10.000 | 0 / 10.000 |
| Save | 71,62 ms | 69,52 ms |
| Restore | 120,09 ms | 108,70 ms |
| Snapshot | 7.364.785 B | 7.364.908 B |

Steady P95 baseline aralığında kaldı. Death peak `126,42-131,95 ms` baseline'ından `79,13-83,72 ms` aralığına indi; karşılaştırma uçlarına göre yaklaşık `%34-40` iyileşme sağlandı.

### Allocation owner sonucu

İlk yüklenebilir 120-frame RAW audit'inde proje kodu yaklaşık `2.394 B/frame` üretiyordu; bunun `2.320 B/frame` kadarı tek başına `MarketUI.Update()` çağrı yolundaydı. Son capture sonucu:

- Proje kodu toplamı: `1.405 B / 121 frame = 11,6 B/frame`.
- `MarketUI.Update()`: `0 B`.
- `WorkerEconomyDrawerUI.Update()`: `0 B`.
- Başlangıç capture'ına göre proje steady allocation azalması: yaklaşık `%99,5`.

Benchmark'taki yaklaşık `17,9 KB/frame` toplamı Unity Editor, GameView, MCP transport ve üçüncü taraf Editor menü callback'lerini de içerir; proje runtime allocation bütçesi olarak yorumlanmaz.

### Güncel karar

`DW-B-SCALE-OPT` içindeki death spike ve kaçınılabilir steady-state allocation blockerları çözüldü. Standalone Player ölçümü save/restore ve render sayacını ürün ortamında doğruladı. Açık kalan ürün stresi:

1. Package D kapsamında 1.000 archer + 10.000 enemy + projectile peak senaryosu.
2. Bu birleşik stres geçmeden release `MaxAliveZombies = 900` değerini yükseltmeme.

Doğrulama: targeted profiler capture `1/1`, targeted 10K benchmarklar geçti, full EditMode `35/35`, full PlayMode `13 passed + 1 explicit skipped` ve StandaloneWindows64 Player-targeted test `1/1`.

### Compact save ve Standalone Player ölçümü - 2026-07-13

Exact snapshot şeması değiştirilmeden `JsonUtility` çıktısı pretty JSON yerine compact JSON olarak yazıldı. İki temiz Editor koşusunda:

| Metrik | Koşu 1 | Koşu 2 |
|---|---:|---:|
| Save | 54,00 ms | 52,84 ms |
| Snapshot | 4.243.628 B | 4.243.491 B |
| Restore | 115,25 ms | 106,65 ms |

Önceki optimize koşularındaki `7.364.785-7.364.908 B` dosyaya göre snapshot yaklaşık `%42,4` küçüldü; `69,52-71,62 ms` save aralığına göre yazım yaklaşık `%24-26` hızlandı. Restore maliyeti şema aynı kaldığı için esasen aynı bantta kaldı.

StandaloneWindows64 Player-targeted 10K sonucu:

| Metrik | Player ölçümü |
|---|---:|
| Frame average / P95 | 7,12 / 6,97 ms |
| Main thread average / max | 6,98 / 11,17 ms |
| Draw call average | 535 |
| Save / snapshot | 52,58 ms / 4.240.003 B |
| Restore | 86,19 ms |
| Death peak | 55,57 ms |
| Used memory counter | 880.067.876 B |

Save yalnız ana menü/uygulama çıkışında, restore ise Continue yükleme yolunda çalıştığı için Player'daki iki işlem de aktif combat frame bütçesinin dışındadır. Bu makinede ikisinin de `100 ms` altında kalması V1 enemy-only kabulü için yeterlidir; düşük donanım matrisi release QA'da ayrıca doğrulanmalıdır.

### Frame Debugger takip ölçümü - 2026-07-13

Editor `Draw Calls Count` sayacı SceneView ve Editor UI render'larını da içerir. Ancak Standalone Player'da da ortalama `535` ölçüldüğü için bu sayı yalnız Editor gürültüsü değildir ve ürünün gerçek düşük seviye draw komutları yüksektir.

10.000 aktif enemy sahadayken durdurulan Main Camera Frame Debugger sonucu:

| Event grubu | Adet |
|---|---:|
| Entities Graphics `HybridBatch` | 1 |
| Normal `SRPBatch` | 3 |
| Tilemap `DynamicGeometry` | 7 |
| Final blit | 1 |
| Canvas/UI overlay | 8 |
| Sparse uploader/root compute | 1 |
| **Toplam** | **21** |

Bu `21` değer üst seviye Frame Debugger event sayısıdır; `HybridBatch` kendi içindeki draw komutlarını toplar ve tek draw call anlamına gelmez. ECS topology ölçümünde 10.000 aktif zombie `202` render/simulation chunk'ına dağıldı; örnek chunk `50/50` dolu ve archetype kapasitesi `50` entity'dir. En büyük per-entity alanlar `LocalToWorld 64 B`, `LocalTransform 32 B`, `SpriteAnimation 28 B`, `ZombieStats 28 B`, `PhysicsBody 24 B`, `RenderBounds 24 B` ve `WorldRenderBounds 24 B` olarak ölçüldü.

Sonuç: `535` draw call ileri render-archetype slimming/batching için gerçek optimizasyon borcudur. Buna rağmen aynı standalone koşuda P95 `6,97 ms` ile 60 FPS bütçesinin (`16,67 ms`) belirgin biçimde altında kaldığı için enemy-only V1 blocker'ı değildir. Shader/material körlemesine değiştirilmedi; release cap birleşik 1K archer + 10K enemy testi görülmeden artırılmayacaktır.

---

## DW-I-POLISH-HORDE-READ takip ölçümü - 2026-07-16

Tek `Vampire` materyali ve `DeadWalls/SpriteSheet` shader'ı, ek renderer/entity/material
instance veya ikinci pass üretmeden genişletildi. Shader `Opaque / Geometry`, tek pass ve DOTS
instancing sözleşmesini korur; Unity shader audit sonucu `0` mesajdır. Zombie pool rent'i entity
index + generation hash'iyle authored `15/15` frame ve `16/16` timer bandını kullanır. Authored
FPS, gameplay movement, attack cooldown ve release `MaxAliveZombies = 900` değişmedi.

İki final gerçek-scene Editor PlayMode koşusu:

| Metrik | Koşu 1 | Koşu 2 |
|---|---:|---:|
| Enemy / archer | 10.000 / 1.000 | 10.000 / 1.000 |
| Aktif projectile | 4 | 56 |
| Frame average / P95 / max | 9,21 / 10,35 / 22,09 ms | 9,80 / 10,71 / 66,70 ms |
| Main thread average / max | 9,11 / 21,93 ms | 9,24 / 37,43 ms |
| Draw call average | 546 | 546 |
| Death peak | 58,98 ms | 58,20 ms |
| Save / restore | 42,68 / 93,34 ms | 40,88 / 121,01 ms |
| Compact snapshot | 163.633 B | 177.749 B |

10K Night görsel QA `1920x1080` Game View'da, stress presentation suppression kapalıyken
tamamlandı. Küçük contact patch zemine temas verdi; muted-cold tek-texel edge komşu
silhouette'leri ayırdı; deterministic phase dağılımı senkron frame-0 titreşimini kaldırdı.
Hedefli doğrulama: `HordeReadabilityTests 3/3`, enemy pool testi `1/1`, gerçek 10K + 1K
benchmark `1/1`, scene validation `0` issue ve Console `0` error.

---

## DW-V1-PERF-CONTINUE-10K-1K takip ölçümü - 2026-07-17

Önceki birleşik benchmark, 1.000 okçuyu doğrudan ECS stress entity'si olarak kurduğu için
`run_save.json` içindeki archer type count ve canonical formation replay'ini kanıtlamıyordu. Bu
koşuda 1.000 Basic Archer üretim `GameManager.RestoreArcherCountsWithinCapacity` owner'ı ile
kuruldu; population/allocation/bed state'i aynı exact run state'ine eşitlendi. Snapshot alındıktan
sonra bütün okçular silindi ve aynı v14 snapshot iki kez Continue edildi.

| Metrik | Canonical Continue koşusu |
|---|---:|
| Enemy / canonical archer | 10.000 / 1.000 Basic |
| Saved archer type count | 1.000 / 0 Rapid / 0 Frost |
| Archer formation version | 1 |
| Combat rebuild bucket | 378 |
| Snapshot save / size | 47,05 ms / 369.097 B |
| İlk / ikinci Continue restore | 417,11 / 436,89 ms |
| Enemy / archer rebuild deterministic | True / True |
| Restore edilen backlog | 777 |
| Frame average / P95 / max | 30,49 / 52,06 / 66,23 ms |
| Main thread average / max | 30,36 / 76,10 ms |
| Sample sonu aktif projectile | 879 |
| Arrow pool total / active / rents | 1.280 / 879 / 8.000 |
| 10K activation / Fireball death peak | 152,37 / 65,83 ms |

Canonical Continue correctness kapısı geçti: save payload'ı gerçekten 1.000 Basic Archer ve
formation version `1` taşıdı; her restore öncesinde sahnede archer sayısı `0` iken her iki Continue
turunda da 1.000 archer yeniden kuruldu. Enemy position multiset'i ile archer type/formation
fingerprint'i iki restore arasında birebir aynı kaldı; 10.000 enemy ve backlog `777` de korundu.

Bu koşudaki snapshot, ölçüm anında `879` aktif projectile içerdiği için önceki düşük-projectile
örneklerinden daha büyüktür. `417,11-436,89 ms` restore değerleri Unity Editor içinde 10K enemy
pool aktivasyonu ile canonical 1K archer spawn/formasyon kurulumunun birlikte maliyetidir; Continue
yükleme ekranında çalışır ve aktif combat frame bütçesi değildir. Bu ölçüm Player allocation/spike
kabulü sayılmaz; birleşik yükte isolated Player Profiler kanıtı ayrı tracker maddesinde açıktır.

---

## DW-V1-PERF-PLAYER-ALLOC-SPIKE takip ölçümü - 2026-07-17

Birleşik yük, Test Framework'ün `StandaloneWindows64` Development Player'ında allocation
callstack açık şekilde yeniden kuruldu. Test doğrudan stress archer entity'si üretmedi; production
`GameManager.RestoreArcherCountsWithinCapacity` owner'ı ile 1.000 Basic Archer, production enemy
pool ile 10.000 aktif enemy ve gerçek finite Arrow/projectile pipeline'ı kullanıldı. Capture 30
warmup frame sonrasında 120 frame topladı; raw profil `73.765.057 B` olarak yazıldı ve analyzer
tarafından 121 okunabilir frame'e dönüştürüldü.

| Metrik | Standalone Player ölçümü |
|---|---:|
| Test sonucu | 1 passed / 0 failed / 0 skipped |
| Enemy / canonical archer | 10.000 / 1.000 |
| Capture sonu aktif projectile | 6 |
| Frame average / P95 / max | 10,593 / 18,694 / 33,234 ms |
| Root GC average / max | 10.437 / 12.020 B/frame |
| Proje kodu GC toplam / ortalama | 58.290 B / 481 B/frame |
| ECS system GC / sync-point | 0 B / yok |
| En ağır ortalama sistem | DamageApplySystem - 1,892 ms ortalama, 4,863 ms max |
| En yüksek tek sistem sample'ı | ArcherShootSystem - 17,444 ms max |
| En yavaş whole-frame sahibi | Frame 2 - 33,234 ms; en büyük named ECS katkısı DamageApplySystem 2,219 ms |

Proje kodu allocation owner'ları `GameManager.Update` 22.848 B, `SpellCastUI.Update` 22.372 B,
`ArrowSupplyUI.Update` 10.080 B, `MarketUI.Update` 1.344 B, `HUDController.Update` 940 B,
`HeartScreenUI.Update` 456 B ve `AmbientAudioController.Update` 250 B olarak ayrıldı. Toplam root
allocation'ın büyük bölümü Player render/test altyapısında; proje kodu payı capture boyunca yaklaşık
`482 B/frame` kaldı. Named ECS sistemlerinin hiçbirinde GC veya sync-point marker'ı bulunmadı.

Bu Development Player koşusu release FPS sertifikası değildir; test assembly, profiler ve allocation
callstack instrumentation'ı taşır. Buna rağmen Editor gürültüsünden ayrılmış birleşik owner kanıtıdır:
average 60 FPS bütçesinin altında, P95 ise `16,67 ms` bütçesinin `2,024 ms` üzerindedir. Bu nedenle
release cap artırılmadı; aktif cap/backlog saturation ve süreklilik kararı sıradaki long-run soak
ölçümünde verilecektir.

---

## DW-V1-PERF-LONG-RUN-SOAK takip ölçümü - 2026-07-17

Release `MaxAliveZombies = 900` değeri test için yükseltilmedi. Ölçüm, bekleme süresini yapay
olarak uzatmadan doygunluğu deterministik kurmak için `ContinuousSpawnBudgetData` içine exact
telemetry ile birlikte `10.000` pending demand seed etti; bu backlog'u yalnız üretim
`WaveSpawnSystem` ve `EnemyPoolRuntimeUtility` tüketti. Cap dolduktan sonra 1.000 canonical Basic
Archer, yüksek test ArrowSupply stoku ve gerçek projectile rent/return pipeline'ı devreye girdi.
Warmup sonrasındaki steady-state pencere `3.600` frame'dir; bu, 60 FPS karşılığında bir dakikalık
oyun frame'i örneğidir. Ardından yeni demand `SpawnTimer` üzerinden donduruldu, active kapasite
128'lik kontrollü pool return turlarıyla açıldı ve mevcut backlog tamamen eritildi.

| Metrik | Koşu 1 | Koşu 2 |
|---|---:|---:|
| Targeted PlayMode | 1/1 passed | 1/1 passed |
| Release active cap / gözlenen max | 900 / 900 | 900 / 900 |
| Cap dolum frame'i | 57 | 57 |
| Warmup / steady-state | 360 / 3.600 frame | 360 / 3.600 frame |
| Soak başlangıç / bitiş backlog | 9.228 / 9.989 | 9.228 / 10.002 |
| Drain frame / max drain | 625 / 16 | 626 / 16 |
| Final demanded / spawned / pending | 10.889 / 10.889 / 0 | 10.902 / 10.902 / 0 |
| Final active enemy | 777 | 790 |
| Enemy pool created / expansion | 1.024 / 7 | 1.024 / 7 |
| Enemy pool rent / return | 10.889 / 10.112 | 10.902 / 10.112 |
| Arrow pool created / expansion | 1.024 / 0 | 1.024 / 0 |
| Arrow pool rent / return | 42.000 / 42.000 | 41.000 / 41.000 |
| Frame average / P95 / max | 5,992 / 7,732 / 37,620 ms | 5,833 / 7,400 / 30,497 ms |
| Main thread average / max | 5,981 / 37,580 ms | 5,821 / 30,508 ms |
| Editor root GC average / max | 28.460 / 305.947 B | 29.079 / 305.025 B |
| Total Used Memory başlangıç / bitiş | 4.001.181.282 / 4.063.759.106 B | 4.030.732.442 / 4.094.979.214 B |

İki koşuda da `TotalDemanded - TotalSpawned = Pending` eşitliği her frame korundu. Cap doluyken
enemy active/rent sayısı değişmedi ve backlog yeni continuous demand ile monoton büyüdü. Demand
dondurulduğunda drain hiçbir frame release `MaxSpawnBatch = 16` sınırını aşmadı; pending sıfıra
indiğinde demanded ile spawned tam eşitlendi. Enemy ve Arrow pool için
`available + active = totalCreated` bütün örneklerde korundu; warmup sonrası yeni entity veya pool
expansion oluşmadı. Bu nedenle saturation davranışı bounded ve backlog kayıpsızdır; release cap
`900` olarak kalır.

GC ve Total Used Memory satırları Unity Editor + Test Runner root sayaçlarıdır; proje koduna ait
allocation/leak sertifikası olarak yorumlanmaz. Bir önceki isolated Standalone Player capture'ı proje
kodu allocation'ını `481 B/frame`, named ECS system GC'sini `0 B` ölçmüştü. Bu soak'ın bellek kabulü
managed sayaçtan ziyade sabit enemy/projectile pool residency'si ve exact rent/return muhasebesidir.

---

## DW-V1-DOD-10K-1K-FRAME-PACING kabul ölçümü - 2026-07-18

Hedef donanım kapısı `StandaloneWindows64` Development Player'da, binary Profiler ve allocation
callstack kapalı ayrı bir pencereyle ölçüldü. Test assembly'si yalnız exact fixture/orchestration
içindir; örnekleme sırasında Profiler instrumentation'ı yoktur. Production `NewGameScene`, enemy
pool, canonical `GameManager.RestoreArcherCountsWithinCapacity` yoluyla 1.000 Basic Archer,
targeting/collision/projectile sistemleri ve Ultra quality render hattı birlikte açıktır. Fixture
Arrow stoku normal run kapasitesi tarafından kesilmez; release economy/tuning asset'i değişmez.

### Hedef makine ve senaryo

| Alan | Değer |
|---|---:|
| CPU / GPU | Intel Core i5-14400F / Intel Arc B580 12 GB |
| Sistem belleği | 32.581 MB |
| Player / çözünürlük | WindowsPlayer / 1920 x 1080 |
| Quality / VSync / target FPS | Ultra / 1 / -1 |
| Warmup / sample | 180 / 600 frame |
| Enemy / canonical Basic Archer | 10.000 / 1.000 |
| Projectile peak | 670 |
| Projectile-positive örnek | 4 / 21 |

### Instrumentation kapalı frame-pacing sonucu

| Metrik | Sonuç | Kabul bütçesi |
|---|---:|---:|
| Frame average | 7,665 ms | <= 16,667 ms |
| Frame P95 | 13,890 ms | <= 16,667 ms |
| Frame P99 | 14,058 ms | <= 33,333 ms |
| Frame maximum | 27,722 ms | Bilgi |
| 16,667 ms üstü | 4 / 600 | Bilgi |
| 33,333 ms üstü | 0 / 600 | 0 |
| En uzun 16,667 ms üstü seri | 1 frame | Bilgi |
| Exact entity + combat kanıtı | Geçti | 10K / 1K / projectile |
| **Kabul** | **Geçti** | Bütün kapılar true |

600-frame pencerede average ve P95 60 FPS bütçesinin altında, P99 ise 30 FPS pacing floor'unun
altında kaldı. 16,667 ms üstündeki dört frame ardışık birikmedi; 33,333 ms üstü frame yoktur.
Bu nedenle tracker'daki `1k archer + 10k enemy target hardware frame pacing` V1 DoD kapısı kabul
edildi. Bu karar release `MaxAliveZombies = 900` değerini yükseltmez; long-run saturation kararı ve
backlog policy'si aynen korunur.

### Ayrı instrumented raw owner kanıtı

Aynı geçen test, kabul penceresinden sonra allocation callstack açık 120-frame raw üretti
(`74.879.570 B`). Analyzer sonucu average/P95/max `9,108 / 12,132 / 44,013 ms`'dir. ECS named
system'lerde GC veya sync-point yoktur; PhysicsCollisionSystem `0,352 ms`, DamageApplySystem
`1,190 ms`, BuildSpatialHashSystem `0,033 ms` ortalamadadır. Raw'daki profiler metadata,
callstack/file-write ve test/render allocation'ları target-hardware pacing sonucu değildir.

Doğrulama: Player-targeted explicit test `1/1 passed`; frame JSON `accepted=true`; raw `120/120`
frame analiz edildi; orchestration status `passed` ve iki capture yolunu birlikte kaydetti.

---

## DW-V1-DOD-RELEASE-TEST-REPORTS soak kapanış koşusu - 2026-07-18

V1 release doğrulamasında explicit long-run soak güncel kod üzerinde yeniden çalıştırıldı. Release
`MaxAliveZombies = 900`, `MaxSpawnBatch = 16`, production enemy/Arrow pool ve backlog policy
değiştirilmedi.

| Metrik | Kapanış koşusu |
|---|---:|
| Targeted PlayMode | `1/1 passed` |
| Release cap / observed max | `900 / 900` |
| Warmup / soak | `360 / 3.600 frame` |
| Backlog before / after soak | `9.244 / 9.989` |
| Drain frame / max drain | `625 / 16` |
| Demanded / spawned / pending | `10.889 / 10.889 / 0` |
| Enemy pool created / expansion | `1.024 / 7` |
| Enemy rent / return | `10.889 / 10.112` |
| Arrow pool created / expansion | `1.024 / 0` |
| Arrow rent / return | `45.000 / 45.000` |
| Frame average / P95 / max | `6,227 / 8,227 / 34,610 ms` |
| Main thread average / max | `6,215 / 34,367 ms` |
| Editor root GC average / max | `29.759 / 398.875 B` |
| Used memory start / end | `4.273.004.896 / 4.341.456.264 B` |

Pending sıfıra indiğinde demanded ve spawned exact eşitlendi; enemy/Arrow pool muhasebesi ve cap
saturation invariant'ları korundu. Fresh sonuç önceki iki koşunun P95 `7,400-7,732 ms` bandına
yakın `8,227 ms` verdi ve 60 FPS bütçesinin belirgin biçimde altında kaldı. Editor GC/memory root
sayaçları yine allocation/leak sertifikası sayılmaz; bounded pool residency kabul sahibidir.
