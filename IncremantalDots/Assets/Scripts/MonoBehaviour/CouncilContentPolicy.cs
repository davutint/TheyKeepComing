using System;
using System.Collections.Generic;

namespace DeadWalls
{
    /// <summary>
    /// Council'in sahip olabilecegi runtime etki alanlarini ve template/atom recetelerini
    /// tek yerde sinirlar. Castle Heart currency/node/upgrade ve Meta progression bu
    /// allowlist'te yoktur; yeni veya bozuk enum degerleri varsayilan olarak reddedilir.
    /// </summary>
    public static class CouncilContentPolicy
    {
        public const string BlockedReason = "UNAPPROVED COUNCIL CONTENT";

        public static bool IsCouncilOwnedEffectKind(CouncilEffectKind kind)
        {
            switch (kind)
            {
                case CouncilEffectKind.GainResource:
                case CouncilEffectKind.PayResource:
                case CouncilEffectKind.TempProductionBoost:
                case CouncilEffectKind.TempProductionPenalty:
                case CouncilEffectKind.WorkerCapBonus:
                case CouncilEffectKind.GainPopulation:
                case CouncilEffectKind.GainFreeArchers:
                case CouncilEffectKind.HealDefensePercent:
                case CouncilEffectKind.NextNightSpawnDelta:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Template'in explicit OptionA/OptionB atom referansinda hangi kind'lar bulunabilir.
        /// Composer'in global ek etkileri bu listeye dahil degildir.
        /// </summary>
        public static bool IsReferencedAtomAllowed(
            CouncilContrastType contrast,
            bool optionA,
            CouncilEffectKind kind)
        {
            switch (contrast)
            {
                case CouncilContrastType.NowVsLater:
                    return optionA
                        ? kind == CouncilEffectKind.GainResource
                        : kind == CouncilEffectKind.TempProductionBoost
                          || kind == CouncilEffectKind.WorkerCapBonus;
                case CouncilContrastType.ResourceTrade:
                    return optionA
                        ? kind == CouncilEffectKind.PayResource
                        : kind == CouncilEffectKind.GainResource;
                case CouncilContrastType.PopulationVsResource:
                    return optionA
                        ? kind == CouncilEffectKind.GainPopulation
                        : kind == CouncilEffectKind.GainResource;
                case CouncilContrastType.EconomyVsDefense:
                    return optionA
                        ? kind == CouncilEffectKind.GainFreeArchers
                        : kind == CouncilEffectKind.HealDefensePercent;
                case CouncilContrastType.SafeVsRisky:
                    return kind == CouncilEffectKind.NextNightSpawnDelta;
                case CouncilContrastType.PayOrSuffer:
                    return optionA
                        ? kind == CouncilEffectKind.TempProductionPenalty
                        : kind == CouncilEffectKind.PayResource;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Runtime'da compose edilmis secenekte bulunabilecek explicit ve composer-owned
        /// global etkileri sinirlar.
        /// </summary>
        public static bool IsComposedEffectAllowed(
            CouncilContrastType contrast,
            bool optionA,
            CouncilEffectKind kind)
        {
            if (!IsCouncilOwnedEffectKind(kind))
                return false;

            switch (contrast)
            {
                case CouncilContrastType.NowVsLater:
                    return IsReferencedAtomAllowed(contrast, optionA, kind);
                case CouncilContrastType.ResourceTrade:
                    return optionA
                        ? kind == CouncilEffectKind.PayResource
                          || kind == CouncilEffectKind.GainResource
                        : kind == CouncilEffectKind.GainResource;
                case CouncilContrastType.PopulationVsResource:
                    return IsReferencedAtomAllowed(contrast, optionA, kind);
                case CouncilContrastType.EconomyVsDefense:
                    return optionA
                        ? kind == CouncilEffectKind.PayResource
                          || kind == CouncilEffectKind.GainFreeArchers
                        : kind == CouncilEffectKind.HealDefensePercent;
                case CouncilContrastType.SafeVsRisky:
                    return optionA
                        ? kind == CouncilEffectKind.NextNightSpawnDelta
                        : kind == CouncilEffectKind.GainResource
                          || kind == CouncilEffectKind.NextNightSpawnDelta;
                case CouncilContrastType.PayOrSuffer:
                    return IsReferencedAtomAllowed(contrast, optionA, kind);
                default:
                    return false;
            }
        }

        public static void ValidateCatalog(CouncilEventCatalogSO catalog, List<string> problems)
        {
            if (catalog == null || problems == null)
                return;

            if (catalog.Atoms != null)
            {
                foreach (CouncilEffectAtomSO atom in catalog.Atoms)
                {
                    if (atom != null && !IsCouncilOwnedEffectKind(atom.Kind))
                    {
                        problems.Add($"'{atom.Id}' atom Council role ownership disinda kind kullaniyor: "
                                     + $"'{atom.Kind}' ({(int)atom.Kind}).");
                    }
                }
            }

            if (catalog.Templates == null)
                return;

            foreach (CouncilTemplateSO template in catalog.Templates)
            {
                if (template == null)
                    continue;

                if (!Enum.IsDefined(typeof(CouncilContrastType), template.Contrast))
                {
                    problems.Add($"'{template.Id}' bilinmeyen Council contrast kullaniyor: "
                                 + $"{(int)template.Contrast}.");
                    continue;
                }

                ValidateTemplateOption(catalog, template, true, problems);
                ValidateTemplateOption(catalog, template, false, problems);
                ValidateGlobalRecipeDependencies(catalog, template, problems);
            }
        }

        public static bool TryValidateOptionRole(
            ComposedCouncilOption option,
            out string problem)
        {
            if (option == null || option.Effects == null || option.Effects.Count == 0)
            {
                problem = "Council option sayisal effect tasimiyor.";
                return false;
            }

            foreach (ComposedCouncilEffect effect in option.Effects)
            {
                if (!IsCouncilOwnedEffectKind(effect.Kind))
                {
                    problem = $"Council-owned olmayan effect kind: '{effect.Kind}' "
                              + $"({(int)effect.Kind}).";
                    return false;
                }
            }

            problem = string.Empty;
            return true;
        }

        public static bool TryValidateComposedEvent(
            CouncilEventCatalogSO catalog,
            ComposedCouncilEvent composed,
            out string problem)
        {
            if (catalog == null)
            {
                problem = "Council catalog eksik.";
                return false;
            }

            if (!catalog.TryValidateRuntimeContent(out problem))
                return false;

            if (composed == null || string.IsNullOrEmpty(composed.TemplateId))
            {
                problem = "Council active event veya TemplateId eksik.";
                return false;
            }

            CouncilTemplateSO template = catalog.GetTemplate(composed.TemplateId);
            if (template == null)
            {
                problem = $"Council active event catalog disi template kullaniyor: "
                          + $"'{composed.TemplateId}'.";
                return false;
            }

            if (composed.SetsFlagOnA != template.SetsFlagOnA
                || composed.SetsFlagOnB != template.SetsFlagOnB)
            {
                problem = $"'{composed.TemplateId}' active event flag payload'i authored template ile uyusmuyor.";
                return false;
            }

            if (!TryValidateComposedOption(template, composed.OptionA, true, out problem)
                || !TryValidateComposedOption(template, composed.OptionB, false, out problem))
            {
                return false;
            }

            problem = string.Empty;
            return true;
        }

        private static void ValidateTemplateOption(
            CouncilEventCatalogSO catalog,
            CouncilTemplateSO template,
            bool optionA,
            List<string> problems)
        {
            string[] ids = optionA ? template.OptionAAtomIds : template.OptionBAtomIds;
            if (ids == null || ids.Length == 0)
            {
                CouncilEffectKind defaultKind = GetDefaultReferencedKind(template.Contrast, optionA);
                if (!HasAtomKind(catalog, defaultKind))
                {
                    problems.Add($"'{template.Id}' {(optionA ? "OptionA" : "OptionB")} "
                                 + $"default atom kind'i catalogda yok: '{defaultKind}'.");
                }
                return;
            }

            foreach (string id in ids)
            {
                if (string.IsNullOrEmpty(id))
                {
                    problems.Add($"'{template.Id}' {(optionA ? "OptionA" : "OptionB")} "
                                 + "bos atom Id referansi iceriyor.");
                    continue;
                }

                CouncilEffectAtomSO atom = catalog.GetAtom(id);
                if (atom == null)
                    continue;

                if (!IsReferencedAtomAllowed(template.Contrast, optionA, atom.Kind))
                {
                    problems.Add($"'{template.Id}' {(optionA ? "OptionA" : "OptionB")} "
                                 + $"Council content recetesi '{atom.Id}' / '{atom.Kind}' atomuna izin vermiyor.");
                }
            }
        }

        private static void ValidateGlobalRecipeDependencies(
            CouncilEventCatalogSO catalog,
            CouncilTemplateSO template,
            List<string> problems)
        {
            CouncilEffectKind requiredKind = CouncilEffectKind.None;
            switch (template.Contrast)
            {
                case CouncilContrastType.ResourceTrade:
                case CouncilContrastType.SafeVsRisky:
                    requiredKind = CouncilEffectKind.GainResource;
                    break;
                case CouncilContrastType.EconomyVsDefense:
                    requiredKind = CouncilEffectKind.PayResource;
                    break;
            }

            if (requiredKind != CouncilEffectKind.None && !HasAtomKind(catalog, requiredKind))
            {
                problems.Add($"'{template.Id}' composer global dependency'si catalogda yok: "
                             + $"'{requiredKind}'.");
            }
        }

        private static bool TryValidateComposedOption(
            CouncilTemplateSO template,
            ComposedCouncilOption option,
            bool optionA,
            out string problem)
        {
            if (!TryValidateOptionRole(option, out problem))
                return false;

            foreach (ComposedCouncilEffect effect in option.Effects)
            {
                if (!IsComposedEffectAllowed(template.Contrast, optionA, effect.Kind))
                {
                    problem = $"'{template.Id}' {(optionA ? "OptionA" : "OptionB")} "
                              + $"composed payload'i content recetesi disinda effect tasiyor: "
                              + $"'{effect.Kind}'.";
                    return false;
                }
            }

            problem = string.Empty;
            return true;
        }

        private static CouncilEffectKind GetDefaultReferencedKind(
            CouncilContrastType contrast,
            bool optionA)
        {
            switch (contrast)
            {
                case CouncilContrastType.NowVsLater:
                    return optionA
                        ? CouncilEffectKind.GainResource
                        : CouncilEffectKind.TempProductionBoost;
                case CouncilContrastType.ResourceTrade:
                    return optionA
                        ? CouncilEffectKind.PayResource
                        : CouncilEffectKind.GainResource;
                case CouncilContrastType.PopulationVsResource:
                    return optionA
                        ? CouncilEffectKind.GainPopulation
                        : CouncilEffectKind.GainResource;
                case CouncilContrastType.EconomyVsDefense:
                    return optionA
                        ? CouncilEffectKind.GainFreeArchers
                        : CouncilEffectKind.HealDefensePercent;
                case CouncilContrastType.SafeVsRisky:
                    return CouncilEffectKind.NextNightSpawnDelta;
                case CouncilContrastType.PayOrSuffer:
                    return optionA
                        ? CouncilEffectKind.TempProductionPenalty
                        : CouncilEffectKind.PayResource;
                default:
                    return CouncilEffectKind.None;
            }
        }

        private static bool HasAtomKind(CouncilEventCatalogSO catalog, CouncilEffectKind kind)
        {
            if (catalog == null || catalog.Atoms == null || !IsCouncilOwnedEffectKind(kind))
                return false;

            foreach (CouncilEffectAtomSO atom in catalog.Atoms)
            {
                if (atom != null && atom.Kind == kind)
                    return true;
            }

            return false;
        }
    }
}
