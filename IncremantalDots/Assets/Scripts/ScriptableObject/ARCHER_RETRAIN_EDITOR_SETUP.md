# Archer Retrain Editor Setup

## Definition asset'leri

`BasicArcher.asset`, `RapidArcher.asset` ve `FrostArcher.asset` içinde
`CostGrowthInterval=25`, `CostGrowthExponent=2` bulunur. Basic hedef retrain olmadığı
için `RetrainCost=0` kalır. Rapid/Frost retrain base maliyetleri ilgili buy maliyetinin
Food içermeyen karşılığıdır; exact balance bu asset'lerden değiştirilir.

Bir definition'da interval `1` altına veya exponent geçersiz değere inse dahi runtime
utility güvenli fallback uygular. Negatif resource alanları harcama/refund üretmez.

## HUD prefab

UI truth:

`Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab`

Beklenen hierarchy:

`ArcherRecruitmentRowTemplate/ArcherRetrainButton/ArcherRetrainButtonText`

Kontrol kaybolursa `Window > DeadWalls > Repair Archer Retrain Control` çalıştırılır.
Repair komutu mevcut `ArcherBuyButton` stilini klonlar, retrain binding adlarını ve
layout'u kurar, prefaba kaydeder ve tekrar çalıştırıldığında ikinci buton üretmez.

## Manuel kontrol

1. `NewGameScene` Play Mode'u aç.
2. Rapid veya Frost'u Castle Heart üzerinden aç.
3. Archer drawer'da hedef satırın `RETRAIN` butonunu doğrula.
4. Bir Basic dönüştür; toplam archer ve population aynı kalmalı.
5. Hedef tür sayısı bir arttığı için sonraki buy ve retrain maliyeti büyümeli.
6. Toplam ordu `1000` iken buy `MAX` kalmalı fakat Basic varsa retrain açık olmalı.
7. Locked tür, Basic yokluğu veya yetersiz kaynak retrain transaction'ı başlatmamalı.

Otomatik kanıt için `ArcherRecruitmentCostUtilityTests` ve
`ArcherRetrainPlayModeTests` çalıştırılır.
