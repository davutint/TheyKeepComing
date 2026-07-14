# Worker Logistics Movement System - Architecture

`WorkerLogisticsMovementSystem`, Castle Interior ekonomi alanindaki DOTS villager worker entity'lerini kaynak pickup noktalari ile merkezi hub delivery noktalari arasinda hareket ettirir.

## Amac

- Workerlar statik durmaz; kaynak noktasindan merkeze kaynak tasiyor gibi gorunur.
- Ekonomi uretim hesabini degistirmez.
- `MobilePopulationAllocation` halen resource worker sayisinin source-of-truth'udur.

## Component Akisi

```text
MobilePopulationAllocation actual count
-> WorkerVisualRepresentationUtility representative count
-> GameManager worker visual sync
-> ResourceWorkerVisual
-> WorkerLogisticsRoute
-> WorkerLogisticsMovementSystem
-> LocalTransform + SpriteAnimation + WorkerLogisticsFeedbackState
-> DOTS material properties -> Idle/Walk/Work/Celebrate + cargo/lantern/delivery
```

`WorkerLogisticsRoute` sunlari tutar:

- `PickupPosition`: Wood/Stone/Iron/Food site marker pozisyonu.
- `SiteApproachPosition`: worker'in kaynak yapisindan acik koridora ciktigi ara nokta.
- `HubApproachPosition`: ortak koridordan kaleye girdigi ara nokta.
- `DeliveryPosition`: `CastleWorkerHub/DeliveryPoints` marker pozisyonu.
- `MovingToHub`: su an hub'a mi pickup'a mi gittigini belirler.
- `RouteLeg`: iki ara nokta ve endpoint arasindaki aktif segmenti belirler.
- `WorkDuration` ve `DeliveryDuration`: rota ucundaki kisa bekleme sureleri.

Gidis rotasi `pickup -> site approach -> hub approach -> delivery`, donus rotasi bunun
tersidir. Boylece workerlar kaynak binasi ile kale arasinda tek diagonal cizgiyle yapi
sprite'larinin icinden gecmez; acik sag koridorda lane'lere dagilarak yurur.

## Animasyon

Villager asset'i birlesik 32-row atlas degildir. `Villager.mat`, ayni 15x8 griddeki
ayri atlaslari tutar:

```text
Idle.png     -> Idle
Walk.png     -> Walk / Carrying / Returning
Attack1.png  -> Pickup work
Special1.png -> Hub delivery
```

`SpriteAnimation.DirectionRow` her atlas icin yalniz `0-7` direction row'udur.
`WorkerAnimationMaterialProperty`, shader'in hangi atlas tablosunu sample edecegini
per-instance secer. Boylece eski, hatali `24-31` row varsayimi kullanilmaz.

## Allocation-Senkronlu Feedback

- `ResourceWorkerVisual.RepresentedWorkerCount`: temsili entity'nin kac actual worker'i temsil ettigini exact dagilimla tutar.
- Pickup beklemesi `Working`; hub'a gidis `Carrying`; hub beklemesi `Delivering`; donus `Returning` olur.
- Cargo yalniz pickup sonrasi hub'a giderken gorunur ve Wood/Stone/Iron/Food rengine boyanir.
- Hub'a varista kisa delivery pulse ve `Special1` teslimat animasyonu tetiklenir.
- Fener yalniz `Dusk` ve `Night` fazlarinda yanar; `Day` ve `Dawn` fazlarinda kapanir.
- Cargo boyutu ve teslimat feedback siddeti represented worker weight ile sinirli/logaritmik olceklenir.

Bu feedback tek worker shader pass'inde DOTS-instanced property'lerle cizilir. Cargo
ve fener icin ek entity, GameObject veya draw call uretilmez.

## Scope

Bu sistem kaynak tick'i, income math, population allocation veya density hesabi yapmaz. Visual teslimat kaynak eklemez; yalniz `WorkerVisualRepresentationUtility` tarafindan belirlenen temsili worker feedback layer'ini hareket ettirir.
