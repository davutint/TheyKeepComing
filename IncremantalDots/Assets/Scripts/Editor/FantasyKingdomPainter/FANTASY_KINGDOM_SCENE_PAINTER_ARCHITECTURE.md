# Fantasy Kingdom Scene Painter - Architecture

## Amac

`FantasyKingdomScenePainterWindow`, SmallScaleInt Fantasy Kingdom paketinin
`Example scene.unity` sahnesini kalici bir yapisal referansa donusturur. Mevcut dort faz:

- referans tilemap katmanlarini analiz eder,
- Roof katmanlarindan yapi adaylari bulur,
- secilen hucre bolgesini cok-katmanli stamp assetine cikarir,
- stamp'i hedef `Grid` uzerinde gecici, tasinabilir bir dry-run preview olarak gosterir,
- mevcut ve korunan hucre cakismalarini raporlar,
- onaylanmis tek stamp'i yalniz tool-owned katmanlara, tek Undo grubuyla uygular,
- data-driven layout'taki tum stamp'leri birlikte, kalici yazim yapmadan compose eder.

Dry-run kalici tile yazmaz. Safe Apply mevcut elle boyanmis `Grass`, `Structures`,
`OverlayProps`, `Roof*` veya `outside*` katmanlarini degistirmez; yalniz purpose'a gore
`FK_PaintedStructures` veya `FK_PaintedBattlefield` altini yazar.

## Neden ayri tool?

Mevcut araclar farkli sahipliklere aittir:

- `BuildingTileComposerWindow`: legacy `BuildingConfigSO`, iki katman (Base/Top).
- `ArenaMapGeneratorWindow`: eski `WorldVisualRoot/MobileArenaGrid` arena katmanlari.
- `MobileCastleSceneSetupWindow`: gameplay binding/setup; boyanmis kok `Grid`i korur ama
  yeniden uretmez.

Example Scene yapilari ise `Walls`, `Roof1/2/3`, `WallDetail1/2`, `Objects`,
`Shadows1/2`, `Ground 2/3` gibi cok sayida katmanin birlikte calismasina dayanir.
Bu nedenle analiz/stamp/painter sorumlulugu yeni ve dar bir tool'da tutulur.

## Katmanlar

### `FantasyKingdomReferenceAnalyzer`

- Referans Scene assetini gecici olarak additive acar.
- Onceki aktif sahneyi saklar ve islem sonunda geri yukler.
- Tool tarafindan acilan referans sahneyi kaydetmeden kapatir.
- En cok Tilemap tasiyan `Grid`i referans grid olarak secer.
- Her tilemap icin occupied cell, unique tile, bounds ve renderer metadata'si toplar.
- Isminde `Roof` bulunan tilemap'lerde 8-komsuluk connected-component taramasi yapar.
- Birbirine yakin roof component'lerini tek structure candidate olarak birlestirir.

### `FantasyKingdomStructureStamp`

Editor-only ScriptableObject'tir. Saklanan veri:

- source scene/grid ve extraction region,
- grid cell layout/swizzle/size/gap,
- semantik `AnchorLocalCell` (eski assetlerde region merkezine guvenli fallback),
- stamp purpose (`Structure`, `ResourceSite`, `BattlefieldDecoration`, `GroundDetail`),
- source layer hierarchy path ve renderer sorting bilgisi,
- tilemap anchor/color/orientation,
- her hucre icin goreli koordinat, `TileBase` referansi, transform matrix, color ve flags.

Tile isimleri string olarak kopyalanmaz; gercek asset referanslari tutulur. Boylece GUID
ve variant bilgisi korunur.

Layout placement origin'i transform kaydirarak degil, her zaman
`TargetAnchorCell - AnchorLocalCell` ile tile koordinatinda hesaplanir.

### `FantasyKingdomScenePainterWindow`

