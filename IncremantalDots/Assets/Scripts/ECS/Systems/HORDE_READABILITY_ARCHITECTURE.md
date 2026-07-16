# Horde Readability Architecture

## Amaç

`DW-I-POLISH-HORDE-READ`, tek `zombie_basic` prefabı ve mevcut DOTS render hattı
korunurken 10.000 düşmanın zeminden ayrılmasını, komşu siluetlerinin seçilmesini ve
animasyon kütlesinin tek frame'de titreşmemesini sağlar. Gameplay movement, attack,
damage, spawn yoğunluğu ve release `MaxAliveZombies` cap'i değişmez.

## Render sahipliği

`Assets/Materials/Vampire.mat`, mevcut `DeadWalls/SpriteSheet` shader'ında yalnız zombie
materyaline açık `_HordeReadability` sözleşmesini taşır:

- `x`: bir atlas texel'i genişliğindeki muted-cold silhouette edge kuvveti (`0.66`).
- `y`: edge örnekleme kalınlığı (`1.0` texel).
- `z`: quad ayak noktasındaki küçük contact-patch kuvveti (`0.56`).
- `w`: V1'de kullanılmaz ve `0` kalır.

Shader mevcut `Opaque / Geometry`, DOTS instancing ve tek-pass sözleşmesini korur. Edge
dört komşu alpha örneğinden, contact patch ise aynı quad içinde hesaplanır. Yeni renderer,
entity, material instance, ikinci pass veya draw çağrısı üretilmez. Worker, archer ve Arrow
materyallerinde `_HordeReadability = 0` olduğu için bu uniform dal kapalıdır.

## Motion cadence sahipliği

`EnemyPoolRuntimeUtility.TryRent`, generation arttıktan sonra
`HordeMotionCadenceUtility.Seed` çağırır. Entity index + pool generation hash'i:

- `CurrentFrame` değerini authored frame aralığına,
- `FrameTimer` değerini authored interval içinde 16 deterministik dilimden birine

dağıtır. Authored FPS/`FrameInterval` değiştirilmez. Aynı entity aynı generation'da aynı
fazı üretir; yeniden kiralama yeni generation ile tekrar dağıtılır.

`ZombieAnimationStateSystem`, loop state/direction değişiminde mevcut frame ve timer fazını
korur. Yalnız ölüm animasyonu bilinçli olarak frame 0'dan başlar. `SpriteAnimationSystem`,
frame hitch'i birden fazla interval aştığında O(1) catch-up uygular; böylece horde animasyonu
frame düşüşünde ağır çekime dönüşmez.

## Performans sınırı

- CPU: rent başına bir hash; steady-state'te yeni query, allocation veya structural change yok.
- GPU: yalnız Vampire fragmanında dört komşu alpha örneği; tek material/HybridBatch korunur.
- Contact patch ve edge alpha-cutoff hattında kalır; transparent queue veya `ZWrite Off`
  kullanılmaz.

## Doğrulama

- EditMode: faz dağılımı, determinism, hitch catch-up ve material/shader tek-pass opaque
  sözleşmesi.
- PlayMode: gerçek `NewGameScene` 10K + 1K archer benchmark correctness/performance.
- Görsel QA: 1920x1080 Night sahnesinde zemin teması, komşu silhouette ayrımı ve senkron
  olmayan hareket ritmi.
