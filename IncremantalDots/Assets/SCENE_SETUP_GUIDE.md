# Dead Walls - Sahne Kurulum Rehberi (Adim Adim)

Bu rehber DOTS bilmeyenler icin sifirdan sahne kurulumunu anlatir.
Unity 6 acik ve proje yuklenmis varsayilir. DOTS paketleri manifest.json'da zaten tanimli.

---

## BOLUM 1: Ilk Acilis — Paket Kontrolu

1. Unity'yi ac. Ilk seferde DOTS paketleri indirilecek, bu 2-5 dakika surebilir.
2. Console'da hata varsa:
   - `Window → Package Manager` ac
   - Sol listede `com.unity.entities` ve `com.unity.entities.graphics` gorunmeli
   - Gorunmuyorsa: sol ustte `+` → `Add package by name` → `com.unity.entities` yaz, `Add` tikla. Ayni sekilde `com.unity.entities.graphics` ekle.
3. Console temizlenene kadar bekle. Bazi uyarilar normal, **kirmizi hata olmamali**.

---

## BOLUM 2: Material Olusturma (Prefab'lar icin gerekli)

Prefab'lara atamak icin 3 farkli renkte material olusturacagiz.

### 2.1 Zombi Material (Kirmizi)
1. Project panelinde `Assets` klasorune sag tikla → `Create → Folder` → adi: `Materials`
2. `Assets/Materials` icine sag tikla → `Create → Material` → adi: `MAT_Zombie`
3. Inspector'da:
   - Shader dropdown'unu tikla → `Universal Render Pipeline → Lit` (veya `Unlit` daha basit)
   - `Base Map` yanindaki **renk kutusunu** tikla → **Kirmizi** sec (R:200 G:50 B:50)
   - `Surface Type` → `Opaque`

### 2.2 Ok Material (Sari)
1. `Assets/Materials` icine sag tikla → `Create → Material` → adi: `MAT_Arrow`
2. Inspector'da:
   - Shader: `Universal Render Pipeline → Lit`
   - Base Map rengi: **Sari** (R:255 G:220 B:50)

### 2.3 Okcu Material (Mavi)
1. `Assets/Materials` icine sag tikla → `Create → Material` → adi: `MAT_Archer`
2. Inspector'da:
   - Shader: `Universal Render Pipeline → Lit`
   - Base Map rengi: **Mavi** (R:50 G:100 B:220)

### 2.4 Duvar Material (Gri)
1. `Assets/Materials` → `Create → Material` → adi: `MAT_Wall`
2. Base Map rengi: **Koyu Gri** (R:100 G:100 B:100)

---

## BOLUM 3: Prefab Olusturma

### 3.1 Zombie_Surungun Prefab
1. Hierarchy panelinde (sol ust) sag tikla → `3D Object → Quad`
2. Adi: `Zombie_Surungun`
3. Inspector'da (sag panel):
   - **Transform:**
     - Position: (0, 0, 0)
     - Scale: **(0.8, 0.8, 1)**
   - **Mesh Renderer → Materials:** `MAT_Zombie` material'ini surukle (Element 0'in ustune birak)
4. Inspector'da en altta `Add Component` butonuna tikla
5. Arama kutusuna `ZombieAuthoring` yaz → tikla (eklendi)
6. Inspector'da ZombieAuthoring degerlerini kontrol et (varsayilanlar yeterli):
   - Move Speed: 1.5
   - Max HP: 20
   - Attack Damage: 5
   - Gold Reward: 5
   - XP Reward: 10
7. **Prefab yapma:** Hierarchy'deki `Zombie_Surungun` objesini surukleyip `Assets/Prefabs` klasorune birak
   - "Original Prefab" sec (eger sorarsa)
8. Hierarchy'deki objeyi sil (sag tikla → Delete)

### 3.2 Arrow Prefab
1. Hierarchy → sag tikla → `3D Object → Quad`
2. Adi: `Arrow`
3. Inspector:
   - **Transform Scale: (0.3, 0.1, 1)** — ince uzun ok seklinde
   - **Mesh Renderer → Materials:** `MAT_Arrow`
4. `Add Component` → `ArrowAuthoring`
   - Speed: 12
   - Damage: 10
