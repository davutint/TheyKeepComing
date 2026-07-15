# Council Events - Editor Setup

## Otomatik Kurulum

`Window > DeadWalls > Mobile Castle Scene Setup > Setup NewGameScene`:

1. `Assets/ScriptableObject/MobileCastle/Council/` altina 11 atom (`Atom_*.asset`) +
   9 sablon (`Template_*.asset`) + `CouncilEventCatalog.asset` seed edilir — MERGE-ONLY:
   mevcut asset degerlerine ve kullanicinin ekledigi atom/sablonlara dokunulmaz. Mevcut iki
   authored follow-up, `CuratedChains` allowlist'ine merge edilir.
2. Katalog `GameManager.councilCatalog` alanina baglanir.
3. `CouncilEventUI` HUD root'a eklenir; kart objeleri isimle bulunur, SFX baglanir
   (Appear = `Book Handle 1-2.wav`, Choose = `Card Place 1-1.wav`); `CouncilEffectBadgeText`
   (aktif etki rozeti) + `NightToastText` (= `SiegeToastText`) + `CouncilTimerText` baglanir.
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
  Regular takvim asset-tunable degildir: `CouncilRegularSchedule` sabit Day `3,6,9,12...`
  owner'idir.
- Legacy `DailyEventChance/PityDays/CooldownDays` serialized uyumluluk icin saklanir ve
  Inspector'da gizlidir; regular Council'i etkilemez.

## Test Adimlari

1. Play'e gir; Day 1 ve Day 2 Dawn'da kart acilmadigini, Day 3 Dawn'da kartin kesin
   acildigini kontrol et. Ayni duzen Day 6/9/12'de devam eder.
2. Kart sol-alt bolgede belirir (odul toast'undan ~1.2s sonra); sure seridi ve `DECIDE Ns`
   sayaci kalan Dawn+Day penceresini gosterir; DUSK girisinde secilmediyse kaybolur.
3. Iki butonun ikinci satirinda exact sonuc gorunur: population `+N PEOPLE -M FOOD`, free
   archer `+N BASIC ARCHERS -N IDLE PEOPLE`, Wall heal gercek uygulanacak HP ve gece etkisi
   exact count yuzdesi. Karsilanamayan exact sonuc butonu pasif yapar ve eksigi yazar.
4. `refugees_at_gate`'te A sec -> catalog'daki exact curated link sayesinde 2+ gun sonra
   `AMONG THE REFUGEES` zinciri cikabilir (OneShot). Link allowlist'ten cikarilirsa context'te
   flag bulunsa bile target fail-closed acilmaz.
5. EditMode: `CouncilComposerTests`, `CouncilRegularScheduleTests`, `RunPersistenceTests`.
6. PlayMode: `CouncilRegularSchedulePlayModeTests` gercek sahnede Day 1-12 cadence ve
   ayni scheduled gunde ikinci acilisi dogrular. `CouncilEffectGuardPlayModeTests` exact quote
   ile `CouncilTimerText` scene binding'ini de dogrular.

## Exact Karar UI Onarimi

`Window > DeadWalls > Repair Council Exact Decision UI`, generated HUD prefabina idempotent
`CouncilTimerText` ekler, title alanini timer ile cakismayacak sekilde daraltir ve aktif
`NewGameScene` icindeki `CouncilEventUI` binding'ini kaydeder. Tam scene setup da ayni prefab
repair adimini otomatik cagirir.

## Context / Curated Chain Onarimi

`Window > DeadWalls > Repair Council Curated Context Contract`, production catalog'a yalniz
mevcut iki authored link'i merge eder: Refugees A -> Among the Refugees ve Merchant A -> An Old
Friend. Var olan custom chain girdilerini silmez; yeni chain veya anlati uretmez. Islem sonunda
`ValidateCatalog()` calisir ve kopuk/onaysiz source/flag/target baglantilarini Console'a yazar.

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
