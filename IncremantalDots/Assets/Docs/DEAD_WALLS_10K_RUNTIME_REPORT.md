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

`DW-B-SCALE-OPT` içindeki death spike ve kaçınılabilir steady-state allocation blockerları çözüldü. Açık kalan işler:

1. `532` draw call için GPU/Frame Debugger ile Entities instancing ve batch kanıtı.
2. Save/restore maliyetinin standalone hedef PC build bütçesine göre değerlendirilmesi.
3. Package D kapsamında 1.000 archer + 10.000 enemy + projectile peak senaryosu.

Doğrulama: targeted profiler capture `1/1`, iki targeted 10K benchmark `2/2`, full EditMode `34/34`, full PlayMode `13 passed + 1 explicit skipped`.
