using Unity.Entities;

namespace DeadWalls
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EnemyPoolInitializationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyPoolRuntimeData>();
            state.RequireForUpdate<EnemyCatalogRuntimeData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();
            Entity poolEntity = SystemAPI.GetSingletonEntity<EnemyPoolRuntimeData>();
            if (EnemyPoolRuntimeUtility.EnsureInitialized(state.EntityManager, poolEntity))
                state.Enabled = false;
        }
    }
}
