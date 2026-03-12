# DEAD WALLS - Game Design Document v3.0

**Tower Defense + Kasaba Insa + Roguelike**
**Platform:** Steam (PC) | **Engine:** Unity 6 - DOTS/ECS + Burst Compiler + Job System

---

## 1. Oyun Ozeti

### 1.1 Konsept
Dead Walls, Orta Cag atmosferinde gecen bir tower defense + kasaba insa oyunudur. Oyuncu, sur ici kasabasini yonetirken duvarlarina saldiran binlerce zombiye karsi 100 gun hayatta kalmaya calisir. Her run'da level atlama kartlariyla farkli build stratejileri denenir.

### 1.2 Temel Bilgiler
| Alan | Detay |
|------|-------|
| Tur | Tower Defense + Kasaba Insa + Roguelike |
| Platform | Steam (PC) |
| Engine | Unity 6 - DOTS/ECS + Burst Compiler + Job System |
| Hedef Kitle | Strateji + tower defense + roguelike severleri |
| Session Uzunlugu | ~2 saat (tam 100 gun run) |
| Monetizasyon | Tek seferlik satin alma (Steam) |
| Gorsel Stil | Pixel Art, 2D Top-Down, Tilemap |
| Tema | Orta Cag, Zombi Istilasi |

### 1.3 Elevator Pitch
"100 gun hayatta kal. Kasabanı kur, kaynaklarini yonet, surlarini guclendir. Her level atlamada farkli bir strateji sec. Ama zombiler her gun daha kalabalik geliyor — ve 100. gun, hepsi geliyor."

---

## 2. Temel Dongu (Core Loop)

### 2.1 Anlik Dongu (Bir Gun Icinde)
1. Gun baslar — kaynaklar uretilir, binalar calisir
2. Zombiler surekli sur'a taciz saldirisi yapar (kucuk gruplar)
3. Okcular ve savunmalar otomatik olarak zombilere saldiri
4. Oldurulen zombilerden XP kazanilir
5. Oyuncu kaynak yonetir: isci ata, bina yap/yukselt, tuzak kur
6. Yeterli XP biriktiginde Level Atlama ekrani acilir — 3 karttan 1'i secilir
7. Gun biter, sonraki gune gecilir (otomatik)

### 2.2 Wave Dongusu
1. Birkac gunde bir buyuk zombi dalgasi gelir
2. Dalga geldiginde gun uzar — dalga bitene kadar devam eder
3. Dalga atlatilirsa oyun devam eder
4. Dalga sikligi gun ilerledikce artar
5. 100. gun final wave — 50.000 zombi

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
- Gun icinde oyuncu serbestce bina yapar, isci atar, kaynak yonetir
- Gun gecislerinde gun sayaci artar
- DOTS seviyesinde performansli bir DayCycleSystem ile yonetilir

### 3.3 Kazanma / Kaybetme
- **Kazanma:** 100. gun dalgesini atlatmak
- **Kaybetme:** Sur (duvar) HP'si sifira duserse, oyun biter
- Duvar yikildiginda dramatik sahne: ciglik sesleri, ekran blur efekti
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
| **Tas** | Tas Ocagi (tas kaynagi yanina) | Duvar onarimi, bina insaat, mancinik mermi, hendek tuzagi |
| **Demir** | Maden (demir kaynagi yanina) | Demirci upgrade'leri, bina bakimi, ayi tuzagi |
| **Yemek** | Ciftlik (herhangi bir yere) | Nufus beslemek (ev + isci + okcu) |

### 4.2 Kaynak Akisi
Her kaynagin surekli gideri vardir — hicbir kaynak sonsuz birikmez.

```
Ahsap Akisi:  Oduncu → Ahsap → Ok Atölyesi (ok uretimi) + Bina insaat + Tuzak
Tas Akisi:    Tas Ocagi → Tas → Duvar onarimi + Mancinik mermi + Bina insaat + Hendek
Demir Akisi:  Maden → Demir → Demirci upgrade'leri + Bina bakimi + Ayi tuzagi
Yemek Akisi:  Ciftlik → Yemek → Nufus beslemek (tum atanmis nufus yemek tuketir)
```

