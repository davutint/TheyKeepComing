#if UNITY_EDITOR || DEVELOPMENT_BUILD
using NUnit.Framework;
using Unity.Mathematics;

namespace DeadWalls.Tests
{
    public class DevelopmentTestRulesTests
    {
        [TestCase(DevelopmentTestRules.Horde2K)]
        [TestCase(DevelopmentTestRules.Horde5K)]
        [TestCase(DevelopmentTestRules.Horde10K)]
        public void SupportedHordeSizes_AreExactApprovedPresets(int count)
        {
            Assert.That(DevelopmentTestRules.IsSupportedHordeSize(count), Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(1_999)]
        [TestCase(2_001)]
        [TestCase(20_000)]
        public void UnsupportedHordeSizes_AreRejected(int count)
        {
            Assert.That(DevelopmentTestRules.IsSupportedHordeSize(count), Is.False);
        }

        [Test]
        public void GridPosition_KeepsTenKCenteredAndOnUnitRenderDepth()
        {
            float3 first = DevelopmentTestRules.GetGridPosition(0, 10_000, 13f);
            float3 last = DevelopmentTestRules.GetGridPosition(9_999, 10_000, 13f);

            Assert.That((first.x + last.x) * 0.5f, Is.EqualTo(13f).Within(0.0001f));
            Assert.That((first.y + last.y) * 0.5f, Is.Zero.Within(0.0001f));
            Assert.That(first.z, Is.EqualTo(MobileCastleRenderDepth.UnitZ));
            Assert.That(last.z, Is.EqualTo(MobileCastleRenderDepth.UnitZ));
            Assert.That(last.x - first.x, Is.EqualTo(99f * DevelopmentTestRules.HorizontalSpacing)
                .Within(0.0001f));
            Assert.That(last.y - first.y, Is.EqualTo(99f * DevelopmentTestRules.VerticalSpacing)
                .Within(0.0001f));
        }
    }
}
#endif
