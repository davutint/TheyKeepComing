# Dead Walls Post-V1 - Follow-up Tracker

> **Amaç:** V1 kapsamı tamamlandıktan sonra yapılacak mantık düzeltmelerini, oyuncuya görünen geri bildirimleri, UI/UX yeniden çalışmalarını, polish işlerini ve performans optimizasyonlarını tek otoriter takip belgesinde yürütmek.
>
> **Tracker sürümü:** 3.3
> **Oluşturulma tarihi:** 2026-07-18
> **Aktif paket:** `DW-P18-IOS-DISTRIBUTION`
> **Aktif iş:** Internal TestFlight kurulumu ve fiziksel iPhone Game Center doğrulaması
> **İlerleme:** `17 / 18` ana görev tamamlandı - `%94,44`

---

## 1. Belgenin Otoritesi ve Çalışma Kuralı

Bu belge, tamamlanmış V1 tracker'ından sonraki geliştirme döneminin aktif takip kaynağıdır. `DEAD_WALLS_V1_IMPLEMENTATION_TRACKER.md` kapalı V1 kapsamının tarihsel kanıtı olarak korunur; bu belge onu geriye dönük değiştirmez.

### Çalışma önceliği ve paralellik

1. Bölüm 3'teki sıra çalışma önceliğini gösterir; bütün ana görevler arasında zorunlu teknik bağımlılık olduğu anlamına gelmez.
2. Birbirinden bağımsız görevler, farklı owner/file/prefab sınırlarında çalışılıyorsa paralel yürütülebilir.
3. Aynı dosya, prefab, scene veya runtime owner'ını değiştiren görevler çakışmalı yürütülmez.
4. Performans optimizasyonu, ölçülecek ilgili davranış kesinleştikten sonra; final kalite kapısı ise bütün aktif kapsam tamamlandıktan sonra yapılır.
5. Her görevin başında owner ile ilgili sorunlar ve doğru hedef davranış konuşulur.
6. Konuşulmamış bir ürün kararı varsayımla doldurulmaz; `[?]` olarak bekletilir.
7. Gerekli rakip oyun, UI/UX ve görsel referans araştırması yalnızca ilgili görev aktif olduğunda yapılır.
8. Kararlar kesinleştikten sonra kabul kriterleri yazılır; ardından uygulama ve doğrulama yapılır.
9. Bir görev ancak karar, uygulama, doğrulama ve tracker güncellemesi tamamlandığında `[x]` olur.

### Statüler

- `[?]` Owner görüşmesi ve karar bekliyor.
- `[ ]` Kararları kesinleşti, uygulama bekliyor.
- `[~]` Uygulama veya doğrulama sürüyor.
- `[x]` Uygulandı, doğrulandı ve tracker güncellendi.
- `[!]` Doğrulanmış engel veya regresyon var.

### İlerleme hesabı

İlerleme, Bölüm 3'teki 18 ana görev üzerinden hesaplanır. Alt maddeler kanıt ve kabul kapsamını gösterir; ayrıca paydayı büyütmez. Ana görevler yalnızca bütün zorunlu alt kapıları tamamlandığında kapanır. Owner'ın Arrow refill'i anlık transaction yerine okunabilir bir teslimat sürecine çevirme kararıyla açılan `DW-P17-ARROW-DELIVERY` paydayı `16 -> 17`; iOS Game Center ve TestFlight hazırlığıyla açılan `DW-P18-IOS-DISTRIBUTION` ise `17 -> 18` değiştirmiştir.

---

## 2. Şimdiden Kilitlenmiş Kararlar

Bu bölüm yalnızca owner tarafından açıkça kesinleştirilmiş kararları içerir.

### Düşman ölümü ve Soul geri bildirimi

- Öldürülen her Skeleton tam olarak `1 Soul` kazandırır.
- Ödül Skeleton öldüğü anda gameplay tarafında kazanılmış sayılır.
- Görsel Soul, ölen Skeleton'ın bulunduğu noktadan çıkar ve HUD'daki Soul göstergesine animasyonlu biçimde gider.
- Görsel ulaşma anı HUD sayacında okunabilir bir varış geri bildirimi üretir.
- Görsel animasyon gameplay ödülünün verilmesini geciktirmez veya iptal etmez.

### Düşmana verilen hasarın görünürlüğü

- Düşmanların aldığı gerçek hasar, kafalarının üzerinde oyuncuya gösterilir.
- Basic, Rapid ve Frost Arrow dahil bütün oyuncu kaynaklı gerçek hasar yolları kapsamdadır.
- Fireball, ikincil patlama, yanma ve alan hasarı gibi oyuncu kaynaklı diğer gerçek hasar uygulamaları da kapsamdadır.
- Oyuncuya gösterilen değer, gerçekten uygulanmış hasarla eşleşir.
- Performans optimizasyonu bu oyuncu tarafından gözlenen davranışı değiştiremez.

### UI/UX kapsam sınırları

- Oyuncunun gördüğü bütün UI ve UX yüzeyleri değerlendirme kapsamındadır.
- Worker dağıtım arayüzü ayrı ana görevdir.
- Arrow Supply arayüzü ayrı ana görevdir.
- Teknoloji ağacı ayrı ana görevdir.
- Castle Heart ayrı ana görev olarak ele alınacaktır.
- Benzer oyun ve UI/UX araştırması şimdi değil, P3 aktif olduğunda yapılacaktır.
- Henüz konuşulmamış layout, kontrol modeli, stil, animasyon ve efekt kararları bu belgede kesinleştirilmiş sayılmaz.

### Gece saldırısı ve günlük Council ritmi

- Düşman saldırısı ve yeni spawn talebi yalnız Night fazında gerçekleşir.
- Timed Night bittiğinde pending backlog veya yaşayan düşman varsa cycle Dawn'a geçmez.
- Clearance sırasında yeni talep üretilmez; mevcut Night backlog'u sahaya akmaya devam eder.
- Dawn yalnız `PendingEnemies == 0` ve `ZombiesAlive == 0` birlikte sağlandığında başlar.
- Her günün Dawn başlangıcında mevcut Council sistemiyle tam bir regular kart açılır; aynı gün ikinci kez açılmaz.
- İlk günlük kart Day 1'de desteklenir; event içeriği mevcut deterministic composer, context, flag, chain ve exact effect sahipliğini korur.

### Oyun hızı, Council pause ve toast sınırı

- Gün döngüsü panelinin altında `1X`, `2X` ve `3X` oyun hızı kontrolleri bulunur.
- Council karar kartı açıkken simulation durur; geçerli seçim commit edilince karttan önceki hız aynen geri gelir.
- Toast altyapısı eklenir, fakat yeni gameplay olaylarının toast üretmesi owner onayı olmadan bağlanmaz.

### Game Over yükseltme açıklığı ve eylem reddi toast'ları

- Game Over kalıcı yükseltme kartı yalnız ad/fiyat göstermez; mevcut kalıcı toplamı ve satın alma
  sonrası exact toplamı oyuncuya açıkça karşılaştırır.
- Oyuncunun bilinçli olarak bastığı purchase/research düğmesi kaynak, worker, kapasite, prerequisite,
  kilit veya maximum level nedeniyle reddedilirse exact neden warning toast olarak gösterilir.
- Kaynak açığı `NEED 14 MORE WOOD` gibi tam kaynak adı ve miktarla söylenir; genel `BLOCKED` copy'si
  bu kapsamdaki player action'lar için yeterli değildir.
- Otomatik phase, pasif kaynak dolması, savunma alarmı ve oyuncu tıklaması olmayan event'ler bu
  onayın dışında kalır.

### Toast sunumu ve button audio polish'i

- Her bilinçli player action'ı ayrı toast kartı üretir; aynı mesaj art arda gelse bile tek karta
  birleştirilmez veya mevcut kartın süresi yenilenmez.
- Ekranda aynı anda en fazla üç toast kartı görünür. Yeni kart altta belirir, eskiler yukarı taşınır;
  dördüncü kart geldiğinde en eski görünen kart kaldırılır.
- Toast'lar unscaled süreyle otomatik kaybolur ve HUD etkileşimini engellemez.
- UI Toolkit button'ları legacy uGUI raycast'ine bağlı kalmadan merkezi UI click sesini çalar.
- Yeni otomatik gameplay toast trigger'i eklenmez; P14'ün owner onaylı action-failure kapsamı korunur.

### Guided onboarding yeniden çalışma sınırı

- Oyuncuya oyun, yeni UI Toolkit yüzeyindeki gerçek kontroller üzerinden adım adım öğretilecektir;
  gizli legacy Canvas kontrolüne bağlı pulse tek başına kabul edilmez.
- İlk zorunlu zincir `ECONOMY -> worker-share slider -> CLOSE -> BARRACKS -> BASIC ARCHER -> 2X` sırasıdır.
- İlk altı adımda ekran hedef dışı bölgelerde kararır ve yalnız gerçek hedef kontrol etkileşim alır;
  zincir yalnız başarıyla tamamlanan player action'ıyla ilerler.
- Görünür tutorial/core veya contextual field tip boyunca simulation durur. Core zincir pause lease'ini
  adımlar arasında kesintisiz korur; son `2X` aksiyonu tamamlanınca seçilen hızla oyun devam eder.
- Contextual field tip unrelated UI input'unu kilitlemez; Housing ve Arrow tipleri pause altında
  kaynak bekleme soft-lock'i yaratmamak için yalnız ilgili satın alım affordable iken açılır.
- Gerçek hedef çerçevesi unscaled zamanda nefes alan padding/opacity/border pulse'i kullanır; kart
  sistemin neden önemli olduğunu açıklayan English body ve açık pause/action yönlendirmesi taşır.
- First Night `RALLY`, first Council exact choice, low-arrow refill, first post-combat Essence ile
  `CASTLE HEART`, dolu population kapasitesinde housing ve ilk wall damage repair adımları
  koşul oluştuğunda contextual gösterilir; unrelated kontrolleri kilitlemez.
- `RALLY` ve `EMERGENCY REPAIR` contextual hedefleri guided pause yüzünden disabled kalmaz. Gerçek
  player action'ı yalnız `GuidedOnboarding` lease'ini kontrollü biçimde bırakıp transaction'ı çalıştırır;
  başarısız denemede pause geri alınır, başarıda önceki oyun hızı aynen geri gelir. Diğer pause owner'ları
  yetenek kullanımını açmaz.
- Council kendi mevcut pause sözleşmesini kullanır; tutorial oyuncu adına otomatik action yapmaz.
- Her adım yalnız mevcut Play oturumunda kaydedilir; Stop -> Play bütün tutorial'i ilk adımdan yeniden
  başlatır. Kalıcı save/UGS sahipliği yayın öncesine ertelenmiştir; Settings içindeki manuel reset yolu
  aynı oturumluk test kolaylığı olarak korunur.
- Kaynak yeterli olduğu sürece sürekli görünen legacy `RECRUIT A BASIC ARCHER.` affordability hint'i
  yeni HUD'dan kaldırılmıştır; bu geçici temizlik yeni tutorial sırası kararı değildir.

### Arrow refill teslimat ritmi

- Refill satın alındığında Wood transaction anında harcanır; Arrow stoku anında dolmaz.
- Satın alınan Arrow miktarı `3` simulation saniyesi boyunca pending ve kullanılamaz kalır;
  gerçek `ArrowSupply.Current` stokuna ancak süre tamamlandığında tek seferde eklenir.
- Kullanılabilir stok `0` ise okçular teslimatın tamamı boyunca ateş etmez; ilk atış yalnız
  atomik stok gelişinden sonraki ECS tick'inde gerçekleşebilir.
- Pause teslimatı durdurur; `2X/3X` hızları diğer simülasyon işleri gibi teslimatı hızlandırır.
- Supply drawer teslimat sırasında `DELIVERING · Ns` durumunu gösterir. Sayısal
  `Current / Capacity` yalnız kullanılabilir stoğu, bar ise mevcut oran ile sipariş sonrası
  oran arasındaki teslimat ilerlemesini gösterir.
- Aynı anda yalnız bir refill teslimatı yürür; ikinci deneme yeni ödeme almadan exact İngilizce
  `SUPPLY DELIVERY IN PROGRESS · Ns REMAINING` uyarısıyla reddedilir.
- Yeni otomatik completion toast'ı eklenmez; yalnız owner onaylı player-action toast sınırı korunur.
- Snapshot alınırken ödemesi yapılmış bekleyen miktar önce stoka uygulanır; save şemasında
  teslim edilmemiş Arrow kaybı oluşmaz.

### UI okunabilirliği ve player-facing terminoloji

- Gameplay HUD USS içinde player-facing yazı boyutu tabanı referans çözünürlükte `10px`tir; kritik phase mesajı `11px` ve yüksek kontrastlıdır.
- Kaynak üretim oranı `/MIN`, archer performansı `DAMAGE / SEC` olarak açık yazılır; worker hedef düğmeleri neyi değiştirdiğini `TARGET` ile belirtir.
- Night clearance, kalan sayıyı `N ENEMIES LEFT` ve kapıyı `CLEAR ENEMIES TO REACH DAWN` ile açıklar.
- Run HUD meta ödülü legacy `SOULS` adıyla sunulmaz; ölüm anındaki tahmini kısa kimlik `EMBERS ON DEATH`tir.
- Kaldırılmış Doctrine yüzeyine yönlendiren metin kullanılmaz; kilitli archer araştırması oyuncuyu `CASTLE HEART`a yönlendirir.

### iOS kimliği, Game Center ve dağıtım hazırlığı

- iOS Bundle ID kalıcı ve küçük harfli `com.pixicorp.zombiecastle` değeridir.
- Apple Developer Team ID `7JVZGHB5S5`; Unity iOS automatic signing açıktır.
- `GameCenterService`, iOS Player açılışında local player authentication başlatan tek runtime
  owner'dır; başarısız kimlik doğrulama oyunun açılmasını engellemez.
- Game Center native yolu yalnız `UNITY_IOS && !UNITY_EDITOR` altında derlenir; Editor ve diğer
  platformlar no-op kalır.
