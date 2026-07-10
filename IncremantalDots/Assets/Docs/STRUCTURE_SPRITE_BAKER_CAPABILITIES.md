# Structure Sprite Baker — Yetenek ve Kapasite Dokumani

> Bu dokuman baska bir Claude oturumuna (veya gelistiriciye) bu pipeline'in NE yapabildigini
> anlatmak icin yazildi. Ornegin ana oyunu ilerleten oturum, gorsel katman boyama talimatini
> bu dokumandaki yetenek ve sablonlara gore verebilir.
> Yazim kurali: SADECE ASCII (repo konvansiyonu).

## Ne bu?

SmallScaleInt "Fantasy kingdom Tileset" parcalarindan prosedurel yapi/vinyet ureten ve bunlari
tek seffaf sprite'a bake eden, Unity MCP uzerinden calisan bir pipeline. Iki calisma modu var:

1. **Sprite bake:** kompozisyonu izole ortamda kur -> seffaf ortho kamera ile texel-perfect render
   -> alfa tight-crop -> PNG -> otomatik sprite import (PPU 128, point filter, alt-orta pivot).
   Cikti dogrudan SpriteRenderer'a atanabilir.
2. **Sahne tilemap boyama:** 2026-07-07 Claude/Fable oturumunda `NewGameScene` kok `Grid`
   katmanlari kalici olarak boyandi. `MobileCastleSceneSetupWindow` bu boyamayi korur; ancak
   final haritanin tam yeniden-uretim recetesi eski scratchpad pipeline'inda kalmisti.

DURUM (2026-07-10): Faz-1 + Faz-2 + Faz-3 tek-stamp araci ve Faz-4 full-map dry-run
EditorWindow'u eklendi:

- `Window > DeadWalls > Fantasy Kingdom Scene Painter`
- `Window > DeadWalls > Fantasy Kingdom Full Map Composer`

- `Example scene.unity` 22 tilemap katmaniyla salt-okunur analiz edilir.
- Roof component'lerinden structure candidate'lari bulunur.
- Secilen bolge, tile referansi + goreli hucre + renk/transform/flags bilgisiyle kalici
  multi-layer `FantasyKingdomStructureStamp` assetine cikarilir.
- Stamp, hedef `Grid` uzerinde `DontSave` preview tilemap'leriyle tasinabilir dry-run olarak
  gosterilir; mevcut/blocking/protected/VillageMarkers/zone/ground-support cakismalari raporlanir.
- `SAFE APPLY STAMP`, yalniz tool-owned `FK_PaintedStructures` / `FK_PaintedBattlefield`
  katmanlarina tek Undo grubuyla yazar; mevcut map/gameplay katmanlarini silmez, scene'i kaydetmez.
- Stamp purpose kontrati vardir: sol yerlesim `Structure/ResourceSite`, sag cephe
  `BattlefieldDecoration/GroundDetail`.
- `FantasyKingdomMapLayout`, `NewGameScene-VisualRebuild-v2` profiliyle semantic-anchor
  kullanan 3 sol yapi + 8 sag kenar mikro-stamp yerlesim recetesidir.
- Composer tum placement'lari birlikte analiz eder; `__FKFullMapPreviewRoot` gecicidir,
  full-layout apply veya scene save sunmaz.
- Full preview acikken `Structures` / `OverlayProps` / `Roof*` legacy object renderer'lari
  gecici gizlenir. `CLEAR FULL PREVIEW`, script/domain reload ve Play Mode gecisi oncesinde
  kaydedilen renderer durumlari geri yuklenir.

Kanonik yeni dokumanlar:
`Assets/Scripts/Editor/FantasyKingdomPainter/FANTASY_KINGDOM_SCENE_PAINTER_ARCHITECTURE.md`
ve `FANTASY_KINGDOM_SCENE_PAINTER_EDITOR_SETUP.md`.

TUM HARITA REVIZYON YONU (owner, 2026-07-10): mevcut harita en sonunda bastan, tek
kompozisyon olarak boyanacak. Sol taraf korunmus tam keep + eksiksiz log cabin + tas girisli
eksiksiz workshop ile okunur; sag tarafin kenarlari mikro-prop'larla cercevelenirken merkez
combat alani bilincli bos kalir. Frontline/moat/spawn ve 40 okcu slotu korunur.

`NewGameScene-VisualRebuild-v2` Phase-4 kiti:

| Bolge | Stamp | Purpose |
|---|---|---|
| Sol | `FK_PreservedVillageKeep_A` | Structure |
| Sol | `FK_LogCabin_House_A` | Structure |
| Sol | `FK_StoneEntry_Workshop_A` | ResourceSite |
| Sag kuzey | `FK_Battlefield_LowRuin_A` | BattlefieldDecoration |
| Sag kuzey | `FK_Battlefield_Rubble_Dense_A` | GroundDetail |
| Sag guney | `FK_Battlefield_BrokenCart_A` | BattlefieldDecoration |
| Sag guney | `FK_Battlefield_DryBranch_A` | BattlefieldDecoration |
| Sag kenar | `FK_Battlefield_Crater_N` | GroundDetail |
| Sag kenar | `FK_Battlefield_Crater_S` | GroundDetail |
| Far-right | `FK_Battlefield_WornScuff_A` | GroundDetail |
| Far-right | `FK_Battlefield_Rubble_Light_A` | GroundDetail |

Guncel v2 full-map dry-run 11/11 placement, 120 tile, 83 unique / 72 solid cell,
0 hard conflict, 8 warning, 20 straight-guide corridor-risk cell, 49 legacy-overlap cell,
0 camera/ref-viewport disi cell ve 3/5 gameplay anchor ile dogrulandi; kalici tile sayisi
3116 -> 3116 kaldi. Food/Iron semantik anchor'lari ertelendi. Marker->keep arasindaki duz
cizgi yalniz konservatif bir rehberdir: tam binayi kirpmak icin kullanilmaz; kalici apply
oncesi mevcut dolasim alanini izleyen route-based koridor cozumu gerekir.

Tarihsel not: kompakt 3+3 `NewGameScene-VisualRebuild-v1`, hard/camera/corridor geometri
raporunu gecmisti ancak eksik/cati-agirlikli sol yapilar ve ekran merkezini dolduran sag
stamp'ler nedeniyle owner gorsel incelemesinde reddedildi. V1 sayilari basari veya guncel
harita truth'u olarak kullanilmaz.

## Kanitlanmis uretimler (Assets/Sprites/BakedStructures/)

| Sprite | Icerik | Uretici parametreleri |
|---|---|---|
| gen_thatch_h4/h6 | Saz catili kutuk ev (parametrik) | kit=thatch, eksen=h, W=4..8, seed |
| gen_shingle_h7/v6 | Ahsap kiremitli kutuk ev (parametrik) | kit=shingle, eksen=h/v, boyut, seed |
| gen_castle | Kale: sur + demir kapi + 2 katli kule + sancak | W x D (or. 11x8), seed |
| gen_quarry | Tas ocagi: kaya duvari, kazi cukuru, vinc, merdiven | seed |
| gen_iron_mine | Demir madeni: tunel agzi, cevher kayalari, araba | seed |
| gen_farm | Tarla: bugday tarhi, ekin sirasi, kuyu, balya, kovan | seed |
| gen_lumber_camp | Kereste kampi: cam fonu, stump, kutuk, atolye | seed |
| bake_house16/24 | Yazar sahnesinden verbatim ev sokumleri | sablon #16/#24 |

Rastgelelik SADECE dogrulanmis kit ici varyantlarda calisir (boyut, varyant no, kapi pozisyonu,
dekor serpme). Kit'ler arasi karisim yok -> gorsel tutarlilik yapisal garanti.

## Dogrulanmis malzeme sozlugu (448 parca gozle incelendi)

- **Ev kitleri:** Wall C (kutuk; C2 kose, C4 pencereli, C1 duz, C6 kapi kemeri, C3 kalas panel)
  + Roof A (saz; A3 mahya, A6 gable) veya Roof C (kiremit; C1/C5 egimler, C6/C8 kule catisi)
  + Roof B (paylasilan alinlik B1/B2/B3). Wall G + Roof E = ciftlik ahsap yapi.
- **Kale:** Wall A ailesi — A1/A15 duz, A2/A16 kose, A4/A5 gotik pencere, A10/A11 parapet,
  A12/A13/A14 merdiven, A20 duvar ucu, A8/A9/A17/A18 yikik varyantlar. Roof D arduvaz.
  Door C demir kapilar (DC5 cift kanat), Misc D sancaklar, Torch East/West mesaleler.
