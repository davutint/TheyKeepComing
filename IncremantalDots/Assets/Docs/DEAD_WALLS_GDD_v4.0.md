# DEAD WALLS - Game Design Document v4.0

**RTS + Kasaba Insa + Roguelike Colony Defense**
**Platform:** Steam (PC) | **Engine:** Unity 6 - DOTS/ECS + Burst Compiler + Job System

---

## 1. Oyun Ozeti

### 1.1 Konsept
Dead Walls, Orta Cag fantezi atmosferinde gecen bir RTS + kasaba insa + roguelike colony defense oyunudur. Oyuncu izometrik perspektiften kasabasini kurar, askerlerini yonetir ve perimeter surlarini savunarak 100 gun boyunca hayatta kalmaya calisir. Her run'da level atlama kartlariyla farkli build stratejileri denenir. 50.000 zombilik final dalgasi oyunun son sinavi.

### 1.2 Temel Bilgiler
| Alan | Detay |
|------|-------|
| Tur | RTS + Kasaba Insa + Roguelike Colony Defense |
| Platform | Steam (PC) — mobil sonra dusunulabilir |
| Engine | Unity 6 - DOTS/ECS + Burst Compiler + Job System |
| Hedef Kitle | Strateji + colony defense + roguelike severleri |
| Session Uzunlugu | ~2 saat (tam 100 gun run) |
| Monetizasyon | Tek seferlik satin alma (Steam) |
| Gorsel Stil | Pixel Art, 2D Izometrik, Tilemap |
| Tema | Orta Cag Fantezi, Zombi Istilasi |
| Referans Oyunlar | They Are Billions + Slay the Spire kart sistemi |

### 1.3 Elevator Pitch
"Kasabani kur, askerlerini yonet, surlarini savun. 100 gun hayatta kal. Her level atlamada farkli bir strateji sec. Ama zombiler her gun daha kalabalik geliyor — ve 100. gun, 50.000'i birden geliyor. Duvarlarinin catlamasini, yikilmasini izle — ya da onlari durdur."

### 1.4 Unique Selling Points
- **50.000 Zombi Ordusu:** DOTS/ECS ile devasa suru, ekrani kaplayan spectacle
- **2D Destruction:** Duvar catlama, yikilma, enkaz efektleri — dramatik gerilim ani
- **Roguelike Kart Sistemi:** Her run farkli build path — They Are Billions'da olmayan replayability
- **RTS Birim Kontrolu:** Askerleri Age of Empires tarzi yonet, sehir halki otomatik calisir

---

## 2. Temel Dongu (Core Loop)

### 2.1 Anlik Dongu (Bir Gun Icinde)
1. Gun baslar — kaynaklar uretilir, binalar calisir, sehir halki otomatik islerini yapar
2. Zombiler perimeter suruna taciz saldirisi yapar (kucuk gruplar, her yonden)
3. Oyuncu askerlerini (okcu + buyucu) stratejik konumlandirir ve yonetir
4. Askerler menzildeki dusmanlara otomatik saldirirlar
5. Oldurulen zombilerden XP kazanilir
6. Oyuncu kaynak yonetir: isci ata, bina yap/yukselt, tuzak kur
7. Yeterli XP biriktiginde Level Atlama ekrani acilir — 3 karttan 1'i secilir
8. Gun biter, sonraki gune gecilir (otomatik)

### 2.2 Wave Dongusu
1. Birkac gunde bir buyuk zombi dalgasi gelir (her yonden)
2. Dalga geldiginde gun uzar — dalga bitene kadar devam eder
3. Dalga atlatilirsa oyun devam eder
4. Dalga sikligi gun ilerledikce artar
5. 100. gun final wave — 50.000 zombi, tum yonlerden

### 2.3 Meta Dongu (Run Bazli)
- Her run bagimsizdir — run'lar arasi hicbir sey tasinmaz
- Tekrar oynama motivasyonu: farkli build path'ler denemek
- Game over ekraninda istatistikler gosterilir (gun, kill, kaynak vs.)
- Her run'da farkli level-up kartlari cikar → farkli stratejiler

---

## 3. Run Yapisi ve Gun Sistemi

