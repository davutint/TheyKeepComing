# First Run Onboarding UI - Editor Setup

> Bu setup legacy v1 controller kontratini belgeler. Aktif UI Toolkit P16 kurulumu icin
> `GUIDED_ONBOARDING_EDITOR_SETUP.md` otoritedir; yeni spotlight hedefleri gizli Canvas'a
> baglanmaz.

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
- Low Ammo target: panel kapaliyken alt-sag `ArrowSupplyToggleButton`, panel oyuncu tarafindan
  acikken `AmmoPackageButton`; threshold effective `Current / Capacity <= %25`, panel otomatik acilmaz.
- Heart entry target: alt-sag dock'taki `CastleHeartOpenButton`; authoritative trigger
  `GraveEssenceAmount > 0`, panel otomatik acilmaz.
- Heart pause hint: panel gercek oyuncu aksiyonuyla acikken top-center gorunur; pulse kapali,
  hint raycast kapali ve nested Canvas yalniz bu adimda `overrideSorting = true`,
  `sortingOrder = 260` kullanir (`CastleHeartPanel = 200`).
- Council exact target: Day 1 Dawn'da oyuncu secimine acilan `CouncilEventPanel` kartinin tamami;
  `OnboardingHintPanel` bottom-left `24,226` konumuna tasinir, iki option butonundan hicbiri
  ayricalikli pulse edilmez ve Council karti tutorial tarafindan acilmaz.
- Daytime repair target: Day sirasinda yasayan Wall `%99,5` altina dustugunde
  `CastleDefensePanel/DefenseRepairButton`; `OnboardingHintPanel` top-center `0,-294` konumuna
  tasinir. Stone yetmese de maliyet gorunur, fakat tutorial butonu enable etmez veya repair
  transaction'i cagirmaz.
- First Night ability-key target: `AbilityBarPanel` icindeki ilk gercek hazir slot;
  `[1] Fireball -> [2] Rally -> [3] Emergency Repair` onceligi kullanilir. Ilk kosunun mevcut
  kilit/state'inde `[2] RallyAbilityButton` pulse olur ve `OnboardingHintPanel` bottom-center
  `0,170` konumuna tasinir. Tutorial butona basmaz veya cooldown/ability state'i yazmaz.

Scene `FirstRunOnboardingUI.CastleHeart` referansi ayni HUD root'taki tek `HeartScreenUI`
component'ine; `FirstRunOnboardingUI.Council` referansi ayni root'taki tek `CouncilEventUI`
component'ine; `FirstRunOnboardingUI.NormalRepair` referansi ayni root'taki tek
`DefenseRepairUI` component'ine; `FirstRunOnboardingUI.Abilities` referansi ayni root'taki tek
`SpellCastUI` component'ine bagli olmalidir.

Idempotent onarim: `Window -> DeadWalls -> Repair First Run Onboarding`.

## Settings Tutorial Reset Binding

`NewGameScene/Canvas/MenuUiRoot/SettingsPanel` ve
`MainMenuScene/Canvas/SettingsPanel` icinde ayni isim sozlesmesi bulunur:

- `TutorialResetButton`
- `TutorialResetStatusText`

Her iki sahnedeki `SettingsUI`, button/label/status referanslarini tasir. Panel ilk durumda
`RESET TUTORIAL` ve `RESETS ONBOARDING ONLY. RUN AND UPGRADES STAY.` metinlerini gosterir.
Idempotent iki-sahne onarimi icin `NewGameScene` aktifken
`Window -> DeadWalls -> Repair Tutorial Reset Setting` calistirilir.

## Dogrulama

- EditMode presentation/rule testleri prefab isim, geometri, English copy, raycast, Day 1 ve
  Basic affordability kapilarini dogrular.
- PlayMode testi gercek `NewGameScene` icinde hint/pulse gorunurlugunu, drawer acilinca hedef
  degisimini, player ratio action'inin meta flag yazmasini ve tutorial'in resource/actual worker
  state'ini kendi basina degistirmedigini dogrular.
- Basic Archer PlayMode testi, yetersiz kaynaktan ilk affordability'ye gecisi, kapali/acik drawer
  hedeflerini ve yalniz basarili satin almanin `tutorial.v1.basic_archer` flag'ini yazdigini dogrular.
- Low Ammo PlayMode testi `%25` inclusive esigi, kapali panelde gercek `ARROW SUPPLY` dock hedefi,
  panelin oyuncu tiklamasina kadar kapali kalmasi, acilis sonrasi gercek `AmmoPackageButton`
  hedefi, basarisiz refill'in flag yazmamasi ve yalniz basarili refill'in
  `tutorial.v1.low_ammo` flag'ini durable yazmasini dogrular.
- Heart PlayMode testi sifir bakiyede sessiz kalmayi, ilk pozitif Grave Essence bakiyesinde gercek
  Heart butonunu pulse etmeyi, panelin oyuncu aksiyonuyla acilmasini, full-pause hint'inin
  `Time.timeScale = 0` iken gorunmesini ve flag'in yalniz player close sonrasinda yazilmasini
  dogrular.
