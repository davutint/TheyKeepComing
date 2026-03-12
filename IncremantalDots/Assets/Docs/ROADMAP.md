# DEAD WALLS — Yol Haritasi ve Yapilacaklar Listesi

> Bu dokuman projenin tek takip kaynagindir. Her gorev tamamlandiginda `[x]` ile isaretlenir.
> GDD v3.0 referans alinir. Milestone siralama GDD Section 17'ye dayanir.

---

## Mevcut Durum Ozeti (M0 — Prototype) ✅

Asagidakiler **tamamlanmis ve calisan** sistemlerdir:

| Sistem | Durum | Dosyalar |
|--------|-------|----------|
| Zombi ECS Entity | ✅ Tamam | ZombieComponents, ZombieAuthoring |
| Zombi State Machine (Moving/Attacking/Queued/Dead) | ✅ Tamam | ZombieState, BoundarySystem, ZombieDeathSystem |
| Physics Pipeline (force→hash→collision→integrate→boundary) | ✅ Tamam | Physics/ klasoru (5 system + SpatialHashGrid) |
| Double-Buffered Spatial Hash | ✅ Tamam | BuildSpatialHashSystem |
| Domino Queuing (zincir etkisi) | ✅ Tamam | BoundarySystem |
| Okcu Sistemi (hedefleme + ok atisi) | ✅ Tamam | ArcherShootSystem, ArrowMoveSystem, ArrowHitSystem |
| Wave Spawn (normal + stress test mode) | ✅ Tamam | WaveSpawnSystem |
| Duvar/Kapi/Kale HP + Hasar Zinciri | ✅ Tamam | CastleComponents, DamageApplySystem |
| Zombi Olum + XP Odulu | ✅ Tamam | DamageCleanupSystem |
| Sprite Sheet Animasyon Pipeline | ✅ Tamam | SpriteAnimationSystem, ZombieAnimationStateSystem |
| Temel HUD (HP bar, XP, Wave, Level) | ✅ Tamam | HUDController |
| Level-Up UI (3 hardcoded secenek) | ✅ Tamam | LevelUpUI, GameManager.ApplyUpgrade |
| Game Over UI | ✅ Tamam | GameOverUI |
| ~~Click Damage~~ | ❌ Kaldirildi | ~~ClickDamageSystem, ClickDamageHandler~~ |
| Profiler Analyzer Editor Tool | ✅ Tamam | ProfilerDataAnalyzer |

### Bilinen Bug'lar ve Teknik Borc (M0)
- [x] **BUG:** WaveSpawnSystem per-wave stats (HP, Speed, Damage) spawn edilen zombilere uygulaniyordu — `SpawnZombieBatch`'de `ZombieStats` set edildi
- [x] **BUG:** ProfilerDataAnalyzer "ZombieAttackSystem" → "ZombieAttackTimerSystem" + "DamageApplySystem" olarak duzeltildi
- [x] **Olu kod:** `ReachedTarget` component silindi
- [x] **Test modu:** StressTestMode default `false` yapildi, `ZombieDamage = 0f` bug'i duzeltildi
- [x] **GDD uyumsuzluk:** Gold, ClickDamage, ClickDamageRequest, ClickDamageSystem, ClickDamageHandler tamamen kaldirildi

---

## M1 — Kaynak + Bina Sistemi

> **Hedef:** 4 kaynak ekonomisi, bina yerlestirme, isci atama, nufus yonetimi.
> **Bagimliliklari:** Yok (M0 uzerine insa edilir)

### M1.1 — Temel Kaynak Altyapisi

