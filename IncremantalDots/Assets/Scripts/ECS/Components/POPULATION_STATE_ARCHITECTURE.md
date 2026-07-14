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
| Capacity | int | Maksimum nufus kapasitesi (BaseCapacity + evler + kale bonusu) |
| BaseCapacity | int | Bina/upgrade olmadan temel kapasite (bake: 20) |
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

Bed state exact save `v5` içinde `BedBaseCapacity` ve `PurchasedBedCapacity` olarak saklanır. `v3/v4` kayıtları mevcut nüfusu geçersiz kılmayacak bir base bed değeriyle migrate edilir.

Bu state henüz Dawn arrival ve `PopulationState.Capacity` hesabının aktif owner'ı değildir. Bed boşluğu + tek seferlik Food kabul bütçesi ayrı tracker maddesinde bağlanacaktır; bu nedenle legacy mobile `999999` kapasite aynası bu pakette bilerek kaldırılmamıştır.

## Nufus Modeli
```
Tum insanlar = TEK HAVUZ
  ├─ Workers: Kaynak binalarina atanir (M1.4+)
  ├─ Archers: Kislada egitilir (M1.6+)
  └─ Idle: Atanmamis, yemek tuketmez
```

## Yemek Tuketimi Entegrasyonu
Toplam yemek tuketimi = bina gideri + nufus gideri. Iki asamali hesaplanir:
1. `BuildingPopulationSystem`: `FoodPerMin = toplam bina yemek gideri` (Ev'lerin FoodCostPerMin toplami)
2. `PopulationTickSystem`: `FoodPerMin += assigned * FoodPerAssignedPerMin` (nufus kismi eklenir)

- `assigned = Workers + Archers`
- Idle bireyler yemek **tuketmez**
- ResourceTickSystem guncel FoodPerMin ile tuketim hesaplar

## Kapasite Hesaplama
`BuildingPopulationSystem` her frame hesaplar:
```
Capacity = BaseCapacity + evlerdenGelen + kaleUpgradeBonus
```
- `BaseCapacity`: Baslangic degeri (20)
- `evlerdenGelen`: Tum PopulationProvider entity'lerinin CapacityAmount toplami
- `kaleUpgradeBonus`: CastleUpgradeData.Level * CapacityPerLevel

## Tradeoff
Daha cok okcu = Daha az isci = Daha az kaynak uretimi (ve daha fazla yemek tuketimi)

## Iliskili Dosyalar
- `PopulationComponents.cs` — Component tanimi
- `MobileBedCapacityUtility.cs` — Toplam/purchase increment, owned-capacity maliyet eğrisi, ardışık toplu fiyat ve int güvenlik kuralları
- `MobileCastleCombatAuthoring.cs` — Mobile başlangıç yatak state'i bake'i
- `BuildingPopulationSystem.cs` — Kapasite + bina yemek gideri hesaplama
- `PopulationTickSystem.cs` — Idle hesaplama + nufus yemek tuketimi (+=)
- `GameStateAuthoring.cs` — Baker (baslangic degerleri, BaseCapacity)
- `GameManager.cs` — MonoBehaviour tarafi okuma + restart reset
- `RunPersistence.cs` — v5 bed state capture/restore ve v3/v4 migration
- `HUDController.cs` — HUD gosterimi

## M1.2 Scope
- Workers ve Archers Inspector'dan test degerleri olarak ayarlanir
- Gercek isci atama M1.4+ (bina sistemi)
- Gercek okcu egitimi M1.6+ (kisla sistemi)
