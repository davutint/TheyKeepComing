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
        EnableBurningGround = 14,
        AddArcherRange = 15,
        ReduceFrostSlowMultiplier = 16,
        IncreaseArrowCapacity = 17,
        IncreaseArrowEfficiency = 18,
        EnableSecondBlast = 19
    }

    [Serializable]
    public struct HeartNodeEffect
    {
        public HeartNodeEffectType Type;
        public double Value;
        public ArcherType ArcherType;
        public EconomyFocusType Resource;

        [Tooltip("Soft-cap kullanan effect icin authored asimptotik limit. Linear effect'lerde 0 kalir.")]
        public double SoftCap;
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

        [Header("Legacy Migration Provenance")]
        [Tooltip("Bu launch node'una fikir/effect kaynagi olan dormant TechTree node Id'leri. Runtime progression icin kullanilmaz.")]
        public string[] LegacySourceNodeIds = Array.Empty<string>();

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

            HeartNodeEffect[] effects = Effects ?? Array.Empty<HeartNodeEffect>();
            for (int i = 0; i < effects.Length; i++)
                CollectEffectValidationErrors(effects[i], i, errors);

            string[] legacySourceIds = LegacySourceNodeIds ?? Array.Empty<string>();
            var seenLegacySourceIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < legacySourceIds.Length; i++)
            {
                string sourceId = legacySourceIds[i];
                if (string.IsNullOrWhiteSpace(sourceId))
                {
                    errors.Add($"LegacySourceNodeIds[{i}] bos olamaz.");
                    continue;
                }

                if (!seenLegacySourceIds.Add(sourceId))
                    errors.Add($"Tekrarlanan legacy source Id: {sourceId}");
            }
        }

        private static void CollectEffectValidationErrors(
            HeartNodeEffect effect,
            int index,
            List<string> errors)
        {
            if (effect.Type == HeartNodeEffectType.None)
            {
                errors.Add($"Effects[{index}] None olamaz.");
                return;
            }

            if (IsBehaviorEffect(effect.Type))
                return;

            if (double.IsNaN(effect.Value)
                || double.IsInfinity(effect.Value)
                || effect.Value <= 0f)
            {
                errors.Add($"Effects[{index}] Value sonlu ve sifirdan buyuk olmalidir.");
            }

            if (RequiresPositiveSoftCap(effect.Type)
                && (double.IsNaN(effect.SoftCap)
                    || double.IsInfinity(effect.SoftCap)
                    || effect.SoftCap <= 0f))
            {
                errors.Add($"Effects[{index}] {effect.Type} icin pozitif SoftCap gerektirir.");
            }

            if ((effect.Type == HeartNodeEffectType.ReduceSpellCooldownPercent
                 || effect.Type == HeartNodeEffectType.ReduceFrostSlowMultiplier)
                && effect.SoftCap >= 1f)
            {
                errors.Add($"Effects[{index}] {effect.Type} SoftCap degeri 1'den kucuk olmalidir.");
            }

            if (effect.Type == HeartNodeEffectType.ReduceFrostSlowMultiplier
                && effect.ArcherType != ArcherType.Frost)
            {
                errors.Add($"Effects[{index}] Frost slow effect'i ArcherType.Frost hedeflemelidir.");
            }
        }

        private static bool IsBehaviorEffect(HeartNodeEffectType type)
        {
            return type == HeartNodeEffectType.UnlockArcherType
                   || type == HeartNodeEffectType.UnlockSpellcasting
                   || type == HeartNodeEffectType.EnableSplitShot
                   || type == HeartNodeEffectType.EnableBurningGround
                   || type == HeartNodeEffectType.EnableSecondBlast;
        }

        private static bool RequiresPositiveSoftCap(HeartNodeEffectType type)
        {
            return type == HeartNodeEffectType.ModifyArcherFireRatePercent
                   || type == HeartNodeEffectType.AddArcherRange
                   || type == HeartNodeEffectType.ReduceFrostSlowMultiplier
                   || type == HeartNodeEffectType.AddSpellRadius
                   || type == HeartNodeEffectType.ReduceSpellCooldownPercent;
        }
    }
}
