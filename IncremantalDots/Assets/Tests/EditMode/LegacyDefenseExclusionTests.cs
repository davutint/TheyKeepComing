using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        [Test]
        public void ActiveRuntime_HasSingleWallDestroyedGameOverWriter()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string editorSegment = $"{Path.DirectorySeparatorChar}Editor{Path.DirectorySeparatorChar}";

            string[] gameOverWriters = Directory
                .GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => path.IndexOf(editorSegment, StringComparison.OrdinalIgnoreCase) < 0)
                .Where(path => Regex.IsMatch(
                    File.ReadAllText(path),
                    @"\bIsGameOver\s*=\s*true\s*;"))
                .Select(path => path.Substring(scriptsRoot.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Replace('\\', '/'))
                .OrderBy(path => path)
                .ToArray();

            Assert.That(gameOverWriters, Is.EqualTo(new[]
            {
                "ECS/Systems/DamageApplySystem.cs"
            }), "Final wave, boss veya ikinci fail phase ayri bir Game Over writer'i eklememeli.");

            string damageApplySource = File.ReadAllText(Path.Combine(
                scriptsRoot, "ECS", "Systems", "DamageApplySystem.cs"));
            Assert.That(damageApplySource,
                Does.Contain("if (SingleWallDefenseRules.IsDestroyed(remainingWallHp))"),
                "Tek terminal writer yalniz Wall destroyed kontratiyla guardlanmali.");

            string gameManagerSource = File.ReadAllText(Path.Combine(
                scriptsRoot, "MonoBehaviour", "GameManager.cs"));
            Assert.That(Regex.Matches(
                gameManagerSource,
                Regex.Escape("OnGameOver?.Invoke();")).Count,
                Is.EqualTo(1));
            Assert.That(gameManagerSource,
                Does.Contain("if (GameState.IsGameOver && !prevGameState.IsGameOver)"),
                "Presentation ve death transaction'i yalniz authoritative rising edge'i izlemeli.");
        }
    }
}
