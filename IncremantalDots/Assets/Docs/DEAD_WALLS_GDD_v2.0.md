# DEAD WALLS - Game Design Document v2.0

**Idle Clicker - Incremental Defense - DOTS Powered**
**Platform:** Steam (PC) | **Engine:** Unity 6 - DOTS/ECS + Burst Compiler + Job System

---

## 1. Oyun Ozeti

### 1.1 Konsept
Dead Walls, Orta Cag atmosferinde gecen bir idle-clicker incremental savunma oyunudur. Oyuncu, kalesinin duvarlarina saldiran yuzlerce hatta on binlerce zombiyi olabildigince uzakta tutmaya calisir. Her level atlamada kaleyi guclendirecek kalici secimler yapilir.

### 1.2 Temel Bilgiler
| Alan | Detay |
|------|-------|
| Tur | Idle Clicker + Incremental Tower Defense |
| Platform | Steam (PC) |
| Engine | Unity 6 - DOTS/ECS + Burst Compiler + Job System |
| Hedef Kitle | Idle/incremental oyun severleri, casual-strategy oyunculari |
| Session Uzunlugu | 2-10 dakika aktif, arka planda surekli calisir |
| Monetizasyon | Tek seferlik satin alma (Steam) |
| Hedef Launch | 2026 Q3 |

### 1.3 Elevator Pitch
"Duvarlar yikilmadan once kacinilmazi durdurmaya calisiyorsun. Her click bir zombiyi yavaslatir, her level atlama kaleyi biraz daha guclendirir. Ama zombiler hic durmaz - sadece cogalir."

---

## 2. Temel Dongu (Core Loop)

### 2.1 Anlik Dongu (Per-Session)
1. Zombi dalgasi baslar - kale duvarlarina dogru akin ederler
2. Oyuncu ekrana tiklayarak dogrudan hasar verir (Click Damage)
3. Yerlestirilmis okcular pasif olarak zombilere ok atar
4. Oldurulen zombilerden Gold ve XP kazanilir
5. Yeterli XP biriktiginde Level Atlama ekrani acilir
6. Oyuncu upgrade secimi yapar - bu kalicidir
7. Bir sonraki dalga daha kalabalik ve guclu gelir

### 2.2 Meta Dongu (Long-Term)
- Kale HP sifirlanirsa oyun biter - Prestige secenegi sunar
- Prestige ile tum ilerleme sifirlanir ama kalici Prestige Bonusu kazanilir
- Her Prestige turunda daha hizli ilerleme, yeni mekanikler ve karakter gorunumleri acilir

### 2.3 Offline Ilerleme
- Oyuncu kapaliyken pasif okcular ve duvar onarimi calismaya devam eder
- **Offline'da dalga ilerlemez** - sadece savunma + gold birikimi olur
- Maksimum offline sure: 8 saat (upgrade ile arttirilabilir)
- Geri donen oyuncuya offline kazanci ozet ekraninda gosterilir

---

## 3. Gameplay Mekanikleri

### 3.1 Click Sistemi
Oyunun temel etkilesimi ekrana (zombilere) tiklamaktir.

- Her tiklama, en yakin zombiye (ya da tiklanan zombiye) Click Damage verir
- Click Damage baslangicta 1 ve yukseltmelerle buyur
- **Click Damage Olcekleme (v2.0):** Gec oyunda click damage anlamsizlasmasin diye:
  - `ClickDamage = BaseDamage + (TotalPassiveDPS * 0.02)`
  - Boylece click her zaman toplam DPS'in en az %2'si kadar vuracak
- Kritik tiklama sansi: base %5, upgrade ile arttirilabilir (x3 hasar)
- Click Combo: Ardisik tiklamalar hiz ve hasar bonusu biriktirir
  - 3 tiklama -> +%10 hasar
  - 10 tiklama -> +%25 hasar
  - 25 tiklama -> +%50 hasar + gorsel efekt

### 3.2 Zombi Sistemi
Zombiler DOTS/ECS ile yonetilir ve teorik olarak ekranda on binlerce zombi ayni anda olabilir.

**Temel Ozellikler:**
| Ozellik | Aciklama |
|---------|----------|
| HP | Wave numarasi ve zombi tipine gore olceklenir |
| Hiz | Base hareket hizi, tipine gore farklilasir |
| Hasar | Kale duvarina veya kapiya verdigi hasar/saniye |
| Reward (Gold) | Oldurulunce verilen gold miktari |
| Reward (XP) | Oldurulunce verilen XP miktari |