- Reference Scene ve analiz parametrelerini sunar.
- Katman secimini kullaniciya birakir; onerilen yapi katmanlari varsayilan secilidir.
- Roof tabanli candidate listesinden extraction region uretir.
- Stamp assetini benzersiz path ile `Assets/Editor/FantasyKingdomPainter/Stamps` altina yazar.
- Secilen stamp ve hedef Grid icin hucre origin'i, tint ve preview sorting ayarlarini sunar.
- Scene View pivot'u veya secili Transform'dan origin alabilir; X/Y nudge ile preview'u tasir.
- Guncel preview raporu temizse `SAFE APPLY STAMP` islemini acar.

### `FantasyKingdomStampPreviewService`

- Stamp ve hedef Grid'in layout, swizzle, cell size ve cell gap degerlerini dogrular.
- Hedef Grid altinda `__FKPreviewRoot` ve stamp'in her katmani icin gecici Tilemap olusturur.
- Preview objeleri `DontSaveInEditor | DontSaveInBuild` oldugu icin scene dosyasina yazilmaz.
- Preview renderer'larini yuksek bir sorting order'da tutar; source katman sirasi korunur.
- `Grass`, `Ground` ve `GroundDetail` cakismalarini zemin olarak, diger mevcut gorsel
  katmanlari blocking olarak raporlar.
- `outside`, `outside0`, `outside2` ve `VillageMarkers` yakinindaki hucreleri protected
  conflict olarak raporlar.
- `Structure` ve `ResourceSite` stamp'lerinin `outside*` ile bulunan wall line'in sagina
  tasmasini engeller; sag cephe sanati `BattlefieldDecoration` purpose ister.
- Structure/resource stamp'lerinin mevcut `Grass`/`Ground` tabani disinda kalan hucrelerini
  eksik zemin destegi olarak raporlar.
- Marker adlarini rapora yazar; Wood/Stone/Food/Iron kaynak kontratlari gorunur kalir.
- Update, clear, pencere kapanisi veya hata durumunda gecici preview kokunu temizler.

### `FantasyKingdomStampApplyService`

- Apply aninda preview analizini yeniden kosar; UI'daki eski rapora guvenmez.
- `NewGameScene` icin kok `Grid`, uc `outside*`, 40 `outside` okcu slotu ve bes exact
  `VillageMarkers` kontratini mutation oncesi dogrular.
- Protected, marker, zone, zemin-destegi veya blocking conflict varsa hard fail verir.
- Purpose'a gore `Grid/FK_PaintedStructures` veya `Grid/FK_PaintedBattlefield` kokunu olusturur.
- Source layer basina canonical `FK_*` Tilemap kullanir; ayni layer her stamp icin yeniden
  kullanilir, stamp basina yeni tilemap acilmaz.
- Ground/shadow katmanlarini `Ground` sorting layer'ina; walls/objects/roof katmanlarini
  mevcut `Structures/OverlayProps/Roof*` siralamasiyla uyumlu `Objects` order'larina route eder.
- Renderer mode, sort direction, tile anchor, layer color ve orientation metadata'sini
  source stamp'ten korur. Mevcut managed layer metadata'si sapmissa sessizce ezmez, abort eder.
- Yeni root/layer olusturma ve tum tile yazimi tek Undo grubundadir.
- Scene'i dirty yapar ancak `SaveScene` veya `AssetDatabase.SaveAssets` cagirmaz.

### `FantasyKingdomMapLayout`

- Editor-only ScriptableObject'tir; sahne tile verisi tasimaz.
- Schema version, target `SceneAsset`, target Grid path, profile id ve seed saklar.
- Her placement stable id, label, enabled, stamp, target anchor cell, zone, gameplay
  anchor ve render band bilgisini tasir.
- Schema 1 eski layout'lar icin `LegacyAuto` davranisini korur. Schema 2 placement'lari
  acikca `Ground`, `BehindUnits` veya `InFrontOfUnits` secmek zorundadir.
- Guncel default `FK_NewGameScene_FullMap_V3_Draft`,
  `NewGameScene-ApprovedVisualRebuild-v3` profilidir. V2 asset'i tarihsel karsilastirma
  icin ayri path'te korunur ve V3 olusturulurken degistirilmez.

