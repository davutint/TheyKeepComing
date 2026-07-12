using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Tech tree'nin tum node tanimlarini toplayan katalog. Kategori/tier YOKTUR;
    /// agacin tek dogruluk kaynagi node'lar arasi RevealChildNodeIds baglantilaridir.
    /// Setup tool eksik default node'lari seed eder ve kullanicinin ekledigi ekstra
    /// node'lari korur. V1'de rezerve dormant id'ler (Moat) aktif catalog'a alinmaz.
    /// </summary>
    [CreateAssetMenu(fileName = "TechTreeCatalog", menuName = "DeadWalls/Mobile Castle/Tech Tree Catalog")]
    public class TechTreeCatalogSO : ScriptableObject
    {
        [Tooltip("Agacin koku; oyun basinda otomatik sahip olunur ve cocuklari gorunur baslar.")]
        public string RootNodeId = "castle_heart";

        public TechNodeDefinitionSO[] Nodes = new TechNodeDefinitionSO[0];

        private Dictionary<string, TechNodeDefinitionSO> _lookup;
        private int _lookupCount = -1;

        public TechNodeDefinitionSO GetNode(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            EnsureLookup();
            return _lookup.TryGetValue(id, out var node) ? node : null;
        }

        public TechNodeDefinitionSO GetRootNode()
        {
            return GetNode(RootNodeId);
        }

        /// <summary>Verilen node'u RevealChildNodeIds listesinde tasiyan ilk node (layout/cizgi parent'i). Root icin null.</summary>
        public TechNodeDefinitionSO FindRevealParent(string id)
        {
            if (string.IsNullOrEmpty(id) || Nodes == null)
                return null;

            for (int i = 0; i < Nodes.Length; i++)
            {
                var candidate = Nodes[i];
                if (candidate == null || candidate.RevealChildNodeIds == null)
                    continue;

                for (int c = 0; c < candidate.RevealChildNodeIds.Length; c++)
                {
                    if (candidate.RevealChildNodeIds[c] == id)
                        return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// Katalog tutarlilik kontrolu: bos/duplicate Id, bilinmeyen reveal/prerequisite hedefi,
        /// eksik root. Bos liste = sorun yok. Setup tool ve testler icin yardimcidir.
        /// </summary>
        public List<string> ValidateCatalog()
        {
            var problems = new List<string>();
            var seen = new HashSet<string>();

            if (Nodes == null || Nodes.Length == 0)
            {
                problems.Add("Catalog bos: hic node yok.");
                return problems;
            }

            foreach (var node in Nodes)
            {
                if (node == null)
                {
                    problems.Add("Nodes listesinde null giris var.");
                    continue;
                }

                if (string.IsNullOrEmpty(node.Id))
                    problems.Add($"'{node.name}' asset'inin Id alani bos.");
                else if (!seen.Add(node.Id))
                    problems.Add($"Duplicate node Id: '{node.Id}'.");
            }

            if (string.IsNullOrEmpty(RootNodeId))
                problems.Add("RootNodeId bos.");
            else if (!seen.Contains(RootNodeId))
                problems.Add($"Root node '{RootNodeId}' katalogda yok.");

            foreach (var node in Nodes)
            {
                if (node == null)
                    continue;

                if (node.RevealChildNodeIds != null)
                {
                    foreach (var childId in node.RevealChildNodeIds)
                    {
                        if (!string.IsNullOrEmpty(childId) && !seen.Contains(childId))
                            problems.Add($"'{node.Id}' bilinmeyen reveal hedefi iceriyor: '{childId}'.");
                    }
                }

                if (node.PrerequisiteNodeIds != null)
                {
                    foreach (var prereqId in node.PrerequisiteNodeIds)
                    {
                        if (!string.IsNullOrEmpty(prereqId) && !seen.Contains(prereqId))
                            problems.Add($"'{node.Id}' bilinmeyen prerequisite iceriyor: '{prereqId}'.");
                    }
                }
            }

            return problems;
        }

        private void EnsureLookup()
        {
            int count = Nodes != null ? Nodes.Length : 0;
            if (_lookup != null && _lookupCount == count)
                return;

            _lookup = new Dictionary<string, TechNodeDefinitionSO>(count);
            for (int i = 0; i < count; i++)
            {
                var node = Nodes[i];
                if (node == null || string.IsNullOrEmpty(node.Id))
                    continue;

                // Duplicate Id durumunda ilk kayit kazanir; ValidateCatalog raporlar.
                if (!_lookup.ContainsKey(node.Id))
                    _lookup.Add(node.Id, node);
            }

            _lookupCount = count;
        }

        private void OnValidate()
        {
            // Editor'da Nodes duzenlenince lookup cache'ini tazele.
            _lookup = null;
            _lookupCount = -1;
        }
    }
}
