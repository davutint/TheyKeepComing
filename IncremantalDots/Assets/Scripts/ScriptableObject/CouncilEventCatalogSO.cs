using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Council event uretim havuzu: atomlar + sablonlar. Somut event'ler asset olarak
    /// TUTULMAZ — CouncilComposer bunlari runtime'da birlestirir. Setup tool merge-only
    /// seed eder (kullanicinin ekledigi atom/sablonlar korunur).
    /// </summary>
    [CreateAssetMenu(fileName = "CouncilEventCatalog", menuName = "DeadWalls/Mobile Castle/Council Event Catalog")]
    public class CouncilEventCatalogSO : ScriptableObject
    {
        public CouncilTemplateSO[] Templates = new CouncilTemplateSO[0];
        public CouncilEffectAtomSO[] Atoms = new CouncilEffectAtomSO[0];

        [Header("Pacing")]
        [Range(0f, 1f)] public float DailyEventChance = 0.30f;
        [Tooltip("Bu kadar gun event cikmadiysa garanti cikar (pity).")]
        public int PityDays = 3;
        [Tooltip("Bir event'ten sonra en az bu kadar gun bosluk.")]
        public int CooldownDays = 1;
        [Tooltip("Son N sablon anti-tekrar sogumasina girer.")]
        public int RecentTemplateMemory = 3;

        public CouncilEffectAtomSO GetAtom(string id)
        {
            if (string.IsNullOrEmpty(id) || Atoms == null)
                return null;

            for (int i = 0; i < Atoms.Length; i++)
            {
                if (Atoms[i] != null && Atoms[i].Id == id)
                    return Atoms[i];
            }

            return null;
        }

        /// <summary>Tutarlilik kontrolu: bos/duplicate Id'ler, sablonlarin bilinmeyen atom referanslari.</summary>
        public List<string> ValidateCatalog()
        {
            var problems = new List<string>();
            var atomIds = new HashSet<string>();
            var templateIds = new HashSet<string>();

            if (Atoms != null)
            {
                foreach (var atom in Atoms)
                {
                    if (atom == null) { problems.Add("Atoms listesinde null giris."); continue; }
                    if (string.IsNullOrEmpty(atom.Id)) problems.Add($"'{atom.name}' atom Id bos.");
                    else if (!atomIds.Add(atom.Id)) problems.Add($"Duplicate atom Id: '{atom.Id}'.");
                }
            }

            if (Templates != null)
            {
                foreach (var template in Templates)
                {
                    if (template == null) { problems.Add("Templates listesinde null giris."); continue; }
                    if (string.IsNullOrEmpty(template.Id)) problems.Add($"'{template.name}' template Id bos.");
                    else if (!templateIds.Add(template.Id)) problems.Add($"Duplicate template Id: '{template.Id}'.");

                    CheckAtomRefs(template.OptionAAtomIds, template, atomIds, problems);
                    CheckAtomRefs(template.OptionBAtomIds, template, atomIds, problems);
                }
            }

            if (atomIds.Count == 0) problems.Add("Katalogda hic atom yok.");
            if (templateIds.Count == 0) problems.Add("Katalogda hic sablon yok.");
            return problems;
        }

        private static void CheckAtomRefs(string[] ids, CouncilTemplateSO template,
            HashSet<string> atomIds, List<string> problems)
        {
            if (ids == null)
                return;

            foreach (var id in ids)
            {
                if (!string.IsNullOrEmpty(id) && !atomIds.Contains(id))
                    problems.Add($"'{template.Id}' bilinmeyen atom referansi: '{id}'.");
            }
        }
    }
}
