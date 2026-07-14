using Unity.Entities;

namespace DeadWalls
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(EnemyPoolInitializationSystem))]
    public partial struct ArrowPoolMaintenanceSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArrowPrefabData>();
            state.RequireForUpdate<ArrowPoolRuntimeData>();
            state.RequireForUpdate<ArrowPoolAvailable>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();
            Entity poolEntity = SystemAPI.GetSingletonEntity<ArrowPoolRuntimeData>();
            Entity arrowPrefab = SystemAPI.GetSingleton<ArrowPrefabData>().ArrowPrefab;
            ArrowPoolRuntimeUtility.Maintain(state.EntityManager, poolEntity, arrowPrefab);
        }
    }
}
