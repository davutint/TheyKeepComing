# Mobile Population Economy System Architecture

`MobilePopulationEconomySystem`, `MobileCastleCombatConfig` bulunan mobile castle loop icin worker tabanli resource ekonomisini yazar.

## Sorumluluklar

- `MobilePopulationAllocation` icindeki Wood/Stone/Iron/Food worker sayilarini population ve resource cap'lerine gore clamp eder.
- Etkin resource cap'lerini `MobileCastleCombatConfig`'ten allocation state'e aynalar.
- Mobile `PopulationState.BaseCapacity/Capacity` değerlerini `MobileBedCapacityState` toplamından senkronlar.
- Kalici target ratio'lari normalize eder ve yalniz yeni gelen population'i bu hedeflere gore deterministik dagitir.
- Pozitif target'larin cap'i doldugunda dagitilamayan kisileri Idle Population'da birakir.
- `PopulationState.Workers` ve `PopulationState.Idle` degerlerini allocation + archer sayisina gore gunceller; idle sonucu `WorkerAllocationUtility.ResolveIdlePopulation` ile `GameManager` player API'siyle ayni owner'dan gelir.
- `ResourceProductionRate` degerlerini worker sayisi x worker production tuning olarak yazar.
- Etkin worker cap ve kisi basi production tuning'ini `GameManager`in profile base + Tech +
  Heart + Meta + worker-building aggregate'i yazdigi `MobileCastleCombatConfig` uzerinden
  tuketir; bina seviyesini burada ikinci kez uygulamaz. V1 base rate'leri
  `DefaultDifficulty.asset` icinde `8 / 5.5 / 4.9 / 7`dir.
- Continuous siege aciksa her tamamlanan 60 saniyelik cycle basina bir kez Dawn arrival bütçesi uygular.
- Legacy DayPrep akisinda her completed wave sonrasi `DayPrep` basinda aynı arrival bütçesini uygular.
- İstenen arrival'ı boş yatak ve mevcut Food / kişi maliyetiyle sınırlar; requested/accepted/required Food sonucunu allocation state'e yazar.
- Kabul edilen survivor'ların toplam Food maliyetini population artışıyla aynı transaction içinde stoktan bir kez düşer.
- `MobileEconomyEventState` icin nadir event roll eder ve aktif production bonusunu rate'lere uygular.

## Akis

Sistem `BuildingPopulationSystem` ve `ArrowProductionSystem` sonrasinda, `PopulationTickSystem` oncesinde calisir. Boylece mobile bed state'i legacy building kapasite hesabının son owner'ı olur; worker allocation eski building producer hesaplarini override eder. `PopulationTickSystem` ayni frame yalniz population aggregate'lerini son kez tutarli hale getirir. V1 castle loop'ta pasif population Food tuketimi yoktur.

Stress mode'da calismaz. Legacy/non-mobile sahnelerde `MobileCastleCombatConfig` ve `MobilePopulationAllocation` olmadigi icin hic devreye girmez.

## Target Ratio ve Arrival Akisi

- Dawn kabul formülü `min(requested, totalBeds - currentPopulation, Food / FoodCostPerArrival)` şeklindedir.
- Aktif V1 tuning'i requested `15`, kişi başı Food `1` değeridir.
- Food yetersizliğinde mevcut population düşmez; yalnız yeni arrival sınırlanır.
- `RequiredFood = accepted × FoodCostPerArrival` aynı işlemde `ResourceData.Food` stokundan düşülür.
- Persistent `LastPopulationGrowthCycle/LastPopulationGrowthWave` marker'ları aynı Dawn'da ve exact Continue sonrasında çift population/harcama yapılmasını engeller.
- Target ratio toplami `10.000` basis point'tir.
- İlk runtime gözlemi mevcut population'i baseline kabul eder; önceden var olan idle nüfusu dağıtmaz.
- Sonraki pozitif population farkı `WorkerAllocationUtility` ile Wood/Stone/Iron/Food hedeflerine atanır.
- Hedef oranı `0` olan resource otomatik worker almaz.
- Pozitif hedeflerin cap'i doluysa overflow idle kalır.
- Target ratio değişikliği gerçek worker count'u anında yeniden dağıtmaz; yalnız sonraki arrival'ların yönünü değiştirir.

## Arrival Görsel Tüketicisi

Bu sistem yalnız accepted population transaction'ını ve persistent marker'ı yazar. `GameManager`, yeni marker ile `LastArrivalAcceptedCount` sonucunu gözleyip mevcut `VillagerWorker` prefabından en fazla `15` geçici temsilci üretir. `SurvivorArrivalVisualSystem` bu entity'leri sağ battlefield'dan Wall arkasına yürütür ve varışta yok eder. Görsel akış population, Food veya worker allocation state'ine tekrar yazmaz.

## Event Modeli

Eventler legacy normal mobile `DayPrep` basinda roll edilir:

- Roll sansi: `MobileCastleCombatConfig.EconomyEventChance`
- Cooldown: `EconomyEventCooldownWaves`
- Pending event secilmezse `DayNightPrepSystem` gece baslarken expire eder.
- Choice B production bonusu `ProductionBonusResource` ve `ProductionBonusMultiplier` alanlarina yazilir; sonraki DayPrep basinda expire olur.

V1 eventleri geneldir: resource stash, quarry crew, refugee cart. UI metinleri `GameManager` tarafinda verilir.

## Bu Alt Pakette Bilerek Yapilmayanlar

- Mevcut worker'ları target ratio değişince zorla retrain/redistribute etmez.
- Worker world representation actual count'tan ayridir; `WorkerVisualRepresentationUtility` Low/Medium/High egriyle resource basina en fazla `32` temsili visual uretir.
- Event popup/polish bu sistemde degil, `CastleEconomyUI` tarafindadir.

## Doğrulama

- Saf allocation matematiği ve migration: EditMode.
- `MobilePopulationArrivalUtilityTests`: istek, yatak, Food ve int sınırları için saf EditMode bütçe sözleşmesi.
- Yeni population, cap ve idle overflow: gerçek `NewGameScene` PlayMode.
- `WorkerAllocationPlayModeTests.DawnArrivalTransaction_SpendsFoodOnceForAcceptedSurvivors`: Food-limitli kabul, gerçek capacity aynası, iki frame boyunca tek transaction, arrival entity izolasyonu ve varış cleanup doğrulaması.
- `WorkerAllocationPlayModeTests.WorkerDrawer_TargetControlsAndBuildingUpgradesUseBoundRuntimeState`: building cap/efficiency aggregate'inin bu sistemin tukettigi config'e dogru yansimasi.
- `SurvivorArrivalVisualUtilityTests`: visual cap, exact represented survivor toplamı ve deterministik lane/hız/gecikme sözleşmesi.
- Actual worker, target ratio, Food ve persistent growth marker save/Continue: `ExactRunContinuePlayModeTests`.
