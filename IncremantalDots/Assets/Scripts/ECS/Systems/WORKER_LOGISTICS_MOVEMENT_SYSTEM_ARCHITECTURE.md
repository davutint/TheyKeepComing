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
-> LocalTransform + SpriteAnimation
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

Villager spritesheet row duzeni Character Creator standardini kullanir:

```text
Walk: Row 0-7
Idle: Row 24-31
```

Sistem hareket yonunden direction index hesaplar ve `SpriteAnimation.DirectionRow` degerini gunceller.

## Scope

Bu sistem kaynak tick'i, income math, population allocation veya density hesabi yapmaz. Sadece `WorkerVisualRepresentationUtility` tarafindan belirlenen temsili worker visual feedback layer'ini hareket ettirir.
