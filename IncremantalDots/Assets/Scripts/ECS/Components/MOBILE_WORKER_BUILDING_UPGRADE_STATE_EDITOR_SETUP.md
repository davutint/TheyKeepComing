# Mobile Worker Building Upgrade State - Editor Setup

## ECS kurulumu

Ek bir scene objesi gerekmez. `MobileCastleCombatAuthoring.Baker`, `MobileWorkerBuildingUpgradeState` component'ini mevcut mobile config entity'sine sıfır seviyelerle ekler.

Başlangıç maliyetleri ve ortak büyüme katsayısı `DefaultDifficulty.asset` içindeki
`Ekonomi Fiyat Eğrileri` alanlarından gelir. `Window > DeadWalls > Difficulty Tuner`
penceresindeki aynı foldout Play Mode'da config entity'sine canlı uygulanabilir.

Onaylı default maliyetler CAP için `100 Wood + 25 Iron`, EFF için
`150 Wood + 50 Iron`, ortak büyüme için `1.35` değeridir. `+10` slot ve additive
`+10%` etki büyüklüğü fiyat tuning'inden ayrı gameplay sözleşmesidir.

## HUD kurulumu

`Window > DeadWalls > Repair Worker Drawer Target Controls` menüsü:

- `Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab` içinde dört worker satırına `CapacityUpgradeButton` ve `EfficiencyUpgradeButton` ekler.
- Drawer genişliğini `980 px` yapar ve mevcut sol kenarı koruyarak yalnız sağa genişletir.
- Aktif sahne `NewGameScene` ise sahnedeki otoriter `WorkerEconomyDrawerUI` referanslarını bağlar ve sahneyi kaydeder.
- Prefab üzerinde ikinci bir `WorkerEconomyDrawerUI` bırakmaz; runtime listener owner'ı sahnedeki mevcut component'tir.

Her kaynak prefix'i için gereken yeni isimler:

```text
WoodCapacityUpgradeButton
WoodEfficiencyUpgradeButton
StoneCapacityUpgradeButton
StoneEfficiencyUpgradeButton
IronCapacityUpgradeButton
IronEfficiencyUpgradeButton
FoodCapacityUpgradeButton
FoodEfficiencyUpgradeButton
```

## Manuel doğrulama

1. `NewGameScene` açıkken repair menüsünü çalıştır.
2. Console'da error olmadığını doğrula.
3. Play Mode'da `Workers` drawer'ını aç.
4. Her satırda `CAP L0 / 100W 25I` ve `EFF L0 / 150W 50I` başlangıç etiketlerini doğrula.
5. Capacity alımının ilgili cap'i `10` artırdığını, Efficiency alımının baz kişi üretimine additive `%10` eklediğini doğrula.
6. Her alımda Wood ve Iron'ın birlikte düştüğünü doğrula.
7. Ana menüye dönüp Continue sonrasında seviyelerin ve bir sonraki maliyetin aynı kaldığını doğrula.
