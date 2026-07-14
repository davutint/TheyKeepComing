using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct WorkerLogisticsMovementSystem : ISystem
    {
        private const int FrameCount = 15;
        private const float ArrivalDistance = 0.035f;
        private const float DeliveryPulseDecayPerSecond = 1.8f;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            bool lanternActive = false;
            if (SystemAPI.HasSingleton<ContinuousSiegeCycleData>())
            {
                lanternActive = WorkerVisualRepresentationUtility.ShouldUseLantern(
                    SystemAPI.GetSingleton<ContinuousSiegeCycleData>().Phase);
            }
            else if (SystemAPI.HasSingleton<WaveStateData>())
            {
                lanternActive = SystemAPI.GetSingleton<WaveStateData>().Phase == RunPhaseType.NightCombat;
            }

            new WorkerLogisticsMoveJob
            {
                DeltaTime = deltaTime,
                LanternActive = lanternActive ? (byte)1 : (byte)0
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct WorkerLogisticsMoveJob : IJobEntity
        {
            public float DeltaTime;
            public byte LanternActive;

            void Execute(
                ref LocalTransform transform,
                ref WorkerLogisticsRoute route,
                ref SpriteAnimation anim,
                ref WorkerLogisticsFeedbackState feedback,
                ref WorkerAnimationMaterialProperty animationProperty,
                ref WorkerFeedbackMaterialProperty feedbackProperty,
                in ResourceWorkerVisual visual)
            {
                feedback.LanternActive = LanternActive;
                feedback.DeliveryPulse01 = math.max(0f,
                    feedback.DeliveryPulse01 - DeltaTime * DeliveryPulseDecayPerSecond);

                if (route.WaitTimer > 0f)
                {
                    route.WaitTimer = math.max(0f, route.WaitTimer - DeltaTime);
                    bool workingAtPickup = route.MovingToHub != 0;
                    feedback.Activity = workingAtPickup
                        ? WorkerLogisticsActivity.Working
                        : WorkerLogisticsActivity.Delivering;
                    feedback.IsCarrying = workingAtPickup ? (byte)1 : (byte)0;
                    SetAnimation(
                        ref anim,
                        ref animationProperty,
                        ResolveDirection(route.LastDirection, anim.DirectionRow % 8),
                        workingAtPickup ? WorkerAnimationKind.Work : WorkerAnimationKind.Celebrate);
                    SetFeedbackProperty(ref feedbackProperty, feedback, visual.RepresentedWorkerCount);
                    return;
                }

                float3 target = ResolveTarget(route);
                float3 delta = target - transform.Position;
                float2 delta2 = delta.xy;
                float distanceSq = math.lengthsq(delta2);

                if (distanceSq <= ArrivalDistance * ArrivalDistance)
                {
                    if (route.RouteLeg < 2)
                    {
                        route.RouteLeg++;
                        return;
                    }

                    bool arrivedAtHub = route.MovingToHub != 0;
                    route.MovingToHub = arrivedAtHub ? (byte)0 : (byte)1;
                    route.RouteLeg = 0;
                    route.WaitTimer = arrivedAtHub ? route.DeliveryDuration : route.WorkDuration;
                    feedback.IsCarrying = arrivedAtHub ? (byte)0 : (byte)1;
                    feedback.Activity = arrivedAtHub
                        ? WorkerLogisticsActivity.Delivering
                        : WorkerLogisticsActivity.Working;
                    if (arrivedAtHub)
                        feedback.DeliveryPulse01 = 1f;

                    SetAnimation(
                        ref anim,
                        ref animationProperty,
                        ResolveDirection(route.LastDirection, anim.DirectionRow % 8),
                        arrivedAtHub ? WorkerAnimationKind.Celebrate : WorkerAnimationKind.Work);
                    SetFeedbackProperty(ref feedbackProperty, feedback, visual.RepresentedWorkerCount);
                    return;
                }

                float distance = math.sqrt(distanceSq);
                float2 direction = delta2 / math.max(0.0001f, distance);
                float step = math.min(distance, math.max(0f, route.Speed) * DeltaTime);
                transform.Position += new float3(direction * step, target.z - transform.Position.z);
                route.LastDirection = direction;
                feedback.Activity = feedback.IsCarrying != 0
                    ? WorkerLogisticsActivity.Carrying
                    : WorkerLogisticsActivity.Returning;
                SetAnimation(
                    ref anim,
                    ref animationProperty,
                    ResolveDirection(direction, anim.DirectionRow % 8),
                    WorkerAnimationKind.Walk);
                SetFeedbackProperty(ref feedbackProperty, feedback, visual.RepresentedWorkerCount);
            }

            private static float3 ResolveTarget(WorkerLogisticsRoute route)
            {
                if (route.MovingToHub != 0)
                {
                    if (route.RouteLeg == 0)
                        return route.SiteApproachPosition;
                    if (route.RouteLeg == 1)
                        return route.HubApproachPosition;
                    return route.DeliveryPosition;
                }

                if (route.RouteLeg == 0)
                    return route.HubApproachPosition;
                if (route.RouteLeg == 1)
                    return route.SiteApproachPosition;
                return route.PickupPosition;
            }

            private static void SetAnimation(
                ref SpriteAnimation anim,
                ref WorkerAnimationMaterialProperty animationProperty,
                int directionRow,
                WorkerAnimationKind animationKind)
            {
                float targetAnimation = (float)animationKind;
                if (anim.DirectionRow == directionRow
                    && anim.FrameCount == FrameCount
                    && animationProperty.Value == targetAnimation)
                {
                    return;
                }

                anim.DirectionRow = directionRow;
                anim.FrameCount = FrameCount;
                anim.CurrentFrame = 0;
                anim.FrameTimer = 0f;
                animationProperty.Value = targetAnimation;
            }

            private static void SetFeedbackProperty(
                ref WorkerFeedbackMaterialProperty property,
                WorkerLogisticsFeedbackState feedback,
                int representedWorkerCount)
            {
                property.Value = new float4(
                    feedback.IsCarrying,
                    feedback.LanternActive,
                    feedback.DeliveryPulse01,
                    WorkerVisualRepresentationUtility.GetProductionFeedbackStrength(
                        representedWorkerCount));
            }

            private static int ResolveDirection(float2 direction, int fallbackDirection)
            {
                if (math.lengthsq(direction) < 0.0001f)
                    return fallbackDirection;

                float angle = math.atan2(direction.y, direction.x);
                int index = (int)math.round((-angle) / (math.PI * 0.25f));
                index %= 8;
                if (index < 0)
                    index += 8;

                return index;
            }
        }
    }
}
