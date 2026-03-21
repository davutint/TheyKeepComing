# Building Tile Composer — Editor Setup & Kullanim Kilavuzu

## On Kosullar
- Unity 6 LTS + 2D Tilemap Extras paketi kurulu
- Fantasy Kingdom Tileset import edilmis (128x256px tile'lar)
- BuildingConfigSO asset'leri olusturulmus

---

## Adim 1: Scene'e Top Tilemap Ekle

1. **Hierarchy** → Grid objesini bul (building_visuals'in parent'i)
2. Grid'e **sag tik → 2D Object → Tilemap → Isometric**
3. Yeni tilemap'i `building_top` olarak adlandir
4. `building_top` TilemapRenderer ayarlari:
   - **Sort Order**: varsayilan
   - **Order in Layer**: `6`
   - **Mode**: Individual (mevcut grid ile ayni)
5. Mevcut `building_visuals` tilemap'in **Order in Layer**: `5` oldugunu dogrula

## Adim 2: BuildingGridManager'a Top Tilemap Ata

1. **Hierarchy** → BuildingGridManager objesini sec
2. **Inspector** → `Building Top Tilemap` field'ina `building_top` tilemap'i surukleet

## Adim 3: Tile Composer'i Ac

1. **Window → DeadWalls → Building Tile Composer**
2. Pencere acilir — dock edebilirsin

## Adim 4: Bina Tile Layout'u Olustur

### 4a. Config Sec
- "Config SO" field'ina bir `BuildingConfigSO` asset surukle
- Grid boyutu ve bina adi otomatik gosterilir
- Mevcut tile layout varsa otomatik yuklenir

### 4a2. Top Grid Boyutu Ayarla (cati icin zorunlu)
- **Top Grid W/H**: Cati layer'inin grid boyutu (0 = base ile ayni)
- **Top Grid OX/OY**: Cati'nin duvara gore offset'i (grid hucre cinsinden)
- Ornek: Base 3x3, Top W=3 H=3, OX=0 OY=2 → cati duvarlardan 2 satir yukarida
- Boyut degistiginde mevcut top tile atamalari sifirlanir (yeni bos grid olusur)

### 4b. Layer Sec
- **Base (duvar/zemin)**: Binanin alt kismi — duvarlar, kapilar, zemin
- **Top (cati/detay)**: Binanin ust kismi — catitar, bayraklar, dekorasyon

### 4c. Tile Sec
- "Secili Tile" field'ina kullanmak istedigin Tile asset'ini surukle
- Project panelinden Tile asset'leri:
  - Fantasy Kingdom Tileset → Environment → Sprites icerisinde
  - Veya kendi olusturdugun Rule Tile / Animated Tile

### 4d. Grid'e Tile Ata
- **Sol tik**: Secili layer'a secili tile'i atar
- **Sag tik**: Secili layer'dan tile siler
- Mouse hover ile hucre mavi olarak vurgulanir
- Her hucrede koordinat etiketi (x,y) gorunur

### 4e. Preview Kontrol
- Preview bolumu **RenderTexture** ile gercek izometrik render uretir
- Unity'nin kendi Tilemap renderer'ini kullanan, sahnedekiyle ayni gorunum
- Tile atandikca veya silinince otomatik guncellenir (dirty flag sistemi)
- Tum tile'lar bosaltilirsa preview kaybolur, yeni tile ataninca tekrar olusur

### 4f. Kaydet
- **"Save to SO"** butonuna tikla
- SO'ya kaydedilir (Undo destegi var)
- Console'da onay mesaji cikar

### 4g. Temizle (opsiyonel)
- **"Clear All"** butonu tum tile atamalari siler
- Onay dialogu cikar

---

## Sorun Giderme

### Preview bos gorunuyor
- En az bir tile atanmis mi? Tum hucreler bossa preview olusturulmaz
- Tile asset'inin bir sprite'i atanmis mi kontrol et
- Window kapatip tekrar acmayi dene (OnDisable cleanup + OnEnable yeniden yukleme)

### Runtime'da tile'lar gorunmuyor
- BuildingGridManager Inspector'da `Building Top Tilemap` atanmis mi?
- TileLayoutBase array boyutu `GridWidth * GridHeight` ile eslesmi mi?
- SO'yu **Save to SO** ile kaydettiniz mi?

### Eski binalar hala tek tile gosteriyor
- TileLayout olmayan SO'lar otomatik olarak eski GhostSprite fallback'ini kullanir
- Composer ile layout atayip kaydedin

### Tile atanmiyor (tiklama calismadi)
- Bir tile secili mi kontrol et (Secili Tile field'i dolu olmali)
- Dogru layer secili mi? (Base vs Top)

---

## Ornek Akis: Oduncu Binasi (3x3)

1. Composer'i ac, `Lumberjack` SO'sunu sec
2. Layer: **Base** sec
3. Duvar tile'i sec → alt satirlara (y=0, y=1) tikla
4. Kapi tile'i sec → (1,0) hucreye tikla (ortadaki alt)
5. Layer: **Top** sec
6. Cati tile'i sec → ust satirlara (y=2) tikla
7. "Save to SO" tikla
8. Play modunda test et — bina yerlestirince 2 katmanli gorunum!
