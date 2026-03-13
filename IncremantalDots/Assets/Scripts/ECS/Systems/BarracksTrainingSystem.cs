using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    /// <summary>
    /// Kisla okcu egitim sistemi.
    /// Idle nufus + yeterli kaynak varsa egitim baslatir, timer bitince okcu spawn eder.
    /// PopulationTickSystem'den SONRA, ResourceTickSystem'den ONCE calisir.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PopulationTickSystem))]
    [UpdateBefore(typeof(ResourceTickSystem))]
    public partial struct BarracksTrainingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PopulationState>();
            state.RequireForUpdate<ResourceData>();
            state.RequireForUpdate<ArcherPrefabData>();
            state.RequireForUpdate<GameStateData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.IsGameOver)
                return;

            float dt = SystemAPI.Time.DeltaTime;
            var popRW = SystemAPI.GetSingletonRW<PopulationState>();
            var resRW = SystemAPI.GetSingletonRW<ResourceData>();
            var archerPrefab = SystemAPI.GetSingleton<ArcherPrefabData>();
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Mevcut okcu entity sayisi (pozisyon hesabi icin)
            int archerCount = 0;
            foreach (var _ in SystemAPI.Query<RefRO<ArcherUnit>>())
                archerCount++;

            foreach (var trainer in SystemAPI.Query<RefRW<ArcherTrainer>>())
            {
                if (trainer.ValueRO.IsTraining)
                {
                    // Egitim devam ediyor — timer dusur
                    trainer.ValueRW.TrainingTimer -= dt;

                    if (trainer.ValueRO.TrainingTimer <= 0f)
                    {
                        // Egitim tamamlandi — okcu sayacini artir
                        popRW.ValueRW.Archers++;
                        trainer.ValueRW.IsTraining = false;

                        // ECS'de okcu entity spawn et
                        // ArcherPrefabData'dan prefab al, ArcherUnit ayarla
                        var archerEntity = ecb.Instantiate(archerPrefab.ArcherPrefab);
                        ecb.SetComponent(archerEntity, new ArcherUnit
                        {
                            FireRate = 1.5f,
                            FireTimer = 0f,
                            ArrowDamage = 10f,
                            Range = 15f
                        });
                        ecb.SetComponent(archerEntity, LocalTransform.FromPosition(
                            new float3(3.76f, -5f + archerCount * 2f, -1f)));

                        archerCount++;
                    }
                }
                else
                {
                    // Egitim bekleniyor — idle nufus ve kaynak kontrolu
                    int idle = popRW.ValueRO.Idle;
                    if (idle <= 0)
                        continue;

                    int foodCost = trainer.ValueRO.FoodCostPerArcher;
                    int woodCost = trainer.ValueRO.WoodCostPerArcher;

                    if (resRW.ValueRO.Food < foodCost || resRW.ValueRO.Wood < woodCost)
                        continue;

                    // Kaynaklari dus, egitimi baslat
                    resRW.ValueRW.Food -= foodCost;
                    resRW.ValueRW.Wood -= woodCost;
                    trainer.ValueRW.IsTraining = true;
                    trainer.ValueRW.TrainingTimer = trainer.ValueRO.TrainingDuration;
                }
            }
        }
    }
}
