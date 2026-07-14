# Castle Heart Screen ve Simulation Pause - Editor Setup

## Dogruluk kaynagi

Castle Heart UI yalniz
`Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab` uzerinde duzenlenir. Eski
UI export/import pipeline'ini veya scene-only gorsel child uretimini kullanmayin.

## Prefab kontrol listesi

Prefab stage'de su objeleri dogrula:

- `CastleHeartOpenButton` label: `HEART`.
- `CastleHeartPanel` default inactive, fullscreen; `CanvasGroup`, `overrideSorting = true`,
  `sortingOrder = 200` nested `Canvas` ve `GraphicRaycaster` sahibi.
- `CastleHeartTitleText` label: `CASTLE HEART`.
- `CastleHeartCloseButton`.
- `HeartViewport`: `ScrollRect`, `RectMask2D`, `TechTreeViewController`.
- `HeartContent`: node/connection runtime parent'i.
- Inactive `HeartNodeTemplate` ve `HeartConnectionTemplate`.
- `GraveEssenceText`, `HeartScreenStatusText`, `HeartBranchCompassText`.
- `HeartQuantityOneButton`, `HeartQuantityTenButton`, `HeartQuantityMaxButton`.
- `CastleHeartBadge` default inactive.
- `CastleHeartToastText` panel altinda, modal Canvas'in en ust feedback alani.

`HeartNodeTemplate` altinda `HeartNodeIconImage`, `HeartNodeIconFallbackText`,
`HeartNodeTitleText`, `HeartNodeLevelText`, `HeartNodeDescriptionText`,
`HeartNodeCostText`, `HeartNodeStatusText`, `HeartNodeBuyButton`,
`HeartNodeBuyButtonText` ve `HeartNodePipsRoot` bulunur.

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

1. Play'e gir ve `HEART` butonuna bas.
2. Panelin fullscreen acildigini ve world/cycle'in durdugunu dogrula.
3. Pause menu ile Heart'i ust uste ac/kapat; ilk kapanan owner simulation'i baslatmamali.
4. Heart'i kapat; onceki time scale ve DOTS group enabled state exact donmeli.
5. Onayli catalog bagliysa hidden node'larda exact baslik/effect sizintisi olmadigini kontrol et.
6. Gorunur numeric node'da current, after, delta ve exact GE maliyetini kontrol et.
7. Repeatable node'da `+1/+10/MAX`; Unlock/Evolution/Keystone'da yalniz tek alim kontrolunu
   dogrula.
8. Rapid/Frost direct unlock ve archer stat upgrade yuzeylerinin drawer'da gorunmedigini
   dogrula.

## Otomatik dogrulama

- Hedefli EditMode: `DeadWalls.Tests.HeartScreenPauseTests`.
- Full EditMode regression.
- Full PlayMode regression.
- Console: compile/runtime error `0`.

Exact graph save/restore, catalog version migration ve Continue replay `DW-E-SAVE` paketinde
test edilir.
