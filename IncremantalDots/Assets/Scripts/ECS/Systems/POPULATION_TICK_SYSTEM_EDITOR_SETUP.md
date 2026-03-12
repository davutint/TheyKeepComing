# PopulationTickSystem — Editor / Test Rehberi

## Onkosuller
- GameStateAuthoring sahnesinde olmali (PopulationState + GameStateData + ResourceConsumptionRate singleton'lari)
- Population field'lari GameStateAuthoring Inspector'indan ayarlanmis olmali

## Test Adimlari

### 1. Temel Calismasi
1. Play mode'a gir
2. Entity Debugger'da GameState entity'sini bul
3. PopulationState component'inin degerlerini kontrol et
4. Idle = Total - Workers - Archers olmali

### 2. Yemek Tuketimi Entegrasyonu
1. GameStateAuthoring Inspector'inda:
   - TestWorkers = 5
   - TestArchers = 3
   - FoodPerAssignedPerMin = 2.0
2. Play mode'a gir
3. Entity Debugger'da ResourceConsumptionRate.FoodPerMin = 16.0 olmali (8 * 2.0)
4. HUD'da yemek gosterimi: net hiz = uretim - tuketim (orn: 4.0 - 16.0 = -12.0)

### 3. Sifir Atama Testi
1. TestWorkers = 0, TestArchers = 0
2. ResourceConsumptionRate.FoodPerMin = 0 olmali
3. Yemek tuketimi olmamalı

### 4. Kapasite Asimi
1. TestWorkers = 8, TestArchers = 5 (toplam 13 > Total 10)
2. Idle = 0 olmali (negatif degil)
3. FoodPerMin = 13 * FoodPerAssignedPerMin (assigned hala toplam)

### 5. GameOver Durumu
1. Oyunu GameOver'a getir (kaleyi dusur)
2. PopulationTickSystem calismayi durdurmali
3. FoodPerMin degismemeli

### 6. Restart
1. GameOver sonrasi Restart butonuna bas
2. PopulationState baslangic degerlerine donmeli
3. FoodPerMin = 0 olmali (Workers=0, Archers=0)

## Performans
- Beklenen sure: <0.01ms (tek singleton okuma/yazma)
- Burst derlemesi aktif — overhead ihmal edilebilir

## Sorun Giderme
- **PopulationState gorunmuyor:** GameStateAuthoring baker'inin PopulationState ekledigini kontrol et
- **FoodPerMin guncellenmiyor:** PopulationTickSystem'in ResourceTickSystem'den once calistigini dogrula
- **Idle negatif:** Bu bir bug — math.max(0, ...) clamp'i calismali
