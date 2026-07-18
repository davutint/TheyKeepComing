# Dead Walls Post-V1 - Follow-up Tracker

> **Amaç:** V1 kapsamı tamamlandıktan sonra yapılacak mantık düzeltmelerini, oyuncuya görünen geri bildirimleri, UI/UX yeniden çalışmalarını, polish işlerini ve performans optimizasyonlarını tek otoriter takip belgesinde yürütmek.
>
> **Tracker sürümü:** 1.1  
> **Oluşturulma tarihi:** 2026-07-18  
> **Aktif paket:** P3 - Tüm Oyuncu UI/UX Denetimi  
> **Aktif iş:** Oyuncunun gördüğü UI yüzeylerinin envanteri ve zamanı gelmiş referans araştırması  
> **İlerleme:** `2 / 10` ana görev tamamlandı - `%20`

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
| 3 | `DW-P3-UIUX-AUDIT` | Oyuncunun gördüğü bütün UI/UX yüzeylerinin uzman denetimi | `[?]` | Mevcut yüzey envanteri, zamanı gelince yapılacak referans araştırması ve onaylı yeniden çalışma planı tamamlandı |
| 4 | `DW-P4-WORKERS` | Worker dağıtım arayüzünün yeniden çalışılması | `[?]` | Kontrol modeli owner ile kararlaştırıldı; yeni layout uygulanıp doğrulandı |
| 5 | `DW-P5-ARROWS` | Arrow Supply arayüzünün yeniden çalışılması | `[?]` | Satın alma ve upgrade bilgi mimarisi kararlaştırıldı; yeni layout uygulanıp doğrulandı |
| 6 | `DW-P6-TECH-TREE` | Teknoloji ağacının görsel ve kullanılabilirlik yeniden çalışması | `[?]` | Okunabilir graph, gezinme, node durumları ve görsel hiyerarşi onaylandı |
| 7 | `DW-P7-CASTLE-HEART` | Castle Heart'ın ayrı kapsamda yeniden değerlendirilmesi | `[?]` | Ürün rolü, etkileşimleri ve sunumu ayrı kararlarla uygulanıp doğrulandı |
| 8 | `DW-P8-UI-POLISH` | Genel UI efektleri, animasyonları ve etkileşim polish'i | `[?]` | Onaylı oyuncu eylemleri tutarlı görsel geri bildirim aldı; gereksiz veya eksik hareket kalmadı |
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
**Durum:** `[?]` Aktif sıradaki görev; araştırma henüz başlatılmadı.

- [?] Oyuncunun görebildiği bütün UI yüzeyleri envanterlenecek.
- [?] Her yüzey amaç, sıklık, önem, ekran konumu ve etkileşim maliyeti açısından değerlendirilecek.
- [?] Benzer oyunlar ve başarılı PC UI/UX örnekleri bu aşamada araştırılacak.
- [?] Dead Walls için tek bir onaylı UI/UX yönü çıkarılacak.
- [?] Global kurallar ile yüzeye özel yeniden çalışma maddeleri ayrıştırılacak.

### `DW-P4-WORKERS` - Worker Dağıtım Arayüzü

**Durum:** `[?]` Kontrol modeli ve layout owner ile henüz kararlaştırılmadı.

- [?] Oyuncunun işçi dağıtırken görmesi gereken bilgiler belirlenecek.
- [?] Dağıtım işleminin kontrol modeli birlikte kararlaştırılacak.
- [?] Capacity ve Efficiency gibi farklı karar türlerinin konumu kararlaştırılacak.
- [?] Onaylı layout uygulandıktan sonra okunabilirlik ve işlem sayısı doğrulanacak.

### `DW-P5-ARROWS` - Arrow Supply Arayüzü

**Durum:** `[?]` Satın alma ve upgrade bilgi mimarisi owner ile henüz kararlaştırılmadı.

- [?] Oyuncunun Arrow stoğu için ihtiyaç duyduğu bilgiler belirlenecek.
- [?] Anlık satın alma seçeneklerinin gerekli kapsamı kararlaştırılacak.
- [?] Capacity ve Efficiency upgrade sunumu ayrıca kararlaştırılacak.
- [?] Onaylı layout uygulandıktan sonra okunabilirlik ve işlem akışı doğrulanacak.

### `DW-P6-TECH-TREE` - Teknoloji Ağacı

**Durum:** `[?]` Görsel ve etkileşim yönü owner ile henüz kararlaştırılmadı.

- [?] Mevcut graph'ın okunabilirlik ve kullanılabilirlik sorunları görev aktif olduğunda çıkarılacak.
- [?] Node hiyerarşisi, bağlantılar, kilit/açık/satın alınmış durumları kararlaştırılacak.
- [?] Gezinme, odak, tooltip ve satın alma geri bildirimi kararlaştırılacak.
- [?] Onaylı yön görsel ve runtime doğrulamadan geçirilecek.

### `DW-P7-CASTLE-HEART` - Ayrı Kapsam

**Durum:** `[?]` Teknoloji ağacından ayrı görüşülecek.

- [?] Castle Heart'ın oyuncuya görünen rolü ve bilgileri yeniden değerlendirilecek.
- [?] Teknoloji ağacıyla ilişkisi ve ayrıldığı sınırlar açıkça yazılacak.
- [?] Etkileşim, geri bildirim ve sunum kararları owner ile ayrı ayrı alınacak.
- [?] Uygulama kendi kabul ve doğrulama kapısıyla tamamlanacak.

### `DW-P8-UI-POLISH` - Genel UI Efekt ve Animasyonları

**Durum:** `[?]` P3-P7 kararları tamamlandıktan sonra ele alınacak.

- [?] Hangi oyuncu eylemlerinin anlık geri bildirim gerektirdiği çıkarılacak.
- [?] Açılma, kapanma, hover, press, satın alma, kazanım, hata ve cooldown durumları değerlendirilecek.
- [?] Hareket dili, süreler ve öncelik kuralları birlikte kararlaştırılacak.
- [?] Efektlerin bilgi okunabilirliğini ve performansı bozmadığı doğrulanacak.

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
