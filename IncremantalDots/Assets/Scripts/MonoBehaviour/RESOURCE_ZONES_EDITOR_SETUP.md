# Resource Zones (Dogal Kaynak Noktalari) — Editor Setup Rehberi

> Bu rehber Tilemap bilgisi gerektirmeden adim adim takip edilebilir sekilde yazilmistir.

---

## Adim 1: resource_zones Tilemap Layer Olustur

Tilemap, Unity'de kare kare (hucre hucre) gorsel boyama yapmani saglayan bir sistemdir. Senin projende zaten bir **Grid** objesi var — `buildable_zone` ve `building_visuals` gibi tilemap'ler onun altinda duruyor. Simdi ayni Grid'in altina bir tane daha tilemap ekleyeceksin: `resource_zones`.

1. **Hierarchy** panelini ac (genellikle sol tarafta, sahnedeki tum objelerin listesi)
2. Hierarchy'de **Grid** objesini bul — bu, mevcut tilemap'lerin parent'i olan objedir. Sol tarafindaki oku tiklayarak altindaki child'lari gorebilirsin (`buildable_zone`, `building_visuals` vs.)
3. **Grid** objesine **sag tikla** → acilan menude **2D Object** → **Tilemap** → **Rectangular** seceneklerini takip et
4. Yeni bir tilemap olusacak ve adi `Tilemap` gibi genel bir isimle gelecek. Bu ismi degistirmen gerekiyor:
   - Hierarchy'de yeni olusturulan tilemap'e **tek tikla** (secili hale gelir)
   - **F2** tusuna bas (veya tekrar yavasca tikla) → isim duzenlenebilir hale gelir
   - Yaz: `resource_zones` → **Enter**'a bas
5. Simdi bu tilemap secili haldeyken sag taraftaki **Inspector** paneline bak. Orada iki component goreceksin:
   - **Tilemap** (veri katmani — hangi hucrede hangi tile var)
   - **Tilemap Renderer** (gorsel katman — ekranda nasil cizilecek)
