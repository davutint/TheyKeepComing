# Zorluk Ayar Kilavuzu (owner icin — teknik degil, pratik)

> Bu kilavuz "hangi degeri neden degistiririm" sorusuna cevap verir.
> Teknik mimari icin: `Editor/DIFFICULTY_TUNER_ARCHITECTURE.md`.
> Panel: `Window > DeadWalls > Difficulty Tuner`. Akis: degistir -> APPLY -> (Play'de) RUN BOT -> histograma bak.

---

## 1. ONCE BUNU OKU: "Hangi hissi degistirmek istiyorum?"

Deger adi ezberleme — asagidaki tablodan hissini bul, hangi ayara dokunacagini gor:

| Sikayet / istek | Dokunacagin ayar | Yon |
|---|---|---|
| "Ilk gunler cok zor, ogrenemeden oluyorum" | Gun Egrileri > **Gece Siddeti** egrisinin ILK gunleri | Ilk keyframe'leri DUSUR (orn. gun1'i 0.5 -> 0.4) veya rampi UZAT (1.0'a gun 7 yerine gun 10'da cikar) |
| "Ilk gunler cok kolay, sikiliyorum" | Ayni egri | Ilk degerleri YUKSELT veya rampi KISALT |
| "Gec oyunda tehdit hissetmiyorum, para birikiyor" | **SpawnBatchGrowthPerCycle** + **MaxSpawnBatch** | ARTIR (kalabalik buyur) |
| "Zombiler sunger oldu, oldurmek zevksiz" | **ZombieHpGrowthPerCycle** DUSUR + **SpawnBatchGrowthPerCycle** ARTIR | Zorluk HP'den degil KALABALIKTAN gelsin (tasarim ilkemiz) |
| "Ekran zombi doldu, telefon kaldirmaz" | **MaxAliveZombies** | DUSUR (performans tavani) |
| "Tamir cok pahali, kurtulamiyorum" | Tamir Maliyeti > **RepairBase*Cost** | DUSUR |
| "Tamir cok ucuz, duvar onemsizlesti" | Ayni | ARTIR |
| "Gunduz cok sakin / cok yogun" | Faz Yogunluklari > **DayIntensity** | 0.55 taban; artir/azalt |
| "Gece yeterince korkutucu degil" | **NightIntensity** | ARTIR (1.65 taban) |
| "Belirli bir GUN cok sert/yumusak" | Ilgili egriye o gune keyframe ekle | Egri = gun bazli ince ayar |
| "Ok cok cabuk bitiyor" | Ekonomi Fiyat Egrileri > **Arrow kapasite / Arrow per Wood** | Kapasiteyi, paket verimini veya efficiency kazancini ARTIR |
| "Ok ekonomisi anlamsiz ucuz" | **Arrow CAP/EFF Wood+Iron base cost** | Ilgili base maliyetleri ARTIR; refill unit price satin alma sayisiyla buyumez |

---

## 2. Egriler nasil okunur? (panelin en guclu kismi)

- **Yatay eksen = GUN** (1'den SampleDays'e, varsayilan 60).
- **Dikey eksen = CARPAN**: `1.0 = etkisiz`, `0.5 = o gun yari siddet`, `1.5 = o gun %50 daha sert`.
- Egri uzerine **cift tikla** = yeni nokta (keyframe); noktayi surukle = degeri degistir;
  noktaya sag tik = silme/teget secenekleri.
- Ornek (su anki default Gece Siddeti): `(gun1, 0.5) (gun3, 0.7) (gun5, 0.85) (gun7, 1.0)`
  -> ilk hafta kademeli isinma, sonra tam siddet. "Olum bandini" DAY 2-3'ten DAY 6+'ya
  tasiyan degisiklik BUYDU (kod degil, bu dort nokta).
- Uc egri var: **Gece Siddeti** (spawn temposunu buker — en cok kullanacagin),
  **Zombi HP** (o gunun zombilerini sisirir/inceltilir), **Spawn Batch** (dalga kalabaligi).

## 3. Deger sozlugu (sade dille)

