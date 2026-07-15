# Meta Progression Canonical Schema - Editor Setup

## Kurulum

Meta v3 schema için Inspector, prefab veya scene alanı eklenmez. `MetaProgression` statik disk
sahibidir ve `persistentDataPath/meta_progress.json` kullanır. Aktif `NewGameScene`
`GameManager` binding'i Unity MCP ile `MetaUpgradeCatalog.asset` yolunu göstermelidir.

Unity MCP denetiminde aktif katalog altı run-graph-isolated upgrade taşır. Legacy
`Meta_start_moat.asset`, `StartingTechLevel` enum yolu ve `MetaUpgradeSO.TechNodeId` kaldırılmıştır.
Katalog validation yalnız `StartingResource`, `StartingArchers`, `WallHpPercent`,
`ArcherDamagePercent` ve `ProductionPercent` effect'lerini kabul eder.

Game Over `MetaProgressionUI`, satın alma için doğrudan `MetaProgression` çağırmaz;
`GameManager.CanBuyMetaUpgrade/TryBuyMetaUpgrade` binding'ini kullanır. Yeni bir meta UI/prefab
eklerken aynı API'yi kullan; yalnız paneli Game Over altında tutmak yeterli güvenlik değildir.

## Schema v3 alanları

- `Souls`, `TotalSoulsEarned`
- `BestDay`, `TotalRuns`, `TotalKillsAllTime`
- `Upgrades`
- `UnlockedPoolIds`
- `TutorialFlags`
- `RewardedRunIds`

Inspector'dan elle JSON üretme. Stable Id'lerde case ve yazım değişikliği yeni kimlik sayılır;
upgrade/pool/tutorial içerik sahibi Id'yi yayımlandıktan sonra değiştirmemelidir.

## Migration kabulü

EditMode'da `MetaProgressionSchemaTests` çalıştır:

- Eksik dosya canonical writable v3 state oluşturur.
- v1 ve v2 state, currency/istatistik/upgrade/receipt kaybetmeden v3'e taşınır.
- Bilinmeyen future version ile corrupt JSON yazmayı kilitler ve orijinal dosyayı korur.
- Duplicate/negatif alanlar deterministic normalize edilir; bilinmeyen upgrade Id'si korunur.
- Pool unlock ve tutorial flag atomik save/reload round-trip yapar.

Mevcut persistence regresyonu için `RunPersistenceTests`, Grave Essence meta ayrımı ve aktif
catalog dormancy testleri birlikte çalıştırılır. PlayMode'da exact Continue ile lethal save guard
yeniden doğrulanır.

Boundary kabulü için ayrıca:

- EditMode `MetaProgressionBoundaryTests`: death-only kural matrisi, effect allowlist'i, legacy
  numeric `3` reddi, production catalog ve public contract reflection denetimi.
- PlayMode `ExactRunContinuePlayModeTests.MetaPurchase_ActiveRunRejectedAndDurableDeathAllowsCanonicalUpgrade`:
  aktif koşuda red, durable ölümden sonra canonical satın alma ve aynı Id'li spoof asset reddi.
- PlayMode `HeartGraphContinuePlayModeTests.Continue_ReplaysExactSavedHeartGraphWithoutReroll`:
  meta boundary değişikliğinin saved generated graph replay'ini etkilemediğini doğrular.

## Manuel teşhis

- `LoadStatus = UnsupportedVersion`: dosyayı otomatik downgrade etme. Desteklenen explicit
  migration eklenmeden Save/upgrade/reward commit edilmez.
- `LoadStatus = Corrupt`: gerçek dosya korunur. Yedekten geri dön veya doğrulanmış repair aracı
  kullan; boş state'i dosyanın üzerine elle yazma.
- `CanPersist = false`: death receipt'in kalması beklenen davranıştır; meta durable olmadan
  transaction tamamlanmış sayılmaz.
- Game Over görünür fakat shop disabled ise `LastRunResult.Persisted`, `CanPersist` ve death
  receipt Console hatalarını kontrol et; UI üzerinden zorla satın alma açma.
- Catalog validation legacy numeric `3` bildirirse stale StartingTech asset'ini yeniden ekleme;
  ilgili upgrade tanımını run-graph-isolated effect modeline açıkça taşı.
- Migration sonrası dosya v3 değilse Console'daki atomik write hatasını ve dosya kilidini denetle.

Testler gerçek meta dosyasını yedekler, izole fixture kullanır ve teardown sonunda geri yükler.
Test koşarken `meta_progress.json` dosyasını başka bir programda kilitli tutma.
