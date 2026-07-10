# Fantasy Kingdom Scene Painter - Editor Setup

## Acilis

Unity menu:

`Window > DeadWalls > Fantasy Kingdom Scene Painter`

Full-map dry-run:

`Window > DeadWalls > Fantasy Kingdom Full Map Composer`

Varsayilan referans:

`Assets/SmallScaleInt/Fantasy kingdom Tileset/Example scene/Example scene.unity`

## Reference analizi

1. Play Mode kapali olmalidir.
2. `ANALYZE REFERENCE SCENE` butonuna bas.
3. Tool referans sahneyi gecici additive acar, analiz eder ve tekrar kapatir.
4. Analysis bolumunde layer/cell/candidate sayilarini kontrol et.

Aktif calisma sahnesi degismez ve kaydedilmez.

## Katman secimi

`Recommended`, ev ve kale stamp'leri icin su katmanlari secer:

- Walls
- Roof1 / Roof2 / Roof3
- WallDetail1 / WallDetail2
- Objects / BrokenObjects
- Shadows1 / Shadows2 / LowerShadows
- Ground 2 / Ground 3

`All Visual`, collider/TileCheck/BuildPreview/indestructible katmanlari haric tum gorsel
katmanlari secer. Base `Ground`, `Water` veya `Foam` gibi genis alan katmanlarini ancak
stamp'in parcasi olmasi isteniyorsa sec.

## Structure candidate kullanimi

Roof component'leri buyukten kucuge listelenir.

1. Bir satirdaki `Use` butonuna bas.
2. Tool roof bounds'a `Extraction Padding` ekleyip Region Min/Size alanlarini doldurur.
3. Region degerleri elle duzeltilebilir.
4. Stamp adini anlamli yap: `FK_ThatchHouse_A`, `FK_CastleTower_A` gibi.

Candidate otomatik tespit yalniz baslangic bolgesidir. Birbirine yakin iki yapi ayni roof
component'i icindeyse region'i elle daraltmak gerekebilir.

## Stamp cikarma

Varsayilan output:

`Assets/Editor/FantasyKingdomPainter/Stamps`

`EXTRACT MULTI-LAYER STAMP`:

- secilen region ve layer'lari okur,
- bos layer'lari atlar,
- benzersiz `.asset` olusturur,
- sonucu Project penceresinde ping'ler.

Stamp cikarmak hedef sahneye tile basmaz.

`Stamp Purpose`:

- `Structure`: keep, ev, atelye, kule gibi sol yerlesim yapilari.
- `ResourceSite`: Wood/Stone/Food/Iron alani icin semantik yapi/dekor grubu.
- `BattlefieldDecoration`: wall line'in sagindaki alcak savas alani dekoru.
- `GroundDetail`: yol, leke, catlak ve zemin gecisi.

Mevcut `FK_Reference_StoneHouse_A` asseti geriye uyumlu olarak `Structure` kabul edilir.

## Dry-run preview

1. Hedef sahneyi ac; normal hedef `Assets/Scenes/NewGameScene.unity` icindeki kok `Grid`dir.
2. `6. Dry-Run Preview (Phase 2)` bolumunu ac.
3. `Stamp` alanina bir `FantasyKingdomStructureStamp` sec. Ornek ev stamp'i varsayilan gelir.
4. `Active Grid` ile aktif sahnenin uygun Grid'ini bul.
5. `Target Origin` gir veya Scene View pivot/Selection butonlarindan birini kullan.
6. `CREATE / UPDATE PREVIEW` ile gecici tilemap'leri olustur.
7. X/Y nudge butonlariyla yapinin yerini birer hucre tasiyarak cakisma raporunu izle.
8. Is bitince `CLEAR PREVIEW` kullan. Pencere kapanirken de preview otomatik temizlenir.

Rapor alanlari:

