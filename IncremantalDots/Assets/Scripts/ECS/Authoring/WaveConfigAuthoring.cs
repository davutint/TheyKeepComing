using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DeadWalls
{
    public struct ZombiePrefabData : IComponentData
    {
        public Entity ZombiePrefab;
    }

    public struct ArrowPrefabData : IComponentData
    {
        public Entity ArrowPrefab;
        public float Speed;
        public float Lifetime;
    }

    public struct ArcherPrefabData : IComponentData
    {
        public Entity ArcherPrefab;
    }

    public class WaveConfigAuthoring : MonoBehaviour
    {
        [Header("Enemy Catalog (V1 runtime owner)")]
        public EnemyCatalogSO EnemyCatalog;

        [Header("Legacy Prefab Fallback")]
        [Tooltip("Yalniz catalog bagli olmayan eski scene migration'i icin kullanilir.")]
        public GameObject ZombiePrefab;
        public GameObject ArrowPrefab;
        public GameObject ArcherPrefab;
        public GameObject WorkerPrefab;

        [Header("Arrow Pool")]
        [Min(0)] public int ArrowPoolPrewarm = 1024;
        [Min(1)] public int ArrowPoolExpandBatch = 256;

        public class Baker : Baker<WaveConfigAuthoring>
        {
            public override void Bake(WaveConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                var entries = AddBuffer<EnemyCatalogEntryData>(entity);
                int activeEntryIndex = -1;
                var catalog = authoring.EnemyCatalog;
                if (catalog != null)
                {
                    DependsOn(catalog);
                    var definitions = catalog.Definitions;
                    if (definitions != null)
                    {
                        for (int i = 0; i < definitions.Length; i++)
                        {
                            var definition = definitions[i];
                            if (definition == null || definition.Prefab == null || string.IsNullOrWhiteSpace(definition.Id))
                                continue;

                            DependsOn(definition);
                            int runtimeIndex = entries.Length;
                            entries.Add(new EnemyCatalogEntryData
                            {
                                Id = new FixedString64Bytes(definition.Id),
                                Prefab = GetEntity(definition.Prefab, TransformUsageFlags.Dynamic),
                                BaseHP = math.max(1f, definition.BaseHP),
                                BaseDamage = math.max(0f, definition.BaseDamage),
                                BaseMoveSpeed = math.max(0.05f, definition.BaseMoveSpeed),
                                Scale = math.max(0.01f, definition.Scale),
                                XPReward = math.max(0, definition.XPReward),
                                SpawnWeight = math.max(0.01f, definition.SpawnWeight),
                                PoolPrewarm = math.max(0, definition.PoolPrewarm),
                                PoolExpandBatch = math.max(1, definition.PoolExpandBatch)
                            });

                            if (definition.Id == catalog.ActiveEnemyId)
                                activeEntryIndex = runtimeIndex;
                        }
                    }
                }

                // Migration fallback: aktif NewGameScene bunu kullanmaz. Eski scene'ler catalog
                // atanmadan da acilabilsin diye legacy prefab tek neutral entry olarak bake edilir.
                if (entries.Length == 0 && authoring.ZombiePrefab != null)
                {
                    float legacyScale = math.max(0.01f, authoring.ZombiePrefab.transform.localScale.x);
                    entries.Add(new EnemyCatalogEntryData
                    {
                        Id = new FixedString64Bytes("legacy_zombie"),
                        Prefab = GetEntity(authoring.ZombiePrefab, TransformUsageFlags.Dynamic),
                        BaseHP = 20f,
                        BaseDamage = 5f,
                        BaseMoveSpeed = 1.5f,
                        Scale = legacyScale,
                        XPReward = 10,
                        SpawnWeight = 1f,
                        PoolPrewarm = 0,
                        PoolExpandBatch = 64
                    });
                    activeEntryIndex = 0;
                }

                if (activeEntryIndex < 0 && entries.Length > 0)
                    activeEntryIndex = 0;

                AddComponent(entity, new EnemyCatalogRuntimeData
                {
                    EntryCount = entries.Length,
                    ActiveEntryIndex = activeEntryIndex
                });

                EnemyCatalogEntryData activeEntry = activeEntryIndex >= 0
                    ? entries[activeEntryIndex]
                    : default;
                AddComponent(entity, new EnemyPoolRuntimeData
                {
                    Initialized = 0,
                    ActiveEntryIndex = activeEntryIndex,
                    PrewarmTarget = activeEntryIndex >= 0 ? activeEntry.PoolPrewarm : 0,
                    ExpandBatch = activeEntryIndex >= 0 ? activeEntry.PoolExpandBatch : 1,
                    TotalCreated = 0,
                    AvailableCount = 0,
                    ActiveCount = 0,
                    ExpansionCount = 0,
                    TotalRentCount = 0,
                    TotalReturnCount = 0
                });
                AddBuffer<EnemyPoolAvailable>(entity);

                AddComponent(entity, new ArrowPoolRuntimeData
                {
                    Initialized = 0,
                    PrewarmTarget = math.max(0, authoring.ArrowPoolPrewarm),
                    ExpandBatch = math.max(1, authoring.ArrowPoolExpandBatch),
                    ExpandRequested = 0,
                    TotalCreated = 0,
                    AvailableCount = 0,
                    ActiveCount = 0,
                    ExpansionCount = 0,
                    TotalRentCount = 0,
                    TotalReturnCount = 0
                });
                AddBuffer<ArrowPoolAvailable>(entity);

                Entity activeEnemyPrefab = activeEntryIndex >= 0
                    ? entries[activeEntryIndex].Prefab
                    : Entity.Null;

                AddComponent(entity, new ZombiePrefabData
                {
                    // Compatibility output: GameManager restore yolu ayni prefab'i okumaya devam eder.
                    ZombiePrefab = activeEnemyPrefab
                });
                var arrowAuthoring = authoring.ArrowPrefab != null
                    ? authoring.ArrowPrefab.GetComponent<ArrowAuthoring>()
                    : null;
                AddComponent(entity, new ArrowPrefabData
                {
                    ArrowPrefab = GetEntity(authoring.ArrowPrefab, TransformUsageFlags.Dynamic),
                    Speed = arrowAuthoring != null
                        ? math.max(0.01f, arrowAuthoring.Speed)
                        : 12f,
                    Lifetime = arrowAuthoring != null
                        ? math.max(0.1f, arrowAuthoring.Lifetime)
                        : ArrowProjectile.DefaultLifetimeSeconds
                });
                AddComponent(entity, new ArcherPrefabData
                {
                    ArcherPrefab = GetEntity(authoring.ArcherPrefab, TransformUsageFlags.Dynamic)
                });

                if (authoring.WorkerPrefab != null)
                {
                    AddComponent(entity, new WorkerPrefabData
                    {
                        WorkerPrefab = GetEntity(authoring.WorkerPrefab, TransformUsageFlags.Dynamic)
                    });
                }
            }
        }
    }
}
