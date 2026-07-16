# CombatFeedbackBridge - Editor Setup

## Kurulum

`Window -> DeadWalls -> Mobile Castle Scene Setup` calistirildiginda ana scene'de `CombatFeedbackRoot` olusur ve `CombatFeedbackBridge` eklenir. Tool duplicate root uretmez.

## Otomatik Baglanan Referanslar

- Arrow Muzzle Prefab: `Assets/VFX_Klaus/Prefabs/Stylized Shoot & Hit Vol.2/FX_Shoot_Arrow_muzzle.prefab`
- Hit Flipbook Sprites: `Assets/Art/Effects/fanfx2_cure_small_red/spritesheet.png`
- Arrow/Frost Particle Prefabs: legacy/optional; normal hit akisi bunlari kullanmaz
- Arrow Shoot Clips: `Assets/Fantasy UI SFX - Lite Edition/Arrow & Bow*.wav`
- Arrow Shoot Clip fallback: `Assets/Fantasy UI SFX - Lite Edition/Arrow & Bow 1-2.wav`
- Castle Hit Clip: `Assets/Fantasy UI SFX - Lite Edition/Rock Impact 37.wav`

`FX_Shoot_Arrow.prefab` demo root'u baglanmaz.

## Default Tuning

- Hit flipbook pool: `128`
- Hit flipbook frame rate / scale: `90 / 0.35`
- Hit flipbook sorting: `Wall`, order `12`
- Hit VFX playback budget / min interval: `24 / 0.04s`
- Particle VFX pool per type: `24`
- Max VFX per frame: `24`
- Audio pool size: `16`
- Max SFX per frame: `4`
- Disable in stress mode: enabled
- Muzzle / Castle hit particle scale: `0.18 / 0.35`
- Shoot / Night Shoot / Hit / Castle SFX min interval: `0.075 / 0.12 / 0.08 / 0.18`
- Archer salvo volume cap / pitch depth: `0.62 / 0.08`
- Pitch random range: `0.94 - 1.06`
- VFX sorting: `Wall`, order `12`

## Play Test

- Okcular ayni frame'de ates edince random bow/arrow kliplerinden tek aggregated salvo cue
  calinmali; shoot muzzle VFX V1'de kapali kalmali.
- Basic/Rapid/Frost isabette kirmizi-sari sprite flipbook impact hedef uzerinde gorunmeli.
- `DenseArrowHits_EmitSpatiallySampledVfxAndAggregatedSfx` testi `1000` gerçek ECS
  isabetini `40` spatial candidate, `24` VFX ve `2` toplu SFX event'ine indirmeli.
- `HitFeedbackBridge_EnforcesPlaybackBudgetAndRateLimit` testi `80` hit VFX talebinde
  `24 played / 56 dropped` üretmeli; aynı scaled-time penceresindeki ikinci burst
  bütünüyle rate-limit edilmelidir.
- Kale hasar alinca castle hit sesi ve VFX calismali.
- Stress mode'da VFX/SFX oynatilmamali.
