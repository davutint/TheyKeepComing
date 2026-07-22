# Council Events - Editor Setup

## Otomatik Kurulum

`Window > DeadWalls > Mobile Castle Scene Setup > Setup NewGameScene`:

1. `Assets/ScriptableObject/MobileCastle/Council/` altina 11 serialized atom
   (`10 launch + cap_bonus legacy dormant`) + 9 launch sablonu + `CouncilEventCatalog.asset`
   seed edilir — MERGE-ONLY:
   mevcut asset degerlerine ve kullanicinin ekledigi atom/sablonlara dokunulmaz. Mevcut iki
   authored follow-up, `CuratedChains` allowlist'ine merge edilir.
2. Katalog `GameManager.councilCatalog` alanina baglanir.
3. `CouncilEventUI` HUD root'a eklenir; kart objeleri isimle bulunur, SFX baglanir
   (Appear = `Book Handle 1-2.wav`, Choose = `Card Place 1-1.wav`); `CouncilEffectBadgeText`
   (aktif etki rozeti) + `NightToastText` (= `SiegeToastText`) + `CouncilTimerText` baglanir.
   Ayni root'taki `FirstRunOnboardingUI.Council` referansi da bu `CouncilEventUI` owner'ina
   baglanir; ilk regular kartin exact karar ogretimi ayri bir kart/popup uretmez.
