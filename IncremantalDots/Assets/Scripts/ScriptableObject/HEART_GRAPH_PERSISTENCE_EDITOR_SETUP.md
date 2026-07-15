# Castle Heart Exact Graph Persistence - Editor Setup

## Catalog version

Production `HeartNodeCatalogSO` asset'inde `CatalogVersion >= 1` olmalidir.

Su degisikliklerden herhangi biri yapildiginda version artirilir:

- node ekleme/silme veya Id degisikligi;
- branch, type, rarity, depth veya tag degisikligi;
- effect, cost, growth veya soft-cap degisikligi;
- Keystone partneri;
- player-facing title/description/icon icerigi.

Eski run'i yeni catalog'a otomatik map eden Editor araci kullanilmaz. Bilincli migration
gerekiyorsa ayri schema/version karari ve regression testi gerekir.

## Scene ve prefab

Ek GameObject, component veya prefab binding'i yoktur. Aktif owner'lar:

- `GameManager.SaveRunSnapshot`: exact graph capture;
- `GameManager.TryRestoreRunFromCheckpoint`: preflight + replay;
- `RunPersistence`: guncel schema v11 JSON; v9 null-graph -> v10 ve v10 Council -> v11 migration;
- `HeartGraphPersistenceUtility`: clone, validation ve effect replay.

Production `heartCatalog` null ise run save calismaya devam eder fakat Heart content gate
olarak unavailable kalir. Onaysiz test catalog'u scene/prefab asset'ine yazilmaz.

## Dogrulama

- EditMode: `HeartGraphGeneratorTests` persistence/version/replay testleri.
- EditMode: `RunPersistenceTests` guncel v11 JSON ve v9/v10 migration testleri.
- PlayMode: `HeartGraphContinuePlayModeTests.Continue_ReplaysExactSavedHeartGraphWithoutReroll`.
- Full EditMode ve PlayMode regression.
- Unity Console compile/runtime error `0`.

PlayMode testi sentetik runtime catalog kullanir; production balance/content asset'i
olusturmaz. Test, exact graph JSON'un Continue ve sonraki capture sonrasinda degismedigini,
Rapid behavior replay'ini ve Heart presentation level state'ini kanitlar.
