using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class GraveEssenceDropUtilityTests
    {
        [Test]
        public void ShouldDrop_ClampsChanceBoundaries()
        {
            Assert.That(GraveEssenceDropUtility.ShouldDrop(0f, 7u, 11u, 1), Is.False);
            Assert.That(GraveEssenceDropUtility.ShouldDrop(-1f, 7u, 11u, 1), Is.False);
            Assert.That(GraveEssenceDropUtility.ShouldDrop(1f, 7u, 11u, 1), Is.True);
            Assert.That(GraveEssenceDropUtility.ShouldDrop(2f, 7u, 11u, 1), Is.True);
            Assert.That(GraveEssenceDropUtility.ShouldDrop(0.10f, 7u, 11u, 0), Is.False);
        }

        [Test]
        public void ShouldDrop_IsDeterministicForTheSameRunInputs()
        {
            for (int kill = 1; kill <= 1000; kill++)
            {
                bool first = GraveEssenceDropUtility.ShouldDrop(0.10f, 91273u, 456u, kill);
                bool second = GraveEssenceDropUtility.ShouldDrop(0.10f, 91273u, 456u, kill);
                Assert.That(second, Is.EqualTo(first), $"Kill ordinal {kill} farkli roll verdi.");
            }
        }

        [Test]
        public void ShouldDrop_ProductionChanceTracksTenPercentAcrossLargeSample()
        {
            const int sampleSize = 100000;
            int drops = 0;
            for (int kill = 1; kill <= sampleSize; kill++)
            {
                if (GraveEssenceDropUtility.ShouldDrop(0.10f, 0x6E624EB7u, 192837u, kill))
                    drops++;
            }

            double observed = drops / (double)sampleSize;
            Assert.That(observed, Is.InRange(0.095d, 0.105d),
                $"Production %10 roll buyuk orneklemde {observed:P3} uretti.");
        }

        [Test]
        public void ShouldDrop_ChangingRunStreamChangesTheDropPattern()
        {
            int differences = 0;
            for (int kill = 1; kill <= 1000; kill++)
            {
                bool first = GraveEssenceDropUtility.ShouldDrop(0.10f, 91273u, 101u, kill);
                bool second = GraveEssenceDropUtility.ShouldDrop(0.10f, 91273u, 202u, kill);
                if (first != second)
                    differences++;
            }

            Assert.That(differences, Is.GreaterThan(0));
        }
    }
}
