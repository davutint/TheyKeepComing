# Building Grid — Editor Setup Rehberi

## Sahne Hazirlik Adimlari

### Adim 1: Tilemap Layer'lar Ekle
1. Hierarchy'de **Grid** objesini sec (mevcut tilemap'lerin parent'i)
2. Sag tikla → **2D Object → Tilemap → Rectangular** → adi: `buildable_zone`
3. Ayni sekilde bir tane daha ekle → adi: `building_visuals`
4. Bir tane daha ekle → adi: `resource_zones` (dogal kaynak zone'lari icin)

### Adim 2: buildable_zone Doldur
> **Amac:** `buildable_zone` gorunmez bir mantik katmanidir. Kod tarafinda BuildingGridManager bu
> tilemap'i okuyarak "buraya bina yerlestirilebilir mi?" kontrolu yapar.
> Tile olan hucre = yerlestirilebilir, bos hucre = yerlestirilemez.
> Bu yuzden hangi tile oldugu onemsiz — sadece dolu olmasi yeterli.

1. **Window → 2D → Tile Palette** penceresini ac
2. Palette'den herhangi bir tile sec (rengi, gorseli farketmez)
3. Brush araci ile **surlarinin ic tarafindaki alani boya** — zombilerin olmadigi, oyuncunun bina yapabilecegi bolgeyi tile'larla doldur
4. `buildable_zone` Tilemap'i sec → Inspector'da **Tilemap Renderer** component'ini bul
5. Oyuncu bu tile'lari gormemeli, sadece logic icin var. Iki yol:
   - **Sorting Order** diger tum layer'larin altina cek, VEYA
   - `TilemapRenderer.enabled = false` yaparak tamamen gizle

### Adim 3: building_visuals Ayarla
> **Amac:** `building_visuals` binalarin gorsellerinin cizildigi katmandir.
> Oyuncu bina yerlestirdiginde kod bu tilemap'e otomatik olarak bina tile'ini koyar.
> Sen bu tilemap'e hicbir sey koymayacaksin — bos birakilacak.

1. `building_visuals` Tilemap'i sec
2. Icerigini bos birak — binalar yerlestirilince kod otomatik doldurur
3. Inspector'da **Tilemap Renderer → Order in Layer** degerini ground layer'indan yuksek yap (ornek: ground=0 ise building_visuals=5)

### Adim 3b: resource_zones Zone Boyama
> **Amac:** `resource_zones` dogal kaynak zone'larini tanimlayan katmandir. Hangi bolgelerde
> hangi kaynak binasi yerlestirilebilir kontrolu icin kullanilir. Zone gerektiren bina
> secildiginde overlay olarak gorunur olur.

1. 3 farkli tile asset olustur (Project → Create → 2D → Tiles → Tile):
   - `ForestZoneTile` — yesil tonlarinda sprite
   - `StoneZoneTile` — gri tonlarinda sprite
   - `IronZoneTile` — kahverengi/turuncu sprite
2. **Window → 2D → Tile Palette** → yeni palette: `ResourceZones`
3. **Active Tilemap** olarak `resource_zones` sec
4. Haritada ilgili alanlari boya (orman, tas, demir bolgeleri)
5. `resource_zones` Tilemap → Inspector → **Tilemap Renderer**:
   - `enabled` checkbox'ini **kapat** (baslangicta gorunmez, kod gerektiginde acar)
   - **Order in Layer**: ground uzerinde, building_visuals altinda (ornek: 2)

### Adim 4: BuildingGridManager GameObject
1. Hierarchy'de bos GameObject olustur → adi: `BuildingGridManager`
2. `BuildingGridManager` script'ini ekle
3. Inspector'dan ayarla:
   - **Buildable Zone Tilemap**: `buildable_zone` tilemap'i surukle
   - **Building Visual Tilemap**: `building_visuals` tilemap'i surukle
   - **Resource Zone Tilemap**: `resource_zones` tilemap'i surukle
   - **Forest Tile**: `ForestZoneTile` asset'ini surukle
   - **Stone Tile**: `StoneZoneTile` asset'ini surukle
   - **Iron Tile**: `IronZoneTile` asset'ini surukle
   - **Building Configs**: SO asset'lerini surukle (Farm_Config, House_Config vs.)
> **Not:** GridWidth, GridHeight ve GridOrigin otomatik hesaplanir — `buildable_zone` tilemap'ini
> boyadigin an cellBounds'tan alinir. Elle girmeye gerek yok, Inspector'da sadece debug icin gorunur.

### Adim 5: Ghost Preview Objesi
> **Amac:** Ghost preview, bina yerlestirme sirasinda fare imlecini takip eden yari-saydam onizleme gorseli.
> Yesil = buraya koyabilirsin, kirmizi = koyamazsin. Yerlestirme tamamlaninca kaybolur.

1. Hierarchy'de bos GameObject olustur → adi: `GhostPreview`
2. **Add Component → SpriteRenderer** ekle
3. Inspector'da **Order in Layer** degerini en yuksek yap (ornek: 100) — her seyin ustunde gorunmeli
4. Bu objeye dokunma, Adim 7'de BuildingPlacementUI'a atanacak

### Adim 6: UI Buton Panel
> **Amac:** Ekranin altinda/yaninda bina secim butonlari. Oyuncu bir butona tiklar,
> sonra haritada istdigi yere tiklayarak binayi yerlestirir.

1. Sahnede zaten bir **Canvas** yoksa olustur (Hierarchy → UI → Canvas)
2. Canvas icinde sag tikla → **UI → Panel** → adi: `BuildingButtonPanel`
3. Panel'e **Add Component → Horizontal Layout Group** (veya Grid Layout Group) ekle
4. Buton prefab'i olustur:
   - Canvas icinde sag tikla → **UI → Button** → adi: `BuildingButton`
   - Button'un child'indaki **Text** (veya TMP_Text) → bina adini yazacak (kod otomatik set eder)
   - (Opsiyonel) Button'a child **Image** ekle → adi `Icon` (bina ikonu icin)
5. Bu Button'u Project paneline surukleyerek **prefab** olarak kaydet
6. Sahnedeki ornek Button'u sil (prefab yeterli)

### Adim 7: BuildingPlacementUI GameObject
> **Not:** Bu adim Adim 5 ve 6'da olusturdugumuz objeleri baglar. Onlari once olusturman gerekiyor.

1. Hierarchy'de bos GameObject olustur → adi: `BuildingPlacementUI`
2. `BuildingPlacementUI` script'ini ekle
3. Inspector'dan ayarla:
   - **Building Button Panel**: Adim 6'da olusturdugum `BuildingButtonPanel` panel'ini surukle
   - **Button Container**: `BuildingButtonPanel`'in kendi Transform'u (butonlar bunun child'i olacak)
   - **Building Button Prefab**: Adim 6'da kaydettidign buton prefab'ini Project panelinden surukle
   - **Ghost Renderer**: Adim 5'teki `GhostPreview` objesinin **SpriteRenderer** component'ini surukle

## Grid Origin, Width, Height
> Bu degerler `buildable_zone` tilemap'inin `cellBounds`'undan **otomatik hesaplanir**.
> Elle girmeye gerek yok. Play mode'a girince Inspector'da debug amacli gorebilirsin.

## SO Asset Olusturma
1. `Assets/ScriptableObject/` klasorune git
2. Sag tikla → **Create → DeadWalls → Building Config**
3. Her bina tipi icin 1 SO olustur
4. M1.3'te test icin 2 SO yeterli: `Farm_Config`, `House_Config`

## Test Proseduru

> **Bina secmek:** Play mode'da ekranin altindaki/yanindaki **BuildingButtonPanel**'deki
> butonlara tikla (ornek: "Ciftlik", "Ev"). Butona tiklayinca yerlestirme modu baslar
> ve fare imlecini takip eden yari-saydam ghost onizleme gorunur.
> Yerlestirmeyi iptal etmek icin **sag tikla** veya **Escape** bas.

### Test 1: Buildable Zone
1. Play mode'a gir
2. UI panelden bir bina butonuna tikla (ornek: "Ciftlik")
3. Fare imlecini buildable zone icine getir → **yesil ghost** gorunmeli
4. Fare imlecini zone disina cikar → **kirmizi ghost** gorunmeli
5. Zone icinde sol tikla → bina yerlessin
6. Zone disinda sol tikla → bina yerlesmemeli

### Test 2: 3x3 Overlap
1. UI panelden bina sec, zone icine yerlestir
2. Ayni yere veya ustu uste gelecek sekilde tekrar bina koymaya calis
3. Kirmizi gosterilmeli, yerlestirmeye izin vermemeli

### Test 3: Maliyet Kontrolu
1. Birden fazla bina yerlestirerek kaynaklari tuket
2. Kaynak yetersizken bina sec → ghost **kirmizi** gorunmeli
3. Kaynak yeterliyken → ghost **yesil**, yerlestirince kaynak azalmali

### Test 4: Restart
1. Birkac bina yerlestir
2. Game Over → Restart
3. Grid sifirlanmali, bina entity'leri silinmeli, tilemap temizlenmeli

### Test 5: Iptal
1. UI panelden bina sec → ghost gorunsun
2. Sag tikla veya Escape → ghost kapansin, yerlestirme modu bitsin

### Test 6: Zone Yakinlik Kontrolu
1. `resource_zones` tilemap'e orman/tas/demir zone'lari boya
2. SO asset'lerde RequiredZone ayarla (Lumberjack→Forest, Quarry→Stone, Mine→Iron)
3. Play mode'da Oduncu sec → zone overlay gorunur
4. Orman yakininda → yesil ghost, uzaginda → kirmizi ghost
5. Ciftlik sec → zone overlay gorunmez (RequiredZone = None)
6. Yerlestir → kaynak duser, bina olusur
