# Dead Walls V1 — Castle Heart Launch Catalog

> **Durum:** Launch content authority
> **Catalog:** `HeartNodeCatalog.asset` — version `1`
> **Runtime kapsamı:** `35` node, `4` branch, `4` Keystone çifti, `4` guaranteed route, `4` repeatable sink
> **Para birimi:** Yalnız `Grave Essence`

Bu belge, Castle Heart ekranında oyuncuya sunulan launch node havuzunun içerik ve effect
sözleşmesidir. Runtime asset'leri ve `CastleHeartProductionCatalogBuilder` bu tablonun çalışan
karşılığıdır. Legacy `TechTreeCatalogSO` yalnız migration provenance kaynağıdır; graph, maliyet,
satın alma veya reveal sahibi değildir.

## Okuma kuralı

- `Depth 1–5`, node'un generator tarafından yerleştirilebileceği aralıktır.
- Tek seferlik node maliyeti `Base` değeridir.
- Repeatable fiyatı: `unit(level) = Base + level × ceil(Base × Growth)`.
- Yüzde effect'ler decimal olarak author edilir: `0.10 = +10%`.
- Fire rate, range, Frost slow, spell radius ve cooldown değerleri tabloda belirtilen soft-cap'e
  asimptotik yaklaşır; node yapay bir max level'a kilitlenmez.
- Keystone seçimi yalnız tabloda adı geçen karşı Keystone'u kapatır.

## Owner-onaylı Keystone trade-off sözleşmesi — 2026-07-18

Keystone, normal bir güç artışından farklı olarak koşu boyunca karakter belirleyen iki eşit
değerli doktrinden biridir. Çift aynı anda görünür, aynı fork üzerinde yan yana sunulur ve
oyuncu **yalnız birini** `COMMIT` edebilir. Seçilen taraf koşu bitene kadar aktif kalır; sadece
exact partner `KeystoneConflict` ile kilitlenir. Her iki seçim de aynı branch devamını açar.

| Branch | Seçenek A | Seçenek B | Kararın oyuncu anlamı |
|---|---|---|---|
| Army | `heavy_draw`: Basic damage `+30%` | `storm_cadence`: Basic fire rate raw `+28%` | Ağır tek atışlar veya daha sık ok yağmuru |
| Defense | `bastion_doctrine`: Wall max HP `+35%` | `salvage_doctrine`: repair Stone cost `-30%` | Daha büyük hata payı veya daha ucuz toparlanma |
| Production | `deep_stores`: her resource worker capacity `+6` | `relentless_shifts`: her resource production `+20%` | Daha geniş iş gücü tavanı veya mevcut işçide daha yüksek verim |
| Heart / Magic | `inferno_heart`: Fireball damage `+45%` | `chronomancer_heart`: cooldown raw `-26%` | Daha yıkıcı tek patlama veya daha sık büyü kullanımı |

UI exact olarak `CHOOSE ONE · RUN COMMITMENT`, partner adı ve seçim sonrası
`DOCTRINE COMMITTED / LOCKED FOR THIS RUN` durumlarını gösterir. Generated graph'ın version `1`
node/edge topolojisi compatibility için değişmez; reveal/purchase katmanı ardışık serialized
çifti oyuncuya gerçek fork olarak bağlar.

## Army — 9 node

| ID | Oyuncu adı | Tür | Depth | Base / Growth | Exact effect | Legacy kaynak |
|---|---|---:|---:|---:|---|---|
| `rapid_archer_unlock` | Rapid Doctrine | Unlock | 1–2 | 16 | Rapid Archer recruitment açılır | `rapid_archer` |
| `frost_archer_unlock` | Winter Oath | Unlock | 1–3 | 18 | Frost Archer recruitment açılır | `frost_archer` |
| `bow_mastery` | Bow Mastery | Repeatable sink | 2–5 | 15 / 0.40 | Basic damage `+6% / level` | `bow_mastery` |
| `volley_mastery` | Volley Mastery | Repeatable | 1–5 | 16 / 0.42 | Basic fire rate raw `+6% / level`; soft-cap `+75%` | `volley_mastery` |
| `rapid_drill` | Clockwork Volley | Evolution | 2–5 | 32 | Rapid fire rate raw `+18%`; soft-cap `+75%` | `rapid_volley` |
| `frostbite_tips` | Frostbite Tips | Evolution | 2–5 | 34 | Frost slow multiplier `-0.10` raw; minimum `0.35` | `frost_arrows` |
| `longbow_geometry` | Longbow Geometry | Evolution | 2–5 | 30 | Basic range `+0.55`; soft-cap `+3.0` | Yeni |
| `heavy_draw` | Heavy Draw | Keystone | 3–5 | 48 | Basic damage `+30%`; `storm_cadence` kapanır | `bow_training` |
| `storm_cadence` | Storm Cadence | Keystone | 3–5 | 48 | Basic fire rate raw `+28%`; soft-cap `+75%`; `heavy_draw` kapanır | Yeni |

