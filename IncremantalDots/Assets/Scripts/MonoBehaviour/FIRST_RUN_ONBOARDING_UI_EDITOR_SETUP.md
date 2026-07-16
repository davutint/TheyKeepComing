# First Run Onboarding UI - Editor Setup

## Aktif Sahne

`NewGameScene/Canvas/MobileCastleHudRoot` scene instance'inda tek
`FirstRunOnboardingUI` bulunmalidir. Runtime controller generated prefab assetine gomulmez;
scene setup tool tarafindan scene owner'ina eklenir ve isimle baglanir.

## Generated Prefab Isim Sozlesmesi

- `OnboardingHintPanel`: bottom-left, `360 x 42`, varsayilan kapali, raycast kapali.
- `OnboardingHintText`: tek satir, English, auto-size.
- `OnboardingHintAccent`: sol amber vurgu cizgisi.
- `OnboardingPulseFrame`: varsayilan kapali, raycast kapali, rounded image + outline.
- Worker target: `WorkerDrawerToggleButton` veya drawer acikken
  `WoodWorkerTargetPlus10Button`.

Idempotent onarim: `Window -> DeadWalls -> Repair First Day Worker Ratio Onboarding`.

## Dogrulama

- EditMode presentation/rule testleri prefab isim, geometri, English copy, raycast ve Day 1
  kapisini dogrular.
- PlayMode testi gercek `NewGameScene` icinde hint/pulse gorunurlugunu, drawer acilinca hedef
  degisimini, player ratio action'inin meta flag yazmasini ve tutorial'in resource/actual worker
  state'ini kendi basina degistirmedigini dogrular.
