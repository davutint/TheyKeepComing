# Dead Walls V1 - Release Verification Report

## Ölçüm kimliği

- Tarih: `2026-07-18`
- Unity: `6000.3.10f1`
- Aktif scene: `Assets/Scenes/NewGameScene.unity`
- Run save şeması: desteklenen `v3-v16`, canonical `v16`
- Meta save şeması: desteklenen `v1-v3`, canonical `v3`
- Release active enemy cap: `900`

Bu rapor V1 Definition of Done içindeki full regression, save migration ve explicit long-run
soak kapılarını tek audit kaydında birleştirir. Target-hardware 10K + 1K pacing kanıtı
`DEAD_WALLS_10K_RUNTIME_REPORT.md` içindedir; burada aynı sonucu yeniden uydurmak yerine referans
olarak korunur.

## Sonuç özeti

| Kapı | Sonuç | Kanıt |
|---|---:|---|
| Full EditMode | Geçti | `404/404` |
| Full PlayMode | Geçti | `88 pass + 2 expected explicit skip` |
| Save migration grubu | Geçti | `56/56` |
| Explicit long-run soak | Geçti | `1/1`, `3.600` sample frame |
| 10K + 1K target hardware | Geçti | Average/P95/P99 `7,665/13,890/14,058 ms` |
| Scene validation | Geçti | `0 issue` |
| Final Console | Geçti | `0 error / 0 warning` |

## Save migration matrisi

`RunPersistence.UpgradeToCurrent` her desteklenen legacy sürümü bir sonraki açık sözleşmeye
taşır. Test matrisi yalnız son sürüm round-trip'ini değil, her şema sınırını ayrı doğrular.

| Kaynak | Canonical sonuç | Guard |
|---|---|---|
| v3 | Worker target ratio, idle mirror ve observation state | Mevcut population/worker sayısı korunur |
| v4 | Gerçek bed base + purchased bed `0` | Legacy `999999` sentinel taşınmaz |
| v5 | Dört worker binası CAP/EFF `L0` | Olmayan yatırım uydurulmaz |
| v6 | Archer formation version `1` | Archer type sayıları korunur |
| v7 | Arrow capacity/efficiency `L0` | Mevcut Arrow stoku korunur; olmayan upgrade uydurulmaz |
| v8 | Grave Essence `0` | Meta'dan run currency icat edilmez |
| v9 | Heart graph açıkça absent | Production catalog'dan sessiz reroll yapılmaz |
| v10 | Regular Council handled-day state | Chance failure scheduled günü tüketmez; kanıtlı kart korunur |
| v11 | Rally/Emergency cooldown hazır | Mevcut Fireball cooldown korunur |
| v12 | Essence meta remainder `0` | Kesirli geçmiş değer uydurulmaz |
| v13 | Legacy exact combat fallback | Aggregate payload tahminle üretilmez |
| v14 | Telemetry peak/timeline temiz başlangıç | Historical telemetry uydurulmaz |
| v15 | Fireball evolution runtime listeleri boş | Legacy save'de olmayan pending strike, delayed blast veya burning ground state'i uydurulmaz |
| v16 | Canonical payload | Bozuk combat rebuild ve sırasız Wall timeline fail-closed; aktif evolution timer ve kalan tick sayısı exact korunur |

Ek durable state kanıtı:

- Meta v1/v2 save'leri canonical v3'e currency, upgrade, receipt, pool unlock ve tutorial
  state'ini uydurmadan taşır; future-version ve corrupt JSON overwrite edilemez.
- Death receipt run identity/reward snapshot'ını round-trip eder; matching pending death run
  snapshot'ını geçersiz kılar ve aynı RunId Meta reward'ını yalnız bir kez uygular.
- Targeted `RunPersistenceTests + MetaProgressionSchemaTests + CouncilRegularScheduleTests`
  birleşik sonucu `56/56`'tir.

## Fireball evolution kapanışı

Production Heart catalog v2, Scorched Earth ve Echoing Detonation behavior node'larıyla `37`
canonical definition taşır. İlk evolution beş aggregate burning-ground tick'ini, ikincisi tek delayed
aggregate blast'i üretir; iki yol da per-enemy telemetry/VFX veya yeni enemy entity'si oluşturmaz.

Targeted EditMode `51/51`, exact aggregate hasar/VFX lifecycle ve gerçek GameManager save/Continue
PlayMode `2/2` geçti. İlk PlayMode turu beşinci Burning Ground tick'inin float expiry sınırında
atlanabildiğini yakaladı; runtime yalnız süreye bakmak yerine canonical `RemainingTicks` sayısını
koruyacak şekilde düzeltildi. Ardından full EditMode `400/400` ve clean full PlayMode
`88 pass + 2 expected explicit skip` geçti.

