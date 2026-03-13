# Wall Slot Manager — Editor Kurulumu

## Scene'e Ekleme
1. Hierarchy > Create Empty → "WallSlotManager" adini ver
2. **WallSlotManager** component'i ekle
3. Inspector'da slot pozisyonlarini ayarla:
   - Slot 0: Position = (2.5, -4, -1) → sur alt kismi
   - Slot 1: Position = (2.5,  0, -1) → sur orta
   - Slot 2: Position = (2.5,  4, -1) → sur ust kismi
4. Maliyet degerlerini ayarla (default: 30W, 50S, 20I)
5. RequireBlacksmith: **true** (production), **false** (test bypass)

## Slot Pozisyonlarini Ayarlama
- Scene view'da WallSlotManager objesini sec
- Gizmos ile slot pozisyonlari gorunur (yesil = bos, kirmizi = dolu)
- Sur sprite'inin uzerine denk gelecek sekilde ayarla
- Z = -1 olmalideki mancinik sprite onden gorunsun

## Test Bypass
- WallSlotManager Inspector'inda **RequireBlacksmith** checkbox'ini kaldir
- Bu sayede Demirci binasi olmadan mancinik yerlestirilebilir
- Production'da tekrar **true** yapmayi unutma!

## Dogrulama
1. Play mode'da bina menusunden "Mancinik" sec
2. Ghost preview en yakin bos slot'a snap etmeli
3. Sol tikla → mancinik yerlesmeli, slot kirmiziya donmeli (Gizmos)
4. 3 slot doldugunda yeni yerlestirme yapilamaz olmali
5. Restart sonrasi tum slotlar yesile donmeli
