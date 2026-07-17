using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Olumde verilen Souls miktarinin production tuning sahibi. Kill katkisi yuksek
    /// surulerde meta ekonomiyi patlatmamak icin uc azalan banda ayrilir; ilerleme,
    /// tamamlanan geceler, peak population ve yeni day rekoruyla da odullendirilir.
    /// </summary>
    [Serializable]
    public sealed class MetaRewardSettings
    {
        [Header("Diminishing Kill Bands")]
        [Min(1)] public int FirstKillBandLimit = 100;
        [Min(2)] public int SecondKillBandLimit = 1000;
        [Min(0f)] public float FirstBandSoulsPerKill = 1f;
        [Min(0f)] public float SecondBandSoulsPerKill = 0.25f;
        [Min(0f)] public float OverflowSoulsPerKill = 0.05f;

        [Header("Run Progress Weights")]
        [Min(0f)] public float SoulsPerDayReached = 10f;
        [Min(0f)] public float SoulsPerNightSurvived = 25f;
        [Min(0f)] public float SoulsPerPeakPopulation = 0.2f;
        [Min(0f)] public float NewRecordSoulsPerDay = 50f;

        public bool IsValid()
        {
            return FirstKillBandLimit > 0
                   && SecondKillBandLimit > FirstKillBandLimit
                   && IsFiniteNonNegative(FirstBandSoulsPerKill)
                   && IsFiniteNonNegative(SecondBandSoulsPerKill)
                   && IsFiniteNonNegative(OverflowSoulsPerKill)
                   && FirstBandSoulsPerKill >= SecondBandSoulsPerKill
                   && SecondBandSoulsPerKill >= OverflowSoulsPerKill
                   && IsFiniteNonNegative(SoulsPerDayReached)
                   && IsFiniteNonNegative(SoulsPerNightSurvived)
                   && IsFiniteNonNegative(SoulsPerPeakPopulation)
                   && IsFiniteNonNegative(NewRecordSoulsPerDay);
        }

        public void CollectValidationErrors(List<string> problems)
        {
            if (problems == null)
                throw new ArgumentNullException(nameof(problems));

            if (FirstKillBandLimit <= 0)
                problems.Add("Meta reward first kill band limit sifirdan buyuk olmali.");
            if (SecondKillBandLimit <= FirstKillBandLimit)
                problems.Add("Meta reward second kill band limit first limit'ten buyuk olmali.");
            if (!IsFiniteNonNegative(FirstBandSoulsPerKill)
                || !IsFiniteNonNegative(SecondBandSoulsPerKill)
                || !IsFiniteNonNegative(OverflowSoulsPerKill))
            {
                problems.Add("Meta reward kill agirliklari sonlu ve negatif olmayan degerler olmali.");
            }
            else if (FirstBandSoulsPerKill < SecondBandSoulsPerKill
                     || SecondBandSoulsPerKill < OverflowSoulsPerKill)
            {
                problems.Add("Meta reward kill agirliklari band ilerledikce artamaz.");
            }

            if (!IsFiniteNonNegative(SoulsPerDayReached)
                || !IsFiniteNonNegative(SoulsPerNightSurvived)
                || !IsFiniteNonNegative(SoulsPerPeakPopulation)
                || !IsFiniteNonNegative(NewRecordSoulsPerDay))
            {
                problems.Add("Meta reward ilerleme agirliklari sonlu ve negatif olmayan degerler olmali.");
            }
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        }
    }

    /// <summary>
    /// Tek kosu icin hesaplanmis immutable-olmasi beklenen odul makbuzu. Death receipt bu
    /// sonucu durable saklar; recovery tuning assetini yeniden okuyup farkli sonuc uretmez.
    /// </summary>
    [Serializable]
    public struct MetaRewardQuote
    {
        public int Day;
        public int Kills;
        public int NightsSurvived;
        public int PeakPopulation;
        public int PreviousBestDay;
        public int KillSouls;
        public int DaySouls;
        public int NightSouls;
        public int PopulationSouls;
        public int RecordSouls;
        public int TotalSouls;
        public bool NewRecord;
    }

    /// <summary>Meta reward hesaplamasinin tek saf formul sahibi.</summary>
    public static class MetaRewardCalculator
    {
        public const int LegacyRecordBonusPerDay = 50;

        public static bool TryCalculate(
            MetaRewardSettings settings,
            int day,
            int kills,
            int peakPopulation,
            int previousBestDay,
            out MetaRewardQuote quote)
        {
            quote = default;
            if (settings == null || !settings.IsValid())
                return false;

            int safeDay = Math.Max(0, day);
            int safeKills = Math.Max(0, kills);
            int safePeakPopulation = Math.Max(0, peakPopulation);
            int safePreviousBestDay = Math.Max(0, previousBestDay);
            int nightsSurvived = Math.Max(0, safeDay - 1);
            bool newRecord = safeDay > safePreviousBestDay;

            int firstKills = Math.Min(safeKills, settings.FirstKillBandLimit);
            int secondKills = Math.Min(
                Math.Max(0, safeKills - settings.FirstKillBandLimit),
                settings.SecondKillBandLimit - settings.FirstKillBandLimit);
            int overflowKills = Math.Max(0, safeKills - settings.SecondKillBandLimit);

            int killSouls = FloorSaturating(
                (double)firstKills * settings.FirstBandSoulsPerKill
                + (double)secondKills * settings.SecondBandSoulsPerKill
                + (double)overflowKills * settings.OverflowSoulsPerKill);
            int daySouls = FloorSaturating((double)safeDay * settings.SoulsPerDayReached);
            int nightSouls = FloorSaturating(
                (double)nightsSurvived * settings.SoulsPerNightSurvived);
            int populationSouls = FloorSaturating(
                (double)safePeakPopulation * settings.SoulsPerPeakPopulation);
            int recordSouls = newRecord
                ? FloorSaturating((double)safeDay * settings.NewRecordSoulsPerDay)
                : 0;

            quote = new MetaRewardQuote
            {
                Day = safeDay,
                Kills = safeKills,
                NightsSurvived = nightsSurvived,
                PeakPopulation = safePeakPopulation,
                PreviousBestDay = safePreviousBestDay,
                KillSouls = killSouls,
                DaySouls = daySouls,
                NightSouls = nightSouls,
                PopulationSouls = populationSouls,
                RecordSouls = recordSouls,
                TotalSouls = SaturatingSum(
                    killSouls, daySouls, nightSouls, populationSouls, recordSouls),
                NewRecord = newRecord
            };
            return true;
        }

        /// <summary>
        /// V1 death receipt migration yolu. Eski receipt tuning snapshot'i tasimadigi icin
        /// yayinlanmis eski 1 kill + yeni record day x 50 sozlesmesiyle tamamlanir.
        /// </summary>
        public static MetaRewardQuote CalculateLegacy(
            int day,
            int kills,
            int previousBestDay)
        {
            int safeDay = Math.Max(0, day);
            int safeKills = Math.Max(0, kills);
            int safePreviousBestDay = Math.Max(0, previousBestDay);
            bool newRecord = safeDay > safePreviousBestDay;
            int recordSouls = newRecord
                ? SaturatingMultiply(safeDay, LegacyRecordBonusPerDay)
                : 0;

            return new MetaRewardQuote
            {
                Day = safeDay,
                Kills = safeKills,
                NightsSurvived = Math.Max(0, safeDay - 1),
                PeakPopulation = 0,
                PreviousBestDay = safePreviousBestDay,
                KillSouls = safeKills,
                RecordSouls = recordSouls,
                TotalSouls = SaturatingSum(safeKills, recordSouls),
                NewRecord = newRecord
            };
        }

        public static bool IsStructurallyValid(MetaRewardQuote quote)
        {
            if (quote.Day < 0
                || quote.Kills < 0
                || quote.NightsSurvived != Math.Max(0, quote.Day - 1)
                || quote.PeakPopulation < 0
                || quote.PreviousBestDay < 0
                || quote.KillSouls < 0
                || quote.DaySouls < 0
                || quote.NightSouls < 0
                || quote.PopulationSouls < 0
                || quote.RecordSouls < 0
                || quote.TotalSouls < 0
                || quote.NewRecord != (quote.Day > quote.PreviousBestDay))
            {
                return false;
            }

            return quote.TotalSouls == SaturatingSum(
                quote.KillSouls,
                quote.DaySouls,
                quote.NightSouls,
                quote.PopulationSouls,
                quote.RecordSouls);
        }

        private static int FloorSaturating(double value)
        {
            if (double.IsNaN(value) || value <= 0d)
                return 0;
            if (double.IsInfinity(value) || value >= int.MaxValue)
                return int.MaxValue;
            return Math.Max(0, (int)Math.Floor(value));
        }

        private static int SaturatingMultiply(int left, int right)
        {
            long product = (long)Math.Max(0, left) * Math.Max(0, right);
            return product >= int.MaxValue ? int.MaxValue : (int)product;
        }

        private static int SaturatingSum(params int[] values)
        {
            long sum = 0L;
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    sum += Math.Max(0, values[i]);
                    if (sum >= int.MaxValue)
                        return int.MaxValue;
                }
            }

            return (int)sum;
        }
    }
}
