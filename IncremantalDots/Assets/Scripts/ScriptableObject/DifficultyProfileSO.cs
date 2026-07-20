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
    /// RunDifficultyProfile V1 contract'inda bu asset BaseSpawn egrisi, faz carpanlari ve
    /// active cap'in icerik owner'idir. Cap doluyken talebi kaybetmeyen backlog politikasi
    /// designer secenegi degildir; ContinuousSpawnBudgetUtility tarafindan sabit uygulanir.
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultyProfile", menuName = "DeadWalls/Mobile Castle/Difficulty Profile")]
    public class DifficultyProfileSO : ScriptableObject
    {
        [Header("Gun Egrileri (x = gun, y = carpan; 1 = etkisiz)")]
        [Tooltip("Gece (ve dusk-sonu) intensity'sine gun carpani. Erken oyun rampi burada: orn. (1, 0.6) (2, 0.8) (3, 1).")]
        public AnimationCurve NightIntensityByDay = AnimationCurve.Constant(1f, 60f, 1f);
        [Tooltip("V1 quantity-only runtime'da dormant legacy alan; zombi HP'sini degistirmez.")]
        public AnimationCurve ZombieHpMultByDay = AnimationCurve.Constant(1f, 60f, 1f);
        [Tooltip("RunDifficultyProfile BaseSpawn gun egrisi: spawn batch'ine intensity ve cycle buyumesinden sonra uygulanan carpan.")]
        public AnimationCurve SpawnBatchMultByDay = AnimationCurve.Constant(1f, 60f, 1f);
        [Tooltip("Egrilerin ornekledigi gun sayisi; sonrasinda son gunun degeri sabit kullanilir.")]
        [Range(10, 200)] public int SampleDays = 60;

        [Header("Kutle Eskalasyonu (config'e yazilir)")]
        public float ZombieBaseHP = 20f;
        public float ZombieHpGrowthPerCycle = 0f;
        public float ZombieBaseDamage = 5f;
        public float ZombieDamagePerCycle = 0f;
        public int SpawnBatchSize = 2;
        public float SpawnBatchGrowthPerCycle = 0.10f;
        [Tooltip("Bir frame'de backlog'dan sahaya aktarilabilecek ve tek intervalde talep edilebilecek quantity tavani.")]
        public int MaxSpawnBatch = 16;
        [Tooltip("Normal run active zombie tavani. Cap doluyken yeni talep silinmez; PendingEnemies backlog'unda korunur.")]
        public int MaxAliveZombies = 900;
        public float BaseSpawnInterval = 0.95f;
        public float MinSpawnInterval = 0.35f;

        [Header("Faz Intensity Carpanlari (config'e yazilir)")]
        public float DayIntensity = 0.55f;
        public float DuskStartIntensity = 1.0f;
        public float DuskEndIntensity = 1.35f;
        public float NightIntensity = 1.65f;
        public float DawnIntensity = 0.15f;

        [Header("Castle Heart Grave Essence Drops")]
        [Tooltip("Her gercek dusman olumunun 1 Grave Essence drop etme ihtimali. 0.10 = %10.")]
        [Range(0f, 1f)] public float GraveEssenceDropChance = 0.10f;
        [Tooltip("Basarili bir drop'un GrantGraveEssence kapisina verdigi taban miktar.")]
        [Min(1)] public int GraveEssencePerDrop = 1;

        [Header("Wall Defense (config'e yazilir)")]
        [Tooltip("Tech, meta ve Heart yuzde carpanlarindan onceki Wall MaxHP baseline degeri.")]
        [Min(1f)] public float WallBaseHp = 350f;
        [HideInInspector] public int RepairBaseWoodCost = 120;
        [HideInInspector] public int RepairBaseStoneCost = 50;
        [Tooltip("Normal repair tek kullanimda Wall MaxHP'nin bu orani kadar iyilestirir.")]
        [Range(0.01f, 1f)] public float NormalRepairHealPercent = 0.25f;
        [Tooltip("Normal repair'de gercek iyilestirilen her HP icin Stone fiyati.")]
        [Min(0.001f)] public float RepairStonePerMissingHp = 0.10f;
        [Tooltip("Day ve Dusk normal repair fiyatina uygulanan global carpan.")]
        [Min(0.01f)] public float RepairDayPriceMultiplier = 1f;

        [Header("Active Abilities (config'e yazilir)")]
        [Min(0.1f)] public float RallyCooldown = 60f;
        [Range(0.01f, 1f)] public float EmergencyRepairHealPercent = 0.20f;
        [Min(0.1f)] public float EmergencyRepairCooldown = 120f;

        [Header("Population Runtime Contract (config/tuning'e yazilir)")]
        [Tooltip("Her tamamlanan Dawn/cycle icin yatak ve Food butcesinden once istenen survivor sayisi.")]
        [Min(0)] public int PopulationGrowthPerDayPrep =
            MobilePopulationArrivalUtility.DefaultRequestedArrivalsPerDawn;
        [Tooltip("Kabul edilen her yeni survivor icin ayni Dawn transaction'inda bir kez harcanan Food.")]
        [Min(1)] public int FoodCostPerArrival =
            MobilePopulationArrivalUtility.DefaultFoodCostPerArrival;

        [Header("Worker Economy (config/tuning'e yazilir)")]
        [Tooltip("Bir Wood worker'in tech, meta, Heart ve bina bonuslarindan onceki dakikalik uretimi.")]
        [Min(0f)] public float WoodWorkerProductionPerMin =
            MobileEconomyPriceTuningUtility.DefaultWoodWorkerProductionPerMin;
        [Tooltip("Bir Stone worker'in tech, meta, Heart ve bina bonuslarindan onceki dakikalik uretimi.")]
        [Min(0f)] public float StoneWorkerProductionPerMin =
            MobileEconomyPriceTuningUtility.DefaultStoneWorkerProductionPerMin;
        [Tooltip("Bir Iron worker'in tech, meta, Heart ve bina bonuslarindan onceki dakikalik uretimi.")]
        [Min(0f)] public float IronWorkerProductionPerMin =
            MobileEconomyPriceTuningUtility.DefaultIronWorkerProductionPerMin;
        [Tooltip("Bir Food worker'in tech, meta, Heart ve bina bonuslarindan onceki dakikalik uretimi.")]
        [Min(0f)] public float FoodWorkerProductionPerMin =
            MobileEconomyPriceTuningUtility.DefaultFoodWorkerProductionPerMin;
        [Tooltip("Her Efficiency bina seviyesinin baz kisi uretimine additive ekledigi oran. 0.10 = +%10.")]
        [Min(0.001f)] public float WorkerEfficiencyPercentPerLevel =
            MobileEconomyPriceTuningUtility.DefaultWorkerEfficiencyPercentPerLevel;

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

        [Header("Archer Runtime Contract - Finite Arrow (runtime tuning'e bake edilir)")]
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
