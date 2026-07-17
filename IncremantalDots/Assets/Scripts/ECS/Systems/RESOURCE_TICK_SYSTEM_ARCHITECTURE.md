# ResourceTickSystem - Mimari

## Sorumluluk

`ResourceProductionRate` değerlerini dakika bazından frame bazına çevirir, kesirli sonucu `ResourceAccumulator` içinde tutar ve tam sayıya ulaşınca `ResourceData` stoklarına aktarır.

## Dead Walls V1 sözleşmesi

Aktif castle loop, `MobileCastleCombatConfig` singleton'ı ile tanınır. Bu modda Wood, Stone, Iron ve Food için pasif upkeep yoktur. Sistem `ResourceConsumptionRate` değerlerini V1 hesaplamasında sıfır kabul eder; ana kaynaklar per-minute drain ile azalmaz. Player purchase/repair ve kabul edilen Dawn nüfusunun bir defalık Food maliyeti gibi committed transaction'lar bu sınırın dışındadır.

Legacy sahnelerde eski `production - consumption` davranışı korunur.

## Frame akışı

1. Game Over ise çık.
2. Base production ve legacy consumption singleton'larını oku.
3. V1 castle loop ise consumption'ı sıfırla; legacy ise olduğu gibi kullan.
4. Gerekirse economy focus'u production'a uygula.
5. `accumulator += netRate * deltaTime / 60`.
6. Accumulator `+1` veya `-1` eşiğini geçtiğinde integer stoka transfer et.

V1'de negatif accumulator yalnız eski save/state kalıntısından gelebilir; yeni pasif tüketim üretilmez.

## Release guard

`ExactRunContinuePlayModeTests.V1CastleLoop_DoesNotApplyPassiveMainResourceConsumption`, aktif
castle world'e yüksek legacy consumption rate enjekte edildiğinde Wood/Stone/Iron/Food stoklarının
azalmadığını doğrular. `ArrowAmmoPlayModeTests.V1CastleCombat_OnlyArrowStockHasContinuousDrain`
aynı sınırı gerçek combat tick'inde kontrol eder: production world'de `ArrowProducer` ve
`ArcherTrainer` yoktur, dört ana stok azalmaz ve başarılı tek projectile rent'i yalnız
`ArrowSupply.Current` değerini `1` düşürür.

## İlgili state

- `ResourceData`: integer stoklar.
- `ResourceProductionRate`: worker ve bonuslardan gelen üretim.
- `ResourceConsumptionRate`: yalnız legacy pasif rate uyumluluğu.
- `ResourceAccumulator`: kesirli üretim/tüketim tamponu; exact run snapshot'a dahildir.
