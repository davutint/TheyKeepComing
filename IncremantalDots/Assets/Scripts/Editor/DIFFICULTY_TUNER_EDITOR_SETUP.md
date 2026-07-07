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
   - Gec oyun baskisi: `ZombieHpGrowthPerCycle`, `SpawnBatchGrowthPerCycle`, `MaxSpawnBatch`.
   - Kurtulus ekonomisi: `RepairBase*Cost`.
4. **Apply** — edit modda subscene'e baglar+kaydeder (bake); play modda ayrica CANLI uygular.
5. Olcum: Play'e gir -> **Run Bot** (profili uygular, temiz kosu baslatir) ->
   kosular bitince **Son Olcumu Ozetle** ile olum-gunu dagilimini oku.

## Notlar

- Egriler x=GUN (1..SampleDays), y=CARPAN; 1 = etkisiz. Gun, SampleDays'i asarsa son deger kullanilir.
- `SpawnTable` ve `SpecialNights` alanlari M-C hazirligidir — sistem henuz okumaz, veri girebilirsin.
- WallHP / Iron uretimi gibi profil-DISI degerler setup tool sabitlerinde yasar
  (setup her kosuda normalize eder) — kalici degisiklik icin MobileCastleSceneSetupWindow'daki
  degeri guncelle.