- `Existing overlap`: stamp'in herhangi bir mevcut tile ile ortak hucreleri.
- `Blocking`: zemin disindaki mevcut gorsel katmanlarla ortak hucreler.
- `Protected`: `outside*` okcu slot katmanlariyla ortak hucreler.
- `Marker conflict`: `VillageMarkers` cocuklarinin bir hucre cevresine giren yapilar.
- `Restricted zone`: Structure/ResourceSite stamp'inin wall line'in sagina tasan hucreleri.
- `Ground support`: Structure/ResourceSite hucrelerinin mevcut `Grass`/`Ground` tabaninda
  kalan kismi; eksik support Apply'i kilitler.

Preview `__FKPreviewRoot` altindaki `DontSave` objelerindedir. Gercek Grid katmanlarina tile
basmaz, sahneyi kaydetmez ve kalici apply yapmaz.

## Tek-stamp Safe Apply (Phase 3)

1. Stamp, Grid ve origin'i sec.
2. `CREATE / UPDATE PREVIEW` ile guncel dry-run raporu olustur.
3. `Blocking`, `Protected`, `Marker conflict`, `Restricted zone` ve `Missing support`
   degerlerinin tamamini sifirla.
4. `7. Safe Apply (Phase 3)` bolumunde `SAFE APPLY STAMP (UNDOABLE)` butonuna bas.
5. Tool purpose'a gore asagidaki kalici koklerden birini kullanir:
   - `Grid/FK_PaintedStructures`
   - `Grid/FK_PaintedBattlefield`
6. Unity sahneyi dirty gosterir ancak kaydetmez. Sonucu incele; kabul edilmezse tek `Ctrl+Z`
   ile apply grubunu geri al.

Managed layer'lar source isimlerinden canonical olarak uretilir: `FK_Ground_2`,
`FK_Shadows1`, `FK_Objects`, `FK_Walls`, `FK_Roof1` gibi. Ayni source layer sonraki
stamp'lerde ayni tilemap'i kullanir.

Safe Apply force/replace modu sunmaz. Mevcut `Structures`, `OverlayProps`, `Roof*`,
`outside*` veya baska managed tile ile cakisma varsa origin tasinmalidir.

Full Map Composer bu servisi placement basina dongude cagirmaz. Phase 4 kendi aggregate
analizini kullanir ve kalici apply sunmaz.

## Tarihsel Full Map Composer v2 dry-run (Phase 4)

1. `Assets/Scenes/NewGameScene.unity` sahnesini ac.
2. Hedefin kok `Grid` oldugunu dogrula.
3. `LOAD / CREATE DEFAULT DRAFT` ile
   `FK_NewGameScene_FullMap_Draft` layout assetini sec veya olustur.
4. `CREATE / UPDATE FULL PREVIEW` ile v2 profilindeki 11 placement'i birlikte analiz et.
5. Scene View'da 16:9 sol/sag kenari, marker, wall, moat, battlefield, spawn ve far-right
   overlay'lerini incele.
6. Hard conflict'leri placement `Target Anchor Cell` degerleriyle duzelt.
7. Game Camera ve bolge screenshot'larini owner onayi icin kontrol et.
8. Is bitince `CLEAR FULL PREVIEW` kullan. Preview acikken gecici gizlenen
   `Structures`/`OverlayProps`/`Roof*` renderer'lari clear, reload veya Play Mode gecisi
   oncesinde eski durumlarina getirilir.

Phase 4'te kalici apply veya scene save yoktur. `NewGameScene` tile'lari degismez.

Aktif kontratlar:

- Referans 16:9 kamera: world X `-8.22..20.22`, Y `-8..8`.
- Desteklenen en genis Android aspect `2.4`: world X `-13.2..25.2`.
- Settlement: X `-8..-1.5`.
- Wall clearance: X `-1.5..1.5`; frontline X `-0.5`.
- Moat: X `1.5..4`, yalniz ground detail.
- Battlefield: X `4..18`, alcak ve seyrek solid prop.
- Far-right framing: V2 icin X `18..27`; kamera kenarini devam ettiren alcak siluet.
- Hidden spawn: X `27..29`, Y `+-6.5`, yalniz ground detail. Bu bandin kamera disinda
  kalmasi bilincli oldugu icin Composer camera-outside warning uretmez.
