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
- `BuildingPopulationSystem.cs` — Kapasite + bina yemek gideri hesaplama
- `PopulationTickSystem.cs` — Idle hesaplama + nufus yemek tuketimi (+=)
- `GameStateAuthoring.cs` — Baker (baslangic degerleri, BaseCapacity)
- `GameManager.cs` — MonoBehaviour tarafi okuma + restart reset
- `HUDController.cs` — HUD gosterimi

## M1.2 Scope
- Workers ve Archers Inspector'dan test degerleri olarak ayarlanir
- Gercek isci atama M1.4+ (bina sistemi)
- Gercek okcu egitimi M1.6+ (kisla sistemi)
