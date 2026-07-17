# Mobile Economy Runtime Tuning - Mimari

## Otorite

Worker economy tuning ve komsu bed/Arrow fiyatlarinin içerik kaynağı `DifficultyProfileSO` ve aktif asset olarak
`Assets/ScriptableObject/MobileCastle/Difficulty/DefaultDifficulty.asset` dosyasıdır.
`MobileCastleTuningResolver.ResolveEconomyPriceTuning` bu alanları sanitize eder;
`MobileCastleCombatAuthoring.Baker` sonucu `MobileEconomyPriceTuning` component'i ve worker
production alanlari olarak config entity'sine yazar. Runtime utility'leri hardcoded maliyet veya
EFF effect yuzdesi kullanmaz. Serialized teknik component adi compatibility icin korunur.

## Alanlar

- Worker production baseline: `Wood/Stone/Iron/FoodWorkerProductionPerMin`
- House bed: `BedBaseWoodCost`, `BedCostGrowthCapacityInterval`
- Worker CAP: `WorkerCapacityBaseWoodCost`, `WorkerCapacityBaseIronCost`
- Worker EFF: `WorkerEfficiencyBaseWoodCost`, `WorkerEfficiencyBaseIronCost`
- Ortak bina eğrisi: `WorkerBuildingCostGrowthMultiplier`
- Worker EFF etkisi: `WorkerEfficiencyPercentPerLevel`
- Arrow stok: `ArrowBaseCapacity`, `ArrowCapacityPerLevel`, `ArrowRefillPackageSize`
- Arrow verim: `ArrowBaseArrowsPerWood`, `ArrowArrowsPerWoodPerEfficiencyLevel`
- Arrow CAP/EFF: Wood + Iron base maliyetleri ve `ArrowUpgradeCostGrowthMultiplier`

Worker production V1 default'lari `8 / 5.5 / 4.9 / 7`; worker/bed fiyat ve effect
default'lari `100`, `25`, `100/25`, `150/50`, `1.35`, `%10`;
Arrow default'ları `200`, `+200`, `100`, `4/Wood`, `+1/Wood`, CAP `150W+25I`,
EFF `200W+50I`, growth `1.35` değerleridir.

## Formüller

- Bed unit cost:
  `ceil(BedBaseWoodCost * (1 + ownedGrowth / BedCostGrowthCapacityInterval)^2)`
- Worker building next cost, her kaynak için ayrı:
  `ceil(baseResourceCost * WorkerBuildingCostGrowthMultiplier^currentLevel)`
- Worker per-worker production:
  `profile base x (1 + tech% + meta% + building level x WorkerEfficiencyPercentPerLevel) x Heart ratio`
- Arrow refill: `ceil(sığan Arrow / mevcut ArrowPerWood)`; purchase count maliyeti büyütmez.
- Arrow upgrade next cost: `ceil(baseResourceCost * ArrowUpgradeCostGrowthMultiplier^currentLevel)`.

Bed toplu alımı her yatağın ardışık unit maliyetini toplar. Worker CAP ve EFF seviyeleri
her bina için bağımsızdır; her iki fiyat da Wood ve Iron birlikte gerektirir.

## Int güvenliği

`MobileEconomyPriceTuningUtility.Sanitize` bütün base maliyetleri ve bed interval'ını en az
`1`, bina büyüme çarpanını en az `1` yapar; NaN/Infinity çarpanını V1 default'una döndürür.
EFF yuzdesi sonlu ve pozitif degilse onayli `%10` default'una doner.
Bed hesabı `decimal` kullanır. Worker exponential hesabı NaN/Infinity ve `int.MaxValue`
üstünü reddeder. Toplu transaction toplamı `int` ile temsil edilemiyorsa satın alım yapılmaz.
Gameplay hard max eklenmez; sınır yalnız temsil ve transaction güvenliğidir.

## Runtime ve save

`GameManager.GetEconomyPriceTuning` bake edilmiş component'i okur ve savunma amaçlı yeniden
sanitize eder. Bed ve worker bina cost API'leri aynı snapshot'ı ilgili utility'ye geçirir.
`ApplyTechEconomyAggregates`, ayni snapshot'taki EFF yuzdesini bina seviyeleriyle birlestirir.
Difficulty Tuner live Apply, `ApplyWorkerEconomyTuning` ile dort base rate'i yeniden kurar;
tech/meta/Heart/bina aggregate'i yeni baseline uzerine tekrar uygulanir.
Tuning içerik verisidir, run-save state'i değildir. Güncel exact save v8 satın alınmış yatak,
sekiz bina seviyesi ve iki Arrow yatırım seviyesini taşır; Continue sonrası maliyet mevcut
profile baseline'ından yeniden hesaplanır.

## Doğrulama

- `MobileCastleTuningResolverTests.EconomyPriceTuning_UsesProfileValuesAndSanitizesInvalidInputs`
- `MobileBedCapacityUtilityTests.PurchaseWoodCost_UsesSanitizedProfileTuningAndRejectsExtremeCurveOverflow`
- `MobileWorkerBuildingUpgradeUtilityTests.CostCurve_UsesSanitizedProfileTuningForBothResources`
- `MobileWorkerBuildingUpgradeUtilityTests.Effects_AreAdditiveAndUnrepresentableCostIsRejectedSafely`
- `ExactRunContinuePlayModeTests.RuntimeTuning_UsesProfileDifficulty_AndAuthoringCycleDurations`
- `ExactRunContinuePlayModeTests.EconomyPriceTuning_RuntimePurchaseApisReadBakedComponent`
- `ExactRunContinuePlayModeTests.WorkerEconomyBaseRateLiveTuning_PreservesEffectiveAggregateRatios`
