# Worker Allocation State Architecture

## Otorite

`MobilePopulationAllocation`, dört hazır üretim alanının worker state otoritesidir. Her resource için şu verileri birlikte tutar:

- Gerçek worker sayısı.
- `10.000` basis-point toplamına normalize edilen hedef oran.
- `MobileCastleCombatConfig` içindeki etkin worker cap'inin runtime aynası.
- Hesaplanmış idle population aynası.
- Yeni nüfusu bir kez işlemek için `LastObservedPopulation` checkpoint'i.

`PopulationState` toplam population, toplam worker, toplam archer ve idle aggregate'lerini tutar. Resource başına dağılım yalnız `MobilePopulationAllocation` içindedir.

## Hedef Oran Sözleşmesi

`WorkerAllocationUtility.NormalizeTargetRatios` negatif girdileri sıfıra çeker ve oranları toplam `10.000` olacak şekilde integer largest-remainder yöntemiyle normalize eder. Eşit kalanlarda sabit öncelik Wood, Stone, Iron, Food sırasıdır. Bütün girdiler sıfırsa dengeli `2500 / 2500 / 2500 / 2500` hedefi kullanılır.

İlk authoring değerleri gerçek worker dağılımından türetilir. NewGameScene'in `20 / 10 / 8 / 15` dağılımı bu nedenle `3774 / 1887 / 1509 / 2830` olarak başlar.

## Yeni Nüfus Dağıtımı

İlk runtime gözlemi yalnız baseline kurar; mevcut idle population zorla worker'a çevrilmez. Sonraki frame'lerde yalnız `PopulationState.Total - LastObservedPopulation` pozitif farkı otomatik dağıtıma girer.

Her yeni kişi, mevcut worker dağılımında hedefinden en fazla geride kalan ve cap'i dolmamış resource'a deterministik olarak atanır. Hedef oranı sıfır olan resource otomatik aday değildir. Pozitif hedeflerin tamamı doluysa kalan kişiler idle kalır.

Cap değeri `0`, mevcut sistem sözleşmesinde uncapped anlamına gelir. Aktif NewGameScene her dört resource için pozitif cap kullanır.

## Save ve Migration

Run save `v4`; gerçek worker sayıları, hedef oranlar, cap aynaları, idle aynası ve population checkpoint'ini birlikte capture/restore eder. Desteklenen `v3` exact snapshot yüklenirken hedef oranlar kayıtlı gerçek worker sayılarından deterministik olarak türetilir; idle ve checkpoint population snapshot'ından kurulur.

Worker görselleri hedef oran değiştiğinde yeniden üretilmez. Görsel senkron yalnız gerçek worker sayıları değiştiğinde tetiklenir.

## Doğrulama

- `WorkerAllocationUtilityTests`: normalize, ilk baseline, deterministik dağıtım ve cap overflow.
- `RunPersistenceTests.TryLoad_Version3Snapshot_MigratesWorkerAllocationToVersion4`: v3 -> v4 geçişi.
- `WorkerAllocationPlayModeTests`: gerçek NewGameScene'de yeni nüfus dağıtımı ve idle overflow.
- `ExactRunContinuePlayModeTests`: actual worker ve target ratio exact Continue round-trip.
