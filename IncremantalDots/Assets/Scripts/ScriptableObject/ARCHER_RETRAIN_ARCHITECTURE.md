# Archer Retrain Architecture

## Ürün sözleşmesi

Basic okçular, ilgili Castle Heart unlock'ı alındıktan sonra Rapid veya Frost türüne
tek seferlik kaynak ödemesiyle dönüştürülebilir. Oyuncu world üzerinde bireysel okçu
seçmez. `GameManager` bir Basic `ArcherUnit` entity'sini yerinde dönüştürür.

Retrain:

- yeni entity üretmez ve var olan entity'yi yok etmez,
- toplam Basic+Rapid+Frost sayısını değiştirmez,
- `PopulationState` alanlarını değiştirmez,
- ortak `1000` cap doluyken de çalışabilir,
- yalnız Heart tarafından açılmış Rapid/Frost hedeflerine izin verir.

## Maliyet owner'ı

Her `ArcherDefinitionSO` şu tuning verilerini taşır:

- `BuyCost`: o türden yeni okçu alma base maliyeti,
- `RetrainCost`: Basic'ten o türe dönüşüm base maliyeti,
- `CostGrowthInterval`,
- `CostGrowthExponent`.

Hem buy hem retrain, transaction öncesindeki hedef tür sayısını kullanır:

`ceil(base × (1 + targetTypeCount / interval) ^ exponent)`

Varsayılan değerler `interval=25`, `exponent=2` değerleridir. Formül
`ArcherRecruitmentCostUtility` içinde int-safe çalışır; negatif girişleri sanitize eder
ve temsil edilemeyen sonucu `int.MaxValue` değerinde doyurur. Exact balance SO
üzerinden ayarlanır; runtime state asset'e yazılmaz.

## Atomic runtime akışı

`MarketUI` → `GameManager.CanRetrainBasicArcher` → resource transaction →
`GameManager.ApplyArcherTypeToEntity` akışı kullanılır.

Dönüştürülen entity'nin transform, fire timer, facing ve animation timer state'i korunur.
Type, damage, fire rate, range, Frost slow verisi ve sprite tint hedef türün mevcut
Heart/meta aggregate'leriyle yeniden yazılır. Ardından type count cache'i yenilenir.

Save schema değişmez. Exact save zaten Basic/Rapid/Frost sayılarını tuttuğu için
retrain sonucu bir sonraki snapshot'a doğal olarak yansır.

## Progression sınırı

Retrain bir archer level sistemi değildir. Rapid/Frost unlock, damage, fire rate,
range ve Frost slow geliştirmeleri Castle Heart effect pipeline'ında kalır. Eski
Market upgrade/unlock kontrolleri player-facing değildir.

## UI

Aktif truth `MobileCastleHudRoot.prefab` içindeki inactive
`ArcherRecruitmentRowTemplate`'tir. Template `ArcherBuyButton` yanında
`ArcherRetrainButton` taşır. Basic runtime satırında retrain gizlenir; Rapid/Frost
satırlarında unlock, Basic varlığı ve kaynak durumuna göre açılır. Cap dolması buy'ı
`MAX` yapar fakat retrain'i engellemez.

`Difficulty Tuner > Archer Runtime Contract`, her definition'in buy ve retrain base
maliyetini, ortak interval/exponent'ini ve secilen target-type count icin iki quote'u ayni
gameplay utility'siyle gosterir. Live Apply mevcut retrain/count state'ini sifirlamaz.

## Doğrulama

- `ArcherRecruitmentCostUtilityTests`: eğri, input sanitization ve int saturation.
- `ArcherRetrainPlayModeTests`: gerçek `NewGameScene` ve gerçek dynamic drawer
  üzerinden Basic→Rapid/Frost dönüşümü; toplam entity/population değişmezliği ve
  hedef tür maliyetinin artması.
