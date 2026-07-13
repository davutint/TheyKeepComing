using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ArrowHitSystem))]
    public partial struct ZombieDeathSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var feedbackClaim = new NativeArray<int>(
                1, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            var feedbackPosition = new NativeArray<float3>(
                1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            JobHandle deathHandle = new DeathCheckJob
            {
                FeedbackClaim = feedbackClaim,
                FeedbackPosition = feedbackPosition
            }.ScheduleParallel(state.Dependency);

            JobHandle feedbackHandle = new EmitDeathSfxJob
            {
                FeedbackClaim = feedbackClaim,
                FeedbackPosition = feedbackPosition,
                ECB = ecb
            }.Schedule(deathHandle);

            JobHandle claimDisposeHandle = feedbackClaim.Dispose(feedbackHandle);
            state.Dependency = feedbackPosition.Dispose(claimDisposeHandle);
        }

        [BurstCompile]
        [WithAll(typeof(ZombieTag))]
        partial struct DeathCheckJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public NativeArray<int> FeedbackClaim;
            [NativeDisableParallelForRestriction] public NativeArray<float3> FeedbackPosition;

            unsafe void Execute(in ZombieStats stats,
                ref ZombieState zombieState, in LocalTransform transform)
            {
                if (zombieState.Value != ZombieStateType.Dead && stats.CurrentHP <= 0f)
                {
                    zombieState.Value = ZombieStateType.Dead;

                    // Bridge frame icinde zaten tek olum sesini duyurabilir. Atomik claim,
                    // ayni frame olen binlerce zombi icin binlerce gecici SFX entity'si
                    // olusturmak yerine temsilci bir konum secer.
                    int* feedbackClaim = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(FeedbackClaim);
                    if (Interlocked.CompareExchange(ref feedbackClaim[0], 1, 0) == 0)
                        FeedbackPosition[0] = transform.Position;
                }
            }
        }

        [BurstCompile]
        private struct EmitDeathSfxJob : IJob
        {
            [ReadOnly] public NativeArray<int> FeedbackClaim;
            [ReadOnly] public NativeArray<float3> FeedbackPosition;
            public EntityCommandBuffer ECB;

            public void Execute()
            {
                if (FeedbackClaim[0] == 0)
                    return;

                Entity sfxEvent = ECB.CreateEntity();
                ECB.AddComponent(sfxEvent, new CombatSfxEvent
                {
                    Position = FeedbackPosition[0],
                    Type = CombatSfxType.ZombieDeath,
                    Volume = 0.35f,
                    Pitch = 1f
                });
            }
        }
    }
}
