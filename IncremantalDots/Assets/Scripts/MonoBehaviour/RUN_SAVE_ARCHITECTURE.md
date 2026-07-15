# Exact Run Snapshot & Continue - Mimari

## Otorite ve oyuncu sözleşmesi

V1 Blueprint kararı: koşu yalnız Wall `0 HP` olduğunda biter. Oyuncu ana menüye dönebilir veya uygulamayı kapatabilir; Continue aynı koşunun aynı anını geri yükler. Aktif koşu varken gönüllü New Run/Restart yolu sunulmaz.

`RunPersistence.cs` içindeki `RunSaveState` bu sözleşmenin disk şemasıdır. Güncel sürüm `v11`, desteklenen en eski sürüm `v3` tür. Eski v2 Dawn-checkpoint kayıtları exact state içermediği için Continue olarak gösterilmez. v3-v8 zinciri worker, bed, bina, formation, Arrow ve Grave Essence state'ini açık migration'larla v9'a taşır. v9 snapshot'larda generated Heart graph bulunmadığı için v10 migration bu alanı `null` bırakır; source catalog'dan graph uydurmaz veya reroll etmez. v10 chance/pity Council state'i v11 exact `LastRegularCouncilDay` takvimine kanıt-temelli migrate edilir.

Eski `v5` snapshot'larda bed state yazılmış olsa bile legacy bedelsiz growth nedeniyle population kayıtlı yataktan büyük olabilir. Restore bu durumda mevcut nüfusu silmez; `BedBaseCapacity` değerini population-safe minimuma yükseltir. Runtime'da `MobilePopulationEconomySystem`, `PopulationState.Capacity` aynasını restore edilen toplam yataktan yeniden kurar. v5'te bulunmayan Wood/Stone/Iron/Food ve v7'de bulunmayan Arrow Capacity/Efficiency seviyeleri açık migration ile sıfır başlar.

Disk çıktısı compact JSON'dur. Pretty-print kullanılmaz; özellikle 10K combat snapshot'ında whitespace dosya boyutu ve senkron I/O maliyeti üretmemelidir. Bu yalnız fiziksel yazım biçimidir; `v11` alan şeması ve `JsonUtility` Continue uyumluluğu değişmez.

## Kayıt anları

- `PauseMenuUI.MainMenu`: sahne değişmeden hemen önce `GameManager.SaveRunSnapshot()` çağrılır. Kayıt başarısızsa ana menüye geçilmez.
- `GameManager.OnApplicationQuit`: koşu canlı ve Game Over değilse exact snapshot alınır.
- Dawn otomatik checkpoint'i yoktur. Faz değişimi kayıt anı değildir.
- Game Over: önce death receipt yazılır, sonra canlı run save geçersiz kılınır/silinir.

## Exact snapshot kapsamı

Kaydedilen state, oyuncunun aynı ana dönmesini etkileyen runtime verisidir:

- Run identity, gün/cycle index, phase, exact cycle timer ve progress değerleri.
- Wave state, spawn timer/budget ve `SpawnRandomState`.
- Wood/Stone/Iron/Food, kesirli üretim accumulator'ları; Arrow current, Capacity/Efficiency seviyeleri ve legacy accumulator; run-only Grave Essence bakiyesi.
- Castle Heart graph version/catalog version/seed, node Id/branch/depth, edge, hidden/reveal, level ve exact Keystone lock state'i.
- Population/legacy capacity; `BedBaseCapacity` ve `PurchasedBedCapacity`; actual worker dağılımı; target ratio, etkin worker cap ve idle aynaları; sekiz worker bina Capacity/Efficiency seviyesi; arrival checkpoint'i ve Dawn/event tekrarını önleyen last-marker alanları.
- Wall current HP, archer sayıları/level state'i, `ArcherFormationVersion`, tech node level'ları ve legacy upgrade tier'ları.
- Council regular handled day, hafıza, salt, cap bonusları, `HasActiveCouncilEvent` discriminator'ı, aktif kart ve seçenek/effect içeriği. v10 cooldown/pity alanları yalnız migration girdisidir.
- Fireball cooldown'u ve aktif Fireball projectile; Fortify/Rally ve süreli economy/horde effect state'i.
- Aktif zombie ve arrow entity'lerinin kompakt combat state'i. Arrow hedefleri zombie snapshot index'iyle tutulur.

Definition asset'lerden güvenle yeniden üretilebilen legacy tech aggregate'leri ve archer formation world pozisyonları kaydedilmez. Castle Heart graph'i definition asset'ten yeniden üretilmez; exact DTO restore edilir ve purchased node effect'leri level state'inden replay edilir. Archer formation kaydedilen `ArcherFormationVersion` ve type count'larla aynı deterministik 40 x 25 algoritmadan tekrar üretilir.

