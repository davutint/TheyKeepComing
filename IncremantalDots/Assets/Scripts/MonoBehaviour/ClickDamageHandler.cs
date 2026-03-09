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

            // ECS'e ClickDamageRequest entity'si olustur
            var requestEntity = _entityManager.CreateEntity(typeof(ClickDamageRequest));
            _entityManager.SetComponentData(requestEntity, new ClickDamageRequest
            {
                WorldPosition = clickPos,
                Damage = clickDamage
            });
        }
    }
}
