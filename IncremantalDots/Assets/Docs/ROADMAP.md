# DEAD WALLS — Yol Haritasi ve Yapilacaklar Listesi (v4.0)

> Bu dokuman projenin tek takip kaynagindir. Her gorev tamamlandiginda `[x]` ile isaretlenir.
> GDD v4.0 referans alinir. Her sistem icin yazilan EDITOR_SETUP md dosyasi UI kurulum talimatlarini da icerir.
> Milestone siralama: M0 → M1 → M-CLN → M-ISO → M2 → M3 → M4 → M5

---

## M0 — Prototype ✅

Asagidakiler **tamamlanmis ve calisan** sistemlerdir:

| Sistem | Durum | Dosyalar |
|--------|-------|----------|
| Zombi ECS Entity | ✅ | ZombieComponents, ZombieAuthoring |
| Zombi State Machine (Moving/Attacking/Queued/Dead) | ✅ | ZombieState, BoundarySystem, ZombieDeathSystem |
| Physics Pipeline (force→hash→collision→integrate→boundary) | ✅ | Physics/ (5 system + SpatialHashGrid) |
| Double-Buffered Spatial Hash | ✅ | BuildSpatialHashSystem |
| Domino Queuing (zincir etkisi) | ✅ | BoundarySystem |
| Okcu Sistemi (hedefleme + ok atisi) | ✅ | ArcherShootSystem, ArrowMoveSystem, ArrowHitSystem |
| Wave Spawn (normal + stress test mode) | ✅ | WaveSpawnSystem |
| Duvar/Kapi/Kale HP + Hasar Zinciri | ✅ | CastleComponents, DamageApplySystem |
| Zombi Olum + XP Odulu | ✅ | DamageCleanupSystem |
| Sprite Sheet Animasyon Pipeline | ✅ | SpriteAnimationSystem, ZombieAnimationStateSystem |
| Temel HUD (HP bar, XP, Wave, Level) | ✅ | HUDController |
| Profiler Analyzer Editor Tool | ✅ | ProfilerDataAnalyzer |

### Bug Fix'ler (M0) ✅
- [x] WaveSpawnSystem per-wave stats fix
- [x] ProfilerDataAnalyzer sistem adi duzeltmesi
- [x] `ReachedTarget` olu kod silindi
- [x] StressTestMode default false, ZombieDamage bug fix
- [x] Gold, ClickDamage tamamen kaldirildi

---

## M1 — Kaynak + Bina Sistemi (~%90 Tamamlandi)

> **Hedef:** 4 kaynak ekonomisi, bina yerlestirme, isci atama, nufus yonetimi.

### M1.1 — Temel Kaynak Altyapisi ✅
- [x] ResourceData, ResourceProductionRate, ResourceConsumptionRate, ResourceAccumulator component'lari
- [x] GameStateAuthoring kaynak baker
- [x] ResourceTickSystem (net hiz * dt → accumulator → int)
- [x] GameManager kaynak property'leri + RestartGame
- [x] HUD 4 kaynak gosterimi

### M1.2 — Nufus Sistemi ✅
- [x] PopulationState singleton (Total, Workers, Archers, Idle, Capacity, FoodPerAssignedPerMin)
- [x] PopulationTickSystem
- [x] Kapasite kontrolu + yemek tuketimi
- [x] HUD nufus gosterimi

