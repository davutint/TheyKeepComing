# System Calisma Sirasi ve Sync Point Stratejisi

## SimulationSystemGroup Sirasi

`EnemyPoolInitializationSystem`, `InitializationSystemGroup` icinde Simulation baslamadan once catalog prewarm rezervini kurar.

```
-5. ContinuousSiegeCycleSystem
-4. DayNightPrepSystem
-3. BuildingProductionSystem
-2. BuildingPopulationSystem
-1. ArrowProductionSystem
 0. PopulationTickSystem
 1. BarracksTrainingSystem
 2. ResourceTickSystem
 3. WaveSpawnSystem
 4. CastleYardPrepSystem / ZombieSlowTimerSystem
    (ikisi de WaveSpawnSystem sonrasi; CastleYardPrepSystem ArcherShootSystem oncesi,
     ZombieSlowTimerSystem ApplyMovementForceSystem oncesi garantilenir)
 4b. MoatSystem (legacy sıra korunur; V1 `MoatGameplayEnabled=false` guard'ında çıkar)
 5. ArcherShootSystem
 6. ApplyMovementForceSystem
 7. BuildSpatialHashSystem
 8. PhysicsCollisionSystem
 9. IntegrateSystem
10. BoundarySystem
11. ZombieAttackTimerSystem
12. ArrowMoveSystem
12b. FireballProjectileSystem (UpdateAfter ArrowMove, UpdateBefore ArrowHit — mermiyi tasir, varista strike uretir)
13. ArrowHitSystem
13b. FireballStrikeSystem  (UpdateAfter ArrowHit, UpdateBefore ZombieDeath)
14. ZombieDeathSystem
15. ZombieAnimationStateSystem
16. ArcherAnimationStateSystem
17. DamageApplySystem
18. DamageCleanupSystem
```

Presentation tarafinda `SpriteAnimationSystem` UV rect hesaplarini yapar.

## Pause Guard

`GameStateData.IsLevelUpPending` veya `IsGameOver` true iken combat update'leri pause edilir. Movement, collision, integrate, boundary, attack timer, arrow move/hit, slow timer ve cleanup yeni state kapanana kadar ilerlemez. Mobile castle mode'da XP threshold artik `IsLevelUpPending` yapmaz, bu yuzden run loop level-up paneliyle durmaz.

## Sync Point Stratejisi

- Sistemlerin buyuk bolumu job schedule eder ve main thread'i bekletmez.
- `DamageApplySystem` tek bilincli sync point'tir; attack damage queue drain etmek icin pending job'lari tamamlar.
- `WaveSpawnSystem` frame basinda sequential calisir.
- Rent/return normal akista enableable component state'i degistirir; structural instantiate yalniz pool batch genislemesinde olur.
- `DamageCleanupSystem`, toplu return'de yalniz Burst-parallel transient reset job'ini tamamlar; ardindan available buffer ve pool telemetry'sini tek commit ile yazar.

## Sistem Notlari

### DayNightPrepSystem

- `ContinuousSiegeCycleData.Enabled` true ise erken cikar.
- Legacy mobile mode'da `DayPrep` sayacini azaltir.
- Sayac bitince `CurrentWave` artar, wave stat'leri configure edilir ve `NightCombat` baslar.
- Stress mode'da calismaz.

### ContinuousSiegeCycleSystem

- Mobile continuous siege mode'da 60s `DAY / DUSK / NIGHT / DAWN` (30/5/20/5) cycle datasini yazar;
  `SiegeDawnDuration=0` bake'lerde legacy 3-faz davranisa duser.
- Her tamamlanan cycle'da `CycleIndex` artar; `wave.CurrentWave = CycleIndex + 1` yazilir ve
  `MobileWaveUtility.ConfigureMobileWave` quantity pacing'i yeniden hesaplar. Enemy HP/damage/speed catalog base degerinde sabit kalir.
- `WaveStateData.WaveActive` degerini uyumluluk icin true tutar ve eski DayPrep dur-kalk akisinin tetiklenmesini engeller.
- `SpawnIntensityMultiplier` (Dawn 0.15 dahil) ve `HordePressure01` degerlerini `WaveSpawnSystem` ve HUD icin uretir.
- Stress mode'da calismaz.

### WaveSpawnSystem

