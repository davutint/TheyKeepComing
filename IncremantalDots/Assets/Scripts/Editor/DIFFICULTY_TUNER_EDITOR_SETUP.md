# Difficulty Tuner - Editor Setup

## Kurulum

`Mobile Castle Scene Setup` calistiginda `Assets/ScriptableObject/MobileCastle/Difficulty/
DefaultDifficulty.asset` seed edilir (varsa DOKUNULMAZ) ve subscene'deki
`MobileCastleCombatAuthoring.Profile` BOSSA baglanir (owner atamasi korunur).

## Kullanim

1. `Window > DeadWalls > Difficulty Tuner` ac.
2. Profil otomatik yuklenir (yoksa "Default Profili Olustur/Bul").
3. Egrileri/degerleri panelden duzenle:
   - Erken oyun sertligi: `NightIntensityByDay` egrisi (dusuk baslat, kac gunde 1.0'a
     cikacagini keyframe'lerle belirle).
   - Gec oyun baskisi: `SpawnBatchGrowthPerCycle`, `MaxSpawnBatch`, `MaxAliveZombies`.
   - Wall Runtime Contract: `WallBaseHp`, normal heal paketi, Stone/HP, Day fiyat carpani,
     Emergency heal yuzdesi ve cooldown.
   - Worker economy: `Economy Runtime Contract` foldout'unda dort kisi basi production
     baseline'i, CAP/EFF Wood+Iron base cost, ortak growth ve EFF seviye yuzdesi.
   - Population: `Population Runtime Contract` foldout'unda Dawn request, Food/arrival,
     House bed base Wood ve owned-bed growth interval'i. Preview alanina population,
     purchased beds ve Food girerek bir sonraki kabul/harcama/+1/+10 bed fiyatini gor.
   - Archers: `Archer Runtime Contract` foldout'unda aktif catalog definition'larinin base
     combat, buy/retrain ve count-growth alanlari; finite Arrow capacity/refill/verim/CAP-EFF
     tuning'i. Preview count ile buy/retrain quote'unu, Play Mode'da effective stat/DPS ve
     gercek pool-rent Arrow/s drain'ini gor.
   - Meta: `Meta Runtime Contract` foldout'unda diminishing kill bandlari, day/night/peak-pop/
     record agirliklari ve exact 11 permanent upgrade'in base cost, exponential growth, cap ve
     effect degerleri. Reward/level preview ile live aggregate ayni runtime formulunu kullanir.
4. **Apply** — edit modda subscene'e baglar+kaydeder (bake); play modda ayrica CANLI uygular.
5. Olcum: Play'e gir -> **Run Bot** (profili uygular, temiz kosu baslatir) ->
   kosular bitince **Son Olcumu Ozetle** ile olum-gunu dagilimini oku.

## Notlar

- Egriler x=GUN (1..SampleDays), y=CARPAN; 1 = etkisiz. Gun, SampleDays'i asarsa son deger kullanilir.
- `SpawnTable` ve `SpecialNights` alanlari M-C hazirligidir — sistem henuz okumaz, veri girebilirsin.
- `DefaultDifficulty.asset` ekonomi default'lari: bed `100W / 25 interval`, CAP
  `100W+25I`, EFF `150W+50I`, ortak growth `1.35`; worker base rate'leri
  `8 / 5.5 / 4.9 / 7`, EFF etkisi seviye basina additive `%10`. Apply, edit modda bunlari
  bake eder; Play Mode'da config ve aggregate katmanlarina canli yazar.
- Population default'lari Dawn request `15`, Food/arrival `1` ve SubScene initial bed `60`tir.
  Request/Food profile-owned, initial bed authoring-owned'dir. Play Mode Apply mevcut purchased
  bed state'ini silmeden bir sonraki Dawn butcesini ve yatak fiyat egrisini canli degistirir.
- Archer definition default'lari Basic `10 damage / 1.5 fire rate / 15 range`, Rapid
  `6 / 3 / 14`, Frost `5 / 1.2 / 14`; type-count growth `25 / exponent 2`dir. Bunlar profile'a
  kopyalanmaz. Finite Arrow default'u `200` capacity, `100` package, `4 Arrow/Wood` ve her
  basarili projectile rent'i icin read-only `1 Arrow`dur. Play Mode Apply count, population,
  formation ve fire timer'i koruyarak mevcut okculari yeni definition baseline'ina rebase eder.
- Wall base HP profile-owned'dir. Profile yoksa `CastleAuthoring.WallHP` fallback olur.
  Worker base rate'leri de profile-owned'dir; profile yoksa SubScene Authoring fallback olur.