### 3.1 100 Gun Yapisi
Oyun 100 gun surer. Her gun sabit bir sure devam eder (Inspector'dan ayarlanabilir, varsayilan ~60 saniye). Wave gelen gunlerde gun uzar, dalga bitene kadar devam eder.

| Gun Araligi | Ozellik |
|-------------|---------|
| 1-20 | Kurulum donemi, seyrek wave, kasaba insasi odakli |
| 21-50 | Orta zorluk, wave sikligi artar |
| 51-80 | Zor, neredeyse her 2 gunde wave |
| 81-99 | Cok zor, her gun wave |
| 100 | Final wave — 50.000 zombi, son sinav |

### 3.2 Gun Dongusu
- Gunler otomatik ilerler (buton yok)
- Gun icinde oyuncu serbestce bina yapar, isci atar, askerleri yonetir, kaynak yonetir
- Gun gecislerinde gun sayaci artar
- DOTS seviyesinde performansli bir DayCycleSystem ile yonetilir

### 3.3 Kazanma / Kaybetme
- **Kazanma:** 100. gun dalgesini atlatmak
- **Kaybetme:** Kale HP'si sifira duserse oyun biter (surlar yikilir → kaleye ulasilir)
- Duvar yikildiginda dramatik sahne: destruction efektleri, catlama, enkaz, ciglik sesleri
- **Game Over Ekrani:** Istatistikler gosterilir:
  - Hayatta kalinan gun sayisi
  - Oldurulen toplam zombi sayisi
  - Maksimum nufus
  - Toplam uretilen kaynak miktarlari
  - Secilen level-up kartlari listesi

---

## 4. Kaynak Sistemi

### 4.1 Kaynaklar
Gold yoktur. Oyun 4 temel kaynakla calisir:

| Kaynak | Uretim Yeri | Ana Giderleri |
|--------|------------|---------------|
| **Ahsap** | Oduncu (orman yanina) | Ok uretimi, bina insaat, sivri kazik tuzagi |
| **Tas** | Tas Ocagi (tas kaynagi yanina) | Duvar onarimi, bina insaat, hendek tuzagi, Buyucu Kulesi insaat |
| **Demir** | Maden (demir kaynagi yanina) | Demirci upgrade'leri, bina bakimi, ayi tuzagi |
| **Yemek** | Ciftlik (herhangi bir yere) | Nufus beslemek (ev + isci + okcu + buyucu) |

### 4.2 Kaynak Akisi
Her kaynagin surekli gideri vardir — hicbir kaynak sonsuz birikmez.

```
Ahsap Akisi:  Oduncu → Ahsap → Ok Atolyesi (ok uretimi) + Bina insaat + Tuzak
Tas Akisi:    Tas Ocagi → Tas → Duvar onarimi + Bina insaat + Hendek + Buyucu Kulesi
Demir Akisi:  Maden → Demir → Demirci upgrade'leri + Bina bakimi + Ayi tuzagi
Yemek Akisi:  Ciftlik → Yemek → Nufus beslemek (tum atanmis nufus yemek tuketir)
```

### 4.3 Kaynak Dengeleme Prensibi
- Daha cok okcu → daha cok ok lazim → daha cok ahsap lazim → daha cok oduncu iscisi lazim → daha cok yemek lazim
- Daha cok buyucu → daha cok yemek + egitim maliyeti (sonra belirlenecek)
- Her kaynak zinciri birbirine bagli
- Oyuncu surekli "nereye yatirim yapayim" kararini vermek zorunda

### 4.4 Dogal Kaynaklar
Haritada sabit konumlarda dogal kaynak noktalari bulunur:
- **Orman** — Oduncu binasi buraya yakin yapilmalidir
- **Tas Kaynagi** — Tas Ocagi buraya yakin yapilmalidir
- **Demir Kaynagi** — Maden buraya yakin yapilmalidir
- Ciftligin ozel bir yerlestirme kurali yoktur, herhangi bir yere yapilabilir

---

## 5. Bina Sistemi

### 5.1 Genel Kurallar
- Binalar izometrik grid'e yerlestirilir (Tilemap tabanli)
- Her bina su an icin 3x3 grid slot kaplar (ileride bina bazinda degisebilir)
- Bazi binalarin yerlestirme kurallari vardir (orman yanina, tas yanina vs.)
- Binalar rotate edilemez
- Binalar upgrade edilebilir (seviye sistemi)
- Grid alani kisitlidir — her bina secimi bir tradeoff'tur

### 5.2 Kaynak Binalari

**Oduncu (Lumberjack)**
- Yerlestirme: Orman yanina
- Isci atanir, ne kadar isci → o kadar ahsap/dakika
- Upgrade: Uretim verimi artar

**Tas Ocagi (Quarry)**
- Yerlestirme: Tas kaynagi yanina
- Isci atanir, ne kadar isci → o kadar tas/dakika
- Upgrade: Uretim verimi artar

**Maden (Mine)**
- Yerlestirme: Demir kaynagi yanina
- Isci atanir, ne kadar isci → o kadar demir/dakika
- Upgrade: Uretim verimi artar

**Ciftlik (Farm)**
- Yerlestirme: Herhangi bir yer
- Isci atanir, ne kadar isci → o kadar yemek/dakika
- Upgrade: Uretim verimi artar
- Kritik bina — yemek tukenirse nufus beslenemez

### 5.3 Altyapi Binalari

**Ev (House)**
- Nufus kapasitesi arttirir
- Upgrade edilebilir (daha fazla kapasite)
- Surekli yemek gideri vardir (icinde yasayanlar yemek tuketir)

| Ev Seviyesi | Kapasite | Yemek Gideri/dk |
|-------------|----------|-----------------|
| 1 | +5 | -3 |
| 2 | +10 | -7 |
| 3 | +15 | -12 |

**Kale (Castle)**
- Haritanin merkezinde, oyun basindan itibaren var
- Upgrade ile nufus kapasitesi artar (Tas + Ahsap harcar)
- Ayri ev binasi gerektirmeden kapasite eklemenin yolu
- Kale upgrade = pahali ama grid slot yemez

### 5.4 Askeri Binalar

**Kisla (Barracks)**
- Nufustan okcu egitir
- Kaynak harcar: Yemek + Ahsap
- Egitilen okcu haritada serbest dolasir, oyuncu tarafindan yonetilir
- Upgrade: Daha hizli egitim, ayni anda daha fazla okcu egitebilme

**Ok Atolyesi (Fletcher)**
- Isci atanir, ahsap tuketir, ok uretir
- Okcular ok olmadan ates edemez veya cok yavas ates eder
- Uretim zinciri: Oduncu → Ahsap → Ok Atolyesi → Okcular ates eder
- Upgrade: Daha hizli uretim

**Buyucu Akademisi (Wizard Academy)**
- Nufustan ates buyucusu egitir
- Kaynak harcar: Yemek + (maliyet sonra belirlenecek)
- Egitilen buyucu haritada serbest dolasir, oyuncu tarafindan yonetilir
- Upgrade: Daha hizli egitim

**Demirci (Blacksmith)**
- Demir tuketir, kalici upgrade'ler sunar
- Oyunun tech tree binasi
- Upgrade secenekleri:
  - Ok hasari artir (demir ok ucu)
  - Duvar guclendirme (demir takviye)
  - Sur kapisi guclendirme
  - Buyucu Kulesi unlock
  - Tuzak unlock
- Upgrade: Daha gelismis tech secenekleri acilir

### 5.5 Savunma Binalari

**Buyucu Kulesi (Wizard Tower)**
- Grid'e yerlestirilen savunma binasi
- Demirciden acilir (unlock)
- Genis AoE ates buyusu atar (otomatik, cooldown tabanli)
- Icinde birim barindirmaz — bina kendisi saldiri yapar
- Uzun menzil, buyuk AoE radius, yavas cooldown
- Upgrade: Daha fazla hasar, daha genis AoE, daha kisa cooldown

---

## 6. Nufus Sistemi

### 6.1 Tek Havuz
Oyundaki tum insanlar tek bir nufus havuzundan gelir. Bu nufus isci, okcu veya buyucu olarak kullanilir.

- **Isci** olarak: Kaynak binalarina atanir (oduncu, maden, ciftlik vs.)
- **Okcu** olarak: Kislada egitilir, oyuncu tarafindan yonetilir
- **Buyucu** olarak: Buyucu Akademisi'nde egitilir, oyuncu tarafindan yonetilir
- **Tradeoff:** Daha cok asker = daha az isci = daha az kaynak uretimi

### 6.2 Nufus Kapasitesi
Nufus sinirsiz degildir. Kapasite iki yolla artar:
- **Ev** yaparak (grid slot harcar, yemek gideri var)
- **Kale** upgrade ederek (grid slot harcamaz, pahali)

### 6.3 Nufus Artisi
Yeni nufus **level atlama** sirasinda gelir:
- Level-up kartlarindan biri "Multeciler" olabilir → +X nufus
- Multeciler sadece kapasite varsa gelir
- Kapasite yoksa multeci karti ciksa bile secilemez (veya cikmaz)

### 6.4 Nufus Gideri
Atanmis her birey (isci, okcu veya buyucu) yemek tuketir. Bosta bekleyen nufus yemek tuketmez. Bu sayede oyuncu "5 kisiyi bosta tutayim, lazim olunca atarim" stratejisi uygulayabilir.

---

## 7. Savunma Sistemi

### 7.1 Perimeter Sur Sistemi
- Kasabanin etrafinda perimeter sur bulunur (baslangicta tek segment, genisletilebilir)
- Zombiler tum yonlerden gelir ve surlara saldirir
- Sur segmentleri bagimsiz HP'ye sahiptir — farkli noktalar farkli hizda yipranir
- Sur yikildiginda o segmentten zombiler icerigirer → kaleye ulasirsa oyun biter
- Sur onarimi: Tas kaynagi ile onarim
- Sur guclendirme: Demirciden upgrade ile HP artirilabilir

### 7.2 Destruction Sistemi (Gorsel)
Surlar hasar aldikca gorsel olarak degisir:
- **%100 HP:** Saglam gorunum
- **%75 HP:** Catlaklar belirmeye baslar + impact VFX
- **%50 HP:** Belirgin kiriklar + debris parcaciklari
- **%25 HP:** Harabe gorunum + buyuk catlaklar + screen shake
- **%0 HP:** Yikilma animasyonu + enkaz kalintisi + dramatik efekt

Her hasar asamasi:
- Tile swap (saglam → catlamis → kirik → harabe → enkaz)
- Impact VFX (her vuruata)
- Color flash (beyaz → kirmizi → normal)
- Shake efekti (Perlin noise tabanli, tile bazinda)
- Enkaz kalintisi (BrokenObjects tilemap'e rubble tile)

### 7.3 Askeri Birimler — RTS Kontrolu
Askeri birimler (okcu + buyucu) Age of Empires tarzi kontrol edilir:
- **Secim:** Sol tikla → tek birim sec, kutu ciz → coklu sec
- **Hareket:** Sag tikla zemine → birim oraya yurur (A* pathfinding)
- **Saldiri:** Sag tikla dusmana → birim yaklasir ve saldirmaya baslar
- **Idle Davranis:** Komut yokken pozisyonunda bekler, menzildeki dusmana otomatik saldirir
- **Sur Yerlestirme:** Okcu ve buyuculer surun ustune konulabilir (nice-to-have)

Sehir halki (isciler, koyluler) oyuncu tarafindan kontrol EDiLMEZ — otomatik calisirlar.

### 7.4 Okcular
- Kisladan egitilir, nufustan cikar
- RTS tarzi kontrol edilir (sec, tasi, saldir)
- Menzildeki en yakin zombiye otomatik ok atar
- Ok tuketir — Ok Atolyesi olmadan ates edemez/cok yavas ates eder
- Her okcu yemek tuketir (nufusun parcasi)
- Tek hedef DPS — kalabaliga karsi zayif, tek hedefe etkili

| Ozellik | Deger |
|---------|-------|
| Hasar (base) | Inspector'dan ayarlanabilir |
| Atis hizi (base) | Inspector'dan ayarlanabilir |
| Menzil | Inspector'dan ayarlanabilir |
| Hedefleme | En yakin zombiye oncelik (otomatik) veya oyuncu komutu |

### 7.5 Buyuculer (Ates Buyucusu)
- Buyucu Akademisi'nden egitilir, nufustan cikar
- RTS tarzi kontrol edilir (sec, tasi, saldir)
- Ates topu atar — kucuk AoE, cooldown tabanli, mermi yok
- Kalabaliga karsi etkili (AoE), tek hedefe verimsiz
- Her buyucu yemek tuketir (nufusun parcasi)
- Simdilik sadece Ates Buyucusu — ileride farkli elementler eklenebilir

| Ozellik | Deger |
|---------|-------|
| Hasar (base) | Inspector'dan ayarlanabilir |
| AoE Radius | Inspector'dan ayarlanabilir (Buyucu Kulesi'nden kucuk) |
| Cooldown | Inspector'dan ayarlanabilir |
| Menzil | Inspector'dan ayarlanabilir |
| Hedefleme | En yakin zombi grubuna (otomatik) veya oyuncu komutu |

### 7.6 Buyucu Kulesi (Savunma Binasi)
- Grid'e yerlestirilen otomatik savunma yapisi
- Genis AoE ates buyusu (buyucu biriminden daha buyuk radius)
- Uzun menzil, buyuk AoE, yavas cooldown
- Oyuncu tarafindan kontrol edilmez — tam otomatik
- Mancinik rolunu devralir: agir AoE savunma, ama buyusel

### 7.7 Tuzaklar (Sur Disi)
Sur disina grid mantigi ile yerlestirilen savunma yapilari. Zombiler uzerinden gecince etki eder.

| Tuzak | Etki | Kaynak Maliyeti | Dayaniklilik |
|-------|------|-----------------|--------------|
| **Sivri Kaziklar** | Hasar | Ahsap | Belirli sayida zombiden sonra kirilir |
| **Hendek** | Yavaslatma | Tas (kazi) | Kalici, kirilmaz |
| **Ayi Tuzagi** | Hasar + durdurma | Demir | Tek kullanimlik, tekrar kurulmasi lazim |

### 7.8 Savunma Hiyerarsisi
```
Fiziksel Savunma          Buyusel Savunma            Pasif Savunma
─────────────────         ─────────────────          ─────────────
Okcu (birim)              Buyucu (birim)             Tuzaklar
Tek hedef DPS             Kucuk AoE + Cooldown       Sur disi engeller
Ok tuketir                Cooldown tabanli           Sarf malzeme
Kontrol edilebilir        Kontrol edilebilir         Yerlestir ve unut

                          Buyucu Kulesi (bina)       Perimeter Sur
                          Genis AoE + Otomatik       Duvar bariyeri
                          Kontrol edilmez            Onarilabilir
```

---

## 8. Level Atlama ve Roguelike Kart Sistemi

### 8.1 XP ve Level Esigi
- Her oldurulen zombi XP kazandirir
- XP esigi her level'da artar (Inspector'dan ayarlanabilir)
- Esik asildiginda oyun duraklar, Level Atlama ekrani acilir

