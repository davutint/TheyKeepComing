using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls.Tests
{
    public class ArcherTargetingUtilityTests
    {
        [Test]
        public void GetCellRadius_CoversRangeAndOneFrameSnapshotPadding()
        {
            Assert.That(ArcherTargetingUtility.GetCellRadius(15f, 2f), Is.EqualTo(9));
            Assert.That(ArcherTargetingUtility.GetCellRadius(-5f, 2f), Is.EqualTo(1));
        }

        [Test]
        public void DistanceSqToCell_ReturnsZeroInsideAndExactGapOutside()
        {
            Assert.That(SpatialHash.DistanceSqToCell(
                new float2(2.5f, 3.5f), new int2(1, 1), 2f), Is.EqualTo(0f));
            Assert.That(SpatialHash.DistanceSqToCell(
                new float2(0f, 0f), new int2(2, 0), 2f), Is.EqualTo(16f));
        }

        [Test]
        public void HasUnreservedHealth_RejectsLethallyReservedTarget()
        {
            Assert.That(ArcherTargetingUtility.HasUnreservedHealth(10f, 9f), Is.True);
            Assert.That(ArcherTargetingUtility.HasUnreservedHealth(10f, 10f), Is.False);
            Assert.That(ArcherTargetingUtility.HasUnreservedHealth(10f, 12f), Is.False);
            Assert.That(ArcherTargetingUtility.HasUnreservedHealth(0f, 0f), Is.False);
        }

        [Test]
        public void IsBetterCandidate_UsesStableEntityOrderForDistanceTie()
        {
            Entity lower = new Entity { Index = 4, Version = 1 };
            Entity higher = new Entity { Index = 9, Version = 1 };

            Assert.That(ArcherTargetingUtility.IsBetterCandidate(
                4f, lower, 4f, higher), Is.True);
            Assert.That(ArcherTargetingUtility.IsBetterCandidate(
                4f, higher, 4f, lower), Is.False);
            Assert.That(ArcherTargetingUtility.IsBetterCandidate(
                3f, higher, 4f, lower), Is.True);
        }
    }
}
