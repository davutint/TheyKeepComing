# Castle Heart Screen ve Simulation Pause - Editor Setup

## Dogruluk kaynagi

Castle Heart UI yalniz
`Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab` uzerinde duzenlenir. Eski
UI export/import pipeline'ini veya scene-only gorsel child uretimini kullanmayin.

## Prefab kontrol listesi

Prefab stage'de su objeleri dogrula:

- `CastleHeartOpenButton`: alt-sag anchor/pivot, `156 x 56`, `(-24,28)`, label `CASTLE HEART`.
- `DrawerToggleButton`: ayni dock'ta `156 x 56`, `(-190,28)`, label `ARCHERS`; `ArcherDrawerPanel` child'i degil, ortak HUD parent'inin child'idir.
- Archer row `ArcherLevelText`/legacy type level alanlari prefab default'unda `HEART` yazar;
  runtime kilitli Rapid/Frost satirlarini `TECH` olarak gunceller.
- `CastleHeartPanel` default inactive, fullscreen; `CanvasGroup`, `overrideSorting = true`,
  `sortingOrder = 200` nested `Canvas` ve `GraphicRaycaster` sahibi.
- `CastleHeartTitleText` label: `CASTLE HEART`.
- `CastleHeartCloseButton`.
- `HeartViewport`: `ScrollRect`, `RectMask2D`, `TechTreeViewController`.
- `HeartContent`: node/connection runtime parent'i.
- Inactive `HeartNodeTemplate` ve `HeartConnectionTemplate`.
- Runtime node layout `264 x 156`, branch spacing `340 / 236`, icon socket `52 x 52` olmalidir.
- `HeartScreenUI.ShowAuthoredNodeIcons` owner icon seti hazirlanana kadar kapali kalmalidir.
- `GraveEssenceText`, `HeartScreenStatusText`, `HeartBranchCompassText`.
- `HeartQuantityOneButton`, `HeartQuantityTenButton`, `HeartQuantityMaxButton`.
- `CastleHeartBadge` default inactive.
- `CastleHeartToastText` panel altinda, modal Canvas'in en ust feedback alani.

`HeartNodeTemplate` altinda `HeartNodeIconImage`, `HeartNodeIconFallbackText`,
`HeartNodeTitleText`, `HeartNodeLevelText`, `HeartNodeDescriptionText`,
`HeartNodeCostText`, `HeartNodeStatusText`, `HeartNodeBuyButton`,
`HeartNodeBuyButtonText` ve `HeartNodePipsRoot` bulunur.

Runtime `HeartNodeIconSocket` dekorasyonunu template clone'una ekler. Icon kapaliyken
`HeartNodeIconImage` ve `HeartNodeIconFallbackText` inactive/empty kalir; gecici harf veya soru
isareti kullanilmaz. Owner iconlari hazirlandiginda definition asset'lerine sprite atanir ve ancak
son ortak QA turunda `ShowAuthoredNodeIcons` acilir.

## Scene binding

`NewGameScene` icindeki `MobileCastleHudRoot` scene instance'inda:

1. `HeartScreenUI` component'i bulunmali.
2. Yukaridaki prefab alanlarinin tumu component'e bagli olmali.
3. `TechTreeUI` component'i bulunmamali.
4. `MarketUI` upgrade butonlari, `ArrowTechPanel` ve direct Rapid/Frost unlock butonlari
   player-facing kapali kalmali.

`MobileCastleSceneSetupWindow.ConfigureTechTree` bu binding'i yapar. Tam scene setup'i tekrar
calistirmadan yalniz cutover onarimi gerekiyorsa Editor kodu ayni private configurator'i aktif
HUD instance'ina uygular; normal owner akisi `Setup NewGameScene`dir.

## Production catalog gate

`GameManager.heartCatalog` yalniz owner tarafindan onaylanmis `HeartNodeCatalogSO` asset'ine
baglanir. Null birakildiginda:

- panel acilir ve simulation durur;
- graph satin alma yuzeyi fail-closed kalir;
- ekranda catalog/runtime hazir degil hatasi gorunur;
- legacy catalog veya Wood/Stone/Iron/Food maliyetine geri donulmez.

Legacy node'lari otomatik migrate etme. Base GE maliyeti, growth, rarity/depth, Keystone
partneri, numeric value/soft-cap ve Evolution pool'u ayri owner onayi gerektirir.

## Play Mode QA

1. Play'e gir ve alt-sag `CASTLE HEART` butonuna bas.
2. Panelin fullscreen acildigini ve world/cycle'in durdugunu dogrula.
3. Pause menu ile Heart'i ust uste ac/kapat; ilk kapanan owner simulation'i baslatmamali.
4. Heart'i kapat; onceki time scale ve DOTS group enabled state exact donmeli.
5. Onayli catalog bagliysa hidden node'larda exact baslik/effect sizintisi olmadigini kontrol et.
6. Gorunur numeric node'da current, after, delta ve exact GE maliyetini kontrol et.
7. Icon seti owner onayi almadiysa her node'da bos socket ayrildigini, sprite/fallback glyph
   gorunmedigini ve sert `HeartAxis_*` arti ekseni uretilmedigini kontrol et.
8. Repeatable node'da `+1/+10/MAX`; Unlock/Evolution/Keystone'da yalniz tek alim kontrolunu
   dogrula.
9. Rapid/Frost direct unlock ve archer stat upgrade yuzeylerinin drawer'da gorunmedigini
   ve unlocked satirin level alaninda `HEART` yazdigini dogrula.
10. Bir Keystone çiftine ilerle; iki kartın birlikte ve üst üste binmeden görünmesini,
   `CHOOSE ONE · RUN COMMITMENT` etiketini ve altın fork/merge damarlarını kontrol et.
11. Her iki seçimi ayrı koşularda dene; yalnız partner kilitlenmeli ve branch'in sonraki node'u
    hangi taraf seçilirse seçilsin reveal olmalıdır.

12. Heart acikken sahneyi yeniden yukle veya Play Mode'dan cik; Console'da yok edilmis
    `RectTransform`/`CanvasGroup` hedefli DOTween safe-mode uyarisi kalmamalidir.

## Otomatik dogrulama

- Hedefli EditMode: `DeadWalls.Tests.HeartScreenPauseTests`.
- Full EditMode regression.
- Full PlayMode regression.
- Console: compile/runtime error `0`.

Exact graph save/restore, catalog version gate ve Continue replay
`HeartGraphContinuePlayModeTests` ile test edilir.
