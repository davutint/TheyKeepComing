# Dead Walls V1 Launch Tuning & Telemetry Targets

## Authority

Bu doküman V1 launch için exact spawn, economy, combat ve meta tuning değerlerinin tek okunabilir
kabul kaydıdır. Runtime sahipleri aşağıdaki production asset ve kod sahipleridir; doküman bunları
özetler, ikinci bir runtime tuning katmanı kurmaz.

| Alan | Production sahibi |
|---|---|
| Spawn, Wall, population, worker, Arrow | `DefaultDifficulty.asset` |
| Basic/Rapid/Frost | `ArcherDefinitionSO` production asset'leri |
| Meta reward ve 11 permanent upgrade | `MetaUpgradeCatalog.asset` |
| Ölçüm kabul bantları | `V1LaunchTelemetryTargets.asset` |
| Event snapshot'ları | `GameplayTelemetry` |

Bu değerler launch kontratıdır. Değişiklik; ölçüm, designer review, asset güncellemesi, fingerprint
güncellemesi ve full regression gerektirir. Telemetry bantları **automatic retuning** yapmaz ve
harici analytics provider seçmez.

Production telemetry target fingerprint:
`58fc60a01a2442fdeaf544f59560159c21ca0e5dff48c0e54f27d817f8059dd3`

## Spawn ve phase eğrileri

### Day eğrileri

| Eğri | Exact key'ler |
|---|---|
| Night intensity | `(1, 0.60)`, `(3, 0.75)`, `(5, 0.86)`, `(7, 0.95)`, `(60, 0.95)` |
| Zombie HP | `(1, 1.00)`, `(60, 1.00)`; V1 quantity-only, stat scaling yok |
| Spawn batch | `(1, 1.00)`, `(60, 1.00)` |

`SampleDays = 60`. Day 60 sonrası son değer korunur.

### Exact spawn kontratı

| Değer | Launch |
|---|---:|
| Zombie base HP / damage | `20 / 5` |
| HP / damage cycle growth | `0 / 0` |
| Base batch / cycle growth | `2 / 0.10` |
| Frame drain ve batch üst sınırı | `16` |
| Active enemy cap | `900` |
| Base / minimum interval | `0.95s / 0.35s` |
| Day / Dusk start / Dusk end / Night / Dawn intensity | `0.55 / 1.00 / 1.35 / 1.65 / 0.15` |

Demand, base batch × phase intensity × cycle growth × day batch multiplier ile üretilir. Active
cap dolduğunda talep silinmez; `PendingEnemies` backlog'unda korunur ve boşalan kapasiteye frame
başına en fazla `16` entity ile akar. Boss, elite veya ikinci enemy type yoktur.

### 2026-07-20 simulator proxy review

Bot kohortlari player telemetry kabulunun yerine gecmez; yalniz ayni build icinde tuning yonunu
ve policy ayrimini tekrar edilebilir bicimde sinamak icin kullanilir.

- Degisiklik oncesi ayni fingerprint'te `50 Economy + 50 Defense` fresh-meta run tamamlandi:
  combined median `Day 7`, dagilim `2x Day 6 + 67x Day 7 + 31x Day 8`, Reach Day 3/6 `%100`,
  Reach Day 12 `%0`. Economy median `Day 7`, Defense median `Day 8` oldu.
- Bu dar `Day 7-8` duvarini genisletmek icin Day 1 night carpani `0.50 -> 0.60` ile erken risk
  artirildi; Day 7+ night carpani `1.00 -> 0.95` ve cycle batch growth `0.15 -> 0.10` ile guclu
  run'larin gec rampi yumusatildi. HP, damage, ekonomi, phase sureleri ve base intensity'ler
  degistirilmedi.
- Degisiklik sonrasi `10 Economy + 10 Defense` directional smoke: Economy `10x Day 8`, Defense
  `6x Day 8 + 4x Day 9`. Eski sample'daki `Day 9+ = %0` duvari kirildi ve savunma politikasinin
  avantaji daha okunur hale geldi.
