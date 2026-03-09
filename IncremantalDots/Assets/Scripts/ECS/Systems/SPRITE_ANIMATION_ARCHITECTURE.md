# Sprite Animation System — Mimari

## Genel Bakis

DOTS/ECS ortaminda sprite sheet animasyonu icin per-instance GPU rendering sistemi.
50K+ entity'de tek draw call ile farkli animasyon frame'leri gosterebilir.
Texture Atlas yaklasimi ile tek material uzerinden birden fazla animasyon destekler.

---

## Temel Yaklasim

```
Quad Mesh + Custom Shader + Per-Instance UV Rect = Animasyon
```

Her entity bir **Quad** mesh kullanir. Shader, her entity icin farkli bir UV rect alir
ve atlas uzerindeki dogru frame'i gosterir. Entities Graphics paketi, per-instance
`_UVRect` degerini otomatik olarak GPU compute buffer'a yazar.

---

## Texture Atlas Yapisi

Her karakter tipi icin tum animasyonlar tek PNG'ye birlestirilmis.
Animasyon bloklari 4'er satir gruplar halinde (Down, Left, Right, Up).

### Vampire_atlas.png (4 col x 12 row, 192x864 px)
```
Row 0-3:  Walk (4 frame/yon)   ← Moving state
Row 4-7:  Hit  (2 frame/yon)   ← Attacking state
Row 8-11: Die  (1 frame/yon)   ← Dead state
```

### Archer_atlas.png (4 col x 8 row, 192x384 px)
```
Row 0-3: Idle (4 frame/yon)
Row 4-7: Move (4 frame/yon)
```

### Redcap_atlas.png (4 col x 8 row, 192x384 px)
```
Row 0-3: Idle (4 frame/yon)
Row 4-7: Walk (4 frame/yon)
```

### Arrow.png (tek sprite, 42x9 px)
Animasyon yok, statik.

### Yon Satirlari (her blok icinde)
```
+0 = Down
+1 = Left
+2 = Right
+3 = Up
```

---

## Atlas Olusturma (Python/Pillow)

Her karakter icin ayri sprite sheet PNG'leri (idle, walk, hit, die) tek atlas'a birlestirilir.

### Ornek: Vampire Atlas (walk + hit + die)

```python
from PIL import Image
import os

sprites_dir = r"C:\UnityProjeler\IncremantalDots\Assets\Art\Sprites"

walk = Image.open(os.path.join(sprites_dir, "Vampire_walk.png"))  # 4col x 4row
hit  = Image.open(os.path.join(sprites_dir, "Vampire_hit.png"))   # 4col x 4row
die  = Image.open(os.path.join(sprites_dir, "Vampire_die.png"))   # 4col x 4row

# Hepsi ayni genislikte olmali
w = walk.width
row_h = walk.height // 4  # tek satir yuksekligi

# Atlas: 4col x 12row (walk 4 + hit 4 + die 4)
atlas = Image.new("RGBA", (w, row_h * 12), (0, 0, 0, 0))
atlas.paste(walk, (0, 0))                    # Row 0-3: Walk
atlas.paste(hit,  (0, walk.height))          # Row 4-7: Hit
atlas.paste(die,  (0, walk.height + hit.height))  # Row 8-11: Die

atlas.save(os.path.join(sprites_dir, "Vampire_atlas.png"))
```

### Ornek: Archer/Redcap Atlas (idle + move)

```python
idle = Image.open(os.path.join(sprites_dir, "Archer_06_Idle.png"))  # 4col x 4row
move = Image.open(os.path.join(sprites_dir, "Archer_06_Move.png"))  # 4col x 4row

w = idle.width
row_h = idle.height // 4

atlas = Image.new("RGBA", (w, row_h * 8), (0, 0, 0, 0))
atlas.paste(idle, (0, 0))           # Row 0-3: Idle
atlas.paste(move, (0, idle.height)) # Row 4-7: Move

atlas.save(os.path.join(sprites_dir, "Archer_atlas.png"))
```

### Kural
- Tum kaynak sheet'ler ayni sutun sayisi ve satir basina ayni piksel yuksekliginde olmali
- Yon sirasi: Down(0), Left(1), Right(2), Up(3) — HER animasyon blogunda ayni
- Atlas'a ekleme sirasi: en temel animasyon en ustte (row 0-3), sonrakiler alta eklenir

---

## Bilesenler

### SpriteAnimation (IComponentData)
Dosya: `Assets/Scripts/ECS/Components/SpriteComponents.cs`

