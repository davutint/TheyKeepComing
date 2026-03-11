using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    /// <summary>
    /// Attacking state'deki zombilerin saldiris timer'ini isler.
    /// Timer dolunca hasar NativeQueue'ya yazilir (main thread beklemez).
    /// Hasar DamageApplySystem'de uygulanir.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BoundarySystem))]
    public partial struct ZombieAttackTimerSystem : ISystem
    {
        // Static field: DamageApplySystem bu queue'yu okur
        // [BurstCompile] struct'tan kaldirildi — static field erisimi
        public static NativeQueue<float> DamageQueue;

        public void OnCreate(ref SystemState state)
        {
            DamageQueue = new NativeQueue<float>(Allocator.Persistent);
            state.RequireForUpdate<WallSegment>();
        }

        public void OnDestroy(ref SystemState state)
        {
            if (DamageQueue.IsCreated)
                DamageQueue.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            DamageQueue.Clear();

            new AttackTimerJob
            {
                Dt = SystemAPI.Time.DeltaTime,
                DamageWriter = DamageQueue.AsParallelWriter()
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(ZombieTag))]
        partial struct AttackTimerJob : IJobEntity
        {
            public float Dt;
            public NativeQueue<float>.ParallelWriter DamageWriter;

            void Execute(ref ZombieStats stats, in ZombieState state)
            {
                if (state.Value != ZombieStateType.Attacking) return;

                stats.AttackTimer -= Dt;
                if (stats.AttackTimer > 0f) return;

                stats.AttackTimer = stats.AttackCooldown;
                DamageWriter.Enqueue(stats.AttackDamage);
            }
        }
    }
}