- Leaderboard ve achievement kimlikleri bu authentication-only paketin kapsamı değildir.
- TestFlight/App Store upload dış servis durumudur; repo implementasyonundan ayrı kanıtlanır.

---

## 3. Ana Görev Sırası

| Sıra | Kimlik | Ana görev | Durum | Tamamlanma kapısı |
|---:|---|---|:---:|---|
| 1 | `DW-P1-LOGIC` | Mantık hatalarının tespiti ve düzeltilmesi | `[x]` | Owner bilinen mantık sorunu olmadığını kesinleştirdi; başlangıç regresyonu temiz |
| 2 | `DW-P2-COMBAT-FEEDBACK` | Skeleton Soul ve düşman hasar geri bildirimleri | `[x]` | Kilitli davranışlar uygulandı; event doğruluğu ve tam regresyon geçti |
| 3 | `DW-P3-UIUX-AUDIT` | Oyuncunun gördüğü bütün UI/UX yüzeylerinin uzman denetimi | `[x]` | Yüzey envanteri, bilimsel referans araştırması, karar PDF'i ve UI Toolkit uygulama planı tamamlandı |
| 4 | `DW-P4-WORKERS` | Worker dağıtım arayüzünün yeniden çalışılması | `[x]` | Mevcut idle nüfusun doğrudan atanması, yeni nüfus hedefi, üretim etkisi ve blocker hiyerarşisi uygulanıp doğrulandı |
| 5 | `DW-P5-ARROWS` | Arrow Supply arayüzünün yeniden çalışılması | `[x]` | Stok, tüketim, refill paketleri ve kapasite/verimlilik bilgi mimarisi uygulanıp doğrulandı |
| 6 | `DW-P6-TECH-TREE` | Teknoloji ağacının görsel ve kullanılabilirlik yeniden çalışması | `[x]` | Ayrı War Doctrine yüzeyi kaldırıldı; player-facing teknoloji ağacı sahipliği Castle Heart altında tekilleştirildi |
| 7 | `DW-P7-CASTLE-HEART` | Castle Heart'ın ayrı kapsamda yeniden değerlendirilmesi | `[x]` | UI Toolkit icon-tree, direct-child reveal, responsive inspector ve normal teknoloji ağacı davranışı uygulandı/doğrulandı |
| 8 | `DW-P8-UI-POLISH` | Genel UI efektleri, animasyonları ve etkileşim polish'i | `[x]` | Durum odaklı hareket, input uyarlaması, feedback katmanları ve modal tutarlılığı doğrulandı |
| 9 | `DW-P9-PERFORMANCE` | Performans profilleme ve optimizasyon | `[x]` | Player zombie-limit ayarı, yoğun feedback batching/pooling ve 10K + 1K yeniden ölçümü tamamlandı |
| 10 | `DW-P10-FINAL-GATE` | Son kalite, tutarlılık ve regresyon kapısı | `[x]` | Bütün aktif kapsam birlikte test edildi; açık kritik hata ve doğrulanmamış ana görev kalmadı |
| 11 | `DW-P11-NIGHT-RHYTHM` | Night-only saldırı, clear gate ve günlük Council ritmi | `[x]` | Night spawn/clearance, günlük event, HUD, exact Continue ve tam regresyon doğrulandı |
| 12 | `DW-P12-UI-READABILITY` | Gameplay HUD okunabilirlik, worker slider ve terminoloji düzeltmesi | `[x]` | UXML/USS/C# kontratı, runtime yeniden dağıtım, onboarding ve canlı Game View doğrulandı |
| 13 | `DW-P13-GAME-FLOW-CONTROLS` | 1X/2X/3X oyun hızı, Council pause ve toast altyapısı | `[x]` | Merkezi speed/pause owner, bounded toast kuyruğu, testler ve canlı Game View doğrulandı |
| 14 | `DW-P14-ACTION-FEEDBACK` | Game Over meta açıklığı ve exact action-failure toast'ları | `[x]` | Exact effect progression, açıklanabilir action state, hedefli test ve canlı Game View doğrulandı |
| 15 | `DW-P15-TOAST-AUDIO-POLISH` | Süreli toast stack'i ve UI Toolkit button sesleri | `[x]` | Üç kartlık tekrar korunumu, otomatik dismiss, merkezi click audio ve hedefli runtime testleri doğrulandı |
| 16 | `DW-P16-GUIDED-ONBOARDING` | UI Toolkit gerçek kontrolleriyle adım adım first-run öğretimi | `[x]` | Gerçek-control spotlight/input gate, Play-session sıra, contextual tip'ler ve hedefli regresyon doğrulandı |
| 17 | `DW-P17-ARROW-DELIVERY` | Üç saniyelik Arrow teslimatı ve canlı stok barı | `[x]` | Sipariş 3 saniye kullanılamaz kaldı, süre sonunda atomik geldi; pause, save, tutorial, 1K Archer ve UI sözleşmesi doğrulandı |
| 18 | `DW-P18-IOS-DISTRIBUTION` | Game Center, iOS imzalama, App Store ikonu ve TestFlight hazırlığı | `[~]` | Repo entegrasyonu ve owner-beyanlı TestFlight upload mevcut; internal kurulum ile fiziksel cihaz Game Center testi bekliyor |

---

## 4. Görev Görüşme ve Uygulama Kayıtları

### `DW-P1-LOGIC` - Mantık Hataları

**Durum:** `[x]` Owner bilinen mantık sorunu olmadığını kesinleştirdi.  
**Kural:** Oyunda bulunmayan sistemler veya varsayımsal problemler bu bölüme eklenmez. Gerçek oyun akışı ve gözlenen davranışlar üzerinden tek tek karar verilir.

#### Karar kaydı

- [x] Owner, mevcut oyunda bildirilecek bir mantık sorunu olmadığını belirtti.
- [x] Oyunda bulunmayan veya varsayımsal sorunlar tracker'a eklenmedi.
- [x] P1 başlangıç regresyonu EditMode `408/408`, PlayMode `89 pass + 2 explicit skip` geçti.

#### Uygulama ve doğrulama

- [x] Onaylanmış hata listesi oluşmadığı için P1 kod değişikliği üretmedi.
- [x] Bu kapanış gelecekte bulunan gerçek bir mantık hatasının tracker'a eklenmesini engellemez.

### `DW-P2-COMBAT-FEEDBACK` - Soul ve Hasar Görünürlüğü

**Paket başlığı:** `DW-P2-COMBAT-FEEDBACK: Soul Pickup + Player Damage Numbers`  
**Durum:** `[x]` Uygulandı ve tam regresyonla doğrulandı.

#### Kilitli kapsam

- [x] Her Skeleton ölümü tam olarak `1 Soul` verir.
- [x] Soul gameplay state'ine ölüm anında yazılır; death-animation cleanup ikinci kez eklemez.
- [x] Soul görseli ölüm noktasından HUD Soul göstergesine gider.
- [x] HUD göstergesi görsel varışta pulse geri bildirimi verir.
- [x] Basic/Rapid/Frost Arrow gerçek uygulanan hasarı düşmanın kafasında gösterir.
- [x] Fireball, ikincil patlama ve Burning Ground gerçek uygulanan hasarı düşmanın kafasında gösterir.
- [x] Overkill sayısı mevcut HP'ye clamp edilir; gösterilen değer gerçekten düşen HP ile eşleşir.

#### Uygulanan sunum ve performans sınırı

- [x] Soul runtime radial cyan icon, `+1`, bezier yolculuk ve sayaç pulse kullanır.
- [x] Hasar sayıları pooled world-space TMP, source-color, punch-rise-fade dili kullanır.
- [x] Damage-number event'leri VFX sampling budget'i tarafından düşürülmez.
- [x] Hasar ve Soul görselleri tamamlandığında pool'a geri döner; başlangıç pool'u yetmezse büyür.
- [x] Targeted EditMode `18/18` ve PlayMode `2/2` geçti.
- [x] Tam regresyon EditMode `420/420`; PlayMode `91 pass + 2 explicit skip` geçti.
- **P9 notu:** 10K + 1K profiler capture ve long-run soak, P2 kapanışını engellemeden ana performans görevinin explicit test kapısı olarak korunur.

### `DW-P3-UIUX-AUDIT` - Tüm Oyuncu UI/UX Denetimi

**Paket başlığı:** `DW-P3-UIUX-AUDIT: Player-Facing UI/UX Audit`  
**Durum:** `[x]` Araştırma, karar sistemi, tam UI Toolkit uygulaması ve regresyon doğrulaması tamamlandı.

- [x] Oyuncunun görebildiği bütün UI yüzeyleri envanterlendi.
- [x] Her yüzey amaç, sıklık, önem, ekran konumu ve etkileşim maliyeti açısından değerlendirildi.
- [x] Benzer oyunlar ile başarılı PC/mobile UI/UX örnekleri araştırıldı.
- [x] Dead Walls için minimal, okunabilir ve bilgi öncelikli UI Toolkit yönü owner tarafından onaylandı.
- [x] Bilimsel yöntem, karşılaştırmalı sentez, bilgi mimarisi, erişilebilirlik ve runtime kararları `DEAD_WALLS_UI_UX_DECISION_SYSTEM.pdf` içinde kalıcılaştırıldı.

#### Onaylı global yön

- [x] Oyuncunun gördüğü bütün UI yüzeyleri UI Toolkit'e geçirildi; legacy UGUI yalnız davranış/veri köprüsü olarak tutuldu.
- [x] PC ve mobile aynı sürümde desteklenecek; kontrol sunumu aktif cihaza göre otomatik uyarlanacak.
- [x] Referans çalışma çözünürlüğü `1920x1080`; bütün player-facing metin ilk sürümde İngilizce.
- [x] Fullscreen yönetim yüzeyleri açıkken simülasyon akmaya devam edecek.
- [x] Görsel dil minimal, yüksek okunabilirlikli ve dekoratif hareketten arındırılmış olacak; animasyon yalnızca durum veya bilgi değişimini anlatacak.
- [x] Ayrı `War Doctrine / Technology` yüzeyi kaldırıldı; player-facing teknoloji ağacı yalnız Castle Heart altında yaşayacak.
- [x] Castle Heart görsel ve etkileşim rework'ü oyunun tek player-facing teknoloji ağacı olarak tamamlandı.

#### Yüzey uygulama kaydı

- [x] Main Menu UI Toolkit'e geçirildi; legacy Canvas render sahibi olmaktan çıkarıldı.
- [x] Main Menu'de yalnızca `CONTINUE/NEW GAME` save durumuna göre gösteriliyor ve `SETTINGS` akışı korunuyor.
- [x] Başlık çizgisi, buton yan çizgileri, day etiketi, input ipucu ve version etiketi kaldırıldı.
- [x] Main Menu'nün tek sürekli hareketi `22s DAY / 8s DUSK / 22s NIGHT / 8s DAWN` arka plan döngüsü.
- [x] Main Menu Unity compile, Console ve Play Mode görsel ağacında doğrulandı; owner tarafından onaylandı.
- [x] Persistent HUD, Economy, Barracks, Arrow Supply, Council, Level Up, Pause, Settings ve Game Over/Meta Shop UI Toolkit ile sıfırdan kuruldu.
- [x] Artsystack ikonları genel HUD/yönetim yüzeylerinde; görsel olarak incelenen `RPG Icons Pixel Art` seçkisi yalnız Castle Heart'ın 37 teknoloji node'unda kullanıldı.
- [x] `Worker Production` ve `Repair Gate` birbirinden ayrıldı: worker üretimi yumruk, kapı onarımı tools ikonunu kullanır.
- [x] Gameplay gün döngüsü legacy celestial-arc davranışı referans alınarak UI Toolkit'te sıfırdan, gerçek cycle progress ve phase durumlarına bağlı daha polish bir sunumla kuruldu.
- [x] Castle Heart UI Toolkit final rework'ü; dört başlangıç node'u, hidden-node yokluğu, direct-child reveal ve responsive inspector ile production'a alındı.
- [x] `1920x1080` ana yüzey QA'sı ve `1280x720` touch-mode uyarlaması canlı Play Mode'da incelendi.
- [x] Son ikon/celestial-arc revizyonunda Unity compile ve Console temiz; targeted UI kontratları `6/6`, tam EditMode `426/426` geçti.

### `DW-P4-WORKERS` - Worker Dağıtım Arayüzü

**Durum:** `[x]` Economy drawer ve worker dağıtım akışı UI Toolkit ile yeniden kuruldu.

- [x] Her kaynakta mevcut çalışan/cap, üretim oranı ve kalan idle nüfus birlikte okunur.
- [x] Mevcut idle nüfus `-10 / -1 / +1 / +10 / FILL` ile doğrudan atanır; yeni gelen nüfusun otomatik dağılım hedefi ayrı kontrollerde tutulur.
- [x] Capacity ve Efficiency geliştirmeleri doğrudan worker atama kararından görsel olarak ayrıştırıldı.
- [x] Visual-tree yeniden kurulduğunda dört worker satırı ve bütün callback'ler yeniden oluşturulur.
- [x] Live runtime işlem testi worker sayısını `20 -> 10 -> 20`, idle nüfusu `3 -> 13 -> 3` değiştirdi.

### `DW-P5-ARROWS` - Arrow Supply Arayüzü

**Durum:** `[x]` Arrow Supply drawer UI Toolkit ile yeniden kuruldu.

- [x] Mevcut stok, kapasite ve savaş tüketim baskısı üst hiyerarşide gösterilir.
- [x] Refill paketleri cost/result/blocker kontratıyla sunulur.
- [x] Capacity ve Efficiency kararları anlık satın alma seçeneklerinden ayrıştırıldı.
- [x] Live satın alma davranışı ve görsel durumları doğrulandı.

### `DW-P6-TECH-TREE` - Teknoloji Ağacı

**Durum:** `[x]` Yanlış ürün ayrımı düzeltilerek ayrı War Doctrine teknoloji yüzeyi production UI'dan kaldırıldı.

