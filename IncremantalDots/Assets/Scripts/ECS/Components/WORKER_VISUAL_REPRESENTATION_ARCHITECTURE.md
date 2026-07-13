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
-> GameManager representative int4 cache
-> Yalniz temsil sayisi degistiyse worker visual sync
-> ResourceWorkerVisual + WorkerLogisticsRoute
-> WorkerLogisticsMovementSystem
```

Actual worker sayisi ayni temsili bucket icinde artsa bile gameplay ve save truth
hemen guncellenir; gereksiz visual spawn/destroy veya route rewrite yapilmaz.
Temsil sayisi degistiginde mevcut entity'ler deterministik index sirasiyla korunur,
fazlalik silinir ve eksik visual `VillagerWorker.prefab` uzerinden tamamlanir.

## Doğrulama

- `WorkerVisualRepresentationUtilityTests`: tier sinirlari, monotonic davranis,
  resource basi `32` cap ve baslangic dagilimi.
- `WorkerAllocationPlayModeTests`: gercek `NewGameScene` icinde
  `12 -> 60 -> 1000 -> 5000 -> 0` actual gecisinde
  `12 -> 24 -> 32 -> 32 -> 0` visual contract'i.
- Game View QA: baslangic state'inde `53` actual worker icin `45` okunabilir visual.

Eşikler ürün tuning'i degistiginde yalnız bu utility sabitlerinden ayarlanir;
allocation, save veya worker production contract'i degistirilmez.