### `FantasyKingdomV3MapDraftBuilder`

- Onayli taş citadel, quarry, iron mine, food field/granary ve canli orman kaynaklarini
  exact region/layer/tile filtreleriyle yeniden uretir.
- Battlefield ve far-right zeminini deterministik hedef maskesinde sakin A1 zemininden;
  kervan yolunu sabit S-polyline recetesinden uretir.
- Enemy forest'i raw 28x26 source rect olarak tasimaz. Source palette'i hedefte
  `x=18..29/y=-8..8` bandina 140 back + 60 front agac olarak yeniden dagitir.
- Ayni V3 stamp/layout assetlerini `CopySerialized` ile gunceller; boylece rerun GUID'leri
  degismez. Yalniz editor asset'i yazar, `NewGameScene` Tilemap'lerine `SetTile` cagirmaz.

### `FantasyKingdomFullMapPreviewService`

- Mevcut tek-stamp analizini dolastirmaz; tum placement'lari once birlikte analiz eder.
- Hedef Grid altinda ayri `__FKFullMapPreviewRoot` olusturur. Kok, placement ve layer
  objelerinin tamami `DontSaveInEditor | DontSaveInBuild` tasir.
- `outside*`, marker 3x3 alani, zone, unknown tilemap, canonical layer/cell ve solid
  footprint cakismalarini hard conflict sayar.
- Legacy gorsel overlap, farkli ground/shadow placement ortusmesi, desteklenen kamera disi
  hucre, 16:9 referans viewport disi settlement hucresi, eksik semantik anchor ve
  marker->keep straight-guide koridor riskini warning olarak raporlar. Duz koridor cizgisi
  konservatif bir tasarim rehberidir; tam bir binayi kesmek/kirpmak icin gerekce degildir.
- Solid sinifi stamp purpose'tan degil source layer adindan gelir: ground/shadow disindaki
  `Objects`, `BrokenObjects`, `Walls`, `WallDetail*` ve `Roof*` hucreleri soliddir.
- Schema 2 preview placement kokleri `Ground/BehindUnits z=0`, unit `z=-1`,
  `InFrontOfUnits z=-2` kontratini kullanir. Owner onayli V3.1'de hem 140 agacli deep
  enemy forest hem 60 agacli front lip `InFrontOfUnits`, `Wall/4` ve Individual renderer
  kullanir; zombie orman kutlesinin ustunde gorunmez. Zombie prefab sorting'i yukseltilmez.
- Worker rotalarinin uzerindeki citadel, living forest, quarry, iron mine, food hedge ve
  granary de `InFrontOfUnits` kullanir. Worker `Wall/3` kalir; yuksek yapi ve bitki
  pikselleri `Wall/4+` ile worker'i orter, zemin placement'lari ise `Ground` kalir.
  Katmanli yapilarda Walls/Structures `4`, Objects `5`, Roof1/2/3 `6/7/8` kullanir;
  boylece worker occlusion saglanirken citadel ve granary'nin kendi cizim sirasi korunur.
- Full preview render edildikten sonra persistent `GroundDetail`, `Structures`,
  `OverlayProps` ve `Roof*`
  TilemapRenderer'lari `forceRenderingOff` ile gecici gizlenir. Onceki durumlari Grid bazinda
  tutulur; clear, script/domain reload ve Play Mode gecisi oncesinde geri yuklenir.
- Preview olusmadan once scene dirty state'i okunur. Temiz sahne beklenmedik sekilde dirty
  olursa tool bunu kaydederek veya flag'i orterek saklamaz; preview'u temizleyip hata verir.
- Clear, assembly reload, Play Mode, editor quit, `sceneSaving` ve `sceneClosing` tum duplicate
  preview/probe koklerini temizler ve gecici renderer durumlarini geri yukler.
