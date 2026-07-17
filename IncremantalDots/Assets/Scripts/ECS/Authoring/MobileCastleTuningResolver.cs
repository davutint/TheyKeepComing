using Unity.Mathematics;

namespace DeadWalls
{
    /// <summary>
    /// Baseline tuning precedence owner'i. DifficultyProfile yalniz difficulty alanlarini,
    /// MobileCastleCombatAuthoring ise geometri/mode/cycle sureleri/ekonomi alanlarini sahiplenir.
    /// Runtime tech, meta ve Council carpanlari bu baseline'in ustune ayri aggregate katmani kurar.
    /// </summary>
    public static class MobileCastleTuningResolver
    {
        public static void ApplyDifficultyProfile(ref MobileCastleCombatConfig config, DifficultyProfileSO profile)
        {
            if (profile == null)
                return;

            config.SpawnBatchSize = math.max(1, profile.SpawnBatchSize);
            config.ZombieBaseHP = math.max(1f, profile.ZombieBaseHP);
            config.ZombieHpGrowthPerCycle = math.max(0f, profile.ZombieHpGrowthPerCycle);
            config.ZombieBaseDamage = math.max(0.1f, profile.ZombieBaseDamage);
            config.ZombieDamagePerCycle = math.max(0f, profile.ZombieDamagePerCycle);
            config.SpawnBatchGrowthPerCycle = math.max(0f, profile.SpawnBatchGrowthPerCycle);
            config.MaxSpawnBatch = math.max(0, profile.MaxSpawnBatch);
            config.MaxAliveZombies = math.max(0, profile.MaxAliveZombies);
            config.BaseSpawnInterval = math.max(0.01f, profile.BaseSpawnInterval);
            config.MinSpawnInterval = math.max(0.01f, profile.MinSpawnInterval);
            config.SiegeDayIntensityMultiplier = math.max(0.01f, profile.DayIntensity);
            config.SiegeDuskStartIntensityMultiplier = math.max(0.01f, profile.DuskStartIntensity);
            config.SiegeDuskEndIntensityMultiplier = math.max(0.01f, profile.DuskEndIntensity);
            config.SiegeNightIntensityMultiplier = math.max(0.01f, profile.NightIntensity);
            config.SiegeDawnIntensityMultiplier = math.max(0.01f, profile.DawnIntensity);
            config.WallBaseHp = math.max(1f, profile.WallBaseHp);
            config.RepairBaseWoodCost = math.max(0, profile.RepairBaseWoodCost);
            config.RepairBaseStoneCost = math.max(0, profile.RepairBaseStoneCost);
            config.NormalRepairHealPercent = math.clamp(profile.NormalRepairHealPercent, 0.01f, 1f);
            config.RepairStonePerMissingHp = math.max(0.001f, profile.RepairStonePerMissingHp);
            config.RepairDayPriceMultiplier = math.max(0.01f, profile.RepairDayPriceMultiplier);
            config.RallyCooldown = math.max(0.1f, profile.RallyCooldown);
            config.EmergencyRepairHealPercent = math.clamp(profile.EmergencyRepairHealPercent, 0.01f, 1f);
            config.EmergencyRepairCooldown = math.max(0.1f, profile.EmergencyRepairCooldown);
        }

        public static DifficultyDaySample ResolveDaySample(DifficultyProfileSO profile, int day)
        {
            if (profile == null)
            {
                return new DifficultyDaySample
                {
                    NightIntensityMult = 1f,
                    ZombieHpMult = 1f,
                    SpawnBatchMult = 1f,
                    BloodMoonIntensityMult = 1f
                };
            }

            return new DifficultyDaySample
            {
                NightIntensityMult = profile.EvaluateCurve(profile.NightIntensityByDay, day),
                ZombieHpMult = profile.EvaluateCurve(profile.ZombieHpMultByDay, day),
                SpawnBatchMult = profile.EvaluateCurve(profile.SpawnBatchMultByDay, day),
                // V1'de special night yoktur. Schema/content gelecekte kullanilmak uzere dormant.
                BloodMoonIntensityMult = 1f
            };
        }

        public static MobileEconomyPriceTuning ResolveEconomyPriceTuning(DifficultyProfileSO profile)
        {
            if (profile == null)
                return MobileEconomyPriceTuningUtility.Default;

            var tuning = new MobileEconomyPriceTuning
            {
                BedBaseWoodCost = profile.BedBaseWoodCost,
                BedCostGrowthCapacityInterval = profile.BedCostGrowthCapacityInterval,
                WorkerCapacityBaseWoodCost = profile.WorkerCapacityBaseWoodCost,
                WorkerCapacityBaseIronCost = profile.WorkerCapacityBaseIronCost,
                WorkerEfficiencyBaseWoodCost = profile.WorkerEfficiencyBaseWoodCost,
                WorkerEfficiencyBaseIronCost = profile.WorkerEfficiencyBaseIronCost,
                WorkerBuildingCostGrowthMultiplier = profile.WorkerBuildingCostGrowthMultiplier,
                ArrowBaseCapacity = profile.ArrowBaseCapacity,
                ArrowCapacityPerLevel = profile.ArrowCapacityPerLevel,
                ArrowRefillPackageSize = profile.ArrowRefillPackageSize,
                ArrowBaseArrowsPerWood = profile.ArrowBaseArrowsPerWood,
                ArrowArrowsPerWoodPerEfficiencyLevel = profile.ArrowArrowsPerWoodPerEfficiencyLevel,
                ArrowCapacityBaseWoodCost = profile.ArrowCapacityBaseWoodCost,
                ArrowCapacityBaseIronCost = profile.ArrowCapacityBaseIronCost,
                ArrowEfficiencyBaseWoodCost = profile.ArrowEfficiencyBaseWoodCost,
                ArrowEfficiencyBaseIronCost = profile.ArrowEfficiencyBaseIronCost,
                ArrowUpgradeCostGrowthMultiplier = profile.ArrowUpgradeCostGrowthMultiplier
            };
            return MobileEconomyPriceTuningUtility.Sanitize(tuning);
        }
    }
}
