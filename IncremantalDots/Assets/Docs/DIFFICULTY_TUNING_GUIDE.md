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
| "Zombiler sunger oldu, oldurmek zevksiz" | Enemy Definition base HP + **SpawnBatchGrowthPerCycle** | V1 HP gunle buyumez; base HP'yi dusur veya tehdidi kalabalikla artir |
| "Ekran zombi doldu, PC hedefinde frame butcesi asildi" | **MaxAliveZombies** | Profil kaniti olmadan 900 ustune cikarma; once Player capture + soak al |
| "Tamir cok pahali, kurtulamiyorum" | Wall Runtime Contract > **RepairStonePerMissingHp** veya **RepairDayPriceMultiplier** | DUSUR |
| "Tamir cok ucuz, duvar onemsizlesti" | Ayni | ARTIR |
| "Duvar cok cabuk yikiliyor / fazla dayanikli" | Wall Runtime Contract > **WallBaseHp** | ARTIR / DUSUR; tech/meta/Heart yuzdeleri bunun ustune biner |
| "Isci ekonomisi cok yavas / fazla hizli" | Economy Runtime Contract > ilgili **WorkerProductionPerMin** | Kaynagin kisi basi baseline'ini ARTIR / DUSUR |
| "Worker bina yatirimlari cok ucuz / pahali" | **CAP/EFF Wood+Iron base cost** ve **WorkerBuildingCostGrowthMultiplier** | Ilk maliyeti veya seviye buyumesini ayarla |
| "Efficiency yatirimi hissedilmiyor / cok guclu" | **WorkerEfficiencyPercentPerLevel** | Her EFF seviyesinin additive uretim yuzdesini ayarla |
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
- Iki aktif spawn egrisi var: **Gece Siddeti** (Night/Dusk-end temposu) ve
  **Spawn Batch** (gunun quantity carpani). **Zombi HP** egrisi V1 quantity-only runtime'da
  dormant legacy alandir; oyunu degistirmez.

## 3. Deger sozlugu (sade dille)

### Kutle Eskalasyonu
| Alan | Ne demek? | Default | Guvenli aralik |
|---|---|---|---|
| ZombieBaseHP | Gun 1 zombisinin cani | 20 | 10-40 |
| ZombieHpGrowthPerCycle | V1 quantity-only runtime'da dormant legacy alan | 0 | Degistirme |
| ZombieBaseDamage / DamagePerCycle | Base damage Enemy Definition owner'indadir; gunluk artis dormant | 5 / 0 | Definition uzerinden tune et |
| SpawnBatchSize | Tek seferde dogan zombi (taban) | 2 | 1-4 |
| SpawnBatchGrowthPerCycle | Kalabaligin gunluk buyumesi (0.15 = gun basi +%15) | 0.15 | 0.05-0.25 |
| MaxSpawnBatch | Tek dogumda ust sinir | 16 | 8-24 |
| MaxAliveZombies | Sahadaki toplam zombi tavani (PC performans sigortasi) | 900 | Player capture + soak kanitina gore |
| BaseSpawnInterval / MinSpawnInterval | Dogumlar arasi sure (taban / taban asagi kirpma) | 0.95 / 0.35 | - |

### Faz Yogunluklari (gunun ritmi)
DAY 0.55 -> DUSK 1.0->1.35 -> NIGHT 1.65 -> DAWN 0.15. Buyuk sayi = sik dogum.
Gece Siddeti EGRISI bu degerlerin USTUNE gun carpani olarak biner (Night ve Dusk-sonu).

### Wall Defense

| Alan | Ne demek? | Default |
|---|---|---:|
| WallBaseHp | Tech/meta/Heart carpanlarindan onceki Wall MaxHP baseline | 350 |
| NormalRepairHealPercent | Day/Dusk normal repair paketinin MaxHP orani | %25 |
| RepairStonePerMissingHp | Gercek iyilestirilen her HP icin Stone | 0.10 |
| RepairDayPriceMultiplier | Day/Dusk repair fiyatina global carpan | 1.0 |
| EmergencyRepairHealPercent | Night, cost-free ability heal orani | %20 |
| EmergencyRepairCooldown | Emergency ability cooldown | 120s |

