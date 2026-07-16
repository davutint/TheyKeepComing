using NUnit.Framework;
using Unity.Mathematics;

namespace DeadWalls.Tests
{
    public class SurvivorArrivalVisualUtilityTests
    {
        [Test]
        public void VisualCountCapsRepresentationWithoutLosingAcceptedSurvivors()
        {
            int visualCount = SurvivorArrivalVisualUtility.GetVisualCount(37);
            int representedTotal = 0;
            for (int index = 0; index < visualCount; index++)
            {
                representedTotal += SurvivorArrivalVisualUtility.GetRepresentedSurvivorCount(
                    37, visualCount, index);
            }

            Assert.That(visualCount, Is.EqualTo(SurvivorArrivalVisualUtility.MaxVisualCount));
            Assert.That(representedTotal, Is.EqualTo(37));
        }

        [Test]
        public void RouteStartsOnRightAndEndsBehindWall()
        {
            const float frontlineX = -0.5f;
            float3 spawn = SurvivorArrivalVisualUtility.GetSpawnPosition(frontlineX, 0f, 0);
            float3 target = SurvivorArrivalVisualUtility.GetTargetPosition(frontlineX, 0f, 0);

            Assert.That(spawn.x, Is.GreaterThan(frontlineX));
            Assert.That(target.x, Is.LessThan(frontlineX));
            Assert.That(spawn.x, Is.GreaterThan(target.x));
            Assert.That(spawn.z, Is.EqualTo(MobileCastleRenderDepth.UnitZ));
            Assert.That(target.z, Is.EqualTo(MobileCastleRenderDepth.UnitZ));
        }

        [Test]
        public void FormationUsesDeterministicLaneSpeedAndDelayVariation()
        {
            float3 first = SurvivorArrivalVisualUtility.GetSpawnPosition(-0.5f, 0f, 0);
            float3 second = SurvivorArrivalVisualUtility.GetSpawnPosition(-0.5f, 0f, 1);

            Assert.That(second.y, Is.Not.EqualTo(first.y));
            Assert.That(SurvivorArrivalVisualUtility.GetMoveSpeed(1),
                Is.GreaterThan(SurvivorArrivalVisualUtility.GetMoveSpeed(0)));
            Assert.That(SurvivorArrivalVisualUtility.GetStartDelay(1),
                Is.GreaterThan(SurvivorArrivalVisualUtility.GetStartDelay(0)));
        }

        [Test]
        public void EntireBoundedFormationReachesBehindWallWithinDawn()
        {
            const float dawnDuration = 5f;
            const float frontlineX = -0.5f;
            float longestArrival = 0f;

            for (int index = 0; index < SurvivorArrivalVisualUtility.MaxVisualCount; index++)
            {
                float3 spawn = SurvivorArrivalVisualUtility.GetSpawnPosition(frontlineX, 0f, index);
                float3 target = SurvivorArrivalVisualUtility.GetTargetPosition(frontlineX, 0f, index);
                float duration = SurvivorArrivalVisualUtility.GetStartDelay(index)
                    + math.distance(spawn.xy, target.xy)
                    / SurvivorArrivalVisualUtility.GetMoveSpeed(index);
                longestArrival = math.max(longestArrival, duration);
            }

            Assert.That(longestArrival, Is.LessThan(dawnDuration));
        }
    }
}
