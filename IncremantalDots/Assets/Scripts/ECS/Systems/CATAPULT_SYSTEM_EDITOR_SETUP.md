# Catapult System — Editor Kurulumu

## Onkosullar
- CatapultPrefab + CatapultProjectilePrefab olusturulmus olmali (bkz: CATAPULT_COMPONENTS_EDITOR_SETUP.md)
- WaveConfigAuthoring'e her iki prefab atanmis olmali
- WallSlotManager scene'e eklenmis olmali

## Test Adimlari

### 1. Temel Calisma Testi
1. Play mode'a gir
2. WallSlotManager.RequireBlacksmith = false yap (test bypass)
3. Bina menusunden Mancinik sec → slot'a yerlestir
4. Zombie dalga gelsin
5. **Gozle:** Mermi parabolik yol izliyor mu? AoE hasar uyguluyor mu?

### 2. Tas Tuketim Testi
1. Baslangic Stone miktarini not al
2. Mancinik atis yaptikca Stone dususunu HUD'da gozle
3. Stone = 0 olunca atesin durdugundan emin ol

### 3. RequireBlacksmith Testi
1. WallSlotManager.RequireBlacksmith = true yap
2. Demirci binasi OLMADAN mancinik butonuna tikla
3. Console'da "Demirci binasi gerekli!" mesaji gorulmeli
4. Demirci yap → tekrar tikla → ghost preview gorunmeli

### 4. Restart Testi
1. Mancinik yerlestir
2. GameManager.RestartGame() cagir (Game Over → Restart)
3. Slot'larin bosaldigini, mancinik entity'lerinin silindigini dogrula

## Entity Debugger ile Dogrulama
- CatapultUnit component'i olan entity'ler → yerlestirilen mancinikler
- CatapultProjectileTag olan entity'ler → havadaki mermiler
- Mermi isabet ettikten sonra CatapultProjectileTag entity sayisi dusmeli

## Bilinen Sinirlamalar
- Mancinik hedefi spawn aninda sabitlenir (tracking yok) — parabolik yol deterministik
- AoE hasar tum zombilere esit uygulanir (merkeze yakinlik bazli azalma YOK)
- Slot pozisyonlari Inspector'dan ayarlanir, runtime degistirilemez