### 4.3 Kaynak Dengeleme Prensibi
- Daha cok okcu → daha cok ok lazim → daha cok ahsap lazim → daha cok oduncu iscisi lazim → daha cok yemek lazim
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
- Binalar sur ici grid'e yerlestirilir (Tilemap tabanli)
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
- Kale upgrade = pahalı ama grid slot yemez

### 5.4 Askeri Binalar

**Kisla (Barracks)**
- Nufustan okcu egitir
- Kaynak harcar: Yemek + Ahsap
- Egitilen okcu sur'a cikar ve savunma yapar
- Upgrade: Daha hizli egitim, ayni anda daha fazla okcu egitebilme

**Ok Atolyesi (Fletcher)**
- Isci atanir, ahsap tuketir, ok uretir
- Okcular ok olmadan ates edemez veya cok yavas ates eder
- Uretim zinciri: Oduncu → Ahsap → Ok Atolyesi → Okcular ates eder
- Upgrade: Daha hizli uretim, farkli ok tipleri

**Demirci (Blacksmith)**
- Demir tuketir, kalici upgrade'ler sunar
- Oyunun tech tree binasi
- Upgrade secenekleri:
  - Ok hasari artir (demir ok ucu)
  - Duvar guclendirme (demir takviye)
  - Sur kapisi guclendirme
  - Mancinik acma (unlock)
  - Ok Atolyesinde yeni ok tipi acma (atesli ok, buzlu ok)
- Upgrade: Daha gelismis tech secenekleri acilir

### 5.5 Ozel Binalar

**Buyucu Kulesi (Wizard Tower)**
- Level-up'ta buyu kartlarinin cikmasini saglar
- Buyucu Kulesi yokken buyu kartlari havuzda bulunmaz
- Grid'de yer kaplar — stratejik bir yatirim karari
- Upgrade: Buyu kartlarinin cikma olasiligini veya buyu gucunu artirabilir

---

## 6. Nufus Sistemi

### 6.1 Tek Havuz
Oyundaki tum insanlar tek bir nufus havuzundan gelir. Bu nufus hem isci hem asker olarak kullanilir.

- **Isci** olarak: Kaynak binlarina atanir (oduncu, maden, ciftlik vs.)
- **Okcu** olarak: Kislada egitilip sura cikarilir
- **Tradeoff:** Daha cok okcu = daha az isci = daha az kaynak uretimi

### 6.2 Nufus Kapasitesi
Nufus sinirsiz degildir. Kapasite iki yolla artar:
- **Ev** yaparak (grid slot harcar, yemek gideri var)
- **Kale** upgrade ederek (grid slot harcamaz, pahalı)

### 6.3 Nufus Artisi
Yeni nufus **level atlama** sirasinda gelir:
- Level-up kartlarindan biri "Multeciler" olabilir → +X nufus
- Multeciler sadece kapasite varsa gelir
- Kapasite yoksa multeci karti ciksa bile secilemez (veya cikmaz)

### 6.4 Nufus Gideri
Atanmis her birey (isci veya okcu) yemek tuketir. Bosta bekleyen nufus yemek tuketmez. Bu sayede oyuncu "5 kisiyi bosta tutayim, lazim olunca atarim" stratejisi uygulayabilir.

---

## 7. Savunma Sistemi

### 7.1 Sur (Duvar)
- Kasabanin sag tarafinda tek bir sur vardir
- Zombiler sagdan gelir ve bu sur'a saldirir
- Sur HP'si sifira duserse oyun biter
- Sur onarimi: Tas kaynagi ile otomatik veya manuel onarim
- Sur guclendirme: Demirciden upgrade ile HP artirilabilir

### 7.2 Okcular
- Kisladan egitilir, nufustan cikar
- Sur ustunde otomatik olarak en yakin zombiye ates eder
- Ok tuketir — Ok Atolyesi olmadan ates edemez/cok yavas ates eder
- Her okcu yemek tuketir (nufusun parcasi)

| Ozellik | Deger |
|---------|-------|
| Hasar (base) | Inspector'dan ayarlanabilir |
| Atis hizi (base) | Inspector'dan ayarlanabilir |
| Hedefleme | En yakin zombiye oncelik (otomatik) |

