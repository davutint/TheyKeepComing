# Dead Walls V1 — Fireball Behavior Evolutions

> **Durum:** Launch content authority
> **Catalog:** Castle Heart catalog `v2`
> **Kapsam:** `Scorched Earth` ve `Echoing Detonation`

Bu belge, V1 Fireball davranış evolution'larının sayı, gameplay, VFX, performans ve exact
Continue sözleşmesidir. Numeric `Searing Flames`, `Greater Blast`, `Arcane Focus`, `Blazing
Core` ve `Ember Reservoir` node'ları korunur; burada yalnız Fireball'ın davranışını değiştiren
iki tek-seferlik evolution tanımlanır.

## 1. Scorched Earth

- Castle Heart node: `scorched_earth`, Rare Evolution, depth `3–5`, maliyet `44 GE`.
- Primary impact konumunda, cast anındaki gerçek radius'un `%70` değerinde alan bırakır.
- Alan `5s` sürer; ilk tick bir saniye sonra gelir ve toplam `5` tick üretir.
- Her tick, cast anındaki gerçek primary impact damage'in `%12` değeridir.
- Toplam ek damage, bütün tick'ler isabet ederse primary impact'in `%60` değeridir.
- Gameplay owner `FireballBurningGroundSystem`; her tick tek aggregate `FireballStrike` üretir.
- VFX, koyu ember fill + turuncu ring olmak üzere alan başına iki sabit renderer'dır. Düşman
  sayısı, hit veya damage adedi yeni renderer/particle/event üretmez.
- Tick'ler SFX üretmez; 10K horde içinde ses ve event flood'u oluşmaz.

## 2. Echoing Detonation

- Castle Heart node: `echoing_detonation`, Rare Evolution, depth `3–5`, maliyet `46 GE`.
- Primary impact'ten `0.85s` sonra aynı merkezde ikinci patlama oluşur.
- İkinci patlama cast anındaki gerçek primary damage'in `%60`, radius'un `%85` değerini kullanır.
- Gameplay owner `FireballSecondBlastSystem`; tek timer entity tek secondary strike üretir.
- VFX mevcut blast flipbook/core/ring renderer'larını sıcak-altın palette yeniden kullanır.
- Secondary blast mevcut rate-limited Fireball SFX kanalını daha düşük volume ve yüksek pitch ile
  kullanır; ayrı audio pool kurmaz.

## 3. Birlikte çalışma ve snapshot kuralı

İki evolution Keystone değildir, conflict taşımaz ve aynı koşuda birlikte çalışabilir. Tüm değerler
cast anında `FireballProjectile.Evolutions`, damage ve radius ile snapshot edilir. Sonradan yapılan
Heart alımı havadaki projectile'ı veya kurulmuş evolution state'ini değiştirmez.

`RunSaveState v16` şu runtime state'i exact korur:

- aktif projectile evolution flag'leri;
- işlenmeyi bekleyen primary/secondary/pulse strike;
- secondary blast kalan delay'i;
- burning ground kalan duration, sonraki tick süresi, exact kalan tick sayısı, radius ve tick
  damage'i.

Catalog `v1` graph restore edildiğinde yalnız catalog kimliği `v2` olur. Yeni node'lar mevcut
graph'a eklenmez ve koşu reroll edilmez.

## 4. Performans ve kabul sınırı

- Hasar enemy başına event veya entity üretmez; mevcut parallel AoE damage job'u kullanılır.
- Scorched Earth saniyede en fazla bir aggregate strike üretir ve tam yaşamında beş pulse ile
  sınırlıdır.
- Echoing Detonation cast başına yalnız bir timer ve bir secondary strike üretir.
- Primary/secondary blast aynı üç renderer'ı paylaşır; ground VFX alan başına iki renderer'dır.
- EditMode kuralları exact multiplier, süre, tick count, sorting ve fade değerlerini kilitler.
- PlayMode testi gerçek `NewGameScene` içinde Heart behavior binding, projectile snapshot,
  aggregate damage toplamı, timer/ground lifecycle ve fixed-renderer sunumunu doğrular.
- Ayrı exact Continue PlayMode testi pending secondary delay ile ground duration/next-tick/kalan
  tick state'inin canlı `GameManager` capture/restore yolunu doğrular.

## 5. Kapsam dışı

- Yeni büyü, yeni enemy prefabı, boss, elemental resistance veya status-stack sistemi yoktur.
- Per-enemy burning particle, floating damage text, ayrı audio asset paketi veya gameplay camera
  shake bu paket kapsamında değildir.
- Exact balance curve polish'i tracker'daki sonraki tuning paketine aittir.
