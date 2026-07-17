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
- `GameManager`: ability transaction'larını, cooldown sayaçlarını, Fireball tech + Heart
  modifier'larını ve exact run save/restore'u yönetir.
- `ActiveAbilityRules`: Rally ve Emergency Repair guard'larının saf otoritesidir;
  kaynak parametresi bilerek içermez.
- `CastleYardPrepState`: Rally aktif süre ve fire-rate multiplier verisini taşır.
- `CastleYardPrepSystem`: Rally timer'ını bütün continuous-cycle fazlarında azaltır.
- `MobileCastleCombatConfig`: normal repair, Rally ve Emergency Repair runtime tuning'ini
  taşır; Wall base HP dahil `MobileCastleTuningResolver` değerleri `DifficultyProfileSO`
  kaynağından yazar. Profile yoksa baked `CastleAuthoring.WallHP` fallback kalir.
- `RunPersistence`: güncel v14 şemasında üç ability cooldown'ını ve aktif ability etkilerini
  exact saklar; v11 kayıtları Rally ve Emergency Repair hazır başlayacak şekilde migrate edilir.
- `GameplayTelemetry`: yeni ability state sahiplenmeden, yalniz kabul edilmis canonical
  transaction sonu `ability_cast` snapshot'ini yayinlar. Fireball kabul aninda speculative isabet
  saymaz; Rally gerçek archer totalini, Emergency Repair gerçek HP farkini taşır.

### `ActiveAbilityState` contract'i

Bu isim ayrı bir ECS component veya ikinci runtime state anlamına gelmez. V1 contract'i
mevcut tek owner zincirinin aşağıdaki birleşimidir:

| Contract alanı | Tek otorite | Save/restore kuralı |
|---|---|---|
| Unlock state | `GameManager`: Fireball tech/Heart effect'lerinden türetilir; Rally ve Emergency Repair V1'de daima açıktır | Tech node level'ları ve exact Heart graph saklanır; unlock tekrar uygulanır |
| Cooldown remaining | `GameManager._fireballCooldownRemaining`, `_rallyCooldownRemaining`, `_emergencyRepairCooldownRemaining` | Üç sayaç `RunSaveState` içinde exact saklanır |
| Fireball tuning modifier'ları | Tech node level'ları + `HeartEffectPipeline`; `GameManager` resolved damage/radius/cooldown sunar | Ham türetilmiş multiplier saklanmaz; tech/Heart state'inden yeniden kurulur |
| Rally/Emergency tuning | `DifficultyProfileSO` -> `MobileCastleTuningResolver` -> `MobileCastleCombatConfig` | Content/config tekrar bake/resolve edilir; cooldown remaining ayrı korunur |
| Aktif Rally etkisi | `CastleYardPrepState.RallyTimer/Duration/FireRateMultiplier` | Aktif değerler exact saklanır |

`SpellCastUI` bu state'in sahibi değildir; yalnız `GameManager` read API'sini görselleştirir
ve kabul edilen input'u aynı transaction API'lerine yollar. Development test unlock'u
production tech/Heart state'ini değiştirmeyen transient bir istisnadır.

## Ability kuralları

### Fireball

- `[1]` veya buton targeting modunu açar; geçerli world click projectile yaratır.
- UI üstündeki click cast sayılmaz.
- Damage, radius ve cooldown değerleri tech aggregate'i ile Castle Heart effect'lerinin
  çözülmüş birleşiminden okunur.
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

## First Night key-hint siniri

- `SpellCastUI.TryGetFirstReadyAbility`, player-facing slotlar arasinda authoritative readiness
  sonucunu `[1] Fireball -> [2] Rally -> [3] Emergency Repair` sirasiyla cozer ve gercek aktif
  button rect'ini onboarding sahibine verir.
- `AbilityHotkeyAcceptedByPlayer` yalniz `HandleHotkeys` tarafindan cagrilan ve gameplay
  transaction'i kabul edilen keyboard yolunda yayilir. Ayni ability'nin UI button handler'i bu
  event'i yaymaz; boylece mouse click key-hint ogretimini tamamlamaz.
- `FirstRunOnboardingUI` bu read-only readiness/accepted-input sinirini yalniz ilk Night cue'su
  icin kullanir. Ability unlock, cooldown, targeting, Wall HP ve kaynak state'inin sahibi olmaya
  calismaz.

## Normal repair sınırı

Normal repair ability değildir. Yalnız Day/Dusk sırasında görünür ve çalışır. Her paket
`NormalRepairHealPercent` kadar Max HP iyileştirmeyi dener; eksik HP daha azsa yalnız
gerçek iyileşecek miktar fiyatlanır. Stone maliyeti
`ceil(actualHealHP × RepairStonePerMissingHp × RepairDayPriceMultiplier × discounts)`
formülüdür. Night guard transaction'dan önce çalıştığı için Stone harcanmaz.
Gameplay quote'u ve Difficulty Tuner baseline preview'u ayni
`SingleWallDefenseRules.CalculateRepairStoneCost` metodunu kullanir.

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
- `RunPersistenceTests`: v11->v12 cooldown ve sonraki v14'e kadar sıralı migration sözleşmesi.
- `ExactRunContinuePlayModeTests`: cost-free kullanım, Night normal repair reddi,
  Fireball tech unlock/modifier rebuild'i, üç cooldown + Rally effect + Wall HP exact
  Continue doğrulaması.
- `HudAbilityBarPresentationTests`: tek bottom-center paneli, üç slot geometrisini,
  cooldown overlay contract'ını ve legacy panel yokluğunu kilitler.
- `FirstRunOnboardingTests` + `WorkerAllocationPlayModeTests`: ilk Night gate'ini, ilk hazir
  slot secimini, dynamic key copy'yi ve yalniz kabul edilmis keyboard yolunun durable flag
  yazmasini kilitler.