- Mobile castle mode'da `MobileCastleCombatConfig` varsa spawn kale merkezi etrafindaki cemberden random aciyla yapilir.
- Continuous siege aktifken spawn yonu random 360 kalir ve interval/batch `ContinuousSiegeCycleData.SpawnIntensityMultiplier` ile akar; wave clear kontrolu calismaz.
- Continuous batch cycle ile de buyur: `SpawnBatchSize * intensity * (1 + (w-1)*SpawnBatchGrowthPerCycle)`, tavan `MaxSpawnBatch` (0 = sinirsiz).
- Continuous modda `MaxAliveZombies` guvenlik tavani (0 = sinirsiz): cap doluyken spawn atlanir; guard timer resetinden ONCE calisir, yer acilinca hemen doldurulur.
- Legacy mobile mode'da wave ici opening/mid/final fazlari interval ve batch'i degistirir.
- Legacy mobile modda wave temizlenince wave clear bonus'u ekler, `WaveClearRewardData` yazar ve `Phase = DayPrep`, `WaveActive = false`, `PrepTimer = DayPrepDuration` yazar.
- Worker economy aktifse wave clear bonus `WorkerEconomyRewardMultiplier` ile azaltilir.
- Yeni wave'i `DayNightPrepSystem` prep sayaci bitince otomatik baslatir.
- Stress mode'da config'teki stress batch, interval ve max alive cap'i kullanilir.
- Stress mode'da reward/bonus verilmez.
- Spawn `EnemyPoolRuntimeUtility.TryRent` kullanir; rezerv biterse definition `PoolExpandBatch` kadar genisler.
- Rent edilen zombide slow, death timer, physics, tint ve animation transient state resetlenir.

### ResourceTickSystem

- Mobile castle mode'da `MobilePopulationAllocation` yoksa legacy `EconomyFocusState` ile effective production hesaplar.
- Worker economy aktifken production zaten `MobilePopulationEconomySystem` tarafindan yazildigi icin focus multiplier uygulanmaz.
- `MobilePrepPauseState.IsPaused` true ise resource accumulator ilerletilmez; Castle Interior ekrani acikken prep timer ile kaynak tick birlikte durur.

### MobilePopulationEconomySystem

- Mobile normal mode'da worker allocation'i clamp eder.
- `ResourceProductionRate` ve `PopulationState.Workers/Idle` degerlerini yazar.
- Continuous siege aktifken population growth DAWN fazinda uygulanir (cycle basina bir kez;
  monotonik isaret degeri buyuk-dt'de Dawn frame'i kacsa bile odulu sonraki fazda telafi eder).
  `DawnDuration=0` legacy bake'lerde eski wrap-tabanli davranis korunur.
- Legacy mobile akista completed wave sonrasi DayPrep basinda population growth uygular.
- Nadir economy event roll eder ve secili production bonusunu rate'lere uygular.
- Stress mode'da calismaz.

### CastleYardPrepSystem

- Mobile normal mode'da `CastleYardPrepState.RallyTimer` degerini sadece `NightCombat` sirasinda azaltir.
- `FortifyActive` timer kullanmaz; wave bitince `WaveSpawnSystem` tarafindan temizlenir.
- `ArcherShootSystem` oncesinde calisir ki rally fire-rate multiplier'i ayni frame okunabilsin.

### ZombieSlowTimerSystem

- Frost slow duration'ini azaltir.
- Slow aktifken zombi `SpriteTint` rengini soguk/mavi yapar.

### MoatSystem (V1 Dormant)

- V1 config `MoatGameplayEnabled=false` olduğu için zombie query/job schedule edilmeden çıkar.
- Stale `MoatSlowMultiplier` veya `MoatDamagePerSecond` değerleri HP, hız veya slow state'ini değiştiremez.
- Legacy job ve execution-order attribute'ları gelecekteki içerik/migration için korunur.
- Ayrıntılı sınır `MOAT_DORMANCY_ARCHITECTURE.md` dosyasındadır.

### ArcherShootSystem

- Aktif target'ları coarse spatial map'e Burst-parallel yazar.
- Uçuşta olan ok damage'leriyle frame-local incoming load'u seed eder.
- Range içindeki yaşayan ve lethal load ile dolmamış en yakın zombiyi hedefler.
- Basic/Rapid/Frost aynı deterministic target policy'yi kullanır.
- Mobile config ve `UnlimitedArrows = true` iken `ArrowSupply.Current` kontrolu/decrement yapmaz.
- `CastleYardPrepState.RallyTimer > 0` iken fire-rate hesabina rally multiplier uygular.
- `ArrowPoolAvailable` rezervinden projectile rent eder; rezerv yoksa expand request yazar ve fire timer/ammo/reservation'i degistirmez.
- Basic/Rapid/Frost okcu stat'lerini rent edilen projectile'a tasir.
- Rent edilen oka okcu tipinin `SpriteTint` rengini yazar.
- Okcunun hedefe bakan `FacingDirection` degerini ve `AttackAnimTimer` degerini yazar.
- Fire timer'a gore pooled oku aktive eder.

