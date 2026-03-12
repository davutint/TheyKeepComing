# Building Grid — Editor Setup Rehberi

## Sahne Hazirlik Adimlari

### Adim 1: Tilemap Layer'lar Ekle
1. Hierarchy'de **Grid** objesini sec (mevcut tilemap'lerin parent'i)
2. Sag tikla → **2D Object → Tilemap → Rectangular** → adi: `buildable_zone`
3. Ayni sekilde bir tane daha ekle → adi: `building_visuals`

### Adim 2: buildable_zone Doldur
1. `buildable_zone` Tilemap'i sec
2. Tile Palette'ten herhangi bir tile sec (renk/sekil onemsiz — sadece varligi onemli)
3. Sur ici bina yerlestirilecek alani tile ile doldur
4. **Tilemap Renderer** component'inde → **Sorting Layer** / **Order** ayarla (diger layer'larin altinda)
5. Runtime'da gorunmez yapmak icin: script'te veya `TilemapRenderer.enabled = false` Inspector'dan

### Adim 3: building_visuals Ayarla
1. `building_visuals` Tilemap'i sec
2. Bos birak — binalar yerlestirilince otomatik dolar
3. **Sorting Layer** / **Order**: bina sprite'lari gorunecek sekilde (ground ustunde)

### Adim 4: BuildingGridManager GameObject
1. Hierarchy'de bos GameObject olustur → adi: `BuildingGridManager`
2. `BuildingGridManager` script'ini ekle
3. Inspector'dan ayarla:
   - **Buildable Zone Tilemap**: `buildable_zone` tilemap'i surukle
   - **Building Visual Tilemap**: `building_visuals` tilemap'i surukle
   - **Grid Width**: 32 (veya harita boyutuna gore)
   - **Grid Height**: 32
   - **Grid Origin**: buildable_zone'un sol-alt kosesinin world pozisyonu (Vector3Int)
   - **Building Configs**: SO asset'lerini surukle (Farm_Config, House_Config vs.)

### Adim 5: BuildingPlacementUI GameObject
1. Hierarchy'de bos GameObject olustur → adi: `BuildingPlacementUI`
2. `BuildingPlacementUI` script'ini ekle
3. Inspector'dan ayarla:
   - **Building Button Panel**: bina butonlari iceren Canvas panel
   - **Button Container**: butonlarin parent Transform'u
   - **Building Button Prefab**: buton prefab'i (Text + Button + optional Icon)
   - **Ghost Renderer**: sahnede bos bir SpriteRenderer objesi olustur, assign et

### Adim 6: Ghost Preview Objesi
1. Hierarchy'de bos GameObject olustur → adi: `GhostPreview`
2. `SpriteRenderer` component'i ekle
3. Sorting Order: tum tile'larin ustunde
4. BuildingPlacementUI'daki **Ghost Renderer** alanina surukle

### Adim 7: UI Buton Panel
1. Canvas icinde yeni Panel olustur → adi: `BuildingButtonPanel`
2. Icine `HorizontalLayoutGroup` veya `GridLayoutGroup` ekle
3. Buton prefab'i olustur:
   - Button component
   - Child: Text (veya TMP_Text) → bina adi
   - Child (opsiyonel): Image → adi `Icon` (bina ikonu)
4. Prefab olarak kaydet, BuildingPlacementUI'a ata

## Grid Origin Belirleme
Grid Origin, tilemap koordinatlarini grid koordinatlarina cevirmek icin kullanilir:
- `buildable_zone`'un sol-alt kosesi world'de (X, Y) ise → `GridOrigin = (X, Y, 0)`
- Ornek: tilemap (-16, -16) ile basliyorsa → `GridOrigin = (-16, -16, 0)`

## SO Asset Olusturma
1. `Assets/ScriptableObject/` klasorune git
2. Sag tikla → **Create → DeadWalls → Building Config**
3. Her bina tipi icin 1 SO olustur
4. M1.3'te test icin 2 SO yeterli: `Farm_Config`, `House_Config`

## Test Proseduru

### Test 1: Buildable Zone
1. Play mode'a gir
2. Bina sec → buildable zone icinde yesil ghost gorunmeli
3. Zone disinda → kirmizi ghost
4. Zone icinde tikla → bina yerlessin
5. Zone disinda tikla → bina yerlesmemeli

### Test 2: 3x3 Overlap
1. Bir bina yerlestir
2. Ayni yere veya ustu uste gelecek sekilde ikinci bina koymaya calis
3. Kirmizi gosterilmeli, yerlestirmeye izin vermemeli

### Test 3: Maliyet Kontrolu
1. Kaynaklari tuketerek yetersiz hale getir
2. Bina secmaya calis → kirmizi gosterilmeli
3. Kaynak yeterse → yesil, yerlestirince kaynak azalmali

### Test 4: Restart
1. Birkaç bina yerlestir
2. Game Over → Restart
3. Grid sifirlanmali, bina entity'leri silinmeli, tilemap temizlenmeli

### Test 5: Iptal
1. Bina sec → ghost gorunsun
2. Sag tikla veya Escape → ghost kapansin, yerlestirme modu bitsin
