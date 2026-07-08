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

        /// <summary>Tamir maliyetini yuzdesel dusurur (Value 0.20 = -%20). Coklu seviye carpimsal birikir.</summary>
        ReduceRepairCostPercent = 8,

        /// <summary>Hendek yavaslatmasini derinlestirir: MoatSlowMultiplier'dan seviye*Value dusulur (0.10 = +%10 ek yavaslatma).</summary>
        DeepenMoatSlowPercent = 9,

        /// <summary>Hendege gecis hasari ekler: MoatDamagePerSecond += seviye*Value (moat_flame evrimi).</summary>
        AddMoatDamagePerSecond = 10,

        /// <summary>Buyuculugu acar: Ates Topu butonu gorunur olur (arcane_tower dugumu).</summary>
        UnlockSpellcasting = 11,

        /// <summary>Buyu hasarina yuzdesel carpan (Value 0.20 = +%20/seviye, carpimsal birikir).</summary>
        ModifySpellDamagePercent = 12,

        /// <summary>Buyu etki yaricapina duz ekleme (Value 0.4 = +0.4 dunya birimi/seviye).</summary>
        AddSpellRadius = 13,

        /// <summary>Buyu cooldown'unu yuzdesel dusurur (Value 0.10 = -%10/seviye, carpimsal birikir).</summary>
        ReduceSpellCooldownPercent = 14,
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

        [Tooltip("Seviye basina maliyet buyumesi: efektif maliyet = Cost * (1 + level * bu). 0 = sabit maliyet. Tekrarlanabilir sink node'lari icin kullanilir.")]
        [Min(0f)] public float CostGrowthPerLevel;

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
