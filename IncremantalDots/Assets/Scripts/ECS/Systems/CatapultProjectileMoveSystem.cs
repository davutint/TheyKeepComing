using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    /// <summary>
    /// Mancinik mermisi hareket sistemi — parabolik yol izler.
    /// lerp(Start, Target, t) + ArcHeight * 4 * t * (1-t) ile parabol hesaplar.
    /// FlightTimer >= FlightDuration olunca pozisyonu TargetPos'a sabitler (hit system isleyecek).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ZombieAttackTimerSystem))]
    public partial struct CatapultProjectileMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new CatapultProjectileMoveJob
            {
                Dt = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(CatapultProjectileTag))]
        partial struct CatapultProjectileMoveJob : IJobEntity
        {
            public float Dt;

            void Execute(ref CatapultProjectile projectile, ref LocalTransform transform)
            {
                projectile.FlightTimer += Dt;

                float t = math.saturate(projectile.FlightTimer / projectile.FlightDuration);

                // Yatay lerp
                float3 flatPos = math.lerp(projectile.StartPos, projectile.TargetPos, t);

                // Dikey parabol ark: 4*t*(1-t) -> t=0.5'te maksimum (1.0)
                float arc = projectile.ArcHeight * 4f * t * (1f - t);
                flatPos.y += arc;

                transform.Position = flatPos;

                // Tanjant vektorunden rotasyon
                float3 prevPos = math.lerp(projectile.StartPos, projectile.TargetPos, math.max(0f, t - 0.01f));
                float prevArc = projectile.ArcHeight * 4f * math.max(0f, t - 0.01f) * (1f - math.max(0f, t - 0.01f));
                prevPos.y += prevArc;

                float3 dir = flatPos - prevPos;
                if (math.lengthsq(dir) > 0.0001f)
                {
                    float angle = math.atan2(dir.y, dir.x);
                    transform.Rotation = quaternion.Euler(0f, 0f, angle);
                }
            }
        }
    }
}
