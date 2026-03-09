# UI - Editor Kurulum

## Canvas Olusturma
1. Hierarchy → UI → Canvas
2. Canvas Scaler: Scale With Screen Size, Reference: 1920x1080
3. Canvas'a UIManager component'i ekle

## HUD Panel
1. Canvas icinde Panel olustur → "HUDPanel"
2. HUDController component'i ekle
3. Icine su UI elemanlarini olustur:
   - **Wall HP Bar**: UI → Slider, Anchor: Top-Left, Fill: kirmizi
   - **Gate HP Bar**: UI → Slider, Fill: turuncu
   - **Castle HP Bar**: UI → Slider, Fill: yesil
   - **GoldText**: UI → TextMeshPro, Anchor: Top-Right
   - **XPText**: UI → TextMeshPro
   - **WaveText**: UI → TextMeshPro, Anchor: Top-Center
   - **LevelText**: UI → TextMeshPro
   - **ZombiesAliveText**: UI → TextMeshPro
4. HUDController referanslarini Inspector'da ata

## Level Up Panel
1. Canvas icinde Panel olustur → "LevelUpPanel"
2. LevelUpUI component'i ekle
3. Icine su elemanlari olustur:
   - **TitleText**: "Level Up!" TextMeshPro, buyuk font
   - **AddArcherButton**: UI → Button-TextMeshPro
   - **ArrowDamageButton**: UI → Button-TextMeshPro
   - **RepairGateButton**: UI → Button-TextMeshPro
4. Referanslari ata
5. Panel'i disable et (varsayilan kapali)

## Game Over Panel
1. Canvas icinde Panel olustur → "GameOverPanel"
2. GameOverUI component'i ekle
3. Icine su elemanlari olustur:
   - **GameOverText**: Buyuk "GAME OVER" text
   - **StatsText**: Istatistik text
   - **RestartButton**: Restart butonu
4. Referanslari ata
5. Panel'i disable et

## UIManager Referanslari
UIManager Inspector'inda:
- HUDPanel → HUDPanel objesi
- LevelUpPanel → LevelUpPanel objesi
- GameOverPanel → GameOverPanel objesi
