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
