using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Dead Walls ses kimliginin tek merkezi profili. Runtime owner'lar kendi eski
    /// serialized clip'lerini fallback olarak korur; ilgili override aciksa bu profil
    /// kullanilir. Profil Resources altinda yasadigi icin scene/prefab kopyasi gerekmez.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DeadWallsAudioProfile",
        menuName = "DeadWalls/Audio/Audio Profile")]
    public sealed class DeadWallsAudioProfileSO : ScriptableObject
    {
        public const string DefaultResourcesKey = "DeadWallsAudioProfile";
        public const string DefaultAssetPath = "Assets/Resources/DeadWallsAudioProfile.asset";

        private static DeadWallsAudioProfileSO _cachedDefault;

        [Header("Runtime Routing")]
        public bool OverrideCombat = true;
        public bool OverrideInterface = true;
        public bool OverrideCastleHeart = true;
        public bool OverrideAmbience;
        public bool OverrideMenuMusic;
        public bool EnableCurrencyArrival = true;

        [Header("Combat")]
        public AudioClip[] ArrowShootClips;
        public AudioClip ArrowHitClip;
        public AudioClip FrostHitClip;
        public AudioClip[] WallHitClips;
        public AudioClip FireballBlastClip;

        [Header("Ability Audition Candidates - Not Routed Yet")]
        public AudioClip FireballCastCandidate;
        public AudioClip FireballBurnTailCandidate;
        public AudioClip EmergencyRepairCandidate;
        public AudioClip RallyCandidate;

        [Header("Interface")]
        public AudioClip UiClickClip;
        public AudioClip UiSuccessClip;
        public AudioClip UiFailClip;
        public AudioClip DeathStingClip;
        [Range(0f, 1f)] public float UiClickVolume = 0.30f;
        [Range(0f, 1f)] public float UiSuccessVolume = 0.44f;
        [Range(0f, 1f)] public float UiFailVolume = 0.38f;
        [Range(0f, 1f)] public float DeathStingVolume = 0.70f;

        [Header("Castle Heart")]
        public AudioClip HeartResearchClip;
        public AudioClip HeartRevealClip;
        public AudioClip HeartDeniedClip;
        public AudioClip HeartPanelOpenClip;
        [Range(0f, 1f)] public float HeartResearchVolume = 0.66f;
        [Range(0f, 1f)] public float HeartRevealVolume = 0.58f;
        [Range(0f, 1f)] public float HeartDeniedVolume = 0.62f;
        [Range(0f, 1f)] public float HeartPanelOpenVolume = 0.50f;

        [Header("Currency Arrival")]
        public AudioClip SoulArrivalClip;
        public AudioClip EssenceArrivalClip;
        [Range(0f, 1f)] public float SoulArrivalVolume = 0.22f;
        [Range(0f, 1f)] public float EssenceArrivalVolume = 0.30f;
        [Min(0.02f)] public float CurrencyArrivalMinInterval = 0.12f;
        [Range(0f, 0.25f)] public float CurrencyAmountVolumeGain = 0.055f;
        [Range(0f, 0.08f)] public float CurrencyAmountPitchGain = 0.012f;

        [Header("Ambience")]
        public AudioClip NightLoop;
        public AudioClip DuskRiser;
        public AudioClip DawnCue;
        public AudioClip NightHordeLoop;
        public AudioClip[] WorkerFoleyClips;

        [Header("Music Audition Candidates - Not Routed Unless Enabled")]
        public AudioClip MenuMusic;
        public AudioClip DayMusicCandidate;
        public AudioClip NightMusicCandidate;
        public AudioClip IntenseNightMusicCandidate;

        public static DeadWallsAudioProfileSO LoadDefault()
        {
            if (_cachedDefault == null)
                _cachedDefault = Resources.Load<DeadWallsAudioProfileSO>(DefaultResourcesKey);
            return _cachedDefault;
        }

        public static void ResetDefaultCache()
        {
            _cachedDefault = null;
        }
    }

    /// <summary>
    /// Ayni frame/pencere icinde toplanmis currency miktarini kontrollu bir ses
    /// siddeti ve pitch'e cevirir. Miktar lineer degil logaritmik etki eder; 10K
    /// kill burst'u tek bir asiri yuksek sese donusmez.
    /// </summary>
    public static class CurrencyArrivalAudioPolicy
    {
        public static float ResolveVolume(int amount, float baseVolume, float amountGain)
        {
            if (amount <= 0 || baseVolume <= 0f)
                return 0f;

            float density = Mathf.Log(Mathf.Max(1, amount), 2f);
            return Mathf.Clamp01(
                Mathf.Clamp01(baseVolume) * (1f + density * Mathf.Max(0f, amountGain)));
        }

        public static float ResolvePitch(int amount, float amountGain)
        {
            if (amount <= 0)
                return 1f;

            float density = Mathf.Log(Mathf.Max(1, amount), 2f);
            return Mathf.Clamp(1f + density * Mathf.Max(0f, amountGain), 0.85f, 1.16f);
        }
    }
}
