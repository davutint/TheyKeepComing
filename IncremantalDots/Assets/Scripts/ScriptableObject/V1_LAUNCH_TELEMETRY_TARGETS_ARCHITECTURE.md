# V1 Launch Telemetry Targets Architecture

## Amaç ve sınır

`V1LaunchTelemetryTargetsSO`, launch ölçümlerinin provider-independent kabul bantlarını tutar.
Gameplay değerini değiştirmez, analytics SDK seçmez, event toplamaz ve otomatik retuning yapmaz.
Runtime event sahipliği `GameplayTelemetry`; gerçek tuning sahipliği Difficulty, Archer ve Meta
production asset'lerindedir.

Production asset:
`Assets/ScriptableObject/MobileCastle/Tuning/V1LaunchTelemetryTargets.asset`

## Veri modeli

Her `V1TelemetryTargetDefinition` stable Id, kategori, unit, inclusive min/max, minimum sample,
cohort, canonical source event listesi ve designer interpretation taşır. `ValidateProfile()`:

- duplicate/boş Id ve eksik metni,
- invalid/negative bandı ve `0..1` dışındaki ratio'yu,
- bilinmeyen veya duplicate source event'i,
- eksik Spawn/Economy/Combat/Council/Meta kategorisini

fail-closed raporlar.

`ComputeFingerprint()`, array sırasını ve bütün contract alanlarını invariant-culture SHA-256 ile
kilitler. Bu kimlik build/cohort karşılaştırması içindir; save schema veya player identity değildir.

## Tüketiciler

`DifficultyTunerWindow`, production asset'i read-only, polish edilmiş `V1 Launch Telemetry Targets`
panelinde gösterir. Asset düzenlemesi Inspector üzerinden bilinçli yapılır. Harici bir subscriber
ileride `GameplayTelemetry.Emitted` kayıtlarını kendi taşıma katmanına gönderebilir; target asset
provider'a referans vermez.

Canonical değer tablosu ve review süreci:
`Assets/Docs/DEAD_WALLS_V1_LAUNCH_TUNING_AND_TELEMETRY_TARGETS.md`.
