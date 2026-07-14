# Difficulty Tuner - Mimari

## Amac

Zorlugu koddan cikarip VERIYE tasimak: `DifficultyProfileSO` zorlugun tek dogruluk kaynagi,
`Difficulty Tuner` penceresi ise ayar+olcum dongusunun tek paneli. Akis:
degeri/egriyi degistir -> Apply -> Run Bot -> olum-gunu dagilimina bak -> tekrar.
Balance isi tahmin degil deney olur (M-A dogrulamasi: olum bandi DAY 2-3'ten 6+'ya
tek profil iterasyonuyla, KOD YAZMADAN tasindi).

## Katmanlar

1. **`DifficultyProfileSO`** (ScriptableObject/MobileCastle/Difficulty/):
   - GUN EGRILERI (AnimationCurve, x=gun, y=carpan, 1=etkisiz): `NightIntensityByDay`
     (erken oyun rampi burada), `ZombieHpMultByDay`, `SpawnBatchMultByDay`; `SampleDays` (60).
   - SKALERLER (config'e yazilir): kutle eskalasyonu (BaseHP/HpGrowth/Damage/Batch/
     MaxSpawnBatch/MaxAlive/interval'lar), faz intensity'leri, repair maliyetleri.
   - EKONOMI FIYAT EGRILERI: House bed base/interval, worker CAP ve EFF icin ayri
     Wood/Iron base maliyetleri, iki bina yatirimi icin ortak growth multiplier.
   - M-C HAZIRLIK ISKELETI (sistem henuz okumuyor, veri hazir): `SpawnTable`
     (gun -> dusman tipi agirliklari) + `SpecialNights` (her N gunde ozel gece).
2. **ECS tasima — `DifficultyDaySample` buffer'i:** AnimationCurve Burst'e giremez;
   baker egrileri gun basina ornekleyip config entity'sindeki buffer'a yazar.
   Sozlesme: index = gun-1; gun uzunlugu asarsa SON eleman; buffer yok/bos = 1 (geriye uyumlu).
3. **Baker (MobileCastleCombatAuthoring):** `Profile` alani doluysa zorluk skalerlerinde
   profil KAZANIR (bos = authoring degerleri, geriye uyumlu) + egri ornekleme. `DependsOn(profile)`
   ile SO degisince re-bake.
4. **Sistem tuketicileri:**
   - `ContinuousSiegeCycleSystem`: Night (ve Dusk-END lerp hedefi) intensity'sine gunun
     `NightIntensityMult`'u; `ConfigureMobileWave`'e gunun `ZombieHpMult`'u gecirilir.
   - `MobileWaveUtility.ConfigureMobileWave(ref wave, config, dayHpMult=1)`: HP'ye ek carpan.
   - `WaveSpawnSystem` (continuous): batch'e gunun `SpawnBatchMult`'u.
5. **`DifficultyTunerWindow`** (Window > DeadWalls > Difficulty Tuner):
   - Profil sec/inline duzenle (CurveField'lar dahil), Default olustur/bul.
   - Ekonomi Fiyat Egrileri foldout'u bed, worker bina ve finite Arrow refill/CAP/EFF
     alanlarini duzenler; Play Mode Apply baked `MobileEconomyPriceTuning` component'ini
     canli gunceller.
   - **Apply**: subscene authoring'e bagla (bake yolu) + play moddaysa CANLI uygula
     (config alanlari SetComponentData + buffer yeniden ornekleme).
   - **Run Bot**: profili canli uygular, RestartGame + Long Run Simulator'u baslatir
     (OpenAndStart koprusu). **Son Olcumu Ozetle**: en yeni CSV'den olum gunleri +
     ortalama + ulasilan en yuksek gun.

## Dogrulama (2026-07-07)

- Eski degerler: olumler [2,2,2,3,2,2,3,2] (~%90 DAY 2-3) veya olumsuz plato.
- Default profil (ramp d1 0.5 -> d7 1.0, HpGrowth 0.40, BatchGrowth 0.15, MaxBatch 16,
  RepairStone 50, Wall 350, Iron uretim +%30): olumler [6], ikinci kosu DAY 20'ye
  SUREKLI MUCADELEYLE ulasti (DAY 13 duvar %1, DAY 18 duvar+kapi dustu core %96,
  47 repair, canli zombi 892 — MaxAlive tavaninin dibi; FPS 203-257 saglikli).
- Kalan ust-uc isi M-C'nin (zombi tipleri + ozel geceler); kaynak birikimi M-B
  meta para birimi + food sink firsati olarak MASTER_PLAN'da islendi.

## Tuzaklar

- Setup tool'un normalize bolumu bazi authoring degerlerini hardcoded yazar (WallHP,
  IronWorkerProductionPerMin...) — kalici deger degisikligi SETUP SABITINDE yapilmali,
  yoksa sonraki SetupScene kosusu ezer (bir kez yasandi).
- Canli uygulama restart sonrasi config'i bake degerlerine dondurur; Tuner'in Run Bot'u
  bu yuzden restart'tan SONRA da ApplyProfileLive cagirir.
- Fiyat alanlari sifir/negatif veya gecersiz girilirse resolver int-guvenli minimumlara
  sanitize eder; runtime UI kendi ayri fiyat formulu tutmaz.
