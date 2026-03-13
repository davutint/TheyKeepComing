using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Sur uzerindeki mancinik slotlarini yonetir.
    /// Inspector'dan slot pozisyonlari tanimlanir.
    /// PlaceCatapult ile slot'a mancinik yerlestir, RemoveCatapult ile kaldir.
    /// </summary>
    public class WallSlotManager : MonoBehaviour
    {
        public static WallSlotManager Instance { get; private set; }

        [System.Serializable]
        public struct WallSlot
        {
            public Vector3 Position;
            [HideInInspector] public bool IsOccupied;
            [HideInInspector] public Entity OccupantEntity;
        }

        [Header("Slot Ayarlari")]
        public WallSlot[] Slots = new WallSlot[]
        {
            new WallSlot { Position = new Vector3(2.5f, -4f, -1f) },
            new WallSlot { Position = new Vector3(2.5f, 0f, -1f) },
            new WallSlot { Position = new Vector3(2.5f, 4f, -1f) }
        };

        private EntityManager _entityManager;
        private BuildingConfigSO _catapultConfig;
        private bool _initialized;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private bool TryInitialize()
        {
            if (_initialized) return true;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return false;

            _entityManager = world.EntityManager;

            // Catapult config'i BuildingGridManager'dan al (tek kaynak)
            if (_catapultConfig == null && BuildingGridManager.Instance != null)
                _catapultConfig = BuildingGridManager.Instance.GetConfigByType(BuildingType.Catapult);

            _initialized = true;
            return true;
        }

        /// <summary>
        /// Belirtilen slot'a mancinik yerlestir.
        /// Kaynak kontrolu + Demirci kontrolu yapar.
        /// </summary>
        public bool PlaceCatapult(int slotIndex)
        {
            if (!TryInitialize()) return false;
            if (slotIndex < 0 || slotIndex >= Slots.Length) return false;
            if (Slots[slotIndex].IsOccupied) return false;

            if (_catapultConfig == null) return false;

            // Demirci kontrolu
            if (_catapultConfig.RequireBlacksmith)
            {
                if (BuildingGridManager.Instance == null ||
                    !BuildingGridManager.Instance.HasBuildingOfType(BuildingType.Blacksmith))
                    return false;
            }

            // Kaynak kontrolu — maliyet BuildingConfigSO'dan okunur
            var resQuery = _entityManager.CreateEntityQuery(typeof(ResourceData));
            if (resQuery.IsEmpty) return false;

            var resEntity = resQuery.GetSingletonEntity();
            var res = _entityManager.GetComponentData<ResourceData>(resEntity);
            if (res.Wood < _catapultConfig.WoodCost || res.Stone < _catapultConfig.StoneCost || res.Iron < _catapultConfig.IronCost)
                return false;

            // Kaynak dus
            res.Wood -= _catapultConfig.WoodCost;
            res.Stone -= _catapultConfig.StoneCost;
            res.Iron -= _catapultConfig.IronCost;
            _entityManager.SetComponentData(resEntity, res);

            // Prefab'dan mancinik entity'si olustur
            var prefabQuery = _entityManager.CreateEntityQuery(typeof(CatapultPrefabData));
            if (prefabQuery.IsEmpty) return false;

            var prefabData = _entityManager.GetComponentData<CatapultPrefabData>(prefabQuery.GetSingletonEntity());
            var entity = _entityManager.Instantiate(prefabData.CatapultPrefab);

            var slotPos = Slots[slotIndex].Position;
            _entityManager.SetComponentData(entity, LocalTransform.FromPosition(
                new float3(slotPos.x, slotPos.y, slotPos.z)));

            // Slot isaretle
            Slots[slotIndex].IsOccupied = true;
            Slots[slotIndex].OccupantEntity = entity;

            return true;
        }

        /// <summary>
        /// Belirtilen slot'taki manciniki kaldir. %50 kaynak iade eder.
        /// </summary>
        public void RemoveCatapult(int slotIndex)
        {
            if (!TryInitialize()) return;
            if (slotIndex < 0 || slotIndex >= Slots.Length) return;
            if (!Slots[slotIndex].IsOccupied) return;

            var entity = Slots[slotIndex].OccupantEntity;
            if (_entityManager.Exists(entity))
                _entityManager.DestroyEntity(entity);

            // %50 kaynak iade — maliyet BuildingConfigSO'dan okunur
            if (_catapultConfig != null)
            {
                var resQuery = _entityManager.CreateEntityQuery(typeof(ResourceData));
                if (!resQuery.IsEmpty)
                {
                    var resEntity = resQuery.GetSingletonEntity();
                    var res = _entityManager.GetComponentData<ResourceData>(resEntity);
                    res.Wood += _catapultConfig.WoodCost / 2;
                    res.Stone += _catapultConfig.StoneCost / 2;
                    res.Iron += _catapultConfig.IronCost / 2;
                    _entityManager.SetComponentData(resEntity, res);
                }
            }

            Slots[slotIndex].IsOccupied = false;
            Slots[slotIndex].OccupantEntity = Entity.Null;
        }

        /// <summary>
        /// Tum slotlari bosalt — RestartGame icin.
        /// </summary>
        public void ResetSlots()
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i].IsOccupied)
                {
                    if (_initialized && _entityManager.Exists(Slots[i].OccupantEntity))
                        _entityManager.DestroyEntity(Slots[i].OccupantEntity);

                    Slots[i].IsOccupied = false;
                    Slots[i].OccupantEntity = Entity.Null;
                }
            }
        }

        /// <summary>
        /// Bos slot indeksi dondur. Bos slot yoksa -1.
        /// </summary>
        public int GetNearestEmptySlot(Vector3 worldPos)
        {
            int bestIndex = -1;
            float bestDist = float.MaxValue;

            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i].IsOccupied) continue;

                float dist = Vector3.Distance(worldPos, Slots[i].Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        /// <summary>
        /// Herhangi bir bos slot var mi?
        /// </summary>
        public bool HasEmptySlot()
        {
            for (int i = 0; i < Slots.Length; i++)
                if (!Slots[i].IsOccupied)
                    return true;
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            if (Slots == null) return;

            for (int i = 0; i < Slots.Length; i++)
            {
                Gizmos.color = Slots[i].IsOccupied ? Color.red : Color.green;
                Gizmos.DrawWireSphere(Slots[i].Position, 0.5f);
                Gizmos.DrawWireCube(Slots[i].Position, new Vector3(1f, 1f, 0.1f));
            }
        }
    }
}
