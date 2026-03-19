# Building Grid (Isometric) — Editor Setup Rehberi

## Sahne Hazirlik Adimlari

### Adim 1: Grid Objesini Izometrik Yap
> **KRITIK:** Bu adim tum child tilemap'leri etkiler. Grid objesinin CellLayout ayari
> child tilemap'lere otomatik inherit edilir.

1. Hierarchy'de **Grid** objesini sec (mevcut tilemap'lerin parent'i)
2. Inspector'da **Grid** component'ini bul
3. **Cell Layout** → **Isometric** olarak degistir
4. **Cell Size** → **(1, 0.5, 1)** yap
   - X=1: hucre genisligi (world unit)
   - Y=0.5: hucre yuksekligi (izometrik yarim yukseklik)
   - Z=1: derinlik (2D'de kullanilmaz ama 1 kalmali)
5. Tum child Tilemap'ler (buildable_zone, building_visuals, resource_zones, ground vb.) bu ayari otomatik inherit eder

### Adim 2: TilemapRenderer Sort Order Ayarla
> **Amac:** Izometrik gorunumde ust-saga yakin tile'lar altta, alt-sola yakin tile'lar
> ustte cizilmeli. Sort Order bunu saglar.

1. Her **TilemapRenderer** component'inde:
   - **Sort Order** → **Top Right** sec
   - Bu, izometrik derinlik siralmasini dogru yapar
2. **Order in Layer** degerleri (onerilen):
   - Ground layer: 0
   - `buildable_zone`: -10 (gorunmez, sadece logic)
   - `resource_zones`: 2 (overlay, baslangicta disabled)
   - `building_visuals`: 5 (binalarin uzerinde)

### Adim 3: Tilemap Layer'lar
> Mevcut tilemap'ler Rectangle idi — Adim 1'deki CellLayout degisikligi sonrasi
> otomatik Isometric olur. Yeni tilemap eklersen de Isometric inherit edilir.

**Mevcut layer'lar:**
1. `buildable_zone` — yerlestirilebilir alan (gorunmez mantik katmani)
2. `building_visuals` — bina sprite'lari (bos birak, kod doldurur)
3. `resource_zones` — dogal kaynak zone'lari (Orman/Tas/Demir)

**Yeni tilemap eklemek icin:**
1. Grid objesine sag tikla → **2D Object → Tilemap → Isometric**
   (veya herhangi bir Tilemap — parent'tan Isometric inherit eder)

### Adim 4: Tile Palette Olustur (Izometrik)
> **Amac:** Izometrik tile'lar diamond seklindedir. Palette'de bu sekilde gorunurler.

1. **Window → 2D → Tile Palette** penceresini ac
2. **Create New Palette** tikla → adi: `IsometricTerrain`
3. **Grid** → **Isometric** sec, **Cell Size** → **(1, 0.5)** yap
4. Tile asset'lerini palette'e surukle (Fantasy Kingdom Tileset iso tile'lari)
5. Boyama yaparken **Active Tilemap** dropdown'undan dogru layer'i sec

### Adim 5: buildable_zone Yeniden Boya
> **ONEMLI:** CellLayout degistikten sonra mevcut tile'lar yanlis pozisyonlarda olabilir.
> buildable_zone'u silip yeniden boyamak en guvenli yol.

1. `buildable_zone` Tilemap'i sec
2. **Tilemap → Clear All** ile mevcut tile'lari temizle (veya Inspector'da Tilemap component → Clear)
3. Tile Palette'den herhangi bir tile sec
4. **Active Tilemap** → `buildable_zone` sec
5. Surlarinin ic tarafindaki alani boya — diamond hucreleri dolduracaksin
6. `buildable_zone` → Inspector → **Tilemap Renderer**:
   - `enabled` checkbox'ini **kapat** (veya Order in Layer = -10)

### Adim 6: resource_zones Yeniden Boya
1. 3 farkli tile asset olustur (yoksa):
   - `ForestZoneTile` — yesil tonlarinda sprite
   - `StoneZoneTile` — gri tonlarinda sprite
   - `IronZoneTile` — kahverengi/turuncu sprite
2. **Active Tilemap** → `resource_zones` sec
3. Haritada ilgili alanlari boya (orman, tas, demir bolgeleri — diamond hucrelerde)
4. `resource_zones` → Inspector → **Tilemap Renderer**:
   - `enabled` checkbox'ini **kapat** (baslangicta gorunmez)
   - **Order in Layer**: 2

### Adim 7: BuildingGridManager Referanslari
> Bu referanslar zaten mevcut olmali. CellLayout degisikligi bunlari etkilemez.
> Sadece kontrol et.

1. `BuildingGridManager` objesini sec
2. Inspector'dan dogrula:
   - **Buildable Zone Tilemap**: `buildable_zone` tilemap atanmis mi
   - **Building Visual Tilemap**: `building_visuals` tilemap atanmis mi
   - **Resource Zone Tilemap**: `resource_zones` tilemap atanmis mi
   - **Forest/Stone/Iron Tile**: tile asset'leri atanmis mi
   - **Building Configs**: SO asset'leri atanmis mi

### Adim 8: Ghost Preview Objesi
> Mevcut GhostPreview objesi ayni kalir. Scale hesaplama kod tarafinda izometrik
> cell boyutlarina gore otomatik yapilir.

1. `GhostPreview` objesinin **SpriteRenderer** component'inin **Order in Layer** degerini kontrol et (en yuksek olmali, ornek: 100)

## Grid Origin, Width, Height
> Bu degerler `buildable_zone` tilemap'inin `cellBounds`'undan **otomatik hesaplanir**.
> Elle girmeye gerek yok. Play mode'a girince Inspector'da debug amacli gorebilirsin.
> CellLayout=Isometric'te cellBounds yine cell koordinatlarindadir (diamond).

## UI Kurulumu
> BuildingPlacementUI ve BuildingDetailUI mevcut UI kurulumu izometrik geciste degismiyor.
> Buton paneli, ghost preview referanslari ayni. Kod tarafinda WorldToGrid/GridToWorld
> otomatik izometrik math kullaniyor.

Mevcut UI baglantilari:
- **BuildingPlacementUI** → BuildingButtonPanel, ButtonContainer, BuildingButtonPrefab, GhostRenderer
- **BuildingDetailUI** → DetailPanel, text/slider/button referanslari

## Test Proseduru

### Test 1: Buildable Zone (Izometrik)
1. Play mode'a gir
2. Console'da `[BuildingGrid] Bounds:... | Buildable:...` log'unu kontrol et
3. UI panelden bina sec
4. Mouse'u diamond hucrelerin uzerinde gezdirmeli — ghost preview diamond'lara snap etmeli
5. Buildable zone icinde → **yesil ghost**
6. Zone disinda → **kirmizi ghost**

### Test 2: Ghost Snap (Diamond)
1. Bina sec, mouse'u yavas gezdirirken ghost'un hucre hucre atladigini gozle
2. Ghost, diamond hucrelerin merkezine oturmali (kose veya kenar degil)
3. Gizmo gorunurlugunu ac (Scene gorunumu) — sari diamond outline gorunmeli

### Test 3: Bina Yerlestirme
1. Zone icinde sol tikla → bina yerlessin
2. Binanin diamond grid'e dogru oturdugunuverify et
3. Ayni yere tekrar bina koymaya calis → kirmizi, yerlestirmeye izin vermemeli

### Test 4: Bina Tiklama (DetailUI)
1. Yerlestirme modu disinda bir binaya tikla
2. BuildingDetailUI dogru binayi gostermeli

### Test 5: Bina Yikma
1. Bina tikla → Detay panelinde yik butonuna bas
2. Grid temizlenmeli, gorsel silinmeli, %50 kaynak iade edilmeli

### Test 6: Zone Yakinlik
1. Kaynak binasi sec (Oduncu, Tascı vb.)
2. Zone overlay gorunmeli
3. Orman yakininda → yesil, uzaginda → kirmizi
4. Ciftlik sec → zone overlay gorunmemeli (RequiredZone = None)

### Test 7: Gizmo Diamond Outline
1. Scene gorunumunde Gizmos toggle'ini ac
2. Bina sec → mouse hareket ettirirken diamond outline gorunmeli
3. Dis ceper sari, ic hucreler yari-saydam sari olmali
