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
- Basic Archer target: `DrawerToggleButton` veya Archer drawer acikken runtime-generated
  `ArcherRecruitmentRow_basic_archer/ArcherBuyButton`.
- Low Ammo target: her durumda ust resource strip'teki `ArrowChip`; threshold effective
  `Current / Capacity <= %25`, panel otomatik acilmaz.
- Heart entry target: alt-sag dock'taki `CastleHeartOpenButton`; authoritative trigger
  `GraveEssenceAmount > 0`, panel otomatik acilmaz.
- Heart pause hint: panel gercek oyuncu aksiyonuyla acikken top-center gorunur; pulse kapali,
  hint raycast kapali ve nested Canvas yalniz bu adimda `overrideSorting = true`,
  `sortingOrder = 260` kullanir (`CastleHeartPanel = 200`).
- Council exact target: Day 3'te oyuncu secimine acilan `CouncilEventPanel` kartinin tamami;
  `OnboardingHintPanel` bottom-left `24,226` konumuna tasinir, iki option butonundan hicbiri
  ayricalikli pulse edilmez ve Council karti tutorial tarafindan acilmaz.

Scene `FirstRunOnboardingUI.CastleHeart` referansi ayni HUD root'taki tek `HeartScreenUI`
component'ine; `FirstRunOnboardingUI.Council` referansi ayni root'taki tek `CouncilEventUI`
component'ine bagli olmalidir.

Idempotent onarim: `Window -> DeadWalls -> Repair First Run Onboarding`.

## Dogrulama

- EditMode presentation/rule testleri prefab isim, geometri, English copy, raycast, Day 1 ve
  Basic affordability kapilarini dogrular.
- PlayMode testi gercek `NewGameScene` icinde hint/pulse gorunurlugunu, drawer acilinca hedef
  degisimini, player ratio action'inin meta flag yazmasini ve tutorial'in resource/actual worker
  state'ini kendi basina degistirmedigini dogrular.
- Basic Archer PlayMode testi, yetersiz kaynaktan ilk affordability'ye gecisi, kapali/acik drawer
  hedeflerini ve yalniz basarili satin almanin `tutorial.v1.basic_archer` flag'ini yazdigini dogrular.
- Low Ammo PlayMode testi `%25` inclusive esigi, gercek Arrow chip hedefi, panelin kapali kalmasi,
  basarisiz refill'in flag yazmamasi ve yalniz basarili refill'in `tutorial.v1.low_ammo` flag'ini
  durable yazmasini dogrular.
- Heart PlayMode testi sifir bakiyede sessiz kalmayi, ilk pozitif Grave Essence bakiyesinde gercek
  Heart butonunu pulse etmeyi, panelin oyuncu aksiyonuyla acilmasini, full-pause hint'inin
  `Time.timeScale = 0` iken gorunmesini ve flag'in yalniz player close sonrasinda yazilmasini
  dogrular.
- Council PlayMode testi regular Day 3 kartinin interaktif olduktan sonra tam kart pulse hedefi
  olmasini, iki option metninin authoritative live exact quote'larla ayni kalmasini, tutorial'in
  secim yapmamasini ve `tutorial.v1.council` flag'inin yalniz basarili player button commit'inde
  yazilmasini dogrular.