| Alan | Tip | Aciklama |
|------|-----|----------|
| TotalColumns | int | Grid sutun sayisi (genelde 4) |
| TotalRows | int | Grid satir sayisi (12 vampire, 8 diger) |
| DirectionRow | int | Aktif satir — atlas icinde **mutlak** index |
| FrameCount | int | Aktif animasyondaki frame sayisi |
| CurrentFrame | int | Su anki frame (0-based) |
| FrameTimer | float | Gecen sure (saniye) |
| FrameInterval | float | Frame basina sure (1/FPS) |

### SpriteUVRect (IComponentData + [MaterialProperty("_UVRect")])
Dosya: `Assets/Scripts/ECS/Components/SpriteComponents.cs`

GPU'ya per-instance gonderilen UV rect.
Value: `(offsetX, offsetY, scaleX, scaleY)`

Entities Graphics paketi, `[MaterialProperty]` attribute'u sayesinde bu component'in
degerini otomatik olarak GPU compute buffer'a yazar. Shader tarafinda `_UVRect`
olarak okunur.

### DeathTimer (IComponentData)
Dosya: `Assets/Scripts/ECS/Components/ZombieComponents.cs`

Olum animasyonu suresi. 0'a dusunce entity silinir.

---

## Sistemler

### SpriteAnimationSystem (PresentationSystemGroup)
Dosya: `Assets/Scripts/ECS/Systems/SpriteAnimationSystem.cs`

Her frame'de:
1. `FrameTimer += deltaTime`
2. Timer >= interval → `CurrentFrame++` (dongusel)
3. UV rect hesapla:
   - `col = CurrentFrame`
   - `uvRow = (TotalRows - 1) - DirectionRow` ← **Y-flip!**
   - `scaleX = 1/TotalColumns`, `scaleY = 1/TotalRows`
   - `offsetX = col * scaleX`, `offsetY = uvRow * scaleY`

**Y-flip neden gerekli?**
Sprite sheet'te Row 0 = resmin ustu, ama UV space'te y=0 = resmin alti.
Bu yuzden `uvRow = (TotalRows-1) - DirectionRow` ile ceviriyoruz.

### ZombieAnimationStateSystem (SimulationSystemGroup)
Dosya: `Assets/Scripts/ECS/Systems/ZombieAnimationStateSystem.cs`

ZombieState degisikligine gore animasyon degistirir:
- `Moving` → Walk row (baseRow), 4 frame
- `Attacking` → Hit row (baseRow + 4), 2 frame
- `Dead` → Die row (baseRow + 8), 1 frame + DeathTimer(0.5s) ekler

`baseRow = DirectionRow % 4` ile mevcut yon korunur.

### DamageCleanupSystem (SimulationSystemGroup)
Dosya: `Assets/Scripts/ECS/Systems/DamageCleanupSystem.cs`

DeathTimer countdown → 0'a dusunce destroy + Gold/XP odul.

---

## Veri Akisi

```
ZombieState degisir (Moving → Attacking → Dead)
  → ZombieAnimationStateSystem: DirectionRow + FrameCount guncelle
    → SpriteAnimationSystem: timer + UV rect hesapla
      → Entities Graphics: per-instance _UVRect → GPU compute buffer
        → Shader: _UVRect okur → dogru frame render
```

---

## Shader: DeadWalls/SpriteSheet

Dosya: `Assets/Shaders/SpriteSheet.shader`

### Ozellikler
- URP HLSL + Alpha Cutoff (clip)
- DOTS_INSTANCING_ON destegi
- Per-instance: `_UVRect` (float4), `_Color` (float4)
- UV transform: `uv = quad_uv * _UVRect.zw + _UVRect.xy`
- Cull Off (her iki yuzden gorunur)

### !!KRITIK!! Shader Kurallari (DOTS Entities Graphics ile)

Bu kurallar debug sirasinda kesfedildi. Uyulmazsa entity'ler GORUNMEZ.

| DOGRU | YANLIS | Sonuc |
|-------|--------|-------|
| `"RenderType" = "Opaque"` | `"RenderType" = "TransparentCutout"` | TransparentCutout → entity gorunmez |
| `"Queue" = "Geometry"` | `"Queue" = "AlphaTest"` | AlphaTest → entity gorunmez |
| LightMode tag'i OLMAMALI | `"LightMode" = "UniversalForward"` | LightMode → entity gorunmez |
| Tek Pass (sadece ana pass) | Ekstra DepthOnly pass | DepthOnly → entity gorunmez |
| `#pragma target 4.5` | Daha dusuk target | Compute buffer destegi yok |
| `#pragma multi_compile_instancing` | Eksik | Instancing calismaz |
| `#pragma multi_compile _ DOTS_INSTANCING_ON` | Eksik | Per-instance property calismaz |

**Ozet:** Entities Graphics, sadece `Opaque/Geometry` tag'li, `LightMode` tag'i olmayan,
tek pass'li shader'lari kabul eder. Alpha cutoff icin `clip()` fonksiyonu kullanilir
ama render queue olarak `Geometry` kalir.