- [x] Command rail'deki `TECHNOLOGY` butonu ve `techScreen` production UXML'den kaldırıldı.
- [x] Castle Heart dışında ikinci bir player-facing teknoloji ağacı kalmadığı canlı visual-tree üzerinde doğrulandı.
- [x] Run-tech runtime data ve gameplay etkileri silinmedi; Castle Heart final rework'ünde kullanılmak üzere davranış katmanında korundu.
- [x] Game Over açılırken açık drawer/fullscreen yüzey zorunlu kapanır; arkada teknoloji veya yönetim yüzeyi açık kalmaz.

### `DW-P7-CASTLE-HEART` - Ayrı Kapsam

**Durum:** `[x]` UI Toolkit final rework tamamlandı ve runtime davranışı doğrulandı.

- [x] Castle Heart oyunun tek player-facing teknoloji ağacı; ikinci War Doctrine yüzeyi yok.
- [x] Merkez Heart ve tam dört başlangıç teknolojisi dışında hidden node/placeholder/silhouette çizilmiyor.
- [x] Satın alma yalnız doğrudan outgoing child/child'ları reveal ediyor; sibling lock veya cascade yok.
- [x] Noktalı-eğrisel bağlantı parent'tan child'a büyüyor; child icon opacity/scale ile ve çoklu child durumunda stagger ile beliriyor.
- [x] Bütün 37 node görsel incelemeyle seçilmiş `RPG Icons Pixel Art` sprite'larına bağlandı; future catalog rebuild aynı map'i koruyor.
- [x] PC right-inspector ve compact/touch bottom-inspector canlı Game View'da doğrulandı; simulation akmaya devam ediyor.
- [x] Graph navigation; PC mouse-wheel, mobile pinch, boş alandan pan ve ortak `- / yüzde / + / FIT` kontrolleriyle görünür bounds güvenli biçimde yakınlaştırılıp uzaklaştırılabiliyor.
- [x] Player-facing akış yalnız `RESEARCH / UPGRADE / RESEARCHED` kullanıyor; branch etiketi, `+10/MAX` ve eski Keystone jargon/lock davranışı yok.
- [x] Targeted EditMode `32/32` ve PlayMode `2/2` geçti; Unity compilation ve Console hata vermedi; canlı başlangıç/purchase visual node ölçümü `5 -> 6` oldu.

### `DW-P8-UI-POLISH` - Genel UI Efekt ve Animasyonları

**Durum:** `[x]` Bilgi odaklı UI polish sistemi bütün yeni yüzeylere uygulandı.

- [x] Hover/focus/press, seçili durum, cooldown, satın alma başarısı ve blocker feedback'i ortak dil kullanır.
- [x] Soul pickup uçuşu, sayaç varış pulse'ı, damage flash, critical banner, toast ve onboarding UI Toolkit katmanına taşındı.
- [x] Gereksiz sürekli dekoratif hareket kaldırıldı; Main Menu'de yalnız day/dusk/night/dawn arka plan döngüsü kaldı.
- [x] Gameplay celestial arc, gerçek gün progress'ini izleyen hareketli gök cismi, dört phase işareti ve gün/gece durum renkleriyle yeniden polish edildi.
- [x] Alt day/dusk/night/dawn rail'i aktif faz boyunca gerçek `PhaseProgress01` ile sürekli dolar; ikon opacity/scale değişimleri yumuşak geçiş kullanır.
- [x] Persistent HUD resource toplamları ile `/m` üretim oranları ayrı dikey bantlara alınarak üç haneli değerlerdeki overlap kaldırıldı.
- [x] Onaylı Artsystack ikonları kaynak, population, combat, drawer, ability, pause/settings ve Game Over/Meta Shop yüzeylerine işlevsel hiyerarşiyi güçlendirecek şekilde uygulandı.
- [x] Modal hiyerarşi, scroll görünürlüğü, touch hit-area ve gamepad focus davranışı doğrulandı.

### `DW-P9-PERFORMANCE` - Ölçüm ve Optimizasyon

**Durum:** `[x]` Ölçülmüş yoğun feedback darboğazları giderildi; player aktif-zombi limiti ve no-despawn backlog sözleşmesi uygulandı.

- [x] PC hedef kanıtı `i5-14400F / Intel Arc B580 / 1080p Ultra` instrumentation-kapalı Player kabulü; fresh P9 ölçümü aynı 10K enemy + 1K Archer Editor benchmark'ıdır. Mobil için sahte target-hardware sertifikası yazılmadı.
- [x] CPU/main-thread, frame pacing, draw calls, root GC, kullanılan bellek ve yoğun UI/combat ölümü yeniden ölçüldü; mevcut target-hardware Player sonucu ayrıca korundu.
- [x] Event başına TMP, legacy Soul GameObject'i ve UI Toolkit Soul elementi üreten kanıtlanmış yoğun-burst darboğazları batched mesh, konumsal toplulaştırma ve element pooling ile giderildi.
- [x] Küçük damage/Soul burst'leri birebir kaldı; yoğun burst'lerde event sayısı, damage toplamı, Soul toplamı, kaynak rengi ve konumsal temsil testlerle kayıpsız doğrulandı.
- [x] Main Menu ve Pause Settings aynı persistent `900 / 2.000 / 5.000 / 10.000` zombie-limit preset'lerini sunar; düşük limitin performans/density trade-off'u player'a açıklanır.
- [x] Koşu sırasında limit düşürmek yaşayan zombileri despawn etmez; yeni spawn demand'i aktif sayı limitin altına inene kadar exact backlog'da bekler.
- [x] Barracks UI backend hard cap'i korurken `/ 1000` değerini player'a göstermez; deployed toplamı ve yalnız gerçek cap'te `GARRISON FULL` gösterir.
- [x] 10K + 1K Editor benchmark'ında main-thread ortalaması `25,47 -> 13,12 ms`, frame P95 `48,00 -> 17,83 ms`, 10K Fireball death peak'i `13.786 -> 63,11 ms` oldu; test exact pool/Continue/backlog kanıtlarıyla geçti.
- [x] Targeted EditMode `19/19`, targeted PlayMode `6/6`, Unity compilation, Console ve `git diff --check` temiz geçti.

### `DW-P10-FINAL-GATE` - Son Kalite Kapısı

**Durum:** `[x]` Tam regresyon, bütün player-facing UI yüzeyleri, ağır performans/soak ve exact Continue kapıları birlikte doğrulandı.

- [x] Mantık düzeltmeleri için regresyon matrisi tamamlandı: final EditMode `440/440`, final PlayMode `95 pass + 2 explicit skip`, `0 fail`.
- [x] Oyuncunun gördüğü Main Menu, Settings, gameplay HUD, Economy, Barracks, Arrow Supply, Castle Heart, Pause ve Game Over/Meta Shop yüzeyleri `1920x1080` PC ile compact/touch düzenlerinde birlikte gözden geçirildi.
- [x] Yoğun combat, uzun süreli oyun ve save/continue senaryoları doğrulandı: explicit 3.600-frame soak `1/1`; 10K Zombie + 1K Archer benchmark, Fireball, pool, backlog ve çift Continue fingerprint testi `1/1` geçti.
- [x] Temiz production boot Console'da `0 error` verdi; player-facing geçici placeholder veya ikinci teknoloji ağacı yüzeyi bulunmadı, aktif teknoloji sahipliği Castle Heart altında tek kaldı.
- [x] Target-hardware profiler sertifikası yalnız Windows Player ortamını kabul edecek şekilde korumaya alındı; mevcut `1920x1080 / Ultra` WindowsPlayer raporları final kabul kanıtı olarak korundu.
- [x] Son kapsam diff'i incelendi ve `git diff --check` temiz geçti.

### `DW-P11-NIGHT-RHYTHM` - Night-Only Assault + Clear Gate + Daily Council

**Paket başlığı:** `DW-P11-NIGHT-RHYTHM: Night-Only Assault + Clear Gate + Daily Council`
**Durum:** `[x]` Uygulandı, exact save/Continue ve tam regresyonla doğrulandı.

- [x] Day, Dusk ve Dawn yeni düşman talebi üretmez; Night timed bölümü mevcut quantity/intensity sistemini kullanır.
- [x] Timed Night bitince yeni talep durur; pending Night backlog'u clearance sırasında kapasite açıldıkça sahaya aktarılır.
- [x] `PendingEnemies` veya `ZombiesAlive` sıfırdan büyükken cycle Night sonunda tutulur; ikisi de sıfır olduğunda Dawn'a geçer.
- [x] UI Toolkit HUD clearance sırasında normal `NIGHT SIEGE` başlığını korur; `N ENEMIES LEFT` ve `CLEAR ENEMIES TO REACH DAWN` ile temizlenme durumunu gösterir.
- [x] Regular Council Day 1'den başlayarak her Dawn'da tam bir kez açılır; chance/pity/cooldown ve Emergency Council yolu eklenmedi.
- [x] Day 1 için temel ekonomi template'leri eligible oldu; diğer template'lerin `MinDay`, flag, curated chain ve deterministic composer kuralları korundu.
- [x] Exact save/Continue, clearance anındaki sıfır intensity'yi ve pending backlog'u değiştirmeden geri yükler.
- [x] Targeted EditMode `63/63`, targeted PlayMode `10/10`, full EditMode `447/447`, full PlayMode `96 pass + 2 explicit skip / 0 fail` geçti.
- [x] `NewGameScene` validation `0` issue, final Unity Console `0 error / 0 warning` ve `git diff --check` temiz geçti.

### `DW-P12-UI-READABILITY` - Gameplay HUD Readability + Terminology

**Paket başlığı:** `DW-P12-UI-READABILITY: Gameplay HUD Readability + Explicit Terminology`
**Durum:** `[x]` Uygulama, otomatik kontrat, runtime davranış ve canlı Game View görsel kabulü tamamlandı.

- [x] Gameplay HUD stilinde `7px`, `8px` veya `9px` player-facing font tanımı bırakılmadı; USS kontratı authored `10px` tabanını kilitler.
- [x] Night clearance mesajı `11px`, yüksek kontrast ve daha açık copy ile güncellendi; phase sayacı kalan nesnenin düşman olduğunu açıkça söyler.
- [x] Kaynak oranı, worker hedefi, archer damage, run-only upgrade, Castle Heart araştırması ve Last Embers projection metinleri player-facing jargon/kısaltma bırakmayacak şekilde güncellendi.
- [x] Rally hazır durumu görünür biçimde `BOOST FIRE RATE` der; üç ability düğmesi doğru davranış tooltip'i taşır.
- [x] Economy drawer'daki `-10/-1/+1/+10/FILL` worker komutları kaldırıldı; dört kaynak `0-100%` share slider'ı üzerinden mevcut worker'ları ve yeni arrival hedefini tek işlemde yeniden dağıtır.
- [x] Bir share slider'ı `%100` olduğunda diğer üç kaynak target'ı `0` olur; archer olmayan bütün nüfus işe atanır, seçilen resource cap'e çarparsa overflow worker Wood -> Stone -> Iron -> Food sırasındaki ilk boş kapasiteye taşınır.
- [x] Ayrı asker rezervi tutulmaz. Basic/Rapid/Frost satın alımı ve Council free-archer sonucu gerekli kişiyi Wood -> Stone -> Iron -> Food sırasıyla resource worker havuzundan Archer'a çevirir.
- [x] Player-facing `IDLE PEOPLE` yerine `UNASSIGNED` kullanılır; değer yalnız dört resource'un toplam kapasitesi tamamen doluysa pozitif kalır ve resource satırlarında küresel sayı olarak tekrarlanmaz.
- [x] Housing CTA resource listesinin üstüne taşındı, full-capacity durumu güçlendirildi ve `ADD 1/10/100 BEDS` paketlerinin her biri tam maliyetini gösterir.
- [x] Toolkit maliyetleri `150W 100I` gibi kısaltmalar yerine `150 WOOD · 100 IRON` biçiminde açık kaynak adları kullanır.
- [x] Targeted EditMode `39/39` (`GameplayHUDToolkitContractTests` + `WorkerAllocationUtilityTests` + `FirstRunOnboardingTests`) ve targeted PlayMode worker-slider entegrasyonu `1/1` geçti.
- [x] Unity asset import/domain reload tamamlandı; değişiklik kapsamındaki final Console `0 error / 0 warning` ve `git diff --check` temizdir.
- [x] `NewGameScene` üzerinde `1920x1080` normal ve zorlanmış compact/touch canlı Game View kontrollerinde worker slider, housing paketleri, tam kaynak adları, overlap ve clipping doğrulandı.
- [x] Post-acceptance workforce düzeltmesi targeted EditMode `36/36`, targeted PlayMode `7/7`, exact Continue, archer cap ve telemetry regresyonlarıyla geçti.
- [x] Canlı runtime başlangıcında `60 total = 4 archers + 56 workers + 0 unassigned`; `%100 WOOD` sonucunda Wood `40/40`, Stone `16/30 CAPACITY OVERFLOW`, Iron/Food `0` ve `0 unassigned` doğrulandı.
- [x] Canlı Basic Archer alımında `4 -> 5 archers`, `56 -> 55 workers`, Wood `40 -> 39` ve Unassigned `0 -> 0` oldu.
- [x] Canlı kabul Play Mode içindeki geçici `NewGameScene` yüklemesiyle yapıldı; çıkışta aktif `HandMadeTiles` sahnesi geri yüklendi ve owner sahne dosyaları değiştirilmedi.

### `DW-P13-GAME-FLOW-CONTROLS` - Game Speed + Council Pause + Toast Infrastructure

**Paket başlığı:** `DW-P13-GAME-FLOW-CONTROLS: 1X/2X/3X Speed + Council Pause + Toast Queue`
**Durum:** `[x]` Uygulama, targeted regresyon ve canlı Game View doğrulaması tamamlandı.

