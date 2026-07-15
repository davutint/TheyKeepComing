# Exact Critical State & Deterministic Continue - Mimari

## Otorite ve oyuncu sözleşmesi

V1 Blueprint kararı: koşu yalnız Wall `0 HP` olduğunda biter. Oyuncu ana menüye dönebilir veya uygulamayı kapatabilir; Continue aynı koşunun exact kritik state'ini ve perceptually faithful combat alanını geri yükler. Aktif koşu varken gönüllü New Run/Restart yolu sunulmaz.

`RunPersistence.cs` içindeki `RunSaveState` bu sözleşmenin disk şemasıdır. Güncel sürüm `v14`, desteklenen en eski sürüm `v3` tür. Eski v2 Dawn-checkpoint kayıtları exact state içermediği için Continue olarak gösterilmez. v3-v8 zinciri worker, bed, bina, formation, Arrow ve Grave Essence state'ini açık migration'larla v9'a taşır. v9 snapshot'larda generated Heart graph bulunmadığı için v10 migration bu alanı `null` bırakır; source catalog'dan graph uydurmaz veya reroll etmez. v10 chance/pity Council state'i v11 exact `LastRegularCouncilDay` takvimine kanıt-temelli migrate edilir. v11 kayıtları Rally ve Emergency Repair cooldown'ları hazır başlayacak biçimde açıkça v12'ye; v12 kayıtları meta Essence gain kesirli remainder'ı `0` olacak biçimde v13'e taşınır. v13 exact zombie listesi v14'e tahminle çevrilmez; legacy fallback olarak aynen korunur. İlk yeni v14 save, 10K alanı deterministic aggregate rebuild payload'ına geçirir.

Eski `v5` snapshot'larda bed state yazılmış olsa bile legacy bedelsiz growth nedeniyle population kayıtlı yataktan büyük olabilir. Restore bu durumda mevcut nüfusu silmez; `BedBaseCapacity` değerini population-safe minimuma yükseltir. Runtime'da `MobilePopulationEconomySystem`, `PopulationState.Capacity` aynasını restore edilen toplam yataktan yeniden kurar. v5'te bulunmayan Wood/Stone/Iron/Food ve v7'de bulunmayan Arrow Capacity/Efficiency seviyeleri açık migration ile sıfır başlar.

Disk çıktısı compact JSON'dur. Pretty-print kullanılmaz. v14 `24 x 16` spatial grid, `4` HP bandı ve canonical state/flag bucket'larıyla 10K zombie pozisyonunu entity başına yazmaz. Üç temiz 10K Player koşusunda `372-375` bucket, aktif Arrow'a bağlı `165.957-227.597 B`, save `31,42-32,93 ms` ve load-path restore `75,09-213,32 ms` ölçüldü; v13 karşılığı `4.240.003 B`, `52,58 / 86,19 ms` idi. Disk payload küçülmesi `%94,63-%96,09` aralığındadır; restore varyansı frame-time kazancı olarak yorumlanmaz. Yazım aynı klasördeki `.tmp` dosyasına flush edildikten sonra replace/move edilir.

## Kayıt anları

- `PauseMenuUI.MainMenu`: sahne değişmeden hemen önce `GameManager.SaveRunSnapshot()` çağrılır. Kayıt başarısızsa ana menüye geçilmez.
- `GameManager.OnApplicationQuit`: initialized runtime'da `SaveRunSnapshot()` çağrılır; metot taze ECS truth'inden yaşayan koşu ile ölüm transaction'ını ayırır.
- Dawn otomatik checkpoint'i yoktur. Faz değişimi kayıt anı değildir.
- Game Over: önce death receipt yazılır, sonra canlı run save geçersiz kılınır/silinir.

## Exact ve perceptual snapshot kapsamı

Kaydedilen state, oyuncunun aynı ana dönmesini etkileyen runtime verisidir:

- Run identity, gün/cycle index, phase, exact cycle timer ve progress değerleri.
- Wave state, spawn timer/budget ve `SpawnRandomState`.
- Wood/Stone/Iron/Food, kesirli üretim accumulator'ları; Arrow current, Capacity/Efficiency seviyeleri ve legacy accumulator; run-only Grave Essence bakiyesi.
- Castle Heart graph version/catalog version/seed, node Id/branch/depth, edge, hidden/reveal, level ve exact Keystone lock state'i.
- Population/legacy capacity; `BedBaseCapacity` ve `PurchasedBedCapacity`; actual worker dağılımı; target ratio, etkin worker cap ve idle aynaları; sekiz worker bina Capacity/Efficiency seviyesi; arrival checkpoint'i ve Dawn/event tekrarını önleyen last-marker alanları.
- Wall current HP, archer sayıları/level state'i, `ArcherFormationVersion`, tech node level'ları ve legacy upgrade tier'ları.
- Council regular handled day, hafıza, salt, cap bonusları, `HasActiveCouncilEvent` discriminator'ı, aktif kart ve seçenek/effect içeriği. v10 cooldown/pity alanları yalnız migration girdisidir.
- Fireball, Rally ve Emergency Repair cooldown'ları; aktif Fireball projectile; Fortify/Rally ve süreli economy/horde effect state'i.
- Aktif zombie toplamı, spatial yoğunluğu, state/HP/slow/death dağılımı ve combat ortalamaları aggregate rebuild payload'ında tutulur; tekil zombie pozisyonları tutulmaz.
- Aktif Arrow projectile state'i exact kalır; hedefi exact entity index'i yerine canonical zombie bucket index'iyle saklanır ve restore'da aynı seed ile bucket içindeki deterministik hedefe bağlanır.

