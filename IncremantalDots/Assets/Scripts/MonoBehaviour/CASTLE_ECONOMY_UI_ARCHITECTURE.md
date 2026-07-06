# Castle Economy UI Architecture

`CastleEconomyUI`, mobile castle loop'taki eski full-screen Castle Interior ekonomi panelinin legacy runtime controller'idir. Yeni player-facing worker assignment akisi sol ust `WorkerEconomyDrawerUI` uzerindedir.

## Sorumluluklar

- `CastleEconomyPanel` panelini debug/legacy amacli acip kapatabilir.
- Mobile continuous worker drawer akisinda `PlayerFacingPanelEnabled = false` kalir.
- Panel acilirse `GameManager.OpenCastleEconomy()` uzerinden `MobilePrepPauseState.IsPaused = true` yapar.
- Close/Confirm ile paneli kapatir ve prep timer'in devam etmesini saglar.
- Worker slider'larini `GameManager.SetResourceWorkers()` API'sine baglar.
- Population total, idle, archer count, growth ve `WorkerBudgetText` alanlarini gunceller.
- `CastleTapHint` yeni player-facing akista gizli kalir.
- Slider degisiminde kalan prep suresine gore projected gain text'lerini gunceller.
- `CastleRepairButton` ile repair aksiyonunu Castle Interior paneli icine tasir.
- Pending economy event varsa 2 secenekli event alanini gosterir.
- Pending event varsa `EconomyEventBadge` ve opsiyonel glow/hint feedback'i aktif eder.

## Veri Akisi

```
Legacy CastleInteriorClickTarget -> CastleEconomyUI.OpenFromCastle()
CastleEconomyUI -> GameManager.OpenCastleEconomy() -> MobilePrepPauseState
Slider Input -> GameManager.SetResourceWorkers() -> MobilePopulationAllocation
Repair Button -> GameManager.RepairDefenseFull() -> Wall/Gate/Castle HP
Event Button -> GameManager.ChooseEconomyEvent() -> MobileEconomyEventState / Resources / Population
```

Prefab sadece gorsel katmandir. Runtime binding isim tabanlidir ve `MobileCastleSceneSetupWindow` tarafindan yapilir.

Projected gain formulu resource bazinda `net/min * WaveStateData.PrepTimer / 60` kullanir. `net/min`, worker production degerinden mevcut `ResourceConsumptionRate` degerinin cikarilmasidir. Bu alan legacy panel icindir; yeni worker assignment UI'i `WorkerEconomyDrawerUI` tarafindadir.

## Bilerek Yapilmayanlar

- Controller UI layout uretmez.
- Event davranisi JSON'a gomulmez.
- Panel yeni player-facing akista acilmaz; worker assignment sol drawer'dan yapilir.
- Projected gain legacy panel icin yalnizca kalan DayPrep suresini kullanir; continuous worker drawer akisi bu paneli player-facing kullanmaz.
