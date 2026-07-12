using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    [CreateAssetMenu(fileName = "EnemyCatalog", menuName = "DeadWalls/Mobile Castle/Enemy Catalog")]
    public class EnemyCatalogSO : ScriptableObject
    {
        public string ActiveEnemyId = "zombie_basic";
        public EnemyDefinitionSO[] Definitions = new EnemyDefinitionSO[0];

        public EnemyDefinitionSO GetActiveDefinition()
        {
            if (Definitions == null)
                return null;

            foreach (var definition in Definitions)
            {
                if (definition != null && definition.Id == ActiveEnemyId)
                    return definition;
            }

            return null;
        }

        public List<string> ValidateV1Catalog()
        {
            var problems = new List<string>();
            if (Definitions == null || Definitions.Length != 1)
                problems.Add("V1 active enemy catalog tam olarak bir definition icermeli.");

            var ids = new HashSet<string>();
            if (Definitions != null)
            {
                foreach (var definition in Definitions)
                {
                    if (definition == null)
                    {
                        problems.Add("Enemy catalog null definition iceriyor.");
                        continue;
                    }

                    problems.AddRange(definition.ValidateDefinition());
                    if (!string.IsNullOrWhiteSpace(definition.Id) && !ids.Add(definition.Id))
                        problems.Add($"Duplicate enemy Id: '{definition.Id}'.");
                }
            }

            if (string.IsNullOrWhiteSpace(ActiveEnemyId))
                problems.Add("ActiveEnemyId bos.");
            else if (GetActiveDefinition() == null)
                problems.Add($"Active enemy '{ActiveEnemyId}' catalogda bulunamadi.");

            return problems;
        }
    }
}