Dawn survivor transaction'ında düşülen Food, resource snapshot'ının parçasıdır. Aynı transaction'ın `LastPopulationGrowthCycle/LastPopulationGrowthWave` marker'ı da allocation snapshot'ında saklandığı için Continue aynı Dawn'ı yeniden oynatıp ikinci kez population veya Food değişikliği yapmaz.

`SurvivorArrivalVisual` entity'leri transient world-space sunumdur; snapshot'a yazılmaz. Restore edilen tamamlanmış growth marker'ı `GameManager` tarafından yeni arrival olarak yorumlanmaz, bu nedenle Continue survivor yürüyüşünü ikinci kez oynatmaz.

## Determinizm

`WaveStateData.SpawnRandomState` spawn RNG stream'inin sahibidir. `WaveSpawnSystem` her batch öncesi bu state'ten `Unity.Mathematics.Random` kurar ve batch sonunda güncel state'i tekrar component'e yazar. Böylece Continue sonrasındaki spawn konumları kapanmadan önceki stream'den devam eder.

## Restore sırası

`GameManager.TryRestoreRunFromCheckpoint()` şu sırayı korur:

1. Geçerli `v3`-`v11` snapshot yüklenir; legacy state açık migration zinciriyle in-memory v11'e yükseltilir. Saved Heart graph varsa temiz runtime kurulmadan önce catalog/version/structure/state preflight'i yapılır.
2. Aynı `RunId` ve worker bina yatırım state'i geri alınır; tech seviyeleri maliyetsiz uygulanıp base + Heart + Council + Meta + bina aggregate'leri kurulur.
3. Council hafızası, `LastRegularCouncilDay` ve discriminator ile doğrulanmış aktif Council kartı aynen yüklenir; regular kart yalnız restore edilen gün scheduled ve henüz handled değilse açılır.
4. `ArcherFormationVersion` yüklenir; mevcut başlangıç okçuları aynı formation cache'ine taşınır, ardından archer level/count state'i, kaynaklar, finite Arrow paid state'i ve Grave Essence bakiyesi geri yazılır. Exact Heart graph effect'leri deferred pipeline ile replay edilir; Arrow effective capacity son aggregate sonrasında bir kez clamp edilir.
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
- `RunPersistenceTests.TryLoad_Version3Snapshot_MigratesWorkerAllocationBedBuildingFormationAndAmmoStateToCurrent`
- `RunPersistenceTests.TryLoad_Version4UnlimitedCapacity_MigratesToPopulationSafeBedBase`
- `RunPersistenceTests.TryLoad_Version5Snapshot_MigratesToCleanWorkerBuildingLevels`
- `RunPersistenceTests.TryLoad_Version6Snapshot_MigratesToFormationVersion1`
- `RunPersistenceTests.TryLoad_Version8Snapshot_MigratesToZeroGraveEssence`
- `RunPersistenceTests.TryLoad_Version9Snapshot_DoesNotInventMissingHeartGraph`
- `RunPersistenceTests.TryLoad_Version10ChanceFailure_DoesNotConsumeScheduledRegularCouncil`
- `RunPersistenceTests.TryLoad_Version10ProducedEvent_PreservesHandledScheduledDay`
- `RunPersistenceTests.CommitDeath_DeletesRunSnapshotContainingGraveEssence`
- `ExactRunContinuePlayModeTests.Continue_RestoresSameCyclePhaseTimerResourcesAndSpawnRng` actual worker ve target ratio state'ini de doğrular.
- `ExactRunContinuePlayModeTests.GraveEssence_UsesHeartTransactionPersistsOnContinueAndResetsWithRun`
- `HeartGraphContinuePlayModeTests.Continue_ReplaysExactSavedHeartGraphWithoutReroll`
- `CouncilRegularSchedulePlayModeTests.RegularCouncil_OpensExactlyOnThreeSixNineCadence_OncePerDay`
- `CouncilRegularSchedulePlayModeTests.ActiveRegularCouncil_ContinueRestoresExactPayloadMemoryAndHandledDay`
- `CouncilRegularSchedulePlayModeTests.ChosenRegularCouncil_ContinueRestoresDecisionAndTimedEffects`
- `ArcherFormationPlayModeTests.FormationV1_BuildsStableThousandPointsAndContinueUsesSameLayout`
- `ExactRunContinuePlayModeTests.BedCapacityPurchase_SpendsWoodAndPersistsAcrossExactContinue`
- `ExactRunContinuePlayModeTests.WorkerBuildingInvestments_SpendBothResourcesAndPersistAcrossExactContinue`
- Runtime kabulü ayrıca Main Menu save, uygulama kapanışı, aynı phase/timer restore, aktif projectile restore ve Wall ölümü sırasında force-close senaryolarını kapsar.
