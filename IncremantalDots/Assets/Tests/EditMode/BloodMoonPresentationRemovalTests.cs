using System.Reflection;
using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class BloodMoonPresentationRemovalTests
    {
        private const BindingFlags InstanceFields = BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic;

        [Test]
        public void RuntimePresentationOwners_DoNotExposeBloodMoonBranches()
        {
            AssertMissingField<AmbientAudioController>("BloodMoonLoop");
            AssertMissingField<AmbientAudioController>("BloodMoonSting");
            AssertMissingField<AmbientAudioController>("BloodMoonVolume");
            AssertMissingField<AmbientAudioController>("StingVolume");
            AssertMissingField<AmbientAudioController>("_stingSource");
            AssertMissingField<DayNightOverlayController>("BloodMoonColor");
            AssertMissingField<MomentVignetteUI>("BloodMoonPeak");
            AssertMissingField<HUDController>("_lastCycleBloodMoon");

            var warningType = typeof(HUDController).Assembly.GetType(
                "DeadWalls.BloodMoonWarningUI",
                false);
            Assert.That(warningType, Is.Null,
                "BloodMoonWarningUI V1 runtime assembly'sinde bulunmamali.");
        }

        private static void AssertMissingField<T>(string fieldName)
        {
            FieldInfo field = typeof(T).GetField(fieldName, InstanceFields);
            Assert.That(field, Is.Null,
                $"{typeof(T).Name}.{fieldName} aktif Blood Moon sunum baglantisi olarak kalmamalidir.");
        }
    }
}
