# Defense Repair - Mimari

## V1 sözleşmesi

Normal Wall repair:

- Yalnız Day ve Dusk phase'lerinde kullanılabilir.
- Tek harcama kaynağı Stone'dur.
- Gercek iyilestirilecek HP'ye gore fiyatlanir ve `ReduceRepairCostPercent` tech çarpanını kullanır.
- Wall `0 HP` olduktan sonra uygulanamaz; aynı-frame lethal damage repair'den üstündür.

## Otorite

`GameManager` bütün repair yollarının tek gameplay owner'ıdır:

- `IsRepairPhaseAvailable()` phase kuralını uygular.
- `CanRepairDefenseFull()` phase, Game Over, Wall HP ve affordability gate'lerini birleştirir.
- `GetRepairCost()` yalnız Stone içeren `ResourceCost` üretir.
- `RepairDefenseFull()` önce gate'i doğrular, Stone transaction'ını yapar ve configured normal
  repair yuzdesini `SingleWallDefenseRules.HealByMaxPercent()` ile uygular.

`DefenseRepairUI`, `CastleEconomyUI`, `MarketUI` ve editor simulator aynı GameManager API'sini kullanır. UI butonları Night/Dawn sırasında interactable değildir; durum metni `Day / Dusk only` gösterir.

Aktif player-facing owner `DefenseRepairUI`'dir. Gercek `DefenseRepairButton` tiklamasi
`GameManager.RepairDefenseFull()` ile basariyla commit edilirse
`NormalRepairCommittedByPlayer` event'i yayilir. Event gameplay transaction'ini tekrar etmez;
first-run onboarding yalniz bu basari sinyalini durable `tutorial.v1.repair` flag'ine cevirir.
Afford edilemeyen tik, programmatic GameManager cagrisi veya Wall hasari event yaymaz.

## Legacy/data notu

Aktif baseline owner zinciri `DifficultyProfileSO -> MobileCastleTuningResolver ->
MobileCastleCombatConfig`'tir. `WallBaseHp`, `NormalRepairHealPercent`,
`RepairStonePerMissingHp`, `RepairDayPriceMultiplier` ve Emergency repair alanlari ayni
profile surface'inde yasar. Profile yoksa yalniz Wall base HP icin
`CastleAuthoring.WallHP` bake degeri fallback olur.

`RepairBaseWoodCost` ve `RepairBaseStoneCost` eski tuning uyumluluğu için serialize kalır
fakat V1 normal repair hesabında okunmaz. Aktif Stone owner'i
`ceil(actualHealHP x RepairStonePerMissingHp x RepairDayPriceMultiplier x discounts)`
formuludur; `SingleWallDefenseRules.CalculateRepairStoneCost` gameplay ve Editor preview
tarafindan ortak kullanilir.

## Doğrulama

- `SingleWallDefenseRulesTests.RepairPhase_AllowsOnlyDayAndDusk`
- `SingleWallDefenseRulesTests.SameFrameLethalDamage_WinsAgainstRepair`
- `SingleWallDefenseRulesTests.RepairStoneCost_UsesActualHealPackage_UnitPriceAndDayMultiplier`
- `ExactRunContinuePlayModeTests.Repair_IsStoneOnly_AndAllowedOnlyDuringDayOrDusk`
- `ExactRunContinuePlayModeTests.WallBaseHpLiveTuning_PreservesHealthRatioAndEffectiveModifiers`
- `WorkerAllocationPlayModeTests.FirstDamagedWallDayRepairOnboarding_PulsesRealRepairAction_AndCompletesOnSuccessfulPlayerRepair`
