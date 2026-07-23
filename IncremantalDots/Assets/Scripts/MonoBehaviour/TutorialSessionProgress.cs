using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Aktif development boyunca tutorial ilerlemesinin tek sahibidir. Bilgi yalnizca
    /// mevcut Play oturumunda yasar; save dosyasini okumaz veya yazmaz.
    /// </summary>
    public static class TutorialSessionProgress
    {
        private static readonly HashSet<string> CompletedFlags =
            new HashSet<string>(StringComparer.Ordinal);

        public static int CompletedFlagCount => CompletedFlags.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void BeginNewPlaySession()
        {
            CompletedFlags.Clear();
        }

        public static bool HasFlag(string flagId)
        {
            string id = Normalize(flagId);
            return !string.IsNullOrEmpty(id) && CompletedFlags.Contains(id);
        }

        public static bool SetFlag(string flagId, bool completed)
        {
            string id = Normalize(flagId);
            if (string.IsNullOrEmpty(id))
                return false;

            if (completed)
                CompletedFlags.Add(id);
            else
                CompletedFlags.Remove(id);
            return true;
        }

        public static bool ResetFlags(IEnumerable<string> flagIds)
        {
            if (flagIds == null)
                return false;

            bool foundValidId = false;
            foreach (string flagId in flagIds)
            {
                string id = Normalize(flagId);
                if (string.IsNullOrEmpty(id))
                    continue;

                foundValidId = true;
                CompletedFlags.Remove(id);
            }

            return foundValidId;
        }

        private static string Normalize(string flagId)
        {
            return flagId?.Trim();
        }
    }
}
