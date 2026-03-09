# Zombie Navigation (Crowd Extension) — Unity Editor Kurulum

## 1. Gerekli Asset'ler

- **Agents Navigation** (ProjectDawn) — base asset
- **Agents Navigation - Crowds** (ProjectDawn) — crowd extension
- Her ikisi de Package Manager'da yuklenmis olmali

## 2. Player Settings

**Edit → Project Settings → Player → Other Settings → Scripting Define Symbols:**

`AGENTS_NAVIGATION_REGULAR_UPDATE` eklenmis olmali.

> Bu olmadan agent system'leri FixedStepSimulationSystemGroup'ta calisir
> ve yuk altinda "death spiral" olusur (FPS duser → daha fazla fixed update → daha fazla duser).

---

## 3. Scene Kurulumu — CrowdSurface Entity

> **KRITIK:** CrowdSurface ve CrowdGroup GameObject'leri **SubScene icinde** olusturulmali!
> Diger oyun entity'lerinin (Castle, GameState vs.) bulundugu ayni SubScene'e ekle.
> ZombieCrowdGroupAuthoring Baker kullaniyor — Baker'lar sadece SubScene baking'de calisir.
> SubScene disina koyarsan ZombieCrowdGroupTag entity'si olusmaz → zombiler spawn olmaz!

### 3.1. Olusturma
1. SubScene'i ac (Hierarchy'de SubScene'e cift tikla)
2. SubScene icinde Right Click → **Create Empty**
3. Isim: `ZombieCrowdSurface`
4. Transform:
   - Position: **(4.6, -15, 0)**  ← grid'in sol-alt kosesi (duvar X=4.76 hizasi)
   - Rotation: **(0, 0, 0)**      ← identity (XY duzlemi icin dokunma!)
   - Scale: **(1, 1, 1)**

> **NOT:** Position, grid'in **kosesini** belirler (merkez degil!).
> Size (30,30) ile grid X=4.6~34.6, Y=-15~15 alanini kapsar.

### 3.2. Component Ekleme
1. Add Component → **Crowd Surface Authoring**
2. Ayarlar:
   - **Size:** (30, 30)          ← X=4~34, Y=-15~15 alanini kapsar
   - **Width:** 30               ← yatay hucre sayisi
   - **Height:** 30              ← dikey hucre sayisi
   - **Density Min:** 0.32       ← varsayilan (tuning gerekebilir)
   - **Density Max:** 1.6        ← varsayilan (tuning gerekebilir)
   - **Density Exponent:** 0.3   ← varsayilan

> **NOT:** CrowdData (ScriptableObject) GEREKMEZ — duz zemin, height field yok.
> Eger Inspector'da CrowdData alani varsa **bos birakin**.

---

## 4. Scene Kurulumu — CrowdGroup Entity

### 4.1. Olusturma
1. SubScene icinde Right Click → **Create Empty** (ayni SubScene!)
2. Isim: `ZombieCrowdGroup`
3. Transform: Varsayilan (0,0,0) — onemli degil

### 4.2. Component Ekleme

#### A) Crowd Group Authoring
1. Add Component → **Crowd Group Authoring**
2. Ayarlar:
   - **Surface:** `ZombieCrowdSurface` GameObject'ini surukleeee
   - **Goal Source:** AgentDestination
   - **Cost Weights:**
     - Distance: **0.2**
     - Time: **0.8**
     - Discomfort: **0.0**
     - Normalize: **true**
   - **Speed:**
     - Min: **0.1**
     - Max: **3.5**
   - **Grounded:** **false**  ← **KRITIK! 2D oyun — Z snapping olmasin**
   - **Mapping Radius:** **5.0**

#### B) Zombie Crowd Group Authoring (Bizim Tag)
1. Add Component → **Zombie Crowd Group Authoring**
   > Bu component, WaveSpawnSystem'in CrowdGroup entity'sini bulmasini saglar.
   > ZombieCrowdGroupTag singleton'i olusturur.

### 4.3. Kontrol Listesi

| Component | Var mi? |
|-----------|---------|
| **CrowdGroupAuthoring** | ← EKLE |
| **ZombieCrowdGroupAuthoring** | ← EKLE |

---

## 5. Zombie Prefab Guncelleme

### 5.1. Olmasi Gereken Component'lar

| Component | Durum |
|-----------|-------|
| MeshFilter (Quad) | ✓ Zaten var |
| MeshRenderer (ZombieMat) | ✓ Zaten var |
| SpriteSheetAuthoring | ✓ Zaten var |
| ZombieAuthoring | ✓ Zaten var |
| **AgentAuthoring** | ✓ Zaten var |
| **AgentCircleShapeAuthoring** | ✓ Zaten var |

### 5.2. AgentAuthoring Ayarlari (kontrol et)

- **Motion Type:** DefaultLocomotion
- **Speed:** 1.5 (WaveSpawnSystem override etmez — prefab degeri kullanilir)
- **Acceleration:** 8
- **Angular Speed:** **0**  ← KRITIK! Sprite rotation korunmali
- **Stopping Distance:** 0.5
- **Auto Breaking:** true

