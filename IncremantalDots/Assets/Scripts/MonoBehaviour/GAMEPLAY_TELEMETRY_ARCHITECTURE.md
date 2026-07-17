# Gameplay Telemetry Architecture

## Owner siniri

`GameplayTelemetry`, gameplay state'i sahiplenmeyen provider-bagimsiz event cikisidir. Her event,
canonical runtime owner'larindan alinmis immutable bir JSON snapshot olarak `Emitted` event'ine
verilir. Harici analytics SDK'si veya hedefi bu katmanda secilmez; owner karari bekleyen telemetry
target'i sonradan bu event'e subscriber olarak baglanir.

Editor ve Development Player'da ayni envelope `[DW-TELEMETRY]` prefix'iyle Console'a yazilir.
Release Player'da log yoktur; subscriber cikisi korunur. Telemetry subscriber hatasi gameplay
transaction'ini durdurmaz.

## `run_started` v1

`GameManager`, yeni run tamamen kurulduktan ve Heart runtime en az bir kez denendikten sonra tek
event uretir:

- envelope: `EventName`, `SchemaVersion`, `RunId`, `PayloadJson`
- Meta: production catalog'daki tum definition Id/level'lari, meta schema ve definition sayisi
- baslangic kaynaklari: Wood/Stone/Iron/Food, Arrow stok/capacity, Grave Essence, population/cap
- Heart identity: catalog configured, runtime attempted/ready, graph/catalog version ve seed

Production Heart catalog owner karari acikken event kaybolmaz: `CatalogConfigured=false`,
`GraphReady=false`, version/seed `0` olarak acik unconfigured state kaydedilir. Catalog geldiginde
ayni contract gercek generated graph identity'sini otomatik tasir.

`GameBootstrap.PendingAction` uygulanmadan event uretilmez. `RestartGame()` yeni RunId icin yeni
event uretir; exact Continue restore edilen mevcut RunId'yi `run_started` olarak tekrar saymaz.
Telemetry guard alanlari yalnız emission idempotency'sidir; run/save state owner'i degildir ve
save schema'ya yazilmaz.

## `phase_changed` v1

`GameManager`, her yeni `(RunId, Day, Phase)` kimligini canonical ECS snapshot'larindan bir kez
yayinlar:

- Day/Phase: `ContinuousSiegeCycleData.CycleIndex + 1` ve `SiegeCyclePhase`
- alive enemies: `WaveStateData.ZombiesAlive`
- spawn backlog: `ContinuousSpawnBudgetData.PendingEnemies`

Phase kimligi machine-readable `day/dusk/night/dawn` degerlerinden biridir. Alive/backlog degerleri
yalniz transition sonrasi ilk gozlem aninin snapshot'idir; ayni phase icindeki sayi degisimleri yeni
event uretmez. Yeni run'da `run_started` once, ilk Day 1 `phase_changed` hemen sonra gelir.
Exact Continue restore edilen mevcut `(RunId, Day, Phase)` kimligi prime edilir ve duplicate event
uretilmez. Bu idempotency save state'e yazilmaz ve cycle/wave/spawn budget sahipligini kopyalamaz.

## `resource_spent` v1

`GameManager`, yalniz basarili player purchase transaction'i canonical runtime state'e commit
edildikten sonra event uretir:

- payload: `Resource`, `Amount`, `PurchaseType`, `ResultingLevel`, `ResultingCount`
- resource identity: `wood/stone/iron/food/grave_essence/meta_currency`
- birden fazla kaynak harcayan tek purchase, ayni purchase type ve sonuc snapshot'iyla kaynak
  basina bir event uretir; sira Wood, Stone, Iron, Food olarak sabittir
- level tabanli upgrade'ler post-commit level'i, quantity tabanli islemler post-commit count'u yazar
- normal Wall repair `ResultingCount` alanina post-commit tam HP birimini yazar

Aktif V1 owner baglantilari normal Wall repair, Arrow refill/capacity/efficiency, bed capacity,
dort worker binasinin capacity/efficiency upgrade'leri, Basic/Rapid/Frost buy, Rapid/Frost retrain,
Castle Heart node purchase ve olum sonrasi durable Meta upgrade transaction'laridir. Meta currency
player-facing adi owner karari bekledigi icin machine identity `meta_currency` olarak sabittir.

Event `SpendResources` yardimcisina genel olarak baglanmaz. Boylece Council negatif effect'leri,
dawn arrival Food kesintisi, pasif/otomatik tuketim ve rollback edilen islemler purchase olarak
sayilmaz. Player-facing olmayan legacy Fortify, Tech Tree, archer type upgrade/unlock ve Castle
upgrade yollari da V1 event kapsamina dahil degildir. `freeEconomyTestMode` gercek debit yapmadigi
icin resource event'i uretmez. Reddedilen veya rollback edilen purchase sifir event uretir.