### 7.3 Mancinik (Catapult)
- Sur ustune yerlestirilen agir savunma silahi
- Demirciden acilir (unlock)
- Yavas atis hizi, buyuk AoE hasar
- Tas tuketir (mermi olarak)
- Tek hedef degil, alan hasari — kalabalik zombi gruplarina etkili

### 7.4 Buyu Sistemi
- Buyucu Kulesi insasiyla aktif olur
- Buyuler level-up kart havuzuna eklenir
- Oyuncu level atladikca buyu kartlari secebilir
- Ayni buyuyu tekrar secerse tier atlar (daha guclu versiyon)
- Buyuler cooldown tabanli calisiyor, kaynak tuketmez

**Buyu Listesi:**
| Buyu | Etki | Tier Artisi |
|------|------|-------------|
| Ates Topu | AoE hasar | Daha buyuk alan, daha cok hasar |
| Buz Duvari | AoE yavaslatma | Daha uzun sure, daha genis alan |
| Simsek Zinciri | Zincirleme hasar (birden fazla zombiye) | Daha fazla zincir, daha cok hasar |
| Iyilestirme Aurasi | Duvar onarimi | Daha hizli onarim, daha genis etki |

### 7.5 Tuzaklar (Sur Disi)
Sur disina grid mantigi ile yerlestirilen savunma yapilari. Zombiler uzerinden gecince etki eder.

| Tuzak | Etki | Kaynak Maliyeti | Dayaniklilik |
|-------|------|-----------------|--------------|
| **Sivri Kaziklar** | Hasar | Ahsap | Belirli sayida zombiden sonra kirilir |
| **Hendek** | Yavaslatma | Tas (kazi) | Kalici, kirilmaz |
| **Ayi Tuzagi** | Hasar + durdurma | Demir | Tek kullanimlik, tekrar kurulmasi lazim |

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
  - Buyu kartlari → sadece Buyucu Kulesi varsa cikar
  - Ileri okcu kartlari → sadece Kisla varsa cikar

### 8.3 Kart Kategorileri

**Nufus Kartlari:**
- Multeciler (+X nufus, kapasite varsa)

**Okcu Kartlari:**
- Ok Hasari Artisi
- Atis Hizi Artisi
- Coklu Ok (multishot)
- Atesli Ok (DoT hasar)
- Buzlu Ok (yavaslatma)

**Buyu Kartlari (Buyucu Kulesi gerektirir):**
- Ates Topu
- Buz Duvari
- Simsek Zinciri
- Iyilestirme Aurasi

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
| **Buyu Build** | AoE crowd control | Ates topu, buz firtinasi, simsek |
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
| Hasar | Sur'a saniye basina verdigi hasar |

### 9.2 Zombi Davranisi
- Zombiler sagdan gelir ve sur'a dogru yurur
- Sur'a ulasinca saldirmaya baslar
- DOTS/ECS ile on binlerce zombi ayni anda yonetilebilir
- Spatial hash grid ile verimli carpisma yonetimi

### 9.3 Zombi Olceklemesi
```
Zombi_Sayisi(gun) = Inspector'dan ayarlanabilir egri
Zombi_HP(gun) = Inspector'dan ayarlanabilir egri
```
Genel prensip: gun ilerledikce zombi sayisi ve HP'si artar. 100. gun icin 50.000 zombi hedeflenir.

### 9.4 Taciz Sistemi
Wave disinda da kucuk zombi gruplari surekli gelir ve sur'a taciz saldirisi yapar. Oyuncu hicbir zaman tamamen guvenlige degildir. Taciz sikligi ve kalabaligi gun ilerledikce artar.

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
- Wave, tum zombiler oldurulene veya sur yikilana kadar devam eder
- Wave sirasinda oyuncu hala bina yapabilir ve kaynak yonetebilir
- Wave kalabaligi gun numarasina gore olceklenir

### 11.3 Final Wave (Gun 100)
- 50.000 zombi
- Oyuncunun 100 gunluk build'inin son sinavi
- Hayatta kalirsa → zafer
- Duvar yikilirsa → game over (ama 100. gune ulasmanin kendisi bir basari)

---

## 12. Gorsel Canlilik

### 12.1 Ambient Koyluler
Kasabanin yasadigi hissini vermek icin ambient koylu entity'leri bulunur:
- Oduncu binasina dogru yuruyen birkac kisi
- Ciftlikte calisan birkac kisi
- Kasaba icinde rastgele dolasan birkac kisi
- Surda nobet tutan okcular

