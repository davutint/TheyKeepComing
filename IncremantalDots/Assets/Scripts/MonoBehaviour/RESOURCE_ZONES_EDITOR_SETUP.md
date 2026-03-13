# Resource Zones (Dogal Kaynak Noktalari) — Editor Setup Rehberi

## Sahne Hazirlik Adimlari

### Adim 1: resource_zones Tilemap Layer Olustur
1. Hierarchy'de **Grid** objesini sec (mevcut tilemap'lerin parent'i)
2. Sag tikla → **2D Object → Tilemap → Rectangular** → adi: `resource_zones`
3. Inspector'da **Tilemap Renderer** component'ini bul:
   - **Sorting Layer**: diger layer'larla ayni (ornek: Default)
   - **Order in Layer**: ground uzerinde ama building_visuals altinda (ornek: 2)
   - **enabled = false** yaparak baslangicta gizle (kod gerektiginde acar)

### Adim 2: Zone Tile Asset'leri Olustur
> Her zone tipi icin farkli renkte tile asset olusturulur. Basit renkli sprite'lar yeterli.

1. **Project** panelinde `Assets/Tiles/` klasorune git (yoksa olustur)
2. Sag tikla → **Create → 2D → Tiles → Rule Tile** veya **Tile** → 3 adet olustur:
   - `ForestZoneTile` — yesil tonlarinda sprite ata
   - `StoneZoneTile` — gri tonlarinda sprite ata
   - `IronZoneTile` — kahverengi/turuncu tonlarinda sprite ata
3. Her tile icin uygun sprite sec (yari saydam overlay olarak gorunecek)

### Adim 3: Tile Palette ile Zone Boyama
1. **Window → 2D → Tile Palette** penceresini ac
2. Yeni palette olustur: **Create New Palette** → adi: `ResourceZones`
3. Olusturdugum 3 tile asset'i palette'e surukle
4. **Active Tilemap** olarak `resource_zones` sec
5. Haritada ilgili alanlari boya:
   - Orman alanlari → `ForestZoneTile` ile
   - Tas alanlari → `StoneZoneTile` ile
   - Demir alanlari → `IronZoneTile` ile
6. Zone'larin buildable_zone ile kesistigi veya yakininda olmasi gerekiyor — bina yerlestirilebilir alan yakininda olmayan zone'lar anlamsizdir

### Adim 4: TilemapRenderer Baslangic Ayarlari
1. `resource_zones` Tilemap'i sec
2. Inspector → **Tilemap Renderer**:
   - `enabled` checkbox'ini **kapat** (baslangicta gorunmez)
   - Kod, zone gerektiren bina secildiginde otomatik acar

### Adim 5: BuildingGridManager Inspector — Zone Field'larini Ata
1. Sahnedeki `BuildingGridManager` GameObject'ini sec
2. Inspector'da **Dogal Kaynak Zone'lari** header'ini bul
3. Asagidaki field'lari ata:
   - **Resource Zone Tilemap**: `resource_zones` tilemap'i surukle
   - **Forest Tile**: Adim 2'de olusturdugum `ForestZoneTile` asset'ini surukle
   - **Stone Tile**: `StoneZoneTile` asset'ini surukle
   - **Iron Tile**: `IronZoneTile` asset'ini surukle

### Adim 6: SO Asset'lerde Zone Gereksinimini Ayarla
1. `Assets/ScriptableObject/` klasorundeki ilgili SO'lari ac:

| SO Asset | Required Zone | Zone Proximity Radius |
|----------|--------------|----------------------|
| Lumberjack_Config | Forest | 3 |
| Quarry_Config | Stone | 3 |
| Mine_Config | Iron | 3 |
| Farm_Config | None | (yoksayilir) |
| House_Config | None | (yoksayilir) |
| Barracks_Config | None | (yoksayilir) |
| Fletcher_Config | None | (yoksayilir) |

> `RequiredZone = None` olan binalarda `ZoneProximityRadius` etkisiz — varsayilan 3 birakilabilir.

## Test Proseduru

### Test 1: Zone Yakinlik Kontrolu
1. Play mode'a gir
2. **Oduncu** butonuna tikla → zone overlay gorunur olmali
3. Fare imlecini **orman zone'una yakin** bir buildable alana getir → **yesil ghost**
4. Fare imlecini **orman zone'undan uzak** bir buildable alana getir → **kirmizi ghost**
5. Yesil alanda tikla → bina yerlessin
6. Kirmizi alanda tikla → bina yerlesmemeli

### Test 2: Zone Gerektirmeyen Bina
1. **Ciftlik** butonuna tikla → zone overlay **gorunmemeli**
2. Herhangi bir buildable alanda yerlestirilebilmeli (zone kisitlamasi yok)

### Test 3: Overlay Gorunurluk
1. **Oduncu** sec → overlay gorunur
2. **Sag tikla** (iptal) → overlay gizlenir
3. **Ciftlik** sec → overlay gizli kalir
4. **Oduncu** sec → overlay tekrar gorunur
5. Yerlestir → overlay gizlenir

### Test 4: Restart
1. Birkac kaynak binasi yerlestir
2. Game Over → Restart
3. Zone tile'lari korunmali (silinmemeli)
4. Binalar sifirlanmali
