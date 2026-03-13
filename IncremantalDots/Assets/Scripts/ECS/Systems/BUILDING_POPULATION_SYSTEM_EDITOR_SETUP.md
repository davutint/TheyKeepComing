# BuildingPopulationSystem — Editor Kurulumu

## Gereksinimler
- Otomatik calisir — `[UpdateInGroup(SimulationSystemGroup)]` ile kayitli
- `PopulationState`, `ResourceConsumptionRate`, `GameStateData` singleton'lari mevcut olmali

## Dogrulama
1. **Window → Entities → Systems** ac
2. `SimulationSystemGroup` icinde `BuildingPopulationSystem` gorunmeli
3. Sira: `BuildingProductionSystem` → `BuildingPopulationSystem` → `PopulationTickSystem`

## House SO Degerleri (BuildingConfig_House.asset)

| Alan | Deger |
|------|-------|
| Type | House (4) |
| DisplayName | Ev |
| GridWidth | 3 |
| GridHeight | 3 |
| WoodCost | 20 |
| StoneCost | 10 |
| IronCost | 0 |
| FoodCost | 0 |
| MaxWorkers | 0 |
| PopulationCapacity | 5 |
| FoodCostPerMin | 1.0 |

## Castle Upgrade Degerleri (CastleAuthoring Inspector)

| Alan | Deger |
|------|-------|
| MaxUpgradeLevel | 5 |
| CapacityPerLevel | 10 |
| UpgradeWoodCost | 20 |
| UpgradeStoneCost | 30 |

## CastleUpgradeUI Kurulumu
1. Canvas → yeni obje olustur: `CastleUpgradePanel`
2. Icine `Button` + `TMP_Text` ekle
3. `CastleUpgradeUI` script'ini `CastleUpgradePanel`'a ata
4. `UpgradeButton` ve `ButtonText` referanslarini surukle

## Test Adimlari
1. **Bina yokken:** Play mode → HUD'da kapasite 20, tuketim 0
2. **Ev yerlestir:** Capacity 20 → 25, Food tuketim -1.0/dk
3. **Ikinci Ev:** Capacity 25 → 30, Food tuketim -2.0/dk
4. **Kale yukselt (1 kez):** Capacity +10, 20 Ahsap + 30 Tas dusmeli
5. **Kale yukselt (max):** 5. seviyeden sonra buton devre disi
6. **Restart:** Capacity 20'ye donmeli, kale seviyesi 0, tuketim 0