### 12.2 Teknik Not
- Birebir her isciyi gostermek gerekli degildir
- Ambient entity sayisi gercek isci sayisini yansitmak zorunda degil
- Sadece kasabanin canli hissettirmesi yeterli
- DOTS/ECS ile yuzlerce kucuk koylu entity'si performans sorunu yaratmaz

### 12.3 Atmosfer Kontrastı
- Sakin gunlerde: koyluler calisir, kasaba huzurlu
- Wave geldiginde: zombi ordusu sur'a dayanir, kaos ve savas

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
- ZombieState (Moving / Attacking / Dead)

**Savunma Entity'leri:**
- ArcherUnit (damage, fireRate, fireTimer, arrowCount)
- CatapultUnit (damage, splashRadius, fireRate, ammoType)
- ArrowProjectile (speed, damage, targetRef)
- CatapultProjectile (speed, damage, splashRadius)
- TrapComponent (type, durability, damage, slowAmount)

**Bina Entity'leri:**
- BuildingTag, BuildingType (enum), BuildingLevel
- ResourceProducer (resourceType, ratePerWorker, assignedWorkers)
- PopulationCapacity (amount)
- FoodConsumer (consumeRate)

**Duvar Entity:**
- WallSegment (maxHP, currentHP, repairRate)

**Game State (Singleton):**
- GameState (currentDay, totalDays, xp, level, isGameOver)
- WaveState (zombiesRemaining, zombiesSpawned, totalZombies, waveActive)
- ResourceState (wood, stone, iron, food)
- PopulationState (total, assigned, capacity)

### 13.3 Temel System'ler
| System | Gorev | Frekans |
|--------|-------|---------|
| DayCycleSystem | Gun sayaci, gun gecisi | Her frame |
| WaveSpawnSystem | Dalga baslangicinda zombi spawn | Wave basinda |
| ZombieMoveSystem | Zombileri sur'a dogru hareket ettirir | Her frame |
| ZombieAttackSystem | Sur'a ulasan zombi hasar hesabi | Her frame |
| ArcherShootSystem | Okcu atis + hedef secimi | Her frame |
| CatapultShootSystem | Mancinik atis + AoE hasar | Her frame |
| ArrowMoveSystem | Ok hareketi + isabet kontrolu | Her frame |
| TrapSystem | Tuzak tetikleme + hasar/yavaslatma | Her frame |
| ResourceProductionSystem | Kaynak uretimi hesaplama | Her frame |
| ResourceConsumptionSystem | Kaynak tuketimi hesaplama | Her frame |
| BuildingSystem | Bina insaat/upgrade yonetimi | Event-driven |
| PopulationSystem | Nufus atama/kapasite yonetimi | Event-driven |
| SpellSystem | Buyu cooldown + etki yonetimi | Her frame |
| WallRepairSystem | Duvar onarimi (tas tuketir) | Her frame |
| EventSystem | Rastgele event tetikleme | Gun basinda |
| LevelUpSystem | XP kontrol + kart gosterimi | Her frame |

### 13.4 Physics Pipeline (mevcut)
```
BuildSpatialHashSystem    → Pozisyonlari hash grid'e yaz
PhysicsCollisionSystem    → Circle-circle carpisma + pozisyon duzeltme
IntegrateSystem           → velocity + position guncelleme
BoundarySystem            → Sur bariyeri
```

### 13.5 Bellek Yonetimi
- NativeArray ve NativeList ile GC baskisi sifirlanir
- Olu zombiler EntityCommandBuffer ile frame sonunda temizlenir
- Burst Compile ile performans optimizasyonu
- Job System ile paralel islem

---

## 14. UI / UX Tasarimi

### 14.1 Ana HUD
| Element | Konum | Bilgi |
|---------|-------|-------|
| Sur HP Bar | Ekranin ustu | Duvar can durumu |
| Gun Sayaci | Ust orta | "Gun 34 / 100" |
| Kaynak Paneli | Sol ust | Ahsap, Tas, Demir, Yemek miktarlari + uretim/tuketim hizi |
| Nufus Gostergesi | Sol ust (kaynak altinda) | "Nufus: 45/60 (23 isci, 12 okcu, 10 bos)" |
| XP Bar | Sag ust | Level progress + level numarasi |
| Wave Uyarisi | Ekran ortasi | Wave geldiginde uyari animasyonu |
| Minimap / Genel Bakis | Sag alt | Haritanin kucuk gorunumu |