### 8.2 Kart Secim Mekanigi
- 3 kart gosterilir, oyuncu 1 tanesini secer
- Secim kalicidir (o run icin)
- Ayni karti tekrar secersen **tier atlar** (daha guclu versiyon)
- Bazi kartlar **on kosul** gerektirir:
  - Buyucu kartlari → sadece Buyucu Akademisi varsa cikar
  - Ileri okcu kartlari → sadece Kisla varsa cikar

### 8.3 Kart Kategorileri

**Nufus Kartlari:**
- Multeciler (+X nufus, kapasite varsa)

**Okcu Kartlari:**
- Ok Hasari Artisi
- Atis Hizi Artisi
- Coklu Ok (multishot)
- Menzil Artisi

**Buyucu Kartlari (Buyucu Akademisi gerektirir):**
- Ates Topu Guclendirme (daha buyuk AoE, daha cok hasar)
- Cooldown Azaltma
- Menzil Artisi
- Ek buyucu tipleri (ileride: Buz, Simsek vs.)

**Savunma Kartlari:**
- Duvar HP Artisi
- Hendek (yavaslatma alani)
- Barikat (gecici zombi engeli)

**Ekonomi Kartlari:**
- Uretim Hizi Boost (tum kaynaklar +%X)
- Verimli Isciler (ayni isci sayisiyla daha fazla uretim)

