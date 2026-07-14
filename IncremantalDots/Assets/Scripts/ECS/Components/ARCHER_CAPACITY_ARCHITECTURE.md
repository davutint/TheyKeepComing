# Archer Capacity Architecture

## Ürün sözleşmesi

Basic, Rapid ve Frost okçular tek bir ortak kapasiteyi paylaşır. Aktif dünyadaki
`Prefab` olmayan bütün `ArcherUnit` entity'lerinin toplamı en fazla `1000` olabilir.
Type başına ayrı cap yoktur; population maliyeti ve type dağılımı bu sayısal sınırdan
ayrı kurallardır.

## Tek owner

`ArcherCapacityUtility.MaxTotalArchers = 1000` sayısal sözleşmenin tek owner'ıdır.
Utility kalan kapasiteyi, bir miktarın eklenip eklenemeyeceğini ve izin verilen ek
miktarı int-safe biçimde hesaplar.

`GameManager.SpawnArcher`, gerçek non-prefab `ArcherUnit` entity sayısını okuyup her
spawn'dan hemen önce bu utility'yi çağıran merkezi runtime kapısıdır.

## Runtime yolları

| Kaynak | Ortak kapıya giden yol |
|---|---|
| Archer drawer satın alma | `CanBuyArcher` erken kontrolü → transaction → `SpawnArcher` son kontrolü |
| Council ücretsiz okçu | `ApplyCouncilEffects` → `SpawnArcher`; cap'te loop biter |
| Meta başlangıç garnizonu | `ApplyMetaProgressionAtRunStart` → `SpawnArcher`; cap'te loop biter |
| Checkpoint/Exact Continue | `RestoreArcherCountsWithinCapacity` → `SpawnArcher` |
| İlk run seed / Restart | `SpawnArcher` |
| Legacy Barracks sızıntısı | `BarracksTrainingSystem`, aktif eğitimleri rezerve slot sayıp aynı utility'yi kullanır |

Satın alma yolu cap doluyken `CanBuyArcher=false` döndürür; kaynak ve population
transaction'ı başlamaz. Spawn ile transaction arasında dünya değişirse ikinci merkezi
kontrol spawn'ı reddeder ve mevcut satın alma rollback'i kaynakları geri verir.

## Restore ve save

Cap yeni save alanı değildir. Save Basic/Rapid/Frost type count'larını tutmaya devam
eder. Restore hedefleri negatif ve aşırı değerler için `0..1000` aralığına çekilir;
Basic → Rapid → Frost sırasıyla tamamlanır ve toplam cap dolduğu anda durur. Böylece
eski veya bozuk snapshot milyarlarca başarısız spawn denemesi üretemez.

## UI

Archer recruitment drawer cap dolduğunda satın alma butonlarını kilitler ve
`ARMY CAP 1000/1000` / `MAX` gösterir. Free Economy Test Mode ortak cap'i bypass etmez.

## Doğrulama

- `ArcherCapacityUtilityTests`: 999/1000/1001 sınırları ve bulk clamp.
- `ArcherCapacityPlayModeTests.CommonCap_BlocksPurchaseCouncilRestoreAndCentralSpawnWithoutSpending`:
  gerçek `NewGameScene` içinde 1000. spawn kabulü, 1001. spawn/satın alma reddi,
  Council ve restore guard'ı, kaynak/population değişmezliği.