- Council PlayMode testi Day 1 Dawn'daki regular kartin interaktif olduktan sonra tam kart pulse hedefi
  olmasini, iki option metninin authoritative live exact quote'larla ayni kalmasini, tutorial'in
  secim yapmamasini ve `tutorial.v1.council` flag'inin yalniz basarili player button commit'inde
  yazilmasini dogrular.
- Daytime repair PlayMode testi hasarli Wall + Day kapisini, gercek REPAIR button hedefini,
  affordability'den bagimsiz cue'yu, basarisiz denemenin HP/Stone/flag degistirmemesini ve yalniz
  basarili player repair'in exact Stone harcayip `tutorial.v1.repair` flag'ini yazmasini dogrular.
- First Night ability-key PlayMode testi ilk Night'ta ilk hazir gercek slotu ve dynamic English
  copy'yi, locked hotkey reddini, mouse button kullaniminin flag yazmamasini, kabul edilmis `[2]`
  hotkey'inin `tutorial.v1.ability_key` flag'ini durable yazmasini ve tutorial'in resource
  state'ine dokunmamasini dogrular.
- Global transaction-free EditMode guard'i controller source'unda Archer/Ammo/Heart/Council/
  repair/ability transaction'i, worker assignment, ECS write, programmatic panel open ve button
  invoke cagrilarini yasaklar; `MetaProgression.SetTutorialFlag` izinli tek persistence yazimidir.
- Global transaction-free PlayMode testi yedi cue'yu action cagirilmadan sirayla gorunur yapar;
  her cue boyunca Wood/Stone/Iron/Food, Arrow, Grave Essence, population, actual worker dagilimi,
  target ratio, bed ve worker-building state'lerinin exact ayni kaldigini dogrular.
- Blocking-pause EditMode kurali, pause yokken normal cue'lara izin verir; aktif pause sirasinda
  yalniz oyuncunun acmis oldugu ilk Heart modalinin `HeartPause` ogretimini allowlist eder.
- Modal-chain PlayMode testi Heart acikken tek `HeartScreenUI` lease'i bulundugunu, sonraki repair
  cue'sunun zincirlenmedigini, player close sonrasinda lease/timeScale'in exact geri dondugunu ve
  repair cue'sunun yeni modal acmadan non-modal devam ettigini dogrular.
- Modal-chain source guard'i `FirstRunOnboardingUI` icinde pause acquire/enforce ile Heart/Pause/
  Settings programmatic open cagrilarini yasaklar.
- Prompt-independent completion source guard'i yedi accepted player-action handler'inin
  `_activeStep`, cue visibility veya hint/pulse state'ine baglanmadigini dogrular.
- Preemptive Heart PlayMode testi Grave Essence sifirken gercek Heart open/close action'inin pause
  dersini tamamladigini ve Essence daha sonra geldiginde giris prompt'inin tekrar acilmadigini
  dogrular.
- Global completion EditMode kurali yedi zorunlu alt flag'in tamamini gerektirir ve stable
  `tutorial.v1.complete` Id'sini kilitler.
- Final-action PlayMode testi son eksik adim tamamlandiginda global flag'in ayni run'da yazildigini;
  legacy-backfill testi yedi alt flag tasiyan eski meta save'in global flag'i ureterek iki durumda
  da `MetaProgression.Load()` sonrasinda durable kaldigini dogrular.
- Second-run suppression PlayMode testi ilk run'i gercek lethal save/death receipt yoluyla bitirir,
  `UIManager.OnRestart()` ile farkli `CurrentRunId` tasiyan Day 1 kosusunu baslatir ve sekiz tutorial
  flag'inin meta reload sonrasinda korundugunu dogrular.
- Ayni test, ikinci run'da normal ilk-worker eligibility'si aktifken 120 frame boyunca shared hint,
  pulse target ve sekiz onboarding cue state'inin tamamini kapali kilitler. Yalniz Settings icindeki
  onayli reset bu kontrati opt-in olarak yeniden acar.
- Tutorial reset EditMode testleri canonical sekiz flag listesini, listenin consumer tarafindan
  mutate edilememesini, iki sahnede tek ve eksiksiz Settings binding'ini ve hedefli resetin diger
  meta state'i koruyarak reload sonrasi durable kalmasini dogrular.
- Tutorial reset PlayMode testi ilk tiklamanin yalniz confirmation kurdugunu, ikinci tiklamanin
  sekiz flag'i temizledigini, bilinmeyen future tutorial flag'ini korudugunu ve Pause Resume
  sonrasinda ilk uygun onboarding cue'sunun yeniden basladigini dogrular.
- English-copy EditMode guard'i yedi step hint'i, uc ability-key varyanti ve tutorial-reset'in alti
  UI state metnini exact approved English degerlerle kilitler; prefab ile iki sahnenin serialized
  baslangic metinleri ayni constant'larla birebir eslesir.
