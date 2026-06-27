# Castle Economy UI Architecture

`CastleEconomyUI`, mobile castle loop'ta DayPrep sirasinda acilan full-screen Castle Interior ekonomi panelinin runtime controller'idir.

## Sorumluluklar

- `CastleEconomyPanel` panelini acip kapatir.
- Panel acikken `GameManager.OpenCastleEconomy()` uzerinden `MobilePrepPauseState.IsPaused = true` yapar.
- Close/Confirm ile paneli kapatir ve prep timer'in devam etmesini saglar.
- Worker slider'larini `GameManager.SetResourceWorkers()` API'sine baglar.
- Population total, idle, archer count, growth ve `WorkerBudgetText` alanlarini gunceller.
- DayPrep sirasinda `CastleTapHint` alanini gosterir; panel acikken, combat sirasinda ve stress mode'da gizler.
- Slider degisiminde kalan prep suresine gore projected gain text'lerini gunceller.
- `CastleRepairButton` ile repair aksiyonunu Castle Interior paneli icine tasir.
- Pending economy event varsa 2 secenekli event alanini gosterir.
- Pending event varsa `EconomyEventBadge` ve opsiyonel glow/hint feedback'i aktif eder.

## Veri Akisi

```
CastleInteriorClickTarget -> CastleEconomyUI.OpenFromCastle()
CastleEconomyUI -> GameManager.OpenCastleEconomy() -> MobilePrepPauseState
Slider Input -> GameManager.SetResourceWorkers() -> MobilePopulationAllocation
Repair Button -> GameManager.RepairDefenseFull() -> Wall/Gate/Castle HP
Event Button -> GameManager.ChooseEconomyEvent() -> MobileEconomyEventState / Resources / Population
```

UI Importer export'u sadece gorsel prefab uretir. Runtime binding isim tabanlidir ve `MobileCastleSceneSetupWindow` tarafindan yapilir.

Projected gain formulu resource bazinda `net/min * WaveStateData.PrepTimer / 60` kullanir. `net/min`, worker production degerinden mevcut `ResourceConsumptionRate` degerinin cikarilmasidir; panel acikken timer paused oldugu icin slider degisimi sonucu sabit ve okunabilir kalir.

## Bilerek Yapilmayanlar

- Controller UI layout uretmez.
- Event davranisi JSON'a gomulmez.
- Panel combat sirasinda acilmaz; sadece DayPrep ve kale tiklamasi ile acilir.
- Projected gain yalnizca kalan DayPrep suresini kullanir; combat suresi tahmini yapmaz.
