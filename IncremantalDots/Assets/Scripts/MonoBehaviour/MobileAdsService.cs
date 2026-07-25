using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// UMP riza akisini tamamlar ve reklam istegi guvenli oldugunda
    /// Google Mobile Ads SDK'sini bir kez baslatir.
    /// </summary>
    public static class MobileAdsService
    {
        public static bool IsAvailable { get; private set; }
        public static bool IsConsentFlowComplete { get; private set; }
        public static bool CanRequestAds { get; private set; }
        public static bool IsInitialized { get; private set; }
        public static bool IsPrivacyOptionsRequired { get; private set; }
        public static string LastConsentError { get; private set; } = string.Empty;

        private static bool _startupRequested;

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        private static bool _sdkInitializationRequested;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewPlayerLoop()
        {
            IsAvailable = false;
            IsConsentFlowComplete = false;
            CanRequestAds = false;
            IsInitialized = false;
            IsPrivacyOptionsRequired = false;
            LastConsentError = string.Empty;

            _startupRequested = false;

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            _sdkInitializationRequested = false;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_startupRequested)
                return;

            _startupRequested = true;

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            IsAvailable = true;

#if UNITY_IOS
            MobileAds.SetiOSAppPauseOnBackground(true);
#endif

            GatherConsent();
#else
            IsAvailable = false;
            IsConsentFlowComplete = true;
#endif
        }

        /// <summary>
        /// Oyuncunun UMP gizlilik seceneklerini yeniden acmasini saglar.
        /// UI, bu metodu yalnizca IsPrivacyOptionsRequired true iken gostermelidir.
        /// </summary>
        public static void ShowPrivacyOptions()
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (!IsAvailable)
                return;

            ConsentForm.ShowPrivacyOptionsForm(showError =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (showError != null)
                    {
                        ReportConsentError(
                            $"Privacy options form failed: {showError.Message}");
                    }

                    RefreshConsentState();
                    TryInitializeMobileAds();
                });
            });
#endif
        }

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        private static void GatherConsent()
        {
            var requestParameters = new ConsentRequestParameters
            {
                // Oyun 13 yas altindaki kullanicilara yonelik degildir.
                TagForUnderAgeOfConsent = false
            };

            ConsentInformation.Update(requestParameters, updateError =>
            {
                if (updateError != null)
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        ReportConsentError(
                            $"Consent information update failed: {updateError.Message}");
                        CompleteConsentFlow();
                    });
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(showError =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        if (showError != null)
                        {
                            ReportConsentError(
                                $"Consent form failed: {showError.Message}");
                        }

                        CompleteConsentFlow();
                    });
                });
            });
        }

        private static void CompleteConsentFlow()
        {
            IsConsentFlowComplete = true;
            RefreshConsentState();

            Debug.Log(
                $"[MobileAds] Consent flow complete. Status={ConsentInformation.ConsentStatus}, " +
                $"CanRequestAds={CanRequestAds}, " +
                $"PrivacyOptionsRequired={IsPrivacyOptionsRequired}.");

            TryInitializeMobileAds();
        }

        private static void RefreshConsentState()
        {
            CanRequestAds = ConsentInformation.CanRequestAds();
            IsPrivacyOptionsRequired =
                ConsentInformation.PrivacyOptionsRequirementStatus ==
                PrivacyOptionsRequirementStatus.Required;
        }

        private static void TryInitializeMobileAds()
        {
            if (!CanRequestAds || _sdkInitializationRequested)
                return;

            _sdkInitializationRequested = true;
            Debug.Log("[MobileAds] Initializing Google Mobile Ads SDK.");

            MobileAds.Initialize(initializationStatus =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    IsInitialized = initializationStatus != null;

                    if (IsInitialized)
                    {
                        Debug.Log("[MobileAds] Google Mobile Ads SDK initialized.");
                    }
                    else
                    {
                        _sdkInitializationRequested = false;
                        Debug.LogError("[MobileAds] Google Mobile Ads SDK initialization failed.");
                    }

                });
            });
        }

        private static void ReportConsentError(string message)
        {
            LastConsentError = message ?? string.Empty;
            Debug.LogWarning($"[MobileAds] {LastConsentError}");
        }
#endif
    }
}
