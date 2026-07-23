# Finite Arrow Supply + Timed Delivery - Mimari

## Otorite ve oyuncu sözleşmesi

V1'de Arrow tek sürekli tüketilen kaynaktır. Her başarılı ok atışı tam `1 Arrow`
harcar. Stok `0` olduğunda okçular hedef seçse bile projectile rent etmez; Wood ile
satın alınan refill için ödeme transaction anında alınır; Arrow'lar takip eden
`3` simulation saniyesi boyunca yolda ve kullanılamaz kalır. Süre tamamlandığında siparişin
tamamı gerçek `ArrowSupply.Current` stokuna tek seferde eklenir; bu ana kadar stok `0` ise
okçular ateş edemez.
Pause teslimatı durdurur, `2X/3X` oyun hızı ise diğer simülasyon işleri gibi teslimatı
hızlandırır. Fletcher, üretim kuyruğu veya worker gereksinimi yoktur.

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
  teslimatı başlatır ve UI event'i yayınlar.
- `GameManager.ArrowDelivery`: tek aktif refill teslimatının toplam miktarını ve geçen
  simulation süresini tutar. Bekleme sırasında canlı ECS stoğuna yazmaz; süre dolduğunda
  siparişin tamamını o andaki kullanılabilir stoğa atomik ekler. Böylece mevcut stoktan
  gerçekleşen eşzamanlı okçu tüketimini ezmez ve sipariş erken kullanıma açılmaz.
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

Üst HUD Arrow chip'i `Current / Capacity` gösterir; `INF` modu yoktur ve diğer resource
chip'leri gibi pasif bilgi yüzeyidir. Alt-sağ management dock'taki `ARROW SUPPLY`
butonu güncel UI Toolkit supply drawer'ını açar. Drawer mevcut stok/verim, paketler,
`Buy Max`, Capacity ve Efficiency yatırımlarını görünür fiyatlarıyla gösterir.

Teslimat sürerken durum metni `DELIVERING · Ns`, supply barı ise altın durum rengini
kullanır. Sayısal `Current / Capacity` yalnız gerçekten kullanılabilir stoğu gösterir;
bar ise mevcut oran ile sipariş teslim edildiğinde oluşacak oran arasında zaman ilerlemesini
görselleştirir. Bu projeksiyon oynanabilir stok değildir. Aynı anda yalnız bir refill
teslimatı olabilir. İkinci satın alma denemesi player-facing
`SUPPLY DELIVERY IN PROGRESS · Ns REMAINING` uyarısıyla reddedilir.

Ilk-kosu onboarding'i finite stok effective kapasitenin inclusive `%25` veya altina indiginde
panel kapaliyken `ARROW SUPPLY` dock butonunu pulse eder. Paneli otomatik acmaz; oyuncu paneli
actiginda pulse gercek `+1 paket` butonuna tasinir. Basarili `+1`, `+5` veya `Buy Max` refill
ammo tutorial flag'ini tamamlar; CAP/EFF yatirimi tamamlamaz.

## Save ve migration

Run save güncel şema `v17`'dir. `ArrowCurrent`, `ArrowCapacityLevel` ve
`ArrowEfficiencyLevel` exact Continue kapsamında tutulur. `v3-v13` kayıtları sıralı
migration ile güncel şemaya yükseltilir; eski kayıtlarda iki yatırım seviyesi `0` başlar. Restore edilen
stok, data-driven kapasiteye clamp edilir. Restart seviyeleri sıfırlar ve base
kapasiteyi doldurur. Ayrı bir teslimat save şeması yoktur: snapshot alınırken ödemesi
yapılmış aktif teslimat önce eksiksiz biçimde stoka uygulanır, ardından `ArrowCurrent`
kaydedilir. Böylece Wood harcanıp teslim edilmemiş Arrow kaybı oluşmaz.

## Difficulty Tuner yuzeyi

`Archer Runtime Contract`, finite Arrow profile alanlarini recruitment/combat owner'lariyla
ayni tuning panelinde gosterir. Preview, `ArrowEconomyUtility` ile capacity, Arrow/Wood,
paket, Buy Max ve CAP/EFF sonraki maliyetini hesaplar. Play Mode telemetry, stok/capacity,
seviyeler, pool active/available/total rent ve rent delta'sindan gercek Arrow/s drain'i okur.
Tuner ayrica teorik effective fire-rate toplamini gosterir; gercek tuketim hedef, cooldown ve
pool gating nedeniyle bu tavandan dusuk olabilir.

## Performans ve test sınırı

Refill tek bir GameManager teslimat state'idir; archer başına üretim entity'si veya queue
oluşturmaz. `Rapid` yüksek fire rate nedeniyle aynı sürede Basic'ten daha fazla Arrow
tüketir. Üç saniyelik bekleme boyunca singleton stok değişmez; süre sonunda sipariş tek
bir singleton write ile eklenir ve mevcut pooled projectile yolu değiştirilmez.

1.000 hazır Basic Archer + stok `0` kabul koşusunda gerçek
`GameManager.TryBuyArrowRefill(10)` transaction'ı `1.000 Arrow / 250 Wood` sipariş eder.
Pause durumunda stok ve rent sayısı değişmez. Simülasyon yeniden akınca 1.000 Arrow,
3 simulation saniyesi boyunca kullanılamaz kalır ve projectile rent oluşmaz. Süre sonunda
stok atomik olarak `1.000` artar; takip eden ECS tick'inde hazır okçular stoğu tüketir ve
toplam projectile rent sayısı tam `1.000` artar. Prewarm pool genişlemez.

Doğrulama sahipleri:

- `ArrowEconomyUtilityTests`: sabit birim fiyat, kısmi dolum, Buy Max, efficiency,
  exponential yatırım, overflow sınırları ve 3 saniyelik teslimat ilerleme matematiği.
- `ArrowAmmoPlayModeTests`: stok `0` iken atışın durması, teslimatın tamamı boyunca stok/rent
  oluşmaması, süre sonunda atomik gelişle atışın yeniden başlaması, pause sözleşmesi,
  1.000 Archer bulk delivery, snapshot sırasında bekleyen teslimatın kaybolmaması,
  projectile başına `1` tüketim ve Rapid tüketim farkı.
- `GameplayHUDToolkitContractTests`: İngilizce teslimat metinleri, durum sınıfı ve bar
  transition sözleşmesi.
- `RunPersistenceTests`: şema `v17`, eski kayıt migration'ı ve yatırım seviyeleri.
