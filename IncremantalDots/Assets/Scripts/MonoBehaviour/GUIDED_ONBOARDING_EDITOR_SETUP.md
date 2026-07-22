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
- `guidedStepLabel`, `guidedTitle`, `guidedBody`

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
gizlenir. Player-facing yazi boyutlari mevcut HUD okunabilirlik tabaninin altina dusurulmez.

## Manuel QA

1. Settings'te `RESET TUTORIAL` iki asamali onayla tamamlanir.
2. New Game acilir; yalniz Economy kontrolu etkilesim alir.
3. Economy acilir; spotlight Wood slider'a tasinir.
4. Slider degeri gercekten degistirilir; spotlight Economy `CLOSE` butonuna tasinir.
5. Drawer gercek Close aksiyonuyla kapanir; ancak bundan sonra Barracks hedef olur.
6. Basic Archer basariyla alinir; `2X` hedef olur.
7. `2X` secilince core karartma/input kilidi kapanir.
8. Uygun gameplay kosullarinda contextual tip'in unrelated kontrollere engel olmadigi incelenir.
9. Council kartinda iki option'in de secilebilir, tutorial'in yalniz ortak container'i isaretliyor
   oldugu dogrulanir.

Runtime teshis icin `GameplayHUDToolkitUI.ActiveGuidedOnboardingStep`,
`ActiveGuidedOnboardingTarget` ve `IsGuidedOnboardingInputLocked` read-only property'leri
kullanilabilir.

## Testler

- EditMode: `DeadWalls.Tests.GuidedOnboardingTests`
- EditMode: `DeadWalls.Tests.GameplayHUDToolkitContractTests`
- EditMode: `DeadWalls.Tests.FirstRunOnboardingTests`
- EditMode: `DeadWalls.Tests.MetaProgressionSchemaTests`
- PlayMode: `GuidedOnboarding_CoreSequenceLocksOutsideInputAndAdvancesOnRealActions`
- PlayMode: P15 Toolkit/legacy Archer toast regresyonlari ve tutorial reset/second-run testleri
