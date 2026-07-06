# System Calisma Sirasi ve Sync Point Stratejisi

## SimulationSystemGroup Sirasi

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
 4b. MoatSystem (ZombieSlowTimerSystem SONRASI, ApplyMovementForceSystem ONCESI —
     hendek bandindaki zombilere ZombieSlow tazeler; timer'in ayni frame disable'i
     moat tarafindan yeniden enable edilebilir; MoatDamagePerSecond > 0 ise HP asindirir)
 5. ArcherShootSystem
 6. ApplyMovementForceSystem
 7. BuildSpatialHashSystem
 8. PhysicsCollisionSystem
 9. IntegrateSystem
10. BoundarySystem
11. ZombieAttackTimerSystem
12. ArrowMoveSystem
13. ArrowHitSystem
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

## Sistem Notlari

### DayNightPrepSystem

- `ContinuousSiegeCycleData.Enabled` true ise erken cikar.
- Legacy mobile mode'da `DayPrep` sayacini azaltir.
- Sayac bitince `CurrentWave` artar, wave stat'leri configure edilir ve `NightCombat` baslar.
- Stress mode'da calismaz.

### ContinuousSiegeCycleSystem

- Mobile continuous siege mode'da 60s `DAY / DUSK / NIGHT / DAWN` (22/8/22/8) cycle datasini yazar;
  `SiegeDawnDuration=0` bake'lerde legacy 3-faz davranisa duser.
- Her tamamlanan cycle'da `CycleIndex` artar; `wave.CurrentWave = CycleIndex + 1` yazilir ve
  `MobileWaveUtility.ConfigureMobileWave` ile zombi stat/pacing yeniden hesaplanir (kutle eskalasyonu:
  HP lineer `ZombieBaseHP*(1+(w-1)*ZombieHpGrowthPerCycle)`, ustel DEGIL).
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
- Spawn edilen zombilerde `ZombieSlow` disabled resetlenir.

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

### MoatSystem (Tek Cephe / K4)

- `SingleFrontEnabled` + hendek etkisi aktifken kosar; `MoatXMin..MoatXMax` bandindaki
  Moving/Queued zombilere `ZombieSlow` uygular (frost ile ayni kanal: en dusuk carpan kazanir,
  sure 0.15s tazelenir — banttan cikinca timer dogal sonumler).
- `MoatDamagePerSecond > 0` (moat_flame tech'i) ise banttaki zombilerin CurrentHP'sini asindirir;
  olum ZombieDeathSystem'de islenir. Her entity yalniz kendi verisini yazar (ScheduleParallel).
- Duration bitince multiplier'i `1` yapar ve `ZombieSlow` component'ini pasifler.
- Duration bitince veya zombi Dead state'e gecince tint'i normale dondurur.
- `ApplyMovementForceSystem` oncesinde calisir.

### ArcherShootSystem

- Range icindeki en yakin zombiyi hedefler.
- Mobile config ve `UnlimitedArrows = true` iken `ArrowSupply.Current` kontrolu/decrement yapmaz.
- `CastleYardPrepState.RallyTimer > 0` iken fire-rate hesabina rally multiplier uygular.
- Basic/Rapid/Frost okcu stat'lerini projectile'a tasir.
- Spawn edilen oka okcu tipinin `SpriteTint` rengini yazar.
- Okcunun hedefe bakan `FacingDirection` degerini ve `AttackAnimTimer` degerini yazar.
- Fire timer'a gore ok spawn eder.

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
- Hedefi olmayan oklari ECB ile siler.

### ArrowHitSystem

- Mesafe `< 0.5` ise hasar uygular ve oku siler.
- Frost ok isabetinde hedefteki `ZombieSlow` duration'ini refresh eder.

### DamageApplySystem

- Stress mode acikken damage queue temizlenir ve kale HP dusmez.
- Mobile normal mode'da `FortifyActive` ise wall/gate/castle hasari Fortify damage multiplier ile carpilir.
- Hasar onceligi: Wall -> Gate -> Castle.
- Castle HP sifira inerse game over yazar.

### DamageCleanupSystem

- Death timer biterse XP ekler ve zombi entity'sini siler.
- Mobile normal mode'da kill reward'i `ResourceAccumulator` uzerine ekler.
- Worker economy aktifse kill reward `WorkerEconomyRewardMultiplier` ile azaltilir.
- Legacy mode'da XP threshold asilirsa `IsLevelUpPending` true olur.
- Mobile castle mode'da XP threshold sadece progress olarak kalir; level-up pause yoktur.
