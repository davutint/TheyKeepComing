# Zombie Navigation System — Mimari (Crowd Extension)

## Genel Bakis

Zombilerin duvara yururken birbirinin icine girmesini ve ust uste yigilmasini onleyen
navigasyon sistemi. **Agents Navigation Crowd Extension** (ProjectDawn) kullanir.

Onceki yaklasimda sadece `AgentSeparation` kullaniliyordu. Separation kuvveti,
normalized seeking kuvvetine karsi kuyruk derinligi olusturamiyordu — zombiler
duvarda ince bir cizgi olusturuyordu.

Yeni sistem **Density Field** + **Flow Field** kullanir:
- Her frame tum agent'lar pozisyonlarini grid'e "splat" eder (density)
- Yogun hucreler pahali hale gelir (CostWeights.Time = 0.8)
- Flow field, yeni gelen zombileri yogunlugu dusuk alanlara yonlendirir
- **Dogal kalabalik** olusur: duvarda yer kalmayinca zombiler yanlara yayilir
- **Burst-compiled**, performansli

---

## Neden Bu Yaklasim?

| Alternatif | Neden Secilmedi |
|------------|-----------------|
| Sadece AgentSeparation | Soft itme, seeking kuvvetine karsi kuyruk derinligi olusturamaz |
| AgentCollider | 4 iterasyon × spatial rebuild/frame — 10K+ entity'de performans krizi |
| Manuel separation kodu | `Velocity = Force * Speed` hem seeking hem separation'i esit olcekler |
| Hiz azaltma (Attacking state) | Speed hem seeking hem separation'i olcekler — oran degismez |

**Secilen yaklasim:** Crowd Extension — Density Field + Flow Field (pathfinding OLMADAN)

---

## Agents Navigation Crowds Entegrasyonu

### Paket Bilgileri
- **Base:** com.projectdawn.navigation (ProjectDawn)
- **Crowd:** com.projectdawn.navigation.crowds (ProjectDawn)
- **Core:** ProjectDawn.ContinuumCrowds (algoritma)
- **Assembly'ler:** Crowd dosyalari `.asmref` ile ProjectDawn.Navigation'a eklenir
  → DeadWalls.asmdef'te EK referans gerekmez

### Crowd Pipeline (Her Frame)

```
AgentPathingSystemGroup icinde:
  1. CrowdWorldSystem
     a. ClearDensity — onceki frame'in density'sini temizle
     b. SplatDensity — tum agent'larin pozisyon+velocity'sini grid'e yaz
     c. NormalizeAverageVelocityField
  2. CrowdGoalSystem — GoalSource=AgentDestination ise, agent Destination'larindan goal topla
  3. CrowdFlowSystem
     a. RecalculateSpeedAndCostField — density + slope → maliyet
     b. RecalculatePotentialField — Dijkstra-benzeri → goal'e en ucuz yol
  4. CrowdSteeringSystem — Flow field'dan velocity sample → AgentBody.Force set

AgentLocomotionSystem (SimulationSystemGroup veya FixedStep):
  → Force → Velocity → Position
```

### Bizim Sistemlerle Entegrasyon Sirasi

```
AgentPathingSystemGroup (FixedStep veya Regular*)
  → Crowd pipeline: density splat → flow hesapla → steering

SimulationSystemGroup
  1. WaveSpawnSystem (OrderFirst)     → Spawn + AgentBody + AgentCrowdPath set
  2. ArrowHitSystem                   → Ok hasari
  3. ZombieDeathSystem                → HP <= 0 → Dead state
  4. ZombieNavigationSystem           → Moving→Attacking, Dead→stop
  5. ZombieAttackSystem               → Saldiri hasari
  6. ZombieAnimationStateSystem       → Animasyon guncelleme
  7. DamageCleanupSystem              → DeathTimer countdown → destroy

PresentationSystemGroup
  → SpriteAnimationSystem            → UV rect hesaplama

* AGENTS_NAVIGATION_REGULAR_UPDATE define'i aktifse SimulationSystemGroup'a tasinir
  (FixedStep death spiral'i onlemek icin — AKTIF OLMALI)
```

---

## Component'lar

### Crowd Extension'dan (Scene'de Entity olarak olusturulur)

| Component | Kaynak | Gorev |
|-----------|--------|-------|
| CrowdSurface | CrowdSurfaceAuthoring | Grid boyutu, hucre sayisi, density ayarlari |
| CrowdSurfaceWorld | Otomatik (cleanup) | Runtime crowd world verisi |
| CrowdGroup | CrowdGroupAuthoring | Goal source, cost weights, speed, grounded |
| CrowdGroupFlow | Otomatik (cleanup) | Runtime flow field verisi |

### Agent'larda (Prefab + Runtime)

