using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// V1 okcu formasyonunun exact outside tile setini ve lokal dagilim tuning'ini tutar.
    /// Runtime okcu sayisi veya pozisyon state'i bu asset'te tutulmaz.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ArcherFormationV1",
        menuName = "DeadWalls/Mobile Castle/Archer Formation Definition")]
    public sealed class ArcherFormationDefinitionSO : ScriptableObject
    {
        [Header("Versioned Layout")]
        [Min(1)] public int Version = ArcherFormationUtility.CurrentVersion;
        public Vector3Int[] TileCoordinates = ArcherFormationUtility.CreateCanonicalV1TileCoordinates();

        [Header("Local Slots")]
        [Range(0f, 0.95f)] public float SafeInset = ArcherFormationUtility.DefaultSafeInset;
        [Min(0f)] public float MinimumLocalDistance = ArcherFormationUtility.DefaultMinimumLocalDistance;
        [Min(1)] public int CandidateAttempts = ArcherFormationUtility.DefaultCandidateAttempts;

        public void ApplyV1Defaults()
        {
            Version = ArcherFormationUtility.CurrentVersion;
            TileCoordinates = ArcherFormationUtility.CreateCanonicalV1TileCoordinates();
            SafeInset = ArcherFormationUtility.DefaultSafeInset;
            MinimumLocalDistance = ArcherFormationUtility.DefaultMinimumLocalDistance;
            CandidateAttempts = ArcherFormationUtility.DefaultCandidateAttempts;
        }

        public bool ValidateV1(out string problem)
        {
            if (Version != ArcherFormationUtility.CurrentVersion)
            {
                problem = $"Formation version {Version} desteklenmiyor.";
                return false;
            }

            if (!ArcherFormationUtility.MatchesCanonicalV1(TileCoordinates))
            {
                problem = "V1 exact 40 outside tile koordinat sirasi bozuk.";
                return false;
            }

            if (SafeInset < 0f || SafeInset >= 0.95f)
            {
                problem = "SafeInset [0, 0.95) araliginda olmali.";
                return false;
            }

            if (MinimumLocalDistance < 0f || CandidateAttempts < 1)
            {
                problem = "Local slot tuning degerleri gecersiz.";
                return false;
            }

            problem = string.Empty;
            return true;
        }

        private void OnValidate()
        {
            Version = Mathf.Max(1, Version);
            SafeInset = Mathf.Clamp(SafeInset, 0f, 0.94f);
            MinimumLocalDistance = Mathf.Max(0f, MinimumLocalDistance);
            CandidateAttempts = Mathf.Max(1, CandidateAttempts);
        }
    }
}