- V3 hard gate; 5/5 marker yakinligi, protected/zone/conflict/support sifirlari, en az 30
  canli orman agaci, 120 back + 40 front enemy tree, front Y-band coverage, tek bagli S-yolu
  ve acik battlefield merkezinde sifir solid hucreyi birlikte dogrular.

### `FantasyKingdomFullMapComposerWindow`

- `Window/DeadWalls/Fantasy Kingdom Full Map Composer` menusunden acilir.
- Layout asseti ve aktif kok Grid'i birlikte gosterir.
- `CREATE / UPDATE FULL PREVIEW`, `ANALYZE ONLY` ve `CLEAR FULL PREVIEW` sunar.
- Scene View'da settlement, wall clearance, frontline, moat, battlefield, spawn ve
  far-right framing bantlarini; 16:9 sol/sag referans kenarlarini, bes marker'i ve
  marker->keep dogrularini cizer.
- Bilincli olarak full-layout Apply veya SaveScene dugmesi sunmaz.

## Guvenlik sinirlari

- Play Mode'da analiz, extraction, preview ve apply calismaz.
- Referans sahneye SetTile, SaveScene veya dirty islem uygulanmaz.
- Stamp isimleri/path'leri sanitize edilir ve yalniz `Assets/` altina yazilir.
- Ayni isimde asset varsa mevcut asset ezilmez; unique path uretilir.
- Dry-run yalniz `__FKPreviewRoot` altina yazar; hedef tilemap'lerde `SetTile` cagrisi yapmaz.
- Full-map dry-run yalniz `__FKFullMapPreviewRoot` altina yazar; gercek Grid tilemap'lerine
  `SetTile` cagrisi yapmaz.
- Preview root save/build disidir ve `CLEAR PREVIEW` ile elle de temizlenebilir.
- Safe Apply'da force/override modu yoktur; mevcut gorsel veya gameplay tile'i silinmez.
- Kalici islem tek Undo grubudur ve sahne otomatik kaydedilmez.

## Tarihsel Faz 4: Full Map Composer v2 dry-run

Owner yonu (2026-07-10): mevcut harita en sonunda tek kompozisyon olarak bastan boyanacak.
Phase 4 bu hedefin kalici yazimdan onceki data-driven dry-run esigidir.

Tarihsel not: kompakt 3+3 `NewGameScene-VisualRebuild-v1`, teknik geometri raporunu
gecmis olsa da eksik/cati-agirlikli sol crop'lar ve ekran merkezini dolduran sag stamp'ler
nedeniyle owner gorsel incelemesinde reddedildi. V1 guncel truth veya basari kaydi degildir.

`NewGameScene-VisualRebuild-v2` yerlesim kiti:

| Placement | Stamp | Anchor | Purpose |
|---|---|---:|---|
| `left.keep` | `FK_PreservedVillageKeep_A` | `(4,15)` | `Structure` |
| `left.house` | `FK_LogCabin_House_A` | `(-5,5)` | `Structure` |
| `left.workshop` | `FK_StoneEntry_Workshop_A` | `(-16,-4)` | `ResourceSite` |
| `right.north.low_ruin` | `FK_Battlefield_LowRuin_A` | `(27,-3)` | `BattlefieldDecoration` |
| `right.north.rubble_dense` | `FK_Battlefield_Rubble_Dense_A` | `(26,-8)` | `GroundDetail` |
| `right.south.broken_cart` | `FK_Battlefield_BrokenCart_A` | `(2,-26)` | `BattlefieldDecoration` |
| `right.south.dry_branch` | `FK_Battlefield_DryBranch_A` | `(6,-28)` | `BattlefieldDecoration` |
| `right.edge.crater_north` | `FK_Battlefield_Crater_N` | `(20,-10)` | `GroundDetail` |
| `right.edge.crater_south` | `FK_Battlefield_Crater_S` | `(13,-19)` | `GroundDetail` |
| `right.far.worn_scuff` | `FK_Battlefield_WornScuff_A` | `(28,-12)` | `GroundDetail` |
| `right.far.rubble_light` | `FK_Battlefield_Rubble_Light_A` | `(14,-30)` | `GroundDetail` |