### 14.2 Bina Menusu
- Binaya tiklaninca acilan panel
- Bina seviyesi, uretim hizi, atanmis isci sayisi
- Isci ekleme/cikarma butonlari
- Upgrade butonu (maliyet gosterilir)
- Yikma butonu

### 14.3 Level Atlama Ekrani
- Oyun duraklar
- 3 kart gosterilir
- Her kartta: ikon, baslik, kisa aciklama, mevcut tier seviyesi
- On kosul karsilanmayan kartlar gri/kilitli gosterilir (veya havuzda bulunmaz)
- Secim sonrasi kart efekti oynanir, oyun devam eder

### 14.4 Game Over Ekrani
- Dramatik gecis: ciglik sesleri, ekran blur
- Istatistik tablosu:
  - Hayatta kalinan gun
  - Oldurulen zombi sayisi
  - Maksimum nufus
  - Toplam kaynak uretimi
  - Secilen kartlar listesi
  - Atlanan wave sayisi
- "Tekrar Oyna" butonu

### 14.5 Event Popup
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
| Mancinik Atisi | Agir firlatma sesi |
| Mancinik Isabeti | Patlama + ezilme sesi |
| Sur'a Vurus | Impact sesi |
| Duvar Kirilmasi | Dramatik cokme sesi |
| Level Atlama | Zafer fanfari |
| Buyu Efektleri | Her buyu icin ozgun ses |
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
- **Savunma Ayarlari:** Okcu stats, mancinik stats, buyu stats, tuzak stats
- **Sur Ayarlari:** HP, onarim hizi, guclendirme degerleri

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
| M1 - Kaynak + Bina | 4 kaynak sistemi + bina yerlestirme + grid sistemi + isci atama |
| M2 - Savunma Derinligi | Mancinik + tuzaklar + buyu sistemi + level-up kartlari |
| M3 - Gun + Wave | 100 gun sistemi + wave skalasyonu + final wave + taciz sistemi |
| M4 - Event + Polish | Event sistemi + UI/UX + ses + gorsel canlilik + denge |
| M5 - Launch | Steam entegrasyonu + lokalizasyon (EN/TR) + son test |

### 17.2 Risk Analizi
| Risk | Olasilik | Etki | Onlem |
|------|----------|------|-------|
| DOTS ogrenme egrisi | Orta | Yuksek | Mevcut prototype ile dogrulanmis |
| 50.000 zombi performansi | Orta | Yuksek | Erken profiling, spatial hash optimizasyonu |
| Kaynak dengesi | Yuksek | Orta | Editor Window ile hizli iterasyon |
| Grid + Tilemap entegrasyonu | Orta | Orta | Basit baslayip iterasyon |
| UI + DOTS hibrit | Orta | Yuksek | UI Toolkit test |
| Kart dengesi | Yuksek | Orta | Playtest verileriyle ayar |

---

*DEAD WALLS - GDD v3.0*
*Bu dokuman canli bir belgedir. Gelistirme surecinde guncellenecektir.*

**v3.0 Temel Degisiklikler (v2.0'dan):**
- Idle-clicker'dan Tower Defense + Kasaba Insa + Roguelike'a evrildi
- Click damage kaldirildi
- Gold kaldirildi → 4 kaynak sistemi (Ahsap, Tas, Demir, Yemek)
- Prestige kaldirildi → 100 gun run yapisi
- 5 zombi tipi → tek tip zombi (guc = kalabalik)
- Bina sistemi eklendi (11 bina turu)
- Nufus yonetimi eklendi (tek havuz: isci + asker)
- Mancinik + tuzak sistemi eklendi
- Buyu sistemi eklendi (Buyucu Kulesi + level-up kartlari)
- Event sistemi eklendi
- Sur disi tuzak sistemi eklendi
- Game over istatistik ekrani eklendi
- Editor Window ile merkezi denge yonetimi eklendi
