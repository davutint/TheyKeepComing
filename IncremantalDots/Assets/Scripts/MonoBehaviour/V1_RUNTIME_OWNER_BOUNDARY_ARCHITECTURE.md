# V1 Runtime Owner Boundary - Mimari

## Amaç

Her gameplay domain'i tek authoritative runtime state ve tek transaction zinciri kullanır.
Yeni özellik mevcut owner'ı dönüştürür; aynı veriyi bağımsız güncelleyen ikinci manager,
controller, ECS component veya system eklemez.

## Owner matrisi

| Domain | Runtime state owner | Köprü / presentation |
|---|---|---|
| Run ve Game Over | `GameStateData` | `GameManager`, `UIManager` |
| Kaynaklar | `ResourceData` + `ResourceAccumulator` | `GameManager` transaction API, HUD |
| Population ve worker | `PopulationState` + `MobilePopulationAllocation` | `GameManager`, worker UI/visual systems |
| Bed ve worker yatırımı | `MobileBedCapacityState` + `MobileWorkerBuildingUpgradeState` | `GameManager`, Workers drawer |
| Cycle ve spawn demand | `ContinuousSiegeCycleData` + `ContinuousSpawnBudgetData` | cycle/spawn systems, Celestial Dial |
| Savunma | tek `WallSegment` | `GameManager`, defense/repair UI |
| Ok stoku | `ArrowSupply` | `GameManager`, `ArrowSupplyUI` |
| Archer formasyonu | `ArcherFormationV1.asset` + deterministic utility/cache zinciri | `MobileCastleArcherTilePlacement` |
| Castle Heart | exact `GeneratedRunGraph` + `GraveEssence` | `GameManager.HeartRuntime`, `HeartScreenUI` |
| Active abilities | `GameManager` cooldown/unlock zinciri + mevcut config/effect component'leri | `SpellCastUI` |
| Council | `GameManager` karar hafızası + `MobileEconomyEventState` süreli etkileri | `CouncilEventUI` |
| Meta | `MetaProgression` ayrı durable save | Main Menu / Game Over meta UI |

## Aynı owner sayılan katmanlar

- `GameManager.cs`, `GameManager.HeartRuntime.cs` ve `GameManagerDevelopmentTools.cs` aynı
  partial sınıftır. Üç bağımsız manager değildir.
- `GameManager` üzerindeki `Resources`, `Population`, `EconomyEvent` gibi public değerler ECS
  truth'inin read cache'idir. Transaction ECS state'ini yazar ve cache'i aynı sonuçla günceller;
  ikinci bir simulation loop çalıştırmaz.
- ScriptableObject definition/config asset'leri runtime state değildir. Baker/resolver bunları
  mevcut ECS config'ine taşır.
- UI controller'ları state sahibi değildir; authoritative read API'sini gösterir ve kabul
  edilen input'u mevcut transaction API'sine yollar.
- Editor/Development test override'ları transienttir, production save'e giremez ve production
  owner'ı değiştiremez.

## Guard

`ExactRunContinuePlayModeTests.ActiveV1Runtime_HasSingleSceneAndEcsOwnerPerDomain`, gerçek
`NewGameScene` içinde ana presentation/bridge component'lerinin her birinden tam bir tane ve
temel ECS state component'lerinin her birinden tam bir singleton bulunduğunu doğrular.

Bu test legacy sınıf dosyasının varlığını yasaklamaz. Legacy/dormant kod ancak aktif scene,
prefab, authoring veya runtime transaction zincirine bağlanırsa bu sınırı ihlal eder.

## `MobileCastle*` isim stabilitesi

`MobileCastle*` öneki mobil platform hedefi değildir; projenin eski teknik isimlendirmesinden
kalan ve aktif V1 kaynak, scene, prefab ve asset referanslarına yayılmış bir serialized
sözleşmedir. Dead Walls V1 PC/Steam ürün hedefini korurken mevcut `MobileCastleCombatConfig`,
`MobileCastleCombatAuthoring`, `MobileCastleTuningResolver`,
`MobileCastleArcherTilePlacement` ve ilişkili tipler yalnız daha estetik görünmeleri için toplu
rename edilmez.

Mevcut bir `MobileCastle*` tipi ancak işlevsel bir ihtiyaç varsa ve owner açıkça onaylarsa rename
edilebilir. Böyle bir değişiklikten önce bütün kod/scene/prefab/asset referansları envantere
alınır; script GUID, type name ve serialized field migration planı hazırlanır; gerektiğinde
`MovedFrom` / `FormerlySerializedAs` gibi compatibility katmanları eklenir; aktif scene, prefab,
save ve regresyon testleri birlikte doğrulanır. Yalnız dosya ve sınıf adını değiştirmek kabul
edilmez.

Yeni V1 tiplerinin `MobileCastle` önekini sürdürme zorunluluğu yoktur; yeni isimler domain'i
anlatmalıdır. Player-facing metinler ise teknik öneki göstermez ve `Dead Walls`, kale, sur veya
ilgili gameplay terimini kullanır. Bu kural mevcut serialized yüzeyi korur; tarihsel öneki yeni
tasarıma yayma zorunluluğu doğurmaz.

## Dormant legacy wiring review sınırı

Legacy etiketli her veri aynı değildir. V1 sınırı iki grubu ayrı tutar:

- Dormant gameplay yüzeyleri: `GateComponent`, `CastleHP`, `ArcherTrainer` /
  `BarracksTrainingSystem` tetik zinciri, Moat slow/damage gameplay'i ve special-night /
  Blood Moon davranışı. Bunlar aktif V1 scene, prefab, authoring, catalog veya ECS world'üne
  bağlanamaz.
- Onaylı compatibility yüzeyleri: `MobileCastleArcherRingGizmo` serialized type alias'ı,
  catalog bulunmayan eski scene'ler için `WaveConfigAuthoring` prefab fallback'i ve
  `RunPersistence` schema migration alanları. Bunlar yalnız eski veriyi okuyup canonical owner'a
  tek yönlü migrate edebilir; yeni runtime truth yazamaz, ikinci transaction owner olamaz ve
  player-facing davranış/presentation üretemez.

Dormant bir yüzeyi yeniden bağlamak refactor değildir, scope değişikliğidir. Uygulamadan önce
owner kararı tracker'a yazılır; Blueprint/V1 kapsam etkisi, mevcut canonical owner, kaldırılacak
veya dönüştürülecek compatibility yolu, save migration'ı, scene/prefab/asset referansları ve
regresyon planı birlikte review edilir. Yalnız component eklemek, system query'sini beslemek,
catalog'a dormant id koymak veya eski UI'ı görünür yapmak açık review yerine geçmez.

`ExactRunContinuePlayModeTests.ActiveV1Runtime_HasSingleSceneAndEcsOwnerPerDomain`, owner
tekilliğine ek olarak aktif world'de `GateComponent`, `CastleHP` ve `ArcherTrainer` sayısını
sıfır; Moat config'ini neutral; bütün difficulty sample special-night çarpanlarını `1` olarak
kilitler. Compatibility kodunun dosyada kalması bu guard'ı ihlal etmez; canonical runtime
zincirine gameplay state olarak sızması ihlal eder.
