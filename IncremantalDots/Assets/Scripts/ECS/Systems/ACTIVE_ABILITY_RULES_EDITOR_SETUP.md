# Unified Active Abilities - Editor Setup

## Otomatik kurulum

1. `NewGameScene` açık ve Play Mode kapalı olsun.
2. `Window > DeadWalls > Repair Unified Ability Bar` çalıştır.
3. Araç `Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab` içindeki
   `AbilityBarPanel` görsel gerçeğini onarır, scene controller binding'lerini günceller
   ve eski `SpellUiRoot` nesnesini kaldırır.
4. Console'da unified ability bar onarım kaydını doğrula.

## Beklenen prefab hiyerarşisi

`MobileCastleHudRoot/AbilityBarPanel`, bottom-center anchor'da `496 x 90` tek paneldir.
Altında soldan sağa şu doğrudan slotlar bulunmalıdır:

- `FireballButton` + `FireballButtonCooldownFill`
- `RallyAbilityButton` + `RallyAbilityButtonCooldownFill`
- `EmergencyRepairAbilityButton` + `EmergencyRepairAbilityButtonCooldownFill`

Her cooldown görseli `Image.Type.Filled`, `FillMethod.Vertical`, bottom origin ve
`raycastTarget=false` sözleşmesini taşır. Slot rect'leri birbiriyle çakışmaz.

Aktif scene'de tek `SpellCastUI`, `MobileCastleHudRoot` üzerinde yaşar. `SpellPanel`,
üç button/fill/label alanı ve Fireball visual sprite binding'leri dolu olmalıdır.
Scene'de ikinci `SpellCastUI` veya `SpellUiRoot` kalmamalıdır.

## Tuning

Default difficulty profile şu runtime alanlarını besler:

- `NormalRepairHealPercent` (`0.25`)
- `RepairStonePerMissingHp` (`0.10`)
- `RepairDayPriceMultiplier` (`1.0`)
- `RallyCooldown` (`60s`)
- `EmergencyRepairHealPercent` (`0.20`)
- `EmergencyRepairCooldown` (`120s`)

Rally duration ve fire-rate multiplier mevcut `CastleYardPrepState` tuning'inden gelir.
Fireball damage/radius/cooldown ise Castle Heart node effect'leriyle değişir.

## Play Mode kabulü

- `1` Fireball targeting açar; UI click'i cast etmez, world click projectile yaratır.
- `2` Rally'yi resources değişmeden etkinleştirir ve cooldown UI'ı başlar.
- `3` Day/Dusk sırasında reddedilir; Night sırasında hasarlı yaşayan Wall'u resources
  değişmeden iyileştirir.
- Normal repair Day/Dusk sırasında Stone harcar; Night başlayınca buton kaybolur ve
  transaction reddedilir.
- Wall `0 HP` olduktan sonra Emergency Repair çalışmaz.
- Save/Continue sonrası üç ability cooldown gösterimi runtime state ile aynıdır.

Otomatik regresyon için `HudAbilityBarPresentationTests`, `ActiveAbilityRulesTests`,
`MobileCastleTuningResolverTests`, `RunPersistenceTests` ve hedefli
`ExactRunContinuePlayModeTests` çalıştırılır.
