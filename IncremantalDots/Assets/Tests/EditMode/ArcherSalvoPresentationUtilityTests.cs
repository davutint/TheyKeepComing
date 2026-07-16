using System.Collections.Generic;
using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class ArcherSalvoPresentationUtilityTests
    {
        [Test]
        public void SmallFormation_KeepsEveryProjectileVisible()
        {
            int visibleCount = CountVisibleProjectiles(48, 1L, 48);

            Assert.That(ArcherSalvoPresentationUtility.GetSamplingStride(48), Is.EqualTo(1));
            Assert.That(visibleCount, Is.EqualTo(48));
        }

        [Test]
        public void ThousandArcherVolley_UsesAtMostFortyEightVisualRepresentatives()
        {
            const int archerCount = 1_000;
            const long firstShotSequence = 237L;

            int visibleCount = CountVisibleProjectiles(
                archerCount,
                firstShotSequence,
                archerCount);

            Assert.That(ArcherSalvoPresentationUtility.GetSamplingStride(archerCount),
                Is.EqualTo(21));
            Assert.That(visibleCount, Is.InRange(47, 48));
            Assert.That(visibleCount,
                Is.LessThanOrEqualTo(ArcherSalvoPresentationUtility.MaxVisibleProjectilesPerSalvo));
        }

        [Test]
        public void ConsecutiveThousandArcherVolleys_RotateRepresentativeLanes()
        {
            const int archerCount = 1_000;
            var firstVolley = CollectVisibleOffsets(archerCount, 1L, archerCount);
            var secondVolley = CollectVisibleOffsets(archerCount, 1_001L, archerCount);

            Assert.That(firstVolley.Count, Is.InRange(47, 48));
            Assert.That(secondVolley.Count, Is.InRange(47, 48));
            Assert.That(secondVolley.SetEquals(firstVolley), Is.False,
                "Ardisik salvolar ayni temsilci seridini tekrar etmemeli.");
        }

        private static int CountVisibleProjectiles(
            int sourceCount,
            long firstShotSequence,
            int projectileCount)
        {
            int visibleCount = 0;
            for (int index = 0; index < projectileCount; index++)
            {
                if (ArcherSalvoPresentationUtility.IsVisualRepresentative(
                        sourceCount,
                        firstShotSequence + index))
                    visibleCount++;
            }

            return visibleCount;
        }

        private static HashSet<int> CollectVisibleOffsets(
            int sourceCount,
            long firstShotSequence,
            int projectileCount)
        {
            var visibleOffsets = new HashSet<int>();
            for (int index = 0; index < projectileCount; index++)
            {
                if (ArcherSalvoPresentationUtility.IsVisualRepresentative(
                        sourceCount,
                        firstShotSequence + index))
                    visibleOffsets.Add(index);
            }

            return visibleOffsets;
        }
    }
}