### 5.3. AgentCircleShapeAuthoring Ayarlari (kontrol et)

- **Radius:** 0.2

### 5.4. KALDIRILMASI Gereken Component'lar

| Component | Neden |
|-----------|-------|
| **AgentSeparationAuthoring** | Crowd density field ayni isi yapar, KALDIR |
| **AgentColliderAuthoring** | Performans sorunu, KALDIR (zaten kaldirilmis olmali) |

### 5.5. EKLENMEYECEK Component'lar

| Component | Neden |
|-----------|-------|
| AgentCrowdPathingAuthoring | Prefab sahne nesnesi referans alamaz — runtime'da eklenir |

> **ONEMLI:** AgentCrowdPath shared component, WaveSpawnSystem tarafindan
> runtime'da `ecb.AddSharedComponent()` ile eklenir. Prefab'a dokunma!

### 5.6. Apply prefab.

---

## 6. Test

### 6.1. Temel Test
1. Play'e bas
2. Zombiler spawn olur ve sola yurur
3. **Beklenen:** Zombiler duvara ulasinca durur
4. **Beklenen:** Yeni gelen zombiler, dolu alanlari bypass ederek bos alanlara yonelir
5. **Beklenen:** Duvarda dogal kalabalik olusur — zombiler yanlara yayilir

### 6.2. Stress Test
1. WaveConfig'de StressTestMode = true
2. 10K+ zombi spawn et
3. **Beklenen:** Zombiler duvarda genis bir alan kaplar, tek sira degil
4. FPS kontrol et

### 6.3. Olum Testi
1. Okcular zombileri oldursun
2. **Beklenen:** Olu zombi die animasyonu oynar (0.5sn)
3. **Beklenen:** Olu zombi density'si yeni gelenleri yonlendirir (kisa sure)
4. **Beklenen:** 0.5sn sonra entity yok edilir

---

## 7. Parametre Tuning Rehberi

### CostWeights (CrowdGroupAuthoring)

| Distance | Time | Etki |
|----------|------|------|
| 0.5 | 0.5 | Dengeli — mesafe ve yogunluk esit onemde |
| 0.2 | 0.8 | Yogunluk oncelikli (BASLANGIC) — zombiler yayilir |
| 0.0 | 1.0 | Sadece yogunluk — en yakin yolu tamamen gormezden gelir |
| 0.8 | 0.2 | Mesafe oncelikli — zombiler direkt duvara gider, az yayilir |

### CrowdSurface Grid (CrowdSurfaceAuthoring)

| Size | Width×Height | Hucre | Etki |
|------|-------------|-------|------|
| 30×30 | 15×15 | 2m² | Dusuk cozunurluk — genis bloklarda hareket |
| 30×30 | 30×30 | 1m² | Dengeli (BASLANGIC) |
| 30×30 | 60×60 | 0.5m² | Yuksek cozunurluk — hassas yayilma ama daha agir |

### Density (CrowdSurfaceAuthoring)

| Min | Max | Etki |
|-----|-----|------|
| 0.1 | 0.5 | Hassas — az yogunluk bile etkiler |
| 0.32 | 1.6 | Varsayilan (BASLANGIC) |
| 1.0 | 5.0 | Toleransli — cok yogunluk olmadan etki yok |

### Speed (CrowdGroupAuthoring)

| Min | Max | Etki |
|-----|-----|------|
| 0.1 | 3.5 | Varsayilan (BASLANGIC) |
| 0.1 | 2.0 | Daha yavas max — yogun alanlarda fark az |
| 0.5 | 5.0 | Daha hizli — yogun alanlarda bile hizli |

---

## 8. Sorun Giderme

| Sorun | Cozum |
|-------|-------|
| Zombiler hareket etmiyor | CrowdGroup → Surface bagli mi? GoalSource=AgentDestination mi? |
| Zombiler hala ust uste biniyor | CrowdSurface grid dogru alanyi kapsiyor mu? CostWeights.Time yukselt |
| Zombiler cok yavas yayiliyor | CostWeights.Time arttir (0.9), Density.Min dusur |
| Zombiler hedefe gitmiyor | Grounded=false mi? (true ise Z snapping yapar, 2D'de bozulur) |
| Performans dustu | Grid boyutu cok buyuk mu? 30×30 yeterli. 60×60 → flow hesaplama 4x |
| "Missing component" hatasi | CrowdSurface + CrowdGroup entity'leri scene'de mi? |
| Zombiler spawn olmuyor | ZombieCrowdGroupAuthoring, CrowdGroup entity'sine eklendi mi? |
| Zombiler spawn olmuyor (2) | CrowdSurface ve CrowdGroup **SubScene icinde** mi? Disindaysa Baker calismaz! |
| Sprite donuyor | AgentAuthoring → Angular Speed = 0 mi? |
| Death spiral (FPS cokus) | AGENTS_NAVIGATION_REGULAR_UPDATE define aktif mi? |
| Console'da "Group is null" | ZombieCrowdGroupTag singleton bulunamiyor — authoring kontrol et |
| Flow field gorsel yok | Scene view'da CrowdSurface Gizmo'sunu ac (Inspector'da) |