Normal repair maliyeti `ceil(actualHealHP x Stone/HP x DayPrice x discounts)` formuluyle
hesaplanir. Tech/Heart repair indirimi en son uygulanir. Eski `RepairBaseWoodCost` ve
`RepairBaseStoneCost` alanlari yalniz serialized uyumluluk icin kalir; V1 fiyatini belirlemez.

### Worker Ekonomisi

Profile-owned kisi basi dakika baseline'lari Wood/Stone/Iron/Food icin `8 / 5.5 / 4.9 / 7`dir.
Capacity yatirimi her seviyede sabit `+10` worker slotu verir; ilk fiyat `100 Wood + 25 Iron`dir.
Efficiency yatirimi her seviyede baz kisi uretimine additive `+%10` verir; ilk fiyat
`150 Wood + 50 Iron`dir. Iki yatirim da kendi seviyesinde ortak `1.35` fiyat carpaniyla buyur
ve her alista Wood ile Iron'i birlikte harcar. Efficiency yuzdesi onceki effective sonucu tekrar
carpmaz: base uretim uzerine tech/meta/bina yuzdeleri toplanir, Heart katmani sonradan uygulanir.

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

- Zombi cani (gun G) = aktif Enemy Definition base HP; V1'de gun/cycle ile buyumez.
- Dogum kalabaligi = BatchSize x faz-yogunlugu x (1 + (G-1) x BatchGrowth) x Batch-egrisi(G), tavan MaxSpawnBatch
- Dogum sikligi = BaseInterval / faz-yogunlugu (asagisi MinSpawnInterval'da kirpilir)

### Spawn Runtime Contract paneli

Difficulty Tuner'daki bu panel, `Preview Day` ile BaseSpawn ve Night day-curve carpanlarini
tek yerde gosterir. Play Mode'da live phase, alive/cap, Pending backlog, last/total demand-spawn
ve effective interval okunur. Backlog sayisini elle tune etmezsin: `PreserveDemand` politikasi
cap doluyken talebi kayipsiz saklar. `MaxAliveZombies` sahadaki tavani, `MaxSpawnBatch` kapasite
acildiginda backlog'un frame basina ne kadar hizli eriyecegini belirler.

### Wall Runtime Contract paneli

Base HP, normal repair paketi, Stone/HP, Day fiyat carpani ve Emergency yuzdesi ayni paneldedir.
`Preview missing HP`, tech/Heart indirimi olmadan baseline paket HP/Stone sonucunu gameplay ile
ayni saf formulle gosterir. Play Mode'da profile baseline ile tech/meta/Heart uygulanmis effective
MaxHP, mevcut HP, gercek Stone quote ve phase gate canli okunur. Apply, Wall MaxHP degisirken
mevcut can oranini korur.

### Economy Runtime Contract paneli

Dort kaynagin profile-owned kisi basi baseline'i, CAP/EFF Wood+Iron ilk maliyetleri, ortak fiyat
buyumesi ve EFF seviye yuzdesi ayni paneldedir. `Preview current level`, secilen seviyedeki bir
sonraki CAP/EFF fiyatini ve birikmis slot/uretim etkisini gameplay utility'siyle hesaplar. Play
Mode telemetry'si her kaynak icin worker/effective cap, profile base/effective/total rate, mevcut
CAP/EFF seviyeleri, additive EFF bonusu ve iki sonraki fiyati canli gosterir. Apply, base rate'i
degistirirken mevcut tech/meta/Heart ve bina katmanlarini yeni baseline uzerine yeniden fold eder.

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

1. **Profil her seyi kapsamiyor:** Wall base HP ve dort worker production baseline'i profildedir;
   geometri, cycle sureleri ve worker cap baseline'lari aktif SubScene Authoring'de kalir. Profile
   yoksa Wall ve worker rate alanlari kendi authoring fallback degerlerini kullanir.
2. **APPLY'siz degisiklik oyuna gitmez:** panelde degeri degistirmek yetmez; APPLY
   (edit modda sahneye kaydeder, play modda aninda uygular) sart.
