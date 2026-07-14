# Tech Tree UI - Legacy Editor Setup

> Bu kurulum tarihsel sabit catalog UI'ini anlatir. Aktif prefab/scene binding'i
> `HEART_SCREEN_EDITOR_SETUP.md` dosyasindadir; `TechTreeUI`yi aktif HUD'a yeniden ekleme.

## Otomatik Kurulum

`Window > DeadWalls > Mobile Castle Scene Setup > Setup NewGameScene`:

1. `TechTreeUI` component'ini sahnedeki HUD root instance'ina ekler
   (HUDController/MarketUI/WorkerEconomyDrawerUI ile ayni GameObject).
2. Prefabdaki Tech Tree objelerini isimle bulup field'lara baglar; prefabda eksikse
   `EnsureFallbackTechTreePanel` minimal iskeleti kurar (normal akista devreye girmez —
   objeler `MobileCastleHudRoot.prefab` icindedir).
3. Template'leri inactive, paneli kapali garanti eder.
4. HUD root'taki missing-script kalintilarini temizler
   (`GameObjectUtility.RemoveMonoBehavioursWithMissingScript` — eski silinmis
   `CastleTechTreeUI` kalintisi bu adimla gitti).

Katalog `GameManager.techTreeCatalog`'a baglanir; `TechTreeUI` catalog'u GameManager'dan okur
(dogrudan SO referansi tutmaz — tek dogruluk kaynagi GameManager API'si).

## Prefab Yerlesimi (MobileCastleHudRoot.prefab)

- `TechTreeOpenButton`: HUD ana katmaninda, `WorkerDrawerToggleButton` yaninda
  ((-725, 410), 126x38, "TECH" etiketli, builtin UISprite).
- `TechTreePanel` [inactive]: fullscreen stretch, duz koyu arka plan (~%98 opak).
  Icinde: `TechTreeTitleText` (sol ust), `TechTreeCloseButton` (sag ust),
  `TechTreeViewport` (stretch, ScrollRect + RectMask2D; drag icin raycastable koyu Image),
  `TechTreeContent` (2400x1400, sol-ust pivot).
- `TechTreeContent` altinda inactive template'ler: `TechConnectionTemplate` (120x3 duz Image)
  ve `TechNodeTemplate` (230x112; tum zorunlu cocuk isimleriyle — bkz. ARCHITECTURE isim sozlesmesi).

## Juice + Gezinme Kurulumu (setup tool otomatik yapar)

- `TechTreeViewController` viewport'a eklenir (`InputMode=Auto`); ScrollRect Elastic + inertia +
  `scrollSensitivity=0` yapilir (tekerlek zoom'a devredildi).
- `TechTreePanel`'e CanvasGroup (fade acilisi icin).
- `TechTreeBadge` (TECH butonu cocugu, sari Knob nokta, default OFF) ve `TechTreeToastText`
  (HUD katmani, alpha 0) bulunur/olusturulur ve baglanir.
- SFX clip'leri baglanir: Buy=`Coins 2-1`, Reveal=`Magical Texture Chimes 1-1`,
  Denied=`Key & Lock 1-1`, PanelOpen=`Book Page 1-2` (Fantasy UI SFX - Lite Edition).
- DOTween gereksinimi: `Assets/Plugins/Demigiant` kurulu + `DOTween.Modules.asmdef` olusturulmus
  + `DeadWalls.asmdef` referanslarinda `DOTween.Modules` olmali (DOTween Utility Panel > Create ASMDEF).

## Test Adimlari

1. Play'e gir; `TECH` butonuna bas — panel acilir, `Castle Heart` (BOUGHT) + 4 cocuk
   (`AVAILABLE`) ve baglanti cizgileri gorunur. Oyun arka planda akmaya devam eder.
2. `Wood Camp` satin al — `Worker Camp` + `Food Stores` belirir; wood uretim/dakika artar
   (resource bar rate + worker drawer).
3. `Rapid Volley` -> `Rapid Archer` satin al — sag drawer'daki Rapid satiri alinabilir olur.
4. `Bow Training` 3 kez alinabilir (`LV x/3`), sonra `MAX`.
5. Restart sonrasi agac 5 gorunur node'a doner, Rapid/Frost yeniden kilitlenir,
   config degerleri (cap/uretim/growth) base'e doner.
6. Hizli test icin `GameManager > Free Economy Test Mode` isaretlenebilir (maliyet bypass).

## Dikkat

- Binding isimlerini degistirme; setup tool exact-match arar.
- `TechTreePanel` prefabin ICINDE kalmali — sahne-override olarak eklenirse HUD prefab
  yeniden kurulumunda kaybolur.
- Prefab TEK dogruluk kaynagidir; UI dogrudan prefab stage'de duzenlenir
  (eski UIImporter/export pipeline'i 2026-07-06'da kaldirildi — sync borcu kavrami kapandi).
