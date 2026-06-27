# Archer Animation State System - Editor Setup

## Gereksinimler

- Okcu prefab'inda `ArcherAuthoring` bulunmali.
- Okcu prefab'inda veya SubScene instance'inda `SpriteSheetAuthoring` bulunmali.
- `SpriteSheetAuthoring` atlas layout'u Character Creator formatinda olmali:
  - Row `0-7`: Walk
  - Row `8-15`: Attack
  - Row `16-23`: Die
  - Row `24-31`: Idle

## Kontrol

`NewGameScene` Play modunda okcu bosken yurume animasyonu oynamamali. Idle row oynuyorsa sistem dogru calisiyor demektir.

Tool tarafinda `BasicArcher_01` icin `SpriteSheetAuthoring.DirectionRow = 24` yazilir. Runtime'da spawn edilen yeni okculari ise sistem kendisi idle row'a ceker.

Okcu ates ederken:
- Hedef zombiye dogru yon row'u secilmeli.
- Kisa sure `Attack` row'u oynatilmali.
- Atis bittikten sonra ayni yonde `Idle` row'una donmeli.
