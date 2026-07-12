using Unity.Mathematics;

namespace DeadWalls
{
    public static class EnemyCatalogRuntimeUtility
    {
        public static int ResolveActiveIndex(EnemyCatalogRuntimeData catalog, int bufferLength)
        {
            if (bufferLength <= 0 || catalog.EntryCount <= 0)
                return -1;

            int available = math.min(bufferLength, catalog.EntryCount);
            return math.clamp(catalog.ActiveEntryIndex, 0, available - 1);
        }

        public static void ApplyBaseStats(ref MobileCastleCombatConfig config, EnemyDefinitionSO definition)
        {
            if (definition == null)
                return;

            config.ZombieBaseHP = math.max(1f, definition.BaseHP);
            config.ZombieBaseDamage = math.max(0f, definition.BaseDamage);
            config.BaseZombieSpeed = math.max(0.05f, definition.BaseMoveSpeed);
            config.ZombieScale = math.max(0.01f, definition.Scale);
        }
    }
}
