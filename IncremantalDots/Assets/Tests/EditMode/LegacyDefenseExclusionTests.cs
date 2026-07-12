using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class LegacyDefenseExclusionTests
    {
        [Test]
        public void RunSaveSchema_ContainsWallState_ButNoGateOrCoreState()
        {
            Assert.That(typeof(RunSaveState).GetField(nameof(RunSaveState.WallCurrentHP)), Is.Not.Null);
            Assert.That(typeof(RunSaveState).GetField("GateCurrentHP"), Is.Null);
            Assert.That(typeof(RunSaveState).GetField("GateMaxHP"), Is.Null);
            Assert.That(typeof(RunSaveState).GetField("CastleCurrentHP"), Is.Null);
            Assert.That(typeof(RunSaveState).GetField("CoreCurrentHP"), Is.Null);
        }

        [Test]
        public void LegacyDefenseComponents_RemainSerializableDataOnly()
        {
            Assert.That(typeof(GateComponent).IsValueType, Is.True);
            Assert.That(typeof(CastleHP).IsValueType, Is.True);
        }
    }
}
