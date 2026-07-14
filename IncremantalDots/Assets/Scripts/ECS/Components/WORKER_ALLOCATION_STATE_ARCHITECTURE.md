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

`WorkerAllocationUtility.SetTargetRatioBps`, secilen resource hedefini exact
`0-10.000` araliginda tutar ve kalan basis-point'leri diger uc hedefe mevcut
oranlariyla, largest-remainder ve sabit resource onceligiyle dagitir. Diger uc
hedefin tamami sifirsa kalan pay esit ve deterministik dagilir. Secilen hedef
`10.000` ise diger hedefler sifir olur.

Player-facing drawer bu kurali `+1%`, `+10%`, `+100%` ve `0-100` direct input
ile cagirir. Bu mutasyon actual worker sayilarini degistirmez; worker gorsel
senkronu ancak sonraki population gelisi actual count'u degistirdiginde calisir.

İlk authoring değerleri gerçek worker dağılımından türetilir. NewGameScene'in `20 / 10 / 8 / 15` dağılımı bu nedenle `3774 / 1887 / 1509 / 2830` olarak başlar.

## Yeni Nüfus Dağıtımı

İlk runtime gözlemi yalnız baseline kurar; mevcut idle population zorla worker'a çevrilmez. Sonraki frame'lerde yalnız `PopulationState.Total - LastObservedPopulation` pozitif farkı otomatik dağıtıma girer.

Her yeni kişi, mevcut worker dağılımında hedefinden en fazla geride kalan ve cap'i dolmamış resource'a deterministik olarak atanır. Hedef oranı sıfır olan resource otomatik aday değildir. Pozitif hedeflerin tamamı doluysa kalan kişiler idle kalır.

Cap değeri `0`, mevcut sistem sözleşmesinde uncapped anlamına gelir. Aktif NewGameScene her dört resource için pozitif cap kullanır.

## Save ve Migration

Run save `v6`; gerçek worker sayıları, hedef oranlar, cap aynaları, idle aynası ve population checkpoint'ini birlikte capture/restore eder. Desteklenen `v3` exact snapshot yüklenirken hedef oranlar kayıtlı gerçek worker sayılarından deterministik olarak türetilir; idle ve checkpoint population snapshot'ından kurulur. Ardından v4 -> v5 bed-state ve v5 -> v6 worker-building state migration'ı uygulanır.

Worker görselleri hedef oran değiştiğinde yeniden üretilmez. Actual worker sayisi
`WorkerVisualRepresentationUtility` ile temsili sayiya cevrilir; goruntu senkronu
yalniz temsili count degistiginde tetiklenir. Resource basina visual cap `32`'dir.

## Doğrulama

- `WorkerAllocationUtilityTests`: normalize, exact hedef mutation, ilk baseline, deterministik dağıtım ve cap overflow.
- `RunPersistenceTests.TryLoad_Version3Snapshot_MigratesWorkerAllocationBedAndBuildingStateToVersion6`: v3 worker state + v5 bed + v6 worker-building state geçişi.
- `WorkerAllocationPlayModeTests`: gerçek NewGameScene'de yeni nüfus dağıtımı, idle overflow ve drawer hedef kontrollerinin actual worker'lari tasimama contract'i.
- `ExactRunContinuePlayModeTests`: actual worker ve target ratio exact Continue round-trip.