4. `ValidateCatalog()` sorunlari Console'a warning basilir.
5. **Metin migration:** mevcut Template asset'inde `BodyVariants` BOS ise seed'in guncel
   anlatilari (Title/BodyVariants/OutcomeA-B/Verb'ler) uygulanir. Kullanici metin girdiyse
   (BodyVariants dolu) dokunulmaz; mekanik alanlara hicbir kosulda dokunulmaz.

## Yeni Icerik Ekleme (kod GEREKMEZ)

- **Yeni atom:** Create > DeadWalls > Mobile Castle > Council Effect Atom. Id benzersiz;
  buyuklugu MinutesOfProduction (kaynak) veya Rate/PerDay (adet/oran) ile ver; BudgetMinutes
  dakika-degeri; director carpanlarini ayarla. Kataloga ekle. TEK atom onlarca varyant dogurur.
- **Yeni sablon:** Create > ... > Council Template. Karsitlik tipini sec; OptionA/BAtomIds ile
  atom kisitla (bos = tur-uyumlu havuz). Zincir icin RequiredFlags + ChainDelayDays (+OneShot)
  yetmez: catalog `CuratedChains` listesine source template/branch/flag/target dordulusunu da
  ekle. `ValidateCatalog()` onaysiz veya kopuk zinciri reddeder.
- `RecentTemplateMemory` katalog asset'inde hard anti-tekrar hafizasidir. Alternatif uygun
  template varken recent template secilemez; tum adaylar recent ise scheduled fallback acilir.
  Memory canli azaltildiginda eski recent liste bir sonraki scheduled compose'dan once yeni sinira
  iner.
- `Window > DeadWalls > Difficulty Tuner > Council Runtime Contract`, production katalogdaki
  Small/Fair/Generous multiplier ve weight'leri, A/B budget tolerance ile recent memory'yi dogrudan
  duzenler. Varsayilan `0.7/1.0/1.4`, `%35/%50/%15`, `1.25`, memory `3` mevcut davranisi korur.
  Weight toplam sifir, multiplier sirasi gecersiz veya tolerance `<1` olursa catalog fail-closed'dur.
  Regular takvim asset-tunable degildir: `CouncilRegularSchedule` her gun Dawn'da tam bir kez
  acilan regular kartin owner'idir.
- Day 1 temel ekonomi sablonlariyla baslar. Diger sablonlarin mevcut `MinDay`, flag ve chain
  kosullari korunur; butun karmasik event'leri ilk Council'a yigmak production testini bozar.
- Legacy `DailyEventChance/PityDays/CooldownDays` serialized uyumluluk icin saklanir ve
  Inspector'da gizlidir; regular Council'i etkilemez.
- `ValidateCatalog`, yalniz Id/ref kontrolu yapmaz: atom kind'inin Council-owned allowlist'te
  olmasini, explicit OptionA/B atomunun template contrast recetesine uymasini ve composer'in
  global dependency atomlarini da zorunlu tutar. Gate'i gecmeyen katalog runtime'da compose edilmez.

## Test Adimlari

1. Play'e gir; Day 1 Dawn'dan baslayarak her gun kartin kesin acildigini ve ayni Dawn'da ikinci
   kez acilmadigini kontrol et.
2. Kart sol-alt bolgede belirir (odul toast'undan ~1.2s sonra); sure seridi ve `DECIDE Ns`
   sayaci kalan Dawn+Day penceresini gosterir; DUSK girisinde secilmediyse kaybolur.
   Tuner'da decision window read-only olarak `SiegeDawnDuration + SiegeDayDuration` gorunmelidir;
   production baseline `5 + 30 = 35s`'dir ve ayri Council timer alani yoktur.
3. Iki butonun ikinci satirinda exact sonuc gorunur: population `+N PEOPLE -M FOOD`, free
   archer `+N BASIC ARCHERS -N IDLE PEOPLE`, Wall heal gercek uygulanacak HP ve gece etkisi
   exact count yuzdesi. Karsilanamayan exact sonuc butonu pasif yapar ve eksigi yazar.
   Ilk incomplete Council tutorial'inda pulse iki butondan birini degil tum `CouncilEventPanel`
   rect'ini kapsar; `COMPARE BOTH EXACT OUTCOMES AND THEIR COSTS.` hint'i gorunur ve oyun durmaz.
4. `refugees_at_gate`'te A sec -> catalog'daki exact curated link sayesinde 2+ gun sonra
   `AMONG THE REFUGEES` zinciri cikabilir (OneShot). Link allowlist'ten cikarilirsa context'te
   flag bulunsa bile target fail-closed acilmaz. A secildikten sonra source event kosu icin
   emekli olur; B secimi source'u yakmaz ve ileride tekrar teklif edilebilir.
5. EditMode: `CouncilComposerTests`, `CouncilContentPolicyTests`, `CouncilRegularScheduleTests`,
   `CouncilOptionPresentationUtilityTests`, `CouncilTuningContractTests`, `RunPersistenceTests`.
6. PlayMode: `CouncilRegularSchedulePlayModeTests` gercek sahnede Day 1-12 cadence,
   onayli flag yazimi ve bozuk role payload'inin fail-closed karar kilidini;
   `ExactRunContinuePlayModeTests` bozuk active Council payload'inin restart oncesi Continue
   preflight'ta reddini;
   ayni scheduled gunde ikinci acilisi; ilk Council full-card pulse/live quote/player-choice flag
   kontratini dogrular. `CouncilEffectGuardPlayModeTests` exact quote
   ile `CouncilTimerText` scene binding'ini de dogrular.

## Exact Karar UI Onarimi

`Window > DeadWalls > Repair Council Exact Decision UI`, generated HUD prefabina idempotent
`CouncilTimerText` ekler, title alanini timer ile cakismayacak sekilde daraltir,
`CouncilTimerFill` Image'ini Filled/Horizontal/Left olarak normalize eder ve aktif
`NewGameScene` icindeki `CouncilEventUI` binding'ini kaydeder. Tam scene setup da ayni prefab
repair adimini otomatik cagirir.

## Context / Curated Chain Onarimi

`Window > DeadWalls > Repair Council Curated Context Contract`, production catalog'a yalniz
mevcut iki authored link'i merge eder: Refugees A -> Among the Refugees ve Merchant A -> An Old
Friend. Var olan custom chain girdilerini silmez; yeni chain veya anlati uretmez. Islem sonunda
`ValidateCatalog()` calisir ve kopuk/onaysiz source/flag/target baglantilarini Console'a yazar.

## Role / Content Ownership Gate

- Yeni atomun `Kind` degeri yalniz `CouncilContentPolicy` allowlist'indeki run-decision
  domain'lerinden biri olabilir. Heart currency/node/upgrade veya Meta progression Council
  content'i olarak eklenemez.
- Template OptionA/B atom Id'leri secilen `CouncilContrastType` recetesiyle uyusmalidir;
  aksi halde `ValidateCatalog()` acik hata verir ve runtime compose fail-closed kalir.
- Production 9 template launch-approved'dur: her biri en az iki authored body varyanti,
  staged `MinDay`, approved contrast recipe ve `<=1.25` budget gate'i tasir. `cap_bonus`
  serialized compatibility atomu katalogda kalir ancak hicbir template referanslayamaz;
  kalici worker capacity yalniz Wood+Iron bina yatirimindan gelir.
- `CouncilComposerTests.ProductionCatalog_LaunchTemplateButceleriVeTokenlariOnayliSinirdaKalir`
  9 template x 3 day band x 200 seed = 5.400 compose sonucunu test eder.

## Effect Guardrail Testi

- Population karti exact miktar icin yeterli bos yatak + Food yoksa pasif olmalidir.
- Uygulanan her Council population kazanimi kabul edilen kisi basina Food'u yalniz bir kez
  harcamali; yatak capacity'sini buyutmemelidir.
- Ucretsiz okcu karti exact miktar icin yeterli idle population + ortak `1000` cap yoksa pasif
  olmali; her gelen okcu bir idle kisiyi kullanmalidir.
- `CouncilEffectGuardPlayModeTests`, Wall heal sirasinda legacy Gate/Core degerlerinin
  degismedigini ve gece etkisinin zombie HP/damage/speed yerine yalniz count multiplier
  yazdigini gercek `NewGameScene` ECS state'inde dogrular.

## Dikkat

- Kart objeleri PREFABDADIR (`CouncilEventPanel`) — sahne-override degil. Prefab TEK dogruluk
  kaynagidir (eski UIImporter/export pipeline'i 2026-07-06'da kaldirildi).
- Test/debug icin `CouncilEventUI.enabled` veya `freeEconomyTestMode` degistirilirse
  EDIT MODDA yapilan degisiklik sahneye KALICI yazilir — geri almayi unutma (bir kez yasandi).
