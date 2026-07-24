using System;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_IOS && !UNITY_EDITOR
using Apple.Core.Runtime;
using Apple.GameKit;
#endif

namespace DeadWalls
{
    /// <summary>
    /// iOS Player acilisinda Game Center kimlik dogrulamasini baslatir.
    /// Editor ve Apple disi platformlarda native API'ye dokunmadan no-op kalir.
    /// </summary>
    public static class GameCenterService
    {
        public static event Action<bool> AuthenticationChanged;
        public static event Action<string> AuthenticationFailed;

        public static bool IsAvailable { get; private set; }
        public static bool IsAuthenticated { get; private set; }
        public static string PlayerDisplayName { get; private set; } = string.Empty;

        private static bool _initialized;

#if UNITY_IOS && !UNITY_EDITOR
        private static bool _eventsHooked;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewPlayerLoop()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (_eventsHooked)
            {
                GKLocalPlayer.AuthenticateUpdate -= HandleAuthenticateUpdate;
                GKLocalPlayer.AuthenticateError -= HandleAuthenticateError;
                _eventsHooked = false;
            }
#endif

            AuthenticationChanged = null;
            AuthenticationFailed = null;
            IsAvailable = false;
            IsAuthenticated = false;
            PlayerDisplayName = string.Empty;
            _initialized = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;

#if UNITY_IOS && !UNITY_EDITOR
            IsAvailable = true;
            HookAuthenticationEvents();
            _ = AuthenticateOnApplePlatformAsync();
#else
            IsAvailable = false;
#endif
        }

        public static Task<bool> AuthenticateAsync()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (!_initialized)
                Initialize();

            return AuthenticateOnApplePlatformAsync();
#else
            return Task.FromResult(false);
#endif
        }

        public static Task<bool> ShowDashboardAsync()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return ShowDashboardOnApplePlatformAsync();
#else
            return Task.FromResult(false);
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        private static void HookAuthenticationEvents()
        {
            if (_eventsHooked)
                return;

            GKLocalPlayer.AuthenticateUpdate += HandleAuthenticateUpdate;
            GKLocalPlayer.AuthenticateError += HandleAuthenticateError;
            _eventsHooked = true;
        }

        private static async Task<bool> AuthenticateOnApplePlatformAsync()
        {
            try
            {
                GKLocalPlayer player = await GKLocalPlayer.Authenticate();
                ApplyPlayer(player);
                return IsAuthenticated;
            }
            catch (GameKitException)
            {
                // Native hata ayrintisi AuthenticateError callback'inde islenir.
                return false;
            }
            catch (Exception exception)
            {
                HandleUnexpectedException("authentication", exception);
                return false;
            }
        }

        private static async Task<bool> ShowDashboardOnApplePlatformAsync()
        {
            if (!IsAuthenticated && !await AuthenticateOnApplePlatformAsync())
                return false;

            GKGameCenterViewController controller = null;
            try
            {
                controller = GKGameCenterViewController.Init(
                    GKGameCenterViewControllerState.Dashboard);
                await controller.Present();
                return true;
            }
            catch (Exception exception)
            {
                HandleUnexpectedException("dashboard", exception);
                return false;
            }
            finally
            {
                controller?.Dispose();
            }
        }

        private static void HandleAuthenticateUpdate(GKLocalPlayer player)
        {
            ApplyPlayer(player);
        }

        private static void HandleAuthenticateError(NSError error)
        {
            ApplyPlayer(null);

            string description = error == null
                ? "Unknown Game Center authentication error."
                : $"Code={error.Code}, Domain={error.Domain}, Description={error.LocalizedDescription}";

            Debug.LogWarning($"[GameCenter] Authentication failed. {description}");
            AuthenticationFailed?.Invoke(description);
        }

        private static void ApplyPlayer(GKLocalPlayer player)
        {
            bool wasAuthenticated = IsAuthenticated;
            string previousDisplayName = PlayerDisplayName;

            IsAuthenticated = player != null && player.IsAuthenticated;
            PlayerDisplayName = IsAuthenticated ? player.DisplayName ?? string.Empty : string.Empty;

            if (wasAuthenticated == IsAuthenticated
                && string.Equals(previousDisplayName, PlayerDisplayName, StringComparison.Ordinal))
            {
                return;
            }

            if (IsAuthenticated)
                Debug.Log($"[GameCenter] Authenticated as {PlayerDisplayName}.");
            else if (wasAuthenticated)
                Debug.Log("[GameCenter] Player is no longer authenticated.");

            AuthenticationChanged?.Invoke(IsAuthenticated);
        }

        private static void HandleUnexpectedException(string operation, Exception exception)
        {
            string description = $"{operation}: {exception.Message}";
            Debug.LogWarning($"[GameCenter] {description}");
            AuthenticationFailed?.Invoke(description);
        }
#endif
    }
}