## Defense — 8 node

| ID | Oyuncu adı | Tür | Depth | Base / Growth | Exact effect | Legacy kaynak |
|---|---|---:|---:|---:|---|---|
| `living_ramparts` | Living Ramparts | Unlock | 1–2 | 14 | Wall max HP `+12%` | `wall_reinforcement` |
| `stone_memory` | Stone Memory | Repeatable sink | 2–5 | 18 / 0.45 | Wall max HP `+7% / level` | Yeni |
| `repair_efficiency` | Measured Repairs | Evolution | 2–5 | 30 | Wall repair Stone cost `-15%` | `repair_efficiency` |
| `layered_masonry` | Layered Masonry | Evolution | 2–5 | 32 | Wall max HP `+18%` | `repair_crew` |
| `arrow_vault` | Arrow Vault | Evolution | 1–5 | 28 | Arrow capacity `+80` | Yeni |
| `fletchers_measure` | Fletcher's Measure | Evolution | 2–5 | 28 | Her Wood satın alımında Arrow output `+4` | Yeni |
| `bastion_doctrine` | Bastion Doctrine | Keystone | 3–5 | 50 | Wall max HP `+35%`; `salvage_doctrine` kapanır | Yeni |
| `salvage_doctrine` | Salvage Doctrine | Keystone | 3–5 | 50 | Wall repair Stone cost `-30%`; `bastion_doctrine` kapanır | Yeni |

## Production — 10 node

| ID | Oyuncu adı | Tür | Depth | Base / Growth | Exact effect | Legacy kaynak |
|---|---|---:|---:|---:|---|---|
| `lumber_covenant` | Lumber Covenant | Repeatable sink | 2–5 | 12 / 0.48 | Wood production `+8% / level` | `wood_camp` |
| `stone_guild` | Stone Guild | Repeatable | 1–5 | 14 / 0.50 | Stone production `+8% / level` | Yeni |
| `iron_foundry` | Iron Foundry | Repeatable | 1–5 | 15 / 0.52 | Iron production `+8% / level` | Yeni |
| `harvest_ledger` | Harvest Ledger | Repeatable | 1–5 | 13 / 0.49 | Food production `+8% / level` | `food_stores` |
| `worker_camp` | Worker Quarters | Evolution | 2–5 | 34 | Wood/Stone/Iron/Food worker capacity ayrı ayrı `+3` | `worker_camp` |
| `dawn_housing` | Dawn Housing | Evolution | 2–5 | 32 | Dawn population arrival `+4` | `population_growth` |
| `arrow_workshop` | Arrow Workshop | Evolution | 2–5 | 27 | Her Wood satın alımında Arrow output `+3` | Yeni |
| `reserve_stacks` | Reserve Stacks | Evolution | 2–5 | 29 | Arrow capacity `+100` | Yeni |
| `deep_stores` | Deep Stores | Keystone | 3–5 | 52 | Dört resource worker capacity'si ayrı ayrı `+6`; `relentless_shifts` kapanır | Yeni |
| `relentless_shifts` | Relentless Shifts | Keystone | 3–5 | 52 | Dört resource production'ı ayrı ayrı `+20%`; `deep_stores` kapanır | Yeni |

## Heart / Magic — 8 node

| ID | Oyuncu adı | Tür | Depth | Base / Growth | Exact effect | Legacy kaynak |
|---|---|---:|---:|---:|---|---|
| `fireball_unlock` | Ember Rite | Unlock | 1–2 | 20 | Fireball targeting/cast açılır | `arcane_tower` |
| `searing_flames` | Searing Flames | Repeatable sink | 2–5 | 18 / 0.50 | Fireball damage `+10% / level` | `fire_power` |
| `greater_blast` | Greater Blast | Evolution | 2–5 | 34 | Fireball radius `+0.90`; soft-cap `+3.5` | `fire_radius` |
| `arcane_focus` | Arcane Focus | Evolution | 2–5 | 36 | Fireball cooldown raw `-18%`; soft-cap `-60%` | `fire_cooldown` |
| `blazing_core` | Blazing Core | Evolution | 2–5 | 38 | Fireball damage `+35%` | Yeni |
| `ember_reservoir` | Ember Reservoir | Evolution | 1–5 | 31 | Fireball radius `+0.65`; soft-cap `+3.5` | Yeni |
| `inferno_heart` | Inferno Heart | Keystone | 3–5 | 55 | Fireball damage `+45%`; `chronomancer_heart` kapanır | Yeni |
| `chronomancer_heart` | Chronomancer Heart | Keystone | 3–5 | 55 | Fireball cooldown raw `-26%`; soft-cap `-60%`; `inferno_heart` kapanır | Yeni |

