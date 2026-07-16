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
        public void PhaseLightTarget_UsesAmberIndigoDuskAndCyanGoldDawnTransitions()
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
                    DayNightOverlayController.DuskAmberPeakProgress,
                    out Color duskAmber,
                    out float duskAmberIntensity);
                controller.ResolvePhaseLightTarget(
                    SiegeCyclePhase.Dusk,
                    1f,
                    out Color duskEnd,
                    out float duskEndIntensity);
                controller.ResolvePhaseLightTarget(
                    SiegeCyclePhase.Dawn,
                    DayNightOverlayController.DawnCyanPeakProgress,
                    out Color dawnCyan,
                    out float dawnCyanIntensity);
                controller.ResolvePhaseLightTarget(
                    SiegeCyclePhase.Dawn,
                    DayNightOverlayController.DawnGoldPeakProgress,
                    out Color dawnGold,
                    out float dawnGoldIntensity);
                controller.ResolvePhaseLightTarget(
                    SiegeCyclePhase.Dawn,
                    1f,
                    out Color dawnEnd,
                    out float dawnEndIntensity);

                Assert.That(duskStart, Is.EqualTo(controller.DayLightColor));
                Assert.That(duskStartIntensity, Is.EqualTo(controller.DayLightIntensity));
                Assert.That(duskAmber, Is.EqualTo(controller.DuskLightColor));
                Assert.That(duskAmberIntensity, Is.EqualTo(controller.DuskLightIntensity));
                Assert.That(duskEnd, Is.EqualTo(controller.NightLightColor));
                Assert.That(duskEndIntensity, Is.EqualTo(controller.NightLightIntensity));
                Assert.That(duskAmber.r, Is.GreaterThan(duskAmber.b + 0.40f));
                Assert.That(duskEnd.b, Is.GreaterThan(duskEnd.r + 0.20f));
                Assert.That(dawnCyan, Is.EqualTo(controller.DawnCyanLightColor));
                Assert.That(dawnCyanIntensity, Is.EqualTo(controller.DawnCyanLightIntensity));
                Assert.That(dawnCyan.b, Is.GreaterThan(dawnCyan.r + 0.40f));
                Assert.That(dawnGold, Is.EqualTo(controller.DawnLightColor));
                Assert.That(dawnGoldIntensity, Is.EqualTo(controller.DawnLightIntensity));
                Assert.That(dawnGold.r, Is.GreaterThan(dawnGold.b + 0.35f));
                Assert.That(dawnEnd, Is.EqualTo(controller.DayLightColor));
                Assert.That(dawnEndIntensity, Is.EqualTo(controller.DayLightIntensity));

                Color overlayCyan = controller.ResolveDawnOverlayColor(
                    controller.NightColor,
                    DayNightOverlayController.DawnCyanPeakProgress);
                Color overlayGold = controller.ResolveDawnOverlayColor(
                    controller.NightColor,
                    DayNightOverlayController.DawnGoldPeakProgress);
                Assert.That(overlayCyan, Is.EqualTo(controller.DawnCyanColor));
                Assert.That(overlayGold, Is.EqualTo(controller.DawnColor));
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

        [Test]
        public void NightPresentation_UsesColdMoonAndBoundedWindowIgnitionEnvelope()
        {
            var gameObject = new GameObject("NightPhasePresentationTest");
            var controller = gameObject.AddComponent<DayNightOverlayController>();
            try
            {
                controller.ResolvePhaseLightTarget(
                    SiegeCyclePhase.Night,
                    0.5f,
                    out Color nightColor,
                    out float nightIntensity);

                Assert.That(nightColor, Is.EqualTo(controller.NightLightColor));
                Assert.That(nightIntensity, Is.EqualTo(controller.NightLightIntensity));
                Assert.That(nightColor.b, Is.GreaterThan(nightColor.r + 0.40f));
                Assert.That(nightIntensity, Is.LessThan(controller.DayLightIntensity));

                Assert.That(controller.ResolvePhaseWindowLightIntensity(
                    SiegeCyclePhase.Day, 1f), Is.Zero);
                Assert.That(controller.ResolvePhaseWindowLightIntensity(
                    SiegeCyclePhase.Dusk,
                    DayNightOverlayController.DuskWindowIgnitionProgress), Is.Zero);
                Assert.That(controller.ResolvePhaseWindowLightIntensity(
                    SiegeCyclePhase.Dusk,
                    DayNightOverlayController.DuskWindowFullProgress),
                    Is.EqualTo(controller.WindowLightIntensity).Within(0.001f));
                Assert.That(controller.ResolvePhaseWindowLightIntensity(
                    SiegeCyclePhase.Night, 0.5f),
                    Is.EqualTo(controller.WindowLightIntensity).Within(0.001f));
                Assert.That(controller.ResolvePhaseWindowLightIntensity(
                    SiegeCyclePhase.Dawn, 1f), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void NightMix_ScalesHordeBedAndAggregatesSalvoWithoutUnboundedGain()
        {
            float noEnemies = AmbientAudioController.ResolveNightHordeActivity01(
                SiegeCyclePhase.Night, 1f, 0);
            float dayEnemies = AmbientAudioController.ResolveNightHordeActivity01(
                SiegeCyclePhase.Day, 1f, 10_000);
            float smallHorde = AmbientAudioController.ResolveNightHordeActivity01(
                SiegeCyclePhase.Night, 0.2f, 32);
            float largeHorde = AmbientAudioController.ResolveNightHordeActivity01(
                SiegeCyclePhase.Night, 1f, 10_000);

            Assert.That(noEnemies, Is.Zero);
            Assert.That(dayEnemies, Is.Zero);
            Assert.That(smallHorde, Is.GreaterThan(0f));
            Assert.That(largeHorde, Is.GreaterThan(smallHorde));
            Assert.That(largeHorde, Is.EqualTo(1f).Within(0.001f));

            float singleVolume = CombatFeedbackBridge.ResolveArcherSalvoVolume(1, 0.35f, 0.62f);
            float groupVolume = CombatFeedbackBridge.ResolveArcherSalvoVolume(32, 0.35f, 0.62f);
            float hugeVolume = CombatFeedbackBridge.ResolveArcherSalvoVolume(10_000, 0.35f, 0.62f);
            float groupPitch = CombatFeedbackBridge.ResolveArcherSalvoPitchMultiplier(32, 0.08f);
            float hugePitch = CombatFeedbackBridge.ResolveArcherSalvoPitchMultiplier(10_000, 0.08f);

            Assert.That(groupVolume, Is.GreaterThan(singleVolume));
            Assert.That(hugeVolume, Is.GreaterThanOrEqualTo(groupVolume));
            Assert.That(hugeVolume, Is.LessThanOrEqualTo(0.62f));
            Assert.That(groupPitch, Is.LessThan(1f));
            Assert.That(hugePitch, Is.LessThanOrEqualTo(groupPitch));
            Assert.That(hugePitch, Is.GreaterThanOrEqualTo(0.92f));
        }
    }
}
