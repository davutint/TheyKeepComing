# Ground Tile Mapper — Editor Kurulum Rehberi

## Window'u Acma
`Window > DeadWalls > Ground Tile Mapper`

## Adim Adim Kullanim

### 1. Paleti Incele
Window'un alt kismindaki **REFERANS PALETI** bolumunde tum Ground A-serisi tile'larin thumbnail'leri gorunur. Her tile'in kisa ismi altinda yazar (A2_E, A3_E, A3_N vs.).

### 2. Pattern'lari Doldur
**MASK ESLEME** bolumunde 16 satir var. Her satir bir komsu pattern'ini temsil eder:
- Renkli harfler: **K**(uzey) **D**(ogu) **G**(uney) **B**(ati)
  - Yesil = cimen komsu
  - Kirmizi = toprak komsu
- Sag taraftaki **aciklama** ne durumu oldugunu yazar (ornek: "1 toprak (K)")

Her satirin **ObjectField** slot'una uygun tile'i surukle:
1. Palete bak → hangi sprite bu pattern'a uyuyor?
2. Project panelinden (veya paletten ismi not alarak) o tile'i slot'a surukle
3. Thumbnail aninda guncellenir

### 3. Onerilen Atamalar
| Mask | Durum | Muhtemel Tile |
|------|-------|---------------|
| 15 | Tam cimen (4 komsu cimen) | Ground A2_E |
| 0 | Izole (4 komsu toprak) | null veya scatter (A6-A12) |
| 7,11,13,14 | 1 kenar toprak | A3 veya A4 varyantlari |
| 3,6,9,12 | 2 bitisik kenar toprak (kose) | A3 varyantlari |
| 5,10 | 2 karsilikli kenar toprak | A5 veya ozel tile |

> **NOT:** Hangi tile hangi pattern'a uyar, sprite'a bakarak GORUNLE belirle. Yukaridaki tablo sadece baslangic noktasi.

### 4. Kaydet
Atamalar otomatik kaydedilir (EditorPrefs). Window kapatip acinca korunur.

### 5. Map Importer'da Kullan
1. `Window > DeadWalls > Map Importer` ac
2. Ground Base tilemap atanmis olmali
3. **GENERATE + PAINT** tikla
4. Console'da `"Ground 2-layer boyandi: 25500 hucre (mapper)"` mesaji gorulmeli

### 6. Sonucu Kontrol Et
Scene'de haritaya bak:
- Tam cimen alanlarinda A2 gorunuyor mu?
- Cimen-toprak sinirinda gecis tile'lari dogru mu?
- Yanlis gorunen tile varsa → Mapper window'da o mask'in slot'unu degistir → tekrar Paint

## MapImporter ile Iliski
- Mapper esleme varsa: her cimen hucresi icin 4-bit mask hesaplanir, tablodaki tile kullanilir
- Mapper esleme yoksa: `_grassOverlayTile` (RuleTile) fallback kullanilir
- Ikisi de yoksa: overlay bos kalir, sadece base A1 gorunur

## Dogrulama Checklist
- [ ] Window > DeadWalls > Ground Tile Mapper aciliyor mu
- [ ] Palet bolumunde A-serisi tile thumbnail'leri gorunuyor mu
- [ ] Mask 15 (tam cimen) slot'una A2_E ataninca thumbnail gorunuyor mu
- [ ] Map Importer'da Generate + Paint → Console'da "(mapper)" mesaji
- [ ] Scene'de cimen-toprak sinirinda atanan tile'lar gorunuyor mu
- [ ] Farkli tile ata → tekrar Paint → degisti mi
- [ ] Window kapat-ac → atamalar korundu mu
- [ ] "Tum Atamalari Temizle" → 16 slot bosaldi mi
