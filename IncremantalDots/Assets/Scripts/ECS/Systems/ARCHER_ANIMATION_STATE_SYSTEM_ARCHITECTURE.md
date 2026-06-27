# Archer Animation State System - Mimari

## Amac

`ArcherAnimationStateSystem`, okcularin hedef yonune bakmasini ve ok atarken kisa attack animasyonuna gecmesini saglar. `Archer.prefab` sprite sheet ayari walk row ile gelebilir; bu sistem runtime'da `ArcherUnit` entity'lerini idle/attack row'larina ceker.

## Davranis

- Query: `ArcherUnit` + `SpriteAnimation`
- `ArcherShootSystem`, ok atildiginda `FacingDirection` ve `AttackAnimTimer` yazar.
- `AttackAnimTimer > 0` ise hedef row `8 + direction` (`Attack`) olur.
- Timer bittiginde hedef row `24 + direction` (`Idle`) olur.
- Row zaten dogruysa animasyon frame'i resetlenmez; animasyon akmaya devam eder.

## Kapsam Disi

- Okcu tipi bazli animasyon secmez.

Basic/Rapid/Frost ayni atlas row'larini kullanir; farklar stat ve tint tarafindan okunur.
