# Mobile Population Economy System Architecture

`MobilePopulationEconomySystem`, `MobileCastleCombatConfig` bulunan mobile castle loop icin worker tabanli resource ekonomisini yazar.

## Sorumluluklar

- `MobilePopulationAllocation` icindeki Wood/Stone/Iron/Food worker sayilarini population ve resource cap'lerine gore clamp eder.
- Etkin resource cap'lerini `MobileCastleCombatConfig`'ten allocation state'e aynalar.
- Kalici target ratio'lari normalize eder ve yalniz yeni gelen population'i bu hedeflere gore deterministik dagitir.
- Pozitif target'larin cap'i doldugunda dagitilamayan kisileri Idle Population'da birakir.
- `PopulationState.Workers` ve `PopulationState.Idle` degerlerini allocation + archer sayisina gore gunceller.
- `ResourceProductionRate` degerlerini worker sayisi x worker production tuning olarak yazar.
- Continuous siege aciksa her tamamlanan 60 saniyelik cycle basina bir kez population growth uygular.
- Legacy DayPrep akisinda her completed wave sonrasi `DayPrep` basinda population growth uygular.
- `MobileEconomyEventState` icin nadir event roll eder ve aktif production bonusunu rate'lere uygular.

## Akis

Sistem `ArrowProductionSystem` sonrasinda, `PopulationTickSystem` oncesinde calisir. Boylece mobile worker allocation eski building producer hesaplarini override eder; `PopulationTickSystem` ayni frame yalniz population aggregate'lerini son kez tutarli hale getirir. V1 castle loop'ta pasif population Food tuketimi yoktur.

Stress mode'da calismaz. Legacy/non-mobile sahnelerde `MobileCastleCombatConfig` ve `MobilePopulationAllocation` olmadigi icin hic devreye girmez.

## Target Ratio ve Arrival Akisi

- Target ratio toplami `10.000` basis point'tir.
- İlk runtime gözlemi mevcut population'i baseline kabul eder; önceden var olan idle nüfusu dağıtmaz.
- Sonraki pozitif population farkı `WorkerAllocationUtility` ile Wood/Stone/Iron/Food hedeflerine atanır.
- Hedef oranı `0` olan resource otomatik worker almaz.
- Pozitif hedeflerin cap'i doluysa overflow idle kalır.
- Target ratio değişikliği gerçek worker count'u anında yeniden dağıtmaz; yalnız sonraki arrival'ların yönünü değiştirir.

## Event Modeli

Eventler legacy normal mobile `DayPrep` basinda roll edilir:

- Roll sansi: `MobileCastleCombatConfig.EconomyEventChance`
- Cooldown: `EconomyEventCooldownWaves`
- Pending event secilmezse `DayNightPrepSystem` gece baslarken expire eder.
- Choice B production bonusu `ProductionBonusResource` ve `ProductionBonusMultiplier` alanlarina yazilir; sonraki DayPrep basinda expire olur.

V1 eventleri geneldir: resource stash, quarry crew, refugee cart. UI metinleri `GameManager` tarafinda verilir.

## Bu Alt Pakette Bilerek Yapilmayanlar

- Satın alınabilir `MobileBedCapacityState`, `GameManager` transaction API'si ve exact save `v5` artık vardır; bu sistem henüz bed boşluğunu Dawn arrival limitine bağlamaz.
- Sahip olunan yatağa göre büyüyen maliyet eğrisi ve tek seferlik arrival Food maliyeti sonraki ayrı tracker işleridir.
- Mevcut worker'ları target ratio değişince zorla retrain/redistribute etmez.
- Worker world representation actual count'tan ayridir; `WorkerVisualRepresentationUtility` Low/Medium/High egriyle resource basina en fazla `32` temsili visual uretir.
- Event popup/polish bu sistemde degil, `CastleEconomyUI` tarafindadir.

## Doğrulama

- Saf allocation matematiği ve migration: EditMode.
- Yeni population, cap ve idle overflow: gerçek `NewGameScene` PlayMode.
- Actual worker + target ratio save/Continue: `ExactRunContinuePlayModeTests`.
