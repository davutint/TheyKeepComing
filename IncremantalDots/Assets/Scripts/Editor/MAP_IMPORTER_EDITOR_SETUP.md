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
- **Ground** → zemin layer tilemap'i
- **Buildable** → yerlesilebilir alan tilemap'i
- **Resources** → dogal kaynak tilemap'i

### 3. Tile Slot'larini Doldur
Fantasy Kingdom tileset'inden uygun tile'lari sec:

**Ground Tile'lari** (4 slot):
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

### 4. Boyama
- **Paint Ground** → sadece ground layer'i boyar
- **Paint Buildable** → sadece buildable layer'i boyar
- **Paint Resources** → sadece resources layer'i boyar
- **TUMUNU BOYA** → 3 layer'i tek seferde boyar (tek Ctrl+Z ile geri alinir)

### 5. Temizleme
- **Clear Ground/Buildable/Resources** → ilgili tilemap'in tum tile'larini siler

### 6. Tile Degistirme
Tile begenmezsen:
1. Slot'a farkli tile ata
2. Tekrar Paint tikla → eski tile'lar yenisiyle degisir

## Koordinat Offset
- Varsayilan (0,0) — JSON haritasi tilemap'in (0,0)'indan baslar
- Haritayi kaydirmak istersen X/Y degerlerini degistir
- Ornek: offset (5,5) → tum hucreler 5 birim saga ve yukari kayar

## Persistence
- Tum slot atamalari (tile'lar, tilemap'ler, offset) otomatik kaydedilir
- Window kapatip acinca ayarlar korunur
- Farkli projede calisirken slot'lar sifirlanir (EditorPrefs proje-bagimsiz degildir)

## Prosedural Uretim Kullanimi

### Onkoşul
Tilemap referanslari ve tile slot'lari (Adim 2 + Adim 3) atanmis olmali.

### Hizli Baslangiç
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
- **GENERATE + PAINT**: Uretir + 3 layer'i boyar (onceki tile'lar temizlenir, Ctrl+Z ile geri alinir)
- **Varsayilanlara Don**: Tum parametreleri fabrika ayarina dondurur

## Dogrulama Checklist — JSON Import
- [ ] JSON parse → "40x40" bilgisi gorundumu
- [ ] Tilemap'ler suruklendi
- [ ] Tile slot'lari dolu
- [ ] "Ground Boya" → Scene'de harita gorundumu
- [ ] Ctrl+Z → geri alindimi
- [ ] Farkli tile ata → tekrar boya → degistimi
- [ ] Window kapat-ac → slot'lar korundumu
- [ ] "Tumunu Boya" → 3 layer birden boyandimi

## Dogrulama Checklist — Prosedural Uretim
- [ ] Prosedural Uretim foldout'u gorunuyor mu
- [ ] Seed=42 → "Sadece Generate" → status "Prosedural 150x170, seed=42"
- [ ] "Generate + Paint" → 3 layer Scene'de boyaniyor mu
- [ ] Seed degistir → tekrar Generate + Paint → harita degisti mi
- [ ] "Rastgele Seed" → farkli seed olustu mu
- [ ] Ctrl+Z → tum boyama geri alindi mi
- [ ] Slider'lari degistir → Generate → terrain dagilimi degisti mi
- [ ] Window kapat-ac → parametreler korundu mu
- [ ] Rocky esigini artir → rocky alani buyudu mu
