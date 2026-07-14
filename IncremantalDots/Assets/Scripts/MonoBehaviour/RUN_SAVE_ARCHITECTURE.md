# Exact Run Snapshot & Continue - Mimari

## Otorite ve oyuncu sözleşmesi

V1 Blueprint kararı: koşu yalnız Wall `0 HP` olduğunda biter. Oyuncu ana menüye dönebilir veya uygulamayı kapatabilir; Continue aynı koşunun aynı anını geri yükler. Aktif koşu varken gönüllü New Run/Restart yolu sunulmaz.

`RunPersistence.cs` içindeki `RunSaveState` bu sözleşmenin disk şemasıdır. Güncel sürüm `v6`, desteklenen en eski sürüm `v3` tür. Eski v2 Dawn-checkpoint kayıtları exact state içermediği için Continue olarak gösterilmez. v3 exact snapshot'lar önce worker target-ratio state'iyle v4'e, ardından açık House bed state'iyle v5'e ve sekiz worker bina yatırım seviyesiyle v6'ya yükseltilir. v4 exact snapshot'lar mevcut population'ı karşılayan bir `BedBaseCapacity` ve sıfır purchased bed ile v5'e; v5 snapshot'lar sıfır bina yatırımıyla v6'ya migrate edilir.

Eski `v5` snapshot'larda bed state yazılmış olsa bile legacy bedelsiz growth nedeniyle population kayıtlı yataktan büyük olabilir. Restore bu durumda mevcut nüfusu silmez; `BedBaseCapacity` değerini population-safe minimuma yükseltir. Runtime'da `MobilePopulationEconomySystem`, `PopulationState.Capacity` aynasını restore edilen toplam yataktan yeniden kurar. v5'te bulunmayan Wood/Stone/Iron/Food Capacity/Efficiency seviyeleri açık migration ile sıfır başlar.

Disk çıktısı compact JSON'dur. Pretty-print kullanılmaz; özellikle 10K combat snapshot'ında whitespace dosya boyutu ve senkron I/O maliyeti üretmemelidir. Bu yalnız fiziksel yazım biçimidir; `v6` alan şeması ve `JsonUtility` Continue uyumluluğu değişmez.

## Kayıt anları

- `PauseMenuUI.MainMenu`: sahne değişmeden hemen önce `GameManager.SaveRunSnapshot()` çağrılır. Kayıt başarısızsa ana menüye geçilmez.
- `GameManager.OnApplicationQuit`: koşu canlı ve Game Over değilse exact snapshot alınır.
- Dawn otomatik checkpoint'i yoktur. Faz değişimi kayıt anı değildir.
- Game Over: önce death receipt yazılır, sonra canlı run save geçersiz kılınır/silinir.

## Exact snapshot kapsamı

Kaydedilen state, oyuncunun aynı ana dönmesini etkileyen runtime verisidir:

- Run identity, gün/cycle index, phase, exact cycle timer ve progress değerleri.
- Wave state, spawn timer/budget ve `SpawnRandomState`.
- Wood/Stone/Iron/Food, kesirli üretim accumulator'ları, Arrow current ve accumulator.
- Population/legacy capacity; `BedBaseCapacity` ve `PurchasedBedCapacity`; actual worker dağılımı; target ratio, etkin worker cap ve idle aynaları; sekiz worker bina Capacity/Efficiency seviyesi; arrival checkpoint'i ve Dawn/event tekrarını önleyen last-marker alanları.
- Wall current HP, archer sayıları/level state'i, tech node level'ları ve legacy upgrade tier'ları.
- Council hafızası, salt, cooldown/pity/cap bonusları, aktif kart ve seçenek/effect içeriği.
- Fireball cooldown'u ve aktif Fireball projectile; Fortify/Rally ve süreli economy/horde effect state'i.
- Aktif zombie ve arrow entity'lerinin kompakt combat state'i. Arrow hedefleri zombie snapshot index'iyle tutulur.

Definition asset'lerden güvenle yeniden üretilebilen tech aggregate'leri ve archer formation pozisyonları kaydedilmez. Tech seviyelerinden aggregate/reveal/spell state'i yeniden kurulur; archer formation mevcut deterministik yerleşim algoritmasından tekrar üretilir.

Dawn survivor transaction'ında düşülen Food, resource snapshot'ının parçasıdır. Aynı transaction'ın `LastPopulationGrowthCycle/LastPopulationGrowthWave` marker'ı da allocation snapshot'ında saklandığı için Continue aynı Dawn'ı yeniden oynatıp ikinci kez population veya Food değişikliği yapmaz.

