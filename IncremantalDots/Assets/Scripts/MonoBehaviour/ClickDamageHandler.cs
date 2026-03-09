using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DeadWalls
{
    public class ClickDamageHandler : MonoBehaviour
    {
        private EntityManager _entityManager;
        private Camera _mainCamera;
        private bool _initialized;

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (!TryInitialize()) return;
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.GameState.IsGameOver) return;
            if (GameManager.Instance.GameState.IsLevelUpPending) return;

            if (Input.GetMouseButtonDown(0))
            {
                HandleClick();
            }
        }

        private bool TryInitialize()
        {
            if (_initialized) return true;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return false;

            _entityManager = world.EntityManager;
            _initialized = true;
            return true;
        }

        private void HandleClick()
        {
            Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            float3 clickPos = new float3(mouseWorld.x, mouseWorld.y, 0f);
            float clickDamage = GameManager.Instance.GameState.ClickDamage;

            // En yakin zombiyi bul
            Entity closestZombie = Entity.Null;
            float closestDist = 2f; // Max click mesafesi

            var query = _entityManager.CreateEntityQuery(
                typeof(ZombieTag), typeof(ZombieStats), typeof(ZombieState), typeof(LocalTransform));

            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var entity in entities)
            {
                var state = _entityManager.GetComponentData<ZombieState>(entity);
                if (state.Value == ZombieStateType.Dead) continue;

                var transform = _entityManager.GetComponentData<LocalTransform>(entity);
                float dist = math.distance(clickPos, transform.Position);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestZombie = entity;
                }
            }

            entities.Dispose();

            if (closestZombie != Entity.Null)
            {
                var stats = _entityManager.GetComponentData<ZombieStats>(closestZombie);
                stats.CurrentHP -= clickDamage;
                _entityManager.SetComponentData(closestZombie, stats);
            }
        }
    }
}
