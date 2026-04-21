# Map Importer — Editor Kurulum Rehberi

## Window'u Acma
`Window > DeadWalls > Map Importer`

## Adim Adim Kullanim

### 1. JSON Dosyasini Yukle
1. **JSON File** slot'una `dead_wall_map` TextAsset'ini surukle (Assets/ klasorunde)
2. **Parse JSON** butonuna tikla
3. Status: `"40x40, 3 layer yuklendi"` gorunmeli
4. Stratejik Bilgi bolumunde Castle ve Zombie Spawn bilgileri gorulecek

### 2. Tilemap Referanslarini Ata
Scene'deki Grid objesinin altindaki Tilemap'leri surukle:
- **Ground Base** → base zemin tilemap'i (A1 opak toprak, sorting order -1)
- **Ground (Overlay)** → cimen overlay tilemap'i (sorting order 0)
- **Buildable** → yerlesilebilir alan tilemap'i
- **Resources** → dogal kaynak tilemap'i

> **ground_base tilemap yoksa:** Grid objesine sag tikla → 2D Object → Tilemap → Isometric. Isim: `ground_base`. TilemapRenderer Sorting Order: mevcut Ground'dan 1 dusuk (orn: Ground=0 ise, ground_base=-1).

### 3. IsometricRuleTile Olustur (2-Katman Ground)
Bu adim cimen-toprak gecislerinin otomatik dogru sprite ile boyanmasi icin GEREKLI.

1. Project panelinde uygun bir klasore git (orn: `Assets/Art/Tiles`)
2. Sag tikla → **Create → 2D → Tiles → Isometric Rule Tile**
3. Isim: `GrassRuleTile`
4. Inspector'da:
   - **Default Sprite**: `Ground A2_E` ata (Sprites klasorunden)
   - Bu, tum komsulari cimen olan hucrelerin sprite'i
5. Kural ekle (+ butonu):
   - Her kural icin komsuluk pattern'i belirle (yesil ok = ayni tile, kirmizi X = farkli)
   - O pattern'a uygun A-serisi sprite'i ata (orn: tek kenar eksik → A3_E)
   - Sprite secimini Unity'de gorsel olarak yap

### 4. Tile Slot'larini Doldur

**Ground Tile'lari — 2-Katman Modu (ONERILEN):**
- **Grass Overlay (RuleTile)** → Adim 3'te olusturdugun `GrassRuleTile`'i surukle

**Ground Tile'lari — Fallback (eski mod, RuleTile yoksa):**
- **Grass** → cimen zemin tile'i
- **Dark Grass** → koyu cimen tile'i
- **Dirt** → toprak tile'i
- **Rocky** → kayalik tile'i

**Buildable Zone Tile** (1 slot):
- **Buildable** → yerlesilebilir alan isaretleyici tile'i

**Resource Tile'lari** (3 slot):
- **Forest** → orman tile'i
- **Stone** → tas tile'i
- **Iron** → demir tile'i

### 5. Boyama
- **Paint Ground** → 2-katman modunda: base'e A1, overlay'e RuleTile boyar
- **Paint Buildable** → sadece buildable layer'i boyar
- **Paint Resources** → sadece resources layer'i boyar
- **TUMUNU BOYA** → tum layer'lari tek seferde boyar (tek Ctrl+Z ile geri alinir)

### 6. Temizleme
- **Clear Ground** → hem base hem overlay tilemap'i temizler
- **Clear Buildable/Resources** → ilgili tilemap'in tum tile'larini siler

### 7. Tile Degistirme
Tile begenmezsen:
1. Slot'a farkli tile ata
2. Tekrar Paint tikla → eski tile'lar yenisiyle degisir

## 2-Katman Ground Nasil Calisir?

```
ground_base tilemap (sorting order -1)
  └── A1_E tile HER hucreye → opak toprak, asla seffaf alan yok

ground tilemap (sorting order 0 — overlay)
  └── Cimen hucrelerine: IsometricRuleTile (komsuluga gore otomatik gecis sprite'i)
  └── Toprak hucrelerine: bos (base'den A1 gorunur)
```

- A1 = opak 3D toprak blok — base katman olarak her hucreyi kaplar
- A2+ = seffaf cimen overlay'leri — sadece cimen hucrelerine konur
- RuleTile komsulara bakarak cimen-toprak sinirinda otomatik dogru gecis tile'ini secer
- `_grassOverlayTile == null` ise fallback: eski tek-tilemap slot modu calisir

## Koordinat Offset
- Varsayilan (0,0) — JSON haritasi tilemap'in (0,0)'indan baslar
- Haritayi kaydirmak istersen X/Y degerlerini degistir
- Ornek: offset (5,5) → tum hucreler 5 birim saga ve yukari kayar

