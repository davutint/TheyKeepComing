# Dead Walls Post-V1 - Follow-up Tracker

> **Amaç:** V1 kapsamı tamamlandıktan sonra yapılacak mantık düzeltmelerini, oyuncuya görünen geri bildirimleri, UI/UX yeniden çalışmalarını, polish işlerini ve performans optimizasyonlarını tek otoriter takip belgesinde yürütmek.
>
> **Tracker sürümü:** 1.2
> **Oluşturulma tarihi:** 2026-07-18
> **Aktif paket:** P9 - Performans Profilleme ve Optimizasyon
> **Aktif iş:** Hedef donanım ve ölçüm senaryolarının owner ile kesinleştirilmesi
> **İlerleme:** `8 / 10` ana görev tamamlandı - `%80`

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

İlerleme, Bölüm 3'teki 10 ana görev üzerinden hesaplanır. Alt maddeler kanıt ve kabul kapsamını gösterir; ayrıca paydayı büyütmez. Ana görevler yalnızca bütün zorunlu alt kapıları tamamlandığında kapanır.

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
| 9 | `DW-P9-PERFORMANCE` | Performans profilleme ve optimizasyon | `[?]` | Ölçülmüş darboğazlar davranış bozulmadan giderildi ve hedef senaryolarda yeniden ölçüldü |
| 10 | `DW-P10-FINAL-GATE` | Son kalite, tutarlılık ve regresyon kapısı | `[?]` | Bütün aktif kapsam birlikte test edildi; açık kritik hata ve doğrulanmamış ana görev kalmadı |

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

**Durum:** `[?]` Önceki oyuncu davranışları kesinleşip uygulandıktan sonra ele alınacak.

- [?] Hedef donanım ve ölçüm senaryoları görev aktif olduğunda kesinleştirilecek.
- [?] CPU, GPU, bellek, GC ve yoğun UI/combat senaryoları ölçülecek.
- [?] Yalnızca kanıtlanmış darboğazlar optimize edilecek.
- [?] Optimizasyonların gameplay ve oyuncuya görünen geri bildirimleri değiştirmediği doğrulanacak.

### `DW-P10-FINAL-GATE` - Son Kalite Kapısı

**Durum:** `[?]` P1-P9 tamamlandıktan sonra açılacak.

- [?] Mantık düzeltmeleri için regresyon matrisi tamamlanacak.
- [?] Oyuncunun gördüğü bütün UI yüzeyleri birlikte gözden geçirilecek.
- [?] Yoğun combat, uzun süreli oyun ve save/continue senaryoları doğrulanacak.
- [?] Açık kritik hata, geçici placeholder ve doğrulanmamış ana görev kalmadığı kanıtlanacak.

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
