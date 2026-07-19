# Grave Essence Drop Editor Setup

## Production Ayari

1. `Window > DeadWalls > Difficulty Tuner` ac.
2. `Heart Runtime Contract` bolumunu genislet.
3. `Enemy death drop chance` ve `Base Essence per successful drop` alanlarini duzenle.
4. `APPLY` ile profile'i scene runtime config'ine uygula.

Production baseline `%10` ve `1`dir. `MobileCastleCombatAuthoring` alanlari yalniz Profile bosken
fallback olarak kullanilir. `GraveEssenceDropSeed = 0` guvenli production fallback seed'ine normalize edilir.

## Dogrulama

- EditMode: `GraveEssenceDropUtilityTests` chance sinirlarini, determinizmi ve buyuk orneklemde `%10`
  dagilimini dogrular.
- EditMode: `HeartTuningContractTests` production profile degerlerini ve tek gain owner zincirini korur.
- PlayMode: `CombatRewardFeedbackPlayModeTests` chance'i `%100` yaparak gercek Skeleton olumunun
  Soul ile birlikte Grave Essence verdigini ve transient event'in tuketildigini dogrular.
