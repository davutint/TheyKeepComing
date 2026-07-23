# Finite Arrow Supply + Timed Delivery - Editor Setup

## Otomatik kurulum

1. `NewGameScene` açık ve Play Mode kapalı olsun.
2. `Tools > Dead Walls > Setup & Repair > Repair Finite Arrow Ammo Panel` çalıştır.
3. Console'da `Finite Arrow ammo paneli prefab ve NewGameScene'de onarildi.` kaydını
   doğrula.
4. Scene'i açıp `GameManager` üzerindeki `ArrowSupplyUI` binding'lerini ve UI Toolkit
   `ARROW SUPPLY` drawer'ını kontrol et.

Araç, UI gerçeği olan
`Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab` içindeki bütün resource chip'lerini
pasif tutar; alt-sağ dock'a `ArrowSupplyToggleButton` ve üstüne tek satırlık
`AmmoPurchasePanel` üretir. Listener sahibi prefab değil,
`NewGameScene` üzerindeki tek `ArrowSupplyUI` component'idir; böylece duplicate listener
oluşmaz.

## Beklenen binding'ler

`ArrowSupplyUI` alanlarının tamamı dolu olmalıdır:

- `AmmoPanel`
- `ToggleButton` (`ArrowSupplyToggleButton` / `ARROW SUPPLY`)
- `StockText`, `EfficiencyText`
- `PackageButton`, `LargePackageButton`, `BuyMaxButton`
- `CapacityUpgradeButton`, `EfficiencyUpgradeButton`

Scene-owned `FirstRunOnboardingUI.AmmoSupply`, ayni `ArrowSupplyUI` component'ine bagli
olmalidir. Prefab asseti runtime onboarding controller tasimaz.

Panel varsayılan olarak kapalıdır ve `ARROW SUPPLY` dock butonuyla açılıp kapanır.
`ArrowSupplyToggleButton`, alt-sağ `(-356,28)` konumunda `156 x 56`; panel ise
`(-24,160)` konumunda `732 x 78` boyutundadır. `HUDController` ayrıca pasif Arrow chip'i
üzerindeki ana değeri `Current / Capacity` biçiminde günceller.

## Tuning

`Window > DeadWalls > Difficulty Tuner > Archer Runtime Contract` içindeki finite Arrow alanları
`DifficultyProfileSO` üzerinden şu runtime değerlerini besler:

- base capacity ve level başına capacity,
- refill package size,
- base Arrow/Wood ve level başına efficiency,
- Capacity/Efficiency Wood + Iron base maliyetleri,
- ortak yatırım growth multiplier.

`APPLY` sonrasında `MobileCastleTuningResolver` değerleri sanitize eder ve aktif
`MobileEconomyPriceTuning` singleton'ına yazar. Scene authoring fallback'leri yalnız
profile bulunmayan eski sahneler içindir.

Ayni panelde `Arrow per successful projectile rent = 1` read-only gorunur. Play Mode'da
effective Archer fire-rate/DPS, teorik shot demand, gercek pool rent Arrow/s, stok/capacity,
CAP/EFF seviyeleri ve sonraki yatirim fiyatlari canli okunur.

## Play Mode kabulü

- Stoku `0` yap: pooled projectile üretilmemeli ve Arrow eksiye düşmemeli.
- Yeterli Wood verip paket al: Wood aynı anda harcanmalı, stok transaction frame'inde
  artmamalı ve takip eden `3` simulation saniyesi boyunca hiç değişmemelidir. Süre dolduğu
  anda satın alınan miktarın tamamı kullanılabilir stoka tek seferde eklenmelidir.
- Stok `0` iken teslimatın ilk `2,9` saniyesinde pooled projectile rent oluşmamalı; okçular
  yalnız atomik stok gelişi tamamlandıktan sonra ateşe devam etmelidir.
- Teslimat sürerken supply drawer `DELIVERING · Ns` göstermeli; sayısal stok gerçek
  `Current / Capacity` değerinde kalırken bar teslimatın görsel ilerlemesini göstermelidir.
- Pause sırasında sayaç, görsel ilerleme ve stok değişmemeli; `2X/3X` hızlarında teslimat aynı oranda
  hızlanmalıdır.
- Aktif teslimat sürerken ikinci refill denemesi yeni ödeme almamalı ve
  `SUPPLY DELIVERY IN PROGRESS · Ns REMAINING` uyarısını göstermelidir.
- Tam kapasiteye yakın paket al: yalnız sığan miktar ve karşılık gelen Wood harcanmalı.
- Capacity/Efficiency al: ikisi de Wood + Iron harcamalı ve fiyatları ayrı seviyeleriyle
  büyümeli.
- Save/Continue: current stok ve iki yatırım seviyesi aynı kalmalı. Snapshot aktif teslimat
  sırasında alınırsa ödemesi yapılmış kalan Arrow önce stoka uygulanmalı ve save'e yazılmalıdır.
- 1.000 hazır okçu + stok `0`: `+10` paketlik gerçek transaction pause'da ilerlememeli;
  simülasyon açıldıktan sonraki 3 simulation saniyesi dolmadan hiçbir pooled projectile
  rent edilmemeli; atomik gelişten sonraki ECS tick'inde toplam tam `1.000` pooled projectile
  rent edilmeli, stok tam `0` olmalı ve pool expansion oluşmamalıdır.
- Onboarding flag incomplete iken stoku `%25` esigine indir: panel kapaliyken
  `ArrowSupplyToggleButton`, panel oyuncu tarafindan acilinca `AmmoPackageButton` pulse olmali;
  `AmmoPurchasePanel` kendiliginden acilmamali. Basarisiz refill flag yazmamali, basarili refill
  `tutorial.v1.low_ammo` flag'ini yazmalidir.

Otomatik regresyon için `ArrowEconomyUtilityTests`, `RunPersistenceTests` ve
`ArrowAmmoPlayModeTests` çalıştırılır.
