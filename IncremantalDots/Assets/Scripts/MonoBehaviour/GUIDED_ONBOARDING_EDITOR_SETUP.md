# Guided Onboarding - Editor Setup

## Scene ve Component

Yeni bir scene component'i veya serialized reference gerekmez. Aktif owner mevcut
`NewGameScene/UIToolkitGameplayHUD` nesnesindeki `GameplayHUDToolkitUI` ve `UIDocument`tir.
Controller partial dosyasi runtime'da ayni owner'a derlenir.

## UXML Isim Sozlesmesi

`Assets/UI/Toolkit/GameplayHUD/GameplayHUD.uxml` icinde su elementler tekil kalmalidir:

- `guidedTutorialLayer`
- `guidedDimTop`, `guidedDimBottom`, `guidedDimLeft`, `guidedDimRight`
- `guidedFocus`
- `guidedCard`
- `guidedStepLabel`, `guidedTitle`, `guidedBody`, `guidedAction`

Bu elementlerin hepsi `picking-mode="Ignore"` kullanir. Hedef disi input kilidi overlay raycast'i
ile degil, document root event gate'i ile uygulanir.

## Hedef Isim Sozlesmesi

Presenter production kontrollerini isimle bulur:

- `economyButton`
- `economyAllocationSliderWood`
- `economyClose`
- `barracksButton`
- `archerBuyBasic`
- `timeSpeedTwo`
- `abilityRally`, `abilityRepair`
- `councilOptionA` parent container'i
- `arrowsButton`, `arrowPackageButton`
- `heartButton`
- `housingOne`

Bu isimler degistirilirse UXML, binding, EditMode contract ve PlayMode core-chain testi birlikte
guncellenmelidir. Gizli legacy Canvas kontrolu yeni target olarak baglanmamalidir.

## Stil Sozlesmesi

`GameplayHUD.uss` icindeki `.guided-tutorial-*` siniflari overlay, dim, focus ve bilgi kartini
yonetir. Core focus amber, contextual focus cyan kullanir. Contextual durumda dim rect'ler
gizlenir. Focus padding/opacity/border pulse'i C# tarafinda `Time.unscaledTime` ile surulur; USS
base renk ve okunabilirlik kontratini tasir. Player-facing yazi boyutlari mevcut HUD okunabilirlik
tabaninin altina dusurulmez.

## Manuel QA

1. Editor'da Play'e basilir; manual `RESET TUTORIAL` kullanmadan tutorial Economy adimindan baslar.
2. New Game acilir; simulation `0` timeScale'de durur, kart `TUTORIAL PAUSED` yazar ve yalniz
   Economy kontrolu etkilesim alir.
3. Economy acilir; spotlight Wood slider'a tasinir.
4. Slider degeri gercekten degistirilir; spotlight Economy `CLOSE` butonuna tasinir.
5. Drawer gercek Close aksiyonuyla kapanir; ancak bundan sonra Barracks hedef olur.
6. Basic Archer basariyla alinir; `2X` hedef olur.
7. `2X` secilince core karartma/input kilidi kapanir ve simulation ayni action sonrasinda `2X`
   olarak devam eder.
8. Focus rect'in pause altinda dahi yavasca genisleyip daraldigi ve opacity/border'inin nefes
   aldigi gorulur.
9. Uygun gameplay kosulunda contextual tip acilir; simulation durur fakat unrelated UI input'u
   kilitlenmez. Action tamamlaninca onceki running speed geri gelir.
10. Housing ve Arrow field tiplerinin ilgili satin alim affordable degilken pause soft-lock'i
    yaratmadigi kontrol edilir.
11. Council kartinda iki option'in de secilebilir, tutorial'in yalniz ortak container'i isaretliyor
    oldugu dogrulanir.
12. Stop ardindan yeniden Play yapilir; core ve butun contextual completion flag'lerinin temiz
    session'da oldugu, Economy adiminin tekrar acildigi ve `meta_progress.json` dosyasinin tutorial
    nedeniyle degismedigi dogrulanir.

Runtime teshis icin `GameplayHUDToolkitUI.ActiveGuidedOnboardingStep`,
`ActiveGuidedOnboardingTarget` ve `IsGuidedOnboardingInputLocked` read-only property'leri
kullanilabilir.

## Testler

- EditMode: `DeadWalls.Tests.GuidedOnboardingTests`
- EditMode: `DeadWalls.Tests.GameplayHUDToolkitContractTests`
- EditMode: `DeadWalls.Tests.FirstRunOnboardingTests`
- EditMode: `DeadWalls.Tests.MetaProgressionSchemaTests`
- PlayMode: `GuidedOnboarding_CoreSequenceLocksOutsideInputAndAdvancesOnRealActions`
- PlayMode: P15 Toolkit/legacy Archer toast regresyonlari, ayni-session restart ve yeni Play
  session reset/meta-save izolasyon testleri
