# Catapult System — Mimari

## Genel Bakis
Mancinik silah pipeline'i uc sistemden olusur: Shoot → Move → Hit. Mevcut okcu pipeline'inin (ArcherShoot → ArrowMove → ArrowHit) AoE versiyonu.

## Sistemler

### CatapultShootSystem
- **Pattern:** ArcherShootSystem (main thread, SystemAPI.Query)
- **Siralama:** `[UpdateAfter(WaveSpawnSystem)] [UpdateBefore(ApplyMovementForceSystem)]`
- **BurstCompile:** Evet (struct + OnUpdate)
- **Isleyis:**
  1. GameState guard (IsGameOver/IsLevelUpPending)
  2. Her CatapultUnit icin timer dusur
  3. Timer <= 0 ise brute-force en yakin zombie bul (menzil icinde)
  4. Tas kontrolu — ResourceData.Stone < StoneCostPerShot ise atma
  5. Tas dusur, timer reset
  6. ECB ile CatapultProjectile entity spawn (StartPos + TargetPos set)

### CatapultProjectileMoveSystem
- **Pattern:** ArrowMoveSystem (IJobEntity, [BurstCompile])
- **Siralama:** `[UpdateAfter(ZombieAttackTimerSystem)]`
- **BurstCompile:** Evet (struct + job)
- **Isleyis:**
  1. FlightTimer += dt
  2. t = FlightTimer / FlightDuration (0-1 araliginda clamp)
  3. Yatay pozisyon: `lerp(StartPos, TargetPos, t)`
  4. Dikey parabol: `ArcHeight * 4 * t * (1-t)` (t=0.5'te maksimum)
  5. Tanjant vektorunden rotasyon hesapla

### CatapultProjectileHitSystem
- **Pattern:** BoundarySystem ([BurstCompile] struct'ta YOK — static field erisimi)
- **Siralama:** `[UpdateAfter(CatapultProjectileMoveSystem)]`
- **BurstCompile:** HAYIR (BuildSpatialHashSystem.ReadMap static field erisimi)
- **Isleyis:**
  1. FlightTimer >= FlightDuration olan mermiler icin:
  2. Spatial hash (ReadMap) uzerinden AoE tarama
  3. cellRange = ceil(SplashRadius / DefaultCellSize) ≈ 6
  4. Yaricap icindeki her zombinin ZombieStats.CurrentHP -= Damage
  5. ECB.DestroyEntity ile mermiyi sil

## Performans Notlari
- ShootSystem: ~3 mancinik x 6000 zombie = 18K distance check — Burst ile trivial
- MoveSystem: IJobEntity parallel — mancinik sayisi az, overhead minimal
- HitSystem: Main thread ama frame basina ~3 mermi isabet — spatial hash taramasi hizli

## Sistem Sira Diyagrami
```
ArcherShootSystem (2)
CatapultShootSystem (2b)     ← ayni sira araliginda
  ...
ArrowMoveSystem (9)
CatapultProjectileMoveSystem (9b)
ArrowHitSystem (10)
CatapultProjectileHitSystem (10b)
```

## Dosya Konumlari
- `Assets/Scripts/ECS/Systems/CatapultShootSystem.cs`
- `Assets/Scripts/ECS/Systems/CatapultProjectileMoveSystem.cs`
- `Assets/Scripts/ECS/Systems/CatapultProjectileHitSystem.cs`
