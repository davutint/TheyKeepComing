# DEAD WALLS - MASTER PLAN (yasayan pusula dokumani)

> STATUS: v0.2 (2026-07-06) — K1/K2/K3 temel kararlari OWNER TARAFINDAN KILITLENDI.
> Kalan [KARAR BEKLIYOR] maddeleri ilgili milestone baslarken karara baglanacak.
> Bu dokuman oyunun YON ve KAPSAM otoritesidir: "oyun ne olacak, ne kaldi, ne yapilmayacak"
> sorularinin TEK cevabi buradadir. Sistemlerin nasil calistigi icin GDD + *_ARCHITECTURE.md'lere bak.
> KURAL: Durum degistiren her calisma oturumunun sonunda bu dokuman guncellenir (sorumlu: Claude).
> `Assets/Docs/ROADMAP.md` bu dokumanla birlikte resmen EMEKLIDIR (tarihsel arsiv).
> Yazim: SADECE ASCII (diger repo dokumanlariyla ayni kural).

---

## 0. TEMEL KARARLAR (2026-07-06'da owner tarafindan KILITLENDI)

| # | Soru | KARAR |
|---|------|-------|
| K1 | Bar nerede? | **PLAY STORE URUNU** — yayinlanacak gercek urun; store varliklari, analytics ve monetizasyon karari plana dahildir (C11-C13 aktif) |
| K2 | Kosu yapisi? | **ROGUELITE META** — olum -> kalici meta-ilerleme -> yeni kosu; olum anlamli, kosular tempolu (C9 aktif) |
| K3 | Platform? | **MOBIL-FIRST, PC KAPISI ACIK** — ana hedef telefon; PC/Steam ileride ayri karar (kod iki girisi destekliyor) |
| K4 | Savas duzeni? | **TEK CEPHE (SAGDAN SALDIRI)** — 360-ring TERK EDILDI. Dusmanlar yalniz sagdan gelir; solda us/ekonomi, ortada savunma hatti. Hat yapisi: DUVAR + HENDEK + KULELER. Ordu modeli: OKCU-DUVAR korunur (melee ordu YOK). Duvar temasi: vurur + domino kuyruk (mevcut fizik). GORSEL IS BOLUMU: koy/us ve duvar tilemap'lerini OWNER olusturur; gameplay baglari (spawn seridi, hat konumu, kule slotu = dolu tilemap hucresi okuma) Claude yapar. **GORSEL KATMAN TAMAM (2026-07-07):** komple harita MCP tile pipeline ile boyandi — koy (kale W6x5 + tarla + tas ocagi + demir madeni + kereste kampi + meydan/patikalar), duvar (owner'in el tasarimi git'ten geri insa + tek panelli kapi), hendek, kuru savas alani + toprak yol, spawn seridi, ust kenar orman bandi. Kontratlar: `outside` = 40 okcu slotu (x=0), `VillageMarkers` 5 tam isimli marker. Setup tool boyamayi korur (fallback arena kapili). Play testi GECTI (okcular duvarda, zombi spawn calisiyor). Detay: `STRUCTURE_SPRITE_BAKER_CAPABILITIES.md`. |

> Bu kararlar geri acilmaz (yeni buyuk bilgi cikmadikca). Tum milestone ve kriterler
> bu kararlara gore okunur. K4 implementasyonu M-0 milestone'udur (kod su an hala 360-ring).

---

## 1. VIZYON (tek paragraf)

Dead Walls, tek elle oynanabilen mobil bir roguelite "surekli kusatma" savunma oyunudur:
solda usunu ve ekonomini buyutursun, sagdan gelen olu surusu duvarina yuklenir — gunduz
uret, gece hatti tut, safakta konseyinle pazarlik et. Oyun kosu icinde HIC durmaz; her
karar akan zamanin icinde verilir. Her olum bir sey ogretir VE bir sey kazandirir
(meta-ilerleme) — "bir gece daha dayandim ve bu benim planimin zaferiydi" duygusu,
kosudan kosuya buyuyen kalici guclenmeyle birlesir.

---

## 2. "OYUN OLDU" TANIMI (bitmis sayilma kriterleri)

K1 = Play Store urunu oldugundan asagidaki listenin TAMAMI (C1-C13) 1.0 esigidir.
Ara hedef olarak "oynanabilir demo" = C1-C10 (store maddeleri haric).