**Zombi Tipleri (Asama asama acilir):**
| Tip | HP | Hiz | Hasar/sn | Ozellik | Acilis |
|-----|-----|-----|----------|---------|--------|
| Surungun | Dusuk | Yavas | Dusuk | Kitleler halinde gelir, temel tip | Wave 1 |
| Kosan | Dusuk | Hizli | Orta | Duvara cabuk ulasir, ok onceligi alir | Wave 11 |
| Zirhli | Yuksek | Cok Yavas | Yuksek | Okcu hasarini %50 azaltir | Wave 26 |
| Tunelci | Orta | Orta | Cok Yuksek | Kaleye zemin altindan saldirir | Wave 51 |
| Dev | Cok Yuksek | Yavas | Ekstrem | Mini boss, zaman zaman gelir | Wave 101 |

**Tunelci Zombi Counter (v2.0):** Tunelci zombilere karsi "Yer Tuzagi" upgrade'i eklenir (Bolum 4.3 Kategori E).

### 3.3 Kale Sistemi

**Duvarlar:**
- Ana savunma hatti - HP bar'i ile gosterilir
- Zombiler duvara ulasinca hasar vermeye baslar
- Duvar HP sifirlanirsa kale HP'si dusmeye baslar
- Level atlama secenegi ile onarim veya guclendirme yapilabilir

**Kale Kapisi:**
- Duvardan ayri bir HP'ye sahip kritik nokta
- Zombiler once kapiya yonelir (en yakin erisim noktasi)
- Kapi yikilirsa zombiler iceri girer ve kale HP'sini direkt dusurur
- Kapi onarimi: Level atlama secenegi veya Gold ile anlik onarim

**Kale HP:**
- Son savunma hatti - tukenirse oyun biter
- Prestige ile kalici max HP artirimi yapilabilir

---

## 4. Level Atlama ve Upgrade Sistemi

### 4.1 XP ve Level Esigi
Her oldurulen zombi XP kazandirir. XP esigi her levelda ustel olarak artar.

`XP_Esigi(level) = 100 * (level ^ 1.5)`

| Level | Gerekli XP | Kumulatif XP |
|-------|-----------|--------------|
| 1 -> 2 | 100 | 100 |
| 5 -> 6 | 559 | 2.693 |
| 10 -> 11 | 1.581 | 10.413 |
| 25 -> 26 | 6.250 | 77.012 |
| 50 -> 51 | 17.678 | 417.720 |
| 100 -> 101 | 50.000 | 2.348.583 |

### 4.2 Level Atlama Ekrani
Level esigine ulasildida oyun duraklar ve Level Atlama Ekrani acilir. Oyuncu kategoriler arasindan 1 secim yapar. Her secim kalicidir.

### 4.3 Upgrade Kategorileri

**Kategori A - Okcu Ekleme**
Kale duvarlarina yeni okcu birimi yerlestirilir. Okcular tamamen pasif calisir.

| Ozellik | Deger |
|---------|-------|
| Hasar (base) | 5 / ok |
| Atis hizi (base) | 1.0 ok/saniye |
| Menzil | Ekran gorus alaninin tamami |
| Hedefleme | En yakin zombiye oncelik |
| Maksimum okcu | Duvar basina 12 |

**Kategori B - Ok Adedini Artir**
Mevcut tum okcularin ayni anda attigi ok sayisi artirilir.

| Tier | Ok Adedi | Toplam DPS Artisi |
|------|----------|-------------------|
| Tier 1 | +1 ok (2 toplam) | +%100 |
| Tier 2 | +1 ok (3 toplam) | +%50 |
| Tier 3 | +2 ok (5 toplam) | +%67 |
| Tier 4 | +3 ok (8 toplam) | +%60 |

**Kategori C - Ok Hasarini Artir**
Mevcut tum okcularin her ok basina verdigi hasar artirilir.

| Tier | Hasar Carpani | Gorsel Degisim |
|------|---------------|----------------|
| Tier 1 | x1.5 | Oklar daha hizli ucar |
| Tier 2 | x2.0 | Oklar ates efekti kazanir |
| Tier 3 | x3.0 | Oklar patlayici alan hasari (yaricap: 1.5m) |
| Tier 4 | x5.0 | Oklar zincirleme hasar (3 zombiye sicrar) |

**Kategori D - Kale Onarimi**

D1: Duvar Tamiri
- Duvar HP'sini %50 onarir
- Pasif duvar onarim hizini kalici olarak +10 HP/sn artirir

D2: Kapi Tamiri
- Kapi HP'sini tam olarak onarir
- Kapi max HP'sini kalici olarak +%25 artirir
- Kapiya Barikat ekler: 1 zombi dalgasini tamamen engeller (recharge: 60 sn)

