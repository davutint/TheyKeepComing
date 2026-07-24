# Game Center Integration

## Runtime Ownership

`GameCenterService` is the single runtime owner for Apple Game Center authentication.
It initializes before the first scene, subscribes to the package authentication callbacks,
and calls `GKLocalPlayer.Authenticate()` once on iOS Player startup.

The service is intentionally scene-independent. It does not require a prefab or a manually
placed GameObject and survives scene changes through static ownership.

## Platform Boundary

- Native GameKit calls compile only for `UNITY_IOS && !UNITY_EDITOR`.
- Unity Editor and non-iOS players use a no-op implementation.
- Game Center authentication must be verified in an installed iOS build, not in Play Mode.
- Authentication failure never blocks the game from starting.

## Public Contract

- `GameCenterService.IsAvailable`: GameKit runtime support is active for this player.
- `GameCenterService.IsAuthenticated`: the local player is authenticated.
- `GameCenterService.PlayerDisplayName`: authenticated player's Game Center display name.
- `GameCenterService.AuthenticateAsync()`: manually retries authentication.
- `GameCenterService.ShowDashboardAsync()`: presents Apple's native Game Center dashboard.
- `AuthenticationChanged` and `AuthenticationFailed`: optional UI integration hooks.

No player-facing dashboard button is bound yet. A future UI owner can call
`ShowDashboardAsync()` without owning native GameKit lifecycle logic.

## Build Configuration

Apple Core `3.2.0` and Apple GameKit `4.0.1` tarballs are versioned under `Packages/`.
The project manifest references them with paths relative to the `Packages` folder so the same
package setup resolves on macOS and Windows without machine-specific download paths.

The installed `Apple.GameKit` build step is enabled in
`Assets/Apple Plug-In Support/Editor/DefaultAppleBuildProfile.asset`.
For iOS builds it:

- adds the `com.apple.developer.game-center` entitlement;
- links `GameKit.framework`;
- embeds the Apple GameKit wrapper library.

App Store Connect uses the explicit bundle identifier `com.pixicorp.zombiecastle`, and
Game Center is enabled for iOS version `1.0`.

## Device Verification

1. Generate the iOS Xcode project from Unity.
2. Verify the target has the Game Center capability and uses the expected bundle identifier.
3. Sign in to Game Center on the test iPhone.
4. Install and launch the build.
5. Confirm the native authentication presentation or welcome banner appears.
6. Confirm the Unity device log contains `[GameCenter] Authenticated as ...`.

Leaderboards and achievements are not configured by this authentication-only integration.
Their App Store Connect identifiers must be decided before reporting progress from gameplay.