`FK_Reference_StoneHouse_A` referans/test stamp'idir; v2 full-map draft'ina dahil
degildir.

- Sol taraf: mevcut tam keep silueti korunur; log cabin ve tas girisli workshop eksiksiz
  yapi receteleridir. CastleKeep/Wood/Stone baglidir; Food/Iron semantik anchor'lari ertelenmistir.
- Duvar/okcu hatti: `outside*` kontrati ve sabit kamera okunurlugu korunacak.
- Sag taraf: sekiz mikro-stamp yalniz kuzey, guney ve far-right kenarlari cerceveler. Combat
  okumasi ve zombi siluetleri icin ekran merkezi bilincli olarak bos birakilir.
- Combat kontrati: frontline `-0.5`, moat world X `1.5..4`, battlefield `4..18`,
  far-right frame `18..27`, hidden spawn X `27..29`, Y `+-6.5`.
  Spawn seridi Android max aspect `2.4`, kamera sarsintisi ve zombi yaricapi dahil ekran
  disindadir. Moat ve spawn seridinde roof/house/wall gibi yuksek siluetli dekor kullanilmayacak.
- Sag cephe icin `BattlefieldDecoration`/`GroundDetail`; sol yerlesim icin
  `Structure`/`ResourceSite` purpose kullanilacak.

Tam harita fazi su kontratlari da korumalidir:

- hedef: `NewGameScene/Grid` ve acikca eslenen katmanlar,
- aggregate dry-run raporunun kullanici onayi ve bolge/Game Camera screenshot'i,
- korunan `outside` okcu slotlari,
- `VillageMarkers`, duvar/hendek/spawn sinirlari,
- seed + profile ile deterministik yeniden uretim,
- setup-tool re-run sonrasi boyamanin korunmasi.

Phase 4 gercek Grid katmanlarina `SetTile` yazmaz ve `SaveScene` cagirmaz. Dogrulanan v2
dry-run: 11/11 placement, 120 tile, 83 unique / 72 solid cell, 0 hard conflict, 8 warning,
20 straight-guide corridor-risk cell, 49 legacy-overlap cell, 0 camera/ref-viewport disi
cell ve 3/5 gameplay anchor; persistent tile sayisi 3116 -> 3116 kalmistir. Food/Iron
semantik anchor'lari ertelenmistir. Legacy overlap bir migration borcu olarak raporlanir;
preview'da renderer gizlemek kalici retirement anlamina gelmez. Straight-guide koridor
sayimi de tam yapilari kirpmayi hakli kilmaz: kalici apply preflight'i mevcut dolasimi
izleyen route-based koridor cozumunu ayri olarak kanitlamalidir.

## Guncel Faz 5: Approved Visual Rebuild V3 dry-run

V3, owner'in su kilit kararlarini uygular:

- kale tamamen tastir; `FK_PreservedVillageKeep`, log cabin ve `gen_castle` kullanilmaz,
- Wood alani tek kamp prop'u degil, en az 30 agacli canli ormandir,
- Stone quarry, Iron mine ve Food field + granary ayri okunur,
- battlefield merkezinde yuksek obje yoktur; zemin sakin ve dusuk varyasyonludur,
- orman agzindan duvar/kale yonune tek bagli S-kervan yolu gider,
- sag bant 140 deep + 60 front agacli enemy forest'tir,
- zombie z=-1 korunur; iki enemy forest katmani da z=-2 `Wall/4` occluder olarak birimi
  orman icindeyken kapatir.

Guncel assetler:

- Layout: `Assets/Editor/FantasyKingdomPainter/Layouts/FK_NewGameScene_FullMap_V3_RetouchPreview.asset`
- Stable stamp set: `Assets/Editor/FantasyKingdomPainter/Stamps/V3`
- Builder menu: `Window/DeadWalls/Fantasy Kingdom/Rebuild V3 Retouch Preview Assets`
- Preview menu: `Window/DeadWalls/Fantasy Kingdom/Create V3 Retouch Preview`
- Occlusion proof: `Window/DeadWalls/Fantasy Kingdom/Create Zombie Forest Occlusion Probe`