**Kategori E - Yer Tuzagi (v2.0) [Wave 51+ ile acilir]**
Tunelci zombilere karsi ozel savunma.
- Tunelci zombilere x2 hasar
- Zemin alti algilama: Tunelci zombileri yuzey uzerindeyken isaretler
- Tier arttirilabilir: hasar + yavaslatma efekti

**Kategori F - Ozel Savunmalar (v2.0) [Wave 50+ ile acilir]**
Gec oyuna cesitlilik katan yeni mekanikler:
- Hendek: Zombileri yavaslatir (%30 hiz azaltma)
- Kaynar Yag: Kapiya yaklasan zombilere alan hasari
- Buyucu Kulesi: Periyodik AoE hasar (her 5 saniyede)

### 4.4 Upgrade Secim Stratejisi
Sistem, oyuncunun mevcut durumuna gore agirlikli olasilik uygular:

| Durum | Onerilen Secenek | Artirilmis Cikma Olasiligi |
|-------|------------------|---------------------------|
| Kapi hasarli (< %30 HP) | D2: Kapi Tamiri | +%30 |
| Duvar hasarli (< %20 HP) | D1: Duvar Tamiri | +%25 |
| Hic okcu yok | A: Okcu Ekleme | +%40 |
| Cok fazla okcu var (8+) | B veya C | +%35 |

Oyuncu yine de tum secenekleri gorebilir ve istedigini secebilir.

---

## 5. Dalga (Wave) Sistemi

### 5.1 Dalga Yapisi
| Wave Araligi | Zombi Sayisi | Yeni Tip | Ozellik |
|-------------|-------------|----------|---------|
| 1-10 | 10-50 | Sadece Surungun | Tutorial zone, cok kolay |
| 11-25 | 50-200 | Kosan eklenir | Hiz zorlugu baslar |
| 26-50 | 200-600 | Zirhli eklenir | Ok hasari onem kazanir |
| 51-100 | 600-2.000 | Tunelci eklenir | Duvar + kapi es zamanli baski |
| 101-200 | 2.000-8.000 | Dev eklenir | Boss odakli micro-yonetim |
| 200+ | 8.000-50.000+ | Ozel varyantlar | DOTS optimizasyonu kritik |

### 5.2 Dalga Skalasyon Formulleri
```
Zombi_Sayisi(wave) = 10 * (wave ^ 1.3)
Zombi_HP(wave) = 20 * (wave ^ 1.4)
Gold_Odulu(wave) = 5 * wave * (1 + Prestige * 0.1)
```

### 5.3 Ozel Dalgalar
- Her 10. dalga: Mini Boss dalgasi - 1 Dev + normal zombiler
- Her 25. dalga: Gece dalgasi - zombiler %30 hizli ama gorus alani azalir
- Her 50. dalga: Tsunami dalgasi - standart 3x zombi sayisi, sure sinirli (90 sn)

---

## 6. Ekonomi Sistemi

### 6.1 Para Birimi: Gold

**Temel Harcamalar:**
| Harcama | Maliyet | Etki |
|---------|---------|------|
| Anlik Kapi Onarimi | 200 Gold | Kapi HP'sini %50 onarir |
| Anlik Duvar Onarimi | 150 Gold | Duvar HP'sini %30 onarir |
| Click Damage +1 | 50 * Mevcut Seviye Gold | Tiklama hasari artar |
| Ok Hizi +%20 | 300 Gold | Tum okculara uygulanir |
| Okcu Hedefleme Degistir | 100 Gold | En guclu / En yakin / Dev once |

**Gold Sink'ler (v2.0):**
| Harcama | Maliyet | Etki |
|---------|---------|------|
| Auto-Clicker | 5.000 Gold | Saniyede 2 otomatik tiklama |
| Auto-Clicker +1 | 10.000 * Tier Gold | Otomatik tiklama hizi artar |
| Okcu Skin | 500 Gold | Kozmetik degisim |
| Kale Dekorasyonu | 1.000 Gold | Kozmetik |
| Gold Magnet | 2.000 Gold | Gold toplama yaricapi artar |
| XP Boost (gecici) | 300 Gold | 60 saniye +%50 XP |

### 6.2 Prestige Sistemi
Kale HP sifirlandiginda veya oyuncu manuel olarak Prestige baslatabilir.

**Temel Bonuslar:**
- Her Prestige: +%10 gold kazanimi, +%5 XP kazanimi (diminishing returns uygulenir)
- Prestige formula: `bonus = base * (1 - e^(-0.1 * prestige_level))` - max %80 cap

