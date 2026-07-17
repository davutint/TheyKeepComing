# Worker Visual Representation Architecture

## Otorite

`MobilePopulationAllocation` gameplay worker sayisinin tek truth kaynagidir.
`WorkerVisualRepresentationUtility` bu sayiyi yalnizca dunya feedback'i icin
temsili DOTS entity sayisina cevirir. Visual sayi save edilmez, production
hesabina girmez ve population/worker limitlerini degistirmez.

## Density Eğrisi

Eğri resource basina uygulanir:

| Seviye | Actual worker | Temsili visual |
|---|---:|---:|
| None | `0` | `0` |
| Low | `1-12` | `1:1` |
| Medium | `13-60` | `12 + ceil((actual - 12) / 4)`; en fazla `24` |
| High | `61+` | `24 + ceil((actual - 60) / 20)`; hard visual cap `32` |

Dort resource birlikte en fazla `128` hareketli worker visual uretebilir.
Baslangic actual dagilimi `20 / 10 / 8 / 15`, dunya temsiline
`14 / 10 / 8 / 13` yani toplam `45` entity olarak yansir.

## Runtime Akışı

```text
MobilePopulationAllocation actual counts
-> WorkerVisualRepresentationUtility.GetRepresentativeCounts()
-> GameManager representative count + actual count cache
-> Temsil sayisi degistiyse worker visual spawn/destroy sync
-> Ayni bucket icindeki actual degisiminde yalniz representation weight sync
-> ResourceWorkerVisual.RepresentedWorkerCount + WorkerLogisticsRoute
-> WorkerLogisticsMovementSystem
```

Actual worker sayisi ayni temsili bucket icinde artsa bile gameplay ve save truth
hemen guncellenir; visual'lara dagitilan weight toplami actual sayiya exact esitlenir,
gereksiz visual spawn/destroy veya route rewrite yapilmaz.
Temsil sayisi degistiginde mevcut entity'ler deterministik index sirasiyla korunur,
fazlalik silinir ve eksik visual `VillagerWorker.prefab` uzerinden tamamlanir.

`RepresentedWorkerCount`, yalniz dunya feedback siddetini olceklendirir. Kaynak
uretimi `MobilePopulationAllocation` ve production rate sistemlerinde kalir; visual
teslimatlar kaynak eklemez ve save edilmez.

## Doğrulama

- `WorkerVisualRepresentationUtilityTests`: tier sinirlari, monotonic davranis,
  resource basi `32` cap, exact representation weight toplami, feedback siddeti,
  Dusk/Night lantern kurali ve baslangic dagilimi.
- `WorkerAllocationPlayModeTests`: gercek `NewGameScene` icinde
  `12 -> 60 -> 1000 -> 5000 -> 0` actual gecisinde
  `12 -> 24 -> 32 -> 32 -> 0` visual contract'i; ayni bucket icinde `101 -> 119`
  actual weight sync'i; pickup cargo/work, hub delivery pulse ve Night lantern state'i.
  Birlesik release guard dort resource'u ayni anda `12 / 60 / 101 / 1000` actual ve
  `12 / 24 / 27 / 32` temsili sayiyla kurar; exact weight, unique index, route,
  cargo rengi, feedback siddeti, Night lantern ve allocation-truth korunmasini
  butun `95` visual entity uzerinde dogrular.
- Game View QA: baslangic state'inde `53` actual worker icin `45` okunabilir visual;
  Night'ta worker olceginde kucuk resource cargo ve sicak lantern noktasi.

Eşikler ürün tuning'i degistiginde yalnız bu utility sabitlerinden ayarlanir;
allocation, save veya worker production contract'i degistirilmez.
