# PopulationState Component — Mimari

## Genel Bakis
`PopulationState` singleton component, GDD v3.0 Bolum 6'daki "Tek Havuz" nufus modelini temsil eder. Tum insanlar tek bir havuzda tutulur ve isci, okcu veya bos olarak siniflandirilir.

## Veri Yapisi

| Alan | Tip | Aciklama |
|------|-----|----------|
| Total | int | Toplam nufus |
| Workers | int | Binalara atanmis isci sayisi |
| Archers | int | Egitilmis okcu sayisi |
| Idle | int | Hesaplanan: Total - Workers - Archers (>=0) |
| Capacity | int | Mobile castle loop'ta sahip olunan toplam House yatak sayısı; legacy/non-mobile akışta bina + kale kapasitesi |
| BaseCapacity | int | Mobile castle loop'ta run başlangıç yatak sayısı; legacy/non-mobile akışta bina/upgrade öncesi temel kapasite |
| FoodPerAssignedPerMin | float | Atanmis kisi basina yemek tuketimi (dk basina) |

## Singleton Pattern
GameState entity uzerinde tutulur (GameStateAuthoring baker'i ekler). `SystemAPI.GetSingletonRW<PopulationState>()` ile erisim.

## Mobile House Bed State

V1 castle loop'taki satın alınabilir yatak gerçeği `MobileBedCapacityState` içinde, `MobileCastleCombatConfig` entity'si üzerinde run-scoped tutulur:

| Alan | Tip | Açıklama |
|---|---|---|
| BaseCapacity | int | Run başlangıcındaki yatak sayısı; aktif authoring varsayılanı `60` |
| PurchasedCapacity | int | Bu run içinde Wood ödenerek alınmış ek yatak sayısı |

Toplam yatak `BaseCapacity + PurchasedCapacity` olarak `MobileBedCapacityUtility` tarafından overflow-safe hesaplanır. Gameplay hard max yoktur; yalnız `int.MaxValue` teknik taşma sınırı uygulanır.

`GameManager.TryBuyBedCapacity` bu state'i anlık satın alım transaction'ıyla büyütür. Sonraki yatağın Wood maliyeti owner onaylı `ceil(100 × (1 + max(0, ToplamYatak - 60) / 25)^2)` eğrisidir. Varsayılan `60` yatakta fiyat `100`, `160` yatakta `2.500`, `360` yatakta `16.900`, `810` yatakta `96.100` Wood olur. Toplu alım mevcut birim fiyatı adetle çarpmaz; her ek yatağın ardışık fiyatını toplar. Gameplay hard max yoktur; temsil edilemeyen `int` transaction taşırılmadan reddedilir. Eğri katsayılarının Inspector/SO tuning yüzeyine taşınması ayrı tracker işidir.

Bed state exact save `v6` içinde `BedBaseCapacity` ve `PurchasedBedCapacity` olarak saklanır. `v3/v4` kayıtları mevcut nüfusu geçersiz kılmayacak bir base bed değeriyle migrate edilir; v5 kayıtları sıfır worker-building yatırımıyla v6'ya yükseltilir.

`MobilePopulationEconomySystem`, mobile castle loop'ta `PopulationState.BaseCapacity` ve `PopulationState.Capacity` aynalarını her frame bu bed state'ten senkronlar. Mobile authoring ve restart tabanı `60` yataktır; eski `999999` mobile kapasite aynası kaldırılmıştır.

## Dawn Arrival Budget

Her tamamlanan cycle için istenen survivor sayısı aşağıdaki saf bütçeyle sınırlandırılır:

```text
accepted = min(requestedDawnCount, totalBeds - currentPopulation, Food / FoodCostPerArrival)
```

- Varsayılan `requestedDawnCount = 15`.
- Owner onaylı V1 değeri `FoodCostPerArrival = 1`.
- Food yetersizse mevcut nüfus azalmaz; yalnız yeni arrival sayısı düşer.
- `MobilePopulationAllocation`, son istenen/kabul edilen sayıyı ve kabul edilenler için gereken Food tutarını saklar.
- Kabul edilen population artışıyla aynı transaction içinde `RequiredFood`, `ResourceData.Food` stokundan yalnız bir kez düşülür.
- `LastPopulationGrowthCycle/LastPopulationGrowthWave` marker'ları aynı Dawn veya Continue sonrasında transaction'ın tekrar uygulanmasını engeller.

## Nufus Modeli
```
Tum insanlar = TEK HAVUZ
  ├─ Workers: Kaynak binalarina atanir (M1.4+)
  ├─ Archers: Kislada egitilir (M1.6+)
  └─ Idle: Atanmamis, yemek tuketmez
```

## V1 Food Sözleşmesi

V1 castle loop'ta population pasif Food tüketmez; açlık, göç, population death ve üretim cezası yoktur. Food yalnız yeni Dawn survivor'ı kabul edilirken kişi başına bir kez düşülür. Legacy/non-mobile alan ve bina tüketim verileri uyumluluk için component'larda kalır, fakat mobile population sistemi bunları pasif nüfus giderine çevirmez.

## Kapasite Hesaplama

Mobile castle loop'ta `MobilePopulationEconomySystem`, `BuildingPopulationSystem` sonrasında çalışır ve son kapasite owner'ı olarak bed state'i aynalar:

```text
Capacity = MobileBedCapacityState.BaseCapacity + PurchasedCapacity
```

Legacy/non-mobile akışta `BuildingPopulationSystem` mevcut hesabını korur:

```
Capacity = BaseCapacity + evlerdenGelen + kaleUpgradeBonus
```
- `BaseCapacity`: Legacy başlangıç değeri
- `evlerdenGelen`: Tum PopulationProvider entity'lerinin CapacityAmount toplami
- `kaleUpgradeBonus`: CastleUpgradeData.Level * CapacityPerLevel

## Tradeoff
Daha cok okcu = Daha az isci = Daha az kaynak uretimi (ve daha fazla yemek tuketimi)

## Iliskili Dosyalar
- `PopulationComponents.cs` — Component tanimi
- `MobileBedCapacityUtility.cs` — Toplam/purchase increment, owned-capacity maliyet eğrisi, ardışık toplu fiyat ve int güvenlik kuralları
- `MobilePopulationArrivalUtility.cs` — Dawn istek, boş yatak ve Food bütçesinden kabul edilen survivor sayısını hesaplayan saf sözleşme
- `MobileCastleCombatAuthoring.cs` — Mobile başlangıç yatak state'i bake'i
- `BuildingPopulationSystem.cs` — Kapasite + bina yemek gideri hesaplama
- `PopulationTickSystem.cs` — Idle hesaplama + nufus yemek tuketimi (+=)
- `GameStateAuthoring.cs` — Baker (baslangic degerleri, BaseCapacity)
- `GameManager.cs` — MonoBehaviour tarafi okuma + restart reset
- `RunPersistence.cs` — v6 bed + worker-building state capture/restore ve v3/v4/v5 migration
- `HUDController.cs` — HUD gosterimi

## M1.2 Scope
- Workers ve Archers Inspector'dan test degerleri olarak ayarlanir
- Gercek isci atama M1.4+ (bina sistemi)
- Gercek okcu egitimi M1.6+ (kisla sistemi)