5. Hierarchy'den `Assets/Prefabs` klasorune surukle → prefab olustur
6. Hierarchy'den sil

### 3.3 Archer Prefab
1. Hierarchy → sag tikla → `3D Object → Quad`
2. Adi: `Archer`
3. Inspector:
   - **Transform Scale: (0.6, 0.8, 1)**
   - **Mesh Renderer → Materials:** `MAT_Archer`
4. `Add Component` → `ArcherAuthoring`
   - Fire Rate: 1.5
   - Arrow Damage: 10
   - Range: 15
5. Hierarchy'den `Assets/Prefabs` klasorune surukle → prefab olustur
6. Hierarchy'den sil

---

## BOLUM 4: Ana Sahneyi Hazirlama

### 4.1 Sahneyi kaydet
1. `File → Save As` → `Assets/Scenes/GameScene.unity` olarak kaydet
2. Eski `SampleScene` varsa silebilirsin

### 4.2 Kamerayi ayarla
1. Hierarchy'de `Main Camera` objesini sec
2. `Add Component` → `CameraSetup` yaz → ekle
3. (CameraSetup otomatik olarak orthographic, size=6, position=(0,0,-10) yapar)
4. **Kontrol:** Inspector'da Camera component'inde `Projection: Orthographic` gorunmeli

