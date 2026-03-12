# PopulationTickSystem — Mimari

## Genel Bakis
`PopulationTickSystem` nufus hesaplamasini yonetir ve yemek tuketim hizini gunceller. ResourceTickSystem'den **ONCE** calisir — boylece yemek tuketimi her zaman guncel degerle hesaplanir.

## Sistem Detaylari

| Ozellik | Deger |
|---------|-------|
| Tip | `partial struct : ISystem` |
| Burst | `[BurstCompile]` (struct + OnUpdate) |
| Grup | `SimulationSystemGroup` |
| Siralama | `[UpdateBefore(typeof(ResourceTickSystem))]` |
| Require | `PopulationState`, `GameStateData` |

## Frame Akisi
```
PopulationTickSystem:
  1. GameOver kontrolu → early return
  2. PopulationState oku
  3. Idle = max(0, Total - Workers - Archers)
  4. assigned = Workers + Archers
  5. ResourceConsumptionRate.FoodPerMin = assigned * FoodPerAssignedPerMin
  6. PopulationState.Idle geri yaz

ResourceTickSystem (sonra calisir):
  → Guncel FoodPerMin ile yemek tuketimi hesaplar
```

## Neden ResourceTickSystem'den Once?
Eger sonra calissaydi, ResourceTickSystem eski frame'in FoodPerMin degerini kullanirdi (1 frame gecikme). Bu kucuk bir fark gibi gorunse de, hizli nufus degisimlerinde tuketim hesabi yanlis olurdu.

## Onemli Notlar
- **Sadece FoodPerMin'i yazar** — Wood/Stone/Iron consumption'a dokunmaz
- Idle negatif olamaz (`math.max(0, ...)` ile clamp)
- Workers + Archers > Total durumunda Idle = 0 (kapasite asimi durumu)
- GameOver'da calismayi durdurur

## Sistem Sirasi (Guncel)
```
SimulationSystemGroup:
-1. PopulationTickSystem          — Nufus hesapla + FoodPerMin guncelle
 0. ResourceTickSystem            — Kaynak uretim/tuketim (guncel FoodPerMin ile)
 1. WaveSpawnSystem (OrderFirst)
 2. ArcherShootSystem
 ... (diger sistemler degismedi)
```

## Iliskili Dosyalar
- `PopulationComponents.cs` — PopulationState struct
- `ResourceComponents.cs` — ResourceConsumptionRate (FoodPerMin yazilir)
- `ResourceTickSystem.cs` — Yemek tuketimini hesaplar (bu sistemden sonra)
