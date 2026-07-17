# Meta Progression Canonical Schema - Editor Setup

## Kurulum

Meta v3 schema için Inspector, prefab veya scene alanı eklenmez. `MetaProgression` statik disk
sahibidir ve `persistentDataPath/meta_progress.json` kullanır. Aktif `NewGameScene`
`GameManager` binding'i Unity MCP ile `MetaUpgradeCatalog.asset` yolunu göstermelidir.

Unity MCP denetiminde aktif katalog şu exact sıradaki 11 run-graph-isolated upgrade'i taşır:
`start_wood`, `start_stone`, `start_iron`, `start_food`, `start_archers`, `start_beds`, `wall_hp`,
`production`, `arrow_efficiency`, `essence_gain`, `node_pool_unlock`.

Legacy `Meta_start_moat.asset`, `Meta_archer_damage.asset`, `StartingTechLevel`,
`ArcherDamagePercent` ve `MetaUpgradeSO.TechNodeId` kaldırılmıştır. Numeric `3` ve `5` yeni effect
için yeniden kullanılmaz. `node_pool_unlock` definition'ı `MaxLevel=1` ve stable
`heart.approved_bonus_pool.v1` Id'si taşır; gerçek node/evolution content'i ayrı owner onayıyla
bu pool'a bağlanır ve mevcut run graph'ına retroaktif enjekte edilemez.

Kaynak/yatak definition'larında `MaxLevel=0` repeatable anlamına gelir. UI bunları `LV N` olarak
gösterir; `MAX` yalnız pozitif hard cap'e ulaşıldığında görünür. Bütün fiyatlar assetteki
`BaseCost` ve `CostGrowthPerLevel` üzerinden üstel hesaplanır.

`MetaUpgradeCatalog.asset > Reward Settings` production ölüm ödülünün tek tuning sahibidir.
Varsayılan kill bandları `100 / 1000`, ağırlıklar `1 / 0.25 / 0.05`; day/night/population/record
ağırlıkları `10 / 25 / 0.2 / 50`dir. Bu alanları ve 11 definition'ın fiyat/etkilerini
`Window > DeadWalls > Difficulty Tuner > Meta Runtime Contract` üzerinden birlikte denetle.
Panel değerleri `DifficultyProfileSO`'ya kopyalamaz.

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

Package I consumer flag'leri `tutorial.v1.worker_ratio`, `tutorial.v1.basic_archer`,
`tutorial.v1.low_ammo`, `tutorial.v1.heart`, `tutorial.v1.council`, `tutorial.v1.repair`,
`tutorial.v1.ability_key` ve turetilmis `tutorial.v1.complete` flag'idir. Id'ler yayınlandıktan
sonra yeniden adlandırılmaz; `FirstRunOnboardingUI` yalniz basarili ilgili player action'inda
adim flag'ini yazar. Settings reset canonical sekizliyi birlikte temizler.

Inspector'dan elle JSON üretme. Stable Id'lerde case ve yazım değişikliği yeni kimlik sayılır;
upgrade/pool/tutorial içerik sahibi Id'yi yayımlandıktan sonra değiştirmemelidir.

## Migration kabulü

EditMode'da `MetaProgressionSchemaTests` çalıştır:

- Eksik dosya canonical writable v3 state oluşturur.
- v1 ve v2 state, currency/istatistik/upgrade/receipt kaybetmeden v3'e taşınır.
- Bilinmeyen future version ile corrupt JSON yazmayı kilitler ve orijinal dosyayı korur.
- Duplicate/negatif alanlar deterministic normalize edilir; bilinmeyen upgrade Id'si korunur.
- Pool unlock ve tutorial flag atomik save/reload round-trip yapar.
- V2 death receipt exact reward quote ve peak population değerini round-trip taşır; recovery
  tuning'i yeniden okumadan aynı amount'u uygular.
- Tutorial reset exact sekiz onboarding flag'ini tek save'de temizler; future tutorial flag,
  pool unlock, Souls ve diger meta state reload sonrasinda korunur.
- First Day worker ratio PlayMode testi gerçek UI action'ının flag yazdığını ve test öncesi
  `meta_progress.json` / temp dosyalarının byte-for-byte geri yüklendiğini doğrular.
- Basic Archer affordability PlayMode testi yetersiz kaynaktan gerçek buy-ready state'e geçişi,
  başarılı satın alma flag'ini ve aynı meta dosyası geri-yükleme sınırını doğrular.
- Low Ammo PlayMode testi inclusive `%25` threshold'u, panelin zorla acilmamasini, basarisiz
  refill'in flag yazmamasini, basarili refill flag'ini ve ayni meta geri-yukleme sinirini dogrular.

Mevcut persistence regresyonu için `RunPersistenceTests`, Grave Essence meta ayrımı ve aktif
catalog dormancy testleri birlikte çalıştırılır. PlayMode'da exact Continue ile lethal save guard
yeniden doğrulanır.

Boundary kabulü için ayrıca:

- EditMode `MetaProgressionBoundaryTests`: death-only kural matrisi, exact 11 definition sırası,
  legacy numeric `3/5` reddi, üstel repeatable maliyet ve tek-seferlik pool contract denetimi.
- EditMode `MetaTuningContractTests`: diminishing 10K quote, non-record breakdown, invalid band
  reddi, quoted idempotency ve production 11-definition maliyet/etki tablosunu denetler.
- PlayMode `ExactRunContinuePlayModeTests.MetaPurchase_ActiveRunRejectedAndDurableDeathAllowsCanonicalUpgrade`:
  aktif koşuda red, durable ölümden sonra canonical satın alma, atomik pool unlock ve aynı Id'li
  spoof asset reddi.
- PlayMode `ExactRunContinuePlayModeTests.MetaCatalog_RunStartEffectsRemainSeparateAndExactAcrossContinue`:
  dört kaynak, Basic-only garnizon, yatak fiyat sınırı, Wall/production, Arrow efficiency ve
  kesirli Essence gain etkilerinin yeni koşu/Continue davranışı.
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
- Catalog validation legacy numeric `3/5` bildirirse stale StartingTech/ArcherDamage asset'ini yeniden ekleme;
  ilgili upgrade tanımını run-graph-isolated effect modeline açıkça taşı.
- Migration sonrası dosya v3 değilse Console'daki atomik write hatasını ve dosya kilidini denetle.

Testler gerçek meta dosyasını yedekler, izole fixture kullanır ve teardown sonunda geri yükler.
Test koşarken `meta_progress.json` dosyasını başka bir programda kilitli tutma.