### 8.4 Build Path'ler
Kart sistemi farkli build stratejilerini destekler:

| Build | Strateji | Odaklanilan Kartlar |
|-------|----------|---------------------|
| **Okcu Build** | Cok okcu, yuksek DPS | Ok hasari, atis hizi, coklu ok |
| **Buyucu Build** | AoE crowd control | Ates topu, cooldown, menzil |
| **Kale Build** | Dayaniklilik, turtle | Duvar takviye, hendek, onarim |
| **Ekonomi Build** | Erken yavas, gec guclu | Kaynak boost, nufus, verimli isci |
| **Hibrit** | Her seyden biraz | Duruma gore secim |

---

## 9. Zombi Sistemi

### 9.1 Tek Tip Zombi
Oyunda tek bir zombi tipi vardir. Zombilerin gucu bireysel degil, **kalabaliklaridir**.

| Ozellik | Aciklama |
|---------|----------|
| HP | Gun numarasina gore olceklenir |
| Hiz | Sabit (Inspector'dan ayarlanabilir) |
| Hasar | Sura saniye basina verdigi hasar |

### 9.2 Zombi Davranisi
- Zombiler haritanin kenarlarindan (360°) gelir ve kaleye/surlara dogru yurur
- Perimeter suruna ulasinca saldirmaya baslar
- Sur yikilirsa o bolgeden icerigirer, kaleye yonelir
- DOTS/ECS ile on binlerce zombi ayni anda yonetilebilir
- Spatial hash grid ile verimli carpisma yonetimi
- Zombilerde pathfinding YOKTUR — duz yurume + collision + domino queuing

### 9.3 Zombi Olceklemesi
```
Zombi_Sayisi(gun) = Inspector'dan ayarlanabilir egri
Zombi_HP(gun) = Inspector'dan ayarlanabilir egri
```
Genel prensip: gun ilerledikce zombi sayisi ve HP'si artar. 100. gun icin 50.000 zombi hedeflenir.

### 9.4 Taciz Sistemi
Wave disinda da kucuk zombi gruplari surekli gelir ve perimeter suruna taciz saldirisi yapar. Oyuncu hicbir zaman tamamen guvende degildir. Taciz sikligi ve kalabaligi gun ilerledikce artar. Taciz gruplari farkli yonlerden gelebilir.

---

## 10. Event Sistemi

### 10.1 Genel Yapi
Rastgele gunlerde ozel event'ler tetiklenir. Event'ler oyuna cesitlilik katar ve her run'i farkli kilar.

### 10.2 Olumlu Event'ler
| Event | Etki |
|-------|------|
| Komsu Kasabadan Erzak | +Tas, +Ahsap |
| Multeciler Kapida | +Nufus (kapasite varsa) |
| Gezgin Tuccar | Kaynak takasi yapabilirsin (ornegin 50 Tas → 20 Demir) |

### 10.3 Olumsuz Event'ler
| Event | Etki |
|-------|------|
| Firtina | Ciftlikler 1 gun uretmiyor |
| Maden Coktu | Maden 2 gun devre disi |
| Veba | Nufus kaybi |

### 10.4 Secimli Event'ler
Oyuncuya risk/odul karari sunulur:
- "Bir kasaba yardim istiyor: 100 yemek gonder → 3 gun sonra 50 demir gelir. Gonderme → hicbir sey olmaz."
- Bu event'ler basit UI popup + kaynak degisikligi ile uygulanir

### 10.5 Teknik Not
Event'ler Inspector/Editor Window'dan ayarlanabilir olmalidir:
- Event havuzu
- Her event'in cikma olasiligi
- Hangi gunlerde cikmaya baslayacagi
- Etki degerleri

---

## 11. Dalga (Wave) Sistemi

### 11.1 Dalga Sikligi
Wave sikligi gun ilerledikce artar:

| Gun Araligi | Ortalama Wave Sikligi |
|-------------|----------------------|
| 1-20 | Her 5 gunde bir |
| 21-50 | Her 3 gunde bir |
| 51-80 | Her 2 gunde bir |
| 81-99 | Her gun |
| 100 | Final wave |

Bu degerler Inspector/Editor Window'dan ayarlanabilir.

### 11.2 Dalga Mekanigi
- Wave gelen gunde gun sabit suresi dolsa bile uzar
- Wave, tum zombiler oldurulene veya kale yikilana kadar devam eder
- Wave sirasinda oyuncu hala bina yapabilir, kaynak yonetebilir ve asker komutlari verebilir
- Wave kalabaligi gun numarasina gore olceklenir
- Zombiler tum yonlerden gelir — perimeter savunmasini test eder

### 11.3 Final Wave (Gun 100)
- 50.000 zombi, tum yonlerden
- Oyuncunun 100 gunluk build'inin son sinavi
- Hayatta kalirsa → zafer
- Kale yikilirsa → game over (ama 100. gune ulasmanin kendisi bir basari)

---

## 12. Gorsel Stil ve Perspektif

### 12.1 Izometrik Perspektif
- 2D izometrik goruntuleme (2:1 ratio, diamond grid)
- Grid cell boyutu: 1 × 0.5 world unit (izometrik standart)
- Y-based depth sorting (yakin objeler onde, uzak objeler arkada)
- Kaydirmali/zoomlanabilir kamera (buyuk haritalar icin)

### 12.2 Art Asset'leri
- **Ortam:** Fantasy Kingdom Tileset (SmallScale Interactive) — izometrik tile'lar, duvar, bina, destruction efektleri
- **Karakterler:** Character Creator - Fantasy 2D (SmallScale Interactive) — 8 yonlu, 128x128, modular karakter olusturma
- **Uyum:** Her iki asset ayni publisher'dan, ayni art style, 128 PPU

### 12.3 Sprite Sistemi
- 8 yon animasyon (E, W, S, N, NE, NW, SE, SW)
- 15 frame per animasyon (ayarlanabilir: 4-15)
- Spritesheet formati: 1920x1024 PNG (15 sutun × 8 satir)
- Frame boyutu: 128×128 piksel
- PPU: 128

### 12.4 Ambient Canlilik
Kasabanin yasadigi hissini vermek icin ambient koylu entity'leri bulunur:
- Binalara dogru yuruyen isciler
- Kasaba icinde dolasan koyluler
- Surda nobet tutan okcular (gorsel)
- Sakin gunlerde huzurlu, wave geldiginde kaos

---

## 13. DOTS/ECS Teknik Mimarisi

### 13.1 Neden DOTS?
| Senaryo | MonoBehaviour | DOTS/ECS |
|---------|---------------|----------|
| 1.000 zombi | ~45 FPS | ~120 FPS |
| 5.000 zombi | ~8 FPS | ~90 FPS |
| 20.000 zombi | ~1 FPS (crash) | ~60 FPS |
| 50.000 zombi | Mumkun degil | ~30-40 FPS |

Final wave (50.000 zombi) icin DOTS zorunludur.

### 13.2 ECS Component Tasarimi

**Zombi Entity:**
- ZombieTag, LocalTransform, ZombieStats (HP, speed, damage)
- ZombieState (Moving / Attacking / Dead / Queued)

**Askeri Birim Entity'leri (Okcu + Buyucu):**
- MilitaryUnit (unitType, isSelected)
- UnitMovement (targetPosition, moveSpeed, isMoving)
- UnitCombat (damage, attackRange, attackCooldown, attackTimer)
- PathBuffer (DynamicBuffer<int2> — A* waypoint listesi)
- ArcherUnit (fireRate, arrowCount) — sadece okcular
- WizardUnit (spellDamage, aoeRadius, spellCooldown) — sadece buyuculer
- ArrowProjectile (speed, damage, targetRef)

**Bina Entity'leri:**
- BuildingTag, BuildingType (enum), BuildingLevel
- ResourceProducer (resourceType, ratePerWorker, assignedWorkers)
- PopulationCapacity (amount)
- FoodConsumer (consumeRate)
- WizardTowerUnit (damage, aoeRadius, range, cooldown, cooldownTimer) — sadece Buyucu Kulesi

**Duvar Entity:**
- WallSegment (maxHP, currentHP, repairRate, damageStage)

**Game State (Singleton):**
- GameState (currentDay, totalDays, xp, level, isGameOver)
- WaveState (zombiesRemaining, zombiesSpawned, totalZombies, waveActive)
- ResourceState (wood, stone, iron, food)
- PopulationState (total, workers, archers, wizards, idle, capacity)

### 13.3 Temel System'ler
| System | Gorev | Frekans |
|--------|-------|---------|
| DayCycleSystem | Gun sayaci, gun gecisi | Her frame |
| WaveSpawnSystem | Dalga baslangicinda zombi spawn (360°) | Wave basinda |
| GridPathfindingSystem | A* pathfinding askeri birimler icin | Istek uzerine |
| UnitMovementSystem | Askeri birim hareketi (waypoint takip) | Her frame |
| UnitSelectionSystem | Birim secim + komut isleme | Her frame |
| ArcherShootSystem | Okcu atis + hedef secimi | Her frame |
| WizardShootSystem | Buyucu ates topu + AoE hedefleme | Her frame |
| WizardTowerShootSystem | Buyucu Kulesi otomatik AoE | Her frame |
| ArrowMoveSystem | Ok hareketi + isabet kontrolu | Her frame |
| ApplyMovementForceSystem | Zombileri kaleye dogru hareket ettirir | Her frame |
| BuildSpatialHashSystem | Pozisyonlari hash grid'e yaz (double buffer) | Her frame |
| PhysicsCollisionSystem | Circle-circle carpisma + pozisyon duzeltme | Her frame |
| IntegrateSystem | velocity + position guncelleme | Her frame |
| BoundarySystem | Perimeter sur bariyeri + domino queuing | Her frame |
| TrapSystem | Tuzak tetikleme + hasar/yavaslatma | Her frame |
| ZombieAttackTimerSystem | Zombie saldiri timer + hasar queue | Her frame |
| DamageApplySystem | TEK SYNC POINT — hasar uygulama | Her frame |
| DestructionVisualSystem | Sur hasar asamasi → tile swap tetikleme | Her frame |
| ResourceTickSystem | Kaynak uretim/tuketim | Her frame |
| PopulationTickSystem | Nufus hesaplama | Her frame |
| BuildingProductionSystem | Bina uretim hizlari | Her frame |
| SpriteAnimationSystem | 8 yonlu sprite animasyon (Burst) | Her frame |

### 13.4 Physics Pipeline (mevcut, izometrige uyarlanacak)
```
ApplyMovementForceSystem  → Kaleye dogru kuvvet (WallX → center-based)
BuildSpatialHashSystem    → Pozisyonlari hash grid'e yaz (double buffer)
PhysicsCollisionSystem    → Circle-circle carpisma + pozisyon duzeltme
IntegrateSystem           → velocity + position guncelleme
BoundarySystem            → Perimeter sur bariyeri + domino queuing
```

### 13.5 Custom DOTS Pathfinding
- Grid-based A* algoritması, izometrik grid uzerinde
- 8 yonlu komsu tarama (N, NE, E, SE, S, SW, W, NW)
- BuildingGridManager._grid[,] uzerinden engel okuma
- Burst compiled + IJobEntity ile parallel hesaplama
- Sadece askeri birimler icin (50-200 birim) — zombiler pathfinding KULLANMAZ
- Heuristik: Octile distance
- Path sonucu: DynamicBuffer<int2> waypoint listesi
- Performans hedefi: <1ms 200 birim icin

### 13.6 Performans Stratejisi
- Tek sync point: DamageApplySystem (state.CompleteDependency)
- Double-buffered spatial hash (ReadMap/WriteMap, .Complete() yok)
- Zombiler: basit fizik (50K), pathfinding yok
- Askerler: A* pathfinding (200), Burst compiled
- IJobEntity pattern — main thread bloklanmaz
- NativeArray/NativeList ile GC baskisi sifir

---

## 14. UI / UX Tasarimi

### 14.1 Ana HUD
| Element | Konum | Bilgi |
|---------|-------|-------|
| Sur HP Bar | Ekranin ustu | Perimeter duvar durumlari (segment bazli) |
| Gun Sayaci | Ust orta | "Gun 34 / 100" |
| Kaynak Paneli | Sol ust | Ahsap, Tas, Demir, Yemek miktarlari + uretim/tuketim hizi |
| Nufus Gostergesi | Sol ust (kaynak altinda) | "Nufus: 45/60 (23 isci, 8 okcu, 4 buyucu, 10 bos)" |
| XP Bar | Sag ust | Level progress + level numarasi |
| Wave Uyarisi | Ekran ortasi | Wave geldiginde uyari animasyonu |
| Minimap | Sag alt | Haritanin kucuk gorunumu + birim konumlari |
| Secili Birim Paneli | Alt orta | Secili askerin bilgileri (HP, hasar, komutlar) |

### 14.2 RTS Kontrol Paneli
- Sol tikla: Birim sec (tek)
- Kutu cizme: Coklu birim sec
- Sag tikla zemine: Hareket komutu
- Sag tikla dusmana: Saldiri komutu
- Secili birim paneli: Portrait, HP, hasar bilgisi
- Grup numaralari (Ctrl+1, 2, 3 ile grup atama)

### 14.3 Bina Menusu
- Binaya tiklaninca acilan panel
- Bina seviyesi, uretim hizi, atanmis isci sayisi
- Isci ekleme/cikarma butonlari
- Upgrade butonu (maliyet gosterilir)
- Yikma butonu

### 14.4 Level Atlama Ekrani
- Oyun duraklar
- 3 kart gosterilir
- Her kartta: ikon, baslik, kisa aciklama, mevcut tier seviyesi
- On kosul karsilanmayan kartlar gri/kilitli gosterilir (veya havuzda bulunmaz)
- Secim sonrasi kart efekti oynanir, oyun devam eder

### 14.5 Game Over Ekrani
- Dramatik gecis: destruction efektleri, duvar cokme, ekran blur
- Istatistik tablosu:
  - Hayatta kalinan gun
  - Oldurulen zombi sayisi
  - Maksimum nufus
  - Toplam kaynak uretimi
  - Secilen kartlar listesi
  - Atlanan wave sayisi
- "Tekrar Oyna" butonu

### 14.6 Event Popup
- Ekranin ortasinda kucuk panel
- Event basligi + aciklama
- Olumlu/olumsuz ikon
- Secimli event'lerde 2 buton (kabul et / reddet)

---

## 15. Ses Tasarimi

| Ses | Aciklama |
|-----|----------|
| Zombi Surusu | Uzak/yakin pan + ambians |
| Ok Atisi | Randomize pitch |
| Ates Topu | Ates patlamasi + impact |
| Buyucu Kulesi Atisi | Guclu buyu patlamasi + AoE dalga |
| Sur'a Vurus | Impact sesi |
| Duvar Catlama | Tas catlama, gicirdama |
| Duvar Kirilmasi | Dramatik cokme sesi + debris |
| Level Atlama | Zafer fanfari |
| Birim Secimi | Kisa onay sesi |
| Birim Komutu | Hareket/saldiri onay sesi |
| Event Bildirimi | Dikkat cekici bildirim sesi |
| Kasaba Ambiyansi | Kus sesleri, demirci vurma, ciftlik |
| Muzik | Dinamik katmanli — sakin gun vs wave muzigi |

---

## 16. Editor Tooling

### 16.1 Custom Editor Window
Tum oyun denge degerleri tek bir Editor Window'dan ayarlanabilir:

- **Wave Ayarlari:** Gun bazli wave sikligi, zombi sayisi egrisi, final wave ayarlari
- **Kaynak Ayarlari:** Uretim hizlari, tuketim hizlari, baslangic degerleri
- **Bina Ayarlari:** Maliyet, uretim verimi, upgrade degerleri, yerlestirme kurallari
- **Nufus Ayarlari:** Baslangic nufus, kapasite degerleri, yemek tuketim oranlari
- **Zombi Ayarlari:** HP egrisi, hiz, hasar, taciz sikligi
- **Kart Ayarlari:** Kart havuzu, cikma olasiliklari, tier degerleri
- **Event Ayarlari:** Event havuzu, olasiliklar, etki degerleri
- **Savunma Ayarlari:** Okcu stats, buyucu stats, Buyucu Kulesi stats, tuzak stats
- **Sur Ayarlari:** HP, onarim hizi, guclendirme degerleri, destruction asamalari
- **Birim Ayarlari:** Hareket hizi, saldiri menzili, pathfinding parametreleri

### 16.2 Amac
- Hizli iterasyon ve playtest
- Programci olmadan denge ayarlama
- Tum degerlerin merkezi yonetimi

---

## 17. Uretim Plani

### 17.1 Milestone'lar
| Milestone | Deliverable |
|-----------|-------------|
| M0 - Mevcut Prototype | ECS zombi hareketi + fizik pipeline + temel savunma (TAMAMLANDI) |
| M1 - Kaynak + Bina | 4 kaynak sistemi + bina yerlestirme + grid sistemi + isci atama (TAMAMLANDI) |
| M-ISO - Izometrik Gecis | Grid → iso, WallX → perimeter, kamera, tile/sprite gecisi |
| M2 - Savunma Derinligi | RTS birim kontrolu + buyucu sistemi + tuzaklar + kart sistemi |
| M3 - Gun + Wave | 100 gun sistemi + wave skalasyonu + final wave + taciz sistemi |
| M4 - Event + Polish | Event sistemi + destruction VFX + UI/UX + ses + gorsel canlilik + denge |
| M5 - Launch | Steam entegrasyonu + lokalizasyon (EN/TR) + son test |

### 17.2 Faz 2 Vizyonu (Post-Launch)
- Harita buyutme (32x32 → 64x64 veya daha buyuk)
- Yeni bina turleri
- Ek buyucu elementleri (Buz, Simsek)
- Survari birimleri (mount sistemi)
- Derin tech tree
- They Are Billions seviyesinde colony defense deneyimi

### 17.3 Risk Analizi
| Risk | Olasilik | Etki | Onlem |
|------|----------|------|-------|
| DOTS ogrenme egrisi | Dusuk | Yuksek | Mevcut prototype ile dogrulanmis |
| 50.000 zombi performansi | Orta | Yuksek | Erken profiling, spatial hash optimizasyonu |
| Izometrik gecis karmasikligi | Orta | Yuksek | Grid + physics perspective-agnostic, %79 kod degismez |
| RTS birim kontrolu + pathfinding | Orta | Yuksek | Custom A* Burst compiled, az birim (200 max) |
| Kaynak dengesi | Yuksek | Orta | Editor Window ile hizli iterasyon |
| Destruction VFX performansi | Dusuk | Orta | Tile swap + particle, GPU-light |
| Kart dengesi | Yuksek | Orta | Playtest verileriyle ayar |

---

*DEAD WALLS - GDD v4.0*
*Bu dokuman canli bir belgedir. Gelistirme surecinde guncellenecektir.*

**v4.0 Temel Degisiklikler (v3.0'dan):**
- Tower Defense → RTS + Colony Defense'e evrildi
- 2D Top-Down → 2D Izometrik perspektif
- Tek sur (sagda) → Perimeter savunma (360°)
- Mancinik/Catapult KALDIRILDI → Buyucu sistemi eklendi
- Ozel ok tipleri (atesli/buzlu ok) KALDIRILDI
- Buyucu Akademisi eklendi (askeri bina — buyucu egitir)
- Buyucu Kulesi yeniden tanimlandi (savunma binasi — genis AoE)
- Askeri birimler RTS tarzi kontrol edilebilir (sec/tasi/saldir)
- Sehir halki otomatik calisir (kontrol edilmez)
- Custom DOTS A* pathfinding (askeri birimler icin)
- Destruction sistemi eklendi (gorsel duvar hasar asamalari)
- 8 yonlu sprite animasyon sistemi (izometrik icin)
- Art asset'leri belirlendi: Fantasy Kingdom Tileset + Character Creator - Fantasy 2D (SmallScale Interactive)
- Faz 2 vizyonu eklendi: They Are Billions seviyesinde buyuk harita genislemesi
