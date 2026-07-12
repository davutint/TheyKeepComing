# Defense Repair - Mimari

## V1 sözleşmesi

Normal Wall repair:

- Yalnız Day ve Dusk phase'lerinde kullanılabilir.
- Tek harcama kaynağı Stone'dur.
- Eksik HP oranına göre ölçeklenir ve `ReduceRepairCostPercent` tech çarpanını kullanır.
- Wall `0 HP` olduktan sonra uygulanamaz; aynı-frame lethal damage repair'den üstündür.

## Otorite

`GameManager` bütün repair yollarının tek gameplay owner'ıdır:

- `IsRepairPhaseAvailable()` phase kuralını uygular.
- `CanRepairDefenseFull()` phase, Game Over, Wall HP ve affordability gate'lerini birleştirir.
- `GetRepairCost()` yalnız Stone içeren `ResourceCost` üretir.
- `RepairDefenseFull()` önce gate'i doğrular, Stone transaction'ını yapar ve `SingleWallDefenseRules.RepairToFull()` çağırır.

`DefenseRepairUI`, `CastleEconomyUI`, `MarketUI` ve editor simulator aynı GameManager API'sini kullanır. UI butonları Night/Dawn sırasında interactable değildir; durum metni `Day / Dusk only` gösterir.

## Legacy/data notu

`MobileCastleCombatConfig.RepairBaseWoodCost` ve Difficulty Profile'daki eş alan eski tuning uyumluluğu için serialize kalabilir fakat V1 normal repair hesabında okunmaz. Aktif maliyet owner'ı `RepairBaseStoneCost` alanıdır.

## Doğrulama

- `SingleWallDefenseRulesTests.RepairPhase_AllowsOnlyDayAndDusk`
- `SingleWallDefenseRulesTests.SameFrameLethalDamage_WinsAgainstRepair`
- `ExactRunContinuePlayModeTests.Repair_IsStoneOnly_AndAllowedOnlyDuringDayOrDusk`
