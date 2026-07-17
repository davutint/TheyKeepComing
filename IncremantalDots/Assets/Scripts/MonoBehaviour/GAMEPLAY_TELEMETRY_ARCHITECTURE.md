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

## Genisleme kurali

Tracker'daki sonraki event'ler ayni `GameplayTelemetryRecord` cikisini kullanir. Yeni manager,
parallel ECS singleton veya ikinci analytics bus kurulmaz. Her payload kendi schema version'ina ve
canonical transaction owner'ina sahip olur.
