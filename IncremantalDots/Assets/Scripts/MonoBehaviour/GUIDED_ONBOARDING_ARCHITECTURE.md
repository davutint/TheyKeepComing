# Guided Onboarding - Mimari

## Aktif Sahiplik

P16 sonrasinda first-run ogretiminin player-facing sahibi UI Toolkit HUD'dir.

- `GuidedOnboardingProgress`: saf sira, English copy ve session-scoped `tutorial.v2.*` flag sozlesmesi.
- `TutorialSessionProgress`: butun core/contextual flag'lerin tek runtime sahibidir; her Play
  baslangicinda temizlenir ve save dosyasi okumaz veya yazmaz.
- `GameplayHUDToolkitUI.Onboarding`: uygun adimi secer, gercek hedefi bulur, unscaled spotlight
  pulse'ini gunceller, tutorial pause lease'ini ve core input gate'ini uygular.
- Gercek gameplay owner'lari: Economy slider, Barracks, speed, ability, Council, Arrow Supply,
  Castle Heart ve housing islemlerinin authoritative basari sonucunu uretir.
- `FirstRunOnboardingUI`: legacy Canvas event kontratlari icin korunur; yeni
  player-facing hint/pulse sahibi degildir.

Tutorial gameplay transaction'i baslatmaz. Kaynak, worker, archer, arrow, phase veya upgrade
state'i yazmaz; yalniz aktif ogretim boyunca `SimulationPauseService` lease'i tutar ve basarili
player action'inin completion flag'ini mevcut Play oturumunda kaydeder.

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

## Pause ve Resume Sozlesmesi

Gorunur her guided adim `GuidedOnboarding` isimli ayri bir `SimulationPauseService` lease'i alir.
Core zincirde lease Economy adimindan 2X tamamlanana kadar kesintisiz korunur; boylece adimlar
arasinda simulation bir frame dahi akmaz. Son 2X aksiyonu pause altinda running speed'i `2X`
olarak yazar, core completion flag'i oturumda kaydedilince tutorial lease'i ayni action
frame'inde birakilir ve simulation `2X` ile devam eder.

Contextual field tip gorunur oldugunda da simulation durur; unrelated UI input'u kilitlenmez.
Contextual action tamamlaninca yalniz tutorial lease'i birakilir ve baska pause sahibi yoksa onceki
running speed geri gelir. Arrow refill ve Housing tipleri pause altinda kaynak bekleme soft-lock'i
olusturmamak icin yalniz en az bir refill veya bir bed satin alimi gercekten affordable iken acilir.
Council kendi `CouncilDecision` lease'ini korur; nested lease'lerde son sahip kapanana kadar oyun
durmaya devam eder.

## Contextual Adimlar

Core tamamlandiktan sonra uygun kosul ilk kez olustugunda su adimlar sunulur:

- First Night ve Rally hazirken `RALLY`.
- Aktif regular Council kartinda iki exact option'in ortak container'i.
- Arrow stoku effective kapasitenin `%25` veya altindayken refill paketi.
- Day/Dawn, pozitif Grave Essence ve kapali Heart yuzeyinde `CASTLE HEART`.
- Population toplam yatak kapasitesine ulastiginda housing paketi.
- Night sirasinda hasarli Wall ve hazir Emergency Repair icin repair ability'si.

Contextual adimlarda ekran kararmaz ve unrelated kontrol kilitlenmez; ancak aktif field tip
tamamlanana veya eligibility kaybolana kadar simulation pause lease'i tutulur. Ayni anda birden
fazla kosul varsa sabit sunum onceligi `Council -> Rally -> Repair -> Arrow -> Castle Heart -> Housing`
olarak uygulanir. Bu oncelik gameplay transaction onceligi degildir; yalniz hangi field tip'in
gosterilecegini belirler.

## Modal ve Pause Siniri

Council kendi mevcut `CouncilDecision` pause lease'inin sahibidir; guided presenter bunun yaninda
ayri lease tutabilir. Council disindaki blocking modal acikken core/contextual sunum gizlenir ve
modal kendi pause sahipligini korur. Contextual Council karti modal acikken gorunebilir; iki
option'dan biri tutorial tarafindan ayricalikli hale getirilmez.

Speed kontrolleri normalde pause altinda disabled kalir. Tek istisna aktif core hedef
`SpeedTwo` iken tutorial'in kendi lease'idir; bu durumda yalniz 2X hedefi gercek action olarak
secilebilir. Pause Menu veya baska blocking modal bu istisnayi acmaz.

## Development Session Progress

Her adimin ayri `tutorial.v2.*` flag'i vardir. Butun core ve contextual flag'ler tamamlandiginda
`tutorial.v2.complete` yalniz mevcut Play oturumunda kaydedilir.

`TutorialSessionProgress.BeginNewPlaySession`, `SubsystemRegistration` asamasinda butun v1/v2 ve
gelecekteki tutorial flag'lerini temizler. Bu yol domain reload kapali olsa dahi her Play'de
calisir. `meta_progress.json` icindeki eski `TutorialFlags` verisi okunmaz; alan schema uyumlulugu
icin `[NonSerialized]` tutulur ve yeni meta save'e yazilmaz.

Game Over restart'i ayni Play oturumunda oldugu icin completion state'ini korur. Editor'da Stop
ardindan Play yapildiginda core ve contextual tutorial tamamen bastan baslar. Settings
`RESET TUTORIAL` yalniz mevcut session'i elle temizleyen ikincil bir debug kontroludur; normal
development test akisi bunu gerektirmez. Production one-time persistence ve UGS entegrasyonu
tutorial tamamlanmaya yaklastiginda ayri kapsamda kurulacaktir.

## Player-Facing Copy

Butun baslik, aciklama ve step label'lari English'tir. Core label `TUTORIAL PAUSED`, contextual
label `FIELD TIP - GAME PAUSED` durumunu acikca soyler; action footer oyuncuya highlighted action'i
tamamlamasini belirtir. Her body yalniz komutu degil, sistemin neden onemli oldugunu da aciklar.
Runtime internal Turkce validation/debug mesajlarini dogrudan UI'ya basmaz. Copy
`GuidedOnboardingProgress.GetCopy` icindeki stable presentation sozlesmesinden gelir.

Focus rect `Time.unscaledTime` ile 1.15 cycle/s hizinda padding, opacity ve border width pulse'i
uygular. Animation simulation pause altinda da calisir; gameplay timeScale'ine bagli degildir.

## Dogrulama Kapilari

- EditMode: exact core sira, contextual priority, unique flag/copy, session reset, meta
  non-serialization ve UXML/USS/source kontratlari.
- PlayMode: gercek UI Toolkit Economy button, Wood slider, Economy Close, Barracks button, Basic
  Archer purchase ve 2X button zinciri; hedef disi submit'in engellenmesi; spotlight-target overlap;
  core boyunca pause, 2X completion sonrasi exact resume, affordable Housing field tip pause/resume,
  yeni Play session reset'inin Economy adimini yeniden acmasi ve meta save'e dokunmamasi.
- P15 regresyonu: yeni tutorial gate tamamlanmis legacy tutorial'da toast/action testlerini
  etkilemez.
