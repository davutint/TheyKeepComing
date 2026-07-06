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
   karsitlik tipi + flag kosullari + zincir alanlari), `CouncilEventCatalogSO` (havuz + pacing:
   DailyEventChance 0.30, PityDays 4, CooldownDays 1, RecentTemplateMemory 3).
2. **Composer (`CouncilComposer.cs`, pure static):** seed + `CouncilContext` -> sablon sec
   (flag/gun filtreleri + anti-tekrar + director on-skoru) -> karsitlik recetesine gore A/B
   atomlari -> uretim-oranli miktarlar (`perMin * MinutesOfProduction * band`; band 0.7/1.0/1.4)
   -> butce dengeleme (A/B "dakika-degeri" toleransi asarsa dusuk taraf olceklenir).
   DETERMINISTIK: ayni seed + ayni context = ayni event (EditMode testli). Rng warm-up
   ardisik-seed korelasyonunu kirar.
3. **Runtime state (GameManager):** `TryRollCouncilEvent` (gunde bir; sans+pity+cooldown;
   seed = hash(ECS RandomSeed, gun)), `ChooseCouncilOption` (efekt uygulama + flag yazimi:
   otomatik `council_{template}_{a|b}` + SetsFlagOnA/B), `ExpireCouncilEvent`. Flag'ler
   `Dictionary<string,int>` (flag -> setlendigi gun; zincir gecikmeleri icin). Restart sifirlar.
4. **UI (`CouncilEventUI.cs`):** faz gecislerini 0.2s poll ile izler (Dawn -> roll,
   Dusk -> expire); kart DOTween slide+fade ile belirir (Dawn odul toast'undan 1.2s gecikmeli),
   sure seridi karar penceresini gosterir; secim punch + Card Place SFX, belirme Book Handle SFX.

## Karsitlik Receteleri (composer'in gramerleri)

| Tip | A | B |
|---|---|---|
| NowVsLater | aninda kaynak | sureli uretim bonusu (ayni kaynak) |
| ResourceTrade | en bol kaynagi ode, en kiti kazan | kucuk teselli |
| PopulationVsResource | +POP | +kaynak (kit) |
| EconomyVsDefense | FOOD ode, +bedava okcu | savunma HP iyilestir |
| SafeVsRisky | sonraki gece -%X horde | cift yagma + sonraki gece +%Y horde |
| PayOrSuffer | uretim cezasina katlan | kaynak ode, gecistir |

## "Akillilik" Kaynaklari

- **Director:** atom bazli baglam carpanlari — kit kaynagi kayirma (ScarcityWeightMult),
  bollukta gider/risk eventleri (AbundanceWeightMult), dusuk savunmada savunma atomlari
  (LowDefenseWeightMult). Oyuncu "sistem beni okuyor" hisseder.
- **Hafiza/zincir:** flag'ler + RequiredFlags/ForbiddenFlags/ChainDelayDays/OneShot —
  `refugees_at_gate`'te A secimi 2 gun sonra `among_the_refugees`'i acar;
  `merchant_caravan` takasi `an_old_friend`'i tohumlar.
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

## Isim Sozlesmesi

`CouncilEventPanel` (+CanvasGroup), `CouncilTitleText`, `CouncilBodyText`, `CouncilTimerFill`
(Filled/Horizontal), `CouncilOptionAButton` (+`CouncilOptionAText`), `CouncilOptionBButton`
(+`CouncilOptionBText`). Setup tool bulur+baglar; katalog `GameManager.councilCatalog`'da.

## Bilinen Notlar / Tuzaklar

- `IsMobileMode`/`_initialized` frame-arasi dalgalanabildiginden council kodlari
  `ContinuousSiegeCycle.Enabled` cache'ini guard olarak kullanir.
- Ayni anda tek temp-production bonusu (slot kisiti) ve tek aktif kart olur.
- Testler: `Assets/Tests/EditMode/CouncilComposerTests.cs` (determinizm, 500-seed butce,
  zincir filtreleri, uretim olcekleme) — 6/6.