**Prestige ile Acilan Mekanikler (v2.0):**
| Prestige | Acilan Icerik |
|----------|---------------|
| 1 | Auto-Clicker satin alma hakki |
| 3 | Atesli Okcu tipi (daha yuksek hasar, yavaslatma) |
| 5 | Yeni karakter skin |
| 7 | Buzlu Okcu tipi (zombileri yavaslatir) |
| 10 | Ozel kale gorunumu + Bomba Okcu (AoE) |
| 15 | Prestige Shop - ozel upgrade agaci |

---

## 7. DOTS/ECS Teknik Mimarisi

### 7.1 Neden DOTS?
| Senaryo | MonoBehaviour | DOTS/ECS |
|---------|---------------|----------|
| 1.000 zombi | ~45 FPS | ~120 FPS |
| 5.000 zombi | ~8 FPS | ~90 FPS |
| 20.000 zombi | ~1 FPS (crash) | ~60 FPS |
| 50.000 zombi | Mumkun degil | ~30-40 FPS |

### 7.2 ECS Component Tasarimi

**Zombie Entity Components:**
- ZombieTag - entity'nin zombi oldugunu isaretler
- LocalTransform - pozisyon, rotasyon
- ZombieStats - HP, maxHP, speed, damage, goldReward, xpReward
- ZombieState - Enum: Moving / Attacking / Dead / Stunned
- ZombieTarget - hedef duvar/kapi EntityRef
- ZombieType - Surungun/Kosan/Zirhli/Tunelci/Dev Enum

**Castle / Archer Entity Components:**
- WallSegment - maxHP, currentHP, repairRate
- GateComponent - maxHP, currentHP, isBreached
- CastleHP - maxHP, currentHP (singleton)
- ArcherUnit - damage, fireRate, fireTimer, arrowCount, targetingMode
- ArrowProjectile - speed, damage, targetEntityRef, isSplash, isChain

**Game State (Singleton):**
- GameState - currentWave, gold, xp, level, clickDamage, totalPassiveDPS, isGameOver
- WaveState - zombiesRemaining, zombiesSpawned, totalZombiesInWave, waveActive

### 7.3 Job System Kullanimi
| Job | Calisma Sekli | Frekans |
|-----|---------------|---------|
| ZombieMoveJob | IJobEntity - her zombiyi paralel hareket ettirir | Her frame |
| ZombieAttackJob | IJobEntity - duvara ulasan zombi hasar hesabi | Her frame |
| ArcherShootJob | IJobEntity - okcu atis timer + hedef secimi | Her frame |
| ArrowMoveJob | IJobEntity - ok hareketi | Her frame |
| ArrowHitJob | IJobEntity - ok-zombi carpisma ve hasar | Her frame |
| WaveSpawnSystem | ISystem - dalga basinda zombi spawn | Dalga basinda |
| OfflineProgressJob | IJob - kapali sure hesabi | Oyun acilisinda |

### 7.4 GPU Instancing
- Graphics.DrawMeshInstanced ile tum ayni tip zombiler tek draw call'da cizilir
- AnimationTexture baking: zombie animasyonlari vertex shader ile GPU'da calistirilir
- LOD: Uzak zombiler dusuk poly mesh, yakin zombiler yuksek poly mesh kullanir

### 7.5 Bellek Yonetimi
- NativeArray ve NativeList ile GC baskisi sifirlanir
- Olu zombiler EntityCommandBuffer ile frame sonunda temizlenir
- Spawn pool: sabit boyutlu chunk allocator ile reuse sistemi

---

## 8. UI / UX Tasarimi

### 8.1 Ana HUD
| Element | Konum | Bilgi |
|---------|-------|-------|
| Kale HP Bar | Ekranin ust ortasi | Toplam kale can durumu |
| Duvar HP Bar | Her duvar uzerinde | Bolumsel hasar gosterimi |
| Kapi HP Bar | Kapi uzerinde | Kritik olunca kirmizi animasyon |
| Gold Sayaci | Sol ust | Anlik miktar + pasif kazanc |
| XP Bar | Sag ust | Level progress + level numarasi |
| Wave Gostergesi | Sag ust kose | Mevcut wave + zombi sayisi |
| Okcu Durumu | Sol alt panel | Her okcunun tier ve hasar bilgisi |

### 8.2 Level Atlama Ekrani
- 4 kart gosterilir (A, B, C, D) - gec oyunda E ve F eklenir
- Her kartta: ikon, baslik, kisa aciklama, mevcut seviyesi
- Secim sonrasi kart buyuyerek ekrandan ucar, oyun devam eder

