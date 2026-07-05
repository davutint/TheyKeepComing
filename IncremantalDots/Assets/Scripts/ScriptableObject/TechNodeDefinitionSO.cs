using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Tech tree node effect tipleri. Sadece mevcut koda temiz baglanan etkiler tanimlidir;
    /// "gosteris icin" sahte effect eklenmez (bkz. TECH_TREE_SO_ARCHITECTURE.md).
    /// </summary>
    public enum TechNodeEffectType
    {
        None = 0,

        /// <summary>ArcherType alanindaki okcu tipini kalici acar (maliyet node'un kendi Cost'udur).</summary>
        UnlockArcherType = 1,

        /// <summary>Tum okcularin hasarina yuzdesel carpan ekler (Value 0.15 = +%15). Canli okculara aninda uygulanir.</summary>
        ModifyArcherDamagePercent = 2,

        /// <summary>Tum okcularin atis hizina yuzdesel carpan ekler (Value 0.12 = +%12). Canli okculara aninda uygulanir.</summary>
        ModifyArcherFireRatePercent = 3,

        /// <summary>Resource alanindaki kaynagin worker cap'ini Value kadar arttirir (Balanced = tum kaynaklar).</summary>
        IncreaseWorkerCap = 4,

        /// <summary>Resource alanindaki kaynagin worker uretimini yuzdesel arttirir (Balanced = tum kaynaklar).</summary>
        IncreaseResourceProductionPercent = 5,

        /// <summary>Cycle basina nufus buyumesini (PopulationGrowthPerDayPrep) Value kadar arttirir.</summary>
        IncreasePopulationGrowth = 6,

        /// <summary>Wall/Gate/Core MaxHP degerlerini yuzdesel arttirir; CurrentHP orani korunur.</summary>
        IncreaseDefenseMaxHpPercent = 7,
    }

    /// <summary>
    /// Tek bir tech effect'i. Alanlar Type'a gore yorumlanir:
    /// yuzdesel etkilerde Value orandir (0.15 = +%15), sayac etkilerde tam degerdir (cap +6, growth +5).
    /// MaxLevel > 1 node'larda her satin alinan seviyede etkiler bir kez daha uygulanir (additif birikir).
    /// </summary>
    [System.Serializable]
    public struct TechNodeEffect
    {
        public TechNodeEffectType Type;

        [Tooltip("Yuzdesel etkilerde oran (0.15 = +%15); IncreaseWorkerCap/PopulationGrowth icin tam sayi deger.")]
        public float Value;

        [Tooltip("Sadece UnlockArcherType icin okunur.")]
        public ArcherType ArcherType;

        [Tooltip("IncreaseWorkerCap / IncreaseResourceProductionPercent hedefi; Balanced = tum kaynaklar.")]
        public EconomyFocusType Resource;
    }

    /// <summary>
    /// Tech tree'deki tek bir node'un sabit datasi. Runtime durum (seviye/reveal) burada TUTULMAZ;
    /// GameManager run-scoped state'inde yasar. Yeni tech eklemek = yeni asset + parent'in
    /// RevealChildNodeIds listesine Id eklemek + catalog'a koymak (UI degisikligi gerekmez).
    /// </summary>
    [CreateAssetMenu(fileName = "TechNodeDefinition", menuName = "DeadWalls/Mobile Castle/Tech Node Definition")]
    public class TechNodeDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id = "tech_node";
        public string Title = "Tech Node";
        [TextArea] public string Description = string.Empty;

        [Tooltip("Simdilik null birakilabilir; UI bas-harf placeholder gosterir, art uretilmez.")]
        public Sprite Icon;

        [Header("Purchase")]
        public ResourceCost Cost = new ResourceCost(40, 0, 0, 0);
        [Min(1)] public int MaxLevel = 1;

        [Header("Graph")]
        [Tooltip("Satin alinabilmek icin sahip olunmasi (>=1 seviye) gereken node Id'leri.")]
        public string[] PrerequisiteNodeIds = new string[0];

        [Tooltip("Bu node ILK kez satin alindiginda gorunur (revealed) olan cocuk node Id'leri. Baglanti cizgileri de bu iliskiden cizilir.")]
        public string[] RevealChildNodeIds = new string[0];

        [Header("Effects")]
        [Tooltip("Her satin alinan seviyede bir kez uygulanir.")]
        public TechNodeEffect[] Effects = new TechNodeEffect[0];
    }
}