### CBUFFER Hizalama
```hlsl
CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;   // 16 byte — float4 once gelmeli
    float4 _UVRect;       // 16 byte
    float4 _Color;        // 16 byte
    float _Cutoff;        // 4 byte  — float en sonda
CBUFFER_END
```
SRP Batcher uyumlulugu icin float4'ler float'lardan once gelmeli.

---

## Yeni Karakter Tipi Ekleme Rehberi

Yeni bir animasyonlu entity tipi eklemek icin asagidaki adimlari takip et:

### Adim 1 — Sprite Sheet'leri Hazirla
- Her animasyon ayri PNG (idle, walk, attack, die vb.)
- Hepsi ayni sutun sayisinda (genelde 4)
- Her satir bir yon: Down(0), Left(1), Right(2), Up(3)

### Adim 2 — Atlas Olustur (Python)
- `pip install Pillow`
- Animasyonlari alt alta birlestir (once idle → sonra walk → sonra attack...)
- `Assets/Art/Sprites/` altina `KarakterAdi_atlas.png` olarak kaydet

### Adim 3 — Unity Texture Import
- Texture Type: **Default** (Sprite degil!)
- Filter Mode: **Point (no filter)** — piksel art icin kritik
- Compression: **None**
- Max Size: **1024**

### Adim 4 — Material Olustur
- Shader: **DeadWalls/SpriteSheet**
- Sprite Sheet: atlas PNG'yi ata
- Alpha Cutoff: **0.5**
- Tint: **beyaz** (1,1,1,1)

### Adim 5 — Prefab Ayarla
- MeshFilter → **Quad**
- MeshRenderer → olusturulan material
- **SpriteSheetAuthoring** component ekle:
  - Columns: atlas sutun sayisi (genelde 4)
  - Rows: atlas **toplam** satir sayisi
  - FPS: animasyon hizi (5-10 arasi)
  - DirectionRow: baslangic yon satiri (1=Left, 2=Right)
  - FrameCount: varsayilan animasyondaki frame sayisi

### Adim 6 — (Opsiyonel) State-Based Animasyon Sistemi
Birden fazla animasyon blogu olan karakterler icin (walk→attack→die gibi),
`ZombieAnimationStateSystem` benzeri bir system yaz:

```csharp
// DirectionRow degistirerek animasyon blogunu sec
// baseRow = currentRow % 4 → yon korunur
// Walk: baseRow + 0, Attack: baseRow + 4, Die: baseRow + 8
anim.ValueRW.DirectionRow = baseRow + (animBlockIndex * 4);
anim.ValueRW.FrameCount = yeniFrameSayisi;
anim.ValueRW.CurrentFrame = 0;
anim.ValueRW.FrameTimer = 0f;
```

---

## Mevcut Entity Tipleri ve Ayarlari

| Entity | Atlas | Rows | Columns | Animasyonlar | Prefab FPS |
|--------|-------|------|---------|--------------|------------|
| Zombi | Vampire_atlas | 12 | 4 | Walk(4f) + Hit(2f) + Die(1f) | 7 |
| Okcu | Archer_atlas | 8 | 4 | Idle(4f) + Move(4f) | 5 |
| Koylu | Redcap_atlas | 8 | 4 | Idle(4f) + Walk(4f) | 5 |
| Ok | Arrow.png | 1 | 1 | Statik | 1 |

---

## Dosya Haritasi

```
Assets/
├── Shaders/
│   └── SpriteSheet.shader              ← Custom URP HLSL shader
├── Scripts/ECS/
│   ├── Components/
│   │   ├── SpriteComponents.cs         ← SpriteAnimation + SpriteUVRect
│   │   └── ZombieComponents.cs         ← DeathTimer
│   ├── Authoring/
│   │   └── SpriteSheetAuthoring.cs     ← Baker (MonoBehaviour → ECS)
│   └── Systems/
│       ├── SpriteAnimationSystem.cs    ← UV rect hesaplama (Burst)
│       ├── ZombieAnimationStateSystem.cs ← State→Animasyon (Burst)
│       └── DamageCleanupSystem.cs      ← DeathTimer countdown
├── Art/
│   ├── Sprites/
│   │   ├── Vampire_atlas.png           ← Zombi atlas (4x12)
│   │   ├── Archer_atlas.png            ← Okcu atlas (4x8)
│   │   ├── Redcap_atlas.png            ← Koylu atlas (4x8)
│   │   └── ... (kaynak sheet'ler)
│   └── Projectiles/
│       └── Arrow.png                   ← Ok sprite (statik)
└── Materials/
    ├── ZombieMat.mat
    ├── ArcherMat.mat
    └── ArrowMat.mat
```