Castle Heart event'i wallet debit aninda degil, `HeartPurchaseService` graph/effect commit'i tamamen
basarili dondukten sonra yazilir. Meta event'i de Souls, level ve disk save ayni atomik transaction'da
basarili olduktan sonra yazilir. Subscriber hatasi gameplay transaction'ini geri almaz.

## `archer_changed` v1

`GameManager`, yalniz basarili player buy veya Basic retrain transaction'i archer entity/state
commit'ini tamamladiktan ve type count cache'ini canonical ECS sorgusundan yeniledikten sonra event
uretir:

- payload: `ChangeType`, `TypeFrom`, `TypeTo`, `TotalCapUsage`
- buy transition'i: `buy`, `none -> basic/rapid/frost`
- retrain transition'i: `retrain`, `basic -> rapid/frost`
- cap snapshot'i: `GetTotalArcherCount()` post-commit sonucu, exact `1..1000`

Buy event'i ayni transaction'in `resource_spent` kayitlarindan sonra gelir. Free economy test
modunda debit olmadigi icin resource event'i cikmasa bile archer entity gercekten commit edildiyse
`archer_changed` cikar. Spawn fail edip resource rollback yapan, locked/cap/population/resource
guard'inda reddedilen veya entity kayboldugu icin retrain rollback eden islem sifir archer event'i
uretir.

Council/meta baslangic bonusu, exact Continue restore ve merkezi `SpawnArcher`/restore yollari
oyuncu buy/retrain transaction'i degildir; bu event'e baglanmaz. Event yeni archer state owner'i,
history listesi veya save alani kurmaz; yalniz mevcut entity ve canonical total snapshot'ini yayar.

## `heart_node_bought` v1

`GameManager.TryPurchaseHeartNode`, yalniz `HeartPurchaseService` Grave Essence spend, graph level,
effect, Keystone lock ve reveal commit'lerini tamamen basarili tamamladiktan sonra event uretir:

- payload: `NodeId`, `Level`, `Depth`, `Cost`, `RevealedChildren`
- level ve cost: `HeartPurchaseQuote.NewLevel` ile bulk-safe `TotalGraveEssenceCost`
- depth: service purchase plan'indaki canonical `GeneratedHeartNodeState.Depth`
- revealed children: ilk purchase'ta gercekten `Hidden -> Revealed` olan outgoing child sayisi

Ayni transaction'in `resource_spent: grave_essence/heart_node` kaydi once, `heart_node_bought`
sonra gelir. Insufficient Essence, hidden/locked/root/unknown node, invalid catalog/graph, effect
preflight veya spend rejection sifir Heart event'i uretir. Evaluate/quote, exact Continue restore ve
effect replay purchase degildir; event'e baglanmaz. Telemetry graph state'ini sahiplenmez ve hidden
child kimliklerini aciga cikarmaz; yalniz reveal sayisini yayar.

## `council_resolved` v1

`GameManager`, regular Council karari canonical runtime transaction'inda kesinlestikten sonra tek
event uretir:

- payload: `Day`, `TemplateId`, `Resolution`, `Effects`, `NextNightDelta`
- resolution identity: player secimi icin `option_a/option_b`, Dusk'ta secilmeden kapanis icin
  `expired`
- her effect machine-readable `Kind`, `Resource`, `Amount`, `Rate`, `DurationDays` snapshot'idir;
  resource kullanmayan effect'ler `none`, tum uretim/cap hedefi `all` kimligini kullanir
- `NextNightDelta`, authored raw rate'i degil production `CouncilEffectGuardUtility` clamp'i
  uygulandiktan sonraki gercek count multiplier delta'sidir

Secim eventi effect apply, otomatik/curated flag yazimi ve active kartin temizlenmesinden sonra;
expire eventi active kart temizlendikten sonra cikar. Affordability/content gate'inde reddedilen
secim, bos state'te tekrar expire ve UI'nin yalniz kart gostermesi sifir event uretir. Expired
payload effect veya next-night delta tasimaz.

Event yeni Council history/state owner'i kurmaz. Cozulmus kart save'de active olmadigi icin exact
Continue ayni karari tekrar yaymaz; unresolved active kart ise mevcut exact payload'iyla restore
olur ve oyuncu daha sonra karar verdiginde tek event uretir. Emergency Council iptal edilen V1
kapsamina dahil degildir.

## Genisleme kurali

Tracker'daki sonraki event'ler ayni `GameplayTelemetryRecord` cikisini kullanir. Yeni manager,
parallel ECS singleton veya ikinci analytics bus kurulmaz. Her payload kendi schema version'ina ve
canonical transaction owner'ina sahip olur.