- [x] Gün döngüsü panelinin altına, aynı görsel dilde ve açık aktif-state metniyle `1X/2X/3X` kontrolleri eklendi.
- [x] Player-facing hız yazımı `SimulationPauseService` altında merkezileştirildi; desteklenmeyen hız reddedilir ve legacy Canvas ikinci bir kontrol yüzeyi taşımaz.
- [x] Pause lease'i seçili koşu hızını ve ECS simulation state'ini birlikte yakalar; nested lease'lerde yalnız son release exact state'i geri yükler.
- [x] Aktif Council kararı `CouncilDecision` lease'i alır. Geçerli seçim kartı kapatıp önceki hızı geri yükler; uygulanamayan seçim kartı ve pause'u açık tutar.
- [x] Pause altında donacak geri sayım kaldırıldı; karar şeridi `GAME PAUSED · CHOOSE TO CONTINUE` metniyle dolu kalır.
- [x] İlk Council exact-choice onboarding hint'i blocking Council pause altında görünür kalır; tutorial pause lease sahipliği almaz.
- [x] `GameplayToastService` en fazla sekiz mesajlık FIFO, süre clamp'i ve dört ton metadata'sıyla eklendi; presenter unscaled zamanda çalışır.
- [x] Mevcut UI aksiyon feedback çağrıları ve legacy feedback mirror'ları kuyruğa taşındı. Yeni otomatik gameplay toast trigger'i eklenmedi; sonraki kaynaklar owner kararı bekler.
- [x] Targeted EditMode `50/50`, Council PlayMode `9/9`, `NewGameScene` validation `0` issue ve final Console `0 error / 0 warning` geçti.
- [x] Canlı `1920x1080` Game View'da `3X ACTIVE`, Council sırasında `PAUSED - 3X`, seçim sonrası yeniden `3X` ve pause altında geçici toast preview doğrulandı; aktif `HandMadeTiles` sahnesi geri yüklendi.

### `DW-P14-ACTION-FEEDBACK` - Meta Upgrade Clarity + Exact Failure Toasts

**Paket başlığı:** `DW-P14-ACTION-FEEDBACK: Meta Benefit Progression + Exact Failure Toasts`
**Durum:** `[x]` Uygulama, hedefli regresyon ve canlı Game View doğrulaması tamamlandı.

- [x] Game Over meta kartları katalog açıklamasının yanında exact `CURRENT → NEXT` kalıcı faydayı,
  `LEVEL N → N+1` transaction'ını ve açık `BUY · N EMBERS` maliyetini gösterir.
- [x] Maximum seviyedeki kart olmayan bir sonraki faydayı vaat etmez; mevcut toplamı
  `MAXIMUM ACTIVE` olarak gösterir. Heart content unlock internal pool dili yerine gelecekteki
  Castle Heart seçeneklerini açtığını söyler.
- [x] Economy bina/housing, Archer recruit/retrain, Arrow refill/upgrade, War Doctrine, Castle Heart
  ve Game Over meta purchase reddi exact warning toast kapsamına bağlandı.
- [x] Terminal olmayan yetersiz action'lar sessizce disable edilmez; `is-action-unavailable`
  sunumuyla tıklanabilir kalır. Tamamlanmış/maxed action'lar terminal disabled state'ini korur.
- [x] Aynı sahnede bulunan UI Toolkit Barracks ve legacy `MarketUI` Archer callback'leri aynı
  açıklanabilir-action sözleşmesini kullanır; kaynak/worker eksikliği iki yüzeyde de butonu disable etmez.
- [x] Kaynak, worker, garrison capacity, locked type, full reserve, prerequisite, maximum level ve
  durable meta save engelleri ayrı nedenlerdir; tam kaynak adları ve exact eksik miktarlar kullanılır.
- [x] Player-facing action-failure toast'ları yalnız İngilizce presentation copy'si kullanır;
  Castle Heart/War Doctrine internal transaction veya reason mesajlarını doğrudan göstermez.
- [x] Hedefli EditMode `23/23` ve hem UI Toolkit hem legacy Market Basic Archer düğmelerini kullanan
  hedefli PlayMode `2/2` geçti. İlgili compile/Console kontrolünde `0 error` görüldü.
- [x] Canlı `1920x1080` Game Over/Meta Shop kontrolünde beş satırlık görünür alanda açıklama,
  exact fayda, level ve maliyet taşmadan okundu; aktif `HandMadeTiles` sahnesi geri yüklendi.

### `DW-P15-TOAST-AUDIO-POLISH` - Timed Toast Stack + UI Toolkit Button Audio

**Paket başlığı:** `DW-P15-TOAST-AUDIO-POLISH: Timed Action Stack + Toolkit Click Audio`
**Durum:** `[x]` Uygulama ve hedefli Unity regresyonu tamamlandı.

- [x] Tek-label presenter kaldırıldı; aynı anda en fazla üç dinamik toast kartı sunan bounded stack
  eklendi. Yeni kart altta görünür, eski kartlar yukarı taşınır ve dördüncü kart en eskiyi düşürür.
- [x] Tekrarlanan aynı mesajlar ayrı player action'ları olarak korunur. Varsayılan kart `2.4`, warning
  kartı `3.2` saniye görünür; `180 ms` exit transition'i sonunda hierarchy'den kaldırılır.
- [x] Toast lifecycle'ı unscaled zamanda çalışır ve stack pointer/raycast almaz.
- [x] UI Toolkit root `ClickEvent` route'u gerçek Button hedeflerini merkezi `UiSoundFeedback` click
  kanalına bağlar. Modal içindeki eski tekil çağrı kaldırılarak çift ses engellendi.
- [x] Yeni otomatik gameplay toast trigger'i eklenmedi; P14 action-failure kaynakları ve İngilizce
  player-facing copy sınırı korundu.
- [x] Hedefli EditMode `22/22`; UI Toolkit + legacy Archer runtime senaryolarını kullanan hedefli
  PlayMode `2/2` geçti. Runtime test üç ayrı warning kartını, görünür state'i, otomatik kaldırmayı ve
  gerçek UI AudioSource playback'ini doğruladı.

### `DW-P16-GUIDED-ONBOARDING` - UI Toolkit Guided First Run

**Paket başlığı:** `DW-P16-GUIDED-ONBOARDING: Real-Control First-Run Guidance`
**Durum:** `[x]` Owner onaylı exact sıra uygulandı ve gerçek UI Toolkit action zinciriyle doğrulandı.

- [x] Mevcut owner zinciri çıkarıldı: `FirstRunOnboardingUI` yedi condition-driven adımı yönetiyor,
  fakat yeni UI Toolkit yalnız gizli Canvas hint metnini aynalıyor; pulse hedefleri legacy kontrollerde kalıyor.
- [x] Basic Archer alınabilir olduğunda flag tamamlanana kadar açık kalan legacy affordability cue'su
  UI Toolkit HUD'dan ayrıldı. Gerçek Archer satın alımı tutorial flag'ini yalnız mevcut Play oturumuna yazar.
- [x] Dar düzeltme hedefli EditMode `1/1`, gerçek ilk-gün onboarding PlayMode `1/1` ve Unity compile
  `0 error` ile doğrulandı.
- [x] İlk zorunlu click zinciri `ECONOMY -> worker-share slider -> CLOSE -> BARRACKS -> BASIC ARCHER -> 2X`
  olarak owner tarafından kesinleştirildi.
- [x] İlk altı core adımda yalnız hedef kontrol etkileşim alacak; ekran hedef dışında kararacak ve
  zincir yalnız gerçek başarılı player action'ıyla ilerleyecek.
- [x] First Night Rally, first Council exact choice, low-arrow refill, first post-combat Essence
  Castle Heart, population-full housing ve first wall-damage repair contextual adımlar olarak
  kesinleştirildi; unrelated kontrol kilitlemeyecekler.
- [x] `GuidedOnboardingProgress` exact core/contextual kuralları, English copy ve ayrı
  `tutorial.v2.*` session flag'lerini taşır. Settings reset v1 ve v2 flag'lerini yalnız mevcut
  Play oturumunda aynı işlemde temizler; eski save içindeki tutorial flag'leri okunmaz.
- [x] UI Toolkit presenter dört dim rect, real-control focus ve bilgi kartını üretir. Core adımda
  root trickle-down input gate yalnız gerçek hedef subtree'sini geçirir; contextual tip unrelated
  action'ları kilitlemez ve overlay elementlerinin tamamı raycast dışıdır.
- [x] Görünür core/contextual guided adım merkezi `SimulationPauseService` üzerinden ayrı lease alır.
  Core lease Economy'den 2X completion'a kadar kesintisiz kalır; son adım seçilen `2X` running speed'i
  ile resume eder. Contextual action tamamlanınca önceki running speed geri gelir.
- [x] Housing ve Arrow contextual eligibility'si gerçek satın alınabilirlik ile sınırlandı; pause
  altında resource accumulation bekleyen görünmez soft-lock yolu kapatıldı.
- [x] Focus rect `Time.unscaledTime` ile padding, opacity ve border width nefes pulse'i uygular.
  Kart genişletildi; pause durumu, sonraki action ve sistem gerekçesi English copy ile açıklaştırıldı.
- [x] Drawer open completion yalnız player callback wrapper'ından; slider, Archer, speed, Rally,
  Council, Arrow, Castle Heart, housing ve repair completion yalnız authoritative başarılı işlem
  sonucundan yazılır. Programmatic drawer açma ve başarısız action progress üretmez.
- [x] Worker slider sonrasında ayrı session-scoped `tutorial.v2.economy_close` adımı gerçek `economyClose`
  butonunu hedefler. Economy açıkken Barracks input'u kesilir; Barracks adımı yalnız drawer gerçek
  player Close action'ıyla kapandıktan sonra başlar.
- [x] Hedefli EditMode `45/45`; gerçek core/raycast zinciri, P15 Archer toast regresyonları,
  Settings reset ve ikinci-run suppression dahil hedefli PlayMode `5/5` geçti. Unity compile
  `0 error`, final Console ve `git diff --check` temiz doğrulandı.

### `DW-P17-ARROW-DELIVERY` - Three-Second Supply Delivery + Live Reserve Bar

**Paket başlığı:** `DW-P17-ARROW-DELIVERY: Three-Second Supply Delivery + Live Reserve Bar`
**Durum:** `[x]` Owner onaylı 3 saniyelik pending + atomik stok teslimatı uygulandı ve hedefli regresyonla doğrulandı.

- [x] `GameManager.ArrowDelivery`, tek aktif refill'in toplam miktarını ve geçen simulation
  süresini taşır. Süre boyunca canlı ECS `ArrowSupply.Current` değerine yazmaz; tamamlanınca
  siparişin tamamını o andaki stoğa atomik ekleyerek eşzamanlı mevcut-stok tüketimini ezmez.
- [x] Wood başarılı purchase transaction'ında hemen düşer; satın alınan Arrow miktarı
  `ArrowEconomyUtility.RefillDeliveryDurationSeconds = 3` süresince kullanılamaz kalır.
- [x] Pause altında teslimat ilerlemez. Oyun hızı scaled `Time.deltaTime` üzerinden teslimata
  uygulanır; `2X/3X` seçimleri teslimatı aynı oranda hızlandırır.
- [x] UI Toolkit supply drawer sayısal gerçek stoğu değiştirmeden gösterir; teslimatta altın bar,
  mevcut oran ile sipariş sonrası oran arasında ilerler, `DELIVERING · Ns` state'i ve `0,15s`
  width transition'i kullanır.
- [x] Aktif teslimat sırasında yeni package/Buy Max transaction'ı reddedilir ve
  `SUPPLY DELIVERY IN PROGRESS · Ns REMAINING` warning toast'ı gösterilir.
- [x] Snapshot aktif teslimatı önce stoka flush eder; Wood harcanıp kalan Arrow'un save dışında
  kaybolması engellenir. Restart bekleyen teslimat state'ini temizler.
- [x] Hedefli EditMode, birleşik kritik PlayMode ve tam `ArrowAmmoPlayModeTests` sonuçları
  atomik teslimat regresyonlarıyla yeniden doğrulandı. Güncel sayılar aşağıdaki journal
  girdisindedir.

### `DW-P18-IOS-DISTRIBUTION` - Game Center + TestFlight Hazırlığı

**Paket başlığı:** `DW-P18-IOS-DISTRIBUTION: Game Center + iOS Distribution Readiness`
**Durum:** `[~]` Repo entegrasyonu ve Mac dağıtım zinciri kayıtlı; internal TestFlight kurulumu
ile fiziksel cihaz Game Center doğrulaması bekliyor.

- [x] Apple Core `3.2.0` ve Apple GameKit `4.0.1` local tarball paketleri projede bulunur;
  canlı Unity MCP her iki paketi de aynı sürümlerle `LocalTarball` kaynağından çözüyor.
- [x] `GameCenterService`, scene-independent `BeforeSceneLoad` başlangıcında iOS local player
  authentication çağrısını yapar; authentication state/event yüzeyini ve native dashboard
  açma API'sini tek owner olarak sunar.
- [x] `GameCenterService` native GameKit kodunu yalnız `UNITY_IOS && !UNITY_EDITOR` altında
  derler; Editor ve Apple dışı platformlarda no-op kalır.
- [x] `DeadWalls.asmdef`, `Apple.Core` ve `Apple.GameKit` assembly referanslarını taşır.
- [x] `DefaultAppleBuildProfile.asset`, Apple GameKit build step'ini ve otomatik entitlement
  üretimini etkin tutar; macOS App Sandbox entitlement'ı kapalıdır.
- [x] Unity Project Settings içinde şirket `PixiCorp`, ürün `Zombie Castle`, Bundle ID
  `com.pixicorp.zombiecastle`, Team ID `7JVZGHB5S5`, sürüm `1.0` ve iOS ikon slotları
  serialize edilmiştir.
- [x] `ZombieCastleAppIcon-1024.png` marketing icon asset'i repoda bulunur.
- [x] Mac commit `e8389c048` imzalı iOS archive, App Store Connect upload ve TestFlight
  `1.0 (1)` kaydını bildirir. Owner 2026-07-24 konuşmasında build'i TestFlight'a
  gönderdiğini yeniden teyit etti; bu dış servis durumu bu Windows oturumunda bağımsız
  olarak açılıp doğrulanmadı.