- **Arazi/vinyet:** Ground G12/G18/G19 ucurum yuzleri (arka duvar olarak TEK SIRA _E kullan,
  uclar G14_E/G20_E), G22 TUNEL AGZI, G3/G9 hoyukler, F5/F6 kazi cukurlari; Stone A19 DEMIR
  CEVHERI, A9/A10 moloz, A8 kirmizi sivili kuyu (ritual gorunumlu, dikkat); Misc B48 VINC,
  B42 catili kuyu, B6/B5 arabalar, B3 kereste istifi, B18/B14 atolye barakalari, B41/B43/B46
  cadirlar, B55/B56 gozetleme kuleleri, E1..E4 saman balyalari, E5/E7/E11 bugday/ekin;
  Ground C4 tohum ekilmis toprak; Tree A4 stump, E2/E4/E5 devrik kutukler, D1-D3 camlar,
  B2/B3 hedge citler; WallFlora sarmasik overlay.
- **YOK olan seyler:** yel degirmeni parcasi YOK; yuvarlak kule YOK (kuleler kare modul).

## Kritik teknik kurallar (ihlal = bozuk gorsel)

1. Paket natif grid ve aktif full-map hedefi `NewGameScene/Grid`:
   cellLayout=Isometric, cellSize=(1, 0.5, 1); sprite 256x256, PPU 128,
   pivot ~(0.5, 0.19). `WorldVisualRoot` sahnede bos/legacy koktur;
   `MobileArenaGrid` aktif tam-harita hedefi degildir.
2. URP aktif: kamera render'i icin `RenderPipeline.SubmitRenderRequest(StandardRequest)` SART;
   `cam.Render()` calismaz. Bake'te sprite'lara `Sprite-Unlit-Default` materyali atanmali
   (yoksa 2D isik olmadigi icin simsiyah cikar).
3. Ayni frame'de SetTile + render calismaz (deferred mesh) -> bake SpriteRenderer kompozisyonu
   ile yapilir. SAHNE boyamasinda bu sorun yok (kalici tilemap, sonraki frame render olur).
4. Sorting taklidi: sortingOrder = katmanOrder * 2000 + (sabit - (cx + cy)).
5. Tuzak parcalar: Wall A7 payandali (rastgele karistirma!), Ground A1 = 3D BLOK tile
   (duz zemin icin C-serisi C3/C4/C6 kullan), Misc B7 aslinda su yalagi (katalog etiketi yanlis),
   cliff _S varyantlari kolonda dikissiz degil.
6. Zemin karisimlarinda acik/koyu tile'lari harmanlama — damali gorunur; tek aile + ayni ton.

## Yan sekme / baska oturum icin: boyama talimati nasil verilmeli?

Talimat su bilgileri icermeli (eksikse Claude sorup netlestirir):

```
HEDEF: NewGameScene/Grid full-map dry-run | tek-stamp preview | yeni sprite bake
K4 SABITLERI: frontline -0.5; wall clearance -1.5..1.5; moat 1.5..4;
  battlefield 4..18; far-right frame 18..27; hidden spawn X 27..29 / Y +-6.5;
  camera ortho 8, Android max aspect 2.4
KORUNACAKLAR: outside*, 40 okcu slotu, 5 exact VillageMarkers,
  marker->keep lojistik koridorlari
TEMA: sol tam ve kaynak-okunur; sag kenarlari mikro-detayli, merkez combat alani bos
ONAY DONGUSU: Full Map Composer dry-run -> bolge/Game Camera screenshot ->
  owner onayi -> daha sonraki ayri, atomik apply fazi
PHASE 4 SINIRI: gercek tilemap'e SetTile yok, SaveScene yok
```

Onerilen akis: once mevcut `NewGameScene/Grid` ve gameplay kontratlarini oku; layout'u aggregate
dry-run ve screenshot ile owner'a goster. Phase 4'te kalici boyama yapma. Daha sonraki ayri apply
fazinda tum layout tek Undo grubunda ele alinmali; legacy gorsel retirement acik allowlist ile
yapilmali ve `outside*`/marker/gameplay katmanlari asla silinmemelidir. Straight-guide
corridor warning'i tam yapilari kesmek icin kullanilmaz; route-based cozum apply preflight'inin
ayri bir kosuludur.

## Ara ciktilarin yeri

- Bake'li sprite'lar: `Assets/Sprites/BakedStructures/` (repo'da, kalici)
- Katalog/sokum JSON'lari + uretici scriptler: Claude session scratchpad (GECICI — kaybolursa
  kalici hafizadaki recete ozeti + bu dokumanla yeniden uretilebilir; sokum parser'i ~5 dk)
- Kalici hafiza kaydi: Claude memory `structure-sprite-baker.md` (faz durumu + tum dersler)