- `outside`: X `0` uzerinde 40 okcu slotu.
- `VillageMarkers`: CastleKeep, Wood, Stone, Food, Iron.
- Resource marker -> keep dogrularinda hedef yaklasik 1 world-unit solid-free koridordur.

Tarihsel `NewGameScene-VisualRebuild-v2` draft raporu: 11/11 placement, 120 tile,
83 unique / 72 solid cell, 0 hard conflict, 8 warning, 20 straight-guide corridor-risk
cell, 49 legacy-overlap cell, 0 camera/ref-viewport disi cell ve 3/5 gameplay anchor.
Persistent tile sayisi 3116 -> 3116 kalir; gercek Grid'e `SetTile` veya `SaveScene` yoktur.

Sol kit korunmus tam keep, eksiksiz log cabin ve tas girisli eksiksiz workshop kullanir.
Sagda sekiz mikro-stamp yalniz kuzey/guney/far-right kenarlarindadir; ekran merkezi combat
okunurlugu icin bostur. Food/Iron semantik anchor'lari ertelenmistir.

Tarihsel kompakt 3+3 v1, teknik geometri raporunu gecmesine ragmen eksik/cati-agirlikli sol
yapilar ve merkezdeki buyuk sag stamp'ler nedeniyle owner gorsel incelemesinde reddedildi;
v1 sayilari guncel dogrulama olarak kullanilmaz.

Aggregate rapor:

- Hard: `outside*`, marker 3x3, zone, unknown mevcut tilemap, ayni canonical layer/cell ve
  solid-footprint cakismasi.
- Warning: legacy gorsel migration overlap'i, farkli ground/shadow placement ortusmesi,
  desteklenen kamera disi hucre, 16:9 referans viewport disi settlement hucresi, eksik
  semantik anchor ve marker->keep straight-guide koridor riski. Duz rehber bir tam binayi
  kesme/kirpma gerekcesi degildir.
- `GroundDetail` purpose tek basina zeminsiz/solidsiz demek degildir; `Objects`, `Walls`,
  `BrokenObjects` ve `Roof*` source layer'lari yine solid kontrol edilir.

## Guncel V3 approved full-map dry-run

1. `Assets/Scenes/NewGameScene.unity` sahnesini temiz durumda ac.
2. `Window > DeadWalls > Fantasy Kingdom > Rebuild V3 Draft Assets` ile stable V3 stamp ve
   layout assetlerini yeniden uret.
3. `Window > DeadWalls > Fantasy Kingdom > Create Default V3 Preview` ile transient preview'u
   olustur. Ayni islem Composer icindeki `REBUILD APPROVED V3 RECIPE ASSETS` ve
   `CREATE / UPDATE FULL PREVIEW` dugmeleriyle de yapilabilir.
4. Raporun `Hard: 0`, `Gameplay anchor: 5/5`, `Protected/Marker/Zone: 0` oldugunu dogrula.
5. V3 satirinda living/back/front tree sayilari, front Y-band coverage, tek road component ve
   open-center solid sifirini kontrol et.
6. Game Camera'da tam harita, sol yerlesim, acik merkez ve sag enemy forest goruntulerini al.
7. `Create Zombie Forest Occlusion Probe` ile runtime zombie'nin kullandigi ayni
   `skeleton_atlas`tan tek-frame bir proxy olarak bir acik-alan kontrolu ve uc orman zombisi
   olustur. Orman proxy'leri front canopy/govde tarafindan kismen kapanmali; unit sorting'i
   globally yukseltilmemelidir. Gercek ECS spawn kaniti kalici apply sonrasi Play Mode'dadir.
8. `Clear Zombie Forest Occlusion Probe` ve `Clear Full Map Preview` kullan. Preview sonrasi
   scene dirty, persistent tile fingerprint ve renderer state baseline ile ayni kalmalidir.

V3 derinlik sozlesmesi:

- `Ground`: z=0, Ground sorting.
- `BehindUnits`: z=0, Individual renderer.
- Zombie/unit: z=-1; prefab veya runtime sorting degeri degistirilmez.
- `InFrontOfUnits`: z=-2, Wall sorting order 4, Individual renderer.

