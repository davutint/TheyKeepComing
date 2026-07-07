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
2. **Sahne tilemap boyama (henuz kullanilmadi ama ayni altyapiyla hazir):** NewGameScene'deki
   `WorldVisualRoot/MobileArenaGrid` tilemap katmanlarina dogrudan SetTile — biome zemin,
   hendek bandi, sur hatti, dekor. K4 tek-cephe yeniden boyamasi icin uygun.

DURUM: Kalici bir EditorWindow HENUZ YOK (Faz 3 backlog'da). Pipeline su an Claude oturumu +
Unity acik + MCP server gerektirir. Ureticiler seed'li Python scriptleri + genel C# baker;
tum recete bilgisi bu dokumanda ve kalici hafizada oldugu icin her oturumda yeniden kurulabilir.

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

1. Paket natif grid: cellLayout=Isometric, cellSize=(1, 0.5, 1); sprite 256x256, PPU 128,
   pivot ~(0.5, 0.19). Sahnedeki MobileArenaGrid ise cellSize (4,2,4) + scale 0.35 kullanir.
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
HEDEF: sahne boyama (WorldVisualRoot) | yeni sprite bake
KATMANLAR: GroundTilemap(-50) / CastleGroundTilemap(-40) / CastleWallTilemap(-30) / CastlePropsTilemap(-20)
BOLGE: hucre koordinat araligi (MobileArenaGrid hucre uzayinda) veya "komple arena"
K4 SABITLERI (degistiyse guncelle): FrontlineX -6, hendek bandi [-4,-1.5], SpawnLineX 13,
  kamera (4.5, 0) ortho 8, CastleCenter (0,0)
TEMA: biome (cim/toprak/kuru), sol taraf kale/koy yogunlugu, sag taraf issizlik derecesi
KORUNACAKLAR: silinmemesi/uzerine boyanmamasi gereken mevcut icerik
ONAY DONGUSU: once tek-seferlik onizleme (screenshot/bake) -> onay -> kalici boyama + sahne save
```

Onerilen akis: Claude once mevcut tilemap icerigini okur (dump), plan onerir, kucuk bir bolgeyi
boyayip screenshot ile gosterir, onaydan sonra tamamini boyar. Kalici sahne degisikligi oncesi
sahne yedegi/commit onerilir.

## Ara ciktilarin yeri

- Bake'li sprite'lar: `Assets/Sprites/BakedStructures/` (repo'da, kalici)
- Katalog/sokum JSON'lari + uretici scriptler: Claude session scratchpad (GECICI — kaybolursa
  kalici hafizadaki recete ozeti + bu dokumanla yeniden uretilebilir; sokum parser'i ~5 dk)
- Kalici hafiza kaydi: Claude memory `structure-sprite-baker.md` (faz durumu + tum dersler)