### Kutle Eskalasyonu
| Alan | Ne demek? | Default | Guvenli aralik |
|---|---|---|---|
| ZombieBaseHP | Gun 1 zombisinin cani | 20 | 10-40 |
| ZombieHpGrowthPerCycle | Her gun cana eklenen oran (0.40 = gun basi +%40 taban) | 0.40 | 0.2-0.6 |
| ZombieBaseDamage / DamagePerCycle | Duvara vurus hasari (taban + gunluk artis) | 5 / 0.5 | - |
| SpawnBatchSize | Tek seferde dogan zombi (taban) | 2 | 1-4 |
| SpawnBatchGrowthPerCycle | Kalabaligin gunluk buyumesi (0.15 = gun basi +%15) | 0.15 | 0.05-0.25 |
| MaxSpawnBatch | Tek dogumda ust sinir | 16 | 8-24 |
| MaxAliveZombies | Ekrandaki toplam zombi tavani (PERFORMANS sigortasi) | 900 | telefon testine gore |
| BaseSpawnInterval / MinSpawnInterval | Dogumlar arasi sure (taban / taban asagi kirpma) | 0.95 / 0.35 | - |

### Faz Yogunluklari (gunun ritmi)
DAY 0.55 -> DUSK 1.0->1.35 -> NIGHT 1.65 -> DAWN 0.15. Buyuk sayi = sik dogum.
Gece Siddeti EGRISI bu degerlerin USTUNE gun carpani olarak biner (Night ve Dusk-sonu).

### Tamir Maliyeti
Tam yikimda odenen taban (120 odun / 50 tas); kismi hasarda oranla azalir.
Tech'teki "Repair Efficiency" bunu ayrica dusurur.

### Arrow Ekonomisi

Default finite stok `200`, refill paketi `100`, verim `4 Arrow/Wood`dur. Capacity
yatirimi seviye basina `+200`, Efficiency yatirimi seviye basina `+1 Arrow/Wood`
verir. CAP ve EFF alimlari Wood+Iron ister ve fiyatlari kendi seviyeleriyle `1.35`
carpaninda buyur. Refill birim fiyati kac kez alindigina gore buyumez; Rapid gibi daha
hizli okcular talebi dogal olarak artirir.

### M-C Hazirlik (SpawnTable / SpecialNights)
SIMDILIK BOS BIRAK — zombi cesitliligi milestone'unda (M-C) sistem bunlari okumaya
baslayacak ("kosucular gun 5'te acilsin", "her 5. gece kanli ay" buradan ayarlanacak).

## 4. Meraklisina: formullerin sade hali

- Zombi cani (gun G) = BaseHP x (1 + (G-1) x HpGrowth) x HP-egrisi(G)
  - Ornek: gun 5 = 20 x (1 + 4 x 0.4) = 52 can
- Dogum kalabaligi = BatchSize x faz-yogunlugu x (1 + (G-1) x BatchGrowth) x Batch-egrisi(G), tavan MaxSpawnBatch
- Dogum sikligi = BaseInterval / faz-yogunlugu (asagisi MinSpawnInterval'da kirpilir)

## 5. Olcum botu nasil yorumlanir?

1. Play'e gir -> RUN BOT (profili uygular, temiz kosu baslatir, 3x hizda oynar).
2. Kosular bitince "Yenile" -> histogram:
   - **KIRMIZI cubuklar** = olum gunleri (nerede yigilmis?)
   - **YESIL serit** = olmeden ulasilan en yuksek gun
3. HEDEF: olumler **DAY 8-15** bandinda yigilsin (1x hizda ~8-15 dakikalik kosu, C1 kriteri).
   - Kirmizilar solda yigildiysa -> erken oyun cok sert (rampi uzat/dusur)
   - Hic kirmizi yoksa ve yesil hep 20'deyse -> gec oyun cok kolay (BatchGrowth/MaxBatch artir)
4. Bot "ortalama oyuncu"dur, optimal degil — senin elle oynayisin 2-4 gun daha uzun gidebilir.

## 6. Bilinmesi gereken iki tuzak

1. **Profil her seyi kapsamiyor:** Duvar HP'si (350) ve iron uretimi gibi degerler setup
   tool'un icinde yasar — onlari degistirmek istersen soyle, setup sabitinde guncelleriz
   (elle Inspector'dan degistirirsen bir sonraki Setup kosusu geri ezer!).
2. **APPLY'siz degisiklik oyuna gitmez:** panelde degeri degistirmek yetmez; APPLY
   (edit modda sahneye kaydeder, play modda aninda uygular) sart.
