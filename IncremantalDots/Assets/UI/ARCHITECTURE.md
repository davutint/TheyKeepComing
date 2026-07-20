# Dead Walls UI Toolkit - Mimari

## Aktif Player-Facing Katman

Oyuncunun gordugu runtime arayuzun tek sunum sahibi UI Toolkit'tir.

- `MainMenuScene`: ayri UI Toolkit menu document'i.
- `NewGameScene`: `UIToolkitGameplayHUD` GameObject'i uzerindeki `UIDocument`.
- Runtime document: `Assets/UI/Toolkit/GameplayHUD/GameplayHUD.uxml`.
- Tasarim sistemi: `Assets/UI/Toolkit/GameplayHUD/GameplayHUD.uss`.
- Runtime owner: `GameplayHUDToolkitUI` partial controller ailesi.
- Cihaz sunumu: `UIInputModeService`; pointer, touch ve gamepad siniflarini son anlamli girdiye gore otomatik degistirir.

Eski Canvas halen veri ve davranis koprusu olarak sahnede bulunur. `GameplayHUDToolkitUI`, Play Mode'da Canvas'i `CanvasGroup` ile gorunmez ve etkilesimsiz yapar; eski controller'lar oyun durumunu, callback'leri ve kayitli davranislari surdurur. Yeni UI eski Canvas'in layout, renk veya hiyerarsisini gorsel referans olarak kullanmaz.

## Bilgi Mimarisi

Kalici HUD yalniz ayni anda izlenmesi gereken iki yuk tipini tasir:

- Sol: kaynaklar, dakika dengesi, population, idle worker.
- Orta: day/dusk/night/dawn durumu, geri sayim, baski mesaji ve tek celestial arc. Tekrar eden alt faz ikonu/progress rayi yoktur.
- Sag: wall integrity, hostile sayisi, arrow reserve, run-ici Grave Essence ve souls.
- Alt orta: kritik combat abilities.
- Alt ray: economy, barracks, anlik `current / capacity` gosteren arrow supply, Castle Heart ve pause.

Yonetim ekranlarinin ortak karar sirasi `mevcut durum -> eylem -> maliyet -> beklenen sonuc -> engel nedeni`dir.

## Runtime Yuzeyleri

- Economy drawer: worker hedef oranlari, capacity/efficiency, housing.
- Barracks drawer: archer katalogu, recruit ve retrain.
- Arrow Supply drawer: saha kasasi, wagon, fill reserve, capacity ve efficiency.
- Castle Heart: hidden-safe runtime presentation'dan uretilen dort kollu graph ve exact-effect inspector.
- War Doctrine: prerequisite depth'lerinden uretilen dinamik tech graph, pan, zoom ve inspector.
- Council: iki exact option sonucu ve karar suresi.
- Level Up: dogrudan `GameManager.GetCurrentUpgradeCards()` verisi.
- Pause, Settings, Game Over ve Meta Shop: ayni Toolkit modal sistemi.
- Feedback: critical banner, onboarding hint, toast, damage flash, day/night tint, soul pickup flight ve gercek dusman olumundeki basarili Grave Essence drop flight'i.

Castle Heart ve Technology acikken simulasyon akmaya devam eder. Pause, level-up ve game-over kendi mevcut duraklatma sozlesmelerini korur. Council mevcut runtime karar sozlesmesini kullanir.

## Animasyon ve Polish Kurali

Runtime UI'da ornamental loop yoktur. Hareket yalniz state transition, hover/focus, acma-kapama, cooldown, damage, toast, soul pickup ve Grave Essence drop gibi anlam tasiyan geri bildirimlerde kullanilir. Main menu'deki tek surekli animasyon onayli day/dusk/night/dawn arka plan dongusudur.

## Responsive ve Erisilebilirlik

- Referans: 1920x1080.
- Landscape mobile ve PC ayni document'i kullanir.
- `input--touch`, `input--gamepad` ve `is-compact` siniflari hit-area, key hint ve olcek davranisini degistirir.
- Gamepad acilisinda ilk anlamli aksiyon focus alir.
- Renk tek basina durum tasimaz; label, sayi, state metni ve disabled hali birlikte kullanilir.
- Player-facing metinlerin ilk surum dili Ingilizcedir.

## Gelistirme Debug UI

`DevelopmentTestPanel` player-facing UI degildir. Varsayilan olarak kapali baslar ve yalniz Editor/Development Build'de `F10` ile acilir.