- Provider-independent hedef tablosundaki kabul oranlari yine gercek `fresh completed` player
  telemetry kohortuna aittir. Bot smoke sonucu bu oranlari gecmis saymaz.

## Economy ve Wall

| Alan | Launch |
|---|---:|
| Wall base HP | `350` |
| Normal repair | `%25 MaxHP`, iyileşen HP başına `0.10 Stone`, Day çarpanı `1.00` |
| Emergency Repair | `%20 MaxHP`, `120s` cooldown |
| Rally | `60s` cooldown; `10s` boyunca `1.25×` fire-rate |
| Dawn survivor isteği / tek seferlik Food | `15 / 1` |
| Worker üretimi Wood / Stone / Iron / Food | `8 / 5.5 / 4.9 / 7` kişi başına dakika |
| Worker efficiency | level başına additive `%10` |
| Bed fiyatı | `100 Wood`; her `25` owned bed'de incremental büyüme |
| Worker CAP başlangıç fiyatı | `100 Wood + 25 Iron` |
| Worker EFF başlangıç fiyatı | `150 Wood + 50 Iron` |
| Worker yatırım büyümesi | `1.35×` |
| Arrow capacity | `200`; level başına `+200` |
| Arrow refill | `100 Arrow`; başlangıçta `4 Arrow / Wood` |
| Arrow efficiency | level başına `+1 Arrow / Wood` |
| Arrow CAP başlangıç fiyatı | `150 Wood + 25 Iron` |
| Arrow EFF başlangıç fiyatı | `200 Wood + 50 Iron` |
| Arrow yatırım büyümesi | `1.35×` |

Survivor Food'u yalnız kabul anında bir kez harcanır. Worker, population ve binalar pasif kaynak
tüketmez. Başarılı projectile pool rent'i `1 Arrow` harcar; oyuncu Arrow azaldıkça Wood ile refill
alır. CAP ve EFF yatırımları tek transaction'da iki kaynak ister.

## Combat

| Archer | Buy | Retrain | Damage | Fire rate | Range | Ek |
|---|---|---|---:|---:|---:|---|
| Basic | `45W + 20F` | yok | `10` | `1.5` | `15` | başlangıç tipi |
| Rapid | `55W + 35I + 20F` | `55W + 35I` | `6` | `3.0` | `14` | `rapid_volley` |
| Frost | `45W + 55S + 25I` | aynı | `5` | `1.2` | `14` | `2s`, `0.55×` slow |

Üç tipte population cost `1`, cost growth interval `25`, exponent `2` ve ortak archer cap `1000`.
Fireball baseline `60` damage, `2.2` radius, `45s` cooldown'dur. Scorched Earth `5 × %12`
aggregate tick'i `1s` aralıkla `%70` radius'ta; Echoing Detonation `0.85s` sonra `%60` damage ve
`%85` radius ile ikinci aggregate blast üretir.

## Meta

### Run reward

| Kaynak | Exact değer |
|---|---:|
| Kill band 1 | ilk `100` kill × `1.00` EMBER |
| Kill band 2 | `101..1000` × `0.25` EMBER |
| Overflow kill | `1000+` × `0.05` EMBER |
| Day reached | `10` EMBER / day |
| Night survived | `25` EMBER / night |
| Peak population | `0.20` EMBER / kişi |
| New record | `50` EMBER / record day |

### Permanent upgrade catalog

| Id | Base cost | Growth | Max | Effect / level |
|---|---:|---:|---:|---:|
| `start_wood` | `150` | `0.60` | sınırsız | `+75` |
| `start_stone` | `175` | `0.65` | sınırsız | `+50` |
| `start_iron` | `225` | `0.70` | sınırsız | `+30` |
| `start_food` | `150` | `0.60` | sınırsız | `+60` |
| `start_archers` | `400` | `1.00` | `1000` | `+1` |
| `start_beds` | `250` | `0.75` | sınırsız | `+5` |
| `wall_hp` | `300` | `0.80` | `5` | `+%5` |
| `production` | `350` | `0.80` | `5` | `+%3` |
| `arrow_efficiency` | `500` | `0.90` | `10` | `+1` |
| `essence_gain` | `600` | `0.90` | `10` | `+%5` |
| `node_pool_unlock` | `2000` | `0` | `1` | content gate |

