# Finite Arrow Supply + Instant Refill - Editor Setup

## Otomatik kurulum

1. `NewGameScene` açık ve Play Mode kapalı olsun.
2. `Window > DeadWalls > Repair Finite Arrow Ammo Panel` çalıştır.
3. Console'da `Finite Arrow ammo paneli prefab ve NewGameScene'de onarildi.` kaydını
   doğrula.
4. Scene'i açıp `GameManager` üzerindeki `ArrowSupplyUI` binding'lerini kontrol et.

Araç, UI gerçeği olan
`Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab` içindeki Arrow chip'ine Button
ekler ve tek satırlık `AmmoPurchasePanel` üretir. Listener sahibi prefab değil,
`NewGameScene` üzerindeki tek `ArrowSupplyUI` component'idir; böylece duplicate listener
oluşmaz.

## Beklenen binding'ler

`ArrowSupplyUI` alanlarının tamamı dolu olmalıdır:

- `AmmoPanel`
- `ToggleButton` (`ArrowChip`)
- `StockText`, `EfficiencyText`
- `PackageButton`, `LargePackageButton`, `BuyMaxButton`
- `CapacityUpgradeButton`, `EfficiencyUpgradeButton`

Scene-owned `FirstRunOnboardingUI.AmmoSupply`, ayni `ArrowSupplyUI` component'ine bagli
olmalidir. Prefab asseti runtime onboarding controller tasimaz.

Panel varsayılan olarak kapalıdır ve Arrow chip'iyle açılıp kapanır. `HUDController`
ayrıca chip üzerindeki ana değeri `Current / Capacity` biçiminde günceller.

## Tuning

`Window > DeadWalls > Difficulty Tuner` içindeki Arrow Economy alanları
`DifficultyProfileSO` üzerinden şu runtime değerlerini besler:

- base capacity ve level başına capacity,
- refill package size,
- base Arrow/Wood ve level başına efficiency,
- Capacity/Efficiency Wood + Iron base maliyetleri,
- ortak yatırım growth multiplier.

`APPLY` sonrasında `MobileCastleTuningResolver` değerleri sanitize eder ve aktif
`MobileEconomyPriceTuning` singleton'ına yazar. Scene authoring fallback'leri yalnız
profile bulunmayan eski sahneler içindir.

## Play Mode kabulü

- Stoku `0` yap: pooled projectile üretilmemeli ve Arrow eksiye düşmemeli.
- Yeterli Wood verip paket al: stok aynı anda artmalı; takip eden simulation tick'inde
  okçular yeniden ateş etmeli.
- Tam kapasiteye yakın paket al: yalnız sığan miktar ve karşılık gelen Wood harcanmalı.
- Capacity/Efficiency al: ikisi de Wood + Iron harcamalı ve fiyatları ayrı seviyeleriyle
  büyümeli.
- Save/Continue: current stok ve iki yatırım seviyesi aynı kalmalı.
- Onboarding flag incomplete iken stoku `%25` esigine indir: `ArrowChip` pulse olmali,
  `AmmoPurchasePanel` kendiliginden acilmamali; basarisiz refill flag yazmamali, basarili refill
  `tutorial.v1.low_ammo` flag'ini yazmalidir.

Otomatik regresyon için `ArrowEconomyUtilityTests`, `RunPersistenceTests` ve
`ArrowAmmoPlayModeTests` çalıştırılır.
