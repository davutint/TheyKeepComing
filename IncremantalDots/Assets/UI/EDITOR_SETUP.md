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

## Eski Canvas Sozlesmesi

Eski Canvas'i sahneden silme. Controller ve serialized referanslar gecis boyunca davranis/data source olarak kullanilir. Play Mode'da Canvas gorunur kalirsa `GameplayHUDToolkitUI` ve `HUDController` referanslarini, UIDocument visual tree'sini ve Console'u kontrol et.

## QA Sirasi

1. Console'da compile ve USS parse error olmadigini dogrula.
2. HUD'u 1920x1080'de render et; kalici cluster'lar dunya gorunurlugunu kapatmamalidir.
3. Economy, Barracks ve Arrow Supply drawer'larini ayri ayri ac.
4. Castle Heart'ta 19 node hidden-safe graph'in ve inspector exact copy'sinin geldigini kontrol et.
5. Technology'de gorunur node sayisina gore graph'in ortalandigini; pan, zoom ve Center aksiyonlarini kontrol et.
6. Council, Level Up, Pause, Settings ve Game Over/Meta Shop modallarini kontrol et.
7. Pointer, gamepad ve touch girdileriyle root class degisimini ve focus/hit-area davranisini kontrol et.
8. EditMode ve PlayMode testlerini Unity Test Runner veya Unity MCP ile calistir.

## Debug Panel

Play Mode test aracini acmak icin `F10` kullan. Paneli acmak transient test session'ini etkinlestirebilir ve run save'i bloke eder; normal oyuncu QA'sinda kapali tut.
