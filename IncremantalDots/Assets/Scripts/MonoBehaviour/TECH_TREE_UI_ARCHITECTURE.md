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

## Gezinme (TechTreeViewController.cs)

Viewport'taki ScrollRect'in USTUNE eklenen pan/zoom controller'i. `TechTreeInputMode` enum'u
(`Auto/Desktop/Mobile`; Auto platforma gore secer) iki tam davranis seti sunar:
- Desktop: mouse tekerlegi = IMLEC MERKEZLI zoom, orta tus surukleme = pan (sol drag ScrollRect'te).
- Mobile: tek parmak pan (ScrollRect), iki parmak pinch = orta-nokta merkezli zoom.
Zoom `content.localScale` uzerindendir (layout pozisyonlari sabit); alt sinir icerik viewport'a
sigiyorsa 1'e clamp'lenir (bos zoom-out yok). ScrollRect: Elastic + inertia, `scrollSensitivity=0`
(tekerlek zoom'a devredildi). TMP SDF oldugundan zoom'da bulanma olmaz.

## Juice Katmani (DOTween — Assets/Plugins/Demigiant, DOTween.Modules asmdef referansi)

Graf INCREMENTAL senkronlanir (`SyncGraph`): mevcut view'lar korunur, yeri degisenler
`DOAnchorPos` ile kayar, yeni reveal edilen cizgi parent'tan cocuga CIZILEREK uzar
(`DOSizeDelta` 0->L) ve node scale-pop ile belirir (`DOScale OutBack`, cizgiden hafif gecikmeli).
Gorunur set KUCULURSE (restart) full clear + yeniden kurulum.
Diger juice ogeleri (tumu unscaled, 0.12-0.35s):
- Satin alma: punch-scale + status/bg renk lerp'i + kaynak chip'lerinde kirmizi flash + toast.
- Reddetme (kilitli/yetersiz): shake + kisa kirmizi bg + kilit SFX'i.
- Unlock: toast "X UNLOCKED" + sag drawer satirinda yesil flash (MarketUI'ya dokunmadan, isimle).
- TECH butonu badge'i: alinabilir node varken (panel kapaliyken) sari nokta + pulse (0.5s poll).
- Yol renklendirme: satin alinmis cocuga giden cizgi parlak yesil, alinabilire giden normal,
  kilitliye giden soluk.
- Panel acilisi: CanvasGroup fade + 0.96->1 scale; acilista son satin alinan (yoksa ilk
  AVAILABLE) node viewport merkezine odaklanir.
- LV gosterimi: MaxLevel 2-4 arasi node'larda pip kutucuklari (dolu=sari), digerlerinde text.
- SFX (Fantasy UI SFX Lite, setup tool baglar): Buy=Coins 2-1, Reveal=Magical Texture Chimes 1-1,
  Denied=Key & Lock 1-1, PanelOpen=Book Page 1-2.
Flash geri-donus renkleri ILK goruste cache'lenir (`_flashOriginalColors`) — flash ortasinda
ikinci flash gelirse yari-flash rengin "orijinal" sanilip kalici kalmasini onler.

## Isim Sozlesmesi (setup tool + runtime bu isimleri arar)

Panel: `TechTreePanel` (+CanvasGroup), `TechTreeOpenButton` (+ cocugu `TechTreeBadge`),
`TechTreeCloseButton`, `TechTreeTitleText`, `TechTreeViewport` (ScrollRect + RectMask2D +
`TechTreeViewController`), `TechTreeContent`. HUD katmaninda: `TechTreeToastText` (panel disinda,
kapaliyken de gorunur toast).
Template (inactive, `TechTreeContent` altinda): `TechNodeTemplate` ve `TechConnectionTemplate`.
`TechNodeTemplate` cocuklari: `TechNodeTitleText`, `TechNodeLevelText`, `TechNodeCostText`,
`TechNodeStatusText`, `TechNodeDescriptionText`, `TechNodeIconImage`
(+ cocugu `TechNodeIconFallbackText`), `TechNodeBuyButton` (+ cocugu `TechNodeBuyButtonText`),
`TechNodePipsRoot` (+ cocuklari `TechNodePip1..4`).

DIKKAT: `ArrowTech*` ve `*TechUnlockButton` isim aileleri legacy gizleme listelerine takilir;
yeni tech UI objelerine bu isimler verilmez.

## Scope

- Veri: `ScriptableObject/TECH_TREE_SO_ARCHITECTURE.md`
- State/effect: `GameManager.cs` Tech Tree bolumu (satin alma kurali + aggregate uygulama)
- Binding/seed: `Editor/MobileCastleSceneSetupWindow.ConfigureTechTree` + `EnsureDefaultTechTreeCatalog`
- MarketUI iliskisi: sag drawer recruitment-only kalir; Rapid/Frost unlock'u tech
  node'larindan gelir (`IsArcherTypeUnlocked` tek dogruluk kaynagi)
