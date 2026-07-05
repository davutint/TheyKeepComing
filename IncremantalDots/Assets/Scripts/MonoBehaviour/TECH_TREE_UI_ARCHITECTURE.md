# Tech Tree UI - Architecture

## Amac

`MobileCastleHudRoot` icindeki fullscreen dinamik Tech Tree panelinin controller'i
(`TechTreeUI.cs`). Agac SABIT DEGILDIR: gorunur node'lar reveal state'inden runtime'da
uretilir, oyuncu satin aldikca graf disari dogru buyur. Kategori raylari
(Economy/Military/Defense) ve elle yerlestirilmis final agac BILEREK yoktur.

## Runtime Akisi

1. `OnEnable`: open/close buton listener'lari baglanir; panel kapali baslar (state sahibi bu controller).
2. `TechTreeOpenButton` -> `OpenPanel()` -> `RebuildGraph()`.
3. `RebuildGraph()`:
   - `GameManager.GetRevealedTechNodes()` gorunur listeyi verir.
   - Gorunur parent->child iliskileri `RevealChildNodeIds`'ten cikarilir (iki uc da gorunurse).
   - Layout: root sol-ortada; `x = derinlik * NodeSpacingX`, `y` gorunur yaprak sayisina gore
     dagitilir (recursive, deterministik, isim/kategori bagimsiz). Dongusel veri guard'lidir.
   - `TechTreeContent.sizeDelta` bounding box'a gore ayarlanir; `ScrollRect` ile pan.
   - Once `TechConnectionTemplate` klonlari (cizgiler altta), sonra `TechNodeTemplate` klonlari.
4. Panel acikken 0.2s unscaled poll: gorunur sayi degistiyse rebuild, degilse node durum refresh'i.
5. `TechNodeBuyButton` -> `GameManager.TryBuyTechNode()` -> basarida aninda rebuild
   (yeni reveal edilen cocuklar gorunur).

## Pause Karari (dokumante)

Panel acikken OYUN DURMAZ — sol/sag drawer emsali. Gerekceler:
- `MobilePrepPauseState` continuous siege'de olu koddur (`CanOpenCastleEconomy` DayPrep ister,
  continuous akista hic olusmaz).
- `Time.timeScale = 0` tum ECS sim'ini durdurur; CLAUDE.md continuous siege "oyun durmaz"
  ilkesiyle catisir. En guvenli davranis pause'suz ac/kapa secildi.

## Node Durum Gorseli (duz renk/text; sprite yok)

| Durum | Etiket | Gorsel |
|---|---|---|
| Satin alinabilir | `AVAILABLE` (yesil) | normal koyu satir, BUY aktif |
| Sahipli (MaxLevel 1) | `BOUGHT` | yesilimsi zemin, BUY gizli |
| Sahipli+max (MaxLevel>1) | `MAX` (sari) | yesilimsi zemin, BUY gizli; `LV x/y` |
| Prereq eksik | `LOCKED` (gri) | soluk zemin, BUY pasif |
| Kaynak yetmiyor | `NEED ...` (turuncu) | BUY pasif |

`Icon` null ise `TechNodeIconImage` kapanir, `TechNodeIconFallbackText` baslik
bas-harflerini (max 2) gosterir. Art/generated icon URETILMEZ.

## Isim Sozlesmesi (setup tool + runtime bu isimleri arar)

Panel: `TechTreePanel`, `TechTreeOpenButton`, `TechTreeCloseButton`, `TechTreeTitleText`,
`TechTreeViewport` (ScrollRect+RectMask2D), `TechTreeContent`.
Template (inactive, `TechTreeContent` altinda): `TechNodeTemplate` ve `TechConnectionTemplate`.
`TechNodeTemplate` cocuklari: `TechNodeTitleText`, `TechNodeLevelText`, `TechNodeCostText`,
`TechNodeStatusText`, `TechNodeDescriptionText`, `TechNodeIconImage`
(+ cocugu `TechNodeIconFallbackText`), `TechNodeBuyButton` (+ cocugu `TechNodeBuyButtonText`).

DIKKAT: `ArrowTech*` ve `*TechUnlockButton` isim aileleri legacy gizleme listelerine takilir;
yeni tech UI objelerine bu isimler verilmez.

## Scope

- Veri: `ScriptableObject/TECH_TREE_SO_ARCHITECTURE.md`
- State/effect: `GameManager.cs` Tech Tree bolumu (satin alma kurali + aggregate uygulama)
- Binding/seed: `Editor/MobileCastleSceneSetupWindow.ConfigureTechTree` + `EnsureDefaultTechTreeCatalog`
- MarketUI iliskisi: sag drawer recruitment-only kalir; Rapid/Frost unlock'u tech
  node'larindan gelir (`IsArcherTypeUnlocked` tek dogruluk kaynagi)
