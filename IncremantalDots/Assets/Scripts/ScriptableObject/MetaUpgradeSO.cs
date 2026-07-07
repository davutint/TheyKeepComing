using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Meta upgrade etki tipleri. V1 odagi: baslangic ivmesi (Starting*) + hafif kalici guc
    /// (yuzdeler). Etkiler kosu BASINDA bir kez uygulanir (GameManager.ApplyMetaProgression);
    /// yuzdesel olanlar tech/council aggregate katmanlarina meta-bonus olarak katilir.
    /// </summary>
    public enum MetaUpgradeEffectType
    {
        None = 0,

        /// <summary>Kosuya ekstra kaynakla basla (Resource alani hedefi; Balanced = 4 kaynaga esit).</summary>
        StartingResource = 1,

        /// <summary>Kosuya ekstra Basic okcuyla basla (population tuketmez).</summary>
        StartingArchers = 2,

        /// <summary>Kosuya belirli tech node'u acik basla (TechNodeId; seviye = upgrade seviyesi, maliyetsiz).</summary>
        StartingTechLevel = 3,

        /// <summary>Duvar/Kapi/Cekirdek MaxHP'sine kalici yuzde (0.05 = +%5/seviye).</summary>
        WallHpPercent = 4,

        /// <summary>Tum okcu hasarina kalici yuzde (0.03 = +%3/seviye).</summary>
        ArcherDamagePercent = 5,

        /// <summary>Tum worker uretimine kalici yuzde (0.03 = +%3/seviye).</summary>
        ProductionPercent = 6,
    }

    /// <summary>
    /// Tek bir kalici meta yukseltmesi (olum ekrani magazasinda satilir; para birimi RUH —
    /// 1 oldurulen zombi = 1 Ruh). Maliyet merdiveni: Cost(seviye) = BaseCost * (1 + seviye * CostGrowthPerLevel).
    /// </summary>
    [CreateAssetMenu(fileName = "MetaUpgrade", menuName = "DeadWalls/Mobile Castle/Meta Upgrade")]
    public class MetaUpgradeSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id = "upgrade";
        public string Title = "UPGRADE";
        [TextArea(1, 2)] public string Description = "";

        [Header("Cost (Ruh)")]
        public int BaseCost = 150;
        [Tooltip("Seviye basina maliyet buyumesi (0.6 = her seviye +%60 taban).")]
        public float CostGrowthPerLevel = 0.6f;
        [Min(1)] public int MaxLevel = 5;

        [Header("Effect")]
        public MetaUpgradeEffectType EffectType = MetaUpgradeEffectType.StartingResource;
        [Tooltip("Seviye basina etki: kaynak adedi / okcu adedi / yuzde orani.")]
        public float ValuePerLevel = 75f;
        [Tooltip("StartingResource icin hedef kaynak (Balanced = 4 kaynaga esit dagitilir).")]
        public EconomyFocusType Resource = EconomyFocusType.Balanced;
        [Tooltip("StartingTechLevel icin tech node id'si (orn. moat_dig).")]
        public string TechNodeId = "";

        public int GetCost(int currentLevel)
        {
            return Mathf.Max(1, Mathf.RoundToInt(BaseCost * (1f + Mathf.Max(0, currentLevel) * Mathf.Max(0f, CostGrowthPerLevel))));
        }
    }
}
