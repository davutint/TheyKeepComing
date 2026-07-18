# Fireball Evolution Systems — Editor Setup

## Asset ve sahne kurulumu

Yeni authoring component veya scene binding gerekmez. `Window -> DeadWalls -> Rebuild Castle Heart
Production Catalog` menüsü catalog `v2` asset'ini ve iki node asset'ini üretir:

- `scorched_earth` -> `EnableBurningGround`
- `echoing_detonation` -> `EnableSecondBlast`

Aktif sahnedeki mevcut `GameManager`, `SpellCastUI` ve Fireball ECS zinciri bu effect'leri otomatik
olarak kullanır.

## Inspector olmayan tuning

Launch değerlerinin tek kod sahibi `FireballEvolutionRules` sabitleridir. Inspector override yoktur;
catalog description, test ve V1 spec bu değerlerle eşit tutulmalıdır.

## Kontrol listesi

1. Production catalog rebuild sonrasında catalog version `2`, node count `37` olmalı.
2. Console'da compile error olmamalı.
3. Castle Heart'ta iki node Rare Evolution olarak depth `3–5` aralığında görünmeli.
4. Scorched Earth alanı beş saniyede sönmeli ve düşman sayısıyla görsel nesne üretmemeli.
5. Echoing Detonation ikinci patlaması `0.85s` sonra sıcak-altın görünmeli.
6. Save/Continue, bekleyen secondary blast ve aktif burning ground'u kaldığı yerden sürdürmeli.

## Otomatik testler

- EditMode: `DeadWalls.Tests.HeartProductionCatalogTests`
- EditMode: `DeadWalls.Tests.SpellFeedbackHierarchyTests`
- EditMode: `DeadWalls.Tests.RunPersistenceTests`
- PlayMode: `DeadWalls.Tests.SpellFeedbackHierarchyPlayModeTests.FireballEvolutions_ApplyExactAggregateDamageAndFixedGroundPresentation`
