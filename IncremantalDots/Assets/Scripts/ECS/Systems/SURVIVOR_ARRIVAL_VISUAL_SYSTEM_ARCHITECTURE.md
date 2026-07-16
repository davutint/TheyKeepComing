# Survivor Arrival Visual System - Mimari

## Amaç ve sınır

`SurvivorArrivalVisualSystem`, Dawn'da kabul edilmiş yeni nüfusu sağdaki savaş alanından Wall arkasına yürüyen geçici DOTS villager'larla temsil eder. Bu akış yalnız sunum katmanıdır. Population artışı ve tek seferlik Food harcaması daha önce `MobilePopulationEconomySystem` transaction'ında kesinleşir; görsel entity'ler bu değerleri değiştirmez.

## Veri sahibi ve oluşturma akışı

1. `MobilePopulationEconomySystem`, yeni Dawn için `LastPopulationGrowthCycle/LastPopulationGrowthWave` marker'ını ve `LastArrivalAcceptedCount` sonucunu yazar.
2. `GameManager.ReadECSData()`, daha önce göstermediği pozitif marker'ı görür.
3. `WorkerPrefabData.WorkerPrefab` içindeki mevcut `VillagerWorker` prefabı instantiate edilir.
4. Arrival entity'lerinden `ResourceWorkerVisual`, `WorkerLogisticsRoute` ve `WorkerLogisticsFeedbackState` kaldırılır. Böylece worker üretimi, allocation sync'i ve kaynak taşıma rotaları bu entity'leri sahiplenmez.
5. Entity'ye `SurvivorArrivalVisual` eklenir; `SurvivorArrivalVisualSystem` gecikme, hareket, yön animasyonu ve varışta destroy işlemini yürütür.

Yeni scene/prefab bağı yoktur. Arrival sunumu mevcut worker prefabı, atlası, material property'leri ve `MobileCastleCombatConfig.FrontlineX/CastleCenter` verisini yeniden kullanır.

## Temsil ve rota sözleşmesi

- Bir arrival en fazla `15` görsel entity üretir.
- Kabul edilen sayı `15` üzerindeyse `RepresentedSurvivorCount` değerleri gerçek accepted toplamını görsellere exact dağıtır.
- Spawn noktaları Wall'ın `9.5` world unit sağında, beş yatay lane ve küçük satır/X farklarıyla üretilir.
- Hedef Wall'ın `0.8` unit arkasındadır; lane'ler kaleye yaklaşırken daralır.
- `3.0+` hareket hızı ve küçük start-delay farkları tek çizgi halinde yürümeyi engeller; en geç
  görsel canonical `5s` Dawn süresi bitmeden Wall arkasına ulaşır.
- Açık mavi tint arrival grubunu kaynak worker'larından ayırır; cargo, lantern ve delivery feedback'i kapalıdır.
- Hedef mesafesine giren entity aynı frame sonunda yok edilir.

Rota sabit bir waypoint asset'i değildir; mevcut tek cepheli battlefield için `SurvivorArrivalVisualUtility` tarafından deterministik üretilir.

Ana portcullis tile'ının açılıp kapanması bu ECS sisteminin sorumluluğu değildir;
`DawnRewardToastUI` aynı accepted marker'ın yalnız presentation kenarını kullanır.

## Save ve yeniden oynatma sınırı

Arrival entity'leri transient sunumdur ve exact run snapshot'a yazılmaz. Kaydedilen marker ve accepted transaction sonucu restore edildiğinde `GameManager` tamamlanmış Dawn'ı yeniden görselleştirmez. `RestartGame()` açık arrival entity'lerini temizler ve yerel görsel marker'ı sıfırlar.

## Doğrulama

- `SurvivorArrivalVisualUtilityTests`: visual cap, exact represented toplam, deterministik
  rota/hız/gecikme ve tüm bounded formasyonun Dawn içinde tamamlanması.
- `WorkerAllocationPlayModeTests.DawnArrivalTransaction_SpendsFoodOnceForAcceptedSurvivors`: gerçek Dawn transaction'ından entity üretimi, worker lojistiğinden izolasyon ve varışta destroy.
- `ExactRunContinuePlayModeTests.Continue_RestoresSameCyclePhaseTimerResourcesAndSpawnRng`: tamamlanmış saved Dawn'ın arrival görselini tekrar üretmemesi.
- Game View QA: gerçek `NewGameScene` içinde 15 survivor'ın sağdan Wall arkasına yaklaşması.