6. **Tilemap Renderer** component'inde su ayarlari yap:
   - **Sorting Layer**: `Default` olarak birak (veya diger tilemap'lerinle ayni ne ise o)
   - **Order in Layer**: `2` yap. Bu sayi, bu katmanin diger katmanlarin ustunde mi altinda mi cizilecegini belirler. Ground (zemin) genellikle 0'dir, bu yuzden 2 yapinca zemin ustunde gorunur ama binalarla karistirilmaz.
   - **Enabled** checkbox'u: Bu checkbox, Tilemap Renderer component basliginin hemen solundaki kutucuktur. **Isareti kaldir** (tikla, bos kare olsun). Boylece oyun basladiginda bu katman gorunmez olur — kod, zone gerektiren bir bina secildiginde bunu otomatik acar.

> **Neden gizliyoruz?** Oyuncu surekli yerde renkli zone'lar gormemeli. Sadece orman gerektiren bir bina (ornegin Oduncu) sectiginde "buraya koyabilirsin" diye overlay acilir.

---

## Adim 2: Zone Tile Sprite'larini Olustur

Her zone tipini temsil edecek sprite'lara ihtiyacin var. Bunlar yari saydam, basit renkli kareler olacak — fancy bir sey olmasina gerek yok.

### 2a: Sprite Dosyalarini Hazirla

En basit yol: herhangi bir gorsel editorde (Paint, Photoshop, GIMP, hatta Paint 3D) **32x32 piksel** boyutunda 3 adet PNG dosyasi olustur:

| Dosya Adi | Renk | Anlami |
|-----------|------|--------|
| `forest_zone.png` | Yesil (ornek: #00FF0080 — yari saydam yesil) | Orman alani |
| `stone_zone.png` | Gri (ornek: #80808080 — yari saydam gri) | Tas alani |
| `iron_zone.png` | Kahverengi/turuncu (ornek: #8B450080 — yari saydam kahverengi) | Demir alani |

> **Onemli:** Rengin son 2 hanesi (`80`) saydamlik belirler. `FF` = tamamen opak, `80` = yarim saydam, `00` = tamamen gorunmez. Yarim saydam yapmayi tercih et ki alttaki zemin gorunsun.

**Alternatif:** Sprite olusturmak istemiyorsan, Unity'nin dahili **Knob** veya **UISprite** sprite'larini da kullanabilirsin (Adim 2c'de anlatiliyor).

### 2b: Sprite'lari Unity'ye Aktar

1. Olusturdugun 3 PNG dosyasini **dosya gezgininden** (Windows Explorer) surukleyip Unity'nin **Project** panelindeki `Assets/Sprites/` klasorune birak. (`Assets/Sprites/` yoksa once olustur: Project panelinde Assets'e sag tikla → Create → Folder → `Sprites`)
2. Unity dosyalari import edecek. Her birinin uzerine tikla ve **Inspector**'da su ayarlari yap:
   - **Texture Type**: `Sprite (2D and UI)` (muhtemelen zaten boyledir)
   - **Pixels Per Unit**: `32` — bu deger, 1 grid hucresinin kac piksel oldugunu belirler. Grid hucre boyutun 1x1 ise ve sprite'in 32x32 piksel ise, 32 yazmalisin ki 1 hucreye tam otursun.
   - **Filter Mode**: `Point (no filter)` — piksel art tarzi temiz gorunum icin
   - **Compression**: `None`
   - En altta **Apply** butonuna tikla

> **Pixels Per Unit neden onemli?** Eger sprite'in 32x32 piksel ama PPU=100 (varsayilan) ise, sprite 0.32 birim buyuklugunde cizilir ve grid hucresini doldurmaz. PPU=32 yapinca 32/32=1 birim olur, grid hucresine tam oturur.

### 2c: Alternatif — Sprite Olusturmadan Dahili Sprite Kullanma

Eger PNG olusturmak istemiyorsan:
1. Adim 3'te tile olustururken sprite olarak Unity'nin dahili beyaz karesini kullanabilirsin
2. Tile'in **Color** alaninda renk ve saydamlik ayarlayabilirsin (asagida anlatiliyor)

---

## Adim 3: Tile Asset'leri Olustur

Tile asset, "bu hucreye ne cizilsin" bilgisini tutan bir dosyadir. Her zone tipi icin bir tane olusturacaksin.

1. **Project** panelinde `Assets/Tiles/` klasorune git. Yoksa olustur:
   - Project panelinde `Assets` klasorune sag tikla → **Create** → **Folder** → adi: `Tiles` → Enter
2. `Assets/Tiles/` klasorune sag tikla → **Create** → **2D** → **Tiles** → **Tile**
   - Yeni dosya olusacak, adi `New Tile` gibi gelir
   - Adini `ForestZoneTile` yap (F2 ile veya yavasca tiklayarak)
3. `ForestZoneTile`'a tikla → Inspector'da su alanlari goreceksin:
   - **Sprite**: Sag taraftaki kucuk yuvarlak ikona tikla (asset picker acilir) → `forest_zone` sprite'ini sec. Veya dogrudan Project panelinden sprite'i bu alana surukle.
   - **Color**: Beyaz (1,1,1,1) birakilabilir. Eger dahili sprite kullaniyorsan burada rengi ayarla: yesil yap ve Alpha (A) degerini 0.5 yap (yari saydam).
4. Ayni islemi 2 kez daha tekrarla:

| Tile Asset Adi | Sprite | Color (dahili sprite kullaniyorsan) |
|----------------|--------|--------------------------------------|
| `ForestZoneTile` | `forest_zone` | Yesil, A=0.5 |
| `StoneZoneTile` | `stone_zone` | Gri, A=0.5 |
| `IronZoneTile` | `iron_zone` | Kahverengi, A=0.5 |

> Sonucta `Assets/Tiles/` klasorunde 3 tile asset'in olmali: `ForestZoneTile`, `StoneZoneTile`, `IronZoneTile`.

---

## Adim 4: Tile Palette Olustur ve Tile'lari Ekle

Tile Palette, "boya fircasi paleti" gibi dusun — icine tile'larini koyuyorsun, sonra sahneye boyamak icin paletten seciyorsun.

### 4a: Tile Palette Penceresini Ac

1. Unity ust menude: **Window** → **2D** → **Tile Palette** tikla
2. Yeni bir pencere acilir. Bu pencereyi Editor'de istedigin yere dock edebilirsin (Scene veya Inspector yanina suruklemenizi oneririm).

### 4b: Yeni Palette Olustur

1. Tile Palette penceresinin ust kisminda bir dropdown var (muhtemelen "No Valid Palette" veya mevcut bir palette adi yazar)
2. Bu dropdown'un yanindaki **Create New Palette** butonuna tikla
3. Bir dialog acilir:
   - **Name**: `ResourceZones` yaz
   - **Grid**: `Rectangle` (varsayilan, degistirme)
   - **Cell Size**: `Automatic` (varsayilan, degistirme)
   - **Create** butonuna tikla
4. Senden bir klasor secmeni ister — `Assets/Tiles/` klasorunu sec → **Select Folder** veya **Save**
5. Palette olusturuldu ve Tile Palette penceresinde `ResourceZones` secili gorunecek

### 4c: Tile Asset'leri Palette'e Ekle

1. **Project** panelinden `Assets/Tiles/ForestZoneTile` dosyasini surukle → **Tile Palette penceresinin** icindeki bos gridalana birak
2. Tile, palette'te bir hucreye yerlesecek
3. Ayni sekilde `StoneZoneTile` ve `IronZoneTile`'i da palette'e surukle
4. Simdi palette'te 3 tile gorunmeli — her biri farkli renkte

> **Not:** Tile'lari palette'e eklerken Unity bir sprite asset olusturmak isteyebilir — eger sorarsa `Assets/Tiles/` klasorunu sec.

---

## Adim 5: Haritada Zone'lari Boya

Simdi asil boyama asamasi — haritandaki ormanlarin, tas alanlarinin ve demir alanlarinin oldugu yerleri boyayacaksin.

### 5a: Dogru Tilemap'i Sec

Bu CIDDEN ONEMLI — yanlis tilemap'e boyarsan baska katmani bozarsin.

1. **Tile Palette** penceresinin ust kisminda **Active Tilemap** yazisi var ve yaninda bir dropdown
2. Bu dropdown'a tikla → listeden **`resource_zones`** sec
3. Eger listede goremiyorsan: Hierarchy'de `resource_zones` tilemap'inin **Grid** objesinin altinda oldugundan emin ol (Adim 1)

### 5b: Brush (Firca) Sec ve Boya

1. Tile Palette penceresinde boyamak istedigin tile'a tikla (ornek: `ForestZoneTile`)
   - Secili tile vurgulanir
2. Palette'in sol tarafinda kucuk ikonlar var — bunlar araclar:
   - **Firca ikonu** (boya fircasi): Hucre hucre boyar. En cok kullanacagin bu.
   - **Dikdortgen ikonu** (kutu secim): Tikla-surukle ile dikdortgen alan boyar. Genis alanlar icin hizli.
   - **Silgi ikonu**: Boyadigin hucreleri siler.
3. **Firca ikonunu** sec (veya klavyeden `B` tusuna bas)
4. Imlecini **Scene** gorunumune getir — imlecin artik bir grid hucresini vurgular
5. **Tiklayarak** veya **tikla-surukle** ile ormanlarin oldugu alanlari boya

### 5c: Nereyi Boyamalisin?

Haritana bagli ama genel kural:

- **Orman zone'lari (ForestZoneTile)**: Oduncu binasi yerlestirilecek alanlarin **yakininda**. Buildable zone'un kenarinda veya hemen disinda bir alan sec, 5-10 hucrelik bir orman alani boya.
- **Tas zone'lari (StoneZoneTile)**: Tas Ocagi icin, farkli bir bolgeye tas alani boya.
- **Demir zone'lari (IronZoneTile)**: Maden icin, yine farkli bir bolgeye demir alani boya.

> **KRITIK:** Zone tile'lari `buildable_zone` ile cakismak zorunda DEGIL. Zone, "burada dogal kaynak var" demek. Bina ise `buildable_zone` uzerine yerlesir. Kod, binanin zone'a **yakin** olup olmadigini kontrol eder (`ZoneProximityRadius` kadar mesafe). Yani zone'u buildable zone'un hemen yanina/disina boyayabilirsin.

### 5d: Boyamayi Kontrol Et

1. Boyamadan sonra `resource_zones` tilemap'ini secip Inspector'daki **Tilemap Renderer** component'inin `enabled` checkbox'unu gecici olarak ac → Scene'de zone'lari gorebilirsin
2. Dogru gorunuyorsa checkbox'u tekrar kapat (baslangicta gizli olmasi gerekiyor)

---

## Adim 6: BuildingGridManager Inspector — Zone Field'larini Ata

Bu adimda, kodun zone tile'larini tanimasi icin referanslari baglayacaksin.

1. **Hierarchy** panelinde `BuildingGridManager` objesini bul ve tikla
2. **Inspector** panelinde `BuildingGridManager (Script)` component'ini bul
3. Asagi kaydir — **Dogal Kaynak Zone'lari** (veya `Resource Zone Settings`) baslikli bir bolum goreceksin. Burada 4 alan var:

| Inspector Field Adi | Ne Surukleyeceksin | Nereden |
|---------------------|--------------------|---------|
| **Resource Zone Tilemap** | `resource_zones` | Hierarchy'den `resource_zones` objesini bu alana surukle |
| **Forest Tile** | `ForestZoneTile` | Project panelinden `Assets/Tiles/ForestZoneTile` dosyasini surukle |
| **Stone Tile** | `StoneZoneTile` | Project panelinden `Assets/Tiles/StoneZoneTile` dosyasini surukle |
| **Iron Tile** | `IronZoneTile` | Project panelinden `Assets/Tiles/IronZoneTile` dosyasini surukle |

> **Surukle-birak yontemi:** Sol el ile surukleyecegin seyi tikla-basili tut, birakma, sag tarafa (Inspector'daki alana) gotur, uygun alanin ustunde birak. Alan mavi vurgu alirsa dogru yere birakiyorsun demektir.

> **Alternatif yontem:** Her alanin sag tarafindaki kucuk **yuvarlak ikon** (asset picker) a tikla → acilan pencerede ismiyle ara ve sec.

---

## Adim 7: SO Asset'lerde Zone Gereksinimini Ayarla

Kaynak binalari (Oduncu, Tas Ocagi, Maden) dogru zone'a yakin olmadan yerlestirilemez. Bu ayari ScriptableObject asset'lerinde yapiyorsun.

1. **Project** panelinde `Assets/ScriptableObject/` klasorune git
2. Asagidaki tabloya gore her SO'yu tikla ve Inspector'da ayarla:

| SO Asset Adi | Required Zone | Zone Proximity Radius |
|--------------|--------------|----------------------|
| `Lumberjack_Config` (veya `BuildingConfig_Lumberjack`) | **Forest** | **3** |
| `Quarry_Config` (veya `BuildingConfig_Quarry`) | **Stone** | **3** |
| `Mine_Config` (veya `BuildingConfig_Mine`) | **Iron** | **3** |

3. Her SO icin:
   - SO asset'ine tikla → Inspector acilir
   - **Required Zone** alanini bul → dropdown'dan ilgili zone tipini sec (Forest, Stone veya Iron)
   - **Zone Proximity Radius** alanini bul → `3` yaz (bina, zone'a en fazla 3 hucre uzaklikta olmali)

4. Diger binalar (Farm, House, Barracks, Fletcher, Blacksmith) icin **Required Zone = None** birakilmali — zaten varsayilan degeri budur, dokunmana gerek yok.

> **Zone Proximity Radius = 3 ne demek?** Bina yerlestirilecegi zaman, binanin merkezinden 3 hucre yaricapinda en az 1 tane ilgili zone tile'i olmali. Yoksa kirmizi ghost gosterilir ve yerlestirme engellenir.

---

## Test Proseduru

Tum setup'u yaptiktan sonra bu testlerle dogru calisip calismadigini kontrol et.

### Test 1: Zone Overlay Gorunurlugu
1. **Play** butonuna bas (oyunu baslat)
2. Bina menusunden **Oduncu** (Lumberjack) butonuna tikla
3. Ekranda zone overlay'i (yesil alanlar) gorunmeli
4. **Sag tikla** veya **Escape** bas (bina secimi iptal)
5. Zone overlay kaybolmali
6. **Ciftlik** (Farm) butonuna tikla → overlay gorunMEmeli (ciftligin zone gereksinimi yok)

### Test 2: Yerlestirme Kisitlamasi
1. **Oduncu** butonuna tikla → zone overlay gorunur
2. Fare imlecini **yesil zone'a yakin** (3 hucre icinde) bir buildable alana getir → **yesil ghost** gorunmeli
3. Fare imlecini **yesil zone'dan uzak** (3 hucre disinda) bir buildable alana getir → **kirmizi ghost** gorunmeli
4. Yesil ghost olan yere tikla → bina yerlesir
5. Kirmizi ghost olan yere tikla → bina yerlesmez (tiklama etkisiz)

### Test 3: Farkli Zone Tipleri
1. **Tas Ocagi** (Quarry) sec → gri zone'lara yakin yerlestirilebilmeli
2. **Maden** (Mine) sec → kahverengi zone'lara yakin yerlestirilebilmeli
3. Yanlis zone'a yakin yerlestirmeye calis → kirmizi ghost (engellenmeli)
   - Ornek: Oduncu'yu tas zone'una yakin ama orman zone'undan uzak yere koymayi dene → kirmizi olmali

### Test 4: Restart Kontrolu
1. Birkac kaynak binasi yerlestir (Oduncu, Tas Ocagi vs.)
2. Oyunu bitir → Game Over → **Restart**
3. Zone tile'lari haritada korunmali (silinmemeli) — bunlar sabit harita ogeleri
4. Binalar sifirlanmali (grid temizlenmis olmali)
