# Council Events (Safak Meclisi) - Architecture

## Amac

Dusuk frekansli, iki secenekli mikro kararlar: kart DAWN'da belirir (dongunun en sakin ani,
konsey safakta toplanir), DAY boyunca yasar, DUSK girisinde secilmemisse "konsey dagilir".
Oyun HICBIR zaman durmaz. Event'ler ASSET DEGILDIR — CouncilComposer sablon x atom x
baglam x olcek carpiminden runtime'da uretir; ~10 atom + ~9 sablon yuzlerce ayirt edilebilir
varyant dogurur ve yeni atom/sablon eklemek cesitliligi CARPARAK buyutur.

## Katmanlar

1. **Veri (ScriptableObject/):** `CouncilEffectAtomSO` (etki parcacigi: tur + uretim-oranli
   buyukluk + director agirlik kurallari + butce degeri), `CouncilTemplateSO` (tema/metin +
   karsitlik tipi + flag kosullari + zincir alanlari), `CouncilEventCatalogSO` (havuz +
   RecentTemplateMemory 3 + explicit `CuratedChains` allowlist'i). Legacy
   DailyEventChance/PityDays/CooldownDays alanlari serialized asset uyumlulugu icin saklidir
   fakat regular schedule tarafindan kullanilmaz.
2. **Composer (`CouncilComposer.cs`, pure static):** seed + `CouncilContext` -> sablon sec
   (curated flag/gun filtreleri + hard anti-tekrar + iki secenekli director on-skoru) ->
   karsitlik recetesine gore A/B
   atomlari -> uretim-oranli miktarlar (`perMin * MinutesOfProduction * band`; band 0.7/1.0/1.4)
   -> butce dengeleme (A/B "dakika-degeri" toleransi asarsa dusuk taraf olceklenir).
   DETERMINISTIK: ayni seed + ayni context = ayni event (EditMode testli). Rng warm-up
   ardisik-seed korelasyonunu kirar.
3. **Runtime state (GameManager):** `TryOpenRegularCouncilEvent` (yalniz Day 3/6/9...;
   seed = hash(ECS RandomSeed, run salt, gun)), `ChooseCouncilOption` (efekt uygulama + flag yazimi:
   otomatik `council_{template}_{a|b}` + yalniz catalog allowlist'indeki SetsFlagOnA/B),
   `ExpireCouncilEvent`. Flag'ler `Dictionary<string,int>` (flag -> setlendigi gun; zincir
   gecikmeleri icin). Restart sifirlar.
4. **UI (`CouncilEventUI.cs`):** faz gecislerini 0.2s poll ile izler (Dawn -> scheduled open,
   Dusk -> expire); kart DOTween slide+fade ile belirir (Dawn odul toast'undan 1.2s gecikmeli),
   sure seridi + `DECIDE Ns` sayaci authoritative cycle state'inden kalan Dawn+Day penceresini
   gosterir; secim punch + Card Place SFX, belirme Book Handle SFX.

## Regular Schedule

- Tek takvim owner'i `CouncilRegularSchedule`'dir: ilk regular gun `3`, interval `3`.
- Day `1/2/4/5...` hicbir chance roll yapmaz; pity ve cooldown regular akisa dahil degildir.
- `_lastRegularCouncilDay` ayni Dawn'da ikinci karti engeller ve exact save v11'de korunur.
- Compose gecersiz catalog nedeniyle null donerse scheduled gun fail-closed islenir; ayni gun
  hot-reload veya tekrar cagri ile farkli kart reroll edilmez.
- Emergency Council bu owner'dan ayridir. Trigger/type/list owner onayi gelmeden runtime'a
  eklenmez ve ileride `_lastRegularCouncilDay` degerini degistiremez.

## Persistence

- v11 `LastRegularCouncilDay`, `HasActiveCouncilEvent`, active composed payload, flags, recent
  template memory, one-shot list ve run salt'i saklar.
- `HasActiveCouncilEvent` otoritedir; `JsonUtility` null nested class'i bos nesne yaptigi icin
  discriminator false ise payload ignore edilip null'a normalize edilir.
- v10 chance/pity state'i migrate edilirken yalniz mevcut regular gunde aktif/gecerli kart veya
  `CouncilDaysSinceEvent == 0` kaniti varsa gun handled sayilir. Chance fail'i Day 3/6/9 kartini
  sessizce yutmaz.

## Karsitlik Receteleri (composer'in gramerleri)

| Tip | A | B |
|---|---|---|
| NowVsLater | aninda kaynak | sureli uretim bonusu (ayni kaynak) |
| ResourceTrade | en bol kaynagi ode, en kiti kazan | kucuk teselli |
| PopulationVsResource | +POP | +kaynak (kit) |
| EconomyVsDefense | FOOD ode, +bedava okcu | savunma HP iyilestir |
| SafeVsRisky | sonraki gece -%X horde | cift yagma + sonraki gece +%Y horde |
| PayOrSuffer | uretim cezasina katlan | kaynak ode, gecistir |

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
- **Olcek:** miktarlar dakikalik uretimden turetilir; DAY 3'te de DAY 30'da da anlamli.
- **Butce:** her etkinin "dakika-degeri" var; A/B normalize edilir — kirik kombinasyon
  (bedava kazanc/haksiz ceza) matematiksel olarak engellenir.

## ECS Dokunuslari

- `MobileEconomyEventState`'e `NextNightSpawnMultiplier` + `NightSpawnExpiresAfterWave`
  (risk atomu); `WaveSpawnSystem` yalniz NIGHT fazinda intensity'ye carpar.
- Sureli etkilerin expire'i continuous dalda `MobilePopulationEconomySystem.
  ExpireContinuousEventEffects` ile islenir (legacy ApplyDayPrepStart continuous'ta kosmaz).
- Temp production bonusu MEVCUT `ProductionBonus*` alanlarini kullanir — TEK aktif slot
  (yeni gelen eskisini ezer; V1 kisiti).
- Worker cap bonuslari GameManager aggregate'inde tech ile birlesir (base+tech+council) —
  tech satin alimi council kazanimini ezmez.

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
hard recent exclusion/fallback, curated source/target contract ve production catalog asset'ini;
`CouncilEffectGuardUtilityTests` saf limitleri;
`CouncilEffectGuardPlayModeTests` gercek ECS population/Food/archer/Wall/count-only
transaction'larini ve scene timer binding'ini dogrular. `CouncilOptionPresentationUtilityTests`
exact metin, affordability, cycle countdown ve generated HUD prefab timer'ini kilitler.

## Isim Sozlesmesi

`CouncilEventPanel` (+CanvasGroup), `CouncilTitleText`, `CouncilBodyText`, `CouncilTimerFill`
(Filled/Horizontal), `CouncilTimerText`, `CouncilOptionAButton` (+`CouncilOptionAText`),
`CouncilOptionBButton` (+`CouncilOptionBText`). Setup tool bulur+baglar; katalog
`GameManager.councilCatalog`'da.

## Bilinen Notlar / Tuzaklar

- `IsMobileMode`/`_initialized` frame-arasi dalgalanabildiginden council kodlari
  `ContinuousSiegeCycle.Enabled` cache'ini guard olarak kullanir.
- Ayni anda tek temp-production bonusu (slot kisiti) ve tek aktif kart olur.
- Testler: `CouncilComposerTests` composer determinizmi/butce/zincir/olcekleme;
  `CouncilRegularScheduleTests` exact cadence/tek-acilis/v10 migration;
  `CouncilRegularSchedulePlayModeTests` gercek `NewGameScene` Day 1-12 entegrasyonu
  ve onayli secimin curated chain flag'ini live `GameManager` state'ine yazmasini dogrular.
