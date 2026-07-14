# Mobile Economy Price Tuning - Mimari

## Otorite

Ekonomi fiyatlarının içerik kaynağı `DifficultyProfileSO` ve aktif asset olarak
`Assets/ScriptableObject/MobileCastle/Difficulty/DefaultDifficulty.asset` dosyasıdır.
`MobileCastleTuningResolver.ResolveEconomyPriceTuning` bu alanları sanitize eder;
`MobileCastleCombatAuthoring.Baker` sonucu `MobileEconomyPriceTuning` component'i olarak
config entity'sine yazar. Runtime utility'leri hardcoded maliyet kullanmaz.

## Alanlar

- House bed: `BedBaseWoodCost`, `BedCostGrowthCapacityInterval`
- Worker CAP: `WorkerCapacityBaseWoodCost`, `WorkerCapacityBaseIronCost`
- Worker EFF: `WorkerEfficiencyBaseWoodCost`, `WorkerEfficiencyBaseIronCost`
- Ortak bina eğrisi: `WorkerBuildingCostGrowthMultiplier`

Onaylı V1 default'ları sırasıyla `100`, `25`, `100/25`, `150/50`, `1.35` değerleridir.

## Formüller

- Bed unit cost:
  `ceil(BedBaseWoodCost * (1 + ownedGrowth / BedCostGrowthCapacityInterval)^2)`
- Worker building next cost, her kaynak için ayrı:
  `ceil(baseResourceCost * WorkerBuildingCostGrowthMultiplier^currentLevel)`

Bed toplu alımı her yatağın ardışık unit maliyetini toplar. Worker CAP ve EFF seviyeleri
her bina için bağımsızdır; her iki fiyat da Wood ve Iron birlikte gerektirir.

## Int güvenliği

`MobileEconomyPriceTuningUtility.Sanitize` bütün base maliyetleri ve bed interval'ını en az
`1`, bina büyüme çarpanını en az `1` yapar; NaN/Infinity çarpanını V1 default'una döndürür.
Bed hesabı `decimal` kullanır. Worker exponential hesabı NaN/Infinity ve `int.MaxValue`
üstünü reddeder. Toplu transaction toplamı `int` ile temsil edilemiyorsa satın alım yapılmaz.
Gameplay hard max eklenmez; sınır yalnız temsil ve transaction güvenliğidir.

## Runtime ve save

`GameManager.GetEconomyPriceTuning` bake edilmiş component'i okur ve savunma amaçlı yeniden
sanitize eder. Bed ve worker bina cost API'leri aynı snapshot'ı ilgili utility'ye geçirir.
Tuning içerik verisidir, run-save state'i değildir. Exact save v6 satın alınmış yatak ve sekiz
bina seviyesini taşır; Continue sonrası maliyet mevcut profile baseline'ından yeniden hesaplanır.

## Doğrulama

- `MobileCastleTuningResolverTests.EconomyPriceTuning_UsesProfileValuesAndSanitizesInvalidInputs`
- `MobileBedCapacityUtilityTests.PurchaseWoodCost_UsesSanitizedProfileTuningAndRejectsExtremeCurveOverflow`
- `MobileWorkerBuildingUpgradeUtilityTests.CostCurve_UsesSanitizedProfileTuningForBothResources`
- `ExactRunContinuePlayModeTests.RuntimeTuning_UsesProfileDifficulty_AndAuthoringCycleDurations`
- `ExactRunContinuePlayModeTests.EconomyPriceTuning_RuntimePurchaseApisReadBakedComponent`
