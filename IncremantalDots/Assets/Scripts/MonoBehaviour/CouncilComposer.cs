using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>Composer'in okudugu oyun durumu anlik goruntusu (pure — test edilebilir).</summary>
    public struct CouncilContext
    {
        public int Day;
        public int Wood, Stone, Iron, Food;
        public float WoodPerMin, StonePerMin, IronPerMin, FoodPerMin;
        public float Defense01;
        /// <summary>flag -> setlendigi gun (zincir gecikmeleri icin).</summary>
        public Dictionary<string, int> Flags;
        public List<string> RecentTemplateIds;
        public HashSet<string> UsedOneShotTemplateIds;
    }

    /// <summary>Uretilmis tek bir etki (somut sayilarla).</summary>
    [Serializable]
    public struct ComposedCouncilEffect
    {
        public CouncilEffectKind Kind;
        public EconomyFocusType Resource;
        public int Amount;
        public float Rate;
        public int DurationDays;
    }

    [Serializable]
    public sealed class ComposedCouncilOption
    {
        public string Label;
        public List<ComposedCouncilEffect> Effects = new List<ComposedCouncilEffect>();
        public float BudgetMinutes;
    }

    [Serializable]
    public sealed class ComposedCouncilEvent
    {
        public string TemplateId;
        public string Title;
        public string Body;
        /// <summary>Secim sonrasi kartta gosterilen sonuc metinleri (token'lari doldurulmus).</summary>
        public string OutcomeA;
        public string OutcomeB;
        public ComposedCouncilOption OptionA;
        public ComposedCouncilOption OptionB;
        public string SetsFlagOnA;
        public string SetsFlagOnB;
    }

    /// <summary>
    /// Council event uretici: sablon x atom x baglam x olcek. Deterministiktir (ayni seed +
    /// ayni context = ayni event). Director agirliklari kit kaynagi/dusuk savunmayi kayirir;
    /// butce dengeleme A/B seceneklerini ortak "dakika-degeri" para birimine normalize eder.
    /// UnityEngine.Object bagimliligi yalniz SO okumaktir — EditMode testlerinde dogrudan kosulur.
    /// </summary>
    public static class CouncilComposer
    {
        private const float SmallBand = 0.7f;
        private const float FairBand = 1.0f;
        private const float GenerousBand = 1.4f;
        private const float RecentTemplateWeightMult = 0.15f;
        private const float BudgetTolerance = 1.25f; // A/B butce orani bu katsayiyi asarsa dusuk taraf yukseltilir

        // ---------------------------------------------------------------
        // Ana giris
        // ---------------------------------------------------------------
        public static ComposedCouncilEvent Compose(CouncilEventCatalogSO catalog, uint seed, in CouncilContext context)
        {
            if (catalog == null || catalog.Templates == null || catalog.Atoms == null)
                return null;

            var rng = new Unity.Mathematics.Random(seed == 0u ? 1u : seed);
            // Warm-up: ardisik/kucuk seed'lerde ilk orneklemin korelasyonunu kirar
            rng.NextUInt();
            rng.NextUInt();
            var template = PickTemplate(catalog, ref rng, context);
            if (template == null)
                return null;

            var composed = new ComposedCouncilEvent
            {
                TemplateId = template.Id,
                Title = template.Title,
                SetsFlagOnA = template.SetsFlagOnA,
                SetsFlagOnB = template.SetsFlagOnB,
            };

            float band = PickBand(ref rng);
            // Govde varyanti secimi (rng tuketimi BuildOptions'tan ONCE — deterministik sira)
            string bodyRaw = template.Body;
            if (template.BodyVariants != null && template.BodyVariants.Length > 0)
                bodyRaw = template.BodyVariants[rng.NextInt(0, template.BodyVariants.Length)];

            BuildOptions(catalog, template, ref rng, context, band, composed);
            if (composed.OptionA == null || composed.OptionB == null)
                return null;

            BalanceBudgets(composed, context);
            composed.OptionA.Label = template.OptionAVerb + "  —  " + DescribeEffects(composed.OptionA.Effects);
            composed.OptionB.Label = template.OptionBVerb + "  —  " + DescribeEffects(composed.OptionB.Effects);
            // Token doldurma: govde her iki secenegin sayilarini kullanabilir (once A, sonra B
            // taranir); sonuc metinleri YALNIZ kendi seceneginin efektlerinden cozulur.
            composed.Body = FillTokens(bodyRaw, composed.OptionA, composed.OptionB, context.Day);
            composed.OutcomeA = FillTokens(template.OutcomeA, composed.OptionA, null, context.Day);
            composed.OutcomeB = FillTokens(template.OutcomeB, composed.OptionB, null, context.Day);
            return composed;
        }

        // ---------------------------------------------------------------
        // Sablon secimi (flag/gun filtreleri + anti-tekrar + director on-skoru)
        // ---------------------------------------------------------------
        private static CouncilTemplateSO PickTemplate(CouncilEventCatalogSO catalog,
            ref Unity.Mathematics.Random rng, in CouncilContext context)
        {
            var candidates = new List<CouncilTemplateSO>();
            var weights = new List<float>();
            float totalWeight = 0f;

            foreach (var template in catalog.Templates)
            {
                if (template == null || template.BaseWeight <= 0f)
                    continue;
                if (context.Day < template.MinDay)
                    continue;
                if (template.OneShot && context.UsedOneShotTemplateIds != null
                    && context.UsedOneShotTemplateIds.Contains(template.Id))
                    continue;
                if (!FlagsSatisfied(template, context))
                    continue;

                float weight = template.BaseWeight;
                if (context.RecentTemplateIds != null && context.RecentTemplateIds.Contains(template.Id))
                    weight *= RecentTemplateWeightMult;

                // Director on-skoru: sablonun A-taraf atom adaylarindan en yuksek baglam carpani
                weight *= MaxDirectorMult(catalog, template, context);

                candidates.Add(template);
                weights.Add(weight);
                totalWeight += weight;
            }

            if (candidates.Count == 0 || totalWeight <= 0f)
                return null;

            float roll = rng.NextFloat(0f, totalWeight);
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f)
                    return candidates[i];
            }

            return candidates[candidates.Count - 1];
        }

        private static bool FlagsSatisfied(CouncilTemplateSO template, in CouncilContext context)
        {
            if (template.RequiredFlags != null)
            {
                foreach (var flag in template.RequiredFlags)
                {
                    if (string.IsNullOrEmpty(flag))
                        continue;
                    if (context.Flags == null || !context.Flags.TryGetValue(flag, out int setDay))
                        return false;
                    if (template.ChainDelayDays > 0 && context.Day < setDay + template.ChainDelayDays)
                        return false;
                }
            }

            if (template.ForbiddenFlags != null && context.Flags != null)
            {
                foreach (var flag in template.ForbiddenFlags)
                {
                    if (!string.IsNullOrEmpty(flag) && context.Flags.ContainsKey(flag))
                        return false;
                }
            }

            return true;
        }

        private static float MaxDirectorMult(CouncilEventCatalogSO catalog, CouncilTemplateSO template,
            in CouncilContext context)
        {
            float best = 1f;
            var pool = ResolveAtomPool(catalog, template.OptionAAtomIds, CouncilEffectKind.None);
            foreach (var atom in pool)
                best = Mathf.Max(best, DirectorMult(atom, context));
            return best;
        }

        // ---------------------------------------------------------------
        // Karsitlik receteleri: A/B seceneklerinin kurulumu
        // ---------------------------------------------------------------
        private static void BuildOptions(CouncilEventCatalogSO catalog, CouncilTemplateSO template,
            ref Unity.Mathematics.Random rng, in CouncilContext context, float band, ComposedCouncilEvent composed)
        {
            var a = new ComposedCouncilOption();
            var b = new ComposedCouncilOption();

            switch (template.Contrast)
            {
                case CouncilContrastType.NowVsLater:
                {
                    var gain = PickAtom(catalog, template.OptionAAtomIds, CouncilEffectKind.GainResource, ref rng, context);
                    var boost = PickAtom(catalog, template.OptionBAtomIds, CouncilEffectKind.TempProductionBoost, ref rng, context);
                    if (gain == null || boost == null) return;
                    var res = ResolveResource(gain, ref rng, context, preferScarce: true);
                    AddEffect(a, gain, res, band, context);
                    AddEffect(b, boost, res, band, context);
                    break;
                }
                case CouncilContrastType.ResourceTrade:
                {
                    var pay = PickAtom(catalog, template.OptionAAtomIds, CouncilEffectKind.PayResource, ref rng, context);
                    var gain = PickAtom(catalog, null, CouncilEffectKind.GainResource, ref rng, context);
                    var consolation = PickAtom(catalog, template.OptionBAtomIds, CouncilEffectKind.GainResource, ref rng, context);
                    if (pay == null || gain == null || consolation == null) return;
                    var scarce = MostScarceResource(context);
                    var abundant = MostAbundantResource(context, exclude: scarce);
                    AddEffect(a, pay, abundant, band, context);
                    AddEffect(a, gain, scarce, band, context);
                    AddEffect(b, consolation, EconomyFocusType.Food, band * 0.5f, context);
                    break;
                }
                case CouncilContrastType.PopulationVsResource:
                {
                    var pop = PickAtom(catalog, template.OptionAAtomIds, CouncilEffectKind.GainPopulation, ref rng, context);
                    var gain = PickAtom(catalog, template.OptionBAtomIds, CouncilEffectKind.GainResource, ref rng, context);
                    if (pop == null || gain == null) return;
                    AddEffect(a, pop, EconomyFocusType.Balanced, band, context);
                    AddEffect(b, gain, ResolveResource(gain, ref rng, context, preferScarce: true), band, context);
                    break;
                }
                case CouncilContrastType.EconomyVsDefense:
                {
                    var pay = PickAtom(catalog, null, CouncilEffectKind.PayResource, ref rng, context);
                    var archers = PickAtom(catalog, template.OptionAAtomIds, CouncilEffectKind.GainFreeArchers, ref rng, context);
                    var heal = PickAtom(catalog, template.OptionBAtomIds, CouncilEffectKind.HealDefensePercent, ref rng, context);
                    if (pay == null || archers == null || heal == null) return;
                    AddEffect(a, pay, EconomyFocusType.Food, band * 0.6f, context);
                    AddEffect(a, archers, EconomyFocusType.Balanced, band, context);
                    AddEffect(b, heal, EconomyFocusType.Balanced, band, context);
                    break;
                }
                case CouncilContrastType.SafeVsRisky:
                {
                    var calm = PickAtom(catalog, template.OptionAAtomIds, CouncilEffectKind.NextNightSpawnDelta, ref rng, context);
                    var loot = PickAtom(catalog, null, CouncilEffectKind.GainResource, ref rng, context);
                    var danger = PickAtom(catalog, template.OptionBAtomIds, CouncilEffectKind.NextNightSpawnDelta, ref rng, context);
                    if (calm == null || loot == null || danger == null) return;
                    // A: sakin gece (negatif delta atomu)
                    AddEffect(a, calm, EconomyFocusType.Balanced, band, context, rateSign: -1f);
                    // B: iki kaynak yagmasi + tehlikeli gece (pozitif delta)
                    var r1 = MostScarceResource(context);
                    var r2 = MostAbundantResource(context, exclude: r1);
                    AddEffect(b, loot, r1, band, context);
                    AddEffect(b, loot, r2, band * 0.7f, context);
                    AddEffect(b, danger, EconomyFocusType.Balanced, band, context, rateSign: 1f);
                    break;
                }
                case CouncilContrastType.PayOrSuffer:
                {
                    var penalty = PickAtom(catalog, template.OptionAAtomIds, CouncilEffectKind.TempProductionPenalty, ref rng, context);
                    var pay = PickAtom(catalog, template.OptionBAtomIds, CouncilEffectKind.PayResource, ref rng, context);
                    if (penalty == null || pay == null) return;
                    var res = ResolveResource(penalty, ref rng, context, preferScarce: false);
                    AddEffect(a, penalty, res, band, context);
                    AddEffect(b, pay, MostAbundantResource(context, exclude: EconomyFocusType.Balanced), band, context);
                    break;
                }
                default:
                    return;
            }

            if (a.Effects.Count == 0 || b.Effects.Count == 0)
                return;

            composed.OptionA = a;
            composed.OptionB = b;
        }

        // ---------------------------------------------------------------
        // Atom secimi + etki uretimi
        // ---------------------------------------------------------------
        private static List<CouncilEffectAtomSO> ResolveAtomPool(CouncilEventCatalogSO catalog,
            string[] restrictIds, CouncilEffectKind kind)
        {
            var pool = new List<CouncilEffectAtomSO>();
            bool restricted = restrictIds != null && restrictIds.Length > 0;
            if (restricted)
            {
                foreach (var id in restrictIds)
                {
                    var atom = catalog.GetAtom(id);
                    if (atom != null && (kind == CouncilEffectKind.None || atom.Kind == kind))
                        pool.Add(atom);
                }
            }
            else
            {
                foreach (var atom in catalog.Atoms)
                {
                    if (atom != null && (kind == CouncilEffectKind.None || atom.Kind == kind))
                        pool.Add(atom);
                }
            }

            return pool;
        }

        private static CouncilEffectAtomSO PickAtom(CouncilEventCatalogSO catalog, string[] restrictIds,
            CouncilEffectKind kind, ref Unity.Mathematics.Random rng, in CouncilContext context)
        {
            // Sablon kisit listesi doluysa tur filtresi kalkar: kisit = tam guven
            // (orn. NowVsLater'in B'sine WorkerCapBonus atomu baglanabilir)
            bool restricted = restrictIds != null && restrictIds.Length > 0;
            var pool = ResolveAtomPool(catalog, restrictIds, restricted ? CouncilEffectKind.None : kind);
            if (pool.Count == 0)
                return null;

            float total = 0f;
            var weights = new float[pool.Count];
            for (int i = 0; i < pool.Count; i++)
            {
                weights[i] = Mathf.Max(0.01f, DirectorMult(pool[i], context));
                total += weights[i];
            }

            float roll = rng.NextFloat(0f, total);
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f)
                    return pool[i];
            }

            return pool[pool.Count - 1];
        }

        /// <summary>Director baglam carpani: kitlik/bolluk/dusuk-savunma kayirmasi.</summary>
        private static float DirectorMult(CouncilEffectAtomSO atom, in CouncilContext context)
        {
            float mult = 1f;
            if (atom.Resource != EconomyFocusType.Balanced)
            {
                float stock = GetStock(context, atom.Resource);
                float perMin = Mathf.Max(0.01f, GetProduction(context, atom.Resource));
                float minutesOfStock = stock / perMin;
                if (minutesOfStock < atom.ScarcityThresholdMinutes)
                    mult *= Mathf.Max(0.01f, atom.ScarcityWeightMult);
                else if (minutesOfStock > atom.ScarcityThresholdMinutes * 2f)
                    mult *= Mathf.Max(0.01f, atom.AbundanceWeightMult);
            }

            if (context.Defense01 < 0.5f)
                mult *= Mathf.Max(0.01f, atom.LowDefenseWeightMult);

            return mult;
        }

        private static void AddEffect(ComposedCouncilOption option, CouncilEffectAtomSO atom,
            EconomyFocusType resource, float band, in CouncilContext context, float rateSign = 1f)
        {
            var effect = new ComposedCouncilEffect
            {
                Kind = atom.Kind,
                Resource = resource,
                DurationDays = Mathf.Max(0, atom.DurationDays),
            };

            switch (atom.Kind)
            {
                case CouncilEffectKind.GainResource:
                case CouncilEffectKind.PayResource:
                {
                    float perMin = GetProduction(context, resource);
                    float raw = perMin > 0.01f
                        ? perMin * atom.MinutesOfProduction * band
                        : atom.FlatFallback * band;
                    effect.Amount = Mathf.Max(1, Mathf.RoundToInt(raw / 5f) * 5); // 5'e yuvarla (okunabilir sayilar)
                    break;
                }
                case CouncilEffectKind.GainPopulation:
                case CouncilEffectKind.GainFreeArchers:
                case CouncilEffectKind.WorkerCapBonus:
                    effect.Amount = Mathf.Max(1, Mathf.RoundToInt((atom.Rate + context.Day * atom.PerDay) * band));
                    break;
                case CouncilEffectKind.TempProductionBoost:
                case CouncilEffectKind.TempProductionPenalty:
                case CouncilEffectKind.HealDefensePercent:
                case CouncilEffectKind.NextNightSpawnDelta:
                    effect.Rate = atom.Rate * band * rateSign;
                    break;
            }

            option.Effects.Add(effect);
            float sign = atom.Kind == CouncilEffectKind.PayResource
                || atom.Kind == CouncilEffectKind.TempProductionPenalty ? -1f : 1f;
            if (atom.Kind == CouncilEffectKind.NextNightSpawnDelta)
                sign = -rateSign; // tehlike (+delta) butceden DUSER, sakinlik (-delta) EKLER
            option.BudgetMinutes += Mathf.Abs(atom.BudgetMinutes) * band * sign;
        }

        /// <summary>A/B butcelerini kaba dengele: oran toleransi asarsa dusuk tarafin ilk kaynak etkisi buyutulur.</summary>
        private static void BalanceBudgets(ComposedCouncilEvent composed, in CouncilContext context)
        {
            float a = Mathf.Max(0.1f, composed.OptionA.BudgetMinutes);
            float b = Mathf.Max(0.1f, composed.OptionB.BudgetMinutes);
            if (a / b <= BudgetTolerance && b / a <= BudgetTolerance)
                return;

            var weak = a < b ? composed.OptionA : composed.OptionB;
            float scale = Mathf.Max(a, b) / Mathf.Max(0.1f, Mathf.Min(a, b));
            scale = Mathf.Min(scale, 2.5f);
            for (int i = 0; i < weak.Effects.Count; i++)
            {
                var effect = weak.Effects[i];
                if (effect.Kind == CouncilEffectKind.GainResource || effect.Kind == CouncilEffectKind.GainPopulation)
                {
                    effect.Amount = Mathf.Max(1, Mathf.RoundToInt(effect.Amount * scale / 5f) * 5);
                    weak.Effects[i] = effect;
                    weak.BudgetMinutes *= scale;
                    break;
                }
            }
        }

        // ---------------------------------------------------------------
        // Yardimcilar
        // ---------------------------------------------------------------
        private static float PickBand(ref Unity.Mathematics.Random rng)
        {
            float roll = rng.NextFloat();
            if (roll < 0.35f) return SmallBand;
            if (roll < 0.85f) return FairBand;
            return GenerousBand;
        }

        private static EconomyFocusType ResolveResource(CouncilEffectAtomSO atom,
            ref Unity.Mathematics.Random rng, in CouncilContext context, bool preferScarce)
        {
            if (atom.Resource != EconomyFocusType.Balanced)
                return atom.Resource;

            if (preferScarce)
                return MostScarceResource(context);

            // rastgele somut kaynak
            switch (rng.NextInt(0, 4))
            {
                case 0: return EconomyFocusType.Wood;
                case 1: return EconomyFocusType.Stone;
                case 2: return EconomyFocusType.Iron;
                default: return EconomyFocusType.Food;
            }
        }

        public static EconomyFocusType MostScarceResource(in CouncilContext context)
        {
            return CompareByStockMinutes(context, findScarce: true, exclude: EconomyFocusType.Balanced);
        }

        public static EconomyFocusType MostAbundantResource(in CouncilContext context, EconomyFocusType exclude)
        {
            return CompareByStockMinutes(context, findScarce: false, exclude: exclude);
        }

        private static readonly EconomyFocusType[] AllResources =
        {
            EconomyFocusType.Wood, EconomyFocusType.Stone, EconomyFocusType.Iron, EconomyFocusType.Food
        };

        private static EconomyFocusType CompareByStockMinutes(in CouncilContext context, bool findScarce,
            EconomyFocusType exclude)
        {
            EconomyFocusType best = EconomyFocusType.Wood;
            float bestValue = findScarce ? float.MaxValue : float.MinValue;
            foreach (var resource in AllResources)
            {
                if (resource == exclude)
                    continue;
                float minutes = GetStock(context, resource) / Mathf.Max(0.01f, GetProduction(context, resource));
                bool better = findScarce ? minutes < bestValue : minutes > bestValue;
                if (better)
                {
                    bestValue = minutes;
                    best = resource;
                }
            }

            return best;
        }

        private static float GetStock(in CouncilContext context, EconomyFocusType resource)
        {
            switch (resource)
            {
                case EconomyFocusType.Stone: return context.Stone;
                case EconomyFocusType.Iron: return context.Iron;
                case EconomyFocusType.Food: return context.Food;
                default: return context.Wood;
            }
        }

        private static float GetProduction(in CouncilContext context, EconomyFocusType resource)
        {
            switch (resource)
            {
                case EconomyFocusType.Stone: return context.StonePerMin;
                case EconomyFocusType.Iron: return context.IronPerMin;
                case EconomyFocusType.Food: return context.FoodPerMin;
                default: return context.WoodPerMin;
            }
        }

        // TMP rich-text renkleri: kazanc yesil, bedel kirmizi, risk turuncu
        private const string GainColor = "#8FD98A";
        private const string CostColor = "#E08A7A";
        private const string RiskColor = "#E5B963";

        private static string DescribeEffects(List<ComposedCouncilEffect> effects)
        {
            var parts = new List<string>(effects.Count);
            foreach (var effect in effects)
            {
                switch (effect.Kind)
                {
                    case CouncilEffectKind.GainResource:
                        parts.Add($"<color={GainColor}>+{effect.Amount} {ResourceName(effect.Resource)}</color>");
                        break;
                    case CouncilEffectKind.PayResource:
                        parts.Add($"<color={CostColor}>-{effect.Amount} {ResourceName(effect.Resource)}</color>");
                        break;
                    case CouncilEffectKind.TempProductionBoost:
                        parts.Add($"<color={GainColor}>{ResourceName(effect.Resource)} +{Mathf.RoundToInt(effect.Rate * 100f)}% for {FormatDays(effect.DurationDays)}</color>");
                        break;
                    case CouncilEffectKind.TempProductionPenalty:
                        parts.Add($"<color={CostColor}>{ResourceName(effect.Resource)} -{Mathf.RoundToInt(effect.Rate * 100f)}% for {FormatDays(effect.DurationDays)}</color>");
                        break;
                    case CouncilEffectKind.WorkerCapBonus:
                        parts.Add($"<color={GainColor}>{ResourceName(effect.Resource)} crew cap +{effect.Amount}</color>");
                        break;
                    case CouncilEffectKind.GainPopulation:
                        parts.Add($"<color={GainColor}>+{effect.Amount} people</color>");
                        break;
                    case CouncilEffectKind.GainFreeArchers:
                        parts.Add(effect.Amount == 1
                            ? $"<color={GainColor}>+1 archer</color>"
                            : $"<color={GainColor}>+{effect.Amount} archers</color>");
                        break;
                    case CouncilEffectKind.HealDefensePercent:
                        parts.Add($"<color={GainColor}>defenses restored +{Mathf.RoundToInt(effect.Rate * 100f)}%</color>");
                        break;
                    case CouncilEffectKind.NextNightSpawnDelta:
                        parts.Add(effect.Rate < 0f
                            ? $"<color={GainColor}>a quieter night ({Mathf.RoundToInt(effect.Rate * 100f)}%)</color>"
                            : $"<color={RiskColor}>a harder night (+{Mathf.RoundToInt(effect.Rate * 100f)}%)</color>");
                        break;
                }
            }

            return string.Join(", ", parts);
        }

        private static string FormatDays(int days)
        {
            return days == 1 ? "1 day" : days + " days";
        }

        // ---------------------------------------------------------------
        // Metin token'lari: {GAIN_N} {GAIN_RES} {PAY_N} {PAY_RES} {POP_N} {ARCHER_N}
        // {BOOST_RES} {BOOST_PCT} {BOOST_D} {PEN_RES} {PEN_PCT} {PEN_D} {HEAL_PCT}
        // {NIGHT_PCT} {CAP_RES} {CAP_N} {DAY} — once primary sonra secondary taranir.
        // ---------------------------------------------------------------
        private static string FillTokens(string text, ComposedCouncilOption primary,
            ComposedCouncilOption secondary, int day)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0)
                return text;

            text = text.Replace("{DAY}", day.ToString());

            ComposedCouncilEffect effect;
            if (TryFindEffect(primary, secondary, CouncilEffectKind.GainResource, out effect))
                text = text.Replace("{GAIN_N}", effect.Amount.ToString())
                           .Replace("{GAIN_RES}", ResourceName(effect.Resource));
            if (TryFindEffect(primary, secondary, CouncilEffectKind.PayResource, out effect))
                text = text.Replace("{PAY_N}", effect.Amount.ToString())
                           .Replace("{PAY_RES}", ResourceName(effect.Resource));
            if (TryFindEffect(primary, secondary, CouncilEffectKind.GainPopulation, out effect))
                text = text.Replace("{POP_N}", effect.Amount.ToString());
            if (TryFindEffect(primary, secondary, CouncilEffectKind.GainFreeArchers, out effect))
                text = text.Replace("{ARCHER_N}", effect.Amount.ToString());
            if (TryFindEffect(primary, secondary, CouncilEffectKind.TempProductionBoost, out effect))
                text = text.Replace("{BOOST_RES}", ResourceName(effect.Resource))
                           .Replace("{BOOST_PCT}", Mathf.RoundToInt(effect.Rate * 100f).ToString())
                           .Replace("{BOOST_D}", effect.DurationDays.ToString());
            if (TryFindEffect(primary, secondary, CouncilEffectKind.TempProductionPenalty, out effect))
                text = text.Replace("{PEN_RES}", ResourceName(effect.Resource))
                           .Replace("{PEN_PCT}", Mathf.RoundToInt(effect.Rate * 100f).ToString())
                           .Replace("{PEN_D}", effect.DurationDays.ToString());
            if (TryFindEffect(primary, secondary, CouncilEffectKind.HealDefensePercent, out effect))
                text = text.Replace("{HEAL_PCT}", Mathf.RoundToInt(effect.Rate * 100f).ToString());
            if (TryFindEffect(primary, secondary, CouncilEffectKind.NextNightSpawnDelta, out effect))
                text = text.Replace("{NIGHT_PCT}", Mathf.RoundToInt(Mathf.Abs(effect.Rate) * 100f).ToString());
            if (TryFindEffect(primary, secondary, CouncilEffectKind.WorkerCapBonus, out effect))
                text = text.Replace("{CAP_RES}", ResourceName(effect.Resource))
                           .Replace("{CAP_N}", effect.Amount.ToString());

            return text;
        }

        private static bool TryFindEffect(ComposedCouncilOption primary, ComposedCouncilOption secondary,
            CouncilEffectKind kind, out ComposedCouncilEffect found)
        {
            if (primary != null)
            {
                foreach (var effect in primary.Effects)
                {
                    if (effect.Kind == kind) { found = effect; return true; }
                }
            }

            if (secondary != null)
            {
                foreach (var effect in secondary.Effects)
                {
                    if (effect.Kind == kind) { found = effect; return true; }
                }
            }

            found = default;
            return false;
        }

        private static string ResourceName(EconomyFocusType resource)
        {
            switch (resource)
            {
                case EconomyFocusType.Stone: return "STONE";
                case EconomyFocusType.Iron: return "IRON";
                case EconomyFocusType.Food: return "FOOD";
                case EconomyFocusType.Wood: return "WOOD";
                default: return "SUPPLIES";
            }
        }
    }
}
