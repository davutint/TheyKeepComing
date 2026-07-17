# Gameplay Telemetry Editor Setup

Inspector veya scene binding gerekmez. `GameManager.Telemetry.cs`, mevcut `GameManager` partial
sinifinin parcasidir; `GameplayTelemetry.cs` ayni `DeadWalls` runtime assembly'sinde derlenir.

## Dogrulama

1. `NewGameScene` Play Mode'a girilir veya ana menuden `NEW RUN` secilir.
2. Console'da tek `[DW-TELEMETRY]` kaydi ve envelope icinde `run_started` aranir.
3. Payload'da production Meta definition seviyeleri, gercek baslangic kaynaklari ve Heart
   graph identity kontrol edilir.
4. Save/Continue yapildiginda ayni RunId icin ikinci `run_started` olmamalidir.

Otomatik kapsam:

- EditMode `GameplayTelemetryTests`: payload factory, envelope serialization ve invalid identity
  guard'lari.
- PlayMode `GameplayTelemetryPlayModeTests`: gercek NewGameScene yeni-run emission'i ve exact
  Continue duplicate guard'i.

Harici analytics target'i bu kurulumun parcasi degildir; tracker'daki owner-karari maddesi
onaylanmadan SDK, servis veya endpoint eklenmez.
