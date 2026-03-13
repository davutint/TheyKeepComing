# BuildingProductionSystem — Editor Kurulumu

## Gereksinimler
Sistem otomatik calisir — ek Editor kurulumu gerekmez.

## Dogrulama
1. Play mode'a gir
2. **Window → Entities → Systems** ac → `BuildingProductionSystem` gorunmeli
3. Calisma sirasi: `BuildingProductionSystem` → `PopulationTickSystem` → `ResourceTickSystem`

## Test Adimlari
1. Play mode → bina yokken HUD'da uretim hizlari 0 olmali
2. Farm yerlestir → Food uretim hizi 4.0/dk olmali (1 isci * 4.0 rate)
3. Ikinci Farm yerlestir → Food uretim hizi 8.0/dk olmali
4. Lumberjack yerlestir → Wood uretim hizi 5.0/dk olmali
5. Restart → tum uretim 0'a donmeli

## SO Asset Degerleri
| Bina | ResourceType | RatePerWorkerPerMin | MaxWorkers |
|------|-------------|--------------------:|----------:|
| Lumberjack | Wood | 5.0 | 3 |
| Quarry | Stone | 3.0 | 3 |
| Mine | Iron | 2.0 | 3 |
| Farm | Food | 4.0 | 3 |

## Bilinen Kisitlamalar
- `AssignedWorkers` su an sabit 1 (M1.7'de isci atama UI gelecek)
- GameStateAuthoring test uretim rate'leri 0'a cekildi — uretim tamamen binalardan gelir
