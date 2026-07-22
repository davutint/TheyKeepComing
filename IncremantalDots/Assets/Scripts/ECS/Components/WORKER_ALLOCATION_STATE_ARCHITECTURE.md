# Worker Allocation State Architecture

## Otorite

`MobilePopulationAllocation`, dört hazır üretim alanının worker state otoritesidir. Her resource için şu verileri birlikte tutar:

- Gerçek worker sayısı.
- `10.000` basis-point toplamına normalize edilen hedef oran.
- `MobileCastleCombatConfig` içindeki etkin worker cap'inin runtime aynası.
- Hesaplanmış unassigned population aynası; asker rezervi değildir.
- Yeni nüfusu bir kez işlemek için `LastObservedPopulation` checkpoint'i.

`PopulationState` toplam population, toplam worker, toplam archer ve idle aggregate'lerini tutar. Resource başına dağılım yalnız `MobilePopulationAllocation` içindedir.

## WorkerAllocation contract (V1)

Tracker'daki `WorkerAllocation` yeni veya paralel bir state değildir. Canlı contract dört owner'ın
tek yönlü zinciridir:

1. **State owner:** `MobilePopulationAllocation`, dört resource target ratio'sunu, dört actual
   worker sayısını, dört effective cap aynasını ve idle mirror'ı taşır.
2. **Matematik owner:** `WorkerAllocationUtility`, ratio toplamını exact `10.000` basis point'e
   normalize eder, bütün sivil nüfusu deterministik dağıtır, target kapasitesi dolduğunda
   Wood -> Stone -> Iron -> Food sırasındaki ilk müsait resource'a overflow atar ve asker
   üretiminde worker'ı aynı sırayla eksiltir.
3. **Runtime transaction owner:** `MobilePopulationEconomySystem`, effective cap'leri
   `MobileCastleCombatConfig` aggregate'inden state'e aynalar; actual count'ları population ve
   cap sınırına çeker; `PopulationState.Workers/Idle` ile allocation idle mirror'ını aynı frame
   tek utility sonucundan yazar.
4. **Player API/presentation mirror:** `GameManager` ve worker UI target mutation API'sini çağırır
   ve authoritative state'i gösterir. Target değişikliği, archer olmayan bütün nüfusu anında
   kapasite-aware biçimde yeniden dağıtır.

Cap baseline'i authoring/config'ten gelir; Heart, Council, Meta ve worker-building etkileri
`MobileCastleCombatConfig` effective aggregate'ine uygulanır. Allocation bu aggregate'i yalnız
runtime mirror olarak taşır ve ikinci kez bonus uygulamaz. `cap = 0` uncapped semantiği korunur.

Unassigned ayrı bir harcanabilir state veya asker rezervi değildir. Canonical formül
`WorkerAllocationUtility.ResolveIdlePopulation` içindedir; `PopulationState.Idle`,
`MobilePopulationAllocation.IdlePopulation` ve `GameManager.GetIdlePopulation()` aynı sonucu
tüketir. Değer yalnız dört resource'un toplam boş kapasitesi sivil nüfusu alamadığında pozitif
kalır. Archer alımı normal durumda Wood -> Stone -> Iron -> Food sırasıyla resource worker'ı
Archer havuzuna taşır.

## Hedef Oran Sözleşmesi

`WorkerAllocationUtility.NormalizeTargetRatios` negatif girdileri sıfıra çeker ve oranları toplam `10.000` olacak şekilde integer largest-remainder yöntemiyle normalize eder. Eşit kalanlarda sabit öncelik Wood, Stone, Iron, Food sırasıdır. Bütün girdiler sıfırsa dengeli `2500 / 2500 / 2500 / 2500` hedefi kullanılır.

`WorkerAllocationUtility.SetTargetRatioBps`, secilen resource hedefini exact
`0-10.000` araliginda tutar ve kalan basis-point'leri diger uc hedefe mevcut
oranlariyla, largest-remainder ve sabit resource onceligiyle dagitir. Diger uc
hedefin tamami sifirsa kalan pay esit ve deterministik dagilir. Secilen hedef
`10.000` ise diger hedefler sifir olur.

Player-facing drawer bu kuralı `0-100` slider input ile çağırır. Mutasyon bütün kullanılabilir
sivil nüfusu anında yeniden dağıtır. Seçilen hedef kendi cap'ine çarparsa sıfır hedefli sıradaki
resource overflow worker alabilir; UI bunu `CAPACITY OVERFLOW` olarak açıklar.

İlk authoring değerleri gerçek worker dağılımından türetilir. NewGameScene'in `20 / 10 / 8 / 15` dağılımı bu nedenle `3774 / 1887 / 1509 / 2830` olarak başlar.

## Yeni Nüfus Dağıtımı

İlk runtime gözleminden itibaren `PopulationState.Total - PopulationState.Archers` sivil havuzunun
tamamı, boş resource kapasitesi olduğu sürece işe atanır. Yeni arrival aynı kuralla anında mevcut
target dağılımına katılır.

Her kişi, mevcut worker dağılımında hedefinden en fazla geride kalan ve cap'i dolmamış resource'a
deterministik olarak atanır. Pozitif hedeflerin tamamı doluysa kalan kişiler sıfır hedefli
resource'lara Wood -> Stone -> Iron -> Food sırasıyla overflow olarak atanır. Unassigned yalnız
bütün resource kapasiteleri dolduğunda kalır.

Cap değeri `0`, mevcut sistem sözleşmesinde uncapped anlamına gelir. Aktif NewGameScene her dört resource için pozitif cap kullanır.

## Save ve Migration

Güncel run save `v14`; gerçek worker sayıları, hedef oranlar, cap aynaları, idle aynası ve population checkpoint'ini birlikte capture/restore eder. Desteklenen `v3` exact snapshot yüklenirken hedef oranlar kayıtlı gerçek worker sayılarından deterministik olarak türetilir; idle ve checkpoint population snapshot'ından kurulur. Sonraki migration zinciri bed-state, worker-building, archer formation, finite Arrow, Heart graph, Council ve pooled-enemy snapshot alanlarını ekler; worker contract alanlarını yeniden yorumlamaz.

Worker görselleri hedef oran değiştiğinde yeniden üretilmez. Actual worker sayisi
`WorkerVisualRepresentationUtility` ile temsili sayiya cevrilir; goruntu senkronu
yalniz temsili count degistiginde tetiklenir. Resource basina visual cap `32`'dir.

## Doğrulama

- `WorkerAllocationUtilityTests`: normalize, exact hedef mutation, bütün sivil havuz dağıtımı,
  capacity overflow ve asker için deterministik worker eksiltme.
- `WorkerAllocationUtilityTests.WorkerAllocationContract_OwnsFourRatiosActualCountsCapsAndDerivedIdle`: dört contract kanalının ve canonical idle formülünün saf kanıtı.
- `RunPersistenceTests.TryLoad_Version3Snapshot_MigratesWorkerAllocationBedBuildingFormationAndAmmoStateToCurrent`: v3 worker state + v5 bed + v6 worker-building + v7 formation + v8 Arrow state geçişi.
- `WorkerAllocationPlayModeTests`: gerçek NewGameScene'de sıfır normal unassigned, capacity overflow,
  slider'ın bütün sivil havuzu taşıması ve asker alımının resource worker tüketmesi.
- `ExactRunContinuePlayModeTests`: actual worker ve target ratio exact Continue round-trip.