### M1.3 — Grid ve Bina Yerlestirme ✅
- [x] Tilemap tabanli grid (32x32, 1x1 hucre)
- [x] BuildingComponents (BuildingType enum, BuildingData, ResourceProducer, PopulationProvider, BuildingFoodCost)
- [x] BuildingConfigSO (ScriptableObject)
- [x] BuildingGridManager (grid truth source + entity olusturma)
- [x] BuildingPlacementUI (secim menusu + ghost preview + tikla-yerlestir)
- [x] Yerlestirme kurallari (M1.8'de tamamlandi)

### M1.4 — Kaynak Binalari ✅
- [x] BuildingProductionSystem
- [x] Test rate'ler sifirlandi
- [x] SO asset'leri (Lumberjack, Quarry, Mine, Farm)
- [ ] Upgrade: uretim verimi artar (M2.5 kart sistemiyle)

### M1.5 — Altyapi Binalari ✅
- [x] Ev (House) — kapasite + yemek gideri
- [x] BuildingPopulationSystem
- [x] Kale Upgrade (CastleUpgradeData + CastleUpgradeUI)
- [ ] Ev upgrade (M2.5 kart sistemiyle)

### M1.6 — Askeri Binalar ✅
- [x] Kisla (ArcherTrainer + BarracksTrainingSystem)
- [x] Ok Atolyesi (ArrowProducer + ArrowProductionSystem + ArrowSupply)
- [x] Demirci (placeholder — tech tree M2.5'te)
- [x] ~~Mancinik~~ (M-CLN'de kaldirilacak)

### M1.7 — Isci Atama UI + Bina Detay Paneli ✅ (kod tamam)
- [x] BuildingDetailUI sistemi yazildi
- [ ] Editor kurulumu yapilmadi — BUILDING_DETAIL_UI_EDITOR_SETUP.md takip edilecek

### M1.8 — Dogal Kaynak Noktalari ✅
- [x] ResourcePointType enum + zone tilemap + _zoneGrid cache
- [x] IsNearZone proximity + CanPlace zone kontrolu
- [x] BuildingConfigSO RequiredZone + ZoneProximityRadius

### M1.9 — Eski Sistem Temizligi (Kismi)
- [x] Gold, ClickDamage, ClickDamageRequest, ClickDamageSystem kaldirildi
- [x] DamageCleanupSystem'den Gold reward kaldirildi
- [x] HUD yeni kaynak sistemine uyumlandi
- [ ] `GameManager.ApplyUpgrade` eski 3-buton sistemi (M2.5 kart sistemiyle degisecek)
- [ ] `LevelUpUI` eski hardcoded butonlar (M2.5 kart sistemiyle degisecek)

---

## M-CLN — Temizlik (Catapult + v3.0 Kalintilari)

> **Hedef:** v4.0'da kaldirilan sistemleri temizle. Izometrik gecis oncesi temiz kod tabani.
> **Bagimliliklari:** M1

### M-CLN.1 — Catapult/Mancinik Sistemi Kaldirma
- [x] `CatapultComponents.cs` sil
- [x] `CatapultAuthoring.cs` sil
- [x] `CatapultProjectileAuthoring.cs` sil
- [x] `CatapultShootSystem.cs` sil
- [x] `CatapultProjectileMoveSystem.cs` sil
- [x] `CatapultProjectileHitSystem.cs` sil
- [x] `WallSlotManager.cs` sil
- [x] `BuildingType.Catapult` enum degeri → `WizardAcademy` olarak degistirildi
- [x] `WaveConfigAuthoring`'den CatapultPrefabData + CatapultProjectilePrefabData kaldirildi
- [x] `BuildingPlacementUI`'dan IsWallSlotBuilding branch'i kaldirildi
- [x] `GameManager.RestartGame`'den mancinik cleanup kodu kaldirildi
- [x] `BuildingConfigSO`'dan IsWallSlotBuilding field'i kaldirildi (RequireBlacksmith korundu)
- [ ] Catapult prefab'larini sahneden kaldir (Editor'de manuel yapilacak)
- [ ] Catapult SO asset'ini sil (Editor'de manuel yapilacak)

### M-CLN.2 — Demirci Referans Temizligi
- [x] Demirci'den ~~Mancinik unlock~~ kaldirildi (BuildingConfigSO IsWallSlotBuilding silindi)
- [x] ~~Ozel ok tipleri~~ referanslari kaldirildi (Ok Atolyesi sadece standart ok uretir)
- [x] Kalan upgrade'ler: Ok hasari artis, Duvar guclendirme, Buyucu Kulesi unlock, Tuzak unlock

### M-CLN.3 — MD Dosyalari Temizligi
- [x] CATAPULT_COMPONENTS_ARCHITECTURE.md silindi
- [x] CATAPULT_COMPONENTS_EDITOR_SETUP.md silindi
- [x] CATAPULT_SYSTEM_ARCHITECTURE.md silindi
- [x] CATAPULT_SYSTEM_EDITOR_SETUP.md silindi
- [x] WALL_SLOT_ARCHITECTURE.md silindi
- [x] WALL_SLOT_EDITOR_SETUP.md silindi
- [x] SYSTEM_EXECUTION_ORDER_ARCHITECTURE.md guncellendi (catapult sistemleri cikarildi)
- [x] COMPONENT_MAP_ARCHITECTURE.md guncellendi

### M-CLN.4 — Ok Atolyesi Sadeleştirme
- [x] Ok Atolyesi sadece standart ok uretir (ozel ok tipleri referanslari yoktu, sadece GDD'de vardi)
- [x] Fletcher upgrade = hiz artisi (kart sistemiyle M2.5'te)

---

## M-ISO — Izometrik Gecis

> **Hedef:** Rectangle grid → izometrik, WallX → perimeter, kamera, 8 yonlu sprite, tile/sprite gecisi.
> **Bagimliliklari:** M-CLN (temiz kod tabani)

### M-ISO.1 — Grid Sistemi Donusumu
- [ ] Unity Grid component'ini Isometric'e cevir (CellLayout=2, CellSize=1,0.5,1)
- [ ] `BuildingGridManager.WorldToGrid()` izometrik math'e guncelle
- [ ] `BuildingGridManager.GridToWorld()` izometrik math'e guncelle
- [ ] `BuildingPlacementUI` ghost preview → baklava (diamond) snap
- [ ] `BuildingDetailUI` tiklama → izometrik grid'de dogru hucre tespiti
- [ ] Tilemap layer'lari izometrik tile'larla yeniden boya (Fantasy Kingdom Tileset)
- [ ] BUILDING_GRID_ARCHITECTURE.md + BUILDING_GRID_EDITOR_SETUP.md guncelle
  - EDITOR_SETUP: Yeni Grid ayarlari (CellLayout, CellSize), Tilemap Palette olusturma, tile boyama adimlari

### M-ISO.2 — Kamera Sistemi
- [ ] `CameraSetup.cs` guncelle — izometrik gorunum icin pozisyon/boyut
- [ ] Kaydirmali kamera (WASD veya edge scroll — buyuk harita icin)
- [ ] Zoom in/out (scroll wheel)
- [ ] Kamera sinir kontrolu (harita disina cikmasin)
- [ ] CAMERA_SYSTEM_ARCHITECTURE.md + CAMERA_SYSTEM_EDITOR_SETUP.md yaz
  - EDITOR_SETUP: Kamera objesine hangi component eklenir, zoom/pan parametreleri

### M-ISO.3 — Perimeter Sur Sistemi
- [ ] `WallXPosition` singleton → `PerimeterWall` sistemi (coklu segment)
- [ ] `WallSegment` component'ine `DamageStage` (int, 0-4) ekle
- [ ] `ApplyMovementForceSystem` guncelle: `WallX - pos.x` → `normalize(center - pos)`
- [ ] `BoundarySystem` guncelle: `pos.x <= WallX` → mesafe/perimeter kontrolu
- [ ] Sur segment HP'leri bagimsiz
- [ ] Sur tile'larini Fantasy Kingdom Tileset'ten yerlestir
- [ ] PERIMETER_WALL_ARCHITECTURE.md + PERIMETER_WALL_EDITOR_SETUP.md yaz
  - EDITOR_SETUP: Sur segment'lerinin sahnede nasil yerlestirilecegi, tile referanslari

### M-ISO.4 — Zombie Spawn 360°
- [ ] `WaveSpawnSystem` guncelle: sag taraftan → harita kenarlari 360° spawn
- [ ] Spawn pozisyonlari: harita kenarinda rastgele noktalar (cember veya dikdortgen kenar)
- [ ] Taciz gruplari farkli yonlerden gelebilir
- [ ] `BarracksTrainingSystem` okcu spawn pozisyonu → kale merkezine yakin
- [ ] `GameManager.ApplyUpgrade` okcu spawn pozisyonu guncelle

### M-ISO.5 — 8 Yonlu Sprite Sistemi
- [x] `SpriteAnimation` component yorumu ve atlas layout dokumantasyonu guncellendi (15x32 grid)
- [x] Yon mapping tablosu: saat yonu E=0, SE=1, S=2, SW=3, W=4, NW=5, N=6, NE=7
- [x] `ZombieAnimationStateSystem` 8 yon hesaplama (dir = DirectionRow % 8, offset + dir)
- [x] `SpriteSheetAuthoring` default'lar guncellendi (Columns=15, Rows=32, FPS=10, FrameCount=15)
- [x] `SpriteAtlasGenerator` Editor tool yazildi (4 PNG → tek atlas birlestirme)
- [x] Character Creator'dan skeleton(zombie), archer, fire_mage spritesheet export edildi
- [x] Atlas'lar olusturuldu (skeleton_atlas, archer_atlas, fire_mage_atlas — Assets/Art/Atlases/)
- [x] Material + prefab guncellendi (scale prefab'dan okunuyor, hardcoded degil)
- [x] SPRITE_ANIMATION_ARCHITECTURE.md + SPRITE_ANIMATION_EDITOR_SETUP.md guncellendi
  - EDITOR_SETUP: Character Creator'da karakter olusturma + export adimlari, atlas tool kullanimi, prefab ayarlari

### M-ISO.6 — Y-Sorting (Derinlik Sıralama)
- [ ] Renderer2D Transparency Sort Mode = Custom Axis (0, 1, 0) — zaten yapildi
- [ ] ECS entity'ler icin sorting: LocalTransform.Position.y → SortingGroup veya material property
- [ ] Tilemap sorting layer'lari duzenle (izometrik derinlik)
- [ ] Test: yakin objeler onde, uzak objeler arkada gorunuyor mu

### M-ISO.7 — Mevcut Sistemler Uyumluluk Testi
- [ ] Fizik pipeline calisma kontrolu (collision, spatial hash, integrate — perspective-agnostic)
- [ ] Okcu hedefleme + ok hareketi (ArrowMoveSystem rotation acisi duzeltme)
- [ ] Kaynak sistemi (degisiklik yok — pure data)
- [ ] Bina uretim/nufus sistemleri (degisiklik yok — pure data)
- [ ] HUD gosterimi (degisiklik yok — UI overlay)
- [ ] Restart/GameOver akisi

### M-ISO.8 — MD Dokumantasyonu
- [ ] ISOMETRIC_TRANSITION_ARCHITECTURE.md yaz (yapilan tum degisiklikler)
- [ ] Mevcut tum md dosyalarini guncelle (izometrik referanslar)

---

## M2 — Savunma Derinligi

> **Hedef:** RTS birim kontrolu, buyucu sistemi, tuzaklar, kart sistemi.
> **Bagimliliklari:** M-ISO (izometrik grid + perimeter + 8 yon)

### M2.1 — RTS Birim Kontrolu
- [ ] `UnitComponents.cs` olustur
  - [ ] `MilitaryUnit` (UnitType enum: Archer/Wizard, IsSelected bool)
  - [ ] `UnitMovement` (TargetPosition, MoveSpeed, IsMoving, HasPath)
  - [ ] `UnitCombat` (Damage, AttackRange, AttackCooldown, AttackTimer)
  - [ ] `PathBuffer` (DynamicBuffer<int2> — A* waypoint listesi)
  - [ ] `Selectable` tag component
- [ ] `UnitSelectionSystem` olustur (MonoBehaviour veya hibrit)
  - [ ] Sol tikla → tek birim sec
  - [ ] Kutu cizme (drag select) → coklu sec
  - [ ] Secili birimlere gorsel indicator (highlight circle veya outline)
- [ ] `UnitCommandSystem` olustur
  - [ ] Sag tikla zemine → PathRequest olustur (hareket komutu)
  - [ ] Sag tikla dusmana → saldiri komutu (yaklasma + ates)
- [ ] `GridPathfindingSystem` olustur (Custom DOTS A*)
  - [ ] 8 yonlu komsu tarama (izometrik grid)
  - [ ] `_grid[,]` uzerinden engel okuma
  - [ ] Octile distance heuristik
  - [ ] [BurstCompile] IJobEntity
  - [ ] Path sonucu → DynamicBuffer<int2>
- [ ] `UnitMovementSystem` olustur
  - [ ] Waypoint listesini takip et
  - [ ] Sonraki waypoint'e yuru, ulasinca bir sonrakine gec
  - [ ] Path bitince dur
- [ ] Mevcut `ArcherShootSystem` guncelle
  - [ ] Sabit pozisyon → hareket eden birim
  - [ ] Menzildeki dusmana otomatik ates (idle davranis)
  - [ ] Oyuncu komutuyla oncelikli hedef
- [ ] Mevcut okcu spawn'u guncelle (sabit pozisyon → haritada serbest)
- [ ] UNIT_CONTROL_ARCHITECTURE.md + UNIT_CONTROL_EDITOR_SETUP.md yaz
  - EDITOR_SETUP: Selection indicator prefab, UI panel ayarlari, input binding, test adimlari
- [ ] PATHFINDING_ARCHITECTURE.md + PATHFINDING_EDITOR_SETUP.md yaz
  - EDITOR_SETUP: Grid boyut ayarlari, debug goruntuleme (path cizgisi), performans test

### M2.2 — Buyucu Sistemi
- [ ] `WizardComponents.cs` olustur
  - [ ] `WizardUnit` (SpellDamage, AoERadius, SpellCooldown, SpellTimer, Range)
  - [ ] `WizardTrainer` (TrainingDuration, TrainingTimer, TrainingCost — kaynak maliyeti sonra belirlenir)
  - [ ] `WizardTowerUnit` (Damage, AoERadius, Range, Cooldown, CooldownTimer)
  - [ ] `FireballProjectile` (Damage, AoERadius, StartPos, TargetPos, FlightDuration, FlightTimer)
- [ ] `BuildingType.WizardAcademy` enum degerini ekle
- [ ] `WizardAcademyTrainingSystem` olustur
  - [ ] Nufustan buyucu egitir
  - [ ] Kaynak harcar (maliyet sonra belirlenecek)
  - [ ] Egitim suresi + timer → ECB ile buyucu entity spawn
  - [ ] BarracksTrainingSystem pattern'ini takip et
- [ ] `WizardShootSystem` olustur
  - [ ] Menzildeki en yakin zombie grubuna ates topu at
  - [ ] Cooldown tabanli (ok tuketimi yok)
  - [ ] Kucuk AoE radius
  - [ ] Fireball entity spawn → hareket → isabet → AoE hasar
- [ ] `FireballMoveSystem` olustur (ArrowMoveSystem benzeri, parabolik veya duz)
- [ ] `FireballHitSystem` olustur (spatial hash ile AoE hasar)
- [ ] `WizardTowerShootSystem` olustur
  - [ ] Buyucu Kulesi bina entity'si otomatik ates eder
  - [ ] Genis AoE, uzun menzil, yavas cooldown
  - [ ] Ayri fireball entity spawn
- [ ] Buyucu Akademisi SO asset olustur (BuildingConfigSO)
- [ ] Buyucu Kulesi SO asset olustur (BuildingConfigSO, Demirciden unlock)
- [ ] PopulationState'e Wizards field ekle
- [ ] HUD nufus gosterimini guncelle: "X isci, Y okcu, Z buyucu, W bos"
- [ ] Character Creator'dan buyucu spritesheet export et ve import et
- [ ] WIZARD_SYSTEM_ARCHITECTURE.md + WIZARD_SYSTEM_EDITOR_SETUP.md yaz
  - EDITOR_SETUP: Buyucu Akademisi + Buyucu Kulesi SO asset olusturma, prefab ayarlari, Character Creator export adimlari, HUD'a buyucu sayisi ekleme, test senaryolari
- [ ] WIZARD_COMPONENTS_ARCHITECTURE.md + WIZARD_COMPONENTS_EDITOR_SETUP.md yaz

### M2.3 — Tuzak Sistemi
- [ ] `TrapComponents.cs` olustur
  - [ ] `TrapData`: TrapType (enum), Damage, SlowAmount, Durability, MaxDurability
  - [ ] `TrapType` enum: SpikedStakes, Trench, BearTrap
- [ ] `TrapTriggerSystem` olustur — zombi uzerinden gecince etki
  - [ ] **Sivri Kaziklar:** Hasar, kirilabilir (Ahsap)
  - [ ] **Hendek:** Yavaslatma, kalici (Tas)
  - [ ] **Ayi Tuzagi:** Hasar + durdurma, tek kullanimlik (Demir)
- [ ] Tuzak yerlestirme: sur disi grid alani (BuildingGridManager genisletme veya ayri grid)
- [ ] Tuzak SO asset'leri olustur
- [ ] Tuzak tile gorunumu (Fantasy Kingdom Tileset'ten)
- [ ] TRAP_SYSTEM_ARCHITECTURE.md + TRAP_SYSTEM_EDITOR_SETUP.md yaz
  - EDITOR_SETUP: Tuzak SO asset olusturma, tuzak tile atama, sur disi alan tanimlama, yerlestirme UI ayarlari

### M2.4 — Demirci Tech Tree
- [ ] Demirci upgrade sistemi olustur
  - [ ] Ok hasari artisi (okcu damage modifier)
  - [ ] Duvar guclendirme (sur maxHP artisi)
  - [ ] Buyucu Kulesi unlock
  - [ ] Tuzak unlock
- [ ] Demir tuketimi: her upgrade demir harcar
- [ ] Demirci UI paneli (upgrade listesi + maliyet gosterimi)
- [ ] BLACKSMITH_TECH_ARCHITECTURE.md + BLACKSMITH_TECH_EDITOR_SETUP.md yaz
  - EDITOR_SETUP: Demirci UI paneli olusturma (Canvas, Panel, Button listesi), upgrade SO asset'leri, maliyet ayarlari

### M2.5 — Level-Up Kart Sistemi (Roguelike)
- [ ] `GameManager.ApplyUpgrade` eski 3-buton sistemini kaldir
- [ ] `LevelUpUI` eski hardcoded butonlari kaldir
- [ ] `CardComponents.cs` olustur
  - [ ] `CardData` ScriptableObject: CardID, Name, Description, Icon, Category, Tier, Effect
  - [ ] `CardCategory` enum: Population, Archer, Wizard, Defense, Economy
- [ ] Kart havuzu sistemi olustur
  - [ ] Tum kartlar SO olarak tanimla
  - [ ] Havuzdan 3 kart cek (on kosullari kontrol et)
  - [ ] Secilen kartin tierini artir
- [ ] Kart etkileri uygulama:
  - [ ] **Nufus:** Multeciler (+X nufus)
  - [ ] **Okcu:** Ok hasari, atis hizi, coklu ok, menzil
  - [ ] **Buyucu:** Ates topu guclendirme, cooldown azaltma, menzil
  - [ ] **Savunma:** Duvar HP artisi, hendek, barikat
  - [ ] **Ekonomi:** Uretim hizi boost, verimli isciler
- [ ] XP esigi artan egri (Inspector'dan ayarlanabilir)
- [ ] CARD_SYSTEM_ARCHITECTURE.md + CARD_SYSTEM_EDITOR_SETUP.md yaz
  - EDITOR_SETUP: Kart SO asset olusturma (her kart icin), kart secim UI olusturma (3 kart paneli, ikon, baslik, aciklama, tier gosterimi), Canvas + Panel + Button hierarchy, animasyon ayarlari

### M2.6 — MD Dokumantasyonu
- [ ] Tum yeni component/system dosyalari icin ARCHITECTURE + EDITOR_SETUP md
- [ ] Mevcut md'leri guncelle (yeni sistemler, kaldirilan sistemler)
- [ ] SYSTEM_EXECUTION_ORDER_ARCHITECTURE.md tamamen yeniden yaz

---

## M3 — Gun + Wave Sistemi

> **Hedef:** 100 gun yapisi, gun dongusu, wave skalasyonu, taciz sistemi, final wave.
> **Bagimliliklari:** M2 (savunma derinligi)

### M3.1 — Gun Dongusu
- [ ] `DayCycleComponents.cs` olustur
  - [ ] `DayState` singleton: CurrentDay, TotalDays(100), DayTimer, DayDuration, IsWaveDay
- [ ] `DayCycleAuthoring` baker
- [ ] `DayCycleSystem` olustur (gun sayaci, gecis, wave gunu uzama)
- [ ] HUD gun sayaci gosterimi
- [ ] DAY_CYCLE_ARCHITECTURE.md + DAY_CYCLE_EDITOR_SETUP.md yaz
  - EDITOR_SETUP: DayCycle authoring Inspector ayarlari, HUD'a gun sayaci ekleme (TMP_Text, pozisyon, format)

### M3.2 — Wave Skalasyonu
- [ ] WaveSpawnSystem guncelle: StressTestMode → normal wave mode
- [ ] Gun bazli wave sikligi (5→3→2→1 gun aralik)
- [ ] 360° spawn (M-ISO.4'te yapildi, burada fine-tune)
- [ ] Zombi sayisi + HP gun numarasina gore olcekleme (Inspector egri)
- [ ] Wave config Editor Window'dan ayarlanabilir

### M3.3 — Taciz Sistemi
- [ ] Wave disi kucuk gruplar (farkli yonlerden)
- [ ] Taciz sikligi + kalabaligi gun ilerledikce artsin
- [ ] `HarassmentSpawnSystem` veya WaveSpawnSystem'e entegre

### M3.4 — Final Wave (Gun 100)
- [ ] 50.000 zombi (360° spawn)
- [ ] Performans testi
- [ ] Kazanma kosulu: final wave atlatilirsa → zafer ekrani

### M3.5 — Kazanma / Kaybetme Guncelleme
- [ ] Zafer ekrani olustur (100. gun basarisi)
- [ ] Game Over ekrani istatistik tablosu genislet
- [ ] VICTORY_GAMEOVER_EDITOR_SETUP.md yaz
  - EDITOR_SETUP: Zafer ekrani UI olusturma (Canvas, Panel, istatistik TMP_Text'leri), Game Over ekrani guncellemesi

### M3.6 — MD Dokumantasyonu
- [ ] DayCycle + Wave md'leri guncelle
- [ ] SYSTEM_EXECUTION_ORDER guncelle

---

## M4 — Event + Polish

> **Hedef:** Event sistemi, destruction VFX, gorsel canlilik, ses, UI/UX, denge.
> **Bagimliliklari:** M3

### M4.1 — Destruction VFX Sistemi
- [ ] `DestructionVisualSystem` olustur
  - [ ] Sur HP esikleri → DamageStage degisimi (%75, %50, %25, %0)
  - [ ] Tile swap tetikleme (saglam → catlamis → kirik → harabe → enkaz)
  - [ ] Impact VFX (Fantasy Kingdom Tileset destruction animasyonlari)
  - [ ] Color flash (beyaz→kirmizi→normal)
  - [ ] Screen shake (Perlin noise, tile bazinda)
  - [ ] Enkaz kalintisi (BrokenObjects tilemap'e rubble tile)
- [ ] DESTRUCTION_VFX_ARCHITECTURE.md + DESTRUCTION_VFX_EDITOR_SETUP.md yaz
  - EDITOR_SETUP: Destruction tile asset'leri atama, VFX prefab olusturma, tilemap layer ayarlari (BrokenObjects), shake parametreleri

### M4.2 — Event Sistemi
- [ ] `EventComponents.cs` olustur (EventData SO)
- [ ] `EventSystem` olustur (gun basinda rastgele tetikleme)
- [ ] Olumlu/olumsuz/secimli eventler
- [ ] Event popup UI
- [ ] EVENT_SYSTEM_ARCHITECTURE.md + EVENT_SYSTEM_EDITOR_SETUP.md yaz
  - EDITOR_SETUP: Event SO asset olusturma, popup UI (Canvas, Panel, butonlar, ikon), event havuzu ayarlari

### M4.3 — Gorsel Canlilik (Ambient)
- [ ] Ambient koylu entity'leri (kasaba icinde dolasan)
- [ ] Isciler binalar arasinda yuruyen (gorsel)
- [ ] Atmosfer kontrasti (sakin gun vs wave kaos)

### M4.4 — Ses Tasarimi
- [ ] AudioManager altyapisi
- [ ] Zombie surusu, ok atisi, ates topu, sur vurma, duvar catlama/kirilma
- [ ] Birim secim/komut sesleri
- [ ] Level atlama fanfari, event bildirimi
- [ ] Kasaba ambiyansi, dinamik muzik

### M4.5 — UI/UX Polish
- [ ] Ana HUD yeniden tasarim (GDD Section 14)
- [ ] RTS kontrol paneli cilalamasi
- [ ] Bina menusu cilalamasi
- [ ] Kart secim UI animasyonlari
- [ ] Minimap
- [ ] UI_POLISH_EDITOR_SETUP.md yaz
  - EDITOR_SETUP: Tum UI elementlerinin detayli kurulumu (anchor, pozisyon, boyut, font, renk)

### M4.6 — Denge Ayarlari
- [ ] Custom Editor Window (GDD Section 16)
  - [ ] Wave, kaynak, bina, nufus, zombi, kart, event, savunma, sur, birim ayarlari
- [ ] Playtest ve iterasyon

### M4.7 — MD Dokumantasyonu
- [ ] Tum md'lerin final guncellemesi

---

## M5 — Launch

> **Hedef:** Steam hazirligi, lokalizasyon, son test.
> **Bagimliliklari:** M4

### M5.1 — Steam Entegrasyonu
- [ ] Steamworks SDK
- [ ] Steam Cloud save
- [ ] Achievement tanimlari
- [ ] Store page materyalleri

### M5.2 — Lokalizasyon
- [ ] Lokalizasyon altyapisi
- [ ] Turkce (TR) + Ingilizce (EN)
- [ ] Dil secme menusu

### M5.3 — Son Test ve Optimizasyon
- [ ] 50.000 zombi performans testi (hedef: 30-40 FPS)
- [ ] Bellek sizintisi kontrolu
- [ ] 100 gunluk tam run testi
- [ ] Farkli build path denge testi
- [ ] Edge case testleri

### M5.4 — MD Dokumantasyonu
- [ ] Tum md dosyalarinin final revizyonu

---

## Faz 2 — Post-Launch Genisleme (They Are Billions Seviyesi)

> Bu bolum post-launch vizyondur. Detaylar gelistirme sirasinda netlesecektir.

- [ ] Harita buyutme (32x32 → 64x64+)
- [ ] Yeni bina turleri
- [ ] Ek buyucu elementleri (Buz, Simsek)
- [ ] Survari birimleri (mount sistemi — Character Creator'da mevcut)
- [ ] Derin tech tree (Demirci genisletme)
- [ ] Daha fazla zombi cesitliligi
- [ ] Multiplayer/co-op (cok uzun vadeli)

---

## Ilerleme Ozeti

| Milestone | Toplam Gorev | Tamamlanan | Yuzde |
|-----------|-------------|------------|-------|
| M0 Prototype + Bug Fix | 5 | 5 | %100 |
| M1 Kaynak + Bina | ~50 | 45 | %90 |
| M-CLN Temizlik | ~15 | 13 | %87 |
| M-ISO Izometrik Gecis | ~25 | 4 | %16 |
| M2 Savunma Derinligi | ~45 | 0 | %0 |
| M3 Gun + Wave | ~20 | 0 | %0 |
| M4 Event + Polish | ~30 | 0 | %0 |
| M5 Launch | ~12 | 0 | %0 |
| **TOPLAM** | **~202** | **50** | **~%25** |

---

## Ilerleme Sirasi

```
1. M0 Bug Fix          ✅ TAMAMLANDI
2. M1 Kaynak + Bina    ✅ %90 TAMAMLANDI
3. M-CLN Temizlik      ✅ %87 TAMAMLANDI (Editor isler kaldi)
4. M-ISO Izometrik     🔧 DEVAM EDIYOR — M-ISO.5 sprite sistemi
5. M2 Savunma          ⬜ — RTS kontrol, buyucu, tuzak, kart
6. M3 Gun + Wave       ⬜ — 100 gun, wave skalasyonu, 50K final
7. M4 Event + Polish   ⬜ — event, destruction VFX, ses, UI, denge
8. M5 Launch           ⬜ — Steam, lokalizasyon, test
```

---

*Son guncelleme: 2026-03-18*
*GDD Referans: DEAD_WALLS_GDD_v4.0.md*
