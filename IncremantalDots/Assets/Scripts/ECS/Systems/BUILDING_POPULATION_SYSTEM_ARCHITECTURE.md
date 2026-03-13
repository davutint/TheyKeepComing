# BuildingPopulationSystem — Mimari Dokumani

## Amac
Ev binalarinin nufus kapasitesini ve yemek giderini, kale yukseltmenin kapasite bonusunu ECS singleton'larina yansitir.

## Dosya
`Assets/Scripts/ECS/Systems/BuildingPopulationSystem.cs`

## Sistem Sirasi
```
-2. BuildingProductionSystem    — Kaynak uretim hizlari + Workers
-1½.BuildingPopulationSystem   — Kapasite + bina yemek tuketimi (BU SISTEM)
-1. PopulationTickSystem        — Idle hesapla + nufus yemek tuketimi (+=)
 0. ResourceTickSystem          — Net hiz uygula
```

## OnUpdate Mantigi
```
1. GameOver kontrolu → return
2. Tum PopulationProvider entity'lerini tara → totalCapacity topla
3. Tum BuildingFoodCost entity'lerini tara → totalBuildingFoodCost topla
4. CastleUpgradeData oku (TryGetSingleton) → castleBonus = Level * CapacityPerLevel
5. PopulationState.Capacity = BaseCapacity + totalCapacity + castleBonus
6. ResourceConsumptionRate sifirla ve bina yemek giderini yaz:
   - WoodPerMin = 0f
   - StonePerMin = 0f
   - IronPerMin = 0f
   - FoodPerMin = totalBuildingFoodCost (SADECE bina kismi)
```

**Neden consumption overwrite:** PopulationTickSystem sonra calisir ve `FoodPerMin += nufus kismi` ekler. Her frame sifirdan hesaplanir, birikmez.

## Performans
- Main thread, SystemAPI.Query — bina sayisi 10-50 max
- `[BurstCompile]` struct + OnUpdate — tam Burst uyumlu
- CastleUpgradeData: `SystemAPI.TryGetSingleton` (castle entity singleton)
- Sync point YOK, structural change YOK, ECB YOK

## Bagimliliklar
**Okur:**
- `PopulationProvider` — ev binalarinin kapasite degeri
- `BuildingFoodCost` — ev binalarinin yemek gideri
- `CastleUpgradeData` — kale yukseltme seviyesi + kapasite/seviye

**Yazar:**
- `PopulationState.Capacity` — BaseCapacity + evler + kale bonusu
- `ResourceConsumptionRate` — bina yemek giderini FoodPerMin'e yazar

## Iliskili Dosyalar
- `BuildingComponents.cs` — PopulationProvider, BuildingFoodCost component'lari
- `CastleComponents.cs` — CastleUpgradeData component
- `PopulationComponents.cs` — PopulationState (BaseCapacity, Capacity)
- `PopulationTickSystem.cs` — FoodPerMin += nufus kismi
- `BuildingProductionSystem.cs` — Workers + kaynak uretim hizi (onceki sistem)