V3 far-right/enemy forest bandi X `18..29`, Y `-8..8`dir. Hidden spawn X `27..29`,
Y `+-6.5` bu orman kutlesinin icindedir. Preview/probe `DontSave`dir.

## Owner onayi sonrasi kalici V3 uygulama

1. Preview ve zombie probe'u temizle; aktif sahnenin `NewGameScene` ve temiz oldugunu dogrula.
2. Son kez `Create Default V3 Preview` calistir; `Hard: 0`, anchor `5/5`, living/back/front
   `32/140/60`, front Y band `14/14`, road `84/1`, open-center solid `0` olmali.
3. `Window > DeadWalls > Fantasy Kingdom > APPLY APPROVED V3 TO NEW GAME SCENE` calistir.
   Islem tek Undo grubudur ve otomatik save yapmaz.
4. `Validate Persistent V3 Map` ile 19 placement, 27 Tilemap, 2488 managed tile,
   legacy visual `0`, collider `0`, anchor `5/5` sonucunu dogrula.
5. Main Camera screenshot'ini owner-onayli preview ile karsilastir. Render sirasi
   serialize edilen placement/layer tie-break'i sayesinde reload sonrasi da ayni kalmalidir.
6. Yalniz gorsel ve yapisal kontrol gectiyse `NewGameScene` sahnesini acikca kaydet.
7. Sahneyi reload et, `Validate Persistent V3 Map` ve Main Camera screenshot'ini tekrarla.
8. Play Mode'da gercek ECS zombie icin spawn X `27..29`, `|Y|<=6.5`, unit z=-1 ve
   forest back z=0 / front z=-2 occlusion kontratini smoke-test et.

Apply sonrasi `Rebuild V3 Draft Assets` guvenlidir: legacy source katmanlari sifirsa ve
`Grid/FK_V3_Map` varsa Wood/Stone/Iron/Food stabil stamp'leri exact tile sayilariyla korunur.
Kismi veya beklenmeyen legacy kaynak snapshot'i builder'i durdurur; mevcut onayli assetleri
bos veriyle overwrite etmez.

## Sorun giderme

- Candidate yok: `Min Roof Component` degerini dusur.
- Iki yapi birlesiyor: `Candidate Merge` veya `Extraction Padding` degerini dusur.
- Stamp bos: region koordinatlarini ve layer secimini kontrol et.
- Yanlis zemin doluyor: base `Ground` katmanini kapat; yalniz `Ground 2/3` detaylarini kullan.
- Referans sahne zaten aciksa tool onu kapatmaz; yalniz kendi actigi sahneyi kapatir.
- Preview acilmiyor: stamp ve target Grid'in layout/swizzle/cell size/cell gap degerlerini kontrol et.
- Yapi gorunmuyor: `Preview Sorting Layer` degerinin projede var oldugunu kontrol et; gecersiz
  ad otomatik olarak `Default` katmanina duser.
- Protected conflict: origin'i nudge ile tasi; bu faz conflict olsa da yalniz preview gosterir.
- Restricted zone: ev/kale stamp'ini wall line'in soluna tasi; sag cephede structure purpose
  kullanma.
- Missing support: yapinin bir kismi boyanmis harita disinda kaliyor; daha kucuk stamp veya
  tam-harita ground fazini bekle.
- Safe Apply kapali: preview ayarlardan sonra stale olmustur; preview'u yeniden olustur.
- Managed layer metadata hatasi: `FK_*` layer elle degistirilmistir; tool sessizce ezmez.
- Full preview hard conflict: Composer'daki issue satirinda placement id ve ihlal turunu oku;
  once anchor cell'i duzelt, sonra aggregate preview'u yeniden uret.
- Reference viewport warning: settlement stamp'ini 16:9 sol/sag kenarlarinin icine tasi veya
  viewport-safe crop kullan; max-aspect kamera kontrolu tek basina yeterli degildir.
- Corridor warning: straight-guide sonucunu once mevcut yaya/dolasim rotasiyla karsilastir.
  Eksiksiz binayi kirpma; kalici apply oncesi route-based koridoru kanitla ve gerekiyorsa
  explicit corridor maskesini bu rota uzerinden tasarla.
