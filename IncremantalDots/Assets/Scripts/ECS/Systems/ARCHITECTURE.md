# ECS Systems - Mimari

## Calisma Sirasi (UpdateOrder)
```
SimulationSystemGroup icinde:
1. WaveSpawnSystem       — Zombi spawn
2. ZombieMoveSystem      — Hareket (WaveSpawnSystem'den sonra)
3. ZombieAttackSystem    — Duvar/kapi/kale hasar (ZombieMoveSystem'den sonra)
4. ArcherShootSystem     — Ok firlat (ZombieAttackSystem'den sonra)
5. ArrowMoveSystem       — Ok hareket (ArcherShootSystem'den sonra)
6. ArrowHitSystem        — Ok isabet + hasar (ArrowMoveSystem'den sonra)
7. ZombieDeathSystem     — HP<=0 isaretleme (ArrowHitSystem'den sonra)
8. DamageCleanupSystem   — Temizlik + gold/XP (ZombieDeathSystem'den sonra)
```

## System Detaylari

### WaveSpawnSystem
- WaveStateData singleton'dan dalga bilgilerini okur
- SpawnTimer ile periyodik zombi spawn
- ZombiPrefabData'dan prefab klonlar
- Wave bittikten sonra yeni wave baslatir

### ZombieMoveSystem
- Burstable IJobEntity ile paralel calisir
- Moving state'deki zombileri sola hareket ettirir
- WallX'e ulasinca Attacking state'e gecer

### ZombieAttackSystem
- Attacking state'deki zombiler duvar/kapi/kale'ye hasar verir
- Oncelik: Wall → Gate → Castle
- CastleHP 0 olunca GameOver isaretler

### ArcherShootSystem
- Okcularin fire timer'ini gunceller
- En yakin zombiyi hedef secer
- ArrowPrefabData'dan ok spawn eder

### ArrowMoveSystem
- Oklari hedeflerine dogru hareket ettirir
- Hedef olmusse oku yok eder

### ArrowHitSystem
- Oklar hedefe yeterince yaklasmisssa hasar uygular ve oku yok eder

### ZombieDeathSystem
- HP <= 0 olan zombileri Dead state'e gecirir

### DamageCleanupSystem
- Dead zombileri entity olarak siler
- Gold ve XP ekler
- Wave alive sayisini dusurur
- Level up kontrolu yapar