Run ölünce tamamen sıfırlanır; yalnız Last Embers permanent progression kalır. Offline progress ve
pasif upkeep yoktur.

## Provider-independent telemetry hedefleri

Bandlar launch hipotezidir. `MinimumSamples` tamamlanmadan karar üretmez; band dışı sonuç otomatik
asset mutasyonu değil designer review tetikler.

| Id | Band | Minimum | Cohort | Source event'ler |
|---|---:|---:|---|---|
| `fresh_median_run_end_day` | Day `6–12` | `100` | fresh completed | `run_started`, `run_ended` |
| `fresh_reach_day_3_rate` | `%85–98` | `100` | fresh completed | `run_started`, `run_ended` |
| `fresh_reach_day_6_rate` | `%50–80` | `100` | fresh completed | `run_started`, `run_ended` |
| `fresh_reach_day_12_rate` | `%15–40` | `100` | fresh completed | `run_started`, `run_ended` |
| `spawn_cap_saturation_run_rate` | `%5–25` | `100` | completed runs | `phase_changed`, `run_ended` |
| `positive_backlog_phase_rate` | `%5–30` | `400` | phase samples | `phase_changed` |
| `zero_player_spend_run_rate` | `%0–10` | `100` | completed runs | `resource_spent`, `run_ended` |
| `median_distinct_main_resources_spent` | `2–4` | `100` | completed runs | `resource_spent`, `run_ended` |
| `normal_repair_run_rate` | `%35–70` | `100` | damaged runs | `wall_repaired`, `run_ended` |
| `arrow_refill_run_rate` | `%30–75` | `100` | archer runs | `resource_spent`, `archer_changed`, `run_ended` |
| `median_unused_bed_ratio_at_death` | `%0–20` | `100` | Wall-loss runs | `run_ended` |
| `median_night_wall_damage_ratio` | `%15–35` | `400` | Night samples | `phase_changed`, `run_ended` |
| `archer_retrain_share` | `%15–45` | `100` | archer changes | `archer_changed` |
| `fireball_use_after_unlock_rate` | `%60–95` | `50` | unlocked runs | `heart_node_bought`, `ability_cast`, `run_ended` |
| `council_expiry_rate` | `%5–20` | `100` | Council resolutions | `council_resolved` |
| `council_option_a_share` | `%35–65` | `100` | non-expired Councils | `council_resolved` |
| `median_meta_reward` | `250–900 EMBERS` | `100` | fresh completed | `run_started`, `run_ended` |
| `first_death_affords_any_upgrade_rate` | `%85–100` | `100` | first death | `run_ended` |
| `post_meta_purchase_median_day_gain` | `+1–4 Day` | `100` | paired post-purchase | `resource_spent`, `run_started`, `run_ended` |

## `run_ended v2`

`run_ended v2`, v1 day/kills/peak enemies/peak population/Wall timeline/meta reward alanlarına
final upgrade-applied Wall MaxHP, Wood/Stone/Iron/Food, Arrow/current capacity,
population/capacity/idle, Basic/Rapid/Frost count ve unspent Grave Essence snapshot'ını ekler.
Snapshot yalnız durable death receipt ve Meta
save başarıyla tamamlandıktan sonra çıkar. Böylece economy, housing, archer mix ve run-currency
hedefleri ayrı gameplay owner'ı veya per-frame telemetry kurulmadan ölçülebilir.

## Review ve değişiklik kuralı

1. Minimum cohort tamamlanır.
2. Aynı build/fingerprint içindeki sonuçlar karşılaştırılır.
3. Band dışı metrik runtime değerini kendiliğinden değiştirmez; designer nedeni belirler.
4. Onaylı değişiklik gerçek owner asset'e uygulanır.
5. Bu doküman, target asset fingerprint'i ve contract testleri aynı commit'te güncellenir.
6. Targeted ve full Unity regression geçmeden yeni tuning launch authority sayılmaz.
