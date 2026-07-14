using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Mobile recruitment panelinde gosterilen tek bir okcu tipinin sabit datasini tutar.
    /// Runtime sayilar, unlock state ve kaynak durumu bu asset'te tutulmaz.
    /// </summary>
    [CreateAssetMenu(fileName = "ArcherDefinition", menuName = "DeadWalls/Mobile Castle/Archer Definition")]
    public class ArcherDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id = "basic_archer";
        public string DisplayName = "Basic Archer";
        public ArcherType Type = ArcherType.Basic;
        [TextArea] public string Description = "Balanced single-target defender.";
        public int SortOrder;

        [Header("Recruitment")]
        public ResourceCost BuyCost = new ResourceCost(45, 0, 0, 20);
        public ResourceCost RetrainCost = ResourceCost.Zero;
        [Min(0)] public int PopulationCost = 1;
        public string RequiredTechId;

        [Header("Incremental Cost")]
        [Min(1)] public int CostGrowthInterval = ArcherRecruitmentCostUtility.DefaultGrowthInterval;
        [Min(0.01f)] public float CostGrowthExponent = ArcherRecruitmentCostUtility.DefaultGrowthExponent;

        [Header("Combat")]
        [Min(0f)] public float Damage = 10f;
        [Min(0f)] public float FireRate = 1.5f;
        [Min(0f)] public float Range = 15f;
        [Min(0f)] public float SlowDuration;
        [Range(0.01f, 1f)] public float SlowMultiplier = 1f;

        [Header("Visual")]
        public Color Tint = Color.white;

        internal ArcherStats ToArcherStats()
        {
            return new ArcherStats
            {
                FireRate = FireRate,
                Damage = Damage,
                Range = Range,
                SlowDuration = SlowDuration,
                SlowMultiplier = SlowMultiplier
            };
        }

        public void ApplyDefaultValues(ArcherType type)
        {
            Type = type;
            PopulationCost = 1;

            switch (type)
            {
                case ArcherType.Rapid:
                    Id = "rapid_archer";
                    DisplayName = "Rapid Archer";
                    Description = "Fast shots, lower damage.";
                    SortOrder = 20;
                    BuyCost = new ResourceCost(55, 0, 35, 20);
                    RetrainCost = new ResourceCost(55, 0, 35, 0);
                    RequiredTechId = "rapid_volley";
                    Damage = 6f;
                    FireRate = 3f;
                    Range = 14f;
                    SlowDuration = 0f;
                    SlowMultiplier = 1f;
                    Tint = new Color(1f, 0.82f, 0.32f, 1f);
                    break;

                case ArcherType.Frost:
                    Id = "frost_archer";
                    DisplayName = "Frost Archer";
                    Description = "Slows one target.";
                    SortOrder = 30;
                    BuyCost = new ResourceCost(45, 55, 25, 0);
                    RetrainCost = new ResourceCost(45, 55, 25, 0);
                    RequiredTechId = "frost_arrows";
                    Damage = 5f;
                    FireRate = 1.2f;
                    Range = 14f;
                    SlowDuration = 2f;
                    SlowMultiplier = 0.55f;
                    Tint = new Color(0.45f, 0.78f, 1f, 1f);
                    break;

                default:
                    Id = "basic_archer";
                    DisplayName = "Basic Archer";
                    Description = "Balanced single-target defender.";
                    SortOrder = 10;
                    BuyCost = new ResourceCost(45, 0, 0, 20);
                    RetrainCost = ResourceCost.Zero;
                    RequiredTechId = string.Empty;
                    Damage = 10f;
                    FireRate = 1.5f;
                    Range = 15f;
                    SlowDuration = 0f;
                    SlowMultiplier = 1f;
                    Tint = Color.white;
                    break;
            }

            CostGrowthInterval = ArcherRecruitmentCostUtility.DefaultGrowthInterval;
            CostGrowthExponent = ArcherRecruitmentCostUtility.DefaultGrowthExponent;
        }

        internal static ArcherDefinitionSO CreateRuntimeDefault(ArcherType type)
        {
            var definition = CreateInstance<ArcherDefinitionSO>();
            definition.hideFlags = HideFlags.DontSave;
            definition.ApplyDefaultValues(type);
            return definition;
        }
    }
}
