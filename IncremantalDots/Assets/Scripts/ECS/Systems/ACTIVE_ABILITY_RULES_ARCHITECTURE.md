# Unified Active Abilities - Mimari

## Oyuncu sözleşmesi

V1 aktif ability barı alt ortada üç slot taşır: `[1] Fireball`, `[2] Rally` ve
`[3] Emergency Repair`. Ability kullanımı Wood, Stone, Iron, Food veya mana tüketmez;
tek kapılar unlock, cooldown, aktif etki, phase, pause/level-up ve geçerli Wall state'idir.
Fireball dünya hedefi ister, Rally bütün okçuların atış hızını geçici artırır ve
Emergency Repair yalnız Night sırasında yaşayan, hasarlı Wall'u Max HP yüzdesi kadar
iyileştirir.

## Sahiplik

- `SpellCastUI`: tek input/presentation sahibidir. `1/2/3` hotkey'leri, üç buton,
  cooldown fill/label ve Fireball world-selection burada birleşir. Pointer UI üstündeyse
  Fireball cast edilmez.
- `GameManager`: ability transaction'larını, cooldown sayaçlarını, Fireball Heart
  modifier'larını ve exact run save/restore'u yönetir.
- `ActiveAbilityRules`: Rally ve Emergency Repair guard'larının saf otoritesidir;
  kaynak parametresi bilerek içermez.
- `CastleYardPrepState`: Rally aktif süre ve fire-rate multiplier verisini taşır.
- `CastleYardPrepSystem`: Rally timer'ını bütün continuous-cycle fazlarında azaltır.
- `MobileCastleCombatConfig`: normal repair, Rally ve Emergency Repair runtime tuning'ini
  taşır; `MobileCastleTuningResolver` değerleri `DifficultyProfileSO` kaynağından yazar.
- `RunPersistence`: güncel v13 şemasında Rally ve Emergency Repair cooldown'larını exact saklar;
  v11 kayıtları iki ability hazır başlayacak şekilde migrate edilir.

## Ability kuralları

### Fireball

- `[1]` veya buton targeting modunu açar; geçerli world click projectile yaratır.
- UI üstündeki click cast sayılmaz.
- Damage, radius ve cooldown değerleri Castle Heart effect'lerinden okunur.
- Aktif projectile ve cooldown exact Continue kapsamında korunur.

### Rally

- `[2]` veya buton anında etkinleştirir; kaynak harcamaz.
- Aktif Rally bitmeden veya cooldown sıfırlanmadan tekrar kullanılamaz.
- Eski `BuyRally` çağrısı aynı cost-free ability transaction'ına yönlendirilir.

### Emergency Repair

- `[3]` veya buton yalnız Night sırasında çalışır; kaynak harcamaz.
- Wall `0 HP`, full HP, Game Over veya level-up pending ise reddedilir.
- `CurrentHP > 0` guard'ı nedeniyle Wall aynı frame sıfıra ulaştıysa Game Over kazanır;
  ability ölümü geri çeviremez.

## Normal repair sınırı

Normal repair ability değildir. Yalnız Day/Dusk sırasında görünür ve çalışır. Her paket
`NormalRepairHealPercent` kadar Max HP iyileştirmeyi dener; eksik HP daha azsa yalnız
gerçek iyileşecek miktar fiyatlanır. Stone maliyeti
`ceil(actualHealHP × RepairStonePerMissingHp × RepairDayPriceMultiplier × discounts)`
formülüdür. Night guard transaction'dan önce çalıştığı için Stone harcanmaz.

## UI ve legacy sınırı

Prefab gerçeği `MobileCastleHudRoot.prefab/AbilityBarPanel`'dır. Panel bottom-center
anchor'lı `496 x 90` tek yüzeydir; Fireball, Rally ve Emergency Repair soldan sağa
üç doğrudan slot taşır. Her slot kendi raycast kapalı, vertical filled cooldown overlay'ine
sahiptir ve `SpellCastUI` kalan süreyi toplam süreye bölerek aynı state'i görselleştirir.
Eski `SpellUiRoot` / sağ-alt `SpellPanel` üretim yolu kaldırılmıştır. Fortify üçlü ability
barında bulunmaz; legacy prep etkisi ayrı kalır. Arrow Storm V1'e eklenmez.

## Test sahipleri

- `ActiveAbilityRulesTests`: cooldown, phase, Wall ölüm/full-health ve Game Over guard'ları.
- `MobileCastleTuningResolverTests`: profile tuning'inin runtime config'e aktarımı.
- `RunPersistenceTests`: v11->v12 cooldown ve v12->v13 Essence remainder migration sözleşmesi.
- `ExactRunContinuePlayModeTests`: cost-free kullanım, Night normal repair reddi ve
  Rally/Emergency cooldown + Wall HP exact Continue doğrulaması.
- `HudAbilityBarPresentationTests`: tek bottom-center paneli, üç slot geometrisini,
  cooldown overlay contract'ını ve legacy panel yokluğunu kilitler.