## Guaranteed route ve sink sözleşmesi

| Branch | Her graph'ta reachable garanti | Repeatable sink |
|---|---|---|
| Army | `rapid_archer_unlock`, `frost_archer_unlock` | `bow_mastery` |
| Defense | `living_ramparts` | `stone_memory` |
| Production | Branch root bağlantısı | `lumber_covenant` |
| Heart / Magic | `fireball_unlock` | `searing_flames` |

## Clean legacy migration

Migration **asset kopyası değildir**. Builder, onaylı 18 legacy fikrin provenance Id'sini yeni
node asset'ine yazar; yeni node'un branch/type/depth/Grave Essence/effect sözleşmesi yine bu launch
catalog tarafından sahiplenilir.

| Legacy ID | Launch Heart ID | Karar |
|---|---|---|
| `rapid_archer` | `rapid_archer_unlock` | Unlock korunur |
| `frost_archer` | `frost_archer_unlock` | Unlock korunur |
| `bow_mastery` | `bow_mastery` | Repeatable damage sink olarak yeniden dengelenir |
| `volley_mastery` | `volley_mastery` | Repeatable fire-rate sink olarak yeniden dengelenir |
| `rapid_volley` | `rapid_drill` | Tek seferlik Rapid uzmanlığına dönüşür |
| `frost_arrows` | `frostbite_tips` | Frost slow uzmanlığına dönüşür |
| `bow_training` | `heavy_draw` | Damage doctrine Keystone'una dönüşür |
| `wall_reinforcement` | `living_ramparts` | Guaranteed Wall yolu olur |
| `repair_efficiency` | `repair_efficiency` | Repair economy korunur |
| `repair_crew` | `layered_masonry` | One-time Wall HP katmanına dönüşür |
| `wood_camp` | `lumber_covenant` | Wood sink olur |
| `food_stores` | `harvest_ledger` | Food repeatable olur |
| `worker_camp` | `worker_camp` | Dört binaya birden capacity verir |
| `population_growth` | `dawn_housing` | Dawn arrival etkisine bağlanır |
| `arcane_tower` | `fireball_unlock` | Fireball garanti unlock'u olur |
| `fire_power` | `searing_flames` | Fireball damage sink olur |
| `fire_radius` | `greater_blast` | Radius evolution'ına dönüşür |
| `fire_cooldown` | `arcane_focus` | Cooldown evolution'ına dönüşür |

Şu legacy içerikler bilinçli olarak migrate edilmez:

- `castle_heart`: authored satın alınabilir node değil, generated graph'ın system-owned root'udur.
- `basic_archer`: koşu başlangıç baseline'ıdır; Heart unlock'u değildir.
- `moat_flame` ve `moat_dig`: Moat V1 guardrail'i gereği dormant kalır.
- Legacy resource cost, prerequisite/reveal edge ve runtime level state'i yeni catalog'a kopyalanmaz.

## Sunum ve versioning kilidi

- Node kartı branch/type eyebrow, exact effect current → after → delta, açık `ESSENCE` maliyeti ve
  node türüne özel `UNLOCK / DEEPEN / EVOLVE / COMMIT` eylemini gösterir.
- Keystone lore metni partner lock bilgisini tekrar etmez; iki kart, fork damarı ve exact
  `CHOOSE ONE · RUN COMMITMENT` satırı kararı birlikte anlatır.
- Catalog version `1` korunur çünkü bu lock turu node Id/effect/cost/graph semantiğini değiştirmez;
  serialized topology değişmeden coupled reveal, ortak branch devamı ve presentation fork'u ekler.
- Gelecekte node Id, effect, cost, conflict veya generator uygunluğu değişirse `CatalogVersion`
  artırılır. Eski exact graph sessizce remap edilmez.
