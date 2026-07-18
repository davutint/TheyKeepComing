# Horde Readability Editor Setup

## Otomatik kurulum

Unity menüsünden `Window > DeadWalls > Repair Horde Readability` çalıştırılır. Araç:

1. `Assets/Materials/Vampire.mat` materyalini yükler.
2. `DeadWalls/SpriteSheet` shader'ını ve GPU instancing'i korur.
3. `_HordeReadability = (0.66, 1.0, 0.56, 0)` değerini yazar.
4. `_HordeGroundContact = (0.50, 0.30, 0.075, 0.025)` ile patch'i aktif atlas ayak
   bandının hemen altına yerleştirir.
5. Soğuk edge ve koyu contact-patch renklerini idempotent biçimde uygular.

Yeni prefab, renderer, material instance veya sahne objesi oluşturmaz.

## Manuel kontrol

- Zombie prefab renderer materyali `Vampire` kalmalıdır.
- Shader `Opaque / Geometry`, tek pass ve DOTS instancing kullanmalıdır.
- 10K Night ekranında ayak temasları zeminde küçük kalmalı; birleşik siyah halı üretmemelidir.
- Combined Night kabulünde aynı 1920x1080 kare `10.000` enemy, `1.000` canonical Basic Archer
  ve aktif projectile taşımalıdır; test log'undaki exact sayaçlar PNG ile aynı capture anına aittir.
- Contact patch zombie ayağından kopuk görünmemeli; ayrı bir havada-gölge izi olmamalıdır.
- Silhouette edge tek atlas texel'i olmalı; sprite'ı neon bir kontura çevirmemelidir.
- En az 12 farklı başlangıç frame'i ve timer dilimi görülmelidir; authored FPS `10` kalmalıdır.

## Testler

- `DeadWalls.Tests.HordeReadabilityTests`
- `DeadWalls.Tests.EnemyPoolRuntimeUtilityTests`
- `DeadWalls.Tests.HordeScalePlayModeTests.HordeScale_10K_WithHudFeedbackPoolFireballAndContinue_ProducesTelemetry`
  - `DW_V1_10K_1K_NIGHT_VISUAL_ACCEPTANCE.png` üretir.
  - `[DW-V1-10K-1K-NIGHT-VISUAL]` log'u phase, exact entity/projectile sayaçları, overlay/light
    değerleri ve çözünürlüğü kaydeder.
