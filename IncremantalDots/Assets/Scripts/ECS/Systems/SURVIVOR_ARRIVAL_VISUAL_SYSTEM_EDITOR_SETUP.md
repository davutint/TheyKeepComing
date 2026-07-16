# Survivor Arrival Visual System - Editor Setup

## Gerekli mevcut bağlar

Bu sistem yeni Inspector alanı veya scene objesi istemez. Aşağıdaki mevcut bağlar korunmalıdır:

- `WaveConfigAuthoring.WorkerPrefab`, `Assets/Prefabs/VillagerWorker.prefab` kaynağına bağlı olmalıdır.
- Baked dünyada `WorkerPrefabData.WorkerPrefab` geçerli bir prefab entity göstermelidir.
- `MobileCastleCombatAuthoring`, gerçek `FrontlineX` ve `CastleCenter` değerlerini bake etmelidir.
- Worker prefabında `SpriteAnimation`, `WorkerAnimationMaterialProperty`, `WorkerFeedbackMaterialProperty`, `SpriteTint` ve `LocalTransform` bulunmalıdır.

## Scene kurulumu

`NewGameScene` veya `MobileCastleCombatSubScene` içine yeni GameObject eklenmez. Spawn noktası, lane dağılımı, yürüyüş hızı ve Wall arkası hedefi `SurvivorArrivalVisualUtility` tarafından runtime'da hesaplanır.

Prefab/component sözleşmesi değişirse önce worker logistics ve arrival testleri birlikte çalıştırılmalıdır; iki akış aynı görsel prefabı paylaşır ancak farklı marker component'leriyle ayrılır.

## Manuel kabul kontrolü

1. `NewGameScene` açılır ve Play başlatılır.
2. Boş yatak ile en az `15 Food` bulunacak şekilde bir Dawn transaction'ı tetiklenir.
3. En fazla 15 açık mavi villager'ın sağ battlefield'dan farklı lane/gecikmelerle Wall'a yürüdüğü görülür.
4. Villager'lar Wall arkasına girdiklerinde yok olmalıdır.
5. Arrival villager'larında resource cargo, lantern veya delivery pulse görünmemelidir.
6. Aynı Dawn marker'ında ikinci grup doğmamalıdır.
7. Save/Continue sonrası tamamlanmış Dawn arrival'ı yeniden oynatılmamalıdır.

Ana gate tile ve glow binding'i bu ECS setup'ının değil, `DawnRewardToastUI` sunumunun
sözleşmesidir. Onarım ve manuel kabul adımları için
`Assets/Scripts/MonoBehaviour/DAWN_PHASE_PRESENTATION_EDITOR_SETUP.md` kullanılır.

Otomatik doğrulama için `SurvivorArrivalVisualUtilityTests`, ilgili `WorkerAllocationPlayModeTests` ve `ExactRunContinuePlayModeTests` çalıştırılır.
