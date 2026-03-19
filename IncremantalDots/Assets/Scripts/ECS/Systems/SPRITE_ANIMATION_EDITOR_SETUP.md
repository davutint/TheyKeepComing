# Sprite Animation — Unity Editor Kurulum

## Genel Bakis

Bu rehber Character Creator - Fantasy 2D asset'inden karakter export etmeyi,
atlas olusturmayi ve Unity prefab'larini ayarlamayi ADIM ADIM anlatir.

---

## 1. Character Creator'dan Karakter Export Etme

### 1.1. Example Scene'i Ac

1. Unity Editor'de **Project** panelinden su yolu takip et:
   `Assets/SmallScaleInt/Character creator - Fantasy/Example Scene/`
2. Scene dosyasini cift tikla (Character Creator scene)
3. **Play** tusuna bas — Character Creator arayuzu acilacak

### 1.2. Zombie Karakteri Olustur

1. **Preset butonlari** (ust kisimda) varsa bunlardan birini secebilirsin
2. Veya parcali olustur:
   - **Head:** zombie/monster gorunumlu kask sec
   - **Chest:** yirtik zirh veya parca
   - **Legs:** basit pantol
   - **Shoes:** basit ayakkabi veya cizme
3. **Skin Color:** koyu/yesil/gri ton (zombie icin)
4. **Weapon:** KAPALI brak (unarmed zombi — aktif weapon toggle'a tekrar tikla)
5. **Shield:** KAPALI
6. **Backpack:** KAPALI

### 1.3. Spritesheet Export Et

1. Sol taraftaki **Spritesheet paneline** gec (Generate butonu olan panel)
2. Asagidaki ayarlari kontrol et:
   - **Shadow:** %0 (shadow kapali — ECS'te kullanmiyoruz)
   - **GunFire:** KAPALI (toggle OFF)
   - **Skin:** ACIK (toggle ON)
3. **"Generate"** butonuna tikla
4. Bekleme ekrani cikacak — her animasyon icin ayri PNG uretiliyor
5. Tamamlaninca dosyalar export klasorune kaydedilir

### 1.4. Export Klasorunu Bul

Export edilen dosyalar su konumda olacak (timestamp'li klasor):
```
Assets/SmallScaleInt/Character creator - Fantasy/Created Spritesheets/[KLASOR_ADI]/
```

Icindeki dosyalar (her biri 1920x1024 px, 15 col x 8 row):
```
Attack1.png          ← Ranged attack (KULLANMAYACAGIZ)
Attack2.png          ← Throwing (KULLANMAYACAGIZ)
Attack3.png          ← Melee swing 1 ★ BUNU KULLANACAGIZ
Attack4.png          ← Melee swing 2 (alternatif)
Attack5.png          ← Ek attack
AttackRun.png        ← Kosan saldiri
AttackRun2.png       ← Kosan saldiri 2
CrouchIdle.png       ← Coemelme
CrouchRun.png        ← Gizli yurume
Die.png              ★ BUNU KULLANACAGIZ
Idle.png             ← Silahli bekleme
Idle2.png            ← Nisangah
Idle3.png            ★ BUNU KULLANACAGIZ (silahsiz bekleme)
Idle4.png            ← Ek idle
RideIdle.png         ← Binekli bekleme
RideRun.png          ← Binekli kos
Run.png              ← Kosma (alternatif Walk)
RunBackwards.png     ← Geri kosma
Special1.png         ← Ozel yetenek
StrafeLeft.png       ← Yana kayma
StrafeRight.png      ← Yana kayma
TakeDamage.png       ← Hasar alma
Taunt.png            ← Dokunme/kullanma
Walk.png             ★ BUNU KULLANACAGIZ
```

**Zombie icin kullanacagimiz 4 dosya:**
- `Walk.png` → Moving + Queued state
- `Attack3.png` → Attacking state (melee swing)
- `Die.png` → Dead state
- `Idle3.png` → Idle state (silahsiz)

### 1.5. Okcu Karakteri Olustur (Ayri Export)

1. Play'i durdur, tekrar Character Creator scene'i ac, tekrar Play
2. Farkli bir karakter olustur:
   - Zirh/kask: okcu gorunumlu
   - **Weapon:** Yay (bow) sec
   - Skin: normal ten rengi
3. Ayni sekilde export et
4. Okcu icin dosyalar:
   - `Walk.png` → hareket
   - `Attack1.png` → ranged attack (yay atisi)
   - `Die.png` → olum
   - `Idle.png` → silahli bekleme (yay tutarak)

---

## 2. Atlas Olusturma (Editor Tool)

### 2.1. Tool'u Ac

1. Unity Editor'de menu cubugunda: **Window > DeadWalls > Sprite Atlas Generator**
2. Kucuk bir pencere acilacak

### 2.2. Zombie Atlas Olustur

1. **Walk (Row 0-7)** slot'una: export edilen `Walk.png` dosyasini surukle-birak
2. **Attack (Row 8-15)** slot'una: `Attack3.png` dosyasini surukle-birak
3. **Die (Row 16-23)** slot'una: `Die.png` dosyasini surukle-birak
4. **Idle (Row 24-31)** slot'una: `Idle3.png` dosyasini surukle-birak
5. **Klasor:** `Assets/Art/Atlases` (degistirmene gerek yok)
6. **Dosya Adi:** `zombie_atlas`
7. **"Generate Atlas"** tikla

Sonuc:
- `Assets/Art/Atlases/zombie_atlas.png` olusturulacak (1920x4096 px)
- Import ayarlari otomatik set edilecek (PPU=128, Point filter, No compression)
- Dosya otomatik Project panelinde secilecek

### 2.3. Archer Atlas Olustur

Ayni adimlari tekrarla:
1. Walk: `Walk.png`
2. Attack: `Attack1.png` (ranged attack — okcu icin)
3. Die: `Die.png`
4. Idle: `Idle.png` (silahli idle — okcu icin)
5. Dosya Adi: `archer_atlas`
6. Generate

### 2.4. Boyut Dogrulama

Her PNG 1920x1024 px olmali (15 frame x 128px = 1920, 8 yon x 128px = 1024).
Eger boyut farkli ise hata mesaji cikacak — Character Creator'dan dogru
export yapilip yapilmadigini kontrol et.

---

## 3. Texture Import Dogrulama

Atlas Generator tool import ayarlarini otomatik set eder, ama kontrol etmek icin:

1. Project panelinde `Assets/Art/Atlases/zombie_atlas.png` sec
2. Inspector'da su degerleri dogrula:
   - **Texture Type:** Sprite (2D and UI)
   - **Sprite Mode:** Single
   - **Pixels Per Unit:** 128
   - **Filter Mode:** Point (no filter) ← KRITIK! Pixel art icin
   - **Compression:** None ← piksel bozulmasi onlenir
   - **Max Size:** 8192 (atlas 4096 yuksekliginde, 8192 yeterli)
   - **Mipmap:** Kapali (Generate Mip Maps unchecked)

Eger degerler farkli ise duzelt ve **Apply** tikla.

---

## 4. Material Guncelleme

### ZombieMat:
1. Project panelinde `Assets/Materials/ZombieMat.mat` sec
2. Inspector'da:
   - Shader: **DeadWalls/SpriteSheet** (degismeli)
   - **Sprite Sheet:** `zombie_atlas.png` ata (Assets/Art/Atlases/)
   - **Alpha Cutoff:** 0.5
   - **Tint:** Beyaz (1,1,1,1)

### ArcherMat:
1. `Assets/Materials/ArcherMat.mat` sec
2. **Sprite Sheet:** `archer_atlas.png` ata
3. Diger ayarlar ayni

### ArrowMat:
Degisiklik yok (Arrow.png statik, tek sprite).

---

## 5. Prefab Guncelleme

### Zombie Prefab:
1. `Assets/Prefabs/Zombie.prefab` ac (cift tikla veya Inspector'da Open)
2. **SpriteSheetAuthoring** component'ini bul (Inspector'da)
3. Degerleri guncelle:
   - **Columns:** `15` (eski: 4)
   - **Rows:** `32` (eski: 12)
   - **FPS:** `10` (eski: 7 — 15 frame'lik animasyon icin 10 FPS iyi)
   - **Direction Row:** `4` (West yonu — zombie sola bakar. Eski: 1)
   - **Frame Count:** `15` (eski: 4)
4. **Apply** tikla (Prefab uzerinde)

### Archer Prefab:
1. `Assets/Prefabs/Archer.prefab` ac
2. SpriteSheetAuthoring:
   - **Columns:** `15`
   - **Rows:** `32`
   - **FPS:** `10`
   - **Direction Row:** `0` (East yonu — okcu saga bakar. Eski: 2)
   - **Frame Count:** `15`
3. Apply

### Arrow Prefab:
Degisiklik yok.

---

## 6. Atlas Satir Referans Tablosu

### zombie_atlas.png / archer_atlas.png (15 col x 32 row, 1920x4096 px)

| Satir | Animasyon | Frame Sayisi | Kullanim |
|-------|-----------|-------------|----------|
| 0-7   | Walk      | 15/yon      | Moving + Queued state |
| 8-15  | Attack    | 15/yon      | Attacking state |
| 16-23 | Die       | 15/yon      | Dead state |
| 24-31 | Idle      | 15/yon      | Bosta (ileride) |

### Yon Indeksleri (her animasyon blogu icinde)

```
+0 = East        (saga bakiyor)
+1 = SouthEast
+2 = South       (kameraya bakiyor, on)
+3 = SouthWest
+4 = West        (sola bakiyor) ← zombie default
+5 = NorthWest
+6 = North       (sirtini donmus)
+7 = NorthEast
```

### DirectionRow Ornekleri

| Karakter | State | Yon | DirectionRow |
|----------|-------|-----|-------------|
| Zombie (Walk, West) | Moving | W=4 | 0 + 4 = **4** |
| Zombie (Attack, West) | Attacking | W=4 | 8 + 4 = **12** |
| Zombie (Die, West) | Dead | W=4 | 16 + 4 = **20** |
| Okcu (Idle, East) | Idle | E=0 | 24 + 0 = **24** |
| Zombie (Walk, South) | Moving | S=2 | 0 + 2 = **2** |
| Zombie (Walk, NorthEast) | Moving | NE=7 | 0 + 7 = **7** |

---

## 7. Test

1. **Play** tusuna bas
2. Zombiler → **Walk animasyonu** (15 frame, sola yurume, akici)
3. Zombi duvara ulasinca → **Attack animasyonu** (melee swing)
4. Zombi olunce → **Die animasyonu** (15 frame, sonra kaybolur)
5. Okcular → **Walk/Idle animasyonu**
6. Oklar → statik sprite (degisiklik yok)

### Beklenen Sonuclar:
- Animasyonlar 15 frame ile eskisine gore cok daha akici olmali
- 8 yon destegi ile ileride (M-ISO.3/4) farkli yonlerden gelen zombiler
  dogru yonde animasyon gosterecek
- FPS=10 ile Walk animasyonu 1.5 saniyede bir dongu = dogal yurume hissi

---

## 8. Sorun Giderme

| Sorun | Cozum |
|-------|-------|
| Entity tamamen gorunmuyor | Shader Tags kontrol: `Opaque`/`Geometry` olmali. `TransparentCutout`/`AlphaTest` kullanilMAmali. LightMode tag'i olmamali. |
| Pembe/Magenta gorunuyor | Shader compile hatasi — Console kontrol et |
| Sprite bulanik | Texture Filter Mode → Point |
| Seffaf kisimlar siyah | Shader `DeadWalls/SpriteSheet` mi? Alpha Cutoff 0.5 mi? |
| Animasyon cok hizli/yavas | Prefab FPS degerini ayarla (10 iyi baslangic) |
| Yanlis yon gorunuyor | DirectionRow kontrol et. West=4, East=0 |
| Yanlis animasyon gorunuyor | atlas siralama kontrol et: Walk(0-7), Attack(8-15), Die(16-23) |
| Atlas boyut hatasi | Her kaynak PNG 1920x1024 olmali. Character Creator'dan tekrar export et |
| Texture 8192'den buyuk uyarisi | Max Size=8192 olmali. Atlas 4096 yuksekliginde, sorun yok |
| "Generate Atlas" buton kapali | 4 PNG slot'unun hepsi doldurulmali |
| Entity gorunmuyor ama Frame Debugger'da var | Material'deki texture dogru atlas'a mi isaret ediyor? |
| Olum animasyonu kesiliyor | DeathTimer cok kisa olabilir — FPS*15 frame kontrol et |

### Shader Debug Yontemi
Entity'ler gorunmuyorsa, asagidaki siralamayla test et:

1. **Kirmizi test:** Shader'in frag fonksiyonunu `return half4(1,0,0,1);` yap → entity gorunuyorsa shader altyapisi dogru
2. **Texture test:** `return SAMPLE_TEXTURE2D(...)` ekle → texture okunuyorsa atlas dogru
3. **UV + clip test:** `_UVRect` ve `clip()` ekle → tam calisan shader

Her adimda entity'yi kontrol et. Gorunmeyen adimda sorun o katidir.

---

## 9. Kritik Shader Kurallari (DOTS Entities Graphics)

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

---

## 10. Yeni Karakter Tipi Ekleme — Adim Adim Ozet

1. Character Creator scene'i ac, Play bas
2. Karakter olustur (gear, renk, silah sec)
3. Generate ile export et (4 PNG: Walk, Attack, Die, Idle)
4. Window > DeadWalls > Sprite Atlas Generator ac
5. 4 PNG'yi slot'lara ata, isim ver, Generate tikla
6. Material olustur (Shader: DeadWalls/SpriteSheet, atlas ata)
7. Prefab'da SpriteSheetAuthoring: Columns=15, Rows=32, FPS=10, FrameCount=15
8. DirectionRow'u varsayilan yonune ayarla (E=0, W=4 vb.)
9. Test et