| Component | Kaynak | Gorev |
|-----------|--------|-------|
| Agent | AgentAuthoring (prefab) | Tag — "bu entity bir agent" |
| AgentBody | AgentAuthoring (prefab) | Destination, Force, Velocity, IsStopped |
| AgentLocomotion | AgentAuthoring (prefab) | Speed, Acceleration, StoppingDistance |
| AgentShape | AgentCircleShapeAuthoring (prefab) | Fiziksel boyut, Circle tipi (2D) |
| AgentCrowdPath | WaveSpawnSystem (runtime) | Shared comp — hangi CrowdGroup'a ait |

### Bizim Component'lar (Degismedi)

| Component | Gorev |
|-----------|-------|
| ZombieTag | Zombi entity isareti |
| ZombieState | Moving / Attacking / Dead |
| ZombieStats | HP, damage, speed, reward |
| ZombieCrowdGroupTag | CrowdGroup entity'sini bulmak icin singleton tag |
| DeathTimer | Olum animasyonu suresi |

---

## Sistem Detaylari

### ZombieNavigationSystem

Dosya: `Assets/Scripts/ECS/Systems/ZombieNavigationSystem.cs`

**Ne yapar:**
- `SimulationSystemGroup` icinde, `WaveSpawnSystem`'den sonra calisir
- `WithNone<DeathTimer>()` — zaten olen zombileri atlar

**Moving state:**
- Pozisyon kontrolu: `position.x <= wallX + 0.3f`
- Evet → `ZombieState = Attacking`, `IsStopped = true`, `Velocity = 0`
- Hayir → bir sey yapma (crowd steering + locomotion hareket ettiriyor)

