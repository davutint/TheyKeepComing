# Archer Targeting - Editor Setup

## Sahne Bağı

Yeni MonoBehaviour, prefab veya Inspector alanı yoktur. `ArcherShootSystem`,
`ArcherPrefabData`, `ArrowPrefabData`, aktif zombie component'leri ve mevcut
Formation V1 pozisyonlarını kullanır.

`NewGameScene` için gereken mevcut bağlar:

- `WaveConfigAuthoring` içinde Archer ve Arrow prefab referansları,
- `MobileCastleArcherTilePlacement` içinde Formation V1 asset'i,
- zombie prefabında `ZombieTag`, `ZombieStats`, `ZombieState`, `LocalTransform`,
  `DeathTimer` ve pool kullanılıyorsa `EnemyPoolMember`.

## Doğrulama

1. Unity Console'da compile error olmadığını kontrol et.
2. EditMode:
   `DeadWalls.Tests.ArcherTargetingUtilityTests`
3. PlayMode davranış:
   `DeadWalls.Tests.ArcherTargetingPlayModeTests`
4. Birleşik ölçek:
   `DeadWalls.Tests.HordeScalePlayModeTests.HordeScale_10K_WithHudFeedbackPoolFireballAndContinue_ProducesTelemetry`
5. Telemetry log'unda `enemy=10000`, `archer=1000`,
   `projectile_after_sample > 0` ve frame ölçümlerini kontrol et.

## Hata Ayırımı

- Okçu ateş etmiyor: target'ın enabled `ZombieTag`, pozitif `CurrentHP`, non-Dead
  state ve range koşullarını kontrol et.
- Bazı okçular bekliyor: range içindeki bütün target HP'leri uçuşta/yeni oklarla
  lethal olarak rezerve edilmiş olabilir; bu beklenen overkill guard'ıdır.
- Eski projectile pool'dan yeniden kiralanmış zombiye gidiyor: `EnemyPoolMember`
  generation ve `ArrowProjectile.TargetPoolGeneration` eşleşmesini kontrol et;
  retarget yapılmaz.
- 10K test yönteme girmeden `PlayModeRunTask.cs` NRE veriyor: bu Unity Test Runner
  initialization sorunudur; gameplay sonucu olarak raporlanmaz, runner job'u
  temizlenip test yeniden başlatılır.
