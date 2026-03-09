# Sprite Animation — Unity Editor Kurulum

## 1. Texture Import Ayarlari

Her atlas PNG icin (Vampire_atlas, Archer_atlas, Redcap_atlas):

1. Project panelinde PNG dosyasini sec
2. Inspector'da:
   - **Texture Type:** Default (Sprite degil! Shader UV ile okuyor)
   - **Filter Mode:** Point (no filter)  ← KRITIK! Pixel art icin
   - **Compression:** None  ← piksel bozulmasi onlenir
   - **Max Size:** 1024 (atlas boyutlari kucuk, yeterli)
   - **Read/Write:** Kapali kalabilir
3. Apply

Arrow.png icin (Assets/Art/Projectiles/Arrow.png):
   - Ayni ayarlar (Default, Point, None)

---

## 2. Material Olusturma

Materyaller `Assets/Materials/` klasorunde.

### ZombieMat:
1. Assets/Materials/ZombieMat.mat sec
2. Shader: **DeadWalls/SpriteSheet**
3. **Sprite Sheet:** `Vampire_atlas.png` ata (Assets/Art/Sprites/)
4. **Alpha Cutoff:** 0.5
5. **Tint:** Beyaz (1,1,1,1)

### ArcherMat:
1. Assets/Materials/ArcherMat.mat sec
2. Shader: **DeadWalls/SpriteSheet**
3. **Sprite Sheet:** `Archer_atlas.png` ata (Assets/Art/Sprites/)
4. **Alpha Cutoff:** 0.5

### ArrowMat:
1. Assets/Materials/ArrowMat.mat sec
2. Shader: **DeadWalls/SpriteSheet**
3. **Sprite Sheet:** `Arrow.png` ata (Assets/Art/Projectiles/)
4. **Alpha Cutoff:** 0.5

---

## 3. Prefab Ayarlari

### Zombie Prefab:
1. Assets/Prefabs/Zombie.prefab ac
2. **MeshFilter:** Mesh → **Quad** (Cube degil!)
3. **MeshRenderer:** Material → ZombieMat
4. **SpriteSheetAuthoring** component ekle:
   - Columns: **4**
   - Rows: **12**  ← Atlas: 4 walk + 4 hit + 4 die
   - FPS: **7**
   - Direction Row: **1** (Left — zombi sola yurur)
   - Frame Count: **4** (walk frame sayisi)
5. Apply

### Archer Prefab:
1. Assets/Prefabs/Archer.prefab ac
2. MeshFilter → **Quad**
3. MeshRenderer → ArcherMat
4. **SpriteSheetAuthoring** component ekle:
   - Columns: **4**
   - Rows: **8**  ← Atlas: 4 idle + 4 move
   - FPS: **5** (idle daha yavas)
   - Direction Row: **2** (Right — okcu saga bakar)
   - Frame Count: **4**
5. Apply

### Arrow Prefab:
1. Assets/Prefabs/Arrow.prefab ac
2. MeshFilter → **Quad**
3. MeshRenderer → ArrowMat
4. **SpriteSheetAuthoring** component ekle:
   - Columns: **1**
   - Rows: **1**
   - FPS: **1** (statik)
   - Direction Row: **0**
   - Frame Count: **1**
5. Apply

---

## 4. Atlas Referans Tablosu

### Vampire_atlas.png (4 col x 12 row, 192x864 px)
| Satir | Animasyon | Frame Sayisi | Kullanim |
|-------|-----------|-------------|----------|
| 0-3   | Walk      | 4/yon       | Moving state |
| 4-7   | Hit       | 2/yon       | Attacking state |
| 8-11  | Die       | 1/yon       | Dead state |

Yon satirlari (her animasyon blogu icinde):
- +0 = Down, +1 = Left, +2 = Right, +3 = Up

Zombi sola bakar → base row = 1
- Walk Left = Row 1, Hit Left = Row 5, Die Left = Row 9

### Archer_atlas.png (4 col x 8 row, 192x384 px)
| Satir | Animasyon | Frame Sayisi |
|-------|-----------|-------------|
| 0-3   | Idle      | 4/yon       |
| 4-7   | Move      | 4/yon       |

### Redcap_atlas.png (4 col x 8 row, 192x384 px)
| Satir | Animasyon | Frame Sayisi |
|-------|-----------|-------------|
| 0-3   | Idle      | 4/yon       |
| 4-7   | Walk      | 4/yon       |

### Arrow.png (tek sprite, 42x9 px)
Animasyon yok, statik.

---

## 5. Yeni Karakter Ekleme — Adim Adim

Yeni bir entity tipi (ornegin Boss, NPC, Pet vb.) eklerken:

### 5.1. Sprite Sheet'leri Hazirla
- Her animasyon blogu ayri PNG olarak hazirlanmali
- Tum PNG'ler ayni sutun sayisinda (genelde 4)
- Her 4 satir bir animasyon: Down(+0), Left(+1), Right(+2), Up(+3)

### 5.2. Atlas Olustur (Python)
```bash
pip install Pillow
python create_atlas.py
```
Animasyon bloklarini **alt alta** birlestir. Sirasi onemli:
- En temel animasyon en ustte (Row 0-3)
- Sonraki bloklar alta eklenir (Row 4-7, 8-11, ...)

### 5.3. Unity'de Texture Import
1. Atlas PNG'yi `Assets/Art/Sprites/` altina koy
2. Inspector:
   - Texture Type: **Default**
   - Filter Mode: **Point (no filter)**
   - Compression: **None**
3. Apply

### 5.4. Material Olustur
1. Assets/Materials/ altinda yeni material olustur
2. Shader: **DeadWalls/SpriteSheet**
3. Sprite Sheet: atlas PNG'yi ata
4. Alpha Cutoff: 0.5, Tint: beyaz

### 5.5. Prefab Ayarla
1. Prefab'daki MeshFilter → **Quad**
2. MeshRenderer → yeni material
3. **SpriteSheetAuthoring** component ekle
4. Rows = atlas **toplam** satir sayisi (ornegin 2 blok = 8, 3 blok = 12)
5. FrameCount = varsayilan animasyondaki frame sayisi

### 5.6. (Gerekirse) State Animasyon Sistemi
Birden fazla animasyon blogu varsa, state degisimlerinde DirectionRow'u
degistiren bir system yaz. Ornek: `ZombieAnimationStateSystem.cs`

---

## 6. Test

1. Play'e bas
2. Zombiler → walk animasyonu (sola yurume)
3. Zombi duvara ulasinca → hit animasyonu (saldiri)
4. Zombi olunce → die sprite 0.5sn → kaybolur
5. Okcular → idle animasyonu
6. Oklar → statik sprite olarak ucmeli

---

## 7. Sorun Giderme

| Sorun | Cozum |
|-------|-------|
| Entity tamamen gorunmuyor | Shader Tags kontrol: `Opaque`/`Geometry` olmali. `TransparentCutout`/`AlphaTest` kullanilMAmali. LightMode tag'i olmamali. |
| Pembe/Magenta gorunuyor | Shader compile hatasi — Console kontrol et |
| Sprite bulanik | Texture Filter Mode → Point |
| Seffaf kisimlar siyah | Shader `DeadWalls/SpriteSheet` mi? Alpha Cutoff 0.5 mi? |
| Animasyon cok hizli/yavas | Prefab FPS degerini ayarla |
| Sprite ters gorunuyor | DirectionRow degistir (1=Left, 2=Right) |
| Entity gorunmuyor | Quad mesh atanmis mi? Z=-1 mi? |
| Olum animasyonu yok | Rows=12 mi kontrol et, atlas Vampire_atlas.png mi? |
| Yanlis frame gorunuyor | Columns ve Rows degerlerini atlas tablosuyla karsilastir |
| "Registering material null" hatasi | Shader tag'larini kontrol et (yukaridaki Sorun #1) |
| Material Inspector'da onizleme bos | Normal, DOTS shader'larinda olabilir — Play modunda test et |

### Shader Debug Yontemi
Entity'ler gorunmuyorsa, asagidaki siralamayla test et:

1. **Kirmizi test:** Shader'in frag fonksiyonunu `return half4(1,0,0,1);` yap → entity gorunuyorsa shader altyapisi dogru
2. **Texture test:** `return SAMPLE_TEXTURE2D(...)` ekle → texture okunuyorsa atlas dogru
3. **UV + clip test:** `_UVRect` ve `clip()` ekle → tam calisan shader

Her adimda entity'yi kontrol et. Gorunmeyen adimda sorun o katidir.

---

## 8. Kritik Shader Kurallari (DOTS Entities Graphics)

Bu kurallar debug sirasinda kesfedildi. **UYULMAZSA ENTITY'LER GORUNMEZ:**

```
DOGRU:                              YANLIS:
─────────────────────────────────   ─────────────────────────────────
"RenderType" = "Opaque"             "RenderType" = "TransparentCutout"
"Queue" = "Geometry"                "Queue" = "AlphaTest"
LightMode tag'i YOK                 "LightMode" = "UniversalForward"
Tek Pass                            Ekstra DepthOnly pass
#pragma target 4.5                  Daha dusuk target
#pragma multi_compile_instancing    Eksik
#pragma multi_compile _ DOTS_...    Eksik
```

Alpha seffaflik icin `clip(col.a - _Cutoff)` kullan ama render queue
**Geometry** olarak kalsin. `AlphaTest` veya `TransparentCutout` KULLANMA.
