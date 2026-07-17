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

## Population Runtime Tuning

### Population Economy

| Alan | Varsayilan | Aciklama |
|---|---:|---|
| Initial Bed Capacity | 60 | `MobileCastleCombatAuthoring` sahibi; run başlangıcındaki House yatak kapasitesi |
| Population Growth Per Day Prep | 15 | `DifficultyProfileSO` sahibi; her tamamlanan Dawn/cycle için istenen survivor sayısı |
| Food Cost Per Arrival | 1 | `DifficultyProfileSO` sahibi; kabul edilen her yeni survivor için tek seferlik Food |

`NewGameScene/MobileCastleCombatSubScene` içindeki `MobileCastleConfig` authoring fallback'lerini
`60 / 15 / 1` olarak serialize eder. Aktif profile varken request/Food profile'dan gelir;
profile yoksa authoring fallback olur. Aynı subscene'deki `GameStateAuthoring.InitialCapacity = 60`
değeridir; eski `999999` mobile kapasite aynası kaldırılmıştır.

Bed fiyatı `DefaultDifficulty.asset` içindeki `BedBaseWoodCost` ve
`BedCostGrowthCapacityInterval` alanlarından gelir. Onaylı default `100 Wood` ve
`25 owned bed interval` değeridir; quadratic eğri korunur. Bu alanlar
`Window > DeadWalls > Difficulty Tuner > Population Runtime Contract` bölümünden Dawn request
ve Food/arrival ile birlikte ayarlanır ve Play Mode'da canlı uygulanabilir.

## Test Senaryolari

### 1. Temel Idle Hesaplama
- Total=10, Workers=3, Archers=2 → Idle=5
- HUD: "Nufus: 10/20 (3 isci, 2 okcu, 5 bos)"

### 2. Legacy / Non-Mobile Yemek Tuketimi
- Workers=5, Archers=5, FoodPerAssignedPerMin=2.0
- → FoodPerMin = 10 * 2.0 = 20.0/dk
- → HUD'da yemek: "Yemek: X (-16.0/dk)" (uretim 4.0 - tuketim 20.0 = -16.0 net)

Mobile V1 castle loop'ta bu pasif nüfus tüketimi uygulanmaz.

### 3. Sifir Clamp
- Workers=8, Archers=5 (toplam 13 > Total 10)
- → Idle = max(0, 10-13) = 0

### 4. Yemek Bittiyse
- Yemek 0'a dustugunde tuketim durur (ResourceTickSystem accumulator mantigi)

### 5. Restart
- Game Over → Restart → nufus baslangic degerlerine doner
- Legacy/non-mobile authoring örneği: Total=10, Workers=0, Archers=0, Idle=10, Capacity=20
- Mobile castle loop: Capacity başlangıç House bed state'ine, varsayılan olarak `60` değerine döner

### 6. Mobile Dawn Arrival Budget

- Başlangıç: Total=`60`, toplam yatak=`65`, Food=`3`
- Config: requested=`15`, FoodCostPerArrival=`1`
- Beklenen: accepted=`3`, Total=`63`, Capacity=`65`
- Beklenen Food: `0`; kabul edilen `3` survivor için toplam `3 Food` aynı Dawn'da yalnız bir kez düşer

## HUD Gosterimi
- TMP_Text field: `PopulationText`
- Format: `"Nufus: {Total}/{Capacity} ({Workers} isci, {Archers} okcu, {Idle} bos)"`
- Canvas'ta yeni bir TMP_Text objesi eklenmeli ve HUDController'a atanmali

## Unity Editor Adimlari
1. GameStateAuthoring Inspector'inda yeni Population field'larini ayarla
2. HUD Canvas'a 1 TMP_Text objesi ekle (PopulationText)
3. HUDController Inspector'ina PopulationText referansini ata
4. Play mode'da Entity Debugger'dan PopulationState degerlerini dogrula
5. Mobile testte `MobileBedCapacityState` için `BaseCapacity=60`, `PurchasedCapacity=0` başlangıcını doğrula
6. `DefaultDifficulty.asset` içinde request=`15`, Food/arrival=`1`; authoring initial bed=`60` doğrula
7. `Population Runtime Contract` preview ve Play Mode telemetry'sinde aynı next-Dawn bütçesini doğrula
