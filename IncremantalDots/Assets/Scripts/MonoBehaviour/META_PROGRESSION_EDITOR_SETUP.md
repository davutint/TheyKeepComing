# Meta Progression Canonical Schema - Editor Setup

## Kurulum

Meta v3 schema için Inspector, prefab veya scene alanı eklenmez. `MetaProgression` statik disk
sahibidir ve `persistentDataPath/meta_progress.json` kullanır. Aktif `NewGameScene`
`GameManager` binding'i Unity MCP ile `MetaUpgradeCatalog.asset` yolunu göstermelidir.

Bu paket production upgrade catalog içeriğini değiştirmez. Unity MCP denetiminde aktif katalog
altı upgrade taşır; dormant `Meta_start_moat.asset` catalog dışında kalır. `StartingTechLevel`
enum/content temizliği ve death-only purchase guard ayrı tracker işidir.

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

## Manuel teşhis

- `LoadStatus = UnsupportedVersion`: dosyayı otomatik downgrade etme. Desteklenen explicit
  migration eklenmeden Save/upgrade/reward commit edilmez.
- `LoadStatus = Corrupt`: gerçek dosya korunur. Yedekten geri dön veya doğrulanmış repair aracı
  kullan; boş state'i dosyanın üzerine elle yazma.
- `CanPersist = false`: death receipt'in kalması beklenen davranıştır; meta durable olmadan
  transaction tamamlanmış sayılmaz.
- Migration sonrası dosya v3 değilse Console'daki atomik write hatasını ve dosya kilidini denetle.

Testler gerçek meta dosyasını yedekler, izole fixture kullanır ve teardown sonunda geri yükler.
Test koşarken `meta_progress.json` dosyasını başka bir programda kilitli tutma.