- [ ] Build internal TestFlight tester akışıyla fiziksel iPhone'a kurulacak.
- [ ] Gerçek cihazda Game Center authentication sonucu, welcome/banner veya local player
  durumu ve `ShowDashboardAsync()` davranışı doğrulanacak.

Leaderboard ve achievement tanımları P18 kapanış kapısı değildir; ileride owner tarafından
ayrı ürün kapsamı olarak açılmalıdır.

---

## 5. Çalışma Günlüğü

### 2026-07-18 - Tracker oluşturuldu

- Tamamlanmış V1 tracker değiştirilmeden yeni Post-V1 takip belgesi oluşturuldu.
- P1-P10 çalışma önceliği kaydedildi; bağımsız görevlerin paralel yürüyebileceği daha sonra netleştirildi.
- Daha önce owner tarafından onaylanmış Skeleton Soul ve düşman hasar görünürlüğü kararları kaydedildi.
- Worker UI, Arrow Supply, teknoloji ağacı ve Castle Heart ayrı ana görevler olarak ayrıldı.
- UI/UX araştırmasının şimdi yapılmayacağı, yalnızca P3 aktif olduğunda başlatılacağı çalışma kuralına yazıldı.
- Konuşulmamış ürün ve tasarım ayrıntıları bilinçli olarak `[?]` bırakıldı.

### 2026-07-18 - P1 kapatıldı

- Owner mevcut oyunda bilinen mantık sorunu olmadığını kesinleştirdi.
- Varsayımsal veya oyunda bulunmayan bir problem eklenmedi.
- Başlangıç regresyonu EditMode `408/408`, PlayMode `89 pass + 2 explicit skip` geçti.

### 2026-07-18 - P2 Soul ve hasar geri bildirimi tamamlandı

- Skeleton ölümü gameplay kill/Soul state'ine death frame'inde ve tam bir kez bağlandı.
- Production meta reward kill bantlarının tamamı `1 Soul / Skeleton` olarak kilitlendi.
- Soul ölüm konumundan HUD sayacına runtime-pooled animasyonla bağlandı; varış pulse üretir.
- Basic/Rapid/Frost Arrow ile Fireball Primary/SecondBlast/BurningGroundPulse gerçek uygulanan hasarı ortak event sözleşmesine bağlandı.
- Aynı hedefe aynı frame'de ulaşan okların lost-update riski kaldırıldı; gösterilen overkill mevcut HP'ye clamp edildi.
- Targeted EditMode `18/18`, targeted PlayMode `2/2`, tam EditMode `420/420` ve tam PlayMode `91 pass + 2 explicit skip` geçti.

### 2026-07-18 - P3 UI Toolkit yönü ve Main Menu onayı

- Benzer oyun ve PC/mobile UI/UX araştırması sonucunda bilgi öncelikli, minimal UI Toolkit yönü owner tarafından onaylandı.
- Bütün player-facing UI yüzeylerinin UI Toolkit'e geçirileceği; `1920x1080`, English copy ve cihaza göre otomatik kontrol sunumu kullanılacağı kilitlendi.
- Main Menu UI Toolkit runtime sahibiyle uygulandı; save-aware ana eylem ve Settings davranışı korundu.
- Owner geri bildirimiyle dekoratif çizgiler, footer etiketleri ve bütün ornamental UI hareketleri kaldırıldı.
- Yalnızca arka plan `DAY / DUSK / NIGHT / DAWN` döngüsü animasyonlu bırakıldı.
- Unity compile ve Play Mode visual-tree doğrulaması tamamlandı; owner Main Menu'yü onayladı.
- P3 ana görevi gameplay HUD çekirdeğiyle devam ediyor; ana görev paydası değişmedi ve toplam ilerleme `2/10 - %20` olarak kaldı.

### 2026-07-18 - P3-P8 tam UI Toolkit rework tamamlandı

- Bilimsel UI/UX araştırması, karşılaştırmalı oyun sentezi ve Dead Walls karar sistemi 14 sayfalık `DEAD_WALLS_UI_UX_DECISION_SYSTEM.pdf` belgesinde tamamlandı.
- Eski UI görsel/layout referansı olarak kullanılmadan bütün player-facing runtime yüzeyleri UI Toolkit ile sıfırdan kuruldu; legacy UGUI yalnız davranış/veri köprüsü olarak tutuldu.
- Persistent HUD, Economy, Barracks, Arrow Supply, Castle Heart, War Doctrine, Council, Level Up, Pause, Settings ve Game Over/Meta Shop canlı `1920x1080` QA'dan geçti; touch uyarlaması ayrıca `1280x720` doğrulandı.
- Castle Heart ve War Doctrine runtime verisinden dynamic graph üretir; Castle Heart açıkken simulation akmaya devam eder.
- Main Menu'de owner kararına uygun olarak yalnız day/dusk/night/dawn arka plan döngüsü sürekli animasyonlu kaldı.
- Unity compilation ve temiz Console doğrulandı; tam EditMode `420/420`, tam PlayMode `91 pass + 2 explicit skip` geçti.
- P3, P4, P5, P6, P7 ve P8 birlikte kapandı; ana görev paydası değişmedi ve ilerleme `8/10 - %80` oldu. P9 performans ve P10 final gate açık kaldı.

### 2026-07-19 - Game Over, worker atama ve duplicate teknoloji yüzeyi düzeltmesi

- Game Over/Meta Shop legacy UGUI satır metinlerini aynalamayı bıraktı; run sonucu, bakiye ve 11 kalıcı yükseltme doğrudan production verisinden yapılandırılmış UI Toolkit kartlarıyla sunuluyor.
- Game Over tam opak modal katman kullanır ve açılırken mevcut drawer/fullscreen yüzeyi kapatır.
- Economy drawer mevcut idle nüfusu kaynaklara doğrudan atayan `-10 / -1 / +1 / +10 / FILL` kontrollerini kazandı; yeni nüfus otomatik hedefi ayrı kaldı.
- Ayrı `TECHNOLOGY / War Doctrine` butonu ve ekranı production UI'dan kaldırıldı; gameplay tech verisi Castle Heart final rework'ü için davranış katmanında korundu.
- Castle Heart owner kararıyla son UI işi olarak yeniden açıldı ve bu pakette içeriğine dokunulmadı.
- UI kontrat testleri `2/2`, tam EditMode `422/422`, tam PlayMode `91 pass + 2 explicit skip` geçti; Unity compilation ve final Console temiz.
- Ana görev paydası değişmedi; P7 yeniden açıldığı için doğrulanmış ilerleme `7/10 - %70` oldu. P7, P9 ve P10 açık kaldı.

### 2026-07-19 - Artsystack ikon giydirme ve celestial-arc gün döngüsü polish'i

- Owner tarafından onaylanan Artsystack ikon seti Castle Heart dışındaki yeni UI Toolkit yüzeylerine uygulandı; RPG Icons Pixel Art paketi bilinçli olarak kullanılmadı.
- `Worker Production` için `fist_128_T`, `Repair Gate` için `tools_2_128_T` kullanılarak iki eylemin görsel kimliği ayrıştırıldı.
- Eski HUD'ın celestial-arc davranışı referans alınarak gerçek `CycleProgress01` değerini takip eden day/dusk/night/dawn arc sunumu UI Toolkit'te yeniden kuruldu.
- `1920x1080` ana HUD, Economy, Game Over/Meta Shop ve `1280x720` touch/compact yüzeyleri canlı Play Mode'da doğrulandı; Castle Heart içeriğine dokunulmadı.
- Resource kartlarında toplam ve `/m` oranı ayrı dikey sayı bloğuna alındı; `230 / +100/m`, `67 / +39.2/m` gibi gerçek runtime değerlerinde overlap olmadığı `1920x1080` canlı görüntüde doğrulandı.
- Alt phase rail'i `PhaseProgress01` tabanlı continuous fill kazandı; canlı ölçümde Dawn `%11.8`, sonraki Day `%19 -> %66.2` ilerledi ve çevrim reset'i doğrulandı.
- Targeted UI kontratları `6/6`, tam EditMode `426/426` geçti; Unity compilation ve Console'da hata yok.
- Tam PlayMode turunda UI kapsamı dışındaki `FirstDayWorkerRatioOnboarding_PulsesRealControlAndCompletesOnPlayerAction` testi, bir frame'lik normal üretim tick'i nedeniyle Wood değerini `160` yerine `161` görerek başarısız oldu; izole tekrar aynı sonucu verdi ve ilgili gameplay/test dosyaları bu pakette değiştirilmedi.
- Ana görev paydası ve statüleri değişmedi; P7, P9 ve P10 açık kaldığı için doğrulanmış ilerleme `7/10 - %70` olarak korundu.

### 2026-07-19 - P7 Castle Heart UI Toolkit final rework tamamlandı

- Eski kart tabanlı ara görünüm kaldırıldı; merkez Heart, icon-first node'lar ve etiketsiz dört yönlü progression dili kuruldu.
- İlk durumda yalnız root + dört başlangıç teknolojisi çiziliyor; hidden node'lar visual tree'ye hiç eklenmiyor.
- Reveal ve purchase servisleri yalnız satın alınan node'un direct outgoing hedeflerini açıyor; eski Keystone pair reveal/lock kuralı ve hidden partner metadata'sı kaldırıldı.
- `Painter2D` tabanlı noktalı-eğrisel connector parent'tan child'a büyüyor; child node kısa opacity/scale transition ve çoklu child stagger ile beliriyor.
- 37 production node'u incelenmiş `RPG Icons Pixel Art` sprite'larına bağlandı ve mapping catalog builder'ın kalıcı source-of-truth'u yapıldı.
- PC right-inspector ile compact/touch bottom-inspector Game View'da doğrulandı; canlı purchase ölçümü node sayısını `5 -> 6` değiştirdi.
- Targeted EditMode `32/32` ve PlayMode `2/2` geçti; Unity compilation ve Console temiz kaldı.
- Ana görev paydası değişmedi; P7 kapatıldığı için doğrulanmış ilerleme `8/10 - %80` oldu. P9 ve P10 açık kaldı.

### 2026-07-19 - Grave Essence üretim kaynağı ve Castle Heart test hibesi

- Gerçek düşman ölümleri varsayılan `%10` olasılıkla `1 Grave Essence` üretir; stress-test ölümleri ödül üretmez ve kazanım doğrudan kanonik Grave Essence bakiyesine eklenir.
- Düşme olasılığı ile miktarı `DifficultyProfileSO` ve Difficulty Tuner üzerinden ayarlanabilir hale getirildi.
- F10 Development Tests paneline `GRANT 1M HEART ESSENCE` eylemi eklendi; bu eylem yalnız bakiyeyi artırır, hiçbir teknolojiyi açmaz veya satın almaz.
- Test hibesiyle Castle Heart teknolojileri normal `RESEARCH / UPGRADE` akışında tek tek alınabilir; direct-child reveal, connector ve node animasyonları gerçek davranışlarıyla çalışır.
- Development test oturumu run snapshot yazımını engeller; test hibesi normal kayıt dosyasına taşınmaz.
- Targeted EditMode `6/6` ve PlayMode `2/2` geçti; Unity compilation ve Console temiz kaldı.
- Bu çalışma P7 için destekleyici doğrulama aracıdır; ana görev paydası ve statüleri değişmedi. İlerleme `8/10 - %80`, P9 ve P10 açık.

### 2026-07-19 - Castle Heart organik conquest-map yerleşim revizyonu

- Owner geri bildirimi ve Zero Stress King conquest-map referansı doğrultusunda dört doğrusal radyal kol, deterministic asimetrik waypoint kümelerine dönüştürüldü.
- Her teknoloji yolu eşit aralıklı düz ray yerine yön değiştiren küçük node adaları oluşturur; mevcut hidden-safe direct-child reveal davranışı değişmedi.
- Connector koordinatları button merkezinden gerçek dairesel socket merkezine taşındı ve yollar socket sınırında başlayıp bitecek şekilde kırpıldı.
- Ana parent yolları yüksek kontrast branch-tint ve koyu halo ile güçlendirildi; cross-link'ler daha geniş cubic kavis ve ayrı nokta ritmiyle ayrıştırıldı.
- Owner yenilenen tam ağaç görünümünü canlı oyunda onayladı.
- Targeted EditMode `23/23` ve PlayMode `4/4` geçti; Unity compilation, final Console ve `git diff --check` temiz kaldı.
- Bu revizyon tamamlanmış P7 kapsamının polish devamıdır; ana görev paydası ve statüleri değişmedi. İlerleme `8/10 - %80`, P9 ve P10 açık.

### 2026-07-19 - Castle Heart zoom ve pan navigation polish'i

- İlk açılıştaki görünür-node auto-fit davranışı korunarak `65% - 225%` zoom aralığı eklendi.
- PC mouse-wheel imleç altındaki graph noktasını korur; touch input iki parmak pinch/midpoint pan kullanır.
- Mouse veya tek parmak yalnız boş graph alanından pan başlatır; node click/tap sahipliği değişmedi.
- Header'a minimal `- / yüzde / + / FIT` kontrolü eklendi; touch modunda hit-area büyür ve kontroller graph node'larının üstünü kapatmaz.
- Görünür bounds tabanlı clamp, pan/zoom sırasında ağacın tamamen kaybolmasını engeller; reveal ve responsive relayout kullanıcı görünümünü korur.
- Targeted EditMode `19/19`, PlayMode `2/2`, canlı `100% -> 156% -> 100%` Game View kontrolü, Unity compilation ve final Console temiz geçti.
- Bu çalışma tamamlanmış P7 kapsamının navigation polish devamıdır; ana görev paydası ve statüleri değişmedi. İlerleme `8/10 - %80`, P9 ve P10 açık.

### 2026-07-19 - Fireball hedef alanı UI Toolkit raycast düzeltmesi

