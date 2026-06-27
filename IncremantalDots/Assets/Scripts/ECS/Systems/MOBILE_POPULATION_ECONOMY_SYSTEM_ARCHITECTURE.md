# Mobile Population Economy System Architecture

`MobilePopulationEconomySystem`, `MobileCastleCombatConfig` bulunan mobile castle loop icin worker tabanli resource ekonomisini yazar.

## Sorumluluklar

- `MobilePopulationAllocation` icindeki Wood/Stone/Iron/Food worker sayilarini clamp eder.
- `PopulationState.Workers` ve `PopulationState.Idle` degerlerini allocation + archer sayisina gore gunceller.
- `ResourceProductionRate` degerlerini worker sayisi x worker production tuning olarak yazar.
- Her completed wave sonrasi `DayPrep` basinda population growth uygular.
- `MobileEconomyEventState` icin nadir event roll eder ve aktif production bonusunu rate'lere uygular.

## Akis

Sistem `ArrowProductionSystem` sonrasinda, `PopulationTickSystem` oncesinde calisir. Boylece mobile worker allocation, eski building producer hesaplarini override eder ve population food consumption ayni frame guncel worker sayisini okur.

Stress mode'da calismaz. Legacy/non-mobile sahnelerde `MobileCastleCombatConfig` ve `MobilePopulationAllocation` olmadigi icin hic devreye girmez.

## Event Modeli

Eventler sadece normal mobile `DayPrep` basinda roll edilir:

- Roll sansi: `MobileCastleCombatConfig.EconomyEventChance`
- Cooldown: `EconomyEventCooldownWaves`
- Pending event secilmezse `DayNightPrepSystem` gece baslarken expire eder.
- Choice B production bonusu `ProductionBonusResource` ve `ProductionBonusMultiplier` alanlarina yazilir; sonraki DayPrep basinda expire olur.

V1 eventleri geneldir: resource stash, quarry crew, refugee cart. UI metinleri `GameManager` tarafinda verilir.

## Bilerek Yapilmayanlar

- Population cap sistemi yok; mobile setup internal high capacity kullanir.
- Worker assignment otomatik optimize edilmez.
- Event popup/polish bu sistemde degil, `CastleEconomyUI` tarafindadir.
