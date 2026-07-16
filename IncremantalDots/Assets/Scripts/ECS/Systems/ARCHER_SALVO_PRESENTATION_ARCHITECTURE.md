# Archer Salvo Presentation - Mimari

## Amaç

1.000 okçunun aynı anda ürettiği gerçek projectile, damage, target reservation, finite
Arrow tüketimi ve pool yaşam döngüsü için korunur. Sunum katmanı bu gameplay truth'u
azaltmadan aynı salvoyu bounded sayıda temsilci okla gösterir; böylece tek tek sprite
kaosu yerine duvar boyunca birlikte çıkan okunur bir atış ritmi oluşur.

## Bounded Temsilci Sözleşmesi

`ArcherSalvoPresentationUtility`, canlı okçu sayısından
`ceil(archerCount / 48)` sampling stride'ı üretir.

- `48` veya daha az okçuda stride `1` olur ve bütün projectile'lar görünürdür.
- `1.000` okçuda stride `21` olur; tek salvoda yalnız `47-48` ok görünür kalır.
- Temsilci seçimi pool'un monotonic `TotalRentCount` sırasına göre yapılır. Bu sıra
  entity index/pool reuse değişiminden bağımsızdır.
- Ardışık 1.000'lik salvolar `1.000 % 21 = 13` faz kaymasıyla farklı temsilci
  şeritlerine geçer; aynı görsel çizgi mekanik biçimde tekrar etmez.

Temsilci ok `LocalTransform.Scale = 1`, sunumdan saklanan gameplay oku `Scale = 0`
alır. Gizli okta `ArrowTag` açık kalır; `ArrowMoveSystem`, `ArrowHitSystem`, target
generation, Frost slow, hit feedback budget, ammo ve pool return aynı şekilde çalışır.
Ek entity, material, renderer, VFX veya managed allocation üretilmez.

## Runtime Akışı

1. `ArcherShootSystem` frame başında gerçek `ArcherUnit + LocalTransform` sayısını alır.
2. Hedef, ammo ve pool rent guard'ları mevcut sırada tamamlanır.
3. Başarılı rent'in güncel `TotalRentCount` değeri shot sequence olur.
4. Utility, okçu sayısı ile sequence'ten projectile scale'ını hesaplar.
5. ECB aynı gameplay projectile verisini yazar; yalnız transform scale sunum kararını
   taşır.
6. Aynı frame'deki `CombatSfxEvent`'ler mevcut `CombatFeedbackBridge` tarafından tek
   rate-limited salvo cue'sunda toplanır. Görsel ve işitsel grup ritmi aynı gerçek
   atış frame'inden beslenir.

## Continue

Aktif ok snapshot'ı gameplay state'ini saklamaya devam eder. Restore, saved active
projectile sayısını source count ve liste ordinal'ını sequence kabul ederek bounded
temsilci dağılımını yeniden kurar. Böylece eski veya yoğun bir snapshot Continue'da
bir anda bütün projectile sprite'larını görünür yapamaz; target, damage, kalan lifetime
ve pool ownership değişmez.

## Test ve Ölçek Kanıtı

- `ArcherSalvoPresentationUtilityTests 3/3`: küçük birlik full visibility, 1.000
  projectile için `47-48` temsilci ve ardışık salvo lane rotasyonu.
- `HordeScalePlayModeTests 1/1`: gerçek `NewGameScene` içinde `10.000` enemy +
  `1.000` archer ilk salvosu tam `1.000` gameplay projectile / `48` görünür temsilci /
  stride `21` olarak doğrulandı ve `1920x1080` screenshot üretildi.
- Aynı koşuda frame average `9,77 ms`, P95 `12,74 ms`, average draw call `544`;
  sample sonunda `745` aktif pooled projectile korunuyor.
- Targeting, finite ammo, arrow pool ve stale generation regresyonları PlayMode `5/5`
  geçti.

Bu modül için Inspector veya scene binding yoktur; limit runtime utility sabitidir.