- Tam ekran `screen` ve `hudLayer` kapsayıcıları `PickingMode.Ignore` kullanacak şekilde düzenlendi; böylece boş savaş alanı tıklamaları `SpellCastUI` hedefleme akışına ulaşırken gerçek HUD butonları kendi raycast sahipliğini koruyor.
- Canlı Play Mode doğrulamasında boş savaş alanı raycast sonucu `1 -> 0` oldu, Fireball butonu raycast sonucu `1` kaldı; hedefleme kabul edildi, Fireball cast'i projectile oluşturdu ve `45s` cooldown başlattı.
- UI Toolkit kontrat regresyonu `9/9` geçti; Unity derlemesinde ve değişiklik kapsamındaki Console kontrolünde yeni hata oluşmadı.
- Bu düzeltme tamamlanmış P3/P8 UI Toolkit kapsamının hata düzeltmesidir; ana görev paydası ve statüleri değişmedi. İlerleme `8/10 - %80`, P9 ve P10 açık.

### 2026-07-19 - P9 performans profilleme ve player zombie limiti tamamlandı

- Main Menu ve Pause Settings aynı persistent `BALANCED 900 / HIGH 2.000 / MASSIVE 5.000 / EXTREME 10.000` aktif-zombi ayarını kazandı; copy düşük limitin daha iyi performans fakat daha düşük battlefield density ürettiğini açıklar.
- Runtime limit değişikliği `MobileCastleCombatConfig.MaxAliveZombies` owner'ına uygulanır. Limit düşürmek yaşayan zombileri silmez; spawn budget exact backlog'da bekler.
- Barracks backend `1.000` hard cap'i korunurken player-facing `/ 1000` gösterimi kaldırıldı; normal durumda deployed toplamı, yalnız cap'te `GARRISON FULL` gösterilir.
- 10K Fireball profili event başına TMP damage-number objesi ile çift legacy/UI Toolkit Soul allocation'ını kanıtlanmış darboğaz olarak gösterdi.
- Damage feedback `512` presentation'lık TMP mesh batch'lerine taşındı. Dense damage event'leri source-aware spatial aggregate olur; event sayısı ve uygulanan toplam hasar public telemetry ile exact korunur.
- Soul feedback `96` event'e kadar birebirdir; daha yoğun aynı-frame burst adaptif konumsal grid ile en fazla `96` `+N` uçuşuna toplanır. Toplam Soul miktarı korunur ve UI Toolkit elementleri pool'lanır.
- Fresh 10K enemy + 1K Archer Editor benchmark'ı geçti: main-thread average `13,12 ms`, frame P95 `17,83 ms`, draw-call average `591`, root GC average `84.626 B/frame`; 10K Fireball death peak `13.786 ms` baseline'dan `63,11 ms`ye düştü.
- Targeted EditMode `19/19`, targeted PlayMode `6/6`, Unity compilation, Console ve `git diff --check` temiz geçti. Main Menu Settings daha önce canlı `1280x720` Game View'da taşma olmadan doğrulandı.
- Ana görev paydası değişmedi; P9 kapatıldığı için doğrulanmış ilerleme `9/10 - %90` oldu. Yalnız P10 final kalite kapısı açık kaldı.

### 2026-07-19 - P10 son kalite kapısı tamamlandı

- Tam paket regresyonu final durumda EditMode `440/440`, PlayMode `95 pass + 2 explicit skip`, `0 fail` geçti.
- Full-suite sahne geçişinde önceki testten kalan static `GameManager` owner'ının okunmasını engelleyen test fixture bekleme kontratı Zombie Limit ve Long-Run Soak testlerine eklendi.
- Long-Run Soak fixture'ı kullanıcı tercihinden bağımsız release cap'i olan `Balanced 900` ile izole edildi; `3.600` frame, `11.219` exact demand/spawn, pool bütünlüğü ve backlog drain kapılarıyla `1/1` geçti.
- 10K Zombie + 1K Archer yoğun combat testi `1/1` geçti; frame average `12,46 ms`, P95 `15,51 ms`, main-thread average `12,32 ms`, exact backlog ve iki deterministic Continue fingerprint'i doğrulandı.
- Target-hardware profiler testi Editor pacing sonucunu kabul/reddet kanıtı saymayacak şekilde Windows Player ortamına kilitlendi; daha önce alınmış `1920x1080 / Ultra` WindowsPlayer kabul raporları korundu.
- Main Menu, Settings, gameplay HUD, Economy, Barracks, Arrow Supply, Castle Heart, Pause ve Game Over/Meta Shop yüzeyleri PC ve compact/touch düzenlerinde canlı olarak incelendi; overlap, ikinci teknoloji ağacı veya player-facing geçici placeholder bulunmadı.
- `NewGameScene` temiz production boot turunda Console `0 error` verdi; yalnız mevcut, kritik olmayan ECS update-order uyarısı raporlandı. `git diff --check` temiz geçti.
- P10 kapatıldı; Post-V1 tracker kapsamı `10/10 - %100` tamamlandı.

### 2026-07-20 - Early-run balance cohort harness ve 10+10 policy smoke audit'i

- Legacy TechCatalog satin alan eski long-run botu current Castle Heart/Grave Essence,
  finite Arrow, housing, worker investment, archer ve combat ability kontratlarina tasindi.
- `Balanced / Economy / Defense` policy'leri, otomatik fresh-meta restart state machine'i,
  run-bazli detay CSV'leri ve tek cohort summary CSV'si eklendi.
- Economy policy ileri `15` yatak hedefini ve Wood/uretim allocation'ini; Defense policy
  okcu/Arrow/Stone surekliligini ve savas odakli Heart secimini uygular. Defense, yuksek
  oncelikli node icin Grave Essence biriktirir ve Fireball acildiginda canli hedefe kullanir.
- Ayni release fingerprint'i ile Economy `10/10` ve duzeltilmis Defense `10/10` fresh run
  tamamlandi: combined median `Day 7`, dagilim `18x Day 7 + 2x Day 8`, Day 3/6 erisimi
  `%100`, Day 12 erisimi `%0` oldu. Economy ortalama Food `720`, Defense `632,4` verdi.
- Launch authority minimumu `100` fresh run oldugu icin bu `20` run yalniz smoke/discovery
  kanitidir; `DefaultDifficulty` veya diger production tuning asset'leri otomatik degistirilmedi.
- Unity compilation ve final Console `0 error` gecti; kullanici meta kaydi SHA-256 eslesmesiyle
  test oncesi haline geri yuklendi.
- Bu audit tamamlanmis P10 kapsaminin post-closure olcum aracidir; ana gorev paydasi ve
  statuleri degismedi. Post-V1 tracker `10/10 - %100` tamamlanmis olarak korunur.

### 2026-07-20 - 100-run simulator proxy cohort ve production ramp revizyonu

- Ayni pre-tuning fingerprint'te `50 Economy + 50 Defense` fresh-meta bot run'i tamamlandi.
  Combined median `Day 7`; dagilim `2x Day 6 + 67x Day 7 + 31x Day 8`, Reach Day 3/6 `%100`
  ve Reach Day 12 `%0` oldu. Economy median `Day 7`, Defense median `Day 8` verdi.
- Kaynak davranisi policy farkini dogruladi: Economy ortalama Food `717,32`, Archer `6`, Repair
  `5,68`; Defense Food `690,52`, Archer `7`, Repair `10,08`, Fireball `3,54` verdi.
- Median hedef araliginda olmasina ragmen tum sonuclarin Day 6-8'e yigilmasi nedeniyle yalniz
  production night/cycle rampi revize edildi: Night curve `(1,.60) (3,.75) (5,.86) (7,.95)
  (60,.95)`, `SpawnBatchGrowthPerCycle .15 -> .10`. HP, damage, ekonomi, phase sureleri ve
  base intensity'ler aynen korundu.
- Post-tuning directional smoke ayni yeni fingerprint'te `10 Economy + 10 Defense` tamamlandi:
  Economy `10x Day 8`, Defense `6x Day 8 + 4x Day 9`. Day 9+ upper-tail acildi ve policy farki
  korundu.
- Bot kohortu tuning proxy kanitidir; provider-independent Reach Day hedeflerinin kabulu gercek
  player `fresh completed` telemetry minimumlariyla yapilacaktir. Bot sonucu launch telemetry
  kabulu olarak yazilmadi.
- Exact production contract, setup default'u, tuning kilavuzu ve test beklentileri yeni baseline'a
  senkronlandi. Bu post-closure tuning calismasi ana gorev paydasini degistirmez; Post-V1 tracker
  `10/10 - %100` tamamlanmis olarak korunur.
- Targeted EditMode tuning kontratlari `9/9`, tam EditMode `440/440`, tam PlayMode
  `95 pass + 2 explicit skip` ve `0 fail` gecti. Unity Console `0 error`; kullanici meta kaydi
  test oncesi SHA-256 ile birebir geri yuklendi ve zombie-limit tercihi `Balanced 900` korundu.

### 2026-07-20 - Runtime HUD currency, arrow reserve ve cycle sadelestirme polish'i

- Sag ust HUD'a kanonik run-ici `Grave Essence` bakiyesi, Souls'tan ayri mor kimlik ve
  Castle Heart ile ortak crystal icon sozluguyle eklendi.
- Castle Heart header'i HUD Essence readout'uyla ayni mor palette tasindi; teknoloji inspector'i
  player-facing copy'de `GRAVE ESSENCE` yerine yalniz `ESSENCE` kullanir.
- Basarili gercek-dusman Grave Essence drop'u, mevcut pooled currency-flight katmaninda olum
  konumundan yeni HUD sayacina gider ve varista sayaci pulse eder. Development grant'leri bu
  player-facing drop gorselini tetiklemez.
- Alt `ARROW SUPPLY` komutu drawer acilmadan `current / capacity` rezervini gosterir; dusuk stok
  state'i ayni esik ve danger rengiyle okunur.
- Day/night HUD'daki tekrar eden dortlu alt faz rayi kaldirildi; gercek cycle progress'i izleyen
  celestial arc, phase adi ve geri sayim tek bilgi sahibi olarak korundu.
- UI Toolkit kontratlari `11/11`, gercek Skeleton Soul + Grave Essence drop PlayMode testi `1/1`
  gecti. Canli `1920x1080` kontrolde Essence/Souls readout'lari ve Arrow Supply rezervi overlap
  olmadan goruldu; old phase rail bulunmazken canonical Essence/arrow label binding'leri ve
  Essence flight sayaci runtime'da dogrulandi. Unity Console `0 error`, `git diff --check` temizdir.
- Bu post-closure UI polish calismasi ana gorev paydasini degistirmez; Post-V1 tracker
  `10/10 - %100` tamamlanmis olarak korunur.

### 2026-07-20 - Merkezi Audio Director ve currency-arrival ses polish'i

- `Assets/Resources/DeadWallsAudioProfile.asset`, Combat, Interface, Castle Heart, Currency,
  Ambience ve audition-only music/ability adaylarinin tek merkezi ses profili olarak eklendi.
- `Tools > Dead Walls > Audio Director`; kategori override, clip/array atama, preview/stop ve
  curated-default kurulumuyla scene/prefab kopyasi gerektirmeyen A/B akisi saglar.
- Gamemaster paketinden uyumlu bow, wall, Fireball, UI ve currency aileleri profile secildi;
  mevcut Arrow/Frost impact, Heart book/reveal/lock ve game-over sting spesifik fallback'leri
  korundu. Biug menu/day/night parcalari audition adayi olarak hazirlandi fakat music/ambience
  override'lari owner oyun icinde dinleyene kadar varsayilan kapali tutuldu.
- Owner karariyla `ZombieDeathSystem` artik bireysel veya aggregate Skeleton death SFX event'i
  uretmez; bridge'de serialized enum uyumlulugu korunurken bu tip daima sessizdir.
- Soul ve Grave Essence sesi gameplay transaction/drop aninda degil, UI Toolkit flight'i kendi
  HUD anchor'ina vardiginda oynar. Ayni penceredeki miktar logaritmik volume/pitch ve kesin tavanla
  tek cue'ya toplanir; Soul ve Essence ayri rate-limit/source kullanir.
- Audio Director profile kurulumunda eksik asset raporlamadi; Editor preview/stop komutlari ve
  Unity compilation Console `0 error` ile calisti. Targeted EditMode `19/19`, targeted PlayMode
  `5/5` gecti; death-SFX yoklugu ile iki currency'nin varis aninda ses uretmesi canli test edildi.
- Bu post-closure audio polish calismasi ana gorev paydasini degistirmez; Post-V1 tracker
  `10/10 - %100` tamamlanmis olarak korunur.

### 2026-07-22 - Night-only saldırı, clear gate ve günlük Council

- Owner kararıyla saldırı ritmi Night-only yapıldı. Day/Dusk/Dawn intensity'si runtime'da sıfır;
  spawn demand ve backlog drain yalnız Night sözleşmesinde çalışır.
- Timed Night sonuna Night clearance kapısı eklendi. Yeni demand durur; pending backlog
  sahaya akıp son yaşayan düşman öldükten sonra Dawn başlar.
- Council occurrence Day 1'den itibaren her Dawn'da tam bir kez olacak şekilde mevcut regular
  schedule owner'ında değiştirildi. Mevcut composer, UI, effect, flag, chain ve save sahipliği korundu.
- Day 1 temel event havuzu production catalog testleriyle kilitlendi; her gün için en az bir valid
  kart olduğu Day 1-30 ve çoklu seed örnekleminde doğrulandı.
- Clearance HUD copy'si ve exact save/Continue için ayrı regression testleri eklendi.
- Targeted EditMode `63/63`, targeted PlayMode `10/10`, full EditMode `447/447`, full PlayMode
  `96 pass + 2 explicit skip / 0 fail` geçti. `NewGameScene` validation `0` issue, final Console
  `0 error / 0 warning` ve `git diff --check` temizdir.
- Owner onaylı yeni ana görev eklendiği için tracker paydası `10 -> 11` değişti;
  `DW-P11-NIGHT-RHYTHM` kapanışıyla Post-V1 ilerleme `11/11 - %100` oldu.
