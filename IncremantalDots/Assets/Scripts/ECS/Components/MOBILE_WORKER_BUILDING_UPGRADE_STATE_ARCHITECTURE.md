# Mobile Worker Building Upgrade State - Architecture

## Ürün sözleşmesi

Wood, Stone, Iron ve Food worker binaları haritada hazır bulunur; oyuncu bina yerleştirmez. Her bina run içinde iki bağımsız ekonomik yatırıma sahiptir:

- Capacity: seviye başına `+10` worker slotu.
- Efficiency: seviye başına baz kişi üretimine additive `+10%`.

Castle Heart teknoloji etkileri ayrı bir katmandır. Doğrudan bina yatırımı Heart seviyesini değiştirmez; Heart, Council, Meta ve bina etkileri aynı config aggregate hesabında birleşir.

## State

`MobileWorkerBuildingUpgradeState`, `MobileCastleCombatConfig` singleton entity'si üzerinde sekiz run-scoped seviye tutar:

- Wood Capacity / Efficiency
- Stone Capacity / Efficiency
- Iron Capacity / Efficiency
- Food Capacity / Efficiency

Kaynak ve yatırım türlerinin seviyeleri birbirinden bağımsızdır. `RestartGame()` sekiz alanı sıfırlar. Exact Continue bu state'i `RunSaveState v6` içinden geri kurar.

## Maliyet eğrisi

`MobileWorkerBuildingUpgradeUtility`, bir sonraki alım maliyetini mevcut seviyeden hesaplar:

```text
Capacity:   ceil(100 Wood × 1.35^level) + ceil(25 Iron × 1.35^level)
Efficiency: ceil(150 Wood × 1.35^level) + ceil(50 Iron × 1.35^level)
```

Her alım hem Wood hem Iron harcar. `GameManager.TryBuyWorkerBuildingUpgrade()` iki kaynağı tek transaction olarak doğrular ve harcar; kaynak yetmezse seviye değişmez.

Gameplay hard max yoktur. `Math.Pow` sonucu veya herhangi bir maliyet `int` ile temsil edilemiyorsa alım güvenli biçimde reddedilir. Bu sınır tasarım seviyesi değildir; sayısal taşma korumasıdır.

## Runtime aggregate

`GameManager.ApplyTechEconomyAggregates()` her değişimde config'i baz değerlerden yeniden kurar:

```text
Worker Cap = base + Heart cap + Council cap + Building Capacity
Per-worker production = base × (1 + Heart% + Meta% + Building Efficiency%)
```

Capacity toplamı `int.MaxValue` sınırında saturate edilir. Efficiency bonusu additive yüzdedir; önceki sonucu tekrar çarparak compound üretmez. `MobilePopulationEconomySystem` bu config'i tüketip etkin cap aynalarını ve `worker count × per-worker production` oranlarını yazar.

## Save sözleşmesi

Schema `v6`, sekiz seviyeyi açık alanlar olarak saklar. `v3 -> v4 -> v5 -> v6`, `v4 -> v5 -> v6` ve `v5 -> v6` migration zincirlerinde eski koşular sıfır bina yatırımıyla devam eder. Restore state'i Heart/Council aggregate hesabından önce yazar; böylece Continue sonunda bütün katmanlar tek seferde doğru birleşir.

## Doğrulama

- `MobileWorkerBuildingUpgradeUtilityTests`: başlangıç maliyetleri, `1.35^level` eğrisi, bağımsız seviyeler, additive etkiler ve taşma reddi.
- `WorkerAllocationPlayModeTests.WorkerDrawer_TargetControlsAndBuildingUpgradesUseBoundRuntimeState`: HUD binding, iki kaynak harcaması, cap ve üretim etkisi.
- `ExactRunContinuePlayModeTests.WorkerBuildingInvestments_SpendBothResourcesAndPersistAcrossExactContinue`: transaction, sonraki fiyat ve exact Continue.
- `RunPersistenceTests`: v6 round-trip ve v3/v4/v5 migration.
