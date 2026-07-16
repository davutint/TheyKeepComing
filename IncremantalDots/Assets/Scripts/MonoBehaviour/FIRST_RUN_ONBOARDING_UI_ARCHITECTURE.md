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
- Oyuncu Heart'i ilk Grave Essence prompt'i uygun olmadan once acarsa gercek player event'i
  yine pause dersini baslatir; close sonrasinda durable flag yazilir ve Essence daha sonra
  geldiginde giris prompt'i tekrar gosterilmez.

## Ilk Regular Council Exact Karar Adimi

- Stable meta flag: `tutorial.v1.council`.
- Gorunme kapisi: mobile worker economy aktif, oyun bitmemis ve regular Council karti oyuncu
  secimine gercekten acik. Ilk kosuda bu durum regular takvimin ilk toplantisi olan Day 3'tur;
  tutorial sonradan resetlenmisse bir sonraki aktif regular kart ayni kontrati ogretebilir.
- Council karar penceresi sureli oldugu icin Heart pause adimi disindaki onceki opportunistic
  prompt'lari gecici olarak sunum onceliginde ezer. Onceki adimlarin durable state'i degismez.
- Pulse tek bir secenegi gostermek yerine `CouncilEventPanel` kartinin tamamini kapsar; tutorial
  Option A/B arasinda yonlendirme yapmaz.
- Tek player-facing metin English'tir: `COMPARE BOTH EXACT OUTCOMES AND THEIR COSTS.` Karttaki iki
  exact sonuc ve bedel `CouncilOptionPresentationUtility` live quote owner'inda kalir; tutorial
  metin veya sayi kopyalamaz.
- Yalniz `CouncilEventUI` butonundan baslayan ve `GameManager.ChooseCouncilOption` tarafindan
  basariyla commit edilen secim `CouncilChoiceCommittedByPlayer` event'ini yayar. Kartin acilmasi,
  Dusk expire'i veya basarisiz/locked tik flag yazmaz.
- Player secimi prompt gorunmeden once commit edilirse event yine flag'i tamamlar. Tutorial karti
  acmaz, secim yapmaz, timer'i uzatmaz, pause yaratmaz veya resource transaction'i cagirmaz.

## Ilk Daytime Wall Repair Adimi

- Stable meta flag: `tutorial.v1.repair`.
- Gorunme kapisi: mobile worker economy aktif, oyun bitmemis, continuous cycle gercek `Day`
  phase'inde ve yasayan Wall `%99,5` altinda hasarli. Normal repair gameplay olarak Dusk'ta da
  kullanilabilir; onboarding ise ilk guvenli management penceresini ogretmek icin yalniz Day'de
  sunulur.
- Stone affordability gorunme kapisi degildir. Oyuncu kaynagi yetmese bile gercek
  `DefenseRepairButton` pulse edilir ve authoritative `DefenseRepairCostText` maliyeti gosterir;
  disabled buton ve maliyet dili mevcut `DefenseRepairUI` sahibinde kalir.
- Tek player-facing metin English'tir: `REPAIR THE WALL DURING THE DAY.` Hint top-center savunma
  panelinin altinda `0,-294` konumuna tasinir ve gercek repair kontrolu disinda hedef uretmez.
- Yalniz `DefenseRepairUI` butonundan baslayip `GameManager.RepairDefenseFull()` tarafindan
  basariyla commit edilen islem `NormalRepairCommittedByPlayer` event'ini yayar ve durable flag'i
  yazar. Basarisiz/afford edilemeyen deneme, Wall'in yalniz hasar almasi veya baska bir gameplay
  owner'inin programmatic repair cagrisi tamamlanma sayilmaz.
- Tutorial Wall HP, Stone, phase veya button state'i yazmaz; repair miktari, exact Stone maliyeti
  ve transaction tamamen `GameManager` / `DefenseRepairUI` otoritesinde kalir.

## Ilk Night Ability Key Adimi

- Stable meta flag: `tutorial.v1.ability_key`.
- Gorunme kapisi: mobile worker economy aktif, oyun bitmemis, continuous cycle ilk kosunun
  `CycleIndex = 0` Night phase'inde ve en az bir aktif ability gercekten hazir.
