using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Council kartinin oyuncuya gosterdigi exact sonucu hesaplamak icin gereken canli durum.
    /// UI, affordability ve uygulama oncesi sayisal ozet ayni snapshot'i kullanir.
    /// </summary>
    public struct CouncilOptionPresentationContext
    {
        public bool RuntimeReady;
        public bool PopulationRulesReady;
        public bool IgnoreResourcePayments;
        public ResourceData Resources;
        public int CurrentPopulation;
        public int TotalBedCapacity;
        public int FoodCostPerArrival;
        public int TotalArchers;
        public int IdlePopulation;
        public float WallCurrentHp;
        public float WallMaxHp;
    }

    public readonly struct CouncilOptionPresentation
    {
        public readonly string RichText;
        public readonly bool CanApplyExactly;
        public readonly string UnavailableReason;

        public CouncilOptionPresentation(string richText, bool canApplyExactly, string unavailableReason)
        {
            RichText = richText;
            CanApplyExactly = canApplyExactly;
            UnavailableReason = unavailableReason;
        }
    }

    /// <summary>
    /// Composed niyeti canli kaynak, population, archer ve Wall state'iyle quote eder.
    /// Secenek effects sirasiyla simule edilir; metinde gorunen sayilar gercek apply sirasiyla aynidir.
    /// </summary>
    public static class CouncilOptionPresentationUtility
    {
        private const string GainColor = "#8FD98A";
        private const string CostColor = "#E08A7A";
        private const string RiskColor = "#E5B963";
        private const string MutedColor = "#B7C0C8";

        public static CouncilOptionPresentation Build(
            ComposedCouncilOption option,
            in CouncilOptionPresentationContext context)
        {
            if (option == null)
                return new CouncilOptionPresentation(string.Empty, false, "NO OPTION");

            if (!CouncilContentPolicy.TryValidateOptionRole(option, out string contentProblem))
            {
                string blockedText = $"<color={CostColor}>{BlockedContentLabel()}</color>";
                return new CouncilOptionPresentation(blockedText, false,
                    CouncilContentPolicy.BlockedReason + ": " + contentProblem);
            }

            ResourceData resources = context.Resources;
            int population = Mathf.Max(0, context.CurrentPopulation);
            int totalArchers = Mathf.Max(0, context.TotalArchers);
            int idlePopulation = Mathf.Max(0, context.IdlePopulation);
            float wallCurrentHp = Mathf.Max(0f, context.WallCurrentHp);
            float wallMaxHp = Mathf.Max(0f, context.WallMaxHp);
            bool canApplyExactly = context.RuntimeReady;
            string unavailableReason = context.RuntimeReady ? string.Empty : "RUNTIME NOT READY";
            var parts = new List<string>(option.Effects != null ? option.Effects.Count : 0);

            if (option.Effects != null)
            {
                foreach (ComposedCouncilEffect effect in option.Effects)
                {
                    int amount = Mathf.Max(0, effect.Amount);
                    EconomyFocusType transactionResource = NormalizeTransactionResource(effect.Resource);

                    switch (effect.Kind)
                    {
                        case CouncilEffectKind.GainResource:
                            AddResource(ref resources, transactionResource, amount);
                            parts.Add(Gain($"+{amount} {ResourceName(transactionResource)}"));
                            break;

                        case CouncilEffectKind.PayResource:
                        {
                            int available = GetResource(resources, transactionResource);
                            parts.Add(Cost($"-{amount} {ResourceName(transactionResource)}"));
                            if (!context.IgnoreResourcePayments)
                            {
                                if (available < amount)
                                {
                                    MarkUnavailable(ref canApplyExactly, ref unavailableReason,
                                        $"NEED {amount - available} MORE {ResourceName(transactionResource)}");
                                }
                                else
                                {
                                    AddResource(ref resources, transactionResource, -amount);
                                }
                            }
                            break;
                        }

                        case CouncilEffectKind.TempProductionBoost:
                            parts.Add(Gain(
                                $"{ProductionName(effect.Resource)} +{Percent(Mathf.Abs(effect.Rate))}% / {Days(effect.DurationDays)}"));
                            break;

                        case CouncilEffectKind.TempProductionPenalty:
                            parts.Add(Cost(
                                $"{ProductionName(effect.Resource)} -{Percent(Mathf.Abs(effect.Rate))}% / {Days(effect.DurationDays)}"));
                            break;

                        case CouncilEffectKind.WorkerCapBonus:
                            parts.Add(Gain(effect.Resource == EconomyFocusType.Balanced
                                ? $"ALL WORKER CAPS +{amount}"
                                : $"{ResourceName(effect.Resource)} WORKER CAP +{amount}"));
                            break;

                        case CouncilEffectKind.GainPopulation:
                        {
                            int unitFoodCost = Mathf.Max(1, context.FoodCostPerArrival);
                            int requiredFood = SaturatingMultiply(amount, unitFoodCost);
                            parts.Add(Gain($"+{amount} PEOPLE"));
                            parts.Add(Cost($"-{requiredFood} FOOD"));

                            if (!context.PopulationRulesReady)
                            {
                                MarkUnavailable(ref canApplyExactly, ref unavailableReason,
                                    "POPULATION RULES NOT READY");
                                break;
                            }

                            MobilePopulationArrivalBudget budget =
                                CouncilEffectGuardUtility.CalculatePopulationGain(
                                    amount,
                                    population,
                                    context.TotalBedCapacity,
                                    resources.Food,
                                    unitFoodCost);
                            if (budget.AcceptedArrivals != amount)
                            {
                                int missingBeds = Mathf.Max(0, amount - budget.AvailableBedSpace);
                                int missingFood = Mathf.Max(0, requiredFood - resources.Food);
                                string reason = missingBeds > 0
                                    ? $"NEED {missingBeds} MORE BEDS"
                                    : $"NEED {missingFood} MORE FOOD";
                                MarkUnavailable(ref canApplyExactly, ref unavailableReason, reason);
                            }
                            else
                            {
                                population += amount;
                                resources.Food = Mathf.Max(0, resources.Food - requiredFood);
                                idlePopulation += amount;
                            }
                            break;
                        }

                        case CouncilEffectKind.GainFreeArchers:
                        {
                            parts.Add(Gain(amount == 1 ? "+1 BASIC ARCHER" : $"+{amount} BASIC ARCHERS"));
                            parts.Add(Cost(amount == 1 ? "-1 IDLE PERSON" : $"-{amount} IDLE PEOPLE"));
                            int allowed = CouncilEffectGuardUtility.GetAllowedFreeArcherGain(
                                amount,
                                totalArchers,
                                idlePopulation);
                            if (allowed != amount)
                            {
                                int remainingCapacity = ArcherCapacityUtility.GetRemainingCapacity(totalArchers);
                                string reason = idlePopulation < amount
                                    ? $"NEED {amount - idlePopulation} MORE IDLE"
                                    : $"NEED {amount - remainingCapacity} MORE ARMY SLOTS";
                                MarkUnavailable(ref canApplyExactly, ref unavailableReason, reason);
                            }
                            else
                            {
                                totalArchers += amount;
                                idlePopulation -= amount;
                            }
                            break;
                        }

                        case CouncilEffectKind.HealDefensePercent:
                        {
                            float healedHp = SingleWallDefenseRules.HealByMaxPercent(
                                wallCurrentHp,
                                wallMaxHp,
                                Mathf.Abs(effect.Rate));
                            float actualHeal = Mathf.Max(0f, healedHp - wallCurrentHp);
                            parts.Add(Gain(
                                $"+{FormatNumber(actualHeal)} WALL HP ({Percent(Mathf.Abs(effect.Rate))}% MAX)"));
                            wallCurrentHp = healedHp;
                            break;
                        }

                        case CouncilEffectKind.NextNightSpawnDelta:
                        {
                            float multiplier = CouncilEffectGuardUtility.ResolveNightCountMultiplier(effect.Rate);
                            int deltaPercent = Mathf.RoundToInt((multiplier - 1f) * 100f);
                            if (deltaPercent <= 0)
                                parts.Add(Gain($"NIGHT HORDE {deltaPercent}%"));
                            else
                                parts.Add(Risk($"NIGHT HORDE +{deltaPercent}%"));
                            break;
                        }
                    }
                }
            }

            if (parts.Count == 0)
                parts.Add($"<color={MutedColor}>NO NUMERICAL EFFECT</color>");

            string action = ExtractAction(option.Label);
            string effectLine = string.Join("  ·  ", parts);
            if (!canApplyExactly && !string.IsNullOrEmpty(unavailableReason))
                effectLine += $"  ·  <color={CostColor}>{unavailableReason}</color>";

            string richText = string.IsNullOrEmpty(action)
                ? $"<size=90%>{effectLine}</size>"
                : $"<b>{action}</b>\n<size=90%>{effectLine}</size>";
            return new CouncilOptionPresentation(richText, canApplyExactly, unavailableReason);
        }

        private static void MarkUnavailable(
            ref bool canApplyExactly,
            ref string unavailableReason,
            string reason)
        {
            canApplyExactly = false;
            if (string.IsNullOrEmpty(unavailableReason))
                unavailableReason = reason;
        }

        private static string ExtractAction(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return string.Empty;

            int separator = label.IndexOf('—');
            return (separator >= 0 ? label.Substring(0, separator) : label).Trim();
        }

        private static EconomyFocusType NormalizeTransactionResource(EconomyFocusType resource)
        {
            return resource == EconomyFocusType.Balanced ? EconomyFocusType.Wood : resource;
        }

        private static int GetResource(in ResourceData resources, EconomyFocusType resource)
        {
            switch (resource)
            {
                case EconomyFocusType.Stone: return resources.Stone;
                case EconomyFocusType.Iron: return resources.Iron;
                case EconomyFocusType.Food: return resources.Food;
                default: return resources.Wood;
            }
        }

        private static void AddResource(ref ResourceData resources, EconomyFocusType resource, int amount)
        {
            switch (resource)
            {
                case EconomyFocusType.Stone: resources.Stone += amount; break;
                case EconomyFocusType.Iron: resources.Iron += amount; break;
                case EconomyFocusType.Food: resources.Food += amount; break;
                default: resources.Wood += amount; break;
            }
        }

        private static string ResourceName(EconomyFocusType resource)
        {
            switch (resource)
            {
                case EconomyFocusType.Stone: return "STONE";
                case EconomyFocusType.Iron: return "IRON";
                case EconomyFocusType.Food: return "FOOD";
                case EconomyFocusType.Balanced: return "ALL";
                default: return "WOOD";
            }
        }

        private static string ProductionName(EconomyFocusType resource)
        {
            return resource == EconomyFocusType.Balanced
                ? "ALL PRODUCTION"
                : ResourceName(resource) + " PRODUCTION";
        }

        private static int Percent(float value)
        {
            return Mathf.RoundToInt(Mathf.Max(0f, value) * 100f);
        }

        private static string Days(int durationDays)
        {
            int days = Mathf.Max(1, durationDays);
            return days == 1 ? "1 DAY" : days + " DAYS";
        }

        private static string FormatNumber(float value)
        {
            return Mathf.Approximately(value, Mathf.Round(value))
                ? Mathf.RoundToInt(value).ToString()
                : value.ToString("0.#");
        }

        private static int SaturatingMultiply(int left, int right)
        {
            long value = (long)Mathf.Max(0, left) * Mathf.Max(0, right);
            return value >= int.MaxValue ? int.MaxValue : (int)value;
        }

        private static string Gain(string text) => $"<color={GainColor}>{text}</color>";
        private static string Cost(string text) => $"<color={CostColor}>{text}</color>";
        private static string Risk(string text) => $"<color={RiskColor}>{text}</color>";
        private static string BlockedContentLabel() => "CONTENT BLOCKED";
    }

    /// <summary>Karar kartinin Dawn + Day penceresini cycle state'inden sayisal olarak uretir.</summary>
    public static class CouncilDecisionWindowUtility
    {
        public static float GetRemainingSeconds(in ContinuousSiegeCycleData cycle)
        {
            float phaseRemaining = 1f - Mathf.Clamp01(cycle.PhaseProgress01);
            switch (cycle.Phase)
            {
                case SiegeCyclePhase.Dawn:
                    return Mathf.Max(0f, cycle.DawnDuration * phaseRemaining + cycle.DayDuration);
                case SiegeCyclePhase.Day:
                    return Mathf.Max(0f, cycle.DayDuration * phaseRemaining);
                default:
                    return 0f;
            }
        }

        public static string FormatCountdown(float remainingSeconds)
        {
            return $"DECIDE  {Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds))}s";
        }
    }
}
