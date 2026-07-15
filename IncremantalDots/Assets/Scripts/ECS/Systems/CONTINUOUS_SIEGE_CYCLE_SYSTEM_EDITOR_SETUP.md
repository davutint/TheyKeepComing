# ContinuousSiegeCycleSystem - Editor Setup

`Window > DeadWalls > Mobile Castle Scene Setup` calistirildiginda `MobileCastleConfig` uzerindeki `MobileCastleCombatAuthoring` su defaultlari yazar:

- Continuous Siege Enabled: `true`
- Siege Cycle Duration: `60`
- Siege Day Duration: `25`
- Siege Dusk Duration: `10`
- Siege Night Duration: `25`
- Siege Day Intensity Multiplier: `0.55`
- Siege Dusk Start Intensity Multiplier: `1.00`
- Siege Dusk End Intensity Multiplier: `1.35`
- Siege Night Intensity Multiplier: `1.65`

HUD prefabinda su isimler varsa `HUDController` alanlarina baglanir:

- `CyclePanel`
- `CycleDayCounterText`
- `CycleProgressTrack` (`CycleCelestialArc` binding'i)
- `CycleCelestialGlow`
- `CyclePhaseText`
- `CycleDayLabelText`
- `CycleDuskLabelText`
- `CycleNightLabelText`
- `CycleProgressFill`
- `CycleProgressMarker`

Owner-secili Celestial Dial'da setup tool `CycleProgressMarker`i `CycleProgress01` yay hareketine, `CycleCelestialGlow`u phase-color crossfade'e baglar. `CyclePhaseText`, uc ham phase label'i ve linear fill player-facing kapali kalir. `CyclePanel` varsa eski fallback `WaveText` ve `KillsText` uretilmez; sahnede kalmislarsa kapatilir. `HordePressurePanel` prefabda varsa player-facing olarak kapali tutulur. Bu dosya UI uretmez; prefabdaki isimler baglanir.
