using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// M-C hazirlik iskeleti: gun bazli dusman karisim tablosu satiri. V1'de SISTEM OKUMAZ
    /// (tek zombi tipi var); zombi cesitliligi geldiginde WaveSpawnSystem bu tabloya baglanir.
    /// </summary>
    [System.Serializable]
    public struct SpawnTableEntry
    {
        [Tooltip("Dusman kimligi (ileride ZombieDefinitionSO id'si).")]
        public string EnemyId;
        [Tooltip("Bu dusmanin havuza girdigi ilk gun.")]
        public int FirstDay;
        [Tooltip("Gun -> secim agirligi (x=gun, y=agirlik).")]
        public AnimationCurve WeightByDay;
    }

    /// <summary>
    /// Gelecekteki ozel gece takvimi satiri. V1 runtime bu veriyi okumaz.
    /// </summary>
    [System.Serializable]
    public struct SpecialNightEntry
    {
        [Tooltip("Her N gunde bir tetiklenir (orn. 5 = 5, 10, 15...).")]
        public int EveryNDays;
        [Tooltip("Tur kimligi (orn. blood_moon, boss).")]
        public string Kind;
        [Tooltip("O gecenin intensity'sine eklenen bonus carpan (0.5 = +%50).")]
        public float IntensityBonus;
    }

    /// <summary>
    /// Zorlugun TEK dogruluk kaynagi. Baker bunu MobileCastleCombatConfig alanlarina yazar ve
    /// gun-egrilerini DifficultyDaySample buffer'ina ornekler; Difficulty Tuner penceresi play
    /// sirasinda canli yeniden uygulayabilir. Egriler x=GUN (1..SampleDays), y=CARPAN'dir ve
    /// temel formullere EK uygulanir (1 = etkisiz):
    ///   gece intensity = faz-intensity * NightIntensityByDay(gun)
    ///   zombi HP       = BaseHP (V1 quantity-only; HP curve/growth dormant)
    ///   spawn batch    = (taban * intensity * cycle buyumesi) * SpawnBatchMultByDay(gun)
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultyProfile", menuName = "DeadWalls/Mobile Castle/Difficulty Profile")]
    public class DifficultyProfileSO : ScriptableObject
    {
        [Header("Gun Egrileri (x = gun, y = carpan; 1 = etkisiz)")]
        [Tooltip("Gece (ve dusk-sonu) intensity'sine gun carpani. Erken oyun rampi burada: orn. (1, 0.6) (2, 0.8) (3, 1).")]
        public AnimationCurve NightIntensityByDay = AnimationCurve.Constant(1f, 60f, 1f);
        [Tooltip("Zombi HP'sine gun carpani (lineer buyumeye EK).")]
        public AnimationCurve ZombieHpMultByDay = AnimationCurve.Constant(1f, 60f, 1f);
        [Tooltip("Spawn batch'ine gun carpani (intensity ve cycle buyumesine EK).")]
        public AnimationCurve SpawnBatchMultByDay = AnimationCurve.Constant(1f, 60f, 1f);
        [Tooltip("Egrilerin ornekledigi gun sayisi; sonrasinda son gunun degeri sabit kullanilir.")]
        [Range(10, 200)] public int SampleDays = 60;

        [Header("Kutle Eskalasyonu (config'e yazilir)")]
        public float ZombieBaseHP = 20f;
        public float ZombieHpGrowthPerCycle = 0f;
        public float ZombieBaseDamage = 5f;
        public float ZombieDamagePerCycle = 0f;
        public int SpawnBatchSize = 2;
        public float SpawnBatchGrowthPerCycle = 0.15f;
        public int MaxSpawnBatch = 16;
        public int MaxAliveZombies = 900;
        public float BaseSpawnInterval = 0.95f;
        public float MinSpawnInterval = 0.35f;

        [Header("Faz Intensity Carpanlari (config'e yazilir)")]
        public float DayIntensity = 0.55f;
        public float DuskStartIntensity = 1.0f;
        public float DuskEndIntensity = 1.35f;
        public float NightIntensity = 1.65f;
        public float DawnIntensity = 0.15f;

        [Header("Savunma Maliyeti (config'e yazilir)")]
        public int RepairBaseWoodCost = 120;
        public int RepairBaseStoneCost = 50;

        [Header("Ekonomi Fiyat Egrileri (runtime tuning'e bake edilir)")]
        [Min(1)] public int BedBaseWoodCost =
            MobileEconomyPriceTuningUtility.DefaultBedBaseWoodCost;
        [Min(1)] public int BedCostGrowthCapacityInterval =
            MobileEconomyPriceTuningUtility.DefaultBedCostGrowthCapacityInterval;
        [Min(1)] public int WorkerCapacityBaseWoodCost =
            MobileEconomyPriceTuningUtility.DefaultWorkerCapacityBaseWoodCost;
        [Min(1)] public int WorkerCapacityBaseIronCost =
            MobileEconomyPriceTuningUtility.DefaultWorkerCapacityBaseIronCost;
        [Min(1)] public int WorkerEfficiencyBaseWoodCost =
            MobileEconomyPriceTuningUtility.DefaultWorkerEfficiencyBaseWoodCost;
        [Min(1)] public int WorkerEfficiencyBaseIronCost =
            MobileEconomyPriceTuningUtility.DefaultWorkerEfficiencyBaseIronCost;
        [Min(1f)] public double WorkerBuildingCostGrowthMultiplier =
            MobileEconomyPriceTuningUtility.DefaultWorkerBuildingCostGrowthMultiplier;

        [Header("Arrow Economy (runtime tuning'e bake edilir)")]
        [Min(1)] public int ArrowBaseCapacity =
            MobileEconomyPriceTuningUtility.DefaultArrowBaseCapacity;
        [Min(1)] public int ArrowCapacityPerLevel =
            MobileEconomyPriceTuningUtility.DefaultArrowCapacityPerLevel;
        [Min(1)] public int ArrowRefillPackageSize =
            MobileEconomyPriceTuningUtility.DefaultArrowRefillPackageSize;
        [Min(1)] public int ArrowBaseArrowsPerWood =
            MobileEconomyPriceTuningUtility.DefaultArrowBaseArrowsPerWood;
        [Min(1)] public int ArrowArrowsPerWoodPerEfficiencyLevel =
            MobileEconomyPriceTuningUtility.DefaultArrowArrowsPerWoodPerEfficiencyLevel;
        [Min(1)] public int ArrowCapacityBaseWoodCost =
            MobileEconomyPriceTuningUtility.DefaultArrowCapacityBaseWoodCost;
        [Min(1)] public int ArrowCapacityBaseIronCost =
            MobileEconomyPriceTuningUtility.DefaultArrowCapacityBaseIronCost;
        [Min(1)] public int ArrowEfficiencyBaseWoodCost =
            MobileEconomyPriceTuningUtility.DefaultArrowEfficiencyBaseWoodCost;
        [Min(1)] public int ArrowEfficiencyBaseIronCost =
            MobileEconomyPriceTuningUtility.DefaultArrowEfficiencyBaseIronCost;
        [Min(1f)] public double ArrowUpgradeCostGrowthMultiplier =
            MobileEconomyPriceTuningUtility.DefaultArrowUpgradeCostGrowthMultiplier;

        [Header("Spawn Tablosu (M-C hazirlik — sistem HENUZ okumuyor)")]
        public SpawnTableEntry[] SpawnTable = new SpawnTableEntry[0];

        [Header("Ozel Geceler (V1 dormant — runtime okumaz)")]
        public SpecialNightEntry[] SpecialNights = new SpecialNightEntry[0];

        /// <summary>Gun egrisini guvenli degerlendirir (gun 1-tabanli; kirpma + negatif korumasi).</summary>
        public float EvaluateCurve(AnimationCurve curve, int day)
        {
            if (curve == null || curve.length == 0)
                return 1f;

            float clampedDay = Mathf.Clamp(day, 1, Mathf.Max(1, SampleDays));
            return Mathf.Max(0.01f, curve.Evaluate(clampedDay));
        }
    }
}
