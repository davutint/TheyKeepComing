using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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

        [Test]
        public void ActiveHudContract_ContainsOnlyWallDefensePresentation()
        {
            Assert.That(typeof(HUDController).GetField("WallHPBar"), Is.Not.Null);
            Assert.That(typeof(HUDController).GetField("DefenseWallText"), Is.Not.Null);
            Assert.That(typeof(HUDController).GetField("DefenseWallFill"), Is.Not.Null);
            Assert.That(typeof(HUDController).GetField("GateHPBar"), Is.Null);
            Assert.That(typeof(HUDController).GetField("CastleHPBar"), Is.Null);
            Assert.That(typeof(HUDController).GetField("DefenseGateText"), Is.Null);
            Assert.That(typeof(HUDController).GetField("DefenseCoreText"), Is.Null);
            Assert.That(typeof(HUDController).GetField("DefenseGateFill"), Is.Null);
            Assert.That(typeof(HUDController).GetField("DefenseCoreFill"), Is.Null);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab");
            Assert.That(prefab, Is.Not.Null);

            string[] defenseObjectNames = prefab.GetComponentsInChildren<Transform>(true)
                .Select(transform => transform.name)
                .ToArray();
            Assert.That(defenseObjectNames, Does.Contain("DefenseWallText"));
            Assert.That(defenseObjectNames, Does.Contain("DefenseWallFill"));
            Assert.That(defenseObjectNames, Has.None.EqualTo("DefenseGateText"));
            Assert.That(defenseObjectNames, Has.None.EqualTo("DefenseGateTrack"));
            Assert.That(defenseObjectNames, Has.None.EqualTo("DefenseGateFill"));
            Assert.That(defenseObjectNames, Has.None.EqualTo("DefenseCoreText"));
            Assert.That(defenseObjectNames, Has.None.EqualTo("DefenseCoreTrack"));
            Assert.That(defenseObjectNames, Has.None.EqualTo("DefenseCoreFill"));
        }
    }
}
