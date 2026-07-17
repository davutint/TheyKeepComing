# Finite Arrow Supply + Instant Refill - Mimari

## Otorite ve oyuncu sözleşmesi

V1'de Arrow tek sürekli tüketilen kaynaktır. Her başarılı ok atışı tam `1 Arrow`
harcar. Stok `0` olduğunda okçular hedef seçse bile projectile rent etmez; Wood ile
satın alınan refill aynı transaction içinde stoka yazılır ve takip eden simulation
tick'inde atış yeniden başlar. Fletcher, üretim kuyruğu, worker veya bekleme süresi yoktur.

## Veri ve sahiplik

- `ArrowSupply.Current`: mevcut Arrow stoku.
- `ArrowSupply.CapacityLevel`: run içi kapasite yatırım seviyesi.
- `ArrowSupply.EfficiencyLevel`: run içi Arrow/Wood yatırım seviyesi.
- `ArrowSupply.Accumulator`: eski save/serialization uyumluluğu için korunur; V1 refill
  üretimi bu alanı kullanmaz.
- `MobileEconomyPriceTuning`: kapasite, paket, verim ve iki yatırım maliyet eğrisinin
  data-driven baseline'ıdır.
- `ArrowEconomyUtility`: kapasite, sabit oranlı paket, kısmi dolum, Buy Max ve yatırım
  maliyeti matematiğinin saf sahibidir.
- `GameManager`: oyuncu transaction'larının tek owner'ıdır; fiyat okur, kaynak harcar,
  `ArrowSupply` yazar ve UI event'i yayınlar.
- `ArrowSupplyUI`: yalniz basarili player-facing refill sonrasinda
  `ArrowRefillPurchasedByPlayer` event'ini yayar; onboarding transaction'i tekrar etmeden dinler.
- `ArcherShootSystem`: yalnız gerçek projectile pool rent'i başarılı olduktan sonra
  `Current` değerini `ArrowCostPerSuccessfulProjectileRent = 1` azaltır. Pool boşsa Arrow
  harcanmaz; bu V1 sabit kuralidir, balance alani degildir.

## Varsayılan ekonomi

- Base capacity: `200`.
- Capacity yatırımı: seviye başına `+200`.
- Refill paketi: `100 Arrow`.
- Base verim: `4 Arrow / Wood`.
- Efficiency yatırımı: seviye başına `+1 Arrow / Wood`.
- Capacity başlangıç maliyeti: `150 Wood + 25 Iron`.
- Efficiency başlangıç maliyeti: `200 Wood + 50 Iron`.
- İki yatırımın fiyat büyümesi: `ceil(base × 1.35^level)`.

Refill birim fiyatı satın alma sayısıyla büyümez. Kapasiteye sığmayan bölüm için Wood
harcanmaz: örneğin `170/200` stokta paket `30 Arrow` verir ve yalnız bu miktarın
ceil maliyetini öder. `Buy Max`, mevcut Wood bütçesi ile kapasite boşluğunun minimumunu
tek transaction'da alır. Capacity ve Efficiency yatırımları birbirinden bağımsızdır
ve her satın alım hem Wood hem Iron harcar.

## UI sözleşmesi

Üst HUD Arrow chip'i `Current / Capacity` gösterir; `INF` modu yoktur. Chip'e basmak
tek satırlık `AmmoPurchasePanel` açar. Panel mevcut stok/verim, `+1 paket`, `+5 paket`,
`Buy Max`, Capacity ve Efficiency yatırımlarını görünür fiyatlarıyla gösterir.
Kaynak yetersizliği butonu kapatır fakat fiyatı gizlemez; yalnız dolu stok `FULL`,
Wood ile hiçbir Arrow alınamayan Buy Max durumu `NEED WOOD` yazar.

Ilk-kosu onboarding'i finite stok effective kapasitenin inclusive `%25` veya altina indiginde
yalniz ust HUD `ArrowChip` satirini pulse eder. Paneli otomatik acmaz. Basarili `+1`, `+5` veya
`Buy Max` refill ammo tutorial flag'ini tamamlar; CAP/EFF yatirimi tamamlamaz.

## Save ve migration

Run save güncel şema `v14`'tür. `ArrowCurrent`, `ArrowCapacityLevel` ve
`ArrowEfficiencyLevel` exact Continue kapsamında tutulur. `v3-v13` kayıtları sıralı
migration ile güncel şemaya yükseltilir; eski kayıtlarda iki yatırım seviyesi `0` başlar. Restore edilen
stok, data-driven kapasiteye clamp edilir. Restart seviyeleri sıfırlar ve base
kapasiteyi doldurur.

## Difficulty Tuner yuzeyi

`Archer Runtime Contract`, finite Arrow profile alanlarini recruitment/combat owner'lariyla
ayni tuning panelinde gosterir. Preview, `ArrowEconomyUtility` ile capacity, Arrow/Wood,
paket, Buy Max ve CAP/EFF sonraki maliyetini hesaplar. Play Mode telemetry, stok/capacity,
seviyeler, pool active/available/total rent ve rent delta'sindan gercek Arrow/s drain'i okur.
Tuner ayrica teorik effective fire-rate toplamini gosterir; gercek tuketim hedef, cooldown ve
pool gating nedeniyle bu tavandan dusuk olabilir.

## Performans ve test sınırı

Refill bir UI/ECS singleton transaction'ıdır; archer başına üretim entity'si veya queue
oluşturmaz. `Rapid` yüksek fire rate nedeniyle aynı sürede Basic'ten daha fazla Arrow
tüketir. 1.000 archer refill sonrası mevcut pooled projectile yolunu kullanmaya devam eder.

1.000 hazır Basic Archer + stok `0` kabul koşusunda gerçek
`GameManager.TryBuyArrowRefill(10)` transaction'ı `1.000 Arrow / 250 Wood` yazar. Takip eden
tek simulation tick'i tam `1.000` gameplay projectile rent eder, stoku tekrar `0` yapar ve
prewarm pool'u genişletmez. İki temiz Editor örneklemi refill transaction için
`0,010-0,600 ms`, restart frame'i için `22,210-23,158 ms` main thread /
`24,327-50,327 ms` wall-frame ve bütün Editor test frame'i için `29.622-30.094 B` GC gösterdi.
Guard bütçesi main thread `< 50 ms`, wall-frame `< 100 ms`'dir. GC
örneklemi test runner dahil tüm Editor frame'ine aittir; izole Player system-allocation kabulü
yerine geçmez.

Doğrulama sahipleri:

- `ArrowEconomyUtilityTests`: sabit birim fiyat, kısmi dolum, Buy Max, efficiency,
  exponential yatırım ve overflow sınırları.
- `ArrowAmmoPlayModeTests`: stok `0` iken atışın durması, instant refill sonrası yeniden
  başlaması, 1.000 Archer bulk-refill restart ölçümü, projectile başına `1` tüketim, ana
  kaynakların combat tick'inde azalmaması ve Rapid tüketim farkı.
- `RunPersistenceTests`: şema `v8`, eski kayıt migration'ı ve yatırım seviyeleri.