Definition asset'lerden güvenle yeniden üretilebilen legacy tech aggregate'leri ve archer formation world pozisyonları kaydedilmez. Castle Heart graph'i definition asset'ten yeniden üretilmez; exact DTO restore edilir ve purchased node effect'leri level state'inden replay edilir. Archer formation kaydedilen `ArcherFormationVersion` ve type count'larla aynı deterministik 40 x 25 algoritmadan tekrar üretilir.

Dawn survivor transaction'ında düşülen Food, resource snapshot'ının parçasıdır. Aynı transaction'ın `LastPopulationGrowthCycle/LastPopulationGrowthWave` marker'ı da allocation snapshot'ında saklandığı için Continue aynı Dawn'ı yeniden oynatıp ikinci kez population veya Food değişikliği yapmaz.

`SurvivorArrivalVisual` entity'leri transient world-space sunumdur; snapshot'a yazılmaz. Restore edilen tamamlanmış growth marker'ı `GameManager` tarafından yeni arrival olarak yorumlanmaz, bu nedenle Continue survivor yürüyüşünü ikinci kez oynatmaz.

## Determinizm

`WaveStateData.SpawnRandomState` spawn RNG stream'inin sahibidir. `WaveSpawnSystem` her batch öncesi bu state'ten `Unity.Mathematics.Random` kurar ve batch sonunda güncel state'i tekrar component'e yazar. Böylece Continue sonrasındaki spawn konumları kapanmadan önceki stream'den devam eder.

Combat rebuild seed ayrı kaydedilir; saved spawn RNG, cycle, kill ve count'tan stable üretilir fakat spawn stream'ini tüketmez. Bucket key'leri canonical sıralanır ve hücre içi stratified jitter yalnız seed + bucket index + item index'e bağlıdır. Ayrıntılı sözleşme `COMBAT_REBUILD_POLICY_ARCHITECTURE.md` içindedir.

## Restore sırası

`GameManager.TryRestoreRunFromCheckpoint()` şu sırayı korur:

1. Geçerli `v3`-`v14` snapshot yüklenir; legacy state açık migration zinciriyle in-memory v14'e yükseltilir. Saved Heart graph ve v14 combat rebuild payload'ı temiz runtime kurulmadan önce validate edilir.
2. Aynı `RunId` ve worker bina yatırım state'i geri alınır; tech seviyeleri maliyetsiz uygulanıp base + Heart + Council + Meta + bina aggregate'leri kurulur.
3. Council hafızası, `LastRegularCouncilDay` ve discriminator ile doğrulanmış aktif Council kartı aynen yüklenir; regular kart yalnız restore edilen gün scheduled ve henüz handled değilse açılır.
4. `ArcherFormationVersion` yüklenir; mevcut başlangıç okçuları aynı formation cache'ine taşınır, ardından archer level/count state'i, kaynaklar, finite Arrow paid state'i ve Grave Essence bakiyesi geri yazılır. Exact Heart graph effect'leri deferred pipeline ile replay edilir; Arrow effective capacity son aggregate sonrasında bir kez clamp edilir.
5. Exact cycle phase/timer, wave state ve spawn RNG state'i geri yazılır. `CycleIndex + 1`, zorunlu Day veya timer `0` uygulanmaz.
6. Wall current HP, ability cooldown ve süreli effect state'i geri yüklenir.
7. v14 aggregate varsa zombie'ler canonical bucket/seed policy ile; legacy v13 fallback varsa exact listeden kurulur. Arrow hedefleri bucket'a veya legacy index'e bağlanır ve aktif Fireball exact kurulur.
8. ECS cache/UI state'i yenilenir.

## Ölüm transaction'ı ve idempotent meta ödülü

`run_death_receipt.json`, run save ile meta save arasındaki küçük transaction journal'ıdır:

1. `SaveRunSnapshot()` önce ECS truth'ini yeniler; lethal state varsa yaşayan snapshot capture edilmez.
2. Wall ölümü kesinleşince `{ RunId, Day, Kills }` receipt'i atomik ve durable yazılır.
3. Receipt authoritative olduktan sonra `run_save.json` temizlenir. Fiziksel silme başarısız olsa bile matching receipt snapshot'ı fail-closed geçersiz kılar.
4. `MetaProgression.AddRunResult(runId, day, kills)` çağrılır ve meta state durable yazılır.
5. Meta save, ödüllendirilmiş son RunId'leri saklar. Aynı RunId tekrar gelirse Souls/istatistik ikinci kez yazılmaz; önceki write başarısız kalmış olabileceği için duplicate state yine diske yazılmayı dener.
6. Yalnız meta write başarılı ve RunId doğrulanmışsa receipt silinir. İşlem ortasında uygulama kapanırsa bir sonraki açılış receipt'i idempotent biçimde tamamlar.

