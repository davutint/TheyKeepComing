# UI - Mimari

## Genel Yapi
UGUI Canvas kullanilir (Screen Space - Overlay). MonoBehaviour UI controller'lari GameManager event'lerini dinler.

## UI Panelleri

### HUD Panel
- **HUDController.cs** kontrol eder
- Her frame guncellenir
- Slider'lar: Wall HP, Gate HP, Castle HP
- Text: Gold, XP, Wave, Level, Zombies Alive

### Level Up Panel
- **LevelUpUI.cs** kontrol eder
- GameManager.OnLevelUp event'i ile acilir
- 3 buton: Okcu Ekle / Ok Hasari / Kapi Tamir
- Time.timeScale = 0 (oyun duraklar)

### Game Over Panel
- **GameOverUI.cs** kontrol eder
- GameManager.OnGameOver event'i ile acilir
- Istatistikler: Wave, Level, Gold
- Restart butonu

## Panel Yonetimi
UIManager singleton tum panel'lerin acilip kapanmasini yonetir.
Time.timeScale ile oyun duraklatma/devam ettirme yapar.
