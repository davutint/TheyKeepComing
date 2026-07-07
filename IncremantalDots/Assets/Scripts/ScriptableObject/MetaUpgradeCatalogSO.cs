using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>Meta magaza katalogu. Setup tool merge-only seed eder (mevcut asset'lere dokunulmaz).</summary>
    [CreateAssetMenu(fileName = "MetaUpgradeCatalog", menuName = "DeadWalls/Mobile Castle/Meta Upgrade Catalog")]
    public class MetaUpgradeCatalogSO : ScriptableObject
    {
        public MetaUpgradeSO[] Upgrades = new MetaUpgradeSO[0];

        public MetaUpgradeSO GetUpgrade(string id)
        {
            if (string.IsNullOrEmpty(id) || Upgrades == null)
                return null;

            foreach (var upgrade in Upgrades)
            {
                if (upgrade != null && upgrade.Id == id)
                    return upgrade;
            }

            return null;
        }

        public List<string> ValidateCatalog()
        {
            var problems = new List<string>();
            var ids = new HashSet<string>();
            if (Upgrades != null)
            {
                foreach (var upgrade in Upgrades)
                {
                    if (upgrade == null) { problems.Add("Upgrades listesinde null giris."); continue; }
                    if (string.IsNullOrEmpty(upgrade.Id)) problems.Add($"'{upgrade.name}' Id bos.");
                    else if (!ids.Add(upgrade.Id)) problems.Add($"Duplicate Id: '{upgrade.Id}'.");
                    if (upgrade.EffectType == MetaUpgradeEffectType.StartingTechLevel && string.IsNullOrEmpty(upgrade.TechNodeId))
                        problems.Add($"'{upgrade.Id}' StartingTechLevel ama TechNodeId bos.");
                }
            }

            return problems;
        }
    }
}
