# ResourceTickSystem - Editor Kurulum (M1.1)

## Gereksinimler
- GameStateAuthoring Sub Scene icinde bake edilmis olmali
- GameStateAuthoring Inspector'inda kaynak field'lari ayarlanmis olmali
- HUDController'da 4 TMP_Text referansi atanmis olmali

## Inspector Ayarlari
GameStateAuthoring Inspector'indaki "Resources" header'lari altinda:
1. **Baslangic degerleri:** Wood=100, Stone=50, Iron=20, Food=100
2. **Test uretim hizlari:** Wood=5/dk, Stone=3/dk, Iron=2/dk, Food=4/dk
3. **Test tuketim hizlari:** Hepsi 0/dk (M1.2 nufus ile degisecek)

## Debug
- Entity Debugger'da GameState entity'sinde 4 yeni component gorunmeli:
  ResourceData, ResourceProductionRate, ResourceConsumptionRate, ResourceAccumulator
- ResourceData int degerleri zamanla artmali (uretim > tuketim ise)
- ResourceAccumulator float degerleri 0-1 arasinda sallanmali

## Play Mode Test
1. TestWoodProdRate=60 → Wood her saniye +1 artiyor mu?
2. TestWoodConsRate=120 → Wood azaliyor mu, 0'da duruyor mu?
3. HUD'da "Ahsap: 101 (+60.0/dk)" dogru gorunuyor mu?
4. Game Over → Restart → kaynaklar baslangic degerlerine donuyor mu?
5. ResourceTickSystem Systems window'da <0.01ms gostermeli
