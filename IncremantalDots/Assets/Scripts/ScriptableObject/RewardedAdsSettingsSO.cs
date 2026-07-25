using System;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Rewarded reklam kimliklerinin provider-bagimsiz proje ayari.
    /// App ID Google Mobile Ads paket ayarinda, Ad Unit ID'ler bu profilde tutulur.
    /// </summary>
    [CreateAssetMenu(
        fileName = "RewardedAdsSettings",
        menuName = "DeadWalls/Monetization/Rewarded Ads Settings")]
    public sealed class RewardedAdsSettingsSO : ScriptableObject
    {
        public const string DefaultResourcesKey = "RewardedAdsSettings";
        public const string DefaultAssetPath = "Assets/Resources/RewardedAdsSettings.asset";

        private static RewardedAdsSettingsSO _cachedDefault;

        [Header("Production Rewarded Ad Unit IDs")]
        [Tooltip("AdMob iOS rewarded Ad Unit ID. ca-app-pub-.../... biciminde olmali.")]
        [SerializeField] private string iOSRewardedAdUnitId = string.Empty;

        [Tooltip("AdMob Android rewarded Ad Unit ID. Android kurulumu yapilana kadar bos kalabilir.")]
        [SerializeField] private string androidRewardedAdUnitId = string.Empty;

        [Header("Development Safety")]
        [Tooltip("Editor ve Development Build'lerde gercek reklam yerine Google test reklamlarini kullanir.")]
        [SerializeField] private bool useTestAdsInDevelopment = true;

        public string IOSRewardedAdUnitId => Normalize(iOSRewardedAdUnitId);
        public string AndroidRewardedAdUnitId => Normalize(androidRewardedAdUnitId);
        public bool UseTestAdsInDevelopment => useTestAdsInDevelopment;

        public bool HasValidIOSRewardedAdUnitId =>
            LooksLikeAdUnitId(IOSRewardedAdUnitId);

        public bool HasValidAndroidRewardedAdUnitId =>
            LooksLikeAdUnitId(AndroidRewardedAdUnitId);

        public static RewardedAdsSettingsSO LoadDefault()
        {
            if (_cachedDefault == null)
                _cachedDefault = Resources.Load<RewardedAdsSettingsSO>(DefaultResourcesKey);
            return _cachedDefault;
        }

        public static void ResetDefaultCache()
        {
            _cachedDefault = null;
        }

        private static bool LooksLikeAdUnitId(string value)
        {
            return value.StartsWith("ca-app-pub-", StringComparison.Ordinal)
                   && value.Contains("/");
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