- Hedef secimi gercek ability barinda sabit oncelikle yapilir: hazirsa `[1] Fireball`, degilse
  `[2] Rally`, degilse `[3] Emergency Repair`. Ilk kosunun mevcut baslangicinda Fireball tech
  kilitli ve Emergency Repair hasarli Wall istedigi icin garanti hedef hazir `[2] Rally` slotudur.
- Dynamic English metin hedefe gore `PRESS 1 TO TARGET FIREBALL.`, `PRESS 2 TO USE RALLY.` veya
  `PRESS 3 TO REPAIR THE WALL.` olur. Hint bottom-center ability barinin ustunde `0,170`
  konumlanir; pulse gercek slot `RectTransform`unu izler.
- Yalniz `SpellCastUI` icindeki kabul edilmis `1/2/3` keyboard yolu
  `AbilityHotkeyAcceptedByPlayer` event'ini yayar ve durable flag'i yazar. Locked/cooldown/
  phase tarafindan reddedilen tus, ability butonuna mouse click veya programmatic gameplay
  cagrisi key-hint ogretimini tamamlamaz.
- Kabul edilmis hotkey prompt gorunmeden once kullanilmissa adim tekrar gosterilmez. Tutorial
  ability kullanmaz, targeting baslatmaz, cooldown/state yazmaz ve resource harcamaz; butun
  transaction `SpellCastUI` ile `GameManager` otoritesinde kalir.

## Prompt Oncesi Completion Siniri

Prompt uygunlugu ve aktif sunum adimi completion kapisi degildir. Tutorial controller'i `Update`
icindeki cue secimini yalniz sunum icin kullanir; kabul edilmis player-action event handler'lari
`_activeStep`, hint veya pulse gorunurlugunu sorgulamadan ilgili durable flag'i yazar.

- Worker ratio, Basic Archer, Arrow refill, regular Council secimi, normal repair ve ability
  hotkey adimlari owner'larinin yalniz basarili transaction sonrasinda yaydigi event ile tamamlanir.
- Heart iki asamalidir: gercek open event'i pause dersini baslatir, gercek close event'i flag'i
  yazar. Ilk Essence henuz yoksa da bu accepted action kaybolmaz.
- Basarisiz, locked, cap-blocked, unaffordable veya programmatic gameplay cagrilari player-action
  event'i yaymadigi icin adimi tamamlamaz.
- Daha once tamamlanan flag, daha sonra presentation eligibility true oldugunda cue'nun yeniden
  acilmasini engeller.

EditMode source guard yedi completion handler'ini prompt/presentation state'inden bagimsiz tutar;
PlayMode preemptive Heart testi Essence yokken gercek open/close akisinin flag yazdigini ve sonraki
Essence gain'in prompt'i yeniden acmadigini kanitlar. Her owner'in accepted-event baglantisi kendi
hedefli PlayMode testinde ayrica korunur.

## Global Tutorial Complete Flag

- Stable meta flag: `tutorial.v1.complete`.
- Global flag yeni bir serialized schema alani degildir; mevcut `MetaProgressState v3`
  `TutorialFlags` listesinde diger stable onboarding flag'leriyle ayni atomik JSON save sahibini
  kullanir.
- Yedi zorunlu flag'in tamami durable oldugunda controller global flag'i yazar. Son accepted
  player action ayni frame'de completion kontrolunu tetikler.
- Eski save'de yedi alt flag mevcut fakat global flag yoksa `Update` ayni kosulu tekrar
  degerlendirip global flag'i durable backfill eder; meta schema version bump gerekmez.
- Global flag mevcutsa onboarding controller'i step eligibility veya gameplay wallet sorgularina
  girmeden shared hint/pulse sunumunu kapali tutar.
- Global flag tek basina alt flag'leri uretmez. Yalniz yedi stable flag'in tamaminin kanitindan
  turetilir; sonraki Settings reset isi global ve alt flag'leri birlikte temizlemelidir.

Pure completion kurali yedi zorunlu flag'in her birini ayri ayri gerektirir. PlayMode final-action
ve legacy-backfill testleri `meta_progress.json` reload sonrasinda `tutorial.v1.complete` flag'inin
durable kaldigini dogrular.