### Cekirdek (pazarlik yok)
- [ ] C1. Ortalama bir kosu 15-40 dakika surer ve olumle biter (kaybedilebilirlik ADIL hisseder)
- [ ] C2. DAY 1 ile DAY 10+ farkli oynanir (sadece "daha kalabalik" degil: yeni dusman/karar/tehdit)
- [ ] C3. Oyuncu olmeden once "keske sunu yapsaydim" diyebilir (okunabilir sebep-sonuc)
- [ ] C4. Telefonda 30+ FPS ile oynanir (gercek cihazda dogrulanmis)
- [ ] C5. Oyun kapatilip acilinca kosu kaybolmaz (save/load) — su an YOK
- [ ] C6. Yeni oyuncu ilk 2 dakikada ne yapacagini anlar (onboarding)
- [ ] C7. Ses vardir: temel combat SFX + ambiyans (sessiz oyun "bitmemis" hisseder)
- [ ] C8. Ana menu / pause / restart / ayarlar (ses ac-kapa) iskeleti vardir

### Roguelite (K2 karari geregi AKTIF)
- [ ] C9. Olum bir seye YARAR: meta-ilerleme kosular arasi tasinir (para birimi + kalici unlock'lar)
- [ ] C10. Rekor takibi roguelite icinde de yasar: "en iyi gunum / toplam kosu" olum ekraninda gorunur

### Play Store urunu (K1 karari geregi AKTIF)
- [ ] C11. Store varliklari (ikon, ekran goruntuleri, tanitim metni), yas derecelendirme
- [ ] C12. Analytics/crash raporlama karari + entegrasyonu
- [ ] C13. Monetizasyon karari (reklamsiz premium? odullu reklam? hicbiri?) — [KARAR BEKLIYOR, M-H oncesi]

---

## 3. SUTUN DURUM TABLOSU (su an ne var / ne eksik / acik karar)

### 3.1 Core Loop (surekli kusatma dongusu)
- VAR: 4-faz 60s dongu (DAY/DUSK/NIGHT/DAWN), kutle eskalasyonu (lineer HP + buyuyen batch,
  MaxAlive 900), gun sayaci, GameOver + restart, dawn odul ani (+15 pop, toast).
- EKSIK: K4 pivotu (360-ring -> sagdan tek cephe) implementasyonu = M-0. Gec oyun
  (DAY 10-20) HIC olculmedi — egri kirilmalari bilinmiyor. Dongu icinde "buyuk an" yok
  (orn. her 5. gece ozel dalga/miniboss). Zorluk tek eksende (sayi) artiyor.
- ACIK KARAR: [KARAR BEKLIYOR] Ozel gece/dalga konsepti (kanli ay, sis gecesi, boss gecesi?)
  isteniyor mu? (C2 kriterinin ana adayi)

### 3.2 Ekonomi
- VAR: Sol worker drawer (4 kaynak, cap'ler), repair maliyeti (kayip-orantili), tekrarlanabilir
  tech sink'leri (mastery), council ekonomik etkileri, gelir/zorluk ayrisik (kill reward sabit).
- EKSIK: Gec oyun para dengesi olculmedi (sink'ler yetiyor mu?). Kaynaklarin kimligi zayif:
  Wood/Stone/Iron/Food neredeyse birbirine esdeger hissediyor (harcama kanallari az ayrisik).
- ACIK KARAR: [KARAR BEKLIYOR] Food'a ozel rol (nufus besleme/aclik mekanigi) eklensin mi,
  yoksa mevcut sadelik korunsun mu?

### 3.3 Savas
- VAR: 3 okcu tipi (Basic/Rapid unlock/Frost unlock), domino queue fizigi, frost slow,
  wall->gate->core hasar zinciri, DOTS ile yuzlerce zombi. (Su an 360 spawn — K4 ile degisecek.)
- EKSIK: K4 geregi tek-cephe donusumu (M-0): sag-serit spawn, savunma hattinin sola tasinmasi,
  HENDEK katmani (yavaslatma/hasar — tasarim M-0 basinda), KULE slotlari (okcu yerlesimi
  owner'in boyadigi kule tile'larindan okunur). TEK zombi tipi (gorsel ve mekanik monotonluk —
  C2'nin en buyuk riski). Oyuncunun savas ANINDA verdigi karar neredeyse yok (sadece repair +
  satin alma). Combat geri bildirimi zayif (hasar sayilari, olum efekti, ses yok).
- ACIK KARAR: [KARAR BEKLIYOR] Savas aninda oyuncu ajansi eklenecek mi? Adaylar:
  (a) aktif yetenek butonlari (yagli ok yagmuru, tamir dalgasi — cooldown'lu)
  (b) hedef onceligi secimi (c) hicbiri — "izle ve planla" kimligi korunur.

### 3.4 Meta-progression / Kosu Yapisi (K2 = ROGUELITE — M-B IMPLEMENTE EDILDI 2026-07-07)
- KARARLAR (owner): para birimi = OLDURULEN ZOMBI (1 kill = 1 RUH; + yeni gun rekorunda
  gun x 50 bonus); magaza odagi = IVME + HAFIF GUC; isim "RUH" placeholder (kesinlesmedi).
- VAR: kill sayaci (GameStateData.TotalKills), kalici JSON depo (persistentDataPath —
  M-E save'inin ilk tuglasi), MetaUpgradeSO katalogu (7 yukseltme: baslangic odun/yiyecek/
  okcu/moat-tech + duvar HP/hasar/uretim yuzdeleri), kosu-basi otomatik uygulama
  (aggregate kanallarindan), olum ekrani ozet + magaza UI. Dok: META_PROGRESSION_ARCHITECTURE.md.
- EKSIK: play dogrulamasi (Unity+MCP bekliyor); isim kesinlesmesi; magaza rafi buyumesi
  (M-C icerigiyle: yeni okcu tipleri meta-unlock adayi).

### 3.5 His / Juice / Ses
- VAR: UI juice guclu (tech tree cizgi animasyonlari, council kart akisi, toast'lar, badge'ler,
  punch/shake, UI SFX'leri). Gunduz/gece overlay'i.
- EKSIK: COMBAT tarafi sessiz ve kuru: ok sesi yok, zombi olum sesi/efekti yok, kale hasar
  aninda his yok (ekran sarsintisi/flash zayif), muzik/ambiyans yok (gece gerilim katmani buyuk firsat).
- ACIK KARAR: [KARAR BEKLIYOR] Muzik kaynagi: asset store paketi mi, telifsiz kutuphane mi?

### 3.6 Icerik Cesitliligi
- VAR: 16 tech node, 9 council sablonu + 11 atom (uretken sistem), 3 okcu tipi, 1 zombi tipi,
  1 harita/arena.
- EKSIK: Zombi tipleri (kosucu/tank/zirhli/patlayici...), council havuzu buyumesi (negatif
  event'ler, uzun zincirler), tech agacinin gec-oyun katmani, (K2'ye gore) meta unlock icerigi.
- NOT: Council ve tech SO-driven — icerik eklemek kod istemez; zombi tipleri ECS isi ister.

### 3.7 Teknik / Platform / Shipping Iskeleti
- VAR: DOTS simulasyon saglam (tek sync point, 900 tavan), EditMode test altyapisi (7 test),
  setup tool idempotent, prefab tek-dogruluk-kaynagi UI akisi, dokunmatik input teoride hazir.
- EKSIK (kritik sirali):
  1. SAVE/LOAD YOK — mobilde oyun kapaninca kosu gidiyor (C5). ECS state serializasyonu
     tasarim ister (hangi state kaydedilir: kaynaklar/gun/tech/council flag'leri/savunma HP —
     zombiler muhtemelen KAYDEDILMEZ, gece basinda temiz baslar).
  2. Gercek cihaz build'i hic alinmadi (C4) — pinch/perf/battery bilinmiyor.
  3. Ana menu / pause / ayarlar yok (C8).
  4. Onboarding yok (C6) — ilk safak karti iyi baslangic ama worker drawer'i kesfettirmiyor.
- ACIK KARAR: [KARAR BEKLIYOR] Save kapsami (mid-night save mi, sadece safak checkpoint'i mi?
  Oneri: SADECE safak checkpoint'i — hem teknik olarak 10x basit hem "gece riskini tasima"
  tasarimini guclendirir).

---

## 4. MILESTONE SIRASI (oneri — her biri "eline alip oynanir sonuc" uretir)

> Oneri mantigi: once cephe pivotu (M-0 — olcumden ONCE, yoksa 360-ring verisi cope gider),
> sonra OLC (M-A), sonra meta (M-B), sonra en buyuk oynanis eksigi (M-C), his (M-D),
> shipping iskeleti (M-E/F), store (M-H).

- **M-0. Tek Cephe Pivotu (K4)** — spawn 360-ring -> sag kenar seridi; savunma hatti
  (Wall->Gate->Core zinciri korunarak) sola tasinir; HENDEK v1 [KILITLENDI 2026-07-06:
  UPGRADE ILE EVRILEN — baslangicta basit cukur (yavaslatma), tech tree upgrade'leriyle
  derinlesir/lav-diken olur (gecis hasari); tech sink'i buyutur]; KULE/okcu slotlari owner
  tilemap hucrelerinden okunur (mevcut Grid/outside modeli uyarlanir); KAMERA [KILITLENDI:
  SABIT TEK EKRAN — tum alan tek bakista, pan/zoom yok, mobil okunurluk oncelikli].
  IS BOLUMU: owner koy+duvar+kule tilemap gorsellerini boyar (paralel); Claude mekanigi
  placeholder gorsellerle kurar, owner tilemap'i gelince baglar. (~1-2 oturum)
- **M-A. Olcum ve Balance Temeli** — hizlandirilmis uzun-kosu simulasyonu (DAY 1-20 metrik
  dokumu, TEK CEPHE duzeninde), egri kirilmalarinin tespiti + duzeltmesi. Cikti: "ortalama
  kosu N gun/M dakika" verisi (M-B meta tasariminin girdisi). (~1 oturum)
- **M-B. Kosu Yapisi + Meta v1** — K2 karari, olum ekrani ("gun X'e ulastin" ozeti),
  (K2=b/c ise) meta para birimi + 3-5 kalici unlock, rekor takibi. (~1-2 oturum)
- **M-C. Dusman Cesitliligi v1 — KAPSAM DEGISTI (owner, 2026-07-08) + IMPLEMENTE EDILDI** —
  Zombi tipleri ERTELENDI (owner: "simdilik tek zombi"; Kosucu/Tank/Zirhli M-C2'ye).
  Yerine: (1) KANLI AY — her 5. gece intensity x1.5 (SpecialNights AKTIF, Difficulty
  Tuner'dan ayarlanir), gunduz uyari toast'u + kirmizi gece etiketi/overlay;
  (2) ATES TOPU — oyuncunun aktif savas gucu: tech'ten acilir (arcane_tower), butona
  bas -> alana tikla -> alan hasari; hasar/yaricap/cooldown AYRI tech dugumleriyle
  gelisir (owner vizyonu; buyucu gorseli soyut, "sifir asker + tiklama savasi" modeli
  sonraya). Dok: TECH_TREE_SO + CONTINUOUS_SIEGE v5.2. Play dogrulamasi bekliyor. (~1 oturum)
- **M-D. His Katmani — TAMAM (2026-07-08)** — combat SFX seti dolduruldu (ok atis/isabet,
  zombi olum, kale vurus, fireball patlama — "RPG Magic ELEMENTAL" paketi, setup yalniz-bossa
  atar), kale hasar hissi (CameraShaker trauma sarsintisi + DamageFlashUI kirmizi vuru),
  gece/kanli-ay drone ambiyansi + kanli ay giris sting'i (AmbientAudioController crossfade).
  Play'de dogrulandi (SFX timestampleri + sarsinti/flash olcumu + ambiyans gecisi).
  Kalan (M-D2 adaylari): muzik, UI tik sesleri, olum ekrani sting'i, volume ayar menusu (M-E).
- **M-E. Shipping Iskeleti** — safak-checkpoint save/load, ana menu + pause + ayarlar,
  minimal onboarding (3 ipucu balonu). (~2 oturum)
- **M-F. Cihaz Dogrulamasi** — Android build, gercek cihazda perf/input/battery testi,
  cozunurluk/safe-area duzeltmeleri. (~1 oturum + owner cihaz testi)
- **M-G. Icerik Dolgusu** — council V1.5 (negatif event'ler, yeni zincirler, simulator
  penceresi), tech gec-oyun katmani, meta unlock icerigi. (surekli/aralara serpilir)
- **M-H. Store/Release Hazirligi** (K1 = Play Store geregi) — monetizasyon karari (C13),
  analytics/crash entegrasyonu, store varliklari, kapali test (internal testing track),
  yas derecelendirme. (~1-2 oturum + owner store hesabi islemleri)

---

## 5. BILINCLI KAPSAM DISI (bunlari YAPMIYORUZ — scope kilidi)

- Grid town-building / bina yerlestirme (eski GDD vizyonu — kesin cikti)
- Lane/telegraph dalga sistemi, manuel "Start Next Wave"
- Manuel okcu yerlestirme / RTS mikro kontrolu
- XP level-up kart secimi, ok stogu/uretimi yonetimi
- M-ISO izometrik grid + perimeter wall gecisi
- Multiplayer / co-op
- Kale disina birim gonderme (saldiri/keşif harici loop)
- Anlati kampanyasi / gorev sistemi (council'in uretken anlatisi YETERLI)

> Liste owner tarafindan 2026-07-06'da ONAYLANDI ve kilitlendi. Geri acmak = bilincli
> yeni karar (bu dokumanda gunlukle birlikte).

## 6. KARAR GUNLUGU (verilmis buyuk kararlar — "neden boyleydi?")

- 2026-06-xx: Grid town-building vizyonundan cikildi -> Mobile Continuous Siege
- 2026-06-30: Sag drawer recruitment-only sadelestirme; plain-UI standardi
- 2026-07-05: Plain Unity UI / text-first gorsel standart kesinlesti (fantasy icon yok)
- 2026-07-05: SO-driven Tech Tree (reveal graph); Rapid/Frost unlock tech'e tasindi
- 2026-07-06: DAWN fazi + kutle eskalasyonu (lineer HP); gelir/zorluk ayristirildi;
  repair maliyetli oldu; tekrarlanabilir sink'ler eklendi
- 2026-07-06: Council Events = uretken sistem (asset degil); kart yalniz SAFAKTA gelir
- 2026-07-06: UIImporter/UIExports pipeline'i SILINDI; prefab = UI'nin tek dogruluk kaynagi
- 2026-07-06: Bu dokuman olusturuldu; ROADMAP.md emekli edildi
- 2026-07-06: K1 = PLAY STORE URUNU, K2 = ROGUELITE META, K3 = MOBIL-FIRST (owner kilitledi)
- 2026-07-06: K4 = TEK CEPHE (sagdan saldiri; 360-ring terk edildi). Hat: duvar+hendek+kule;
  ordu modeli okcu-duvar (melee YOK); duvar temasi vurus+kuyruk; koy/duvar gorselleri owner
  tilemap isi, gameplay baglari Claude (referans: yatay hero-castle-defense duzeni)
- 2026-07-06: M-0 tasarim kararlari — HENDEK: upgrade ile evrilen (cukur/yavaslatma ->
  tech ile lav-diken/hasar); KAMERA: sabit tek ekran (pan/zoom yok); KAPSAM DISI listesi
  (9 madde) owner tarafindan onaylanip kilitlendi

## 7. GUNCEL DURUM OZETI (son guncelleme: 2026-07-06)

- Cekirdek dongu calisiyor ve "iyi hissettiriyor" (owner, DAY 1-3 araligi).
- K1/K2/K3/K4 KILITLENDI: Play Store urunu / Roguelite meta / Mobil-first / Tek cephe (sagdan).
- **M-0 MEKANIGI TAMAM (2026-07-06):** sag-serit spawn, duvar hatti, hendek (yavaslatma +
  moat_flame yakma evrimi, tech zinciri wall_reinforcement -> moat_dig -> moat_flame), okcu
  kolon-fallback'i, sabit kamera — hepsi play-mode dogrulandi (kuyruk olusumu, duvar hasari,
  hendek 14/14 slow + HP erimesi, GameOver). GERIYE KALAN (owner): eski kale siluetinin
  kaldirilip koy/duvar/kule/hendek tilemap'lerinin boyanmasi; Claude hazir olunca baglar
  (okcu yerlesimi otomatik tilemap-oncelikli olur).
- Bilinen acik teknik borc: TechTreeUI'de IsMobileMode guard'i (dalgalanma riski, dusuk oncelik);
  gercek cihaz hic test edilmedi; save yok.
- **M-0 KAPANDI (2026-07-07):** gorsel katman + VillageMarkers -> villager rotalari
  setup-tool koprusuyle baglandi; play'de 56 villager koy yapilarina yuruyor,
  okcular duvarda, tam akis dogrulandi (screenshot'li).
- **M-A OLCUM TAMAM (2026-07-07):** 10 kosu (Logs/LongRun/*.csv). ANA BULGU — "cift
  kambur": kosularin ~%90'i DAY 2-3'te oluyor (duvar 200 HP tek gecede gidiyor; repair
  stone'a bagli ve erken oyunda stone yetmiyor), DAY 4'u atlatan tek kosu ise DAY 20'ye
  kadar HIC olmuyor (okcu+tech gucu eskalasyonu geciyor; gate %66'da 14 gun sabit).
  C1 hedefi (15-40 dk kosu) su tuning'de imkansiz: kosu ya 2-3 dk ya sonsuz.
  Ek bulgular: IRON kalici darbogaz (uretim 114/dk, tech talebi cok ustunde);
  STONE+FOOD gec oyunda olu para (S2894/F5225 birikti — food'un hic sink'i yok);
  okcu enflasyonu (council free-archer akisi + alimlar -> DAY 20'de 95 okcu);
  FPS saglikli (gun-sonu 180-280; anlik gece diplerinde ~58-130).
  Balance duzeltme paketi owner onayi bekliyor; yapisal plato cozumu = M-C
  (zombi tipleri + ozel geceler), food kimligi = M-B/3.2 karari.
- **DIFFICULTY TUNER TAMAM + BALANCE DUZELDI (2026-07-07):** zorluk veriye tasindi —
  `DifficultyProfileSO` (gun-egrileri + eskalasyon; SpawnTable/SpecialNights M-C iskeleti)
  + `Difficulty Tuner` penceresi (Apply canli + Run Bot + dagilim ozeti). Default profil
  (ramp d1 0.5 -> d7 1.0, HpGrowth 0.40, BatchGrowth 0.15, MaxBatch 16, RepairStone 50,
  Wall 350, Iron +%30, council free-archer kisildi) ile dogrulama: olumler DAY 2-3'ten
  6+'ya tasindi; DAY 20'ye ulasan kosu SUREKLI MUCADELE yasadi (DAY 18'de duvar+kapi
  dustu, 47 repair, 892 canli zombi @ FPS 200+). "Ya 2 dk ya sonsuz" cift-kamburu KIRILDI.
  Ust-uc inceltme M-C'de (zombi tipleri + ozel geceler); ayar artik owner'in elinde
  (Tuner, kod istemez).
- M-B TAMAM (2026-07-07): kill -> RUH meta dongusu implemente + play'de iki tam olum
  dongusuyle dogrulandi (kazanim/rekor/magaza/restart-etkisi/kalicilik). Detay: 3.4.
- M-C V1 IMPLEMENTE EDILDI (2026-07-08): Kanli Ay + Ates Topu (buyuculuk tech dali).
  Kapsam degisikligi ve detay: milestone listesindeki M-C maddesi. PLAY DOGRULAMASI
  BEKLIYOR (Unity setup kosusunda kesildi).
- M-C V1 DOGRULANDI (2026-07-08): buyuculuk dali formulleri birebir (72/40.5s/2.6);
  fireball izole vurusu tam hasar; kanli ay DAY 5 intensity 2.10 birebir + tum UI katmani.
  Yakalanan buglar: CyclePhaseText rich-text render etmiyor (renk artik TMP color ile),
  Tuner canli-apply blood moon alani (baker ile ayni formul yazildi).
- M-D TAMAM (2026-07-08, owner secimi): his katmani — detay milestone listesinde.
- SIRADAKI IS: yol karari — M-C2 (zombi tipleri: Kosucu/Tank, SpawnTable bagla) ya da
  M-E (shipping iskeleti: safak-checkpoint save + ana menu + pause). Kucuk acik uclar:
  RUH ismi kesinlesmedi; olum ekrani GAME OVER basligi kozmetik cakisma; buyucu gorseli yok.
