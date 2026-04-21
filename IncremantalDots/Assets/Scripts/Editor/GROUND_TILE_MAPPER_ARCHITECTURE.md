# Ground Tile Mapper — Mimari Dokuman

## Amac
4-bit komsu mask ile grass overlay tile esleme araci. Her cimen hucresinin 4 kardinal komsusuna (K/D/G/B) bakarak uygun Ground A-serisi tile'i secer. Esleme kullanici tarafindan gorsel olarak yapilir, MapImporterWindow tarafindan tuketilir.

## Dosya
- `GroundTileMapperWindow.cs` — tek Editor window dosyasi

## Mask Bit Tanimlamasi
```
Bit 3 (8) = Kuzey (row-1) — cimen mi?
Bit 2 (4) = Dogu  (col+1) — cimen mi?
Bit 1 (2) = Guney (row+1) — cimen mi?
Bit 0 (1) = Bati  (col-1) — cimen mi?
```

## 16 Pattern Tablosu
| Mask | Binary | K | D | G | B | Aciklama |
|------|--------|---|---|---|---|----------|
| 0    | 0000   | _ | _ | _ | _ | Izole (4 toprak) |
| 1    | 0001   | _ | _ | _ | C | Sadece bati cimen |
| 2    | 0010   | _ | _ | C | _ | Sadece guney cimen |
| 3    | 0011   | _ | _ | C | C | Guney+Bati cimen (kose) |
| 4    | 0100   | _ | C | _ | _ | Sadece dogu cimen |
| 5    | 0101   | _ | C | _ | C | Dogu+Bati cimen (karsilikli) |
| 6    | 0110   | _ | C | C | _ | Dogu+Guney cimen (kose) |
| 7    | 0111   | _ | C | C | C | Sadece kuzey toprak |
| 8    | 1000   | C | _ | _ | _ | Sadece kuzey cimen |
| 9    | 1001   | C | _ | _ | C | Kuzey+Bati cimen (kose) |
| 10   | 1010   | C | _ | C | _ | Kuzey+Guney cimen (karsilikli) |
| 11   | 1011   | C | _ | C | C | Sadece dogu toprak |
| 12   | 1100   | C | C | _ | _ | Kuzey+Dogu cimen (kose) |
| 13   | 1101   | C | C | _ | C | Sadece guney toprak |
| 14   | 1110   | C | C | C | _ | Sadece bati toprak |
| 15   | 1111   | C | C | C | C | Tam cimen (4 cimen) |

`C` = cimen komsu, `_` = toprak komsu

## MapImporterWindow Entegrasyonu

### Fallback Zinciri
```
PaintGround2Layer() icinde her cimen hucresi icin:
1. GroundTileMapper esleme var mi? → mask hesapla → mapperTiles[mask]
2. mapperTiles[mask] null mi? → _grassOverlayTile fallback
3. _grassOverlayTile da null mi? → null (base A1 gorunur)
```

### Komsu Kontrol
- `IsGrassAt(r, c)`: terrain == "grass" || terrain == "dark_grass"
- Sinir disi hucreler `true` doner (cimen sayilir → kenarlar temiz gorunur)

## EditorPrefs
- Key pattern: `"DeadWalls_GroundMapper_Mask0"` — `"DeadWalls_GroundMapper_Mask15"`
- Deger: tile asset GUID
- Iki window arasinda paylasilan iletisim kanali (coupling yok)

## Palet
- `AssetDatabase.FindAssets("Ground A t:TileBase")` ile tum A-serisi taranir
- `_0` varyantlari filtrelenir (duplikat)
- Isme gore siralanir
- Thumbnail + kisa isim gosterilir