### ArrowPoolMaintenanceSystem

- Initialization grubunda `ArrowPoolRuntimeData` owner'ini prewarm eder.
- Deferred return buffer/state sayaclarini uzlastirir.
- Rezerv tukendiyse sonraki frame `ExpandBatch` kadar yeni inactive ok hazirlar.

### ArrowMoveSystem / ArrowHitSystem

- Move job'u aktif ok lifetime'ini Burst-parallel azaltir ve valid hedefe hareket eder.
- Hit job'u isabet, timeout, disabled hedef veya generation mismatch'te ayni return yolunu kullanir.
- Pool okunu destroy etmez; transform/projectile/tint resetler, `ArrowTag` kapatir ve entity'yi rezerve append eder.

### ApplyMovementForceSystem

- Mobile mode'da hedef `CastleCenter`, eski mode'da `WallXPosition`.
- Moving zombilere hedefe dogru kuvvet uygular.
- `ZombieSlow` enabled ise hareket kuvvetini slow multiplier ile carpar.
- Attacking/Dead/Queued state'lerinde kuvvet sifirlanir.

### BoundarySystem

- Mobile mode'da `AttackRadius` icine giren zombiler `Attacking` olur.
- Eski mode'da `WallXPosition` bariyeri korunur.

### ArrowMoveSystem

- Oklari hedeflerine dogru hareket ettirir.
- `RemainingLifetime` degerini Burst-parallel azaltir.
- Invalid hedefte hareket etmez; cleanup'i tek return owner'i olan ArrowHitSystem'a birakir.

### ArrowHitSystem

- Mesafe `< 0.5` ise hasar uygular ve pooled oku rezerve dondurur.
- Timeout, disabled hedef ve generation mismatch ayni pool return yolunu kullanir.
- Frost ok isabetinde hedefteki `ZombieSlow` duration'ini refresh eder.

### FireballStrikeSystem (M-C buyuculuk)

- `FireballStrike` entity'lerini (GameManager.TryCastFireball yaratir) main-thread toplar,
  ECB ile siler; tek `IJobEntity` yaricap ici TUM zombilerin `CurrentHP`'sini dusurur.
- `RequireForUpdate<FireballStrike>` — cast yokken hic kosmaz (cooldown'lu oyuncu aksiyonu).
- Olum akisina karismaz (HP<=0 -> ZombieDeathSystem); pause guard ArrowHit ile ayni.

### DamageApplySystem

- Stress mode acikken damage queue temizlenir ve kale HP dusmez.
- Mobile normal mode'da `FortifyActive` ise Wall hasari Fortify damage multiplier ile carpilir.
- Tum dusman hasari yalniz `WallSegment` uzerine uygulanir.
- Wall HP sifira inerse Game Over tek yonlu olarak yazilir; repair/Council Wall'i diriltemez.

### ZombieDeathSystem / ZombieAnimationStateSystem

- Ayni frame olen zombiler arasindan atomik claim ile tek temsilci death SFX konumu secilir; 10K gecici event entity'si uretilmez.
- Death animasyonuna geciste enableable `DeathTimer` job icinde dogrudan yazilip acilir; entity basina ECB komutu yoktur.

### DamageCleanupSystem

- Death timer biterse XP ekler; pool uyelerini toplu olarak Burst job'da resetler ve tek buffer/state commit ile pool rezervine dondurur.
- Mobile normal mode'da kill reward'i `ResourceAccumulator` uzerine ekler.
- Return entity'yi scale `0` ve disabled `ZombieTag` ile inactive yapar; ayni entity sonraki rent'te yeni generation alir.
- Worker economy aktifse kill reward `WorkerEconomyRewardMultiplier` ile azaltilir.
- Legacy mode'da XP threshold asilirsa `IsLevelUpPending` true olur.
- Mobile castle mode'da XP threshold sadece progress olarak kalir; level-up pause yoktur.