- Owner görsel kontrolü sonrası clearance faz başlığındaki `LAST STAND` kaldırıldı; başlık normal
  `NIGHT SIEGE` olarak kalırken `N LEFT` sayacı ve temizleme mesajı korundu.
- Güncel HUD kontratı targeted EditMode `12/12`, Unity Console `0 error` ve `git diff --check`
  ile doğrulandı; tracker paydası değişmedi.

### 2026-07-22 - Gameplay HUD okunabilirlik ve terminoloji uygulaması

- Owner kararıyla `DW-P12-UI-READABILITY` ayrı ana görev olarak açıldı; tracker paydası `11 -> 12` değişti.
- Gameplay HUD authored font tabanı `10px`e çıkarıldı; Night clearance mesajı `11px` ve yüksek kontrast oldu. Touch/compact tipografi override'ları aynı tabanla hizalandı.
- `/m`, `DPS`, `L1`, `AUTO`, belirsiz `AVAILABLE`, legacy `SOULS`, kaldırılmış `DOCTRINE` ve çelişkili `PERMANENT RUN UPGRADE` copy'leri açık player-facing karşılıklarla değiştirildi.
- Clearance copy'si `N ENEMIES LEFT` ve `CLEAR ENEMIES TO REACH DAWN` oldu; Rally hazır durumu etkisini `BOOST FIRE RATE` olarak açıklar.
- Targeted EditMode HUD kontratı `14/14` geçti; Unity domain reload tamamlandı, final Console `0 error / 0 warning` ve `git diff --check` temizdir.
- Aktif ve kaydedilmemiş `HandMadeTiles` sahnesi korunarak sahne değişimi yapılmadı. Canlı `NewGameScene` Game View görsel kontrolü tamamlanmadığı için P12 `[~]`, tracker ilerlemesi `11/12 - %91,7` olarak bırakıldı.

### 2026-07-22 - Economy worker slider ve housing CTA yenilemesi

- Owner kararıyla worker atamasındaki beş ayrı artır/azalt/fill komutu, dört kaynağın toplam `%100` share kontratını paylaşan dinamik slider'larla değiştirildi.
- Slider değişikliği hem yeni arrival target'ını hem mevcut worker dağılımını aynı transaction içinde günceller; `%100` share diğer üç kaynağı sıfırlar, kapasite taşması idle havuzuna döner.
- Housing CTA resource scroll'unun üstüne taşındı; full-capacity uyarısı ve `1/10/100 bed` paketlerinin açık maliyetleri aynı kartta görünür oldu.
- Toolkit `ResourceCost` sunumu tam kaynak adlarına geçirildi; örnek kontrat `150 WOOD · 100 IRON` olarak kilitlendi.
- Slider etkileşimi mevcut first-run onboarding'i de tamamlar; player hint'i `DRAG A WORKER SHARE SLIDER.` olarak güncellendi.
- Targeted EditMode `39/39`, targeted PlayMode `1/1`, Unity Console `0 error / 0 warning` ve `git diff --check` temiz geçti. Aktif `HandMadeTiles` sahnesi PlayMode testinden sonra geri yüklendi ve repo sahne dosyalarına dokunulmadı.
- `NewGameScene` canlı `1920x1080` normal ve zorlanmış compact/touch görsel kabulünde housing paketleri taşmadan göründü; `%100 WOOD` runtime kontrolü diğer üç kaynak slider/worker değerlerini `0` yaptı ve `16` taşan worker idle havuzuna döndü.
- P12 bütün zorunlu kapıları geçti; tracker `12/12 - %100` olarak kapatıldı.

### 2026-07-22 - P12 post-acceptance workforce ve asker kaynağı düzeltmesi

- Owner, Idle'ın asker rezervi olmadığını ve asker üretiminin resource worker havuzundan kişi çekmesi gerektiğini kesinleştirdi.
- `WorkerAllocationUtility` bütün archer-dışı nüfusu işe atar; target kapasitesi dolduğunda overflow worker'lar Wood -> Stone -> Iron -> Food sırasındaki ilk boş kapasiteye geçer. Unassigned yalnız toplam job kapasitesi yetersizse kalır.
- `GameManager` slider değişikliğinde yalnız önceden atanmış worker'ları değil bütün sivil havuzu yeniden dağıtır. Archer buy ve Council free-archer akışları aynı sabit sırayla resource worker eksiltir; Market/Council copy'si `NEED WORKER` ve `RESOURCE WORKER` kullanır.
- Economy drawer küresel Idle tekrarlarını kaldırdı; üst özet `UNASSIGNED`, slider satırları `% TARGET`, kapasite nedeniyle sıfır hedefli resource'a giden kişi `CAPACITY OVERFLOW` olarak görünür.
- Targeted EditMode `36/36`; worker, Council, exact Continue, archer cap ve telemetry targeted PlayMode toplam `7/7` geçti.
- Canlı `NewGameScene` kontrolünde başlangıç `4 archers + 56 workers + 0 unassigned`; `%100 WOOD` sonrası `40 Wood + 16 Stone overflow + 0 unassigned`; bir Basic Archer sonrası `5 archers + 55 workers + 0 unassigned` doğrulandı.
- P12 kabulü yeni workforce sözleşmesiyle düzeltildi; ana görev paydası değişmedi ve tracker `12/12 - %100` kaldı.

### 2026-07-22 - Oyun hızı, Council pause ve toast altyapısı

- Owner kararıyla `DW-P13-GAME-FLOW-CONTROLS` açıldı; tracker paydası `12 -> 13` değişti.
- UI Toolkit HUD'da celestial day-cycle panelinin altına `1X/2X/3X` kontrol rayı eklendi. Aktif hız hem seçili düğme hem `NX ACTIVE` metniyle gösterilir.
- Merkezi pause koordinatörü koşu hızını koruyacak şekilde genişletildi. Council kartı açıldığında simulation `0` olur; valid seçim sonrası önceki `1X/2X/3X` exact geri gelir.
- Council'ın pause altında ilerlemeyen eski decision countdown sunumu, açık `GAME PAUSED · CHOOSE TO CONTINUE` copy'siyle değiştirildi. İlk-kosu exact-choice hint'i bu blocking pause'un allowlist istisnası oldu.
- Sekiz mesajlık bounded FIFO toast servisi ve unscaled UI Toolkit presenter eklendi. Mevcut action feedback kaynakları korundu; owner onayı olmayan yeni bir otomatik toast olayı bağlanmadı.
- Targeted EditMode `50/50` ve Council PlayMode `9/9` geçti. Canlı `1920x1080` doğrulamada `3X -> Council pause -> 3X restore`, sıkışmayan hız rail'i ve pause sırasında geçici toast preview görüldü.
- `NewGameScene` validation `0` issue, final Console `0 error / 0 warning` ve `git diff --check` temiz geçti; owner `AGENTS.md` ile `HandMadeTiles` dosyalarına dokunulmadı.
- P13 kapanışıyla Post-V1 ilerleme `13/13 - %100` oldu.

### 2026-07-22 - Game Over meta açıklığı ve exact action-failure toast'ları

- Owner, Game Over'da seçilen kalıcı yükseltmenin ne olduğunu ve ne vereceğini açık görme; kaynak
  veya benzer requirement yetersizliğinde exact neden toast'ı alma kararını kesinleştirdi.
- Meta kartları katalog effect owner'ından mevcut ve satın alma sonrası toplamı üretir; kaynak,
  Archer, yatak, Wall HP, worker production, arrow efficiency, Grave Essence ve future Heart
  option unlock semantikleri player-facing adlarla ayrıldı.
- Purchase/research düğmeleri terminal olmayan reddedilebilir durumda tıklanabilir kaldı;
  `GameplayActionFeedbackUtility` canlı maliyet, kaynak, worker, capacity ve meta bakiye snapshot'ını
  `NEED N MORE RESOURCE` warning copy'sine dönüştürür.
- Hedefli EditMode `21/21`, gerçek Basic Archer button-submit PlayMode `1/1`, Unity compile
  `0 error` ve `1920x1080` Game Over görsel kontrolü geçti.
- Geniş `WorkerAllocationPlayModeTests` taraması bu paketle ilgisiz iki mevcut phase/presentation
  testinde (`DawnArrivalTransaction...`, `DayPresentation...`) bağımsız hata göstermeye devam etti;
  P14'ün yeni button/toast testi izole kabulde geçti ve bu iki eski test bu pakette değiştirilmedi.
- Owner onaylı yeni ana görev nedeniyle payda `13 -> 14` değişti; P14 kapanışıyla Post-V1 ilerleme
  `14/14 - %100` oldu.

### 2026-07-22 - P14 Archer interactable ve İngilizce toast post-acceptance düzeltmesi

- Owner canlı kullanımda Archer düğmesinin kaynak yetersizliğinde hâlâ disabled kaldığını ve
  player-facing Türkçe toast kabul edilmediğini bildirdi.
- Kök neden aynı sahnedeki iki UI owner'ından yalnız UI Toolkit Barracks yolunun önceki kabulde
  düzeltilmiş olmasıydı; legacy `MarketUI` hâlâ `CanBuyArcher == false` sonucunu doğrudan
  `Button.interactable = false` yapıyordu.
- Legacy ve Toolkit Archer yolları ortak `GameplayActionFeedbackUtility` sözleşmesine bağlandı.
  Kaynak/worker eksikliği tıklanabilir warning state'i korur; locked/max terminal durumları kapalı kalır.
- Castle Heart ve War Doctrine action-failure yüzeyleri internal Türkçe transaction/reason mesajını
  doğrudan taşımak yerine İngilizce presentation mapping kullanır. Oyunun bütün player-facing metin
  dili için İngilizce sınırı yeniden kilitlendi.
- Hedefli EditMode `23/23`, gerçek UI Toolkit + legacy Market Archer PlayMode `2/2` ve Unity compile
  `0 error` geçti. Ana görev paydası değişmedi; tracker `14/14 - %100` kaldı.

### 2026-07-22 - Süreli toast stack'i ve UI Toolkit button sesleri

- Owner, toast'ın kalıcı görünmemesini, her tıklamanın ayrı kart üretmesini, kartların üst üste
  kontrollü biçimde dizilmesini ve button seslerinin çalışmasını istedi.
- Kök neden tek aktif label'ın kuyruktaki tekrarları kesintisiz yeniliyor gibi göstermesi ve
  `UiSoundFeedback` pointer kontrolünün yalnız legacy uGUI `Button` raycast'ini tanımasıydı.
- Presenter üç aktif kartlık dinamik stack'e geçirildi; duplicate action'lar korunur, yeni kart alta
  eklenir, warning kartları `3.2` saniye sonra `180 ms` exit ile kaldırılır ve dördüncü kart en eskiyi düşürür.
- UI Toolkit document root'u `ClickEvent` ile gerçek Button hedeflerini merkezi audio profile click
  kanalına bağlar. Yeni otomatik toast kaynağı eklenmedi.
- Hedefli EditMode `22/22`, hedefli PlayMode `2/2`, Unity compile `0 error` ve `git diff --check`
  temiz geçti. Owner'a ait `AGENTS.md` ile `HandMadeTiles` dosyalarına dokunulmadı.
- Owner onaylı yeni ana görev nedeniyle payda `14 -> 15` değişti; P15 kapanışıyla Post-V1 ilerleme
  `15/15 - %100` oldu.

### 2026-07-22 - Guided onboarding görüşmesi ve legacy Archer hint temizliği

- Owner, eski Canvas tutorial fikrinin yeni UI Toolkit gerçek kontrolleriyle adım adım yeniden
  düşünülmesini; exact click sırasının uygulamadan önce konuşulmasını istedi.
- Mevcut sistemin strict bir sıra olmadığı doğrulandı: yedi condition-driven hint'i legacy Canvas
  target'larına pulse ederken UI Toolkit yalnız metni aynalıyordu.
- `RECRUIT A BASIC ARCHER.` yazısının kaynak uyarısı değil, Basic Archer alınabilir olduğunda purchase
  flag'i yazılana kadar açık kalan legacy onboarding cue'su olduğu doğrulandı.
- Bu cue yeni UI Toolkit HUD'da aynalanmayacak şekilde kaldırıldı; gerçek satın alım ve durable flag
  davranışı değiştirilmedi. Hedefli EditMode `1/1`, PlayMode `1/1` ve Unity compile `0 error` geçti.
- Yeni P16 kapsamı owner kararı beklediği için `[?]` açıldı; payda `15 -> 16`, ilerleme
  `15/16 - %93,75` oldu.

### 2026-07-22 - Guided onboarding sırası owner tarafından onaylandı

- Owner, ilk zorunlu zinciri `ECONOMY -> worker-share slider -> BARRACKS -> BASIC ARCHER -> 2X`
  olarak onayladı. İlk beş adım hedef dışı input'u kilitleyecek ve yalnız gerçek action ile ilerleyecek.
- Rally, Council exact choice, low-arrow refill, Castle Heart, housing ve repair öğretimleri koşula
  bağlı contextual adımlar olarak onaylandı; unrelated kontrolleri kilitlemeyecek.
- P16 `[?] -> [~]` durumuna taşındı. Uygulama ve test tamamlanmadığı için ilerleme
  `15/16 - %93,75` olarak kaldı.

### 2026-07-22 - P16 gerçek-control guided onboarding tamamlandı

- Aktif UI Toolkit HUD'a core zincir için hedef dışı ekran karartması, real-control focus kartı ve
  yalnız hedef subtree'sini geçiren input gate eklendi.
- Exact `ECONOMY -> Wood slider -> BARRACKS -> BASIC ARCHER -> 2X` zinciri yalnız gerçek başarılı
  player action'larıyla ilerler; programmatic drawer açma durable completion yazmaz.
- Rally, Council, low-arrow refill, Castle Heart, housing ve Wall repair öğretimleri koşullu,
  tek-seferlik ve unrelated input'u kilitlemeyen contextual tip'ler olarak bağlandı.
- Stable `tutorial.v2.*` ilerlemesi, v1 tamamlanmış-save uyumluluğu ve Settings'in iki nesli birlikte
  temizleyen reset sözleşmesi eklendi. Legacy Canvas hint/pulse sunumu yeni HUD'dan ayrıldı.