### 4.3 GameManager olustur
1. Hierarchy'de sag tikla → `Create Empty`
2. Adi: `GameManager`
3. `Add Component` → `GameManager` (DeadWalls namespace'inden)
4. `Add Component` → `ClickDamageHandler`

---

## BOLUM 5: Sub Scene Olusturma (KRITIK ADIM)

**Sub Scene nedir?** DOTS/ECS'de entity'ler normal GameObject'lerden farklidir. Authoring component'lari (ZombieAuthoring, CastleAuthoring vs.) sadece Sub Scene icinde "bake" edilir, yani ECS entity'lerine donusturulur. Sub Scene olmadan ECS calismaz.

### 5.1 Sub Scene olustur
1. Hierarchy'de sag tikla → `Create Empty`
2. Adi: `GameSubScene`
3. Bu obje secili iken Inspector'da `Add Component` → aramaya `Sub Scene` yaz → `Sub Scene` ekle
   - **NOT:** Eger `Sub Scene` bulamazsan, `Unity.Scenes` veya `SubScene` dene
4. Inspector'da Sub Scene component'inde `Scene Asset` alani bos olacak
5. **"New" butonuna tikla** (veya Edit Scene butonuna)
   - Senden bir scene dosyasi kaydetmeni isteyecek
   - `Assets/Scenes/GameSubScene.unity` olarak kaydet
6. Simdi Sub Scene "editing" moduna gecti — Hierarchy'de `GameSubScene` yaninda bir **acik kilit ikonu** veya **cesitli renkli cerceve** goreceksin

### 5.2 Sub Scene icine GameState ekle
**ONEMLI: Bu adimlari Sub Scene EDITING modunda iken yap!**

1. Hierarchy'de `GameSubScene` uzerine sag tikla → `Create Empty`
   - (Veya `GameSubScene`'in child'i olarak olustur)
2. Adi: `GameState`
3. `Add Component` → `GameStateAuthoring`
   - Click Damage: 10
   - XP To Next Level: 100
   - Spawn Interval: 0.8
   - Wave Start Delay: 3
   - Base Zombie Speed: 1.5
4. **Ayni objeye** `Add Component` → `WaveConfigAuthoring`
   - **Zombie Prefab** alanina: Project panelinden `Assets/Prefabs/Zombie_Surungun` prefab'ini surukle
   - **Arrow Prefab** alanina: Project panelinden `Assets/Prefabs/Arrow` prefab'ini surukle

### 5.3 Sub Scene icine Castle ekle
1. `GameSubScene` icinde sag tikla → `3D Object → Quad` (veya Create Empty)
2. Adi: `Castle`
3. Inspector:
   - **Transform Position: (-6, 0, 0)** — ekranin sol tarafi
   - Scale: (0.5, 10, 1) — dikey uzun duvar seklinde
   - Mesh Renderer → Materials: `MAT_Wall`
4. `Add Component` → `CastleAuthoring`
   - Wall HP: 200
   - Gate HP: 100
   - Castle Max HP: 500
   - Wall X Pos: -6

### 5.4 Sub Scene icine Okcu ekle
1. `GameSubScene` icinde sag tikla → `3D Object → Quad`
2. Adi: `Archer_01`
3. Inspector:
   - **Transform Position: (-5.5, 2, 0)** — duvar uzerinde
   - Scale: (0.6, 0.8, 1)
   - Mesh Renderer → Materials: `MAT_Archer`
4. `Add Component` → `ArcherAuthoring`
   - Fire Rate: 1.5
   - Arrow Damage: 10
   - Range: 15

### 5.5 Sub Scene'den cik
1. Hierarchy'de `GameSubScene`'in yanindaki **kilit/close butonuna** tikla
   - Veya Inspector'da Sub Scene component'inde **"Close" butonuna** tikla
2. Sub Scene artik "closed" modda — icindekiler gri gorunecek, bu normal
3. **File → Save** ile sahneyi kaydet (Ctrl+S)

---

## BOLUM 6: UI Canvas Olusturma

### 6.1 Canvas
1. Hierarchy sag tikla → `UI → Canvas`
2. Canvas secili iken Inspector:
   - **Canvas Scaler:**
     - UI Scale Mode: `Scale With Screen Size`
     - Reference Resolution: `1920 x 1080`
     - Match: `0.5`
3. Canvas objesine `Add Component` → `UIManager`

### 6.2 HUD Panel
1. Canvas'in icinde sag tikla → `UI → Panel`
2. Adi: `HUDPanel`
3. Inspector:
   - Image component'inde `Color` alpha'sini 0 yap (seffaf arka plan)
4. `Add Component` → `HUDController`

#### HUD icerigi olustur:

**Wave Text:**
1. HUDPanel icinde sag tikla → `UI → Text - TextMeshPro`
   - (Ilk seferde TMP import sordugunda "Import TMP Essentials" tikla)
2. Adi: `WaveText`
3. Rect Transform: Anchor = Top Center, Pos Y = -30, Width = 300, Height = 50
4. Text: "Wave: 1", Font Size: 28, Alignment: Center

**Gold Text:**
1. HUDPanel icinde → `UI → Text - TextMeshPro`
2. Adi: `GoldText`
3. Rect Transform: Anchor = Top Right, Pos X = -100, Pos Y = -30, Width = 200, Height = 40
4. Text: "Gold: 0", Font Size: 22, Alignment: Right

**XP Text:**
1. HUDPanel icinde → `UI → Text - TextMeshPro`
2. Adi: `XPText`
3. Rect Transform: Anchor = Top Right, Pos X = -100, Pos Y = -60, Width = 200, Height = 40
4. Text: "XP: 0/100", Font Size: 22, Alignment: Right

**Level Text:**
1. HUDPanel icinde → `UI → Text - TextMeshPro`
2. Adi: `LevelText`
3. Rect Transform: Anchor = Top Right, Pos X = -100, Pos Y = -90, Width = 200, Height = 40
4. Text: "Level: 1", Font Size: 22, Alignment: Right

**Zombies Text:**
1. HUDPanel icinde → `UI → Text - TextMeshPro`
2. Adi: `ZombiesAliveText`
3. Rect Transform: Anchor = Top Center, Pos Y = -60, Width = 300, Height = 40
4. Text: "Zombies: 0", Font Size: 22, Alignment: Center

**Wall HP Bar:**
1. HUDPanel icinde sag tikla → `UI → Slider`
2. Adi: `WallHPBar`
3. Rect Transform: Anchor = Top Left, Pos X = 150, Pos Y = -30, Width = 200, Height = 20
4. Slider component: Min=0, Max=200, Value=200, **Whole Numbers: isaretli degil**
5. Child objelerini duzenle:
   - `Background` → Image Color: koyu kirmizi
   - `Fill Area → Fill` → Image Color: **kirmizi**
   - `Handle Slide Area` → **sil** (handle gerekmiyor)
   - Slider Inspector'da `Interactable` → **kapat** (tiklanmasin)

**Gate HP Bar:**
1. HUDPanel icinde → `UI → Slider` → adi: `GateHPBar`
2. Rect Transform: Pos X = 150, Pos Y = -55, Width = 200, Height = 20
3. Ayni ayarlar, Fill rengi: **turuncu**
4. Handle sil, Interactable kapat

**Castle HP Bar:**
1. HUDPanel icinde → `UI → Slider` → adi: `CastleHPBar`
2. Rect Transform: Pos X = 150, Pos Y = -80, Width = 200, Height = 20
3. Ayni ayarlar, Fill rengi: **yesil**
4. Handle sil, Interactable kapat

**HUDController referanslarini bagla:**
1. HUDPanel objesini sec
2. Inspector'da HUDController component'inde her alan icin ilgili objeyi surukle:
   - Wall HP Bar → WallHPBar
   - Gate HP Bar → GateHPBar
   - Castle HP Bar → CastleHPBar
   - Gold Text → GoldText
   - XP Text → XPText
   - Wave Text → WaveText
   - Level Text → LevelText
   - Zombies Alive Text → ZombiesAliveText

### 6.3 Level Up Panel
1. Canvas icinde sag tikla → `UI → Panel`
2. Adi: `LevelUpPanel`
3. Image Color: siyah, alpha = 200 (yari seffaf karartma)
4. `Add Component` → `LevelUpUI`

**Title Text:**
1. LevelUpPanel icinde → `UI → Text - TextMeshPro`
2. Adi: `TitleText`
3. Rect Transform: Anchor = Top Center, Pos Y = -100, Width = 400, Height = 80
4. Text: "LEVEL UP!", Font Size: 48, Alignment: Center, Color: beyaz

**Butonlar:**
1. LevelUpPanel icinde sag tikla → `UI → Button - TextMeshPro`
2. Adi: `AddArcherButton`
3. Rect Transform: Anchor = Middle Center, Pos X = -250, Pos Y = -30, Width = 200, Height = 120
4. Child `Text (TMP)` → adi: yalniz birak, text: "Okcu Ekle\n+1 Okcu", Font Size: 18
5. **Ayni sekilde iki buton daha:**
   - `ArrowDamageButton` → Pos X = 0, Text: "Ok Hasari\n+5 Hasar"
   - `RepairGateButton` → Pos X = 250, Text: "Kapi Tamir\nTam HP"

**LevelUpUI referanslarini bagla:**
1. LevelUpPanel sec → Inspector'da LevelUpUI:
   - Add Archer Button → AddArcherButton
   - Arrow Damage Button → ArrowDamageButton
   - Repair Gate Button → RepairGateButton
   - Add Archer Text → AddArcherButton'un child'indaki Text (TMP)
   - Arrow Damage Text → ArrowDamageButton'un child'indaki Text (TMP)
   - Repair Gate Text → RepairGateButton'un child'indaki Text (TMP)
   - Title Text → TitleText

**ONEMLI:** LevelUpPanel'i **deaktif** et:
- Inspector'da sol ustteki **checkbox'u kapat** (obje gri olacak)

### 6.4 Game Over Panel
1. Canvas icinde → `UI → Panel`
2. Adi: `GameOverPanel`
3. Image Color: siyah, alpha = 220
4. `Add Component` → `GameOverUI`

**Game Over Text:**
1. Icinde → `UI → Text - TextMeshPro` → adi: `GameOverText`
2. Text: "GAME OVER", Font Size: 56, Color: kirmizi, Alignment: Center
3. Rect Transform: Anchor = Middle Center, Pos Y = 80

**Stats Text:**
1. Icinde → `UI → Text - TextMeshPro` → adi: `StatsText`
2. Text: "Wave: 1\nLevel: 1\nGold: 0", Font Size: 28, Color: beyaz
3. Rect Transform: Pos Y = 0

**Restart Button:**
1. Icinde → `UI → Button - TextMeshPro` → adi: `RestartButton`
2. Child Text: "RESTART", Font Size: 24
3. Rect Transform: Pos Y = -80, Width = 200, Height = 60

**GameOverUI referanslarini bagla:**
- Game Over Text → GameOverText
- Stats Text → StatsText
- Restart Button → RestartButton

**ONEMLI:** GameOverPanel'i **deaktif** et (checkbox kapat)

### 6.5 UIManager referanslarini bagla
1. Canvas objesini sec
2. UIManager component'inde:
   - HUD Panel → HUDPanel
   - Level Up Panel → LevelUpPanel
   - Game Over Panel → GameOverPanel

---

## BOLUM 7: Son Kontroller

### Hierarchy Gorunumu (son hali)
```
GameScene
├── Main Camera          (Camera + CameraSetup)
├── Directional Light    (varsayilan isik)
├── GameManager          (GameManager + ClickDamageHandler)
├── GameSubScene         (Sub Scene component)
│   ├── GameState        (GameStateAuthoring + WaveConfigAuthoring)  ← SUB SCENE ICINDE
│   ├── Castle           (Quad + CastleAuthoring)                    ← SUB SCENE ICINDE
│   └── Archer_01        (Quad + ArcherAuthoring)                    ← SUB SCENE ICINDE
├── Canvas               (Canvas + UIManager)
│   ├── HUDPanel         (Panel + HUDController)
│   │   ├── WaveText
│   │   ├── GoldText
│   │   ├── XPText
│   │   ├── LevelText
│   │   ├── ZombiesAliveText
│   │   ├── WallHPBar
│   │   ├── GateHPBar
│   │   └── CastleHPBar
│   ├── LevelUpPanel     (Panel + LevelUpUI) [DEAKTIF]
│   │   ├── TitleText
│   │   ├── AddArcherButton
│   │   ├── ArrowDamageButton
│   │   └── RepairGateButton
│   └── GameOverPanel    (Panel + GameOverUI) [DEAKTIF]
│       ├── GameOverText
│       ├── StatsText
│       └── RestartButton
└── EventSystem          (otomatik olusur Canvas ile)
```

### Kontrol Listesi
- [ ] DOTS paketleri kurulu, Console'da kirmizi hata yok
- [ ] 3 prefab Assets/Prefabs icinde (Zombie_Surungun, Arrow, Archer)
- [ ] Sub Scene icinde GameState, Castle, Archer_01 var
- [ ] WaveConfigAuthoring'de Zombie ve Arrow prefab'lari atanmis
- [ ] CastleAuthoring'de Wall X Pos = -6
- [ ] Main Camera'da CameraSetup var
- [ ] GameManager objesinde GameManager + ClickDamageHandler var
- [ ] Canvas'ta UIManager var, 3 panel referansi atanmis
- [ ] HUDController'da tum text ve slider referanslari atanmis
- [ ] LevelUpUI'da tum buton ve text referanslari atanmis
- [ ] GameOverUI'da text ve buton referanslari atanmis
- [ ] LevelUpPanel ve GameOverPanel DEAKTIF
- [ ] Sahne kaydedildi (Ctrl+S)

---

## BOLUM 8: Test (Play Mode)

1. **Play butonuna bas** (ust ortadaki ucgen)
2. Beklenen davranis:
   - 3 saniye bekleme (wave start delay)
   - Zombiler sag taraftan kirmizi kareler olarak cikip sola yurur
   - Okcu (mavi kare) otomatik ok (sari) atar
   - Oklarin zombilere isabet ettigini gor (zombi kaybolur)
   - **Sol mouse tikla** ile zombilere hasar ver
   - Duvar HP bar'i azalir (zombiler duvara ulasinca)
   - XP dolunca Level Up paneli acilir
   - Upgrade sec → oyun devam eder
   - Kale HP 0 olunca Game Over paneli acilir

### Sik Karsilasilan Sorunlar

**Zombiler spawn olmuyor:**
- Sub Scene icinde WaveConfigAuthoring var mi? Zombie Prefab atanmis mi?
- Console'da hata var mi? `RequireForUpdate` hatalari singleton eksikligine isaret eder.

**Ekranda hicbir sey gorunmuyor:**
- Camera orthographic mi? CameraSetup eklendi mi?
- Material'lar atandi mi?

**UI guncellenmiyor:**
- HUDController referanslari bos mu? Inspector'dan kontrol et.
- GameManager component eklendi mi?

**Level Up / Game Over acilmiyor:**
- UIManager referanslari (3 panel) atandi mi?
- Panel'ler Canvas'in child'i mi?

**"No singleton" hatasi:**
- Sub Scene duzenlemeden cikildi mi? Sub Scene "closed" modda olmali.
- GameState objesi Sub Scene ICINDE mi?