## Global Transaction-Free Siniri

`FirstRunOnboardingUI` gameplay command sahibi degildir. Controller yalniz authoritative state
okur, gercek player-action event'lerine subscribe olur, shared hint/pulse sunumunu yazar ve kabul
edilmis action sonrasinda `MetaProgression.SetTutorialFlag` ile tutorial completion saklar.

- Wood, Stone, Iron, Food, Arrow, Grave Essence veya Souls harcayamaz/veremez.
- Population, actual worker dagilimi, target ratio, bed veya worker-building yatirim state'i
  yazamaz.
- Archer satin alamaz, Council option secemez, Wall repair edemez veya ability kullanamaz.
- Worker/Archer/Ammo/Heart drawer ya da modalini oyuncu adina acamaz; yalniz gercek kontrolu
  pulse eder.
- Gameplay transaction'i yalniz ilgili owner (`WorkerEconomyDrawerUI`, `MarketUI`,
  `ArrowSupplyUI`, `HeartScreenUI`, `CouncilEventUI`, `DefenseRepairUI`, `SpellCastUI` ve
  `GameManager`) tarafindan player input sonrasinda baslatilir.

Bu sinir EditMode source guard ile yasak transaction/assignment cagrilarina; PlayMode'da ise
yedi cue'nun her birinde `ResourceData`, `ArrowSupply`, `GraveEssence`, `PopulationState`,
`MobilePopulationAllocation`, bed ve worker-building state snapshot'larina karsi kilitlenir.

## Modal Pause Zinciri Siniri

Onboarding yeni modal veya pause owner'i degildir. Full-simulation pause gerektiren tek tutorial
ani, oyuncunun gercek `HeartOpenButton` aksiyonuyla actigi ilk Castle Heart ekranidir.

- Pause lease'ini yalniz `HeartScreenUI` alir; `FirstRunOnboardingUI` lease alamaz, pause state'ini
  enforce edemez veya Heart/Pause/Settings modalini programmatic acamaz.
- Ilk Heart acikken tutorial yalniz mevcut modal uzerindeki `HeartPause` aciklamasini gosterir.
  Ayni anda Council, repair, ability veya baska onboarding cue'su zincirlenmez.
- Heart tutorial'i tamamlandiktan sonra Heart yeniden acilirsa ya da Pause Menu/LevelUp gibi baska
  blocking pause aktifse butun onboarding sunumu pause kapanana kadar gizlenir.
- Player Heart'i kapattiginda `HeartScreenUI` kendi tek lease'ini birakir. Active lease sayisi sifira,
  `Time.timeScale` onceki degerine doner; sonraki uygun adim yeni modal acmadan non-modal cue olarak
  devam eder.
- Regular Council compact karttir ve simulation pause sahibi degildir.

Pure `ShouldSuppressForBlockingPause` kurali ve PlayMode Heart -> Day repair gecis testi bu siniri
kilitler. Source guard ayrica pause lease/enforce ve modal open cagrilarini controller'da yasaklar.

## Sunum Siniri

`OnboardingHintPanel` ve `OnboardingPulseFrame`, aktif generated HUD prefabinin responsive
gorsel root'u altindadir. Raycast kapali oldugu icin combat ve management input'unu engellemez.
Pulse `Time.unscaledTime` kullanir; simulation pause state'ini degistirmez.

Heart pause hint'i pulse kullanmaz ve `Time.timeScale = 0` iken gorunur kalir. Hint kendi
basina yeni modal veya pause lease'i olusturmaz; yalniz zaten acik Castle Heart modalinin
mevcut full-pause davranisini aciklar. `OnboardingHintPanel` nested `Canvas`i normal adimlarda
parent siralamasini kullanir; yalniz Heart pause adiminda `overrideSorting = true / 260` olur ve
Heart modalinin `200` sorting order'i ustunde okunur.

Ability key adimi da ayni scene-owned controller ve stable meta flag sinirini kullanir; hicbir
tekil adim final tutorial complete flag'i yerine gecmez.
