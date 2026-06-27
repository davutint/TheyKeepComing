# Arena Map Generator — Editor Setup / Kullanim

Adim adim kullanim. (Kod tarafi hazir; bu dosya "nasil kullanirim"i anlatir.)

## Onkosul (bir kez)
1. `NewGameScene.unity`'yi ac (Project: `Assets/Scenes/NewGameScene.unity` -> cift tikla).
2. Sahnede `WorldVisualRoot/MobileArenaGrid` olmali. Yoksa once:
   `Window > DeadWalls > Mobile Castle Scene Setup` -> "Setup NewGameScene". Bu, arena grid'ini + kaleyi kurar.

## Tool'u acma
- Menu: `Window > DeadWalls > Arena Map Generator`. Pencere acilir (sol/sag panele dock edebilirsin).

## Tek-tik uretim dongusu (ana kullanim)
1. **Seed** alanina bir sayi gir veya **Rastgele** butonuna bas.
2. **GENERATE** (buyuk yesil buton) -> arena ANINDA boyanir.
3. **Game view**'a (veya Scene view) bak: uretilen harita gercek arenadir.
4. Begenmedin mi? **Rastgele** -> tekrar **GENERATE**. Veya **< onceki / sonraki >** ile seed'i 1 adim oynatip otomatik uret.
5. Begendin -> birak, zaten sahnede kayitli. Yanlis oldu -> **Ctrl+Z** (tek Undo hepsini geri alir).
6. Tum uretileni silmek icin **Temizle (generator katmanlari)**.

> Not: Pencere ici mini onizleme YOK -- onizleme dogrudan Scene/Game view'dir (en dogru gorunum). Bu bilincli tercih.

## Kontroller ne yapar
- **Seed / Rastgele / < onceki / sonraki >:** ureteci besleyen sayi. Ayni seed = ayni harita. Sonsuz varyasyon icin cevir.
- **Biome agirliklari (Cim / Toprak / Kayalik):** goreli oran. Cim'i artir -> daha cesil arena. Hepsi 0..10.
- **Sari cim yamalari:** aciksa bazi cim alanlari kuru/sari cime doner (cesitlilik).
- **Biome olcek:** noise frekansi. Dusuk = buyuk yumusak bolgeler; yuksek = kucuk parcali desen.
- **Dekor yogunlugu (Agac / Kaya / Cali):** hucre basina yerlesme sansi (0..0.3). 0 = o dekor yok.
  Kaya = curated kucuk-kaya listesi (Stone A2/A3/A8/A9; dikilitaş/mezar/kuyu HARIC). "Misc" (comlek/sandik/kuyu) bilincli olarak YOK.
- **Agac golgesi:** agaclarin altina golge decal koyar (izo derinlik hissi).
- **Harabe (varsayilan KAPALI):** dagilmis kirik duvar/tas/kapi set parcalari. Ac ve ayri degerlendir.
- **Kule + Kule sayisi (varsayilan KAPALI):** dis halkaya birkac dekoratif kule (duvar + cati; cati offset deneysel).
- **Yol (varsayilan KAPALI):** kale kenarindan disa doseme patikalari (gri durabilir).
- **Arena yaricap:** uretilen diamond'in buyuklugu (varsayilan 22 = mevcut arena).
- **Merkez temiz yaricap:** kale + savas alaninin etrafinda dekor/yapi konmayan bos halka (varsayilan 5).

## Sorun giderme
- **"WorldVisualRoot/MobileArenaGrid bulunamadi"**: NewGameScene acik degil ya da arena kurulmamis -> Mobile Castle Scene Setup calistir.
- **Bir biome/dekor cikmiyor**: ilgili slider 0 olabilir; veya tile bulunamadi (Console'da uyari). Tile isimleri
  `Assets/SmallScaleInt/Fantasy kingdom Tileset/Environment/Tiles` altinda beklenir.
- **Tile katalogu eski**: yeni tile eklediysen pencereyi kapatip tekrar ac (katalog pencere acilista cache'lenir).
- **Dekor kalenin onunde/garip duruyor**: merkez temiz yaricapi artir; veya ilgili dekor yogunlugunu dusur.

## Sonraki adimlar (v2 fikirleri)
- Cok-kademeli bina kompozisyonu (Basement + Wall + Roof tier'lari).
- Tek birlesik prop katmani + per-tile izo sort (cross-layer sorting kusursuzlugu).
- Gameplay-etkili yapilar (ECS collider + pathing) -- ayri tasarim.
- Su/foam bolgeleri, clip/elevation (G ailesi) ile yukselti.
