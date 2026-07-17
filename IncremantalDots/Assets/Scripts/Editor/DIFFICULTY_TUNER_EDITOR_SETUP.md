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
   - House/worker yatirim fiyatlari: `Ekonomi Fiyat Egrileri` foldout'unda bed base ve
     interval, CAP/EFF Wood+Iron base ve ortak worker building growth multiplier.
4. **Apply** — edit modda subscene'e baglar+kaydeder (bake); play modda ayrica CANLI uygular.
5. Olcum: Play'e gir -> **Run Bot** (profili uygular, temiz kosu baslatir) ->
   kosular bitince **Son Olcumu Ozetle** ile olum-gunu dagilimini oku.

## Notlar

- Egriler x=GUN (1..SampleDays), y=CARPAN; 1 = etkisiz. Gun, SampleDays'i asarsa son deger kullanilir.
- `SpawnTable` ve `SpecialNights` alanlari M-C hazirligidir — sistem henuz okumaz, veri girebilirsin.
- `DefaultDifficulty.asset` ekonomi default'lari: bed `100W / 25 interval`, CAP
  `100W+25I`, EFF `150W+50I`, ortak growth `1.35`. Apply, edit modda bunlari bake eder;
  Play Mode'da mevcut config entity'sine de canli yazar.
- Wall base HP profile-owned'dir. Profile yoksa `CastleAuthoring.WallHP` fallback olur.
  Iron uretimi gibi profil-disi baseline'lar setup tool/active SubScene Authoring'de kalir.