**Attacking state:**
- `IsStopped = true` zorla (crowd steering Force set etse bile locomotion atlar)
- Duvar bariyeri: `position.x < wallX` → `position.x = wallX`
- Density hala splat edilir (SplatterDensityJob query'si IsStopped filtrelemez)
  → durmus zombiler density olusturur → yeni gelenler baska yere yonlendirilir

**Dead state:**
- `IsStopped = true`, `Velocity = 0`
- DeathTimer eklenince query'den duser
- 0.5sn sonra entity destroy edilir

### WaveSpawnSystem

Dosya: `Assets/Scripts/ECS/Systems/WaveSpawnSystem.cs`

**Eklenen:**
- `RequireForUpdate<ZombieCrowdGroupTag>()` — CrowdGroup entity olmadan calisma
- `GetSingletonEntity<ZombieCrowdGroupTag>()` — CrowdGroup entity'sini bul
- `ecb.AddSharedComponent(zombie, new AgentCrowdPath { Group = crowdGroupEntity })`
  → Her zombiyi CrowdGroup'a bagla (flow field routing baslasin)

**Prefab referans sorunu:** Prefab'lar sahne nesnesini referans alamaz.
Bu yuzden AgentCrowdPathingAuthoring prefab'a EKLENMEZ.
Runtime'da WaveSpawnSystem, ZombieCrowdGroupTag singleton ile
CrowdGroup entity'sini bulur ve AddSharedComponent ile baglar.

---

## Veri Akisi

```
Spawn:
  WaveSpawnSystem → AgentBody.Destination = (wallX, spawnY, -1)
                  → AgentCrowdPath.Group = crowdGroupEntity
                  → IsStopped = false

Crowd Pipeline (her frame):
  CrowdWorldSystem → Tum agent'larin density'sini grid'e splat et
  CrowdGoalSystem → Agent Destination'larindan goal topla
  CrowdFlowSystem → Density + goal → maliyet → potansiyel → flow field
  CrowdSteeringSystem → Flow field'dan velocity sample → AgentBody.Force

Hareket:
  AgentLocomotionSystem → Force → Velocity → Position

Varil:
  ZombieNavigationSystem → position.x <= wallX+0.3 → Attacking + IsStopped=true

Kalabalik Yayilma:
  Durmus zombi density splat eder → hucre "pahali" olur
  → Flow field yeni zombileri daha bos hucrelere yonlendirir
  → Zombiler dogal olarak yanlara yayilir

Olum:
  ZombieDeathSystem → HP <= 0 → Dead
  ZombieNavigationSystem → IsStopped=true, Velocity=0
  ZombieAnimationStateSystem → die animasyonu + DeathTimer
  DamageCleanupSystem → DeathTimer → destroy
```

---

## CrowdSurface Grid Kurulumu

Grid, zombilerin hareket ettigi alayi kapsamali:

```
Zombi spawn: X=28~33, Y=-12~12
Duvar X: 4.76
Grid pozisyonu: (4.6, -15, 0)  ← sol-alt kose (Position = kose, merkez degil!)
Grid boyutu: 30x30 unit        ← X=4.6~34.6, Y=-15~15
Hucre sayisi: 30x30            ← her hucre ~1x1 unit
```

### Neden 30x30 Hucre?
- Cok az hucre (10x10) → dusuk cozunurluk, zombiler genis bloklarda hareket eder
- Cok fazla hucre (100x100) → performans maliyeti artar (O(width*height) flow hesaplama)
- 30x30 iyi bir denge: ~1m² hucre boyutu, zombi scale=0.3 icin yeterli hassasiyet

---

## CostWeights Aciklama

```
CostWeights(Distance=0.2, Time=0.8, Discomfort=0.0)
```

| Agirlik | Anlam |
|---------|-------|
| Distance=0.2 | Mesafe maliyeti dusuk — zombiler kisa yolu cok az onemsyor |
| Time=0.8 | Zaman maliyeti yuksek — yogun alanlarda hiz duser → pahali olur |
| Discomfort=0 | Kullanilmiyor (obstacle ile engel eklenmedi) |

**Sonuc:** Zombiler yogun alanlardan kacınır cunku oralarda "zaman maliyeti" yuksek.
Bu da dogal kalabalik yayilmasini saglar.

---

## Density Splatting Detayi

SplatterDensityJob query'si:
```csharp
Execute(in Agent, in AgentBody, in AgentShape, in LocalTransform)
```

- **Filtre YOK** — tum agent'lar (moving, attacking, dead) density splat eder
- Durmus zombi (Velocity=0) → pozisyon-bazli density hala eklenir
- Bu ISTENEN davranis: duvardaki durmus zombiler density olusturur
  → flow field yeni zombileri bos alanlara yonlendirir

---

## Onemli Ayarlar

| Ayar | Deger | Konum | Neden |
|------|-------|-------|-------|
| AGENTS_NAVIGATION_REGULAR_UPDATE | AKTIF | Player Settings → Scripting Define Symbols | FixedStep death spiral onleme |
| CrowdSurface.Size | 30x30 | CrowdSurface entity | Spawn-duvar arasini kapsar |
| CrowdSurface.Width/Height | 30/30 | CrowdSurface entity | ~1m² hucre boyutu |
| CostWeights.Distance | 0.2 | CrowdGroup entity | Mesafe az onemli |
| CostWeights.Time | 0.8 | CrowdGroup entity | Yogunluk cok onemli |
| GoalSource | AgentDestination | CrowdGroup entity | Her zombi kendi Destination'ina gider |
| Grounded | false | CrowdGroup entity | 2D oyun — Z snapping olmasin |
| AgentShape.Radius | 0.2 | Zombie prefab | Density splat boyutu |
| AngularSpeed | 0 | Zombie prefab | Sprite rotation korunur |
| StoppingDistance | 0.5 | Zombie prefab | Duvara yakin durur |
| Density.Min | 0.32 | CrowdSurface entity | Density etkisi baslama esigi |
| Density.Max | 1.6 | CrowdSurface entity | Tam yogunluk esigi |

---

## Dosya Haritasi

```
Assets/Scripts/
├── DeadWalls.asmdef                          ← Degismedi (crowd .asmref ile dahil)
├── ECS/
│   ├── Components/
│   │   └── ZombieComponents.cs               ← ZombieCrowdGroupTag eklendi
│   ├── Authoring/
│   │   ├── ZombieAuthoring.cs                ← Degismedi
│   │   └── ZombieCrowdGroupAuthoring.cs      ← YENI (CrowdGroup tag)
│   └── Systems/
│       ├── ZombieNavigationSystem.cs         ← GUNCELLENDI (crowd yaklasimi)
│       ├── WaveSpawnSystem.cs                ← GUNCELLENDI (AgentCrowdPath ekleme)
│       ├── ZombieAttackSystem.cs             ← Degismedi
│       ├── ZombieDeathSystem.cs              ← Degismedi
│       ├── ZombieAnimationStateSystem.cs     ← Degismedi
│       ├── DamageCleanupSystem.cs            ← Degismedi
│       ├── SpriteAnimationSystem.cs          ← Degismedi
│       ├── ZOMBIE_NAVIGATION_ARCHITECTURE.md ← Bu dosya
│       └── ZOMBIE_NAVIGATION_EDITOR_SETUP.md ← Editor kurulum rehberi
```

Silinen/Degismeyen: `ZombieMoveSystem.cs` (onceden silinmisti)

---

## Prefab'dan Kaldirilan Component'lar

| Component | Neden Kaldirildi |
|-----------|-----------------|
| AgentSeparationAuthoring | Crowd density field ayni isi yapar, gereksiz |
| AgentColliderAuthoring | 4 iterasyon × spatial rebuild → performans krizi (onceden kaldirilmisti) |
