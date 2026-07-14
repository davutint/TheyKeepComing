using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    public static class HeartGraphConstants
    {
        public const string RootNodeId = "castle_heart";
        public const string RapidGuaranteeTag = "guarantee:rapid";
        public const string FrostGuaranteeTag = "guarantee:frost";
        public const string FireballGuaranteeTag = "guarantee:fireball";
        public const string WallGuaranteeTag = "guarantee:wall";
        public const string RepeatableSinkTag = "sink:repeatable";
    }

    public static class HeartNodeTagUtility
    {
        public static bool HasTag(HeartNodeDefinitionSO definition, string tag)
        {
            if (definition == null || string.IsNullOrWhiteSpace(tag))
                return false;

            string[] tags = definition.Tags ?? Array.Empty<string>();
            for (int i = 0; i < tags.Length; i++)
            {
                if (string.Equals(tags[i], tag, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Castle Heart generator'inin authored node havuzudur.
    /// Run graph state'i veya runtime satin alma state'i tasimaz.
    /// </summary>
    [CreateAssetMenu(
        fileName = "HeartNodeCatalog",
        menuName = "DeadWalls/Castle Heart/Heart Node Catalog")]
    public sealed class HeartNodeCatalogSO : ScriptableObject
    {
        [Tooltip("System-owned root kimligi. Authored node havuzunda bu Id kullanilamaz.")]
        public string RootNodeId = HeartGraphConstants.RootNodeId;

        public HeartNodeDefinitionSO[] Nodes = Array.Empty<HeartNodeDefinitionSO>();

        public HeartNodeDefinitionSO GetNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                return null;

            HeartNodeDefinitionSO[] nodes = Nodes ?? Array.Empty<HeartNodeDefinitionSO>();
            for (int i = 0; i < nodes.Length; i++)
            {
                HeartNodeDefinitionSO definition = nodes[i];
                if (definition != null
                    && string.Equals(definition.Id, nodeId, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        public void CollectValidationErrors(List<string> errors)
        {
            if (errors == null)
                throw new ArgumentNullException(nameof(errors));

            if (!string.Equals(RootNodeId, HeartGraphConstants.RootNodeId, StringComparison.Ordinal))
            {
                errors.Add($"RootNodeId sabit '{HeartGraphConstants.RootNodeId}' olmalidir.");
            }

            HeartNodeDefinitionSO[] nodes = Nodes ?? Array.Empty<HeartNodeDefinitionSO>();
            var definitionsById = new Dictionary<string, HeartNodeDefinitionSO>(StringComparer.Ordinal);

            for (int i = 0; i < nodes.Length; i++)
            {
                HeartNodeDefinitionSO definition = nodes[i];
                if (definition == null)
                {
                    errors.Add($"Nodes[{i}] bos olamaz.");
                    continue;
                }

                var definitionErrors = new List<string>();
                definition.CollectValidationErrors(definitionErrors);
                for (int errorIndex = 0; errorIndex < definitionErrors.Count; errorIndex++)
                    errors.Add($"Node '{definition.Id}': {definitionErrors[errorIndex]}");

                if (string.Equals(definition.Id, RootNodeId, StringComparison.Ordinal))
                    errors.Add($"Root Id '{RootNodeId}' authored node olarak kullanilamaz.");

                if (!string.IsNullOrWhiteSpace(definition.Id)
                    && !definitionsById.TryAdd(definition.Id, definition))
                {
                    errors.Add($"Tekrarlanan node Id: {definition.Id}");
                }

                ValidateTags(definition, errors);
            }

            foreach (KeyValuePair<string, HeartNodeDefinitionSO> pair in definitionsById)
            {
                HeartNodeDefinitionSO definition = pair.Value;
                if (definition.Type != HeartNodeType.Keystone)
                    continue;

                string partnerId = definition.ConflictNodeIds[0];
                if (!definitionsById.TryGetValue(partnerId, out HeartNodeDefinitionSO partner))
                {
                    errors.Add($"Keystone '{definition.Id}' partner '{partnerId}' catalog'da yok.");
                    continue;
                }

                if (partner.Type != HeartNodeType.Keystone)
                {
                    errors.Add($"Keystone '{definition.Id}' partner '{partnerId}' Keystone degil.");
                    continue;
                }

                string[] partnerConflicts = partner.ConflictNodeIds ?? Array.Empty<string>();
                if (partnerConflicts.Length != 1
                    || !string.Equals(partnerConflicts[0], definition.Id, StringComparison.Ordinal))
                {
                    errors.Add($"Keystone cifti simetrik degil: '{definition.Id}' <-> '{partnerId}'.");
                }
            }
        }

        private static void ValidateTags(HeartNodeDefinitionSO definition, List<string> errors)
        {
            string[] tags = definition.Tags ?? Array.Empty<string>();
            var seenTags = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < tags.Length; i++)
            {
                string tag = tags[i];
                if (string.IsNullOrWhiteSpace(tag))
                {
                    errors.Add($"Node '{definition.Id}' bos tag tasiyamaz.");
                    continue;
                }

                if (!seenTags.Add(tag))
                    errors.Add($"Node '{definition.Id}' tekrarlanan tag tasiyor: {tag}");
            }
        }
    }
}