### 8.3 Gorsel Feedback
- Zombi olumu: Basit dissolve shader (performans)
- Kritik click: Ekran flasi + ses efekti
- Duvar hasar: Duvar dokusunda catlak katmanlari
- Ok isabet: Kan partikülleri (ayarlardan kapatilabilir)

---

## 9. Ses Tasarimi
| Ses | Aciklama | Teknik Not |
|-----|----------|------------|
| Zombi Surusu | Ambians | Uzak/yakin pan + reverb |
| Ok Atisi | Randomize pitch | Object pool reuse |
| Kale Vurma | Impact sesi | Throttle: max 5/sn |
| Level Atlama | Zafer fanfari | UI layer |
| Kapi Kirilma | Crash sesi | One-shot |
| Muzik | Dinamik katmanli | FMOD entegrasyonu |

---

## 10. Denge ve Tasarim Prensipleri

### 10.1 Temel Denge Kurallari
- Oyuncu her zaman kurtarilabilir hissettirmeli
- Click damage gec oyunda bile HER ZAMAN ise yaramali (v2.0 olcekleme formulu ile)
- Yeni okcu eklemek her zaman hasar artirmaktan daha az verimli olmali
- Kapi tamiri gec oyunda duvar tamirinden daha kritik hale gelir
- Prestige bonusu diminishing returns ile cap'lenir (v2.0)

### 10.2 Difficulty Curve
| Asama | Wave Araligi | Zorluk | Oyuncu Hissi |
|-------|-------------|--------|--------------|
| Honeymoon | 1-15 | Cok kolay | Omnipotent |
| Ogrenme | 16-40 | Orta | Ilk hasar gorulur |
| Denge | 41-80 | Zor | Dikkatli secim gerekir |
| Kriz | 81-150 | Cok zor | Reaktif kararlar |
| Prestige Noktasi | 150+ | Ezici | Prestige degerlendirilir |

---

## 11. Sosyal Ozellikler (v2.0)

### 11.1 Leaderboard
- En yuksek wave siralama (global + arkadaslar)
- Haftalik challenge: Ozel kosullarla (sadece click, okcu yok vs.) en yuksek wave
- Steam Achievements entegrasyonu

### 11.2 Haftalik Challenge
- Her pazartesi yeni challenge baslar
- Ozel kurallar: "Sadece 3 okcu", "Click hasar yok", "Hizli dalgalar" vs.
- Odul: Ozel kozmetikler + bonus Prestige token

---

## 12. Uretim Plani

### 12.1 Milestone'lar
| Milestone | Hedef Tarih | Deliverable |
|-----------|------------|-------------|
| M0 - Prototype | 2025 Q4 | ECS zombi hareketi + click damage, 10k zombi test |
| M1 - Core Loop | 2026 Q1 | Upgrade secimleri + dalga sistemi + kale HP |
| M2 - Content | 2026 Q2 | Tum zombi tipleri + ozel dalgalar + prestige |
| M3 - Polish | 2026 Q3 | Ses, VFX, UI/UX, balancing, playtesting |
| M4 - Launch | 2026 Q3 | Steam launch, localization (EN/TR minimum) |

### 12.2 Demo Scope (v2.0)
Ilk calisir demo icin minimum set:
- [x] Tek zombi tipi (Surungun)
- [x] Duvara okcu yerlestirme
- [x] Ok hasari upgrade
- [x] Kapi tamir
- [x] Click damage
- [x] Basit dalga sistemi
- [x] Kale HP / Duvar HP / Kapi HP
- [x] Level atlama ekrani (basit)

### 12.3 Risk Analizi
| Risk | Olasilik | Etki | Onlem |
|------|----------|------|-------|
| DOTS ogrenme egrisi uzar | Yuksek | Yuksek | Early prototype - M0'da dogrula |
| GPU Instancing edge case'leri | Orta | Orta | Erken profiling, fallback LOD |
| Denge problemi | Yuksek | Orta | Playtest verileri ile ayarla |
| Offline ilerleme exploit | Dusuk | Dusuk | Max offline sure cap |
| **DOTS + UI entegrasyonu (v2.0)** | **Orta** | **Yuksek** | **Hybrid renderer, UI Toolkit test** |
| **Prestige denge (v2.0)** | **Orta** | **Orta** | **Diminishing returns formulu** |

---

*DEAD WALLS - GDD v2.0*
*Bu dokuman canli bir belgedir. Gelistirme surecinde guncellenecektir.*
*v2.0 Degisiklikler: Click damage olcekleme, yeni upgrade kategorileri (E,F), gold sink'ler, prestige mekanikleri, sosyal ozellikler, offline netlik, tunelci counter, DOTS+UI riski*
