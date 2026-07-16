# First Run Onboarding UI - Mimari

## Sorumluluk

`FirstRunOnboardingUI`, Package I ilk-kosu ogretiminin scene-owned sunum sahibidir. Gameplay
transaction'i, otomatik drawer acma, resource harcama veya worker dagitma yapmaz. Gercek UI
owner'larinin player-action event'lerini dinler; yalniz non-modal hint ve pulse gosterir.

## Ilk Day Worker Ratio Adimi

- Stable meta flag: `tutorial.v1.worker_ratio`.
- Gorunme kapisi: mobile worker economy aktif, oyun bitmemis, continuous cycle Day 1 / Day.
- Drawer kapaliyken `WorkerDrawerToggleButton`, acikken `WoodWorkerTargetPlus10Button` pulse olur.
- Tek player-facing metin English'tir: `ADJUST A WORKER TARGET RATIO.`
- Herhangi bir resource target ratio islemi `GameManager` tarafinda basarili oldugunda
  `WorkerEconomyDrawerUI.WorkerTargetRatioChangedByPlayer` event'i yayilir.
- Event prompt gorunmeden once gelse bile adim tamamlanir. Durable flag yazimi basarisizsa
  hint kapanmaz ve state fail-closed kalir.

## Ilk Basic Archer Affordability Adimi

- Stable meta flag: `tutorial.v1.basic_archer`.
- Gorunme kapisi: mobile worker economy aktif, oyun bitmemis ve authoritative
  `GameManager.CanBuyArcher(Basic)` sonucu true.
- Worker ratio adimi ayni anda gorunuyorsa worker adimi sunum onceligini korur; Basic satin alma
  prompt'tan once yapilirsa event yine flag'i tamamlar.
- Drawer kapaliyken `DrawerToggleButton`, acikken runtime-generated Basic row'un gercek
  `ArcherBuyButton` kontrolu pulse olur.
- Tek player-facing metin English'tir: `RECRUIT A BASIC ARCHER.`
- Yalniz `MarketUI` uzerinden basarili gercek Basic satin alma
  `ArcherPurchasedByPlayer` event'ini yayar. Basarisiz/locked/cap-blocked tik flag yazmaz.

## Ilk Dusuk Ammo Adimi

- Stable meta flag: `tutorial.v1.low_ammo`.
- Gorunme kapisi: mobile worker economy aktif, oyun bitmemis ve finite Arrow stoku effective
  kapasitenin inclusive `%25` veya altinda.
- Worker ratio ve Basic Archer adimlari ayni anda uygunsa onceki adimlar sunum onceligini korur;
  prompt gorunmeden yapilan basarili refill yine ammo adimini tamamlar.
- Tek hedef, ust resource strip'teki gercek `ArrowSupplyUI.ToggleButton` / `ArrowChip` satiridir.
  `AmmoPurchasePanel` oyuncu adina acilmaz.
- Tek player-facing metin English'tir: `RESTOCK YOUR ARROWS.`
- Yalniz `ArrowSupplyUI` uzerinden basarili `+1`, `+5` veya `Buy Max` refill satin alimi
  `ArrowRefillPurchasedByPlayer` event'ini yayar. Basarisiz refill ve CAP/EFF yatirimi flag yazmaz.

## Ilk Grave Essence / Castle Heart Adimi

- Stable meta flag: `tutorial.v1.heart`.
- Gorunme kapisi: mobile worker economy aktif, oyun bitmemis ve authoritative
  `GameManager.GraveEssenceAmount > 0`.
- Bu adim kill basina Essence miktari veya drop orani tanimlamaz; yalniz ilk gercek pozitif
  Heart bakiyesini gozlemler. Essence gain dengesi tracker'daki ayri balance isinin sahibidir.
- Grave Essence runtime sorgusu yalniz onceki presentation adimlari uygun degilken ve Heart
  flag'i incomplete iken yapilir; durable completion sonrasinda per-frame Heart wallet okumasi yoktur.
- Heart kapaliyken gercek `HeartScreenUI.HeartOpenButton` / `CastleHeartOpenButton` pulse olur;
  panel oyuncu adina acilmaz.
- Giris metni English'tir: `OPEN THE CASTLE HEART.`
- Yalniz gercek Heart butonu `HeartOpenedByPlayer` event'ini yayar. Panel acilinca mevcut
  `SimulationPauseService` lease'i simulation'i durdurur; pulse kapanir ve unscaled, tek satir
`THE CASTLE HEART FULLY PAUSES THE BATTLE.` hint'i modal yuzeyin ustunde gosterilir.
- Durable flag, oyuncu Heart'i close butonu veya Escape ile kapattiginda gelen
  `HeartClosedByPlayer` event'inde yazilir. Programmatic open/close cagrilari tutorial'i
  tamamlamaz; pause bilgisi gorulmeden flag yazilmaz.

## Sunum Siniri

`OnboardingHintPanel` ve `OnboardingPulseFrame`, aktif generated HUD prefabinin responsive
gorsel root'u altindadir. Raycast kapali oldugu icin combat ve management input'unu engellemez.
Pulse `Time.unscaledTime` kullanir; simulation pause state'ini degistirmez.

Heart pause hint'i pulse kullanmaz ve `Time.timeScale = 0` iken gorunur kalir. Hint kendi
basina yeni modal veya pause lease'i olusturmaz; yalniz zaten acik Castle Heart modalinin
mevcut full-pause davranisini aciklar. `OnboardingHintPanel` nested `Canvas`i normal adimlarda
parent siralamasini kullanir; yalniz Heart pause adiminda `overrideSorting = true / 260` olur ve
Heart modalinin `200` sorting order'i ustunde okunur.

Sonraki Council, repair ve ability onboarding adimlari ayni
scene-owned controller ve stable meta flag siniri uzerinden eklenir; bu adim final tutorial
complete flag'i yerine gecmez.
