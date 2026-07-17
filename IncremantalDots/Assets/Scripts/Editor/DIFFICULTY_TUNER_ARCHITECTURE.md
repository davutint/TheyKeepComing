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
   - WORKER ECONOMY CONTRACT: Wood/Stone/Iron/Food kisi basi production baseline'lari,
     worker CAP/EFF icin ayri Wood/Iron base maliyetleri, ortak growth multiplier ve
     profile-driven additive EFF yuzdesi.
   - KOMSU FIYAT VERILERI: House bed ve finite Arrow alanlari ayni asset'te kalir;
     tracker'daki Population/Archer audit'leri bunlari kendi runtime yuzeylerinde kapatir.
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
   - `MobileWaveUtility.ConfigureMobileWave(ref wave, config)`: V1 quantity-only; enemy HP day
     curve/growth okumaz, base stat aktif `EnemyDefinitionSO`/catalog'dan gelir.
   - `WaveSpawnSystem` (continuous): batch'e gunun `SpawnBatchMult`'u.
5. **`DifficultyTunerWindow`** (Window > DeadWalls > Difficulty Tuner):
   - Profil sec/inline duzenle (CurveField'lar dahil), Default olustur/bul.
   - **Economy Runtime Contract** foldout'u worker base rate, CAP cost, EFF cost/growth ve
     EFF effect yuzdesini tek yuzeyde duzenler. Preview ayni runtime utility ile bir sonraki
     maliyet ve birikmis etkiyi hesaplar; Play Mode telemetry dort kaynak icin worker/cap,
     base/effective/total production, seviye ve sonraki fiyatlari canli gosterir.
   - Play Mode Apply baked `MobileEconomyPriceTuning` component'ini gunceller ve
     `GameManager.ApplyWorkerEconomyTuning` ile tech/meta/Heart/bina aggregate'lerini
     yeni production baseline'i uzerine yeniden fold eder.
   - **Apply**: subscene authoring'e bagla (bake yolu) + play moddaysa CANLI uygula
     (config alanlari SetComponentData + buffer yeniden ornekleme).
   - **Run Bot**: profili canli uygular, RestartGame + Long Run Simulator'u baslatir
     (OpenAndStart koprusu). **Son Olcumu Ozetle**: en yeni CSV'den olum gunleri +
     ortalama + ulasilan en yuksek gun.
   - **Spawn Runtime Contract**: secilen gun icin BaseSpawn quantity ve Night/Dusk-end
     day-curve carpanlarini; profile-owned phase, MaxSpawnBatch ve MaxAlive baseline'larini
     tek panelde ozetler. Play Mode'da phase/day, alive/cap, exact Pending backlog,
     last/total demand-spawn ve effective interval telemetrisini canli gosterir.
     Backlog policy ayarlanabilir enum degildir; `PreserveDemand` read-only contract olarak
     aciklanir, yalniz drain hizi `MaxSpawnBatch` ve saha tavani `MaxAliveZombies` tune edilir.
   - **Wall Runtime Contract**: profile-owned base HP, normal repair heal paketi,
     Stone/HP, Day fiyat carpani ve Emergency heal/cooldown alanlarini tek panelde duzenler.
     Baseline package preview gameplay ile ayni `SingleWallDefenseRules` formulunu kullanir.
     Play Mode'da config baseline/effective MaxHP, current HP, gercek Stone quote ve phase
     gate gorunur; live Apply health ratio'yu koruyup tech/meta/Heart aggregate'lerini yeniden
     fold eder. Legacy RepairBase Wood/Stone alanlari active panelde gosterilmez.

## Dogrulama (2026-07-07)

- Eski degerler: olumler [2,2,2,3,2,2,3,2] (~%90 DAY 2-3) veya olumsuz plato.
- Default profil (ramp d1 0.5 -> d7 1.0, HpGrowth 0.40, BatchGrowth 0.15, MaxBatch 16,
  RepairStone 50, Wall 350, Iron uretim +%30): olumler [6], ikinci kosu DAY 20'ye
  SUREKLI MUCADELEYLE ulasti (DAY 13 duvar %1, DAY 18 duvar+kapi dustu core %96,
  47 repair, canli zombi 892 — MaxAlive tavaninin dibi; FPS 203-257 saglikli).
- Kalan ust-uc isi M-C'nin (zombi tipleri + ozel geceler); kaynak birikimi M-B
  meta para birimi + food sink firsati olarak MASTER_PLAN'da islendi.

## Tuzaklar

- Profile yoksa setup tool'un `CastleAuthoring.WallHP` degeri fallback'tir. Aktif profile
  varken Wall base HP Difficulty Tuner'dan gelir. Worker production baseline'lari da profile
  aittir; profile yoksa `MobileCastleCombatAuthoring` alanlari fallback'tir. Worker cap,
  population ve cycle baseline'lari halen authoring sahibindedir.
- Canli uygulama restart sonrasi config'i bake degerlerine dondurur; Tuner'in Run Bot'u
  bu yuzden restart'tan SONRA da ApplyProfileLive cagirir.
- Fiyat alanlari sifir/negatif veya gecersiz girilirse resolver int-guvenli minimumlara,
  gecersiz EFF yuzdesi onayli `%10` default'una sanitize edilir; runtime UI kendi ayri fiyat
  veya effect formulu tutmaz.
