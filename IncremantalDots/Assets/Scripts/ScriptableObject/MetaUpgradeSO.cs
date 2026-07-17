using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Blueprint v1.0 sabit meta katalog effect'leri. Serialized uyumluluk icin kaldirilan
    /// StartingTechLevel=3 ve ArcherDamagePercent=5 degerleri bilerek yeniden kullanilmaz.
    /// </summary>
    public enum MetaUpgradeEffectType
    {
        None = 0,

        /// <summary>Kosuya ekstra kaynakla basla (Resource alani hedefi).</summary>
        StartingResource = 1,

        /// <summary>Kosuya ekstra Basic okcuyla basla; Rapid/Frost acmaz.</summary>
        StartingArchers = 2,

        /// <summary>Duvar MaxHP'sine kalici yuzde (0.05 = +%5/seviye).</summary>
        WallHpPercent = 4,

        /// <summary>Tum worker uretimine kalici yuzde (0.03 = +%3/seviye).</summary>
        ProductionPercent = 6,

        /// <summary>Kosuya ekstra yatak tabaniyla basla; run yatak fiyat egrisi devam eder.</summary>
        StartingBeds = 7,

        /// <summary>Her Wood basina alinan ok sayisina kalici additive bonus.</summary>
        ArrowEfficiency = 8,

        /// <summary>Run-ici Grave Essence kazancina kalici yuzde.</summary>
        EssenceGainPercent = 9,

        /// <summary>Yalniz gelecekte uretilecek Heart graph'larinin olasi content havuzunu acar.</summary>
        NodePoolUnlock = 10,
    }

    /// <summary>
    /// Meta etkilerinin kosu sinirini tek yerde tanimlar. Meta yeni kosunun baslangic
    /// degerlerine/aggregate carpanlarina katkida bulunabilir veya gelecek graph'lar icin
    /// stable pool Id acabilir; aktif generated graph node/edge/Keystone sonucunu degistiremez.
    /// </summary>
    public static class MetaUpgradePolicy
    {
        public static bool IsRunGraphIsolatedEffect(MetaUpgradeEffectType effectType)
        {
            switch (effectType)
            {
                case MetaUpgradeEffectType.StartingResource:
                case MetaUpgradeEffectType.StartingArchers:
                case MetaUpgradeEffectType.WallHpPercent:
                case MetaUpgradeEffectType.ProductionPercent:
                case MetaUpgradeEffectType.StartingBeds:
                case MetaUpgradeEffectType.ArrowEfficiency:
                case MetaUpgradeEffectType.EssenceGainPercent:
                case MetaUpgradeEffectType.NodePoolUnlock:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsRunStartEffect(MetaUpgradeEffectType effectType)
        {
            return IsRunGraphIsolatedEffect(effectType)
                   && effectType != MetaUpgradeEffectType.NodePoolUnlock;
        }

        public static bool IsContentUnlockEffect(MetaUpgradeEffectType effectType)
        {
            return effectType == MetaUpgradeEffectType.NodePoolUnlock;
        }
    }

    /// <summary>
    /// Tek bir kalici meta yukseltmesi. Repeatable maliyet:
    /// ceil(BaseCost * (1 + CostGrowthPerLevel)^currentLevel). MaxLevel=0 limitsiz sink'tir.
    /// </summary>
    [CreateAssetMenu(fileName = "MetaUpgrade", menuName = "DeadWalls/Mobile Castle/Meta Upgrade")]
    public class MetaUpgradeSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id = "upgrade";
        public string Title = "UPGRADE";
        [TextArea(1, 2)] public string Description = "";

        [Header("Cost (Souls)")]
        public int BaseCost = 150;
        [Tooltip("Ustel maliyet tabani. 0.6 = her seviyede onceki fiyat x1.6.")]
        public float CostGrowthPerLevel = 0.6f;
        [Tooltip("0 = limitsiz repeatable sink; pozitif deger hard level cap.")]
        [Min(0)] public int MaxLevel = 5;

        [Header("Effect")]
        public MetaUpgradeEffectType EffectType = MetaUpgradeEffectType.StartingResource;
        [Tooltip("Seviye basina etki: kaynak/okcu/yatak adedi, additive ok verimi veya yuzde orani.")]
        public float ValuePerLevel = 75f;
        [Tooltip("StartingResource icin hedef kaynak.")]
        public EconomyFocusType Resource = EconomyFocusType.Balanced;
        [Tooltip("NodePoolUnlock icin future graph content havuzunun stable Id'si.")]
        public string PoolContentId = string.Empty;

        public bool IsRepeatable => MaxLevel == 0;

        public bool IsMaxLevel(int currentLevel)
        {
            return MaxLevel > 0 && Math.Max(0, currentLevel) >= MaxLevel;
        }

        public int GetCost(int currentLevel)
        {
            int level = Math.Max(0, currentLevel);
            double growth = Math.Max(0d, CostGrowthPerLevel);
            double raw = Math.Max(1, BaseCost) * Math.Pow(1d + growth, level);
            if (double.IsNaN(raw) || double.IsInfinity(raw) || raw >= int.MaxValue)
                return int.MaxValue;

            return Math.Max(1, (int)Math.Ceiling(raw));
        }

        public double GetTotalEffect(int currentLevel)
        {
            int level = Math.Max(0, currentLevel);
            if (MetaUpgradePolicy.IsContentUnlockEffect(EffectType))
                return level > 0 ? 1d : 0d;

            double total = (double)ValuePerLevel * level;
            if (double.IsNaN(total) || total <= 0d)
                return 0d;
            return double.IsInfinity(total) ? double.MaxValue : total;
        }

        public bool IsConfigurationValid()
        {
            if (string.IsNullOrWhiteSpace(Id)
                || BaseCost <= 0
                || float.IsNaN(CostGrowthPerLevel)
                || float.IsInfinity(CostGrowthPerLevel)
                || CostGrowthPerLevel < 0f
                || MaxLevel < 0
                || !MetaUpgradePolicy.IsRunGraphIsolatedEffect(EffectType))
            {
                return false;
            }

            if (MetaUpgradePolicy.IsContentUnlockEffect(EffectType))
                return MaxLevel == 1 && !string.IsNullOrWhiteSpace(PoolContentId);

            return !float.IsNaN(ValuePerLevel)
                   && !float.IsInfinity(ValuePerLevel)
                   && ValuePerLevel > 0f
                   && (!IsRepeatable || CostGrowthPerLevel > 0f);
        }

        public void CollectValidationErrors(List<string> problems)
        {
            if (problems == null)
                throw new ArgumentNullException(nameof(problems));

            string label = string.IsNullOrWhiteSpace(Id) ? name : Id;
            if (string.IsNullOrWhiteSpace(Id)) problems.Add($"'{name}' Id bos.");
            if (BaseCost <= 0) problems.Add($"'{label}' BaseCost sifirdan buyuk olmali.");
            if (float.IsNaN(CostGrowthPerLevel) || float.IsInfinity(CostGrowthPerLevel)
                || CostGrowthPerLevel < 0f)
                problems.Add($"'{label}' CostGrowthPerLevel sonlu ve negatif olmayan bir deger olmali.");
            if (MaxLevel < 0) problems.Add($"'{label}' MaxLevel negatif olamaz.");
            if (IsRepeatable && CostGrowthPerLevel <= 0f)
                problems.Add($"'{label}' repeatable sink buyuyen maliyet tasimali.");

            if (!MetaUpgradePolicy.IsRunGraphIsolatedEffect(EffectType))
                problems.Add($"'{label}' effect '{EffectType}' run graph isolation politikasina aykiri.");

            if (MetaUpgradePolicy.IsContentUnlockEffect(EffectType))
            {
                if (MaxLevel != 1) problems.Add($"'{label}' content unlock MaxLevel=1 olmali.");
                if (string.IsNullOrWhiteSpace(PoolContentId))
                    problems.Add($"'{label}' content unlock stable PoolContentId tasimali.");
            }
            else if (float.IsNaN(ValuePerLevel) || float.IsInfinity(ValuePerLevel)
                     || ValuePerLevel <= 0f)
            {
                problems.Add($"'{label}' ValuePerLevel sonlu ve sifirdan buyuk olmali.");
            }
        }
    }
}
