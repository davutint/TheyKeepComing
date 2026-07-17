using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace DeadWalls.Tests
{
    public class ActiveAbilityRulesTests
    {
        [Test]
        public void AbilityContract_ContentAssetsOwnUnlockAndTuningInputs()
        {
            DifficultyProfileSO profile = AssetDatabase.LoadAssetAtPath<DifficultyProfileSO>(
                "Assets/ScriptableObject/MobileCastle/Difficulty/DefaultDifficulty.asset");
            TechNodeDefinitionSO unlock = AssetDatabase.LoadAssetAtPath<TechNodeDefinitionSO>(
                "Assets/ScriptableObject/MobileCastle/TechTree/ArcaneTower.asset");
            TechNodeDefinitionSO damage = AssetDatabase.LoadAssetAtPath<TechNodeDefinitionSO>(
                "Assets/ScriptableObject/MobileCastle/TechTree/SearingFlames.asset");
            TechNodeDefinitionSO radius = AssetDatabase.LoadAssetAtPath<TechNodeDefinitionSO>(
                "Assets/ScriptableObject/MobileCastle/TechTree/GreaterBlast.asset");
            TechNodeDefinitionSO cooldown = AssetDatabase.LoadAssetAtPath<TechNodeDefinitionSO>(
                "Assets/ScriptableObject/MobileCastle/TechTree/ArcaneFocus.asset");

            Assert.That(profile, Is.Not.Null);
            Assert.That(unlock, Is.Not.Null);
            Assert.That(damage, Is.Not.Null);
            Assert.That(radius, Is.Not.Null);
            Assert.That(cooldown, Is.Not.Null);
            Assert.That(profile.WallBaseHp, Is.EqualTo(350f));
            Assert.That(profile.NormalRepairHealPercent, Is.EqualTo(0.25f));
            Assert.That(profile.RepairStonePerMissingHp, Is.EqualTo(0.10f));
            Assert.That(profile.RepairDayPriceMultiplier, Is.EqualTo(1f));
            Assert.That(profile.RallyCooldown, Is.EqualTo(60f));
            Assert.That(profile.EmergencyRepairHealPercent, Is.EqualTo(0.20f));
            Assert.That(profile.EmergencyRepairCooldown, Is.EqualTo(120f));

            Assert.That(unlock.Effects.Single().Type,
                Is.EqualTo(TechNodeEffectType.UnlockSpellcasting));
            Assert.That(damage.Effects.Single().Type,
                Is.EqualTo(TechNodeEffectType.ModifySpellDamagePercent));
            Assert.That(damage.Effects.Single().Value, Is.EqualTo(0.20f));
            Assert.That(radius.Effects.Single().Type,
                Is.EqualTo(TechNodeEffectType.AddSpellRadius));
            Assert.That(radius.Effects.Single().Value, Is.EqualTo(0.40f));
            Assert.That(cooldown.Effects.Single().Type,
                Is.EqualTo(TechNodeEffectType.ReduceSpellCooldownPercent));
            Assert.That(cooldown.Effects.Single().Value, Is.EqualTo(0.10f));
        }

        [Test]
        public void Rally_IsGuardedOnlyByUnlockCooldownActiveStateAndGameState()
        {
            Assert.That(ActiveAbilityRules.CanUseRally(true, 0f, 0f, false, false), Is.True);
            Assert.That(ActiveAbilityRules.CanUseRally(false, 0f, 0f, false, false), Is.False);
            Assert.That(ActiveAbilityRules.CanUseRally(true, 1f, 0f, false, false), Is.False);
            Assert.That(ActiveAbilityRules.CanUseRally(true, 0f, 1f, false, false), Is.False);
            Assert.That(ActiveAbilityRules.CanUseRally(true, 0f, 0f, true, false), Is.False);
        }

        [Test]
        public void EmergencyRepair_IsNightOnlyAndCannotReviveDestroyedWall()
        {
            Assert.That(ActiveAbilityRules.CanUseEmergencyRepair(
                true, 0f, SiegeCyclePhase.Night, 50f, 100f, false, false), Is.True);
            Assert.That(ActiveAbilityRules.CanUseEmergencyRepair(
                true, 0f, SiegeCyclePhase.Day, 50f, 100f, false, false), Is.False);
            Assert.That(ActiveAbilityRules.CanUseEmergencyRepair(
                true, 0f, SiegeCyclePhase.Night, 0f, 100f, false, false), Is.False);
            Assert.That(ActiveAbilityRules.CanUseEmergencyRepair(
                true, 0f, SiegeCyclePhase.Night, 100f, 100f, false, false), Is.False);
        }

        [Test]
        public void SameFrameLethalDamage_WinsBeforeEmergencyRepairGuard()
        {
            float afterDamage = SingleWallDefenseRules.ApplyDamage(25f, 25f);
            bool canRepair = ActiveAbilityRules.CanUseEmergencyRepair(
                true, 0f, SiegeCyclePhase.Night, afterDamage, 500f, false, false);
            float afterRepair = canRepair
                ? SingleWallDefenseRules.HealByMaxPercent(afterDamage, 500f, 0.20f)
                : afterDamage;

            Assert.That(canRepair, Is.False);
            Assert.That(afterRepair, Is.Zero);
        }
    }
}
