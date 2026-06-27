# DayNightPrepSystem - Editor Setup

## Gereken Authoring

SubScene icinde `MobileCastleConfig` objesinde `MobileCastleCombatAuthoring` bulunmali ve su alanlar ayarli olmali:

- Initial Day Prep Duration: `12`
- Day Prep Duration: `15`
- Day Overlay Alpha: `0`
- Night Overlay Alpha: `0.50`
- Unlimited Arrows: enabled

`Window -> DeadWalls -> Mobile Castle Scene Setup` bu alanlari idempotent olarak yazar.

## Play Beklentisi

- `NewGameScene` ilk acildiginda HUD `DAY 01 - 12s` benzeri bir sayac gosterir.
- Sayac bitince `NIGHT 01` baslar ve zombiler spawn olur.
- Wave temizlenince HUD `DAY 02 - 15s` benzeri hazirlik sayacina gecer.
- Stress mode acikken bu sistem sayac baslatmaz.

Unity scriptleri editor refresh sonrasi otomatik derler; bu sistem icin disaridan manuel compile komutu calistirilmez.
