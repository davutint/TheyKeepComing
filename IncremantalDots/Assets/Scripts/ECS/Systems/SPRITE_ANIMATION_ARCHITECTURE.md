# Sprite Animation System — Mimari

## Genel Bakis

DOTS/ECS ortaminda sprite sheet animasyonu icin per-instance GPU rendering sistemi.
50K+ entity'de tek draw call ile farkli animasyon frame'leri gosterebilir.
Texture Atlas yaklasimi ile tek material uzerinden birden fazla animasyon destekler.

**Asset:** Character Creator - Fantasy 2D (SmallScale Interactive)

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

Her karakter tipi icin tum animasyonlar tek PNG atlas'a birlestirilmis.
**Character Creator - Fantasy 2D** asset'inden export edilen 4 ayri animasyon PNG'si
(Walk, Attack, Die, Idle) Editor tool ile birlestirilir.

### Atlas Grid: 15 col x 32 row (1920 x 4096 px)

Her animasyon blogu 8 satir (8 yon), her satir 15 frame, her frame 128x128 px.

```
Row  0- 7:  Walk    (15 frame/yon)  ← Moving + Queued state
Row  8-15:  Attack  (15 frame/yon)  ← Attacking state (melee swing)
Row 16-23:  Die     (15 frame/yon)  ← Dead state
Row 24-31:  Idle    (15 frame/yon)  ← Bosta / ileride kullanilacak
```

### 8 Yon Indeksleri (saat yonu, East'ten baslayarak)

```
+0 = East       (saga bakiyor)
+1 = SouthEast
+2 = South      (kameraya bakiyor, on gorunum)
+3 = SouthWest
+4 = West       (sola bakiyor) ← zombie default
+5 = NorthWest
+6 = North      (sirtini donmus)
+7 = NorthEast
```

### DirectionRow Hesaplama

```
DirectionRow = animOffset + directionIndex

Walk:   0 + dir  →  0- 7
Attack: 8 + dir  →  8-15
Die:   16 + dir  → 16-23
Idle:  24 + dir  → 24-31
```

### Yon Cikartma (mevcut DirectionRow'dan)

```
directionIndex = DirectionRow % 8
```

---

## Atlas Olusturma (Editor Tool)

**Eski yontem (Python/Pillow) kaldirildi.** Yeni yontem Unity Editor icerisinde calisir.

### Editor Tool: SpriteAtlasGenerator

Dosya: `Assets/Scripts/Editor/SpriteAtlasGenerator.cs`
Menu: `Window > DeadWalls > Sprite Atlas Generator`

1. Character Creator'dan 4 animasyon PNG'si export et (her biri 1920x1024 px)
2. Editor tool'u ac, 4 slot'a PNG'leri ata
3. "Generate Atlas" tikla → otomatik 1920x4096 atlas olusturulur
4. Import ayarlari otomatik set edilir (PPU=128, Point filter, No compression)

Tool asagidakileri otomatik yapar:
- Read/Write kapali texture'lardan bile piksel okur (RenderTexture trick)
- Walk→Attack→Die→Idle sirasinda alt alta birlestirir
- PNG olarak kaydeder
- TextureImporter ayarlarini set eder

---

## Bilesenler

### SpriteAnimation (IComponentData)
Dosya: `Assets/Scripts/ECS/Components/SpriteComponents.cs`

| Alan | Tip | Aciklama |
|------|-----|----------|
| TotalColumns | int | Grid sutun sayisi (15) |
| TotalRows | int | Grid satir sayisi (32 = 4 anim x 8 yon) |
| DirectionRow | int | Aktif satir — animOffset + directionIndex (0-31) |
| FrameCount | int | Aktif animasyondaki frame sayisi (15) |
| CurrentFrame | int | Su anki frame (0-based, 0-14) |
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

Olum animasyonu suresi. Die animasyonunun toplam frame suresi kadar
(15 frame * FrameInterval). 0'a dusunce entity silinir.

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

**Degisiklik yok** — bu system tamamen generic, TotalRows/TotalColumns ne verilirse calisir.

### ZombieAnimationStateSystem (SimulationSystemGroup)
Dosya: `Assets/Scripts/ECS/Systems/ZombieAnimationStateSystem.cs`

ZombieState degisikligine gore animasyon degistirir:

| State | Offset | Satir Araligi | Frame Sayisi |
|-------|--------|---------------|-------------|
| Moving | WalkOffset=0 | 0-7 | 15 |
| Attacking | AttackOffset=8 | 8-15 | 15 |
| Queued | WalkOffset=0 | 0-7 | 15 (walk ile ayni) |
| Dead | DieOffset=16 | 16-23 | 15 |

Yon cikartma: `dir = DirectionRow % 8`
Hedef satir: `targetRow = offset + dir`

Dead state'te DeathTimer = `DieFrameCount * FrameInterval` (animasyonun tam suresi)

### DamageCleanupSystem (SimulationSystemGroup)
Dosya: `Assets/Scripts/ECS/Systems/DamageCleanupSystem.cs`

DeathTimer countdown → 0'a dusunce destroy + XP odul.

---

## Veri Akisi

```
ZombieState degisir (Moving → Attacking → Dead)
  → ZombieAnimationStateSystem: DirectionRow = offset + dir, FrameCount guncelle
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

## Mevcut Entity Tipleri ve Ayarlari

| Entity | Atlas | Rows | Columns | Frame/Anim | Animasyonlar | Prefab FPS |
|--------|-------|------|---------|------------|-------------|------------|
| Zombi | zombie_atlas | 32 | 15 | 15 | Walk + Attack + Die + Idle | 10 |
| Okcu | archer_atlas | 32 | 15 | 15 | Walk + Attack + Die + Idle | 10 |
| Ok | Arrow.png | 1 | 1 | 1 | Statik | 1 |

> **Not:** Eski Vampire_atlas (4x12), Archer_atlas (4x8), Redcap_atlas (4x8) artik kullanilmiyor.
> Character Creator - Fantasy 2D atlas'lari ile degistirildi.

---

## Dosya Haritasi

```
Assets/
├── Shaders/
│   └── SpriteSheet.shader              ← Custom URP HLSL shader
├── Scripts/
│   ├── ECS/
│   │   ├── Components/
│   │   │   ├── SpriteComponents.cs     ← SpriteAnimation + SpriteUVRect
│   │   │   └── ZombieComponents.cs     ← DeathTimer
│   │   ├── Authoring/
│   │   │   └── SpriteSheetAuthoring.cs ← Baker (MonoBehaviour → ECS)
│   │   └── Systems/
│   │       ├── SpriteAnimationSystem.cs    ← UV rect hesaplama (Burst)
│   │       ├── ZombieAnimationStateSystem.cs ← State→Animasyon (Burst)
│   │       └── DamageCleanupSystem.cs      ← DeathTimer countdown
│   └── Editor/
│       └── SpriteAtlasGenerator.cs     ← Atlas birlestirme Editor tool
├── Art/
│   └── Atlases/
│       ├── zombie_atlas.png            ← Zombi atlas (15x32, 1920x4096)
│       └── archer_atlas.png            ← Okcu atlas (15x32, 1920x4096)
├── SmallScaleInt/
│   └── Character creator - Fantasy/    ← Kaynak asset (export icin)
│       ├── spritesheets/               ← Parcali sprite PNG'leri
│       └── Created Spritesheets/       ← Export edilen tam PNG'ler
└── Materials/
    ├── ZombieMat.mat
    ├── ArcherMat.mat
    └── ArrowMat.mat
```
