using NUnit.Framework;
using Unity.Mathematics;

namespace DeadWalls.Tests
{
    public class ZombieQueueFlowUtilityTests
    {
        [TestCase(ZombieStateType.Moving, true)]
        [TestCase(ZombieStateType.Queued, true)]
        [TestCase(ZombieStateType.Attacking, false)]
        [TestCase(ZombieStateType.Dead, false)]
        public void ForwardPressure_IsAvailableToMovingAndQueuedOnly(
            ZombieStateType state,
            bool expected)
        {
            Assert.That(ZombieQueueFlowUtility.ReceivesForwardPressure(state), Is.EqualTo(expected));
        }

        [Test]
        public void SingleFront_OnlyCloserToWallNeighborBlocks()
        {
            float2 follower = new float2(5f, 0f);

            Assert.That(ZombieQueueFlowUtility.IsAheadOf(
                true, true, float2.zero, follower, new float2(4.8f, 0f)), Is.True);
            Assert.That(ZombieQueueFlowUtility.IsAheadOf(
                true, true, float2.zero, follower, new float2(5f, 0.2f)), Is.False);
            Assert.That(ZombieQueueFlowUtility.IsAheadOf(
                true, true, float2.zero, follower, new float2(5.2f, 0f)), Is.False);
        }

        [Test]
        public void EqualProgressQueuedNeighbors_CannotMutuallyBlock()
        {
            float2 first = new float2(5f, -0.1f);
            float2 second = new float2(5f, 0.1f);

            Assert.That(ZombieQueueFlowUtility.IsAheadOf(
                true, true, float2.zero, first, second), Is.False);
            Assert.That(ZombieQueueFlowUtility.IsAheadOf(
                true, true, float2.zero, second, first), Is.False);
        }

        [Test]
        public void RadialFront_OnlyNeighborCloserToCastleCenterBlocks()
        {
            float2 follower = new float2(5f, 0f);

            Assert.That(ZombieQueueFlowUtility.IsAheadOf(
                true, false, float2.zero, follower, new float2(4.8f, 0f)), Is.True);
            Assert.That(ZombieQueueFlowUtility.IsAheadOf(
                true, false, float2.zero, follower, new float2(0f, 5f)), Is.False);
            Assert.That(ZombieQueueFlowUtility.IsAheadOf(
                true, false, float2.zero, follower, new float2(5.2f, 0f)), Is.False);
        }

        [Test]
        public void LegacyWall_UsesSameForwardProgressRule()
        {
            float2 follower = new float2(5f, 0f);

            Assert.That(ZombieQueueFlowUtility.IsAheadOf(
                false, false, float2.zero, follower, new float2(4.8f, 0f)), Is.True);
            Assert.That(ZombieQueueFlowUtility.IsAheadOf(
                false, false, float2.zero, follower, new float2(5f, 0.2f)), Is.False);
        }

        [Test]
        public void SingleFront_DiagonalAheadNeighbor_DoesNotBlockOpenLane()
        {
            float2 follower = new float2(5f, 0f);

            Assert.That(ZombieQueueFlowUtility.CanBlockQueue(
                true, true, float2.zero, follower, new float2(4.80f, 0.05f)), Is.True);
            Assert.That(ZombieQueueFlowUtility.CanBlockQueue(
                true, true, float2.zero, follower, new float2(4.95f, 0.25f)), Is.False);
        }

        [Test]
        public void RadialFront_TangentNeighbor_DoesNotBlockOpenLane()
        {
            float2 follower = new float2(5f, 0f);

            Assert.That(ZombieQueueFlowUtility.CanBlockQueue(
                true, false, float2.zero, follower, new float2(4.80f, 0.05f)), Is.True);
            Assert.That(ZombieQueueFlowUtility.CanBlockQueue(
                true, false, float2.zero, follower, new float2(4.95f, 0.25f)), Is.False);
        }

        [Test]
        public void LegacyWall_DiagonalAheadNeighbor_UsesSameLaneRule()
        {
            float2 follower = new float2(5f, 0f);

            Assert.That(ZombieQueueFlowUtility.CanBlockQueue(
                false, false, float2.zero, follower, new float2(4.80f, 0.05f)), Is.True);
            Assert.That(ZombieQueueFlowUtility.CanBlockQueue(
                false, false, float2.zero, follower, new float2(4.95f, 0.25f)), Is.False);
        }
    }
}
