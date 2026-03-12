# Resource Components - Editor Kurulum (M1.1)

## GameStateAuthoring Inspector Ayarlari

### Resources — Baslangic
| Alan | Varsayilan | Aciklama |
|------|-----------|----------|
| InitialWood | 100 | Baslangic ahsap miktari |
| InitialStone | 50 | Baslangic tas miktari |
| InitialIron | 20 | Baslangic demir miktari |
| InitialFood | 100 | Baslangic yemek miktari |

### Resources — Test Uretim (dk basina)
| Alan | Varsayilan | Aciklama |
|------|-----------|----------|
| TestWoodProdRate | 5 | Ahsap uretim hizi (dk basina) |
| TestStoneProdRate | 3 | Tas uretim hizi |
| TestIronProdRate | 2 | Demir uretim hizi |
| TestFoodProdRate | 4 | Yemek uretim hizi |

### Resources — Test Tuketim (dk basina)
| Alan | Varsayilan | Aciklama |
|------|-----------|----------|
| TestWoodConsRate | 0 | Ahsap tuketim hizi |
| TestStoneConsRate | 0 | Tas tuketim hizi |
| TestIronConsRate | 0 | Demir tuketim hizi |
| TestFoodConsRate | 0 | Yemek tuketim hizi |

## HUD Ayarlari
HUDController Inspector'inda "Resources" header'i altinda 4 TMP_Text referansi atanmali:
- **WoodText** — "Ahsap: 150 (+5.0/dk)"
- **StoneText** — "Tas: 50 (+3.0/dk)"
- **IronText** — "Demir: 20 (+2.0/dk)"
- **FoodText** — "Yemek: 100 (+4.0/dk)"

## Hizli Test
1. TestWoodProdRate=60 yap → Wood her saniye +1 artmali
2. TestWoodConsRate=120 yap → Wood her saniye -2 azalmali, 0'da durmali
3. HUD'da format: miktar + net hiz parantez icinde
