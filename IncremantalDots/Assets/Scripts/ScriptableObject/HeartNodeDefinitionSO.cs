using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    public enum HeartNodeType
    {
        Unlock = 0,
        Repeatable = 1,
        Evolution = 2,
        Keystone = 3
    }

    public enum HeartNodeBranch
    {
        Army = 0,
        Defense = 1,
        Production = 2,
        HeartMagic = 3
    }

    public enum HeartNodeRarity
    {
        Standard = 0,
        Rare = 1
    }

    public enum HeartNodeEffectType
    {
        None = 0,
        UnlockArcherType = 1,
        UnlockSpellcasting = 2,
        ModifyArcherDamagePercent = 3,
        ModifyArcherFireRatePercent = 4,
        ModifyWallMaxHpPercent = 5,
        ReduceWallRepairCostPercent = 6,
        IncreaseWorkerCapacity = 7,
        IncreaseResourceProductionPercent = 8,
        IncreasePopulationGrowth = 9,
        ModifySpellDamagePercent = 10,
        AddSpellRadius = 11,
        ReduceSpellCooldownPercent = 12,
        EnableSplitShot = 13,
        EnableBurningGround = 14
    }

    [Serializable]
    public struct HeartNodeEffect
    {
        public HeartNodeEffectType Type;
        public float Value;
        public ArcherType ArcherType;
        public EconomyFocusType Resource;
    }

    /// <summary>
    /// Castle Heart havuzundaki bir node'un yalniz authored, degismez tanimidir.
    /// Reveal, level ve lock gibi run state bu asset'te tutulmaz.
    /// </summary>
    [CreateAssetMenu(
        fileName = "HeartNodeDefinition",
        menuName = "DeadWalls/Castle Heart/Heart Node Definition")]
    public sealed class HeartNodeDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id = "heart_node";
        public string Title = "Heart Node";
        [TextArea] public string Description = string.Empty;
        public Sprite Icon;
        public string[] Tags = Array.Empty<string>();

        [Header("Classification")]
        public HeartNodeType Type = HeartNodeType.Unlock;
        public HeartNodeBranch Branch = HeartNodeBranch.Army;
        public HeartNodeRarity Rarity = HeartNodeRarity.Standard;

        [Header("Generator Eligibility")]
        [Min(0)] public int MinimumDepth;
        [Min(0)] public int MaximumDepth = 8;

        [Header("Grave Essence Cost")]
        [Min(1)] public long BaseGraveEssenceCost = 10;
        [Min(0f)] public double CostGrowthPerLevel;

        [Header("Keystone Conflict")]
        [Tooltip("Yalniz Keystone node'unda kullanilir. Satin alinan Keystone sadece eslestigi node'u kilitler.")]
        public string[] ConflictNodeIds = Array.Empty<string>();

        [Header("Effects")]
        public HeartNodeEffect[] Effects = Array.Empty<HeartNodeEffect>();

        public bool IsRepeatable => Type == HeartNodeType.Repeatable;

        public void CollectValidationErrors(List<string> errors)
        {
            if (errors == null)
                throw new ArgumentNullException(nameof(errors));

            if (string.IsNullOrWhiteSpace(Id))
                errors.Add("Id bos olamaz.");
            if (MinimumDepth < 0)
                errors.Add("MinimumDepth negatif olamaz.");
            if (MaximumDepth < MinimumDepth)
                errors.Add("MaximumDepth, MinimumDepth degerinden kucuk olamaz.");
            if (BaseGraveEssenceCost <= 0)
                errors.Add("BaseGraveEssenceCost sifirdan buyuk olmalidir.");
            if (double.IsNaN(CostGrowthPerLevel)
                || double.IsInfinity(CostGrowthPerLevel)
                || CostGrowthPerLevel < 0d)
            {
                errors.Add("CostGrowthPerLevel sonlu ve negatif olmayan bir deger olmalidir.");
            }

            string[] conflicts = ConflictNodeIds ?? Array.Empty<string>();
            if (Type == HeartNodeType.Keystone)
            {
                if (conflicts.Length != 1 || string.IsNullOrWhiteSpace(conflicts[0]))
                    errors.Add("Keystone tam olarak bir karsi Keystone Id'si tasimalidir.");
            }
            else if (conflicts.Length > 0)
            {
                errors.Add("ConflictNodeIds yalniz Keystone node'larinda kullanilabilir.");
            }

            var seenConflicts = new HashSet<string>(StringComparer.Ordinal);
            foreach (string conflictId in conflicts)
            {
                if (string.IsNullOrWhiteSpace(conflictId))
                    continue;
                if (string.Equals(conflictId, Id, StringComparison.Ordinal))
                    errors.Add("Node kendisiyle conflict olamaz.");
                if (!seenConflicts.Add(conflictId))
                    errors.Add($"Tekrarlanan conflict Id: {conflictId}");
            }
        }
    }
}
