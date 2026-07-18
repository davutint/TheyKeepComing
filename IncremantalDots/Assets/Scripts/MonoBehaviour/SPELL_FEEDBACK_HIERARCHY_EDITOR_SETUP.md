# Spell Feedback Hierarchy - Editor Setup

## Otomatik Kurulum

`Window -> DeadWalls -> Mobile Castle Scene Setup` çalıştırıldığında mevcut `CombatFeedbackBridge` ve `SpellCastUI` bileşenlerine hierarchy varsayılanları yazılır. Araç yeni gameplay prefabı veya yeni combat sistemi oluşturmaz.

## CombatFeedbackBridge Değerleri

- Hit Flipbook Sorting Layer / Order: `Wall / 12`
- Frost Hit Sorting Order: `48`
- Frost Hit Scale Multiplier: `3.2`
- Frost Ring Start / End Scale: `1.05 / 2.2`
- Frost Hit Color: açık cyan
- Frost Ring Color: doygun cyan

Frost ring renderer'ları pool kurulurken otomatik oluşturulur; Inspector'dan prefab veya child referansı bağlanmaz.

## SpellCastUI Değerleri

- Spell Sorting Layer: `Wall`
- Projectile Aura / Projectile: `219 / 220`
- Blast / Core / Ring: `230 / 231 / 232`
- Scorched Earth Fill / Ring: `227 / 228`
- Projectile Aura Diameter / Pulse: `3.4 / 0.08`
- Blast Diameter Multiplier: `2.4`
- Blast Core Diameter Multiplier: `2.05`
- Blast Ring Diameter Multiplier: `2.8`
- Blast Ring Start / End Scale: `0.9 / 1.18`
- Blast Core Color: sıcak turuncu, alpha `0.72`
- Blast Ring Color: ateş turuncusu
- Echoing Detonation: mevcut blast renderer'larında sıcak-altın palette
- Scorched Earth: koyu ember fill + turuncu ring; `5s` boyunca ECS state'inden pulse/fade
- World Z: `MobileCastleRenderDepth.ProjectileZ (-2.5)`

Aura, core, ring ve Scorched Earth sprite'ları runtime'da üretilir; yeni asset veya prefab
bağlanmaz. Evolution node'ları `HeartNodeCatalog.asset` catalog v2 içinde bulunur; sahneye yeni
MonoBehaviour referansı eklenmez.

## Play Kontrolü

1. `NewGameScene` açılır.
2. Frost hit'lerin ordinary hit'lerden büyük ve cyan olduğu doğrulanır.
3. Fireball uçuşunda ana sprite çevresindeki sıcak aura kontrol edilir.
4. Patlamada turuncu core ile genişleyen ring'in horde üzerinde kaybolmadığı doğrulanır.
5. Patlama hasarı, yarıçapı, cooldown'u ve Frost slow davranışının değişmediği kontrol edilir.
6. `Scorched Earth` satın alındığında ember alanın beş saniyede sönmesi; `Echoing Detonation`
   satın alındığında ikinci altın patlamanın `0.85s` sonra görünmesi kontrol edilir.

## Otomatik Testler

- EditMode: `DeadWalls.Tests.SpellFeedbackHierarchyTests`
- Evolution PlayMode: `DeadWalls.Tests.SpellFeedbackHierarchyPlayModeTests.FireballEvolutions_ApplyExactAggregateDamageAndFixedGroundPresentation`
- PlayMode 10K: `DeadWalls.Tests.SpellFeedbackHierarchyPlayModeTests.FireballAndFrostHierarchy_RemainsOrderedInsideTenThousandEnemyHorde`
- Hit budget regresyonu: `DeadWalls.Tests.WorkerAllocationPlayModeTests.HitFeedbackBridge_EnforcesPlaybackBudgetAndRateLimit`
- Fireball/Continue 10K regresyonu: `DeadWalls.Tests.HordeScalePlayModeTests.HordeScale_10K_WithHudFeedbackPoolFireballAndContinue_ProducesTelemetry`
