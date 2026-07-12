# BuildingPopulationSystem - Mimari

## Sorumluluk

`PopulationProvider` kapasitesini ve `CastleUpgradeData` kapasite bonusunu `PopulationState.Capacity` alanına yansıtır. Game Over sırasında çalışmaz.

## V1 upkeep sınırı

Dead Walls V1 castle loop'unda pasif ana kaynak tüketimi yoktur. `MobileCastleCombatConfig` bulunduğunda sistem her frame `ResourceConsumptionRate` içindeki Wood, Stone, Iron ve Food değerlerini sıfırlar.

`BuildingFoodCost` yalnız legacy sahnelerde `FoodPerMin` üretir. V1 scene'de component serialize kalsa bile kaynak azaltamaz.

## Akış

1. Population provider kapasitelerini topla.
2. Legacy building food rate'lerini topla.
3. Castle upgrade capacity bonusunu hesapla.
4. `PopulationState.Capacity` yaz.
5. Consumption rate'lerini sıfırla; yalnız legacy modda building Food rate'ini geri yaz.

`ResourceTickSystem` ayrıca V1 consumption değerlerini sıfır kabul eder. Böylece yanlışlıkla rate yazan gelecekteki bir sistem ana stokları pasif azaltamaz.
