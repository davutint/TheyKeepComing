using NUnit.Framework;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class DayPhasePresentationTests
    {
        [Test]
        public void DayLightTarget_IsWarmBrightAndStableAcrossDayProgress()
        {
            var gameObject = new GameObject("DayPhasePresentationTest");
            var controller = gameObject.AddComponent<DayNightOverlayController>();
            try
            {
                controller.ResolvePhaseLightTarget(
                    SiegeCyclePhase.Day,
                    0f,
                    out Color startColor,
                    out float startIntensity);
                controller.ResolvePhaseLightTarget(
                    SiegeCyclePhase.Day,
                    1f,
                    out Color endColor,
                    out float endIntensity);

                Assert.That(startColor, Is.EqualTo(controller.DayLightColor));
                Assert.That(endColor, Is.EqualTo(controller.DayLightColor));
                Assert.That(startIntensity, Is.EqualTo(controller.DayLightIntensity));
                Assert.That(endIntensity, Is.EqualTo(controller.DayLightIntensity));
                Assert.That(startColor.r, Is.GreaterThan(startColor.b + 0.10f));
                Assert.That(startColor.g, Is.GreaterThan(startColor.b + 0.05f));
                Assert.That(startIntensity, Is.GreaterThanOrEqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PhaseLightTarget_UsesContinuousDuskAndTwoStageDawnTransitions()
        {
            var gameObject = new GameObject("PhaseLightTransitionTest");
            var controller = gameObject.AddComponent<DayNightOverlayController>();
            try
            {
                controller.ResolvePhaseLightTarget(
                    SiegeCyclePhase.Dusk,
                    0f,
                    out Color duskStart,
                    out float duskStartIntensity);
                controller.ResolvePhaseLightTarget(
                    SiegeCyclePhase.Dusk,
                    1f,
                    out Color duskEnd,
                    out float duskEndIntensity);
                controller.ResolvePhaseLightTarget(
                    SiegeCyclePhase.Dawn,
                    0.5f,
                    out Color dawnMiddle,
                    out float dawnMiddleIntensity);
                controller.ResolvePhaseLightTarget(
                    SiegeCyclePhase.Dawn,
                    1f,
                    out Color dawnEnd,
                    out float dawnEndIntensity);

                Assert.That(duskStart, Is.EqualTo(controller.DayLightColor));
                Assert.That(duskStartIntensity, Is.EqualTo(controller.DayLightIntensity));
                Assert.That(duskEnd, Is.EqualTo(controller.DuskLightColor));
                Assert.That(duskEndIntensity, Is.EqualTo(controller.DuskLightIntensity));
                Assert.That(dawnMiddle, Is.EqualTo(controller.DawnLightColor));
                Assert.That(dawnMiddleIntensity, Is.EqualTo(controller.DawnLightIntensity));
                Assert.That(dawnEnd, Is.EqualTo(controller.DayLightColor));
                Assert.That(dawnEndIntensity, Is.EqualTo(controller.DayLightIntensity));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void WorkerFoleyCadence_IsSilentAtZeroAndScalesWithoutUnboundedDensity()
        {
            float emptyActivity = AmbientAudioController.ResolveWorkerActivity01(0);
            float smallActivity = AmbientAudioController.ResolveWorkerActivity01(4);
            float mediumActivity = AmbientAudioController.ResolveWorkerActivity01(64);
            float hugeActivity = AmbientAudioController.ResolveWorkerActivity01(100000);

            Assert.That(emptyActivity, Is.Zero);
            Assert.That(smallActivity, Is.GreaterThan(emptyActivity));
            Assert.That(mediumActivity, Is.GreaterThan(smallActivity));
            Assert.That(hugeActivity, Is.EqualTo(1f));

            float noWorkersInterval = AmbientAudioController.ResolveWorkerFoleyInterval(0, 1.6f, 5.2f);
            float activeInterval = AmbientAudioController.ResolveWorkerFoleyInterval(64, 1.6f, 5.2f);
            float hugeInterval = AmbientAudioController.ResolveWorkerFoleyInterval(100000, 1.6f, 5.2f);
            Assert.That(noWorkersInterval, Is.EqualTo(5.2f).Within(0.001f));
            Assert.That(activeInterval, Is.LessThan(noWorkersInterval));
            Assert.That(hugeInterval, Is.EqualTo(1.6f).Within(0.001f));
        }
    }
}