## Faz 6: owner-onayli kalici V3 snapshot apply

Owner screenshot onayindan sonra kalici sahip `FantasyKingdomV3MapApplyService` olmustur.
Phase 3 `ApplySafely` placement basina dongude cagrilmaz. Tum layout tek atomik Undo
grubunda `Grid/FK_V3_Map` altina yazilir:

- `00_Ground`: z=0, 8 placement-specific Tilemap, 2171 tile,
- `10_BehindUnits`: z=0, bos band root'u,
- `20_FrontOccluders`: z=-2, 16 Tilemap, 524 tile,
- toplam: 16 placement, 24 Tilemap, 2695 tile.

Her placement ve source layer ayri persistent Tilemap tasir; ayni adli source layer'lar
birlesmez. Ground/Behind renderer sorting order'i semantic local order + placement/layer
indexinden olusan serialize edilmis tie-break kullanir. Boylece onayli preview, apply oncesi
ve scene reload sonrasi render sirasi instance ID'ye bagli kalmaz. Enemy forest `Wall/4`;
worker rotasinin kestigi katmanli settlement yapi occluder'lari `Wall/4..8`, `Individual`
ve z=-2 kalir; zombie/unit z=-1 sahipligi degismez.

Apply once temiz `NewGameScene`, exact V3 schema/profile ve full preflight hard gate ister.
Yeni snapshot staging root'ta tamamen kurulup dogrulandiktan sonra yalniz exact direct-child
allowlist temizlenir: `GroundDetail`, `Structures`, `OverlayProps`, `RoofLow`, `RoofHigh` ve
opsiyonel `Roof1/2/3`. `Grass`, `outside`, `outside0`, `outside2`, 40 okcu slotu ve bes
`VillageMarkers` cell/tile/transform fingerprint ile korunur. Managed root altinda collider
olamaz. Servis sahneyi dirty birakir fakat `SaveScene` cagirmak bilincli olarak ayri adimdir.
Root/container transformlari ile yeni Tilemap/TilemapRenderer metadata ve cell verisi
`RegisterCompleteObjectUndo` snapshot'ina dahildir. Canli `Apply -> Undo -> Redo` testi;
eski root'un geri gelmesini, Redo sonrasi 27/2488 icerigi, front z=-2 sozlesmesini ve
onayli screenshot hash'ini kanitlamistir.

Post-reload validator yalniz toplam saymaz: exact placement/layer hiyerarsisi, transform,
component allowlist, source stamp tile/matrix/color, renderer material/mode/sort order,
protected `Grass/outside*` fingerprint'i ve marker world pozisyonlarini kontrol eder.

Kalici apply menusu:
`Window/DeadWalls/Fantasy Kingdom/APPLY APPROVED V3 TO NEW GAME SCENE`.
Post-apply yapisal kontrol menusu:
`Window/DeadWalls/Fantasy Kingdom/Validate Persistent V3 Map`.

Legacy source tile'lari temizlendikten sonra builder, managed V3 root'u ve sifir legacy
snapshot'i algilar. Wood/Stone/Iron/Food icin mevcut stabil V3 stamp assetlerini exact
`32, 10/13, 10/12, 35/5` tile kontratiyla yeniden kullanir; eksik veya kismi kaynakta asseti
sessizce overwrite etmez. Guard, legacy durumda per-layer `240/186/8/8/5` sayilarinin
yaninda onayli original scene disk hash'ini; applied durumda ise yedi frozen stamp'in exact
disk hash'lerini ve persistent V3 root kontratini dogrular. Example Scene ve deterministik
uretilen battlefield/forest stamp'leri normal sekilde yeniden uretilmeye devam eder.