Receipt veya `.tmp` marker'ı var fakat payload okunamıyorsa Continue fail-closed reddedilir. Atomik write, orphan temp recovery ve çökme matrisi `DEATH_RECEIPT_ARCHITECTURE.md` içindedir.

## Değişiklik kuralı

Yeni bir koşu state'i eklenirken üç sınır birlikte güncellenir:

- `RunSaveState` alanı,
- `GameManager.SaveRunSnapshot()` capture yolu,
- `GameManager.TryRestoreRunFromCheckpoint()` restore yolu.

Entity referansı doğrudan JSON'a yazılmaz. Referans gerekiyorsa compact stable identity/index kullanılır. Combat grid/key/seed semantiği değişirse policy version; disk DTO semantiği değişirse `RunSaveState.CurrentVersion` artırılır ve migration açıkça yazılır. Eksik exact state veya aggregate payload varsayımla doldurulmaz.

## Doğrulama

- `RunPersistenceTests.SchemaVersion_RejectsLegacyCheckpoint_AndAcceptsExactSnapshot`
- `RunPersistenceTests.JsonRoundTrip_PreservesExactCycleCombatCouncilAndAbilityState`
- `RunPersistenceTests.DeathReceipt_RoundTrip_PreservesRunIdentityAndRewardInputs`
- `RunPersistenceTests.Save_WritesCompactJson_AndRemainsLoadable`
- `RunPersistenceTests.CombatRebuildPolicy_10KField_IsCompactValidAndDeterministic`
- `RunPersistenceTests.TryLoad_Version13ExactCombat_MigratesWithoutInventingAggregatePayload`
- `RunPersistenceTests.TryLoad_Version14InvalidCombatRebuild_FailsClosed`
- `RunPersistenceTests.TryLoad_Version3Snapshot_MigratesWorkerAllocationBedBuildingFormationAndAmmoStateToCurrent`
- `RunPersistenceTests.TryLoad_Version4UnlimitedCapacity_MigratesToPopulationSafeBedBase`
- `RunPersistenceTests.TryLoad_Version5Snapshot_MigratesToCleanWorkerBuildingLevels`
- `RunPersistenceTests.TryLoad_Version6Snapshot_MigratesToFormationVersion1`
- `RunPersistenceTests.TryLoad_Version8Snapshot_MigratesToZeroGraveEssence`
- `RunPersistenceTests.TryLoad_Version9Snapshot_DoesNotInventMissingHeartGraph`
- `RunPersistenceTests.TryLoad_Version10ChanceFailure_DoesNotConsumeScheduledRegularCouncil`
- `RunPersistenceTests.TryLoad_Version10ProducedEvent_PreservesHandledScheduledDay`
- `RunPersistenceTests.CommitDeath_DeletesRunSnapshotContainingGraveEssence`
- `RunPersistenceTests.CorruptDeathReceiptMarker_FailsClosedAndInvalidatesSnapshot`
- `RunPersistenceTests.PendingDeathReward_RecoversOnceAndSurvivesReload`
- `RunPersistenceTests.PendingDeathReceipt_RecoversOrphanedDurableTemp`
- `ExactRunContinuePlayModeTests.SaveRunSnapshot_LethalEcsState_CannotRewriteContinueAfterDeath`
- `ExactRunContinuePlayModeTests.Continue_RestoresSameCyclePhaseTimerResourcesAndSpawnRng` actual worker ve target ratio state'ini de doğrular.
- `ExactRunContinuePlayModeTests.GraveEssence_UsesHeartTransactionPersistsOnContinueAndResetsWithRun`
- `HeartGraphContinuePlayModeTests.Continue_ReplaysExactSavedHeartGraphWithoutReroll`
- `CouncilRegularSchedulePlayModeTests.RegularCouncil_OpensExactlyOnThreeSixNineCadence_OncePerDay`
- `CouncilRegularSchedulePlayModeTests.ActiveRegularCouncil_ContinueRestoresExactPayloadMemoryAndHandledDay`
- `CouncilRegularSchedulePlayModeTests.ChosenRegularCouncil_ContinueRestoresDecisionAndTimedEffects`
- `ArcherFormationPlayModeTests.FormationV1_BuildsStableThousandPointsAndContinueUsesSameLayout`
- `HordeScalePlayModeTests.HordeScale_10K_WithHudFeedbackPoolFireballAndContinue_ProducesTelemetry`
- `ExactRunContinuePlayModeTests.BedCapacityPurchase_SpendsWoodAndPersistsAcrossExactContinue`
- `ExactRunContinuePlayModeTests.WorkerBuildingInvestments_SpendBothResourcesAndPersistAcrossExactContinue`
- Runtime kabulü ayrıca Main Menu save, uygulama kapanışı, aynı phase/timer restore, aktif projectile restore ve Wall ölümü sırasında force-close senaryolarını kapsar.
