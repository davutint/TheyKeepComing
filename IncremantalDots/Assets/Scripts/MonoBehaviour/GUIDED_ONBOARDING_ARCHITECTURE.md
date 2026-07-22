# Guided Onboarding - Mimari

## Aktif Sahiplik

P16 sonrasinda first-run ogretiminin player-facing sahibi UI Toolkit HUD'dir.

- `GuidedOnboardingProgress`: saf sira, English copy ve durable `tutorial.v2.*` flag sozlesmesi.
- `GameplayHUDToolkitUI.Onboarding`: uygun adimi secer, gercek hedefi bulur, spotlight geometrisini
  gunceller ve core input gate'ini uygular.
- Gercek gameplay owner'lari: Economy slider, Barracks, speed, ability, Council, Arrow Supply,
  Castle Heart ve housing islemlerinin authoritative basari sonucunu uretir.
- `FirstRunOnboardingUI`: legacy Canvas kontratlari ve eski save uyumlulugu icin korunur; yeni
  player-facing hint/pulse sahibi degildir.

Tutorial gameplay transaction'i baslatmaz. Kaynak, worker, archer, arrow, phase, pause veya
upgrade state'i yazmaz; yalniz basarili player action'inin durable completion flag'ini kaydeder.

## Zorunlu Core Zincir

Core sira sabittir:

1. `ECONOMY`
2. Wood `WORKER SHARE` slider'i
3. Economy `CLOSE`
4. `BARRACKS`
5. `BASIC ARCHER` satin alimi
6. `2X` oyun hizi

Economy slider islemi tamamlandiktan sonra spotlight gercek `economyClose` butonuna tasinir.
Barracks command-rail hedefi Economy drawer gercek player action'iyla kapanmadan acilmaz. Bir
drawer hedefi kapaliysa spotlight once gercek command-rail butonunu gosterir. Drawer acildiktan
sonra ayni adim gercek slider veya purchase kontrolune tasinir. Adim yalniz ilgili
authoritative islem `true` dondurdugunde tamamlanir; tik, hover, drawer'in programmatic acilmasi,
reddedilmis satin alim veya degismeyen slider degeri completion sayilmaz.

## Core Input Gate

Core adimlarda dort raycast-disli olmayan dim rect hedef disini karartir. Root'a trickle-down
asamasinda baglanan `PointerDownEvent`, `ClickEvent` ve `NavigationSubmitEvent` yalniz aktif hedef
ve hedefin child subtree'si icin gecirilir. Diger UI action'lari `StopImmediatePropagation` ile
kesilir. Spotlight, dim ve bilgi kartinin tamami `PickingMode.Ignore` oldugu icin hedef kontrolu
kendi gercek callback zincirini kullanir.

Spotlight geometrisi hedefin layout ofsetini ikinci kez eklememek icin sifir-orijinli local rect'i
`ChangeCoordinatesTo` ile document root koordinatina donusturur. Dogrudan `target.localBound`
donusturulmez; nested drawer kontrollerinde bu deger layout konumunu iki kez sayar.

Global Escape/Start menu islemi core zincir boyunca bastirilir. Slider drag hareketi hedef
subtree'sinde devam eder. Gate core adim tamamlaninca ayni frame'de kapanir.

## Contextual Adimlar

Core tamamlandiktan sonra uygun kosul ilk kez olustugunda su adimlar sunulur:

- First Night ve Rally hazirken `RALLY`.
- Aktif regular Council kartinda iki exact option'in ortak container'i.
- Arrow stoku effective kapasitenin `%25` veya altindayken refill paketi.
- Day/Dawn, pozitif Grave Essence ve kapali Heart yuzeyinde `CASTLE HEART`.
- Population toplam yatak kapasitesine ulastiginda housing paketi.
- Night sirasinda hasarli Wall ve hazir Emergency Repair icin repair ability'si.

Contextual adimlarda ekran kararmaz ve unrelated kontrol kilitlenmez. Ayni anda birden fazla
kosul varsa sabit sunum onceligi `Council -> Rally -> Repair -> Arrow -> Castle Heart -> Housing`
olarak uygulanir. Bu oncelik gameplay transaction onceligi degildir; yalniz hangi field tip'in
gosterilecegini belirler.

## Modal ve Pause Siniri

Council kendi mevcut `CouncilDecision` pause lease'inin sahibidir. Tutorial pause almaz veya
birakmaz. Council disindaki blocking modal acikken core/contextual sunum gizlenir. Contextual
Council karti modal acikken gorunebilir; iki option'dan biri tutorial tarafindan ayricalikli hale
getirilmez.

## Durable Progress

Her adimin ayri `tutorial.v2.*` flag'i vardir. Butun core ve contextual flag'ler tamamlandiginda
`tutorial.v2.complete` yazilir. Mevcut `tutorial.v1.complete` eski oyuncular icin v2 sunumunu da
bastirir; boylece tamamlanmis tutorial bir guncelleme sonrasi zorla tekrar acilmaz.

Settings `RESET TUTORIAL` islemi v1 ve v2 flag listelerini ayni atomik meta transaction'da
temizler. Run restart, Game Over veya run save silme tutorial progress'ini temizlemez.

## Player-Facing Copy

Butun baslik, aciklama ve step label'lari English'tir. Runtime internal Turkce validation/debug
mesajlarini dogrudan UI'ya basmaz. Copy `GuidedOnboardingProgress.GetCopy` icindeki stable
presentation sozlesmesinden gelir.

## Dogrulama Kapilari

- EditMode: exact core sira, contextual priority, unique flag/copy ve UXML/USS/source kontratlari.
- PlayMode: gercek UI Toolkit Economy button, Wood slider, Economy Close, Barracks button, Basic
  Archer purchase ve 2X button zinciri; hedef disi submit'in engellenmesi; spotlight-target overlap; contextual
  adimin input kilitlememesi.
- P15 regresyonu: yeni tutorial gate tamamlanmis legacy tutorial'da toast/action testlerini
  etkilemez.
