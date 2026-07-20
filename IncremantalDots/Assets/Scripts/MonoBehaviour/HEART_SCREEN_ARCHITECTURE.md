# Castle Heart UI Toolkit Screen - Mimari

## Kapsam

Castle Heart, oyundaki tek player-facing teknoloji ağacıdır. Production render owner'ı
`GameplayHUDToolkitUI.CastleHeart.cs`, şablonu `GameplayHUD.uxml`, görsel dili
`GameplayHUD.uss` dosyasıdır. Legacy `HeartScreenUI` yalnız mevcut ses klipleri ve davranış
köprüsü için bulunabilir; layout veya render referansı değildir.

Castle Heart tam ekran açılırken gameplay simulation akmaya devam eder. Bu yüzey pause lease
almaz, `Time.timeScale` veya DOTS `SimulationSystemGroup` state'ini değiştirmez.

## Bilgi ve görünürlük sınırı

UI yalnız `GameManager.TryBuildHeartPresentation` sonucunu tüketir. Presentation içindeki
`IsExactContentVisible == false` node'lar UI visual tree'sine hiç eklenmez:

- İlk açılışta merkez Castle Heart ve tam dört başlangıç teknolojisi görünür.
- Silhouette, soru işareti, boş slot, gelecek başlığı veya branch etiketi çizilmez.
- Bir teknoloji ilk kez satın alındığında yalnız kendi authored outgoing edge hedefleri belirir.
- Bir node iki doğrudan child taşıyorsa ikisi de kısa stagger ile belirir; başka depth cascade olmaz.
- Eski Keystone/content sınıflandırması sibling lock veya mutually-exclusive seçim üretmez.

Bu filtre, hidden-safe presentation modelinde topology bulunabilse bile oyuncuya gizli içeriğin
yanlışlıkla sızmasını engeller.

## Yerleşim ve görsel hiyerarşi

- Castle Heart büyük, merkezî medalyondur.
- Dört içerik yönü, `HeartConquestLayoutUtility` içindeki deterministic fakat asimetrik waypoint
  kümeleriyle ayrışır. Eşit aralıklı düz raylar yerine her yol yön değiştirerek küçük teknoloji
  adaları oluşturur; oyuncuya Army, Defense, Production veya Heart Magic etiketi yazılmaz.
- Node'lar compact icon-first butonlardır. Başlık socket altında kısa bir satırdır; level/owned
  bilgisi küçük badge ile gösterilir.
- Bütün 37 node ikonu `Assets/RPG Icons Pixel Art` içinden görsel incelemeyle seçilmiştir.
- Görünür ağacın bounds'u ölçülür; ilk halka okunaklı yakın görünür, ağaç büyüdükçe otomatik
  olarak viewport'a sığacak şekilde uzaklaşır.
- PC'de inspector sağda, compact/touch düzende altta yaşar.
- Inspector title, description, gerçek resolved effect, state ve Essence maliyetini gösterir.
  Kod/veri owner'ı `GraveEssence` adını korur; player-facing Castle Heart copy'si yalnız `ESSENCE`
  kullanır ve header HUD ile aynı mor renk/icon kimliğini taşır.
  Branch adı veya internal `Unlock/Evolution/Keystone` sınıfı göstermez.

## Etkileşim

- İlk satın alma eylemi `RESEARCH`.
- Repeatable node'un sonraki tek-level satın alımı `UPGRADE`.
- Tamamlanan tek-seferlik node `RESEARCHED` olarak disabled kalır.
- Player-facing yüzey `+10`, `MAX`, `UNLOCK`, `EVOLVE`, `DEEPEN` veya `COMMIT` jargonunu kullanmaz.
- Satın alma `HeartPurchaseQuantity.One` ile canonical `HeartPurchaseService` kapısından geçer.
- Yetersiz Essence veya runtime blocker inspector state ve toast ile açıklanır.

### Graph navigation

- İlk açılış ve `FIT`, yalnız görünür node bounds'unu viewport'a otomatik sığdırır.
- PC'de mouse wheel imlecin altındaki graph noktasını koruyarak zoom yapar.
- Touch cihazlarda iki parmak pinch hem zoom hem midpoint pan üretir.
- Mouse veya tek parmak, yalnız boş graph alanından başladığında pan yapar; teknoloji node'u ve kontrol
  butonları click/tap sahipliğini korur.
- Header'daki `- / yüzde / + / FIT` kontrolü pointer ve touch için aynı işlevi sunar. Yüzde, otomatik
  fit ölçeğine göre `65% - 225%` aralığını gösterir.
- Pan sınırı görünür node bounds'unu tamamen kaybettirmez. Reveal veya responsive relayout sırasında
  kullanıcı zoom/pan durumu korunur; graph yeniden açılırken gereksiz kamera sıçraması oluşmaz.

## Bağlantı ve reveal polish'i

`HeartConnectorLayer`, Unity UI Toolkit `Painter2D` ile bağlantıları çizer:

- Connector koordinatları node button merkezine değil gerçek dairesel socket merkezine bağlanır;
  yol socket sınırında başlar ve biter.
- Düz çizgi yerine iki control point'li cubic rota kullanılır. Ana parent yolu kısa ve güçlü,
  cross-link ise daha geniş kavisli ve daha ince çizilir.
- Koyu halo üstüne yüksek kontrast branch-tint çizildiği için karanlık zeminde ve uzun rotalarda
  noktalı bağ kaybolmaz.
- Round cap ile ana bağlarda `3.4 / 6.8`, cross-link'lerde `2.4 / 8.8` noktalı patern kullanılır.
- Yeni edge parent'tan child'a yaklaşık `340ms` içinde büyür.
- Child icon edge ilerledikten sonra opacity/scale transition ile görünür; iki child `95ms`
  stagger kullanır.
- Sürekli particle, pulse veya dekoratif hareket yoktur.

Başarılı purchase mevcut `BuyClip`, reveal varsa `RevealClip`, blocker ise `DeniedClip` kullanır.
Ses seviyesi `SoundSettings.SfxVolume` ile çarpılır.

## Responsive davranış

`HandleGeometryChanged` hem root responsive class'larını hem `RelayoutHeartGraph` hesaplamasını
yeniler:

- Geniş PC: graph solda, inspector sağda.
- Compact/touch: graph üstte, yatay inspector altta.
- Görünür node bounds'u ve kullanılabilir viewport ayrı hesaplandığı için gizli depth'ler ilk
  açılış zoom'unu küçültmez.
- Touch modunda zoom kontrolü `46px` butonlara büyür; mevcut ortak minimum hit-area kuralları korunur.

## Doğrulama

- `HeartGraphRevealTests`: başlangıç `4`, direct-child reveal, cascade yok, legacy lock cleanup.
- `HeartPurchasePipelineTests`: Grave Essence transaction, normal ağaç, bağımsız eski Keystone
  node'ları ve direct continuation.
- `GameplayHUDToolkitContractTests`: ikinci tech surface yok, branch legend/bulk controls yok,
  inspector/navigation contract'ları var ve 37 icon onaylı paketten geliyor.
- `HeartScreenPauseTests`: zoom clamp ve anchored zoom matematiği.
- Canlı Game View kontrolünde ilk açılışta `5` visual node (root + 4), bir başlangıç teknolojisi
  araştırıldıktan sonra `6` visual node ölçülmüştür.
