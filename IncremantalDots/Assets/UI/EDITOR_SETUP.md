# Dead Walls UI Toolkit - Editor Kurulum ve Dogrulama

## Surum ve Sahne

- Unity: `6000.3.10f1`.
- Gameplay sahnesi: `Assets/Scenes/NewGameScene.unity`.
- Aktif root: `UIToolkitGameplayHUD`.
- Gerekli component'ler: `UIDocument`, `UIInputModeService`, `GameplayHUDToolkitUI`.

## UIDocument

`UIDocument.sourceAsset`, `Assets/UI/Toolkit/GameplayHUD/GameplayHUD.uxml` olmali. Document, projedeki runtime Panel Settings ile 1920x1080 referans cozunurlukte calisir. UXML ve USS isimleri controller query'leriyle sozlesmedir; element adlarini degistirirken ilgili `GameplayHUDToolkitUI` partial dosyalarini birlikte guncelle.

## Controller Dosyalari

- `GameplayHUDToolkitUI.cs`: lifecycle, legacy suppression, HUD ve surface koordinasyonu.
- `GameplayHUDToolkitUI.Management.cs`: economy, barracks ve arrows.
- `GameplayHUDToolkitUI.Graphs.cs`: Castle Heart ve Technology.
- `GameplayHUDToolkitUI.Modals.cs`: Council, level-up, pause/settings ve game-over/meta.
- `GameplayHUDToolkitUI.Feedback.cs`: onboarding, toast, soul flight ve feedback kopruleri.
- `GameplayHUDToolkitUI.GameFlow.cs`: `1X/2X/3X` binding'i, secili hiz sunumu ve bounded FIFO
  toast presenter'i.
- `GameplayActionPresentationUtility.cs`: player tiklamasiyla reddedilen satin alma/research
  eylemlerinin exact nedenleri ve Game Over meta kartlarinin mevcut -> sonraki effect copy'si.

## Eski Canvas Sozlesmesi

Eski Canvas'i sahneden silme. Controller ve serialized referanslar gecis boyunca davranis/data source olarak kullanilir. Play Mode'da Canvas gorunur kalirsa `GameplayHUDToolkitUI` ve `HUDController` referanslarini, UIDocument visual tree'sini ve Console'u kontrol et.

## QA Sirasi

1. Console'da compile ve USS parse error olmadigini dogrula.
2. HUD'u 1920x1080'de render et; kalici cluster'lar dunya gorunurlugunu kapatmamalidir.
3. Economy, Barracks ve Arrow Supply drawer'larini ayri ayri ac.
4. Castle Heart'ta 19 node hidden-safe graph'in ve inspector exact copy'sinin geldigini kontrol et.
5. Technology'de gorunur node sayisina gore graph'in ortalandigini; pan, zoom ve Center aksiyonlarini kontrol et.
6. `1X/2X/3X` butonlarini ve aktif hiz state'ini kontrol et; Council'i `3X` hizda acip
   simulation'in durdugunu ve secimden sonra `3X` devam ettigini dogrula.
7. Barracks'ta kaynagi yetmeyen bir Archer recruit action'ina bas; warning toast'in eksik kaynak
   adini ve exact miktari gosterdigini kontrol et. Worker yok, garrison full ve locked type
   durumlarinin kaynak hatasi gibi sunulmadigini dogrula.
8. Council, Level Up, Pause, Settings ve Game Over/Meta Shop modallarini kontrol et. Game Over'da
   her kartin neyi kalici degistirdigini ve satin alim sonrasi exact toplami gosterdigini; Ember
   yetmeyen karta basinca exact eksik miktarin toast'a geldigini kontrol et.
9. Pointer, gamepad ve touch girdileriyle root class degisimini ve focus/hit-area davranisini kontrol et.
10. EditMode ve PlayMode testlerini Unity Test Runner veya Unity MCP ile calistir.

## Debug Panel

Play Mode test aracini acmak icin `F10` kullan. Paneli acmak transient test session'ini etkinlestirebilir ve run save'i bloke eder; normal oyuncu QA'sinda kapali tut.
