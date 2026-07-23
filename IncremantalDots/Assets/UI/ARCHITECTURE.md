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
- Orta: day/dusk/night/dawn durumu, geri sayim, baski mesaji, tek celestial arc ve panelin
  altindaki `1X/2X/3X` oyun hizi secimi. Tekrar eden alt faz ikonu/progress rayi yoktur.
- Sag: wall integrity, hostile sayisi, arrow reserve, run-ici Grave Essence ve souls.
- Alt orta: kritik combat abilities.
- Alt ray: economy, barracks, anlik `current / capacity` gosteren arrow supply, Castle Heart ve pause.

Yonetim ekranlarinin ortak karar sirasi `mevcut durum -> eylem -> maliyet -> beklenen sonuc -> engel nedeni`dir.
Kaynak veya requirement yetersizligi action dugmesini sessizce etkilesimsiz yapmaz. Terminal
olmayan reddedilebilir action tiklanabilir kalir, `is-action-unavailable` ile zayiflatilir ve
tiklandiginda canli maliyet/bakiye snapshot'indan exact warning toast uretir. Maximum level veya
tamamlanmis tek-seferlik satin alim gibi terminal durumlar gercek disabled state'ini korur.

## Runtime Yuzeyleri

- Economy drawer: worker hedef oranlari, capacity/efficiency, housing.
- Barracks drawer: archer katalogu, recruit ve retrain.
- Arrow Supply drawer: saha kasasi, wagon, fill reserve, capacity ve efficiency.
- Drawer header kontrati: baslik/aciklama kolonu `surface-header-copy` ile daralir ve uzun subtitle'i
  satir kirarak tam gosterir; `surface-close` daralmaz ve kendi alanini korur. Economy, Barracks ve
  Arrow Supply ayni no-overlap kontratini kullanir.
- Castle Heart: hidden-safe runtime presentation'dan uretilen dort kollu graph ve exact-effect inspector.
- War Doctrine: prerequisite depth'lerinden uretilen dinamik tech graph, pan, zoom ve inspector.
- Council: iki exact option sonucu; kart acikken pause ve secim sonrasi onceki hiz restore'u.
- Level Up: dogrudan `GameManager.GetCurrentUpgradeCards()` verisi.
- Pause, Settings, Game Over ve Meta Shop: ayni Toolkit modal sistemi. Meta kartlari katalog
  aciklamasinin yaninda exact mevcut -> sonraki kalici faydayi ve `LEVEL N -> N+1` transaction'ini
  gosterir.
- Feedback: critical banner, onboarding hint, en fazla uc gorunen kartlik sureli action-feedback
  toast stack'i, merkezi UI button click sesi, damage flash, day/night tint, soul pickup flight ve
  gercek dusman olumundeki basarili Grave Essence drop flight'i.
- Legacy `FirstRunOnboardingUI` hint/pulse sunumu yeni HUD'a aynalanmaz. Aktif first-run sahibi
  UI Toolkit'teki gercek kontrolleri izleyen `GameplayHUDToolkitUI.Onboarding`dir. Zorunlu
  `ECONOMY -> Wood slider -> CLOSE -> BARRACKS -> BASIC ARCHER -> 2X` zinciri hedef disi input'u
  kilitler ve `SimulationPauseService` lease'iyle oyunu durdurur. Son 2X action'i tutorial lease'ini
  birakip simulation'i secilen 2X running speed'de devam ettirir. Gorunur contextual field tipleri
  de action tamamlanana kadar oyunu durdurur fakat unrelated UI input'unu kilitlemez. Spotlight
  `Time.unscaledTime` ile padding/opacity/border nefes pulse'i uygular;
  kosullu Rally, Council, Arrow, Castle Heart, housing ve repair tip'leri unrelated kontrolleri
  kilitlemez.

Castle Heart ve Technology acikken simulasyon akmaya devam eder. Pause, level-up ve game-over
kendi mevcut duraklatma sozlesmelerini korur. Council secim yapilana kadar merkezi pause lease'i
tutar ve sonra secili kosu hizini geri yukler.

## Animasyon ve Polish Kurali

Runtime UI'da ornamental loop yoktur. Hareket yalniz state transition, hover/focus, acma-kapama, cooldown, damage, toast, soul pickup ve Grave Essence drop gibi anlam tasiyan geri bildirimlerde kullanilir. Main menu'deki tek surekli animasyon onayli day/dusk/night/dawn arka plan dongusudur.

## Responsive ve Erisilebilirlik

- Referans: 1920x1080.
- Landscape mobile ve PC ayni document'i kullanir.
- `input--touch`, `input--gamepad` ve `is-compact` siniflari hit-area, key hint ve olcek davranisini degistirir.
- Gamepad acilisinda ilk anlamli aksiyon focus alir.
- Renk tek basina durum tasimaz; label, sayi, state metni, terminal disabled hali ve exact action
  failure toast'i birlikte kullanilir.
- Player-facing metinlerin ilk surum dili Ingilizcedir. Internal servislerdeki Turkce debug,
  validation veya transaction mesaji UI label/toast'a dogrudan basilarak bu sinir delinmez;
  player copy'si Ingilizce presentation utility veya sabit UI kontratindan gelir.

## Gelistirme Debug UI

`DevelopmentTestPanel` player-facing UI degildir. Varsayilan olarak kapali baslar ve yalniz Editor/Development Build'de `F10` ile acilir.