## Persistence
- Tum slot atamalari (tile'lar, tilemap'ler, offset) otomatik kaydedilir
- Window kapatip acinca ayarlar korunur
- Farkli projede calisirken slot'lar sifirlanir (EditorPrefs proje-bagimsiz degildir)

## Prosedural Uretim Kullanimi

### Onkosul
- Tilemap referanslari (Adim 2) atanmis olmali
- **2-Katman modu (onerilen)**: Ground Base tilemap + Grass Overlay (RuleTile) atanmis olmali
- **Fallback**: RuleTile yoksa eski Ground tile slot'lari atanmis olmali

### Hizli Baslangic
1. Window'da **PROSEDÜREL ÜRETIM** foldout'unu ac
2. **GENERATE + PAINT** tikla → varsayilan parametrelerle harita uretilir ve boyanir
3. Begenmediysen **Seed** degerini degistir veya **Rastgele Seed** tikla
4. Tekrar **GENERATE + PAINT** → yeni harita

### Parametre Ayari
- **Harita Boyutu**: Genislik/Yukseklik (varsayilan 150x170)
- **Seed**: Ayni seed → ayni harita. Farkli seed → farkli harita
- **Ground Noise**: `Noise Scale` buyutulurse terrain yamalari kuculur, `Octave Sayisi` arttikca detay artar
  - `Domain Warp` artarsa terrain daha organik/bulutsu olur (0 = duz fBM, 30+ = organik)
  - `Smoothing` artarsa kenar gecisleri daha yumusak olur (0 = ham, 1-2 = ideal)
- **Esikler**: Rocky/Dirt/Dark Grass esikleri — slider'lar otomatik sirali kalir
  - Rocky esigini artirirsan rocky alan buyur
  - Dark Grass esigini azaltirsan grass alan buyur
- **Buildable Zone**: Kale pozisyonu + yaricap + kenar noise genlik
  - `Sinir Genlik` artarsa kenar daha dalgali olur (amip seklinde)
  - `Zombie Sinir` sag tarafi keser (zombie spawn bolgesi)
- **Kaynaklar**: Her kaynak tipi icin yogunluk slider'i
  - `Kenar Yanliligi` artarsa ormanlar harita kenarlarinda toplanir
  - `Kayalik Bonus` artarsa tas kaynaklari rocky zemin uzerinde daha yogun olur

### Sadece Generate vs Generate + Paint
- **SADECE GENERATE**: Veriyi uretir ama boyamaz — Stratejik Bilgi bolumunde sonucu gorursun
- **GENERATE + PAINT**: Uretir + tum layer'lari boyar (onceki tile'lar temizlenir, Ctrl+Z ile geri alinir)
- **Varsayilanlara Don**: Tum parametreleri fabrika ayarina dondurur

## Dogrulama Checklist — 2-Katman Ground
- [ ] ground_base tilemap olusturuldu mu (sorting order -1)
- [ ] IsometricRuleTile olusturuldu mu (GrassRuleTile)
- [ ] Map Importer'da Ground Base slot'una ground_base tilemap suruklendi mi
- [ ] Grass Overlay slot'una GrassRuleTile suruklendi mi
- [ ] "Generate + Paint" → Console'da "Ground 2-layer boyandi: 25500 hucre" mesaji
- [ ] Scene'de: opak toprak base + uzerinde seffaf cimen overlay gorunuyor mu
- [ ] Cimen-toprak sinirinda RuleTile gecis sprite'lari dogru secilmis mi
- [ ] Buildable zone ve Resources layer'lari cimen/toprak uzerinde gorunuyor mu
- [ ] Ctrl+Z → tum layer'lar geri aliniyor mu
- [ ] Seed degistir → GENERATE + PAINT → harita degisiyor mu

## Dogrulama Checklist — JSON Import
- [ ] JSON parse → "40x40" bilgisi gorundumu
- [ ] Tilemap'ler suruklendi
- [ ] Tile slot'lari dolu
- [ ] "Ground Boya" → Scene'de harita gorundumu
- [ ] Ctrl+Z → geri alindimi
- [ ] Farkli tile ata → tekrar boya → degistimi
- [ ] Window kapat-ac → slot'lar korundumu
- [ ] "Tumunu Boya" → tum layer'lar birden boyandimi

## Dogrulama Checklist — Prosedural Uretim
- [ ] Prosedural Uretim foldout'u gorunuyor mu
- [ ] Seed=42 → "Sadece Generate" → status "Prosedural 150x170, seed=42"
- [ ] "Generate + Paint" → tum layer'lar Scene'de boyaniyor mu
- [ ] Seed degistir → tekrar Generate + Paint → harita degisti mi
- [ ] "Rastgele Seed" → farkli seed olustu mu
- [ ] Ctrl+Z → tum boyama geri alindi mi
- [ ] Slider'lari degistir → Generate → terrain dagilimi degisti mi
- [ ] Window kapat-ac → parametreler korundu mu
- [ ] Rocky esigini artir → rocky alani buyudu mu
