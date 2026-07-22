using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    [System.Serializable]
    public sealed class CouncilEffectBandSettings
    {
        public const float DefaultSmallMultiplier = 0.7f;
        public const float DefaultFairMultiplier = 1f;
        public const float DefaultGenerousMultiplier = 1.4f;
        public const float DefaultSmallWeight = 0.35f;
        public const float DefaultFairWeight = 0.50f;
        public const float DefaultGenerousWeight = 0.15f;
        public const float DefaultBudgetTolerance = 1.25f;

        [Min(0.01f)] public float SmallMultiplier = DefaultSmallMultiplier;
        [Min(0.01f)] public float FairMultiplier = DefaultFairMultiplier;
        [Min(0.01f)] public float GenerousMultiplier = DefaultGenerousMultiplier;
        [Min(0f)] public float SmallWeight = DefaultSmallWeight;
        [Min(0f)] public float FairWeight = DefaultFairWeight;
        [Min(0f)] public float GenerousWeight = DefaultGenerousWeight;
        [Min(1f)] public float BudgetTolerance = DefaultBudgetTolerance;

        public bool TryValidate(out string problem)
        {
            if (!IsFinitePositive(SmallMultiplier)
                || !IsFinitePositive(FairMultiplier)
                || !IsFinitePositive(GenerousMultiplier))
            {
                problem = "Council effect band multiplier degerleri sonlu ve sifirdan buyuk olmali.";
                return false;
            }

            if (SmallMultiplier > FairMultiplier || FairMultiplier > GenerousMultiplier)
            {
                problem = "Council effect band sirasi Small <= Fair <= Generous olmali.";
                return false;
            }

            if (!IsFiniteNonNegative(SmallWeight)
                || !IsFiniteNonNegative(FairWeight)
                || !IsFiniteNonNegative(GenerousWeight)
                || SmallWeight + FairWeight + GenerousWeight <= 0f)
            {
                problem = "Council effect band agirliklari sonlu, negatif olmayan ve toplamda sifirdan buyuk olmali.";
                return false;
            }

            if (!IsFinitePositive(BudgetTolerance) || BudgetTolerance < 1f)
            {
                problem = "Council budget tolerance sonlu ve en az 1 olmali.";
                return false;
            }

            problem = string.Empty;
            return true;
        }

        public float GetTotalWeight()
        {
            return Mathf.Max(0f, SmallWeight)
                   + Mathf.Max(0f, FairWeight)
                   + Mathf.Max(0f, GenerousWeight);
        }

        private static bool IsFinitePositive(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public enum CouncilChoiceBranch
    {
        OptionA = 0,
        OptionB = 1,
    }

    /// <summary>
    /// Editoryal olarak onaylanmis tek flag zinciri. Runtime yalnız bu source/branch/flag
    /// secimiyle bu target template'in flag constraint'ini acabilir.
    /// </summary>
    [System.Serializable]
    public struct CouncilCuratedChain
    {
        public string SourceTemplateId;
        public CouncilChoiceBranch SourceBranch;
        public string Flag;
        public string TargetTemplateId;
    }

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

        [Header("Effect Bands")]
        [Tooltip("Composer'in Small/Fair/Generous olcekleri, secim agirliklari ve A/B butce toleransi.")]
        public CouncilEffectBandSettings EffectBands = new CouncilEffectBandSettings();

        // v10 ve daha eski authored asset uyumlulugu icin serialized tutulur. V1 Council
        // schedule'i bunlari kullanmaz; exact daily takvimin tek owner'i
        // CouncilRegularSchedule'dir.
        [HideInInspector]
        [Range(0f, 1f)] public float DailyEventChance = 0.30f;
        [HideInInspector]
        public int PityDays = 3;
        [HideInInspector]
        public int CooldownDays = 1;

        [Header("Presentation Memory")]
        [Tooltip("Son N sablon, alternatif uygun template varsa havuzdan tamamen cikarilir.")]
        [Min(1)]
        public int RecentTemplateMemory = 3;

        [Header("Curated Memory Chains")]
        [Tooltip("Yalniz editoryal olarak onaylanmis source secimi -> flag -> target baglantilari.")]
        public CouncilCuratedChain[] CuratedChains = new CouncilCuratedChain[0];

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

        public CouncilTemplateSO GetTemplate(string id)
        {
            if (string.IsNullOrEmpty(id) || Templates == null)
                return null;

            for (int i = 0; i < Templates.Length; i++)
            {
                if (Templates[i] != null && Templates[i].Id == id)
                    return Templates[i];
            }

            return null;
        }

        public bool TryValidateRuntimeContent(out string problem)
        {
            List<string> problems = ValidateCatalog();
            if (problems.Count > 0)
            {
                problem = problems[0];
                return false;
            }

            problem = string.Empty;
            return true;
        }

        public bool IsApprovedChainSource(string sourceTemplateId, bool optionA, string flag)
        {
            if (string.IsNullOrEmpty(sourceTemplateId) || string.IsNullOrEmpty(flag)
                || CuratedChains == null)
                return false;

            CouncilChoiceBranch branch = optionA
                ? CouncilChoiceBranch.OptionA
                : CouncilChoiceBranch.OptionB;
            foreach (CouncilCuratedChain chain in CuratedChains)
            {
                if (chain.SourceTemplateId == sourceTemplateId
                    && chain.SourceBranch == branch
                    && chain.Flag == flag)
                    return true;
            }

            return false;
        }

        public bool IsApprovedChainConstraint(string targetTemplateId, string flag)
        {
            if (string.IsNullOrEmpty(targetTemplateId) || string.IsNullOrEmpty(flag)
                || CuratedChains == null)
                return false;

            foreach (CouncilCuratedChain chain in CuratedChains)
            {
                if (chain.TargetTemplateId == targetTemplateId && chain.Flag == flag)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Zinciri baslatan secim yapildiktan sonra source template'in ayni kosuda
        /// yeniden sunulmasini engelleyen authored ForbiddenFlags constraint'i.
        /// </summary>
        public bool IsApprovedChainSourceRetirement(string sourceTemplateId, string flag)
        {
            if (string.IsNullOrEmpty(sourceTemplateId) || string.IsNullOrEmpty(flag)
                || CuratedChains == null)
                return false;

            foreach (CouncilCuratedChain chain in CuratedChains)
            {
                if (chain.SourceTemplateId == sourceTemplateId && chain.Flag == flag)
                    return true;
            }

            return false;
        }

        /// <summary>Tutarlilik kontrolu: bos/duplicate Id'ler, sablonlarin bilinmeyen atom referanslari.</summary>
        public List<string> ValidateCatalog()
        {
            var problems = new List<string>();
            var atomIds = new HashSet<string>();
            var templateIds = new HashSet<string>();
            var templatesById = new Dictionary<string, CouncilTemplateSO>();

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
                    else templatesById.Add(template.Id, template);

                    CheckAtomRefs(template.OptionAAtomIds, template, atomIds, problems);
                    CheckAtomRefs(template.OptionBAtomIds, template, atomIds, problems);
                }
            }

            CouncilContentPolicy.ValidateCatalog(this, problems);
            ValidateCuratedChains(templatesById, problems);

            if (EffectBands == null)
                problems.Add("Council EffectBands ayarlari bos olamaz.");
            else if (!EffectBands.TryValidate(out string bandProblem))
                problems.Add(bandProblem);

            if (atomIds.Count == 0) problems.Add("Katalogda hic atom yok.");
            if (templateIds.Count == 0) problems.Add("Katalogda hic sablon yok.");
            if (RecentTemplateMemory < 1) problems.Add("RecentTemplateMemory en az 1 olmali.");
            return problems;
        }

        private void ValidateCuratedChains(Dictionary<string, CouncilTemplateSO> templatesById,
            List<string> problems)
        {
            var chainKeys = new HashSet<string>();
            if (CuratedChains != null)
            {
                foreach (CouncilCuratedChain chain in CuratedChains)
                {
                    string key = chain.SourceTemplateId + "|" + (int)chain.SourceBranch + "|"
                                 + chain.Flag + "|" + chain.TargetTemplateId;
                    if (!chainKeys.Add(key))
                        problems.Add($"Duplicate curated Council chain: '{key}'.");

                    if (string.IsNullOrEmpty(chain.SourceTemplateId)
                        || string.IsNullOrEmpty(chain.Flag)
                        || string.IsNullOrEmpty(chain.TargetTemplateId))
                    {
                        problems.Add("Curated Council chain source/flag/target bos olamaz.");
                        continue;
                    }

                    if (!templatesById.TryGetValue(chain.SourceTemplateId, out CouncilTemplateSO source))
                    {
                        problems.Add($"Curated chain bilinmeyen source template: '{chain.SourceTemplateId}'.");
                        continue;
                    }

                    if (!templatesById.TryGetValue(chain.TargetTemplateId, out CouncilTemplateSO target))
                    {
                        problems.Add($"Curated chain bilinmeyen target template: '{chain.TargetTemplateId}'.");
                        continue;
                    }

                    string authoredFlag = chain.SourceBranch == CouncilChoiceBranch.OptionA
                        ? source.SetsFlagOnA
                        : source.SetsFlagOnB;
                    if (authoredFlag != chain.Flag)
                        problems.Add($"Curated chain source '{source.Id}' {chain.SourceBranch} "
                                     + $"'{chain.Flag}' yerine '{authoredFlag}' setliyor.");

                    if (!ContainsFlag(target.RequiredFlags, chain.Flag)
                        && !ContainsFlag(target.ForbiddenFlags, chain.Flag))
                    {
                        problems.Add($"Curated chain target '{target.Id}' flag '{chain.Flag}' "
                                     + "constraint'ini kullanmiyor.");
                    }
                }
            }

            foreach (CouncilTemplateSO template in templatesById.Values)
            {
                ValidateChainConstraints(template, template.RequiredFlags,
                    allowSourceRetirement: false, problems);
                ValidateChainConstraints(template, template.ForbiddenFlags,
                    allowSourceRetirement: true, problems);
                ValidateChainSource(template, true, template.SetsFlagOnA, problems);
                ValidateChainSource(template, false, template.SetsFlagOnB, problems);
            }
        }

        private void ValidateChainConstraints(CouncilTemplateSO template, string[] flags,
            bool allowSourceRetirement, List<string> problems)
        {
            if (flags == null)
                return;

            foreach (string flag in flags)
            {
                bool approved = IsApprovedChainConstraint(template.Id, flag)
                                || (allowSourceRetirement
                                    && IsApprovedChainSourceRetirement(template.Id, flag));
                if (!string.IsNullOrEmpty(flag) && !approved)
                    problems.Add($"'{template.Id}' onaysiz Council chain constraint kullaniyor: '{flag}'.");
            }
        }

        private void ValidateChainSource(CouncilTemplateSO template, bool optionA, string flag,
            List<string> problems)
        {
            if (!string.IsNullOrEmpty(flag) && !IsApprovedChainSource(template.Id, optionA, flag))
            {
                problems.Add($"'{template.Id}' {(optionA ? "OptionA" : "OptionB")} "
                             + $"onaysiz Council chain flag'i setliyor: '{flag}'.");
            }
        }

        private static bool ContainsFlag(string[] flags, string expected)
        {
            if (flags == null)
                return false;

            foreach (string flag in flags)
            {
                if (flag == expected)
                    return true;
            }

            return false;
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
