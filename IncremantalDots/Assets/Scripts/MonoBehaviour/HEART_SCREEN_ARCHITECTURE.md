# Castle Heart Screen ve Simulation Pause - Mimari

## Kapsam

`DW-E-UI`, generated Castle Heart graph'ini player-facing tam ekran bir yuzeye baglar ve
ekran acikken hem DOTS simulation'i hem de zaman bazli MonoBehaviour gameplay'ini durdurur.
UI'nin tek gorsel dogruluk kaynagi
`Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab` dosyasidir.

Bu paket production node, fiyat, Keystone cifti veya Evolution davranisi uretmez.
`GameManager.heartCatalog` owner-onayli bir `HeartNodeCatalogSO` almadan graph olusturulmaz;
ekran acik bir hata gosterir ve legacy `TechTreeCatalogSO`'ya geri dusmez.

## Runtime sahipleri

### HeartScreenUI

- `IHeartScreenRuntime` uzerinden presentation, quote ve purchase ister.
- Yalniz `HeartGraphPresentation` okur; hidden node id, baslik ve effect bilgisine erismez.
- Army sag, Defense sol, Production yukari, Heart/Magic asagi olacak sekilde deterministic
  compass layout kurar.
- Gorunur numeric effect'lerde actual `current -> after` ve delta degerini gosterir.
- Repeatable node'larda global `+1`, `+10`, `MAX`; tek seferlik node'larda yalniz `+1`
  kullanir.
- Keystone conflict bilgisini presentation contract'indaki safe baslik/slot ile cizer. Exact çift
  birlikte görünürken aynı depth orta noktasında orthogonal olarak ayrılır: Army/Defense dikey,
  Production/Heart Magic yatay iki kart olur. Altın fork/merge damarları iki seçeneğin aynı branch
  devamına sahip olduğunu, `CHOOSE ONE · RUN COMMITMENT` etiketi ise kalıcı koşu kararını anlatır.
- Panel, toast ve node animasyonlari unscaled DOTween zamaninda calisir.
- Gercek open-button aksiyonu basarili panel gecisinden sonra `HeartOpenedByPlayer`, gercek
  close-button veya Escape aksiyonu ise `HeartClosedByPlayer` event'ini yayar. Programmatic
  `OpenPanel`/`ClosePanel` cagrilari player action olarak raporlanmaz.
- `TechTreeViewController` yalniz pan/zoom yardimcisi olarak yeniden kullanilir; progression
  owner'i degildir.

### GameManager.HeartRuntime

`GameManager` partial'i su contract'lari birlikte uygular:

- `IHeartScreenRuntime`: graph/presentation/quote/purchase giris kapisi.
- `IHeartEffectBaselineProvider`: Heart katkisi eklenmemis canli baseline degerleri.
- `IHeartRuntimeEffectSink`: hazirlanmis actual numeric/behavior sonucunu gercek owner'a uygular.

Graph seed'i mevcut run id'sinin stable FNV hash'inden gelir. Generator settings Inspector'da
serialize edilir; graph reveal baslangici idempotenttir. Production catalog yoksa veya graph
validation fail ederse runtime fail-closed kalir.

Heart effect'leri diger progression sahiplerini ezmez. Archer, Wall, worker economy ve Fireball
degerleri Heart'siz baseline uzerine oran/additive bonus olarak birlesir. Arrow capacity ve
efficiency bonuslari `ArrowSupply.HeartCapacityBonus` ve `HeartEfficiencyBonus` alanlarinda,
odenen Arrow upgrade level'larindan ayri tutulur.

Rapid/Frost ve spellcasting unlock'lari canli state'e baglidir. Split Shot, Burning Ground ve
Second Blast flag'leri pipeline'da saklanir; owner-onayli production behavior sistemi gelmeden
ek gameplay davranisi uydurulmaz.

## Pause sahipligi

`SimulationPauseService` lease tabanli merkezi owner'dir:

1. Ilk lease, mevcut `Time.timeScale` ve `SimulationSystemGroup.Enabled` degerlerini exact
   olarak kaydeder.
2. Aktif lease varken `Time.timeScale = 0` ve `SimulationSystemGroup.Enabled = false` tutulur.
3. Heart ve pause menu gibi birden fazla owner ayni anda lease alabilir.
4. Bir owner kapaninca diger lease'ler simulation'i paused tutar.
5. Son lease dispose edilince ilk kaydedilen iki deger exact restore edilir.

