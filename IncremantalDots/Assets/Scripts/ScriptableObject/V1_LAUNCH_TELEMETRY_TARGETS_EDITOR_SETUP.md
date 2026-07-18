# V1 Launch Telemetry Targets Editor Setup

## Production asset

Production profil yolu:
`Assets/ScriptableObject/MobileCastle/Tuning/V1LaunchTelemetryTargets.asset`.

Normal kullanımda asset'i yeniden oluşturma. `Window > DeadWalls > Difficulty Tuner` içindeki
`V1 Launch Telemetry Targets` foldout'u profil/version, default sample, SHA-256 fingerprint ve 19
bandı read-only gösterir. `SELECT TARGET ASSET` ile Inspector'a geçilebilir.

## Değişiklik akışı

1. Cohort sonucu ve designer kararı kayda alınır.
2. Inspector'da yalnız onaylı target band/sample/cohort alanı değiştirilir.
3. `ValidateProfile()` sonucu sıfır problem olmalıdır.
4. Yeni fingerprint launch authority dokümanına ve `V1LaunchTuningContractTests` guard'ına yazılır.
5. Targeted EditMode ve full regression çalıştırılır.

Provider endpoint, API key veya SDK bu asset'e yazılmaz. Gameplay tuning asset'i de bu panelden
otomatik değiştirilmez.