## Exact launch tuning ve telemetry target kapanışı

Production `DefaultDifficulty.asset`, üç `ArcherDefinitionSO` ve `MetaUpgradeCatalog.asset`
değerleri spekülatif rebalance yapılmadan exact launch authority olarak kilitlendi. Tek okunabilir
değer/ölçüm tablosu `DEAD_WALLS_V1_LAUNCH_TUNING_AND_TELEMETRY_TARGETS.md` içindedir.

Yeni `V1LaunchTelemetryTargets.asset`, Spawn/Economy/Combat/Council/Meta için `19` inclusive band,
cohort, minimum sample ve canonical source event tanımı taşır. Validation `19/19` tanımı kabul eder;
contract fingerprint'i
`58fc60a01a2442fdeaf544f59560159c21ca0e5dff48c0e54f27d817f8059dd3` olarak kilitlidir.
Bu profil harici analytics provider seçmez ve automatic retuning yapmaz.

`run_ended v2`, mevcut final summary'ye upgrade-applied Wall MaxHP, Wood/Stone/Iron/Food, Arrow current/capacity,
population/capacity/idle, Basic/Rapid/Frost count ve unspent Grave Essence snapshot'ını ekler.
Target contract EditMode `4/4`, telemetry EditMode `28/28`, gerçek `NewGameScene` telemetry
PlayMode `9/9`, full EditMode `404/404` ve full PlayMode `88 pass + 2 expected explicit skip`
geçti. Scene validation `0 issue`; final Console `0 error / 0 warning` kaldı.

## Long-run soak kapanış koşusu

Explicit test release `MaxAliveZombies = 900` değerini yükseltmeden `10.000` pending demand seed
etti. Production `WaveSpawnSystem`, enemy pool ve Arrow pool 360 warmup + 3.600 steady-state frame
boyunca çalıştı; ardından demand dondurulup backlog yalnız `MaxSpawnBatch = 16` ile eritildi.

| Metrik | Sonuç |
|---|---:|
| Targeted test | `1/1 passed` |
| Active cap / observed max | `900 / 900` |
| Cap fill | `57 frame` |
| Warmup / soak | `360 / 3.600 frame` |
| Backlog before / after soak | `9.244 / 9.989` |
| Drain frame / max per frame | `625 / 16` |
| Demanded / spawned / pending | `10.889 / 10.889 / 0` |
| Final active enemy | `777` |
| Enemy pool created / expansion | `1.024 / 7` |
| Enemy rent / return | `10.889 / 10.112` |
| Arrow pool created / expansion | `1.024 / 0` |
| Arrow rent / return | `45.000 / 45.000` |
| Frame average / P95 / max | `6,227 / 8,227 / 34,610 ms` |
| Main thread average / max | `6,215 / 34,367 ms` |
| Editor root GC average / max | `29.759 / 398.875 B` |
| Used memory start / end | `4.273.004.896 / 4.341.456.264 B` |

`TotalDemanded - TotalSpawned = Pending`, pool `available + active = totalCreated`, cap saturation
ve batch-bounded drain invariant'ları test boyunca korundu. Editor root GC ve used-memory sayaçları
runtime allocation/leak sertifikası değildir; bounded pool residency ve exact rent/return
muhasebesi release kabul sahibidir.

## Test kararlılığı

İlk full PlayMode closure turunda `Exact2KHorde_FireballGapRefillsUnderQueuedPressure`, ağır suite
frame'i sabit `4,5s` realtime penceresini çok az simulation update'iyle tükettiği için strike
sonucunu erken okuyup `killed=0` verdi. Test tek başına geçti; fixture daha sonra realtime deadline
yanında minimum queue/refill frame sayısını da bekleyecek şekilde dar kapsamlı stabilize edildi.
Gameplay, Fireball, queue veya horde davranışı değiştirilmedi.

Stabilizasyon sonrası targeted test `1/1`, güncel clean full PlayMode tekrar `88 pass + 2 expected
explicit skip` geçti. Bu nedenle ilk dalgalanma gizlenmedi; kökü ve test-only düzeltmesi bu raporda durable
kanıt olarak tutuldu.

## Release kararı

Full regression, bütün desteklenen save migration sınırları, regular-Council legacy geçişi,
durable Meta/death state'i, target-hardware combined load ve release-cap long-run soak temizdir.
V1 DoD test-report kapısı kapatılabilir. Bu karar enemy cap'i artırmaz ve yeni content kararı
vermez; açık owner-content maddeleri tracker'da ayrı kalır.