- Hedefli EditMode `45/45`; core/raycast, P15 Archer toast, Settings reset ve ikinci-run dahil
  hedefli PlayMode `5/5` geçti. Unity compile `0 error`, final Console ve `git diff --check` temizdi.
- P16 kapanışıyla payda değişmeden Post-V1 ilerleme `16/16 - %100` oldu.

### 2026-07-22 - P16 post-acceptance Economy Close sıra düzeltmesi

- Owner canlı kontrolde worker slider sonrasında Economy drawer kapatılmadan tutorial'in doğrudan
  Barracks'a geçtiğini bildirdi ve önce gerçek `CLOSE` adımının öğretilmesini kesinleştirdi.
- Kök neden core sırada drawer-close adımı/flag'i bulunmaması ve spotlight dönüşümünde nested
  target'ın `localBound` layout ofsetinin iki kez sayılmasıydı. Canlı örnekte Close `x=515` iken
  focus yanlışlıkla `x=994` konumuna gidiyordu.
- Exact core sıra `ECONOMY -> worker-share slider -> CLOSE -> BARRACKS -> BASIC ARCHER -> 2X`
  olarak düzeltildi. `economyClose` yalnız gerçek player callback'inden durable completion yazar;
  programmatic `CloseSurface()` tutorial progress üretmez.
- Spotlight artık hedef boyutunu sıfır-orijinli local rect olarak root koordinatına dönüştürür;
  drawer içindeki Close/slider/purchase kontrollerinde layout ofseti ikinci kez eklenmez.
- Hedefli EditMode `37/37`, gerçek Close zorunluluğu ve spotlight overlap kontrolünü içeren PlayMode
  `1/1`, Unity compile `0 error` ve final Console `0 error` geçti.
- P16 doğrulanmış durumda kaldı; payda değişmedi ve Post-V1 ilerleme `16/16 - %100` olarak korundu.

### 2026-07-22 - P16 post-acceptance pause, copy ve spotlight polish'i

- Owner canlı kontrolde guided kart görünürken oyunun akmaya devam ettiğini, açıklama metinlerinin
  yavan kaldığını ve hedef çerçevesinin görsel yönlendirme üretmediğini bildirdi.
- Kök neden guided presenter'ın yalnız UI input gate uygulaması, `SimulationPauseService` lease'i
  almaması ve focus rect'in tamamen statik olmasıydı.
- Görünür core ve contextual adımlar `GuidedOnboarding` pause lease'i alır. Core lease Economy'den
  son `2X` aksiyonuna kadar korunur; `2X` pause altında running speed olarak seçilir ve durable core
  completion sonrası simulation aynı action frame'inde `2X` ile devam eder.
- Contextual adımlar unrelated UI input'unu kilitlemeden oyunu durdurur; Housing ve Arrow refill
  yalnız bir action gerçekten affordable olduğunda gösterilerek pause soft-lock'i engellenir.
- Player-facing kartlar `TUTORIAL PAUSED / FIELD TIP - GAME PAUSED` state'i, açık continue/resume
  footer'ı ve sistem gerekçesini anlatan daha ayrıntılı English copy taşır. Focus rect unscaled
  padding/opacity/border pulse'iyle nefes alır.
- Hedefli EditMode `24/24`; core pause, 2X exact resume, Housing contextual pause/resume ve gerçek
  action zincirini kapsayan PlayMode `1/1` geçti. Unity compile `0 error` ve final Console `0 error`
  doğrulandı.
- P16 kapalı kalır; payda değişmedi ve Post-V1 ilerleme `16/16 - %100` olarak korundu.

### 2026-07-23 - P16 post-acceptance drawer header okunabilirlik düzeltmesi

- Owner canlı kontrolde Economy açıklamasının `CLOSE` butonunun altında kaldığını ve son `resource`
  kelimesinin görünmediğini bildirdi.
- Kök neden header copy kolonunun daralabilir bir flex alanı olarak tanımlanmaması ve Close butonu için
  sabit yer ayrılmamasıydı.
- Economy, Barracks ve Arrow Supply drawer header'ları ortak `surface-header-copy` kontratına alındı.
  Copy alanı daralabilir ve satır kırabilir; Close butonu daralmaz ve header'ın üstünde kendi alanını korur.
- Hedefli EditMode `5/5`; tam core tutorial akışını ve gerçek Economy subtitle/Close bounds ayrımını
  kapsayan PlayMode `1/1` geçti. Unity compile `0 error` ve `git diff --check` temiz doğrulandı.
- P16 kapalı kalır; payda değişmedi ve Post-V1 ilerleme `16/16 - %100` olarak korundu.

### 2026-07-23 - Post-acceptance Editor araç menüsü konsolidasyonu

- Owner, Unity `Tools` menüsü altında görünen iki farklı Dead Walls kökünün tek çatı altında
  toplanmasını istedi. Canlı menü denetimi ayrıca eski araçların `Window/DeadWalls` köküne de
  dağıldığını doğruladı.
- Toplam `46` proje Editor aracı tek `Tools/Dead Walls` köküne taşındı; hiyerarşi `Audio`,
  `Balancing`, `Content`, `Maps`, `Profiling` ve `Setup & Repair` kategorilerine ayrıldı.
- Bütün `[MenuItem]` tanımları `DeadWallsEditorMenuPaths` merkezi sabitlerini kullanır. Eski
  `Tools/DeadWalls`, `Window/DeadWalls` ve genel `Tools/Analyze Profiler Data` yolları kaldırıldı.
- Canlı Unity menu-items kaynağı yalnız `Tools/Dead Walls` kökünü ve `46/46` aracı gösterdi.
  `EditorMenuHierarchyTests` hedefli EditMode doğrulaması `1/1` geçti; Unity compile `0 error`.
- Ana görev paydası değişmedi; Post-V1 ilerleme `16/16 - %100` olarak korundu.

### 2026-07-23 - P16 post-acceptance development tutorial save kaldırma

- Owner, aktif geliştirme boyunca tutorial'in tekrar tekrar test edilebilmesi için bütün core ve
  contextual adımların her yeni Unity Play oturumunda otomatik olarak sıfırdan başlamasını kesinleştirdi.
- Kök neden tutorial flag'lerinin `meta_progress.json` içinde kalıcı tutulmasıydı; bir kez tamamlanan
  adımlar sonraki Play girişlerinde yeniden açılmıyordu.
- Yeni `TutorialSessionProgress`, bütün tutorial flag'lerinin tek oturumluk sahibidir. Unity
  `SubsystemRegistration` aşamasında flag havuzunu temizler; bu davranış domain reload kapalıyken de geçerlidir.
- `MetaProgressState.TutorialFlags` legacy şema uyumluluğu için bellekte bırakıldı ancak serialize edilmez;
  eski JSON flag'leri yüklenmez ve yeni save'lere tutorial ilerlemesi yazılmaz. Diğer meta progression korunur.
- Aynı Play oturumundaki Game Over restart tamamlanmış adımları yeniden açmaz; Stop -> Play bütün tutorial'i
  `ECONOMY` ilk adımından başlatır. Kalıcı one-time onboarding/save ve UGS entegrasyonu yayın öncesine ertelendi.
- Tutorial/meta sözleşmesini ve `SubsystemRegistration` otomatik reset kaydını kapsayan hedefli EditMode `49/49`; core zincir, aynı-oturum restart ve yeni-Play
  reset senaryoları PlayMode `3/3`, Settings session reset regresyonu ayrı PlayMode `1/1` geçti. Unity compile
  `0 error` ve `git diff --check` temiz doğrulandı.
- P16 kapalı kalır; ana görev paydası değişmedi ve Post-V1 ilerleme `16/16 - %100` olarak korundu.

### 2026-07-23 - P16 post-acceptance paused ability hedefi düzeltmesi

- Owner canlı kontrolde `RALLY` ve `EMERGENCY REPAIR` contextual tutorial hedeflerine tıklandığında
  aksiyonların çalışmadığını ve oyunun tutorial pause'unda takılı kaldığını bildirdi.
- Kök neden guided presenter'ın `Time.timeScale = 0` yapması, iki ability owner'ının da paused durumda
  gerçek transaction'ı reddetmesi ve periyodik HUD refresh'inin hedef butonu disabled bırakabilmesiydi.
- Yalnız aktif guided ability hedefi için tutorial lease'i transaction öncesi kontrollü bırakılır;
  reddedilen aksiyonda aynı pause anında geri alınır. Rally `2`, Emergency Repair `3` kısayolu da aynı
  güvenli callback yolunu kullanır; normal pause/Council yetenek kullanım sınırı değişmez.
- Aktif contextual ability hedefi tutorial'ın açıldığı ilk frame dahil interactable tutulur. Başarılı
  action gerçek Rally cast/duvar heal işlemini yapar, ilgili session flag'ini yazar ve önceki `1X/2X/3X`
  hızını geri yükler.
- Yeni Rally + Emergency Repair PlayMode regresyonu `1/1`, mevcut core tutorial PlayMode regresyonu
  `1/1` ve saf guided onboarding EditMode paketi `6/6` geçti. Unity compile ve final Console `0 error`;
  `git diff --check` temiz doğrulandı.
- P16 kapalı kalır; ana görev paydası değişmedi ve Post-V1 ilerleme `16/16 - %100` olarak korundu.

### 2026-07-23 - P17 üç saniyelik Arrow teslimatı tamamlandı

- Owner, Arrow refill düğmesinin stoku anında doldurması yerine teslimatın `3` saniye sürmesini ve
  stok barının aynı süreçte kademeli dolmasını kesinleştirdi.
- `GameManager.ArrowDelivery`, ödemesi yapılmış tek aktif siparişi gerçek ECS stokuna simulation
  zamanında kademeli ekler. Pause ilerlemeyi durdurur; oyun hızı teslimat süresini ölçekler.
- UI Toolkit Arrow Supply drawer'ı `DELIVERING · Ns` state'i, altın renk ve gerçek stok oranına
  bağlı width transition'i kullanır. İkinci sipariş exact İngilizce uyarıyla reddedilir.
- Save snapshot bekleyen teslimatı önce stoka flush eder; restart pending state'i temizler.
- Hedefli EditMode `36/36`, birleşik kritik PlayMode `4/4` ve tam Arrow ammo PlayMode `5/5`
  geçti. Unity compile `0 error`; `git diff --check` temizdir.
- Yeni owner-onaylı ana görev nedeniyle payda `16 -> 17` değişti; P17 kapanışıyla Post-V1
  ilerleme `17/17 - %100` oldu.

### 2026-07-23 - P17 post-acceptance atomik teslimat düzeltmesi

- Owner, refill düğmesine basıldığı anda okçuların yeniden ateş etmemesini; satın alınan
  Arrow'ların `3` simulation saniyesi boyunca tamamen yolda ve kullanılamaz kalmasını,
  siparişin tamamının yalnız süre sonunda stoğa gelmesini kesinleştirdi.
- Kök neden önceki teslimat state'inin her frame küçük deltaları canlı `ArrowSupply.Current`
  değerine eklemesiydi. Stok `0` iken gelen ilk delta bile `ArcherShootSystem` tarafından
  kullanılabildiği için okçular üç saniye dolmadan ateşe başlıyordu.
- `GameManager.ArrowDelivery` artık bekleme sırasında canlı stoğa hiç yazmaz; süre dolduğunda
  siparişin tamamını o andaki stoğa tek seferde ekler. Snapshot flush aynı atomik yolu kullanır.
- Supply drawer sayısal `Current / Capacity` değerini gerçek kullanılabilir stokta tutar;
  altın bar mevcut oran ile sipariş sonrası oran arasında yalnız teslimat ilerlemesini gösterir.
- Hedefli EditMode `36/36` geçti. Tam `ArrowAmmoPlayModeTests` `5/5` ve low-ammo tutorial
  refill regresyonuyla birlikte hedefli PlayMode `6/6` geçti. Sıfır stokta tek okçu ve
  `1.000` okçu için teslimat tamamlanmadan stok/rent oluşmadığı doğrulandı.
- Unity compile ve final Console `0 error`; `git diff --check` temizdir. Tracker `v3.2`
  oldu; P17 kapalı kaldı ve ana görev paydası değişmeden `17/17 - %100` korundu.

### 2026-07-24 - P18 Game Center ve TestFlight dokümantasyon kurtarması

- Mac üzerinde oluşturulan `e8389c048` commit'i Apple Core/GameKit paketlerini, iOS
  authentication servisini, build profilini, Project Settings kimliğini, App Store ikonunu
  ve Game Center entegrasyon dokümanını ekledi.
- Mac branch'i güncel P17 Arrow paketini içermediği için iOS işini stale `DW-P17` adıyla
  kaydetmişti. `f22ade045` merge commit'i runtime/paket/Project Settings değişikliklerini
  korurken tracker'ın iOS bölümünü mevcut `v3.2` tarafıyla değiştirdi.
- Otoriter sıra yeniden uzlaştırıldı: Arrow teslimatı `DW-P17-ARROW-DELIVERY` olarak kapalı
  kalır; Apple dağıtım işi `DW-P18-IOS-DISTRIBUTION` kimliğine taşındı.
- Windows canlı Unity MCP, Apple Core `3.2.0` ve Apple GameKit `4.0.1` paketlerinin
  çözüldüğünü, Unity `6000.3.10f1` Editor'ün compile beklemediğini ve `NewGameScene` üzerinde
  idle olduğunu doğruladı. Bu dokümantasyon geçişinde Unity Test Framework çalıştırılmadı.
- Owner build'i TestFlight'a gönderdiğini teyit etti; Mac commit'i ayrıca App Store Connect
  upload ve `Ready to Submit` kaydını bildiriyor. Internal install ve fiziksel cihaz Game Center
  davranışı görülmediği için P18 `[~]` bırakıldı.
- Tracker `v3.3` oldu; payda `17 -> 18` değişti ve ilerleme `17/18 - %94,44` olarak güncellendi.