- [x] Kaynak component'lari olustur: ResourceData (int), ResourceProductionRate, ResourceConsumptionRate, ResourceAccumulator (float)
- [x] GameStateAuthoring'e kaynak field'lari + Baker ekle (baslangic degerleri + test uretim/tuketim hizlari Inspector'dan)
- [x] ResourceTickSystem olustur — tek sistem, net hiz (uretim-tuketim) * dt → accumulator → int transfer
- [x] GameManager'a Resources, ResourceProduction, ResourceConsumption property + ReadECSData + RestartGame ekle
- [x] HUD'da 4 kaynak gosterimi: "Ahsap: 150 (+5.0/dk)" formati, string alloc caching

### M1.2 — Nufus Sistemi

- [x] `PopulationState` singleton component olustur
  - [x] Total, Workers, Archers, Idle, Capacity (int) + FoodPerAssignedPerMin (float)
- [x] GameStateAuthoring baker'ina PopulationState ekle (baslangic nufus + kapasite + test atama)
- [x] `PopulationTickSystem` olustur — Idle hesapla + FoodPerMin guncelle (ResourceTickSystem'den once)
- [x] Nufus kapasitesi kontrolu — Idle negatif olamaz (clamp >= 0)
- [x] Yemek tuketimi: atanmis her birey (isci + okcu) yemek tuketir, bosta bekleyen tuketmez
- [x] HUD'da nufus gosterimi: "Nufus: 10/20 (0 isci, 0 okcu, 10 bos)"

### M1.3 — Grid ve Bina Yerlestirme Altyapisi

- [x] Bina grid sistemi tasarla (Tilemap tabanli, sur ici alan)
  - [x] Grid boyutu ve hucre boyutu belirle (32x32, 1x1 hucre)
  - [x] Yerlestirilebilir/yerlestirilemez alan tanimi (buildable_zone Tilemap)
- [x] `BuildingComponents.cs` olustur
  - [x] `BuildingType` enum: Lumberjack, Quarry, Mine, Farm, House, Barracks, Fletcher, Blacksmith, WizardTower
  - [x] `BuildingData` (IComponentData): Type, Level, GridX, GridY
  - [x] `ResourceProducer` (IComponentData): ResourceType, RatePerWorkerPerMin, AssignedWorkers, MaxWorkers
  - [x] `PopulationProvider` (IComponentData): CapacityAmount
  - [x] `BuildingFoodCost` (IComponentData): FoodPerMin
- [x] `BuildingConfigSO` ScriptableObject olustur (prefab yerine SO + runtime entity)
- [x] `BuildingGridManager` olustur (MonoBehaviour — grid truth source)
  - [x] Grid uzerinde bos alan kontrolu (3x3 slot)
  - [x] Maliyet kontrolu + kaynak dusme
  - [x] Bina entity'si olustur + grid'i guncelle
  - [x] Restart'ta bina temizligi (GameManager entegrasyonu)
- [x] Bina yerlestirme UI'i olustur (`BuildingPlacementUI`)
  - [x] Bina secim menusu (SO listesinden otomatik buton)
  - [x] Ghost/preview gosterimi (yesil/kirmizi renk)
  - [x] Maliyet gosterimi + kaynak yeterliligi kontrolu
  - [x] Tikla-yerlestir mekanigi + sag tikla/Escape iptal
- [ ] ~~`BuildingAuthoring` baker~~ — Iptal: prefab yerine runtime CreateEntity + Tilemap gorsel secildi
- [ ] Yerlestirme kurallari: Oduncu→orman yanina vs. (M1.8 dogal kaynak zone'larina ertelendi)

### M1.4 — Kaynak Binalari (Oduncu, Tas Ocagi, Maden, Ciftlik)

- [ ] **Oduncu (Lumberjack)**
  - [ ] Prefab + Authoring olustur
  - [ ] Isci atanir → atanan isci sayisina gore Ahsap/dk uretir
  - [ ] Yerlestirme kurali: Orman dogal kaynagi yanina
  - [ ] Upgrade: uretim verimi artar (Level 1→2→3)
- [ ] **Tas Ocagi (Quarry)**
  - [ ] Prefab + Authoring olustur
  - [ ] Isci atanir → Tas/dk uretir
  - [ ] Yerlestirme kurali: Tas kaynagi yanina
  - [ ] Upgrade: uretim verimi artar
- [ ] **Maden (Mine)**
  - [ ] Prefab + Authoring olustur
  - [ ] Isci atanir → Demir/dk uretir
  - [ ] Yerlestirme kurali: Demir kaynagi yanina
  - [ ] Upgrade: uretim verimi artar
- [ ] **Ciftlik (Farm)**
  - [ ] Prefab + Authoring olustur
  - [ ] Isci atanir → Yemek/dk uretir
  - [ ] Yerlestirme kurali: Herhangi bir yer
  - [ ] Upgrade: uretim verimi artar

### M1.5 — Altyapi Binalari (Ev, Kale Upgrade)

- [ ] **Ev (House)**
  - [ ] Prefab + Authoring olustur
  - [ ] Nufus kapasitesi arttirir (+5/+10/+15 level bazli)
  - [ ] Surekli yemek gideri vardir (icindekiler tuketir)
  - [ ] Upgrade: daha fazla kapasite, daha fazla yemek gideri
- [ ] **Kale Upgrade Mekanigi**
  - [ ] Mevcut CastleAuthoring'e upgrade sistemi ekle
  - [ ] Tas + Ahsap maliyet ile nufus kapasitesi arttir
  - [ ] Grid slot harcamaz — pahalı ama verimli

### M1.6 — Askeri Binalar (Kisla, Ok Atolyesi, Demirci)

- [ ] **Kisla (Barracks)**
  - [ ] Prefab + Authoring olustur
  - [ ] Nufustan okcu egitir (Yemek + Ahsap harcar)
  - [ ] Egitim suresi + kuyruk sistemi
  - [ ] Upgrade: daha hizli egitim
- [ ] **Ok Atolyesi (Fletcher)**
  - [ ] Prefab + Authoring olustur
  - [ ] Isci atanir, Ahsap tuketir, Ok uretir
  - [ ] Okcular ok olmadan ates edemez/cok yavas ates eder — ok envanteri sistemi
  - [ ] `ArrowSupply` singleton component olustur (toplam ok miktari)
  - [ ] ArcherShootSystem'i guncelle: ok yoksa ates etme
  - [ ] Upgrade: daha hizli uretim
- [ ] **Demirci (Blacksmith)**
  - [ ] Prefab + Authoring olustur
  - [ ] Demir tuketir, kalici upgrade'ler sunar (tech tree)
  - [ ] Upgrade secenekleri: Ok hasari artis, Duvar guclendirme, Mancinik unlock, Ozel ok tipleri
  - [ ] Upgrade UI paneli

### M1.7 — Isci Atama UI

- [ ] Binaya tiklaninca acilan detay paneli olustur
  - [ ] Bina adi, seviyesi, uretim hizi gosterimi
  - [ ] Atanmis isci sayisi gosterimi
  - [ ] Isci ekleme/cikarma butonlari (+/-)
  - [ ] Idle nufustan isci cek / iscileri idle'a dondur
  - [ ] Upgrade butonu (maliyet gosterimi + kaynak kontrolu)
  - [ ] Yikma butonu

### M1.8 — Dogal Kaynak Noktalari

- [ ] Haritada sabit dogal kaynak konumlari olustur (Tilemap veya entity)
  - [ ] Orman noktasi (Oduncu yerlestirme icin)
  - [ ] Tas kaynagi noktasi (Tas Ocagi icin)
  - [ ] Demir kaynagi noktasi (Maden icin)
- [ ] Yerlestirme kurali: kaynak binasi ilgili dogal kaynaga yakin mi kontrolu
- [ ] Gorsel gosterim (Tilemap sprite veya overlay)

### M1.9 — Eski Sistemlerin Temizligi

- [x] `Gold` alanini `GameStateData`'dan kaldir
- [x] `ClickDamage` alanini `GameStateData`'dan kaldir
- [x] `ClickDamageSystem` ve `ClickDamageHandler` kaldir (GDD'de click damage yok)
- [x] `ClickDamageRequest` component kaldir
- [ ] `GameManager.ApplyUpgrade` eski 3-buton sistemini kaldir (M2.4 kart sistemiyle degisecek)
- [ ] `LevelUpUI` eski hardcoded butonlari kaldir (M2.4'te kart sistemi ile degisecek)
- [x] `DamageCleanupSystem`'den Gold reward'i kaldir, XP reward kalsin
- [x] HUD'u yeni kaynak sistemine uyumlu hale getir (M1.1'de yapildi — 4 kaynak TMP_Text)

### M1.10 — MD Dokumantasyonu

- [x] `Components/ARCHITECTURE.md` guncelle (Gold, ClickDamage, ReachedTarget kaldirildi)
- [ ] `Components/EDITOR_SETUP.md` guncelle
- [x] `Systems/ARCHITECTURE.md` guncelle (ClickDamageSystem kaldirildi, sistem sirasi guncellendi)
- [ ] `Systems/EDITOR_SETUP.md` guncelle
- [ ] Yeni klasor olusturulduysa o klasore ARCHITECTURE + EDITOR_SETUP md yaz

---

## M2 — Savunma Derinligi

> **Hedef:** Mancinik, tuzaklar, buyuler, level-up kart sistemi.
> **Bagimliliklari:** M1 (kaynak sistemi, Demirci unlock mekanigi)

### M2.1 — Mancinik (Catapult)

- [ ] `CatapultComponents.cs` olustur
  - [ ] `CatapultUnit`: Damage, SplashRadius, FireRate, FireTimer, AmmoCount
  - [ ] `CatapultProjectile`: Speed, Damage, SplashRadius, TargetPos (float3)
- [ ] `CatapultAuthoring` + prefab olustur
- [ ] `CatapultShootSystem` olustur — en kalabalik zombi grubuna ates
  - [ ] Spatial hash ile yogunluk tespiti
  - [ ] Yavas atis hizi, buyuk AoE
- [ ] `CatapultProjectileSystem` olustur — mermi hareketi + AoE hasar uygulama
- [ ] Tas tuketimi: her atis Tas harcar (ResourceConsumption baglantisi)
- [ ] Demirciden unlock mekanigi (M1.6 Demirci ile entegrasyon)

### M2.2 — Tuzak Sistemi

- [ ] `TrapComponents.cs` olustur
  - [ ] `TrapData`: TrapType (enum), Damage, SlowAmount, Durability, MaxDurability
  - [ ] `TrapType` enum: SpikedStakes, Trench, BearTrap
- [ ] `TrapAuthoring` + prefablar olustur (3 tuzak tipi)
- [ ] `TrapPlacementSystem` — sur disina grid mantigi ile yerlestirme
- [ ] `TrapTriggerSystem` olustur — zombi uzerinden gecince etki
  - [ ] **Sivri Kaziklar:** Hasar verir, belirli sayida zombiden sonra kirilir (Ahsap maliyet)
  - [ ] **Hendek:** Yavaslatma, kalici, kirilmaz (Tas maliyet)
  - [ ] **Ayi Tuzagi:** Hasar + durdurma, tek kullanimlik (Demir maliyet)
- [ ] Tuzak yerlestirme UI (sur disi alan icin)

### M2.3 — Buyu Sistemi

- [ ] `SpellComponents.cs` olustur
  - [ ] `SpellData`: SpellType (enum), Tier, Cooldown, CooldownTimer, Damage/Effect
  - [ ] `SpellType` enum: Fireball, IceWall, LightningChain, HealingAura
  - [ ] `ActiveSpells` (IBufferElement veya singleton) — oyuncunun sahip oldugu buyuler
- [ ] `SpellSystem` olustur — cooldown yonetimi + etki uygulama
  - [ ] **Ates Topu:** AoE hasar (spatial hash ile zombi tespiti)
  - [ ] **Buz Duvari:** AoE yavaslatma (zombi speed modifier)
  - [ ] **Simsek Zinciri:** Zincirleme hasar (en yakin N zombiye sicar)
  - [ ] **Iyilestirme Aurasi:** Duvar HP onarimi
- [ ] Tier sistemi: ayni buyuyu tekrar secince guc artar
- [ ] Buyucu Kulesi on kosulu: buyu kartlari sadece Buyucu Kulesi varsa havuzda
- [ ] Buyu kullanma UI (cooldown gosterimi, hedef secimi)

### M2.4 — Level-Up Kart Sistemi (Roguelike)

- [ ] Mevcut LevelUpUI'yi tamamen yeniden tasarla
- [ ] `CardComponents.cs` olustur
  - [ ] `CardData` ScriptableObject: CardID, Name, Description, Icon, Category, Tier, PrerequisiteBuilding, Effect
  - [ ] `CardCategory` enum: Population, Archer, Spell, Defense, Economy
- [ ] Kart havuzu sistemi olustur
  - [ ] Tum kartlar ScriptableObject olarak tanimla
  - [ ] Havuzdan 3 kart cek (on kosullari kontrol et)
  - [ ] Secilen kartin tierini artir (ayni kart tekrar secilirse)
- [ ] Kart secim UI olustur
  - [ ] 3 kart gosterimi: ikon, baslik, aciklama, tier seviyesi
  - [ ] On kosul karsilanmayan kartlar gri/kilitli
  - [ ] Secim animasyonu + efekt
- [ ] Kart etkileri uygulama sistemi:
  - [ ] **Nufus Kartlari:** Multeciler (+X nufus, kapasite varsa)
  - [ ] **Okcu Kartlari:** Ok hasari artisi, atis hizi artisi, coklu ok, atesli ok (DoT), buzlu ok (slow)
  - [ ] **Buyu Kartlari:** Ates Topu, Buz Duvari, Simsek Zinciri, Iyilestirme Aurasi
  - [ ] **Savunma Kartlari:** Duvar HP artisi, hendek, barikat
  - [ ] **Ekonomi Kartlari:** Uretim hizi boost, verimli isciler
- [ ] XP esigi her level'da artar (Inspector'dan ayarlanabilir egri)

### M2.5 — Ozel Bina: Buyucu Kulesi

- [ ] Buyucu Kulesi prefab + authoring olustur
- [ ] Grid'de yer kaplar — stratejik yatirim karari
- [ ] Insa edildiginde buyu kartlarini kart havuzuna ekle
- [ ] Upgrade: buyu kartlarinin cikma olasiligini/gucunu artir

### M2.6 — MD Dokumantasyonu

- [ ] Yeni component/system dosyalari icin ARCHITECTURE + EDITOR_SETUP md'leri yaz
- [ ] Mevcut md'leri guncelle

---

## M3 — Gun + Wave Sistemi

> **Hedef:** 100 gun yapisi, gun dongusu, wave skalasyonu, taciz sistemi, final wave.
> **Bagimliliklari:** M1 (kaynak uretimi gun bazli), M2 (savunma derinligi)

### M3.1 — Gun Dongusu (DayCycleSystem)

- [ ] `DayCycleComponents.cs` olustur
  - [ ] `DayState` singleton: CurrentDay (int), TotalDays (100), DayTimer (float), DayDuration (float), IsWaveDay (bool)
- [ ] `DayCycleAuthoring` baker olustur (gun suresi Inspector'dan)
- [ ] `DayCycleSystem` olustur
  - [ ] Gun sayaci her frame ilerler
  - [ ] Gun bittiginde sonraki gune gecis (otomatik, buton yok)
  - [ ] Wave gunu ise gun uzar — wave bitene kadar devam
  - [ ] Gun gecisinde kaynak uretimi/tuketimi tetiklenir
- [ ] HUD'da gun sayaci: "Gun 34 / 100"

### M3.2 — Wave Skalasyonu

- [ ] WaveSpawnSystem'i guncelle: StressTestMode → normal wave mode
- [ ] Gun bazli wave sikligi:
  - [ ] Gun 1-20: Her 5 gunde bir
  - [ ] Gun 21-50: Her 3 gunde bir
  - [ ] Gun 51-80: Her 2 gunde bir
  - [ ] Gun 81-99: Her gun
  - [ ] Gun 100: Final wave
- [x] **BUG FIX:** Per-wave stats (HP, Speed, Damage) spawn edilen zombilere uygula (M0'da yapildi)
- [ ] Zombi sayisi gun numarasina gore olcekle (Inspector'dan ayarlanabilir egri)
- [ ] Zombi HP gun numarasina gore olcekle
- [ ] Wave config'i Inspector/Editor Window'dan ayarlanabilir yap

### M3.3 — Taciz Sistemi

- [ ] Wave disi kucuk zombi gruplari surekli gelsin
- [ ] Taciz sikligi ve kalabaligi gun ilerledikce artsin
- [ ] `HarassmentSpawnSystem` olustur veya WaveSpawnSystem'e entegre et
- [ ] Gun 1-20: cok seyrek, 1-3 zombi
- [ ] Gun 81-99: sik, 10-20 zombi grubu

### M3.4 — Final Wave (Gun 100)

- [ ] 50.000 zombi hedefi
- [ ] Ozel spawn paterni (belki dalgalar halinde, performans icin)
- [ ] Performans testi: 50.000 zombi FPS kontrolu
- [ ] Kazanma kosulu: final wave atlatilirsa → zafer ekrani

### M3.5 — Kazanma / Kaybetme Guncelleme

- [ ] Kazanma: 100. gun dalgesini atlat → zafer ekrani olustur
- [ ] Kaybetme: mevcut GameOver'i koru (Castle HP → 0)
- [ ] Game Over ekranini guncelle: gun sayisi, oldurulen zombi, max nufus, toplam kaynak, secilen kartlar
- [ ] Dramatik sahne: ciglik sesleri, ekran blur efekti (opsiyonel, M4'e birakılabilir)

### M3.6 — MD Dokumantasyonu

- [ ] DayCycle sistemi icin ARCHITECTURE + EDITOR_SETUP md yaz
- [ ] Wave sistemi md'lerini guncelle

---

## M4 — Event + Polish

> **Hedef:** Rastgele eventler, gorsel canlilik, ses, UI/UX cilalama, denge.
> **Bagimliliklari:** M1, M2, M3 (tum ana sistemler hazir olmali)

### M4.1 — Event Sistemi

- [ ] `EventComponents.cs` olustur
  - [ ] `EventData` ScriptableObject: EventID, Name, Description, EffectType, EffectValue, MinDay, Probability, IsChoice
- [ ] `EventSystem` olustur — gun basinda rastgele event tetikle
- [ ] **Olumlu Eventler:**
  - [ ] Komsu kasabadan erzak (+Tas, +Ahsap)
  - [ ] Multeciler kapida (+Nufus, kapasite varsa)
  - [ ] Gezgin tuccar (kaynak takasi)
- [ ] **Olumsuz Eventler:**
  - [ ] Firtina (Ciftlikler 1 gun uretmiyor)
  - [ ] Maden coktu (Maden 2 gun devre disi)
  - [ ] Veba (Nufus kaybi)
- [ ] **Secimli Eventler:**
  - [ ] Risk/odul karari popup'i
  - [ ] Kabul et / reddet secenekleri
- [ ] Event popup UI olustur
- [ ] Event havuzu + olasiliklar Inspector/Editor Window'dan ayarlanabilir

### M4.2 — Gorsel Canlilik (Ambient)

- [ ] Ambient koylu entity'leri olustur
  - [ ] Kasaba icinde rastgele dolasan kucuk sprite'lar
  - [ ] Binalar arasinda yuruyen isciler (gorsel, gercek isci sayisini yansitmak zorunda degil)
  - [ ] Surda nobet tutan okcular (gorsel)
- [ ] Atmosfer kontrasti: sakin gun vs wave kaos farki

### M4.3 — Ses Tasarimi

- [ ] Temel ses altyapisi olustur (AudioManager)
- [ ] Zombi surusu ambiyansi (uzak/yakin pan)
- [ ] Ok atisi sesi (randomize pitch)
- [ ] Sur'a vurus impact sesi
- [ ] Duvar kirilmasi dramatik sesi
- [ ] Level atlama fanfari
- [ ] Event bildirim sesi
- [ ] Kasaba ambiyansi (kus, demirci, ciftlik)
- [ ] Dinamik muzik: sakin gun vs wave muzigi

### M4.4 — UI/UX Polish

- [ ] Ana HUD'u GDD Section 14'e gore yeniden tasarla
  - [ ] Sur HP bar (ust)
  - [ ] Gun sayaci (ust orta)
  - [ ] Kaynak paneli (sol ust) + uretim/tuketim hizi
  - [ ] Nufus gostergesi (sol ust, kaynak altinda)
  - [ ] XP bar (sag ust)
  - [ ] Wave uyarisi animasyonu (orta)
  - [ ] Minimap (sag alt, opsiyonel)
- [ ] Bina menusu cilalamasi
- [ ] Level atlama kart UI animasyonlari
- [ ] Game over ekrani istatistik tablosu genisletme
- [ ] Sur onarimi gorsel efekti

### M4.5 — Denge Ayarlari

- [ ] Custom Editor Window olustur (GDD Section 16)
  - [ ] Wave ayarlari: gun bazli wave sikligi, zombi sayisi egrisi
  - [ ] Kaynak ayarlari: uretim/tuketim hizlari, baslangic degerleri
  - [ ] Bina ayarlari: maliyet, verim, upgrade degerleri
  - [ ] Nufus ayarlari: kapasite, yemek tuketim oranlari
  - [ ] Zombi ayarlari: HP egrisi, hiz, hasar
  - [ ] Kart ayarlari: havuz, olasiliklar, tier degerleri
  - [ ] Event ayarlari: havuz, olasiliklar, etki degerleri
  - [ ] Savunma ayarlari: okcu/mancinik/buyu/tuzak stats
  - [ ] Sur ayarlari: HP, onarim hizi
- [ ] Playtest ve iterasyon

### M4.6 — MD Dokumantasyonu

- [ ] Tum yeni sistemler icin ARCHITECTURE + EDITOR_SETUP md
- [ ] Mevcut md'lerin final guncellemesi

---

## M5 — Launch

> **Hedef:** Steam hazirligi, lokalizasyon, son test.
> **Bagimliliklari:** M1-M4 tamamlanmis olmali

### M5.1 — Steam Entegrasyonu

- [ ] Steamworks SDK entegrasyonu
- [ ] Steam Cloud save
- [ ] Steam Achievement tanimlari
- [ ] Store page materyalleri (screenshot, trailer, aciklama)

### M5.2 — Lokalizasyon

- [ ] Lokalizasyon altyapisi (Unity Localization package veya custom)
- [ ] Turkce (TR) tam ceviri
- [ ] Ingilizce (EN) tam ceviri
- [ ] Dil secme menusu

### M5.3 — Son Test ve Optimizasyon

- [ ] 50.000 zombi performans testi (hedef: 30-40 FPS)
- [ ] Bellek sızıntısı kontrolu (NativeContainer dispose)
- [ ] 100 gunluk tam run testi (baslangiçtan sona)
- [ ] Farkli build path'lerin denge testi
- [ ] Edge case testleri (kaynak sifir, nufus sifir, tum binalar yikildi vs.)

### M5.4 — MD Dokumantasyonu

- [ ] Tum md dosyalarinin final revizyonu
- [ ] Bu ROADMAP.md'yi final duruma getir

---

## Ilerleme Ozeti

| Milestone | Toplam Gorev | Tamamlanan | Yuzde |
|-----------|-------------|------------|-------|
| M0 Bug Fix | 5 | 5 | %100 |
| M1 Kaynak + Bina | ~50 | 31 | %62 |
| M2 Savunma Derinligi | ~35 | 0 | %0 |
| M3 Gun + Wave | ~20 | 1 | %5 |
| M4 Event + Polish | ~35 | 0 | %0 |
| M5 Launch | ~12 | 0 | %0 |
| **TOPLAM** | **~157** | **37** | **%24** |

---

## Oneri: Baslama Sirasi

```
1. M0 Bug Fix ✅ TAMAMLANDI
2. M1.9 Temizlik (Gold/Click kaldir) ✅ KISMEN TAMAMLANDI (LevelUpUI + ApplyUpgrade M2.4'e birakildi)
3. M1.1 Kaynak Altyapisi → M1.2 Nufus
4. M1.3 Grid + Bina Yerlestirme (en buyuk teknik risk)
5. M1.4-M1.8 Binalar (grid hazir olduktan sonra)
6. M3.1-M3.2 Gun + Wave (kaynak sistemi bunu bekliyor)
7. M2.4 Kart Sistemi (en cok gameplay etkisi olan M2 parcasi)
8. M2.1-M2.3 Mancinik + Tuzak + Buyu
9. M4 Polish (tum mekanikler hazir olduktan sonra)
10. M5 Launch
```

---

*Son guncelleme: 2026-03-12 (M0 Bug Fix tamamlandi, M1.1 tamamlandi, M1.2 tamamlandi, M1.3 tamamlandi, M1.9 kismen tamamlandi)*
*GDD Referans: DEAD_WALLS_GDD_v3.0.md*
