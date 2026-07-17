# Council Events (Safak Meclisi) - Architecture

## Amac

Dusuk frekansli, iki secenekli mikro kararlar: kart DAWN'da belirir (dongunun en sakin ani,
konsey safakta toplanir), DAY boyunca yasar, DUSK girisinde secilmemisse "konsey dagilir".
Oyun HICBIR zaman durmaz. Event'ler ASSET DEGILDIR — CouncilComposer sablon x atom x
baglam x olcek carpiminden runtime'da uretir. Production katalog 9 launch sablonu ve 11
serialized atom tasir; `cap_bonus` yalniz legacy uyumluluk icin dormant kalir ve hicbir launch
recetesi tarafindan kabul edilmez. Diger 10 atom authored tariflerde kullanilir.

## Katmanlar

1. **Veri (ScriptableObject/):** `CouncilEffectAtomSO` (etki parcacigi: tur + uretim-oranli
   buyukluk + director agirlik kurallari + butce degeri), `CouncilTemplateSO` (tema/metin +
   karsitlik tipi + flag kosullari + zincir alanlari), `CouncilEventCatalogSO` (havuz +
   `CouncilEffectBandSettings` + RecentTemplateMemory 3 + explicit `CuratedChains` allowlist'i).
   `ValidateCatalog`,
   `CouncilContentPolicy` role/recipe kontratini da zorunlu tutar. Legacy
   DailyEventChance/PityDays/CooldownDays alanlari serialized asset uyumlulugu icin saklidir
   fakat regular schedule tarafindan kullanilmaz.
2. **Composer (`CouncilComposer.cs`, pure static):** yalniz runtime-content gate'inden gecen
   katalogla seed + `CouncilContext` -> sablon sec
   (curated flag/gun filtreleri + hard anti-tekrar + iki secenekli director on-skoru) ->
   karsitlik recetesine gore A/B
   atomlari -> uretim-oranli miktarlar (`perMin * MinutesOfProduction * band`; production
   varsayilani 0.7/1.0/1.4 ve %35/%50/%15) -> authored `BudgetTolerance` asilirsa A/B
   "dakika-degeri" dengelemesi. Composer bu degerleri sabit koddan degil katalogdan okur.
   DETERMINISTIK: ayni seed + ayni context = ayni event (EditMode testli). Rng warm-up
   ardisik-seed korelasyonunu kirar.
3. **Runtime state (GameManager):** `TryOpenRegularCouncilEvent` (yalniz Day 3/6/9...;
   seed = hash(ECS RandomSeed, run salt, gun)), `ChooseCouncilOption` (efekt uygulama + flag yazimi:
   otomatik `council_{template}_{a|b}` + yalniz catalog allowlist'indeki SetsFlagOnA/B),
   `ExpireCouncilEvent`. Flag'ler `Dictionary<string,int>` (flag -> setlendigi gun; zincir
   gecikmeleri icin). `ChooseCouncilOption` composed payload'i catalog template/contrast/branch
   recetesiyle yeniden dogrulamadan effect veya flag uygulamaz. Restart sifirlar.
4. **UI (`CouncilEventUI.cs`):** faz gecislerini 0.2s poll ile izler (Dawn -> scheduled open,
   Dusk -> expire); kart DOTween slide+fade ile belirir (Dawn odul toast'undan 1.2s gecikmeli),
   sure seridi + `DECIDE Ns` sayaci authoritative cycle state'inden kalan Dawn+Day penceresini
    gosterir; iki buton `CouncilOptionPresentationUtility` canli quote'unu rich text olarak basar;
    sure seridi Filled/Horizontal/Left contract'inda sayacla birlikte azalir. Secim punch +
    Card Place SFX, belirme Book Handle SFX.
5. **Ilk-kosu ogretimi (`FirstRunOnboardingUI.cs`):** Council karti gercek secime acildiginda
   `CouncilEventPanel` tam kartini non-modal pulse eder ve iki exact sonuc/bedelin okunmasini
   ister. Tek bir branch'i isaretlemez; karti acmaz, secim yapmaz, timer/pause/resource state'ine
   dokunmaz. `CouncilChoiceCommittedByPlayer` yalniz basarili gercek UI seciminden sonra yayilir.

## `CouncilRunState` Contract'i

Bu contract ayri bir ikinci state nesnesi degildir. Regular-only Council akisinin mevcut
owner'lari asagidaki tek zinciri olusturur:

| Contract alani | Tek otorite | Exact save davranisi |
|---|---|---|
| Regular handled day | `CouncilRegularSchedule` cadence + `GameManager._lastRegularCouncilDay` | `LastRegularCouncilDay` aynen saklanir; ayni gun ikinci kart acilmaz |
| Flag ve chain hafizasi | `GameManager._councilFlags` (`flag -> set day`) | `CouncilFlags` listesi aynen saklanir |
| Recent/one-shot hafiza | `_recentCouncilTemplates` + `_usedOneShotCouncils` | Sirali recent liste ve one-shot set girdileri saklanir |
| Deterministik run salt | `GameManager._councilRunSalt` | Ilk regular meeting beklenmeden ilk exact save'de non-zero commit edilir |
| Cozulmemis aktif kart | `GameManager._activeCouncilEvent` + discriminator | Payload yeniden compose edilmez; production catalog preflight'i fail-closed'dur |
| Cozulmus sureli etkiler | `MobileEconomyEventState` production/next-night multiplier + expiry alanlari | Kart yeniden acilmaz; multiplier ve expiry aynen restore edilir |

Council V1 regular-only'dir. Emergency meeting type, trigger, rarity veya ayri run-state
dali yoktur. UI, onboarding ve presentation katmanlari bu state'i okuyabilir fakat yazma
otoritesi yalniz `GameManager` transaction'lari ve mevcut ECS effect owner'larindadir.

## Regular Schedule

- Tek takvim owner'i `CouncilRegularSchedule`'dir: ilk regular gun `3`, interval `3`.
- Day `1/2/4/5...` hicbir chance roll yapmaz; pity ve cooldown regular akisa dahil degildir.
- `_lastRegularCouncilDay` ayni Dawn'da ikinci karti engeller; alan v11'de eklenmistir ve
  guncel exact save v14'te korunur.
- Compose gecersiz catalog nedeniyle null donerse scheduled gun fail-closed islenir; ayni gun
  hot-reload veya tekrar cagri ile farkli kart reroll edilmez.
- V1 Council regular-only'dir; Day `3/6/9...` disinda ikinci bir meeting type veya trigger
  yolu yoktur.
- Launch staging: Day 3 yalniz temel ekonomi (`abandoned_cache`, `merchant_caravan`,
  `quarry_crew`); Day 6 population/savunma (`refugees_at_gate`, `wandering_veterans`,
  `cold_snap`); Day 9 gece riski (`strange_bonfires`). Follow-up'lar flag + gecikme ile acilir.

## Tuning ve Telemetry Contract'i

- `Difficulty Tuner > Council Runtime Contract`, production `CouncilEventCatalogSO` asset'ini
  dogrudan duzenler. Small/Fair/Generous multiplier ve weight'leri, A/B budget tolerance ile
  `RecentTemplateMemory` `DifficultyProfileSO` icine kopyalanmaz.
- Varsayilan `0.7/1.0/1.4`, `%35/%50/%15`, `1.25` ve memory `3` degerleri paket oncesi composer
  davranisini aynen korur. Gecersiz/sirasiz multiplier, negatif/bos weight veya `<1` tolerance
  katalog validation'i ve runtime compose'u fail-closed durdurur.
- Memory degeri canli azaltildiginda `GameManager`, eski recent listeyi bir sonraki scheduled
  compose'dan once yeni limite indirir ve secilen kart eklendikten sonra ayni limiti tekrar uygular.
- Karar timer'inin ayri tuning/state owner'i yoktur. `CouncilDecisionWindowUtility`, toplam pencereyi
  active `ContinuousSiegeCycleData.DawnDuration + DayDuration` olarak turetir; kalan sure faz
  progress'inden gelir ve Dusk'ta sifirlanir.
- `GameManager.GetCouncilRuntimeTuningTelemetry`, katalog validasyonu, memory/flag/one-shot sayilari,
  active kart butceleri ve production/next-night expiry'lerini aggregate verir; yeni gameplay state'i
  kurmaz veya gizli secenek icerigi uretmez.
- Provider-bagimsiz `GameplayTelemetry`, yalniz secim effect/flag/active-clear transaction'i veya
  Dusk expire active-clear transaction'i kesinlestikten sonra `council_resolved v1` yayar. Payload
  day/template, `option_a/option_b/expired`, concrete effect snapshot'lari ve production guard'dan
  gecmis next-night count delta'sini tasir. Rejected secim, bos tekrar expire ve cozulmus karar
  sonrasi exact Continue duplicate event uretmez; Emergency Council yolu yoktur.

## Persistence

- Guncel v14 save; v11'de eklenen `LastRegularCouncilDay`, `HasActiveCouncilEvent`, active
  composed payload, flags, recent template memory, one-shot list ve run salt'i saklar.
- Yeni run salt'i ilk meeting'e kadar ertelenmez; ilk exact snapshot'tan once uretilir.
  Boylece Day 1/2 Main Menu -> Continue gelecekteki regular kart dizisini degistirmez.
- `HasActiveCouncilEvent` otoritedir; `JsonUtility` null nested class'i bos nesne yaptigi icin
  discriminator false ise payload ignore edilip null'a normalize edilir.
- v10 chance/pity state'i migrate edilirken yalniz mevcut regular gunde aktif/gecerli kart veya
  `CouncilDaysSinceEvent == 0` kaniti varsa gun handled sayilir. Chance fail'i Day 3/6/9 kartini
  sessizce yutmaz.
- Continue active Council payload'ini `CouncilContentPolicy` ile production catalog'a karsi
  preflight eder. Catalog disi template, authored flag uyusmazligi, bilinmeyen role veya option
  recetesi disi effect varsa kosu restart edilmeden restore fail-closed reddedilir.
- Cozulmus secimin otomatik/curated flag'leri ile temp-production ve next-night effect
  multiplier/expiry alanlari ayni snapshot'ta korunur. Continue cozulmus karti yeniden acmaz,
  aktif karti yeniden compose etmez ve future `3/6/9...` regular schedule'i engellemez.

## Karsitlik Receteleri (composer'in gramerleri)

| Tip | A | B |
|---|---|---|
| NowVsLater | aninda kaynak | sureli uretim bonusu (ayni kaynak) |
| ResourceTrade | en bol kaynagi ode, en kiti kazan | kucuk teselli |
| PopulationVsResource | +POP | +kaynak (kit) |
| EconomyVsDefense | FOOD ode, +bedava okcu | savunma HP iyilestir |
| SafeVsRisky | sonraki gece -%X horde | cift yagma + sonraki gece +%Y horde |
| PayOrSuffer | uretim cezasina katlan | kaynak ode, gecistir |
| DefenseVsProduction | Wall onarimi | sureli uretim bonusu |
| ResourceVsPopulation | anlik kaynak | yatak + tek seferlik Food isteyen population |

## Anlati Katmani (promise -> choice -> consequence)

- **Placeholder'li metinler:** sablon `BodyVariants[]` (2-3 varyant; composer rastgele secer) +
  `OutcomeA/OutcomeB`. Token'lar composer'da gercek sayilarla doldurulur: `{GAIN_N} {GAIN_RES}
  {PAY_N} {PAY_RES} {POP_N} {ARCHER_N} {BOOST_RES} {BOOST_PCT} {BOOST_D} {PEN_RES} {PEN_PCT}
  {PEN_D} {HEAL_PCT} {NIGHT_PCT} {CAP_RES} {CAP_N} {DAY}`. Govde her iki secenegin sayilarina
  bakabilir (once A, sonra B); outcome yalniz kendi seceneginden cozulur. Boylece hikaye teklifin
  KENDISINI anlatir ("Their wagons carry good IRON — and they know exactly how badly we need it").
- **Sonuc ani:** secimden sonra kart kapanmaz — butonlar/sure seridi gizlenir, govde 3.4s
  boyunca outcome metnine donusur ("A crew digs in at the depot. WOOD output is up 35%...").
  Expire'da genel "The moment passes..." metni 2.4s gosterilir.
- **Aktif etki rozeti (`CouncilEffectBadgeText`):** surel etki yasarken sol-ustte
  "PACT — WOOD +35% . 2d left" (yesil) / "HARDSHIP — ..." (kirmizi) / "OMEN — the horde comes
  harder tonight (+%X)" (turuncu). Her poll'da `GameManager.EconomyEvent` cache'inden hesaplanir.
- **Gece hatirlatmasi:** NIGHT'a geciste gece carpani aktifse `SiegeToastText` uzerinden toast
  ("The noise carried. They come harder tonight (+14%).") — riskli secim ile zor gece arasindaki
  nedensellik bagini kapatir.
- **Renkli buton ozetleri:** DescribeEffects TMP rich-text uretir (kazanc #8FD98A, bedel
  #E08A7A, risk #E5B963).
- **Canli exact quote:** `CouncilOptionPresentationUtility`, composer label'ini oyuncuya dogrudan
  basmaz. Authored verb'i korur; canli state'ten population icin tek seferlik Food, free Basic
  archer icin idle population, Wall icin gercek clamp edilmis HP ve count-only gece yuzdesini
  yeniden hesaplar. Kilit sebebi ayni satirda gorunur; `GameManager.CanAffordCouncilOption`
  ayni quote sonucunu kullanir.
- Testler cozulmemis token kalmadigini garanti eder (300 uretimde `{` taramasi).

## "Akillilik" Kaynaklari

- **Director:** atom bazli baglam carpanlari — kit kaynagi kayirma (ScarcityWeightMult),
  bollukta gider/risk eventleri (AbundanceWeightMult), dusuk savunmada savunma atomlari
  (LowDefenseWeightMult). Template skoru hem Option A hem Option B authored atomlarini okur;
  heal B tarafindaysa bile dusuk Wall baglami secimi etkiler. Balanced resource atomlari
  gercek stock/production dakikasindan en kit ve en bol kaynagi secer.
- **Hard recent memory:** Son `RecentTemplateMemory` template, baska uygun aday varsa havuzdan
  tamamen cikar. Butun uygun adaylar recent ise scheduled kartin bos kalmamasi icin ayni havuz
  deterministik fallback olur.
- **Curated zincir:** RequiredFlags/ForbiddenFlags/SetsFlag alanlari tek basina yetmez.
  `CouncilEventCatalogSO.CuratedChains`, source template + source branch + flag + target template
  dordulusunu explicit onaylamalidir; composer ve choice writer onaysiz zinciri fail-closed
  reddeder. Mevcut iki approved bag: `refugees_at_gate A -> refugees_taken ->
  among_the_refugees` ve `merchant_caravan A -> traded_with_merchant -> an_old_friend`.
  Source event, tetikleyen branch secilene kadar tekrar gelebilir; flag yazilinca kendi
  `ForbiddenFlags` emeklilik kontratiyla kosu havuzundan cikar. Reddetmek zinciri yakmaz.
- **Olcek:** miktarlar dakikalik uretimden turetilir; DAY 3'te de DAY 30'da da anlamli.
- **Butce:** her etkinin "dakika-degeri" var; A/B normalize edilir. Production testi 9
  template x 3 gun bandi x 200 seed = 5.400 compose sonucunu token, content policy ve
  `<= 1.25` A/B oranina karsi kilitler.

## ECS Dokunuslari

- `MobileEconomyEventState`'e `NextNightSpawnMultiplier` + `NightSpawnExpiresAfterWave`
  (risk atomu); `WaveSpawnSystem` yalniz NIGHT fazinda intensity'ye carpar.
- Sureli etkilerin expire'i continuous dalda `MobilePopulationEconomySystem.
  ExpireContinuousEventEffects` ile islenir (legacy ApplyDayPrepStart continuous'ta kosmaz).
- Temp production bonusu MEVCUT `ProductionBonus*` alanlarini kullanir — TEK aktif slot
  (yeni gelen eskisini ezer; V1 kisiti).
- Legacy WorkerCapBonus runtime destegi save/uyumluluk icin durur; `cap_bonus` launch
  template'lerinde referanslanmaz ve `CouncilContentPolicy` authored recipe olarak reddeder.
  Kalici worker capacity yalniz Wood+Iron incremental bina yatiriminin owner'idir.

## Role / Content Ownership Gate

`CouncilContentPolicy`, Council'in runtime'da sahip olabilecegi effect kind'larinin tek
allowlist'idir: run kaynak transaction'i, gecici production, population, guardli Basic archer,
Wall heal ve next-night count multiplier. WorkerCapBonus enum/runtime destegi compatibility
icin korunur fakat hicbir launch contrast recipe'si bu atomu authored secenekte kabul etmez.
`None`, bilinmeyen enum degeri ve gelecekte eklenebilecek baska domain'ler reddedilir.

- Grave Essence kazanma/harcama, Heart node reveal/purchase/upgrade/evolution ve Meta Souls/shop
  Council rolunde degildir; Council apply switch'i bu owner API'lerini cagiramaz.
- Katalog validation her template'in OptionA/B atom referansini kendi contrast recetesiyle
  eslestirir ve composer'in global dependency atomlarini kontrol eder.
- Composer catalog gate'ini gecmeyen havuzdan event uretmez. Live quote bilinmeyen effect'i
  `CONTENT BLOCKED` olarak kilitler; `ChooseCouncilOption` karti kapatmadan veya flag yazmadan
  composed event'i authored catalog'a karsi yeniden dogrular.
- Production 9 template/11 serialized atom launch review'undan gecmistir. `cap_bonus` dormant
  compatibility atomudur; 9 template'in hicbiri referanslamaz. Her template en az iki authored
  govde varyanti, staged MinDay, approved recipe, curated repeat kurali ve 5.400-ornek budget
  regression gate'i tasir.

## Effect Guardrail Owner'i

`CouncilEffectGuardUtility`, Council sonucunu ana sistemlerin mevcut sinirlarina baglayan saf
sayisal owner'dir. `GameManager.ApplyCouncilEffects` bu utility'nin sonucunu gercek ECS
transaction'ina cevirir.

- `GainPopulation`, Dawn arrival ile ayni `MobilePopulationArrivalUtility` butcesini kullanir.
  Kabul edilen kisi sayisi bos yatak ve Food ile sinirlanir; kabul edilen her kisi Food'u tam
  bir kez harcar. Council population kazanimi `PopulationState.Capacity/BaseCapacity` degerini
  buyutemez.
- `GainFreeArchers`, Basic/Rapid/Frost ortak `1000` cap'i ile gercek idle population'in minimumu
  kadar calisir ve her spawn icin bir idle kisiyi `PopulationState.Archers` havuzuna tasir.
  Free Economy Test Mode bu urun guard'ini bypass etmez.
- Kartin yazdigi exact population/archer sonucunun tamami uygulanamiyorsa
  `CanAffordCouncilOption` secenegi kilitler. Private effect uygulayiciya bozuk/stale payload
  gelirse ikinci guard sonucu guvenli bicimde clamp eder.
- `HealDefensePercent` yalniz `WallSegment` owner'ina gider; Gate/Core okunmaz veya yazilmaz.
- `NextNightSpawnDelta` yalniz `MobileEconomyEventState.NextNightSpawnMultiplier` alanini
  `0.25..2.0` araliginda yazar. Zombie HP, damage ve speed alanlarina dokunmaz.

Test owner'lari: `CouncilComposerTests` scarcity/production, iki tarafli Wall director,
hard recent exclusion/fallback, curated source/target+source-retirement contract'ini, staged
launch gunlerini, dormant cap isolation'ini ve production 5.400-sample budget/token gate'ini;
`CouncilContentPolicyTests` effect role whitelist'ini, contrast/branch recetesini, catalog
provenance'ini ve production asset gate'ini;
`CouncilEffectGuardUtilityTests` saf limitleri;
`CouncilEffectGuardPlayModeTests` gercek ECS population/Food/archer/Wall/count-only
transaction'larini ve scene timer binding'ini dogrular. `CouncilOptionPresentationUtilityTests`
exact metin, affordability, cycle countdown ve generated HUD prefab timer'ini kilitler.
`CouncilTuningContractTests` production band degerlerini ve Tuner owner baglarini;
`CouncilRegularSchedulePlayModeTests` canli memory sinirini, aggregate timer telemetry'sini ve bozuk
payload'in karar/on-state mutation yapamadigini;
`ExactRunContinuePlayModeTests` ayni payload'in Continue preflight'ta restart oncesi
reddedildigini; `CouncilRegularSchedulePlayModeTests` active payload/memory/handled-day ile
cozulmus secim, sureli effect state'i ve ilk meeting oncesi committed run salt'in exact
Continue davranisini; `GameplayTelemetryTests` ile `GameplayTelemetryPlayModeTests` ise
`council_resolved` contract/envelope guard'larini ve gercek secim/expire/Continue emission'ini
dogrular.

## Isim Sozlesmesi

`CouncilEventPanel` (+CanvasGroup), `CouncilTitleText`, `CouncilBodyText`, `CouncilTimerFill`
(Filled/Horizontal), `CouncilTimerText`, `CouncilOptionAButton` (+`CouncilOptionAText`),
`CouncilOptionBButton` (+`CouncilOptionBText`). Setup tool bulur+baglar; katalog
`GameManager.councilCatalog`'da.

Ilk-kosu binding'i ayni HUD root'ta `FirstRunOnboardingUI.Council -> CouncilEventUI` olarak
kurulur. Stable completion flag'i `tutorial.v1.council`dir; Dusk expire'i completion sayilmaz.

## Bilinen Notlar / Tuzaklar

- `IsMobileMode`/`_initialized` frame-arasi dalgalanabildiginden council kodlari
  `ContinuousSiegeCycle.Enabled` cache'ini guard olarak kullanir.
- Ayni anda tek temp-production bonusu (slot kisiti) ve tek aktif kart olur.
- Testler: `CouncilComposerTests` composer determinizmi/butce/zincir/olcekleme;
  `CouncilContentPolicyTests` role/content ownership gate'i;
  `CouncilRegularScheduleTests` exact cadence/tek-acilis/v10 migration;
  `CouncilRegularSchedulePlayModeTests` gercek `NewGameScene` Day 1-12 entegrasyonu
  ve onayli secimin curated chain flag'ini live `GameManager` state'ine yazmasini;
  `ExactRunContinuePlayModeTests` active Council save preflight'ini dogrular.