`HeartScreenUI.Update()` ve `PauseMenuUI.Update()` aktif pause'u enforce eder. Boylece baska
bir sistemin yanlislikla time scale veya SimulationSystemGroup'u acmasi baseline'i kaybetmeden
duzeltilir.

Bu iki katman birlikte su alanlari durdurur:

- cycle, spawn, movement, combat ve DOTS worker simulation'i;
- time scale kullanan ability cooldown ve gameplay timer'lari.

UI event'i, tooltip, graph refresh, purchase feedback ve panel animasyonu unscaled zamanda
calismaya devam eder.

Panel fade, toast ve runtime node scale tween'leri `HeartScreenUI` sahipligindedir. Panel
kapanirken veya component disable olurken bu tween'ler explicit kill edilir; runtime node
silinmeden once hedef tween'i de durdurulur. `KillOnDestroy` link'i ikinci emniyet katmanidir.
Boylece scene reload/test sirasi yok edilmis `RectTransform` veya `CanvasGroup` hedeflerine
unscaled tween tasimaz.

## Prefab ve sahne cutover'i

Aktif prefab isim sozlesmesi:

- `CastleHeartOpenButton`, `CastleHeartPanel`, `CastleHeartCloseButton`;
- `HeartViewport`, `HeartContent`, `HeartNodeTemplate`, `HeartConnectionTemplate`;
- `GraveEssenceText`, `HeartScreenStatusText`, `HeartBranchCompassText`;
- `HeartQuantityOneButton`, `HeartQuantityTenButton`, `HeartQuantityMaxButton`;
- `CastleHeartBadge`, `CastleHeartToastText`;
- `HeartNode...` template alt alanlari.

`CastleHeartOpenButton`, alt-sag dock'ta `CASTLE HEART` label'iyle `156 x 56` sabit
butondur (`anchoredPosition = (-24,28)`). Yanindaki `ARCHERS` butonu ayni dock'ta
`(-190,28)` konumundadir; Archer drawer kayarken iki giris butonu sabit kalir.

`CastleHeartPanel`, `overrideSorting = true` ve `sortingOrder = 200` kullanan nested
`Canvas` ile kendi `GraphicRaycaster`'ini tasir. Boylece pause ve soul gibi ayri HUD
canvas'lari acik Heart ekraninin ustune cikamaz. `CastleHeartToastText` de ayni modal
Canvas altinda, son sibling olarak render edilir.

`NewGameScene/MobileCastleHudRoot` uzerinde aktif component `HeartScreenUI`dir. Legacy
`TechTreeUI` scene instance'inda bulunmaz. `MobileCastleSceneSetupWindow.ConfigureTechTree`
bu cutover'i idempotent uygular ve eski isimleri yalniz migration fallback'i olarak tanir.

## Persistence siniri

E5 runtime graph'i canli kosu icinde deterministic uretir. E6 `DW-E-SAVE`, exact graph DTO,
reveal, level ve Keystone lock state'ini v10'da save'e ekledi; guncel schema v11 bunu korur. Continue saved graph'i clone ve
validate eder; catalog'dan yeniden graph uretmez ve purchased effect'leri replay eder.

## Performans

- Pause acquire/enforce/release O(1)'dir.
- Heart UI kapaliyken graph view rebuild edilmez.
- Acikken presentation node sayisi kadar view ve connection senkronize edilir; hidden data
  redaction'i UI katmaninda tekrar hesaplanmaz.
- Purchase quote icin Buy Max binary-search contract'i korunur.
- Keystone fork bağlantıları yalnız panel refresh'inde, görünür çift başına sabit sayıda üretilir;
  per-frame gameplay sistemi eklenmez.

## Test

`HeartScreenPauseTests` lease nesting/exact restore, external resume repair, missing ECS group,
compass layout, runtime settings kopyasi ve Arrow Heart bonuslarinin paid level'lardan
ayrilmasini kapsar. Exact save/load ve gercek Continue pause testi E6'ya aittir.
`WorkerAllocationPlayModeTests.FirstEssenceHeartOnboarding...` gercek button event'ini,
pozitif Essence giris kapisini, panel acikken full pause bilgisini ve player close sonrasi
durable onboarding flag'ini birlikte dogrular.
`HeartProductionRuntimePlayModeTests.NewGameScene_PresentsKeystoneAsARealTwoCardCommitment`
production graph'ta çift reveal'i, ayrı kart konumlarını, commitment etiketi ve exact partner
kilidini gerçek prefab üzerinden doğrular.
