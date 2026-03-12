# PopulationState — Editor / Inspector Ayarlari

## GameStateAuthoring Inspector

### Population — Baslangic
| Alan | Varsayilan | Aciklama |
|------|-----------|----------|
| Initial Population | 10 | Oyun basinda toplam nufus |
| Initial Capacity | 20 | Maksimum nufus kapasitesi |

### Population — Test Atama
| Alan | Varsayilan | Aciklama |
|------|-----------|----------|
| Test Workers | 0 | Test icin isci sayisi (M1.4'e kadar elle ayarlanir) |
| Test Archers | 0 | Test icin okcu sayisi (M1.6'ya kadar elle ayarlanir) |

### Population — Tuketim
| Alan | Varsayilan | Aciklama |
|------|-----------|----------|
| Food Per Assigned Per Min | 2.0 | Her atanmis bireyin dakika basina yemek tuketimi |

## Test Senaryolari

### 1. Temel Idle Hesaplama
- Total=10, Workers=3, Archers=2 → Idle=5
- HUD: "Nufus: 10/20 (3 isci, 2 okcu, 5 bos)"

### 2. Yemek Tuketimi
- Workers=5, Archers=5, FoodPerAssignedPerMin=2.0
- → FoodPerMin = 10 * 2.0 = 20.0/dk
- → HUD'da yemek: "Yemek: X (-16.0/dk)" (uretim 4.0 - tuketim 20.0 = -16.0 net)

### 3. Sifir Clamp
- Workers=8, Archers=5 (toplam 13 > Total 10)
- → Idle = max(0, 10-13) = 0

### 4. Yemek Bittiyse
- Yemek 0'a dustugunde tuketim durur (ResourceTickSystem accumulator mantigi)

### 5. Restart
- Game Over → Restart → nufus baslangic degerlerine doner
- Total=10, Workers=0, Archers=0, Idle=10, Capacity=20

## HUD Gosterimi
- TMP_Text field: `PopulationText`
- Format: `"Nufus: {Total}/{Capacity} ({Workers} isci, {Archers} okcu, {Idle} bos)"`
- Canvas'ta yeni bir TMP_Text objesi eklenmeli ve HUDController'a atanmali

## Unity Editor Adimlari
1. GameStateAuthoring Inspector'inda yeni Population field'larini ayarla
2. HUD Canvas'a 1 TMP_Text objesi ekle (PopulationText)
3. HUDController Inspector'ina PopulationText referansini ata
4. Play mode'da Entity Debugger'dan PopulationState degerlerini dogrula
