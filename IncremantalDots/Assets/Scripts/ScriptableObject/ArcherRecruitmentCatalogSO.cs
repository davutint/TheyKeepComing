using System;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Mobile Archer Recruitment drawer'inda listelenecek okcu definition asset'lerini siralar.
    /// </summary>
    [CreateAssetMenu(fileName = "ArcherRecruitmentCatalog", menuName = "DeadWalls/Mobile Castle/Archer Recruitment Catalog")]
    public class ArcherRecruitmentCatalogSO : ScriptableObject
    {
        public ArcherDefinitionSO[] Definitions = Array.Empty<ArcherDefinitionSO>();

        public ArcherDefinitionSO[] GetOrderedDefinitions()
        {
            if (Definitions == null || Definitions.Length == 0)
                return Array.Empty<ArcherDefinitionSO>();

            var ordered = new ArcherDefinitionSO[Definitions.Length];
            Array.Copy(Definitions, ordered, Definitions.Length);
            Array.Sort(ordered, CompareDefinitions);
            return ordered;
        }

        public ArcherDefinitionSO GetDefinition(ArcherType type)
        {
            if (Definitions == null)
                return null;

            for (int i = 0; i < Definitions.Length; i++)
            {
                var definition = Definitions[i];
                if (definition != null && definition.Type == type)
                    return definition;
            }

            return null;
        }

        private static int CompareDefinitions(ArcherDefinitionSO a, ArcherDefinitionSO b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            int order = a.SortOrder.CompareTo(b.SortOrder);
            return order != 0
                ? order
                : string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
        }
    }
}
