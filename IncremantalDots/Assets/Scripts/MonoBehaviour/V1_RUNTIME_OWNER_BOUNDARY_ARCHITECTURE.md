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
