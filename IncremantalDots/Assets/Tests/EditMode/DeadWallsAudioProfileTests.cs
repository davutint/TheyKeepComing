using NUnit.Framework;
using UnityEditor;

namespace DeadWalls.Tests
{
    public class DeadWallsAudioProfileTests
    {
        [Test]
        public void CurrencyArrivalPolicy_IsSilentForInvalidAmountAndBoundedForDenseBurst()
        {
            Assert.That(CurrencyArrivalAudioPolicy.ResolveVolume(0, 0.3f, 0.05f), Is.Zero);
            Assert.That(CurrencyArrivalAudioPolicy.ResolveVolume(-1, 0.3f, 0.05f), Is.Zero);

            float one = CurrencyArrivalAudioPolicy.ResolveVolume(1, 0.22f, 0.055f);
            float dense = CurrencyArrivalAudioPolicy.ResolveVolume(10_000, 0.22f, 0.055f);
            Assert.That(one, Is.EqualTo(0.22f).Within(0.0001f));
            Assert.That(dense, Is.GreaterThan(one));
            Assert.That(dense, Is.LessThanOrEqualTo(1f));
            Assert.That(
                CurrencyArrivalAudioPolicy.ResolvePitch(10_000, 0.012f),
                Is.InRange(1f, 1.16f));
        }

        [Test]
        public void DefaultProfile_ContainsCuratedRuntimeFamiliesWithoutZombieDeathSlot()
        {
            DeadWallsAudioProfileSO profile =
                AssetDatabase.LoadAssetAtPath<DeadWallsAudioProfileSO>(
                    DeadWallsAudioProfileSO.DefaultAssetPath);

            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.ArrowShootClips, Has.Length.EqualTo(4));
            Assert.That(profile.WallHitClips, Has.Length.EqualTo(3));
            Assert.That(profile.SoulArrivalClip, Is.Not.Null);
            Assert.That(profile.EssenceArrivalClip, Is.Not.Null);
            Assert.That(profile.GetType().GetField("ZombieDeathClips"), Is.Null,
                "Audio profile Skeleton/zombie death sesi sunmamali.");
        }
    }
}
