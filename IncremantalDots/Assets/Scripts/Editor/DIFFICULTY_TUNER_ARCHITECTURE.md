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
   - POPULATION CONTRACT: Dawn basina istenen survivor, kabul edilen kisi basi tek seferlik
     Food ve House bed quadratic fiyat egrisi.
   - FINITE ARROW CONTRACT: capacity/refill/verim ve CAP/EFF yatirim fiyatlari profile-owned
     `MobileEconomyPriceTuning` baseline'ina bake edilir. Projectile basina tuketim tune edilmez;
     `ArcherShootSystem` basarili pool rent'inden sonra sabit `1 Arrow` harcar.
   - ARCHER DEFINITION CONTRACT: combat, buy/retrain base maliyeti, population cost ve
     target-type growth tuning'i profile'a kopyalanmaz; aktif `ArcherDefinitionSO` asset'lerinde kalir.
   - HEART RUNTIME CONTRACT: graph depth/cross-link/Keystone/rarity agirliklari scene'deki canonical
     `GameManager.heartGraphSettings` sahibinde; node cost/growth/rarity/depth ise production
     `HeartNodeDefinitionSO` asset'lerinde kalir. DifficultyProfile'a kopyalanmaz. Essence drop
     miktari/cadence'i Blueprint'te kesinlesmedigi ve production grant caller'i olmadigi icin panel
     bunu acik owner gate olarak gosterir; sahte per-kill deger uretmez.
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
   - **Population Runtime Contract** foldout'u Dawn request, Food/arrival ve House bed egrisini
     tek yerde duzenler. Preview ve Play Mode telemetry gameplay ile ayni
     `MobilePopulationArrivalUtility` ve `MobileBedCapacityUtility` hesaplarini kullanir;
     live Apply config/tuning baseline'larini degistirir ama run bed state'ini sifirlamaz.
   - **Archer Runtime Contract** foldout'u aktif `GameManager.ArcherCatalog` definition'larinin
     base combat, buy/retrain ve type-count growth alanlarini dogrudan duzenler. Preview ayni
     `ArcherRecruitmentCostUtility` ile quote uretir. Finite Arrow profile alanlari, paket/Buy Max,
     yatirim quote'lari ve read-only `1 Arrow / successful rent` kuralinin ayni yuzeyindedir.
     Play Mode telemetry effective ECS stat/count/DPS, teorik shot ceiling, olculen pool-rent
     Arrow/s, stok/capacity/verim ve yatirim fiyatlarini gosterir. Live Apply mevcut archer state'ini
     koruyup combat statlarini ayni Heart/Tech/Meta aggregate'leriyle yeni baseline'a yeniden fold eder.
   - **Heart Runtime Contract** foldout'u canonical `GameManager`/catalog owner'ini, run-only wallet
     ve grant/spend kapilarini tek yerde gosterir. Graph settings dogrudan future-run generator
     girdilerini; production catalog varsa definition cost/growth/rarity/depth alanlarini dogrudan
     duzenler. +1/+10/Buy Max preview'u `HeartPurchasePricing`, seed preview'u ise gercek
     `HeartGraphGenerator` + validator kullanir. Aktif exact graph reroll edilmez. Play Mode telemetry
     hidden node Id'lerini acmadan bakiye/meta remainder, graph/catalog version, seed ve aggregate
     node/edge/reveal/purchase/lock sayilarini gosterir. Catalog null veya Essence drop source yoksa
     legacy fallback yerine acik owner gate verir.
   - **Council Runtime Contract** foldout'u canonical `GameManager` ile production
     `CouncilEventCatalogSO` owner'ini birlestirir. Small/Fair/Generous multiplier ve weight'leri,
     A/B budget tolerance ile `RecentTemplateMemory` dogrudan katalog asset'inde duzenlenir;
     `DifficultyProfileSO` kopyasi yaratmaz. Day `3/6/9...` regular cadence read-only ve sabittir;
     Emergency Council yoktur. Decision timer ayri bir alan degil, active cycle'in Dawn+Day
     surelerinden turetilen read-only kontrattir. Play Mode aggregate telemetry handled day,
     recent/flag/one-shot sayilari, active card butceleri ve sureli effect/expiry state'ini gosterir.
   - **Meta Runtime Contract** foldout'u production `MetaUpgradeCatalogSO` icindeki diminishing
     kill bandlari, day/night/peak-pop/record agirliklari ve 11 permanent definition'in exact
     cost/growth/cap/effect alanlarini dogrudan duzenler. Reward preview ayni
     `MetaRewardCalculator`, upgrade preview ayni `GetCost/GetTotalEffect` formulunu kullanir.
     Play Mode aggregate telemetry current death quote breakdown, Souls/lifetime state ve
     applied Wall/production/Arrow/Essence katkilarini gosterir. Death receipt v2 quote'u
     sabitledigi icin pending recovery sonradan degisen tuning'i yeniden okumaz.
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
  aittir. Dawn request ve Food/arrival da profile-owned'dir; ayni isimli authoring alanlari
  yalniz profile yokken fallback'tir. Initial bed, worker cap ve cycle baseline'lari authoring
  sahibinde kalir.
- Archer definition asset'leri DifficultyProfile'in alt kopyasi degildir. Aktif catalog owner'i
  belirler; Tuner bu asset'leri dogrudan duzenler. Apply cost state'i veya count'u sifirlamaz.
- Heart graph settings ve definition asset'leri de DifficultyProfile alt kopyasi degildir. Settings
  degisikligi yalniz sonraki yeni run generation'ina gider; aktif/Continue exact graph asla reroll
  edilmez. Production Heart catalog ve Essence drop sayilari owner onayi olmadan olusturulmaz.
- Council effect band/memory ayarlari production Council catalog'a aittir. Takvim veya timer icin
  profile/scene icinde ikinci alan acilmaz; karar suresi mevcut cycle Dawn+Day owner'indan turetilir.
- Meta reward ve permanent upgrade ayarlari production Meta catalog'a aittir. Bunlari
  DifficultyProfile'a kopyalama; current/pending death sonucunu asset degisikligiyle geriye donuk
  yeniden hesaplama.
- Canli uygulama restart sonrasi config'i bake degerlerine dondurur; Tuner'in Run Bot'u
  bu yuzden restart'tan SONRA da ApplyProfileLive cagirir.
- Fiyat alanlari sifir/negatif veya gecersiz girilirse resolver int-guvenli minimumlara,
  gecersiz EFF yuzdesi onayli `%10` default'una sanitize edilir; runtime UI kendi ayri fiyat
  veya effect formulu tutmaz.