`SurvivorArrivalVisual` entity'leri transient world-space sunumdur; snapshot'a yazılmaz. Restore edilen tamamlanmış growth marker'ı `GameManager` tarafından yeni arrival olarak yorumlanmaz, bu nedenle Continue survivor yürüyüşünü ikinci kez oynatmaz.

## Determinizm

`WaveStateData.SpawnRandomState` spawn RNG stream'inin sahibidir. `WaveSpawnSystem` her batch öncesi bu state'ten `Unity.Mathematics.Random` kurar ve batch sonunda güncel state'i tekrar component'e yazar. Böylece Continue sonrasındaki spawn konumları kapanmadan önceki stream'den devam eder.

## Restore sırası

`GameManager.TryRestoreRunFromCheckpoint()` şu sırayı korur:

1. Geçerli `v3`, `v4`, `v5` veya `v6` snapshot yüklenir; v3 worker oranları actual count'lardan türetilir, v3/v4 state açık bed alanlarıyla v5'e ve bütün eski state'ler bina yatırım alanlarıyla in-memory v6'ya yükseltilir. Ardından temiz runtime tabanı oluşturulur.
2. Aynı `RunId` ve worker bina yatırım state'i geri alınır; tech seviyeleri maliyetsiz uygulanıp base + Heart + Council + Meta + bina aggregate'leri kurulur.
3. Council hafızası ve aktif Council kartı aynen yüklenir; reroll yapılmaz.
4. Archer level/count state'i ve kaynak/population/allocation state'i geri yazılır.
5. Exact cycle phase/timer, wave state ve spawn RNG state'i geri yazılır. `CycleIndex + 1`, zorunlu Day veya timer `0` uygulanmaz.
6. Wall current HP, ability cooldown ve süreli effect state'i geri yüklenir.
7. Zombie'ler oluşturulur; ardından arrow hedefleri restore edilen zombie index'lerine bağlanır ve aktif Fireball kurulur.
8. ECS cache/UI state'i yenilenir.

## Ölüm transaction'ı ve idempotent meta ödülü

`run_death_receipt.json`, run save ile meta save arasındaki küçük transaction journal'ıdır:

1. Wall ölümü kesinleşince `{ RunId, Day, Kills }` receipt'i yazılır.
2. `run_save.json` silinir; bu run artık Continue edilemez.
3. `MetaProgression.AddRunResult(runId, day, kills)` çağrılır.
4. Meta save, ödüllendirilmiş son RunId'leri saklar. Aynı RunId tekrar gelirse Souls/istatistik ikinci kez yazılmaz.
5. Ödül doğrulandıktan sonra receipt silinir. İşlem ortasında uygulama kapanırsa bir sonraki açılış receipt'i idempotent biçimde tamamlar.

## Değişiklik kuralı

Yeni bir koşu state'i eklenirken üç sınır birlikte güncellenir:

- `RunSaveState` alanı,
- `GameManager.SaveRunSnapshot()` capture yolu,
- `GameManager.TryRestoreRunFromCheckpoint()` restore yolu.

Entity referansı doğrudan JSON'a yazılmaz. Referans gerekiyorsa compact stable identity/index kullanılır. Şema semantiği değişirse `CurrentVersion` artırılır ve migration açıkça yazılır; eksik exact state varsayımla doldurulmaz.

## Doğrulama

- `RunPersistenceTests.SchemaVersion_RejectsLegacyCheckpoint_AndAcceptsExactSnapshot`
- `RunPersistenceTests.JsonRoundTrip_PreservesExactCycleCombatCouncilAndAbilityState`
- `RunPersistenceTests.DeathReceipt_RoundTrip_PreservesRunIdentityAndRewardInputs`
- `RunPersistenceTests.Save_WritesCompactJson_AndRemainsLoadable`
- `RunPersistenceTests.TryLoad_Version3Snapshot_MigratesWorkerAllocationBedAndBuildingStateToVersion6`
- `RunPersistenceTests.TryLoad_Version4UnlimitedCapacity_MigratesToPopulationSafeBedBase`
- `RunPersistenceTests.TryLoad_Version5Snapshot_MigratesToCleanWorkerBuildingLevels`
- `ExactRunContinuePlayModeTests.Continue_RestoresSameCyclePhaseTimerResourcesAndSpawnRng` actual worker ve target ratio state'ini de doğrular.
- `ExactRunContinuePlayModeTests.BedCapacityPurchase_SpendsWoodAndPersistsAcrossExactContinue`
- `ExactRunContinuePlayModeTests.WorkerBuildingInvestments_SpendBothResourcesAndPersistAcrossExactContinue`
- Runtime kabulü ayrıca Main Menu save, uygulama kapanışı, aynı phase/timer restore, aktif projectile restore ve Wall ölümü sırasında force-close senaryolarını kapsar.
