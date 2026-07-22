using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeadWalls
{
    public sealed partial class GameplayHUDToolkitUI
    {
        private sealed class SoulFlight
        {
            public VisualElement Element;
            public Label AmountLabel;
            public Vector2 Start;
            public Vector2 Control;
            public Vector2 Target;
            public VisualElement TargetAnchor;
            public int Amount;
            public bool IsEssence;
            public float Elapsed;
            public float Duration;
        }

        private readonly List<SoulFlight> _soulFlights = new List<SoulFlight>();
        private readonly Stack<VisualElement> _soulFlightPool = new Stack<VisualElement>();
        private string _lastDawnToast = string.Empty;
        private string _lastNightToast = string.Empty;
        private string _lastWaveToast = string.Empty;

        public long TotalGraveEssenceFlightsStartedCount { get; private set; }
        public long TotalCurrencyArrivalSfxPlayedCount { get; private set; }
        public int LastCurrencyArrivalSfxAmount { get; private set; }

        private AudioSource _soulArrivalAudioSource;
        private AudioSource _essenceArrivalAudioSource;
        private int _pendingSoulArrivalAmount;
        private int _pendingEssenceArrivalAmount;
        private float _nextSoulArrivalSfxAt;
        private float _nextEssenceArrivalSfxAt;

        private void RefreshFeedbackPresentation()
        {
            bool hasLegacyHint = _onboardingLegacy != null
                                 && _onboardingLegacy.HintPanel != null
                                 && _onboardingLegacy.HintPanel.activeInHierarchy
                                 && _onboardingLegacy.HintText != null
                                 && !string.IsNullOrWhiteSpace(_onboardingLegacy.HintText.text);
            bool hintVisible = ShouldMirrorLegacyOnboardingHint(
                hasLegacyHint,
                _onboardingLegacy != null && _onboardingLegacy.IsBasicArcherStepVisible,
                HasGuidedOnboardingPresentationOwner);
            _onboardingHint.EnableInClassList("is-visible", hintVisible);
            if (hintVisible)
            {
                string hint = _onboardingLegacy.HintText.text;
                if (_onboardingLegacy.IsNightAbilityKeyStepVisible
                    && _inputMode != null
                    && _inputMode.CurrentMode != UIInputMode.Pointer)
                {
                    hint = "USE A READY COMBAT ABILITY.";
                }

                _onboardingHintText.text = hint;
            }

            MirrorToast(_dawnToastLegacy != null ? _dawnToastLegacy.ToastText : null,
                ref _lastDawnToast, true);
            MirrorToast(_councilLegacy != null ? _councilLegacy.NightToastText : null,
                ref _lastNightToast, false);
            MirrorToast(_legacyHud != null ? _legacyHud.WaveRewardText : null,
                ref _lastWaveToast, true);
        }

        internal static bool ShouldMirrorLegacyOnboardingHint(
            bool hasLegacyHint,
            bool isBasicArcherAffordabilityStep,
            bool hasGuidedOnboardingOwner)
        {
            return hasLegacyHint
                && !isBasicArcherAffordabilityStep
                && !hasGuidedOnboardingOwner;
        }

        private void MirrorToast(TMPro.TMP_Text source, ref string cache, bool primary)
        {
            if (source == null || !source.gameObject.activeInHierarchy || source.alpha <= 0.05f)
                return;
            string value = source.text;
            if (string.IsNullOrWhiteSpace(value) || value == cache)
                return;
            cache = value;
            if (primary)
                ShowPrimaryToast(value);
            else
                ShowSecondaryToast(value);
        }

        private void HandleSoulPickupRequested(SoulPickupEvent pickup)
        {
            StartCurrencyFlight(
                new Vector3(pickup.Position.x, pickup.Position.y + 0.45f, pickup.Position.z),
                pickup.Amount,
                _soulAnchor,
                false);
        }

        private void HandleGraveEssenceDropped(GraveEssenceDropEvent drop)
        {
            if (StartCurrencyFlight(
                    new Vector3(drop.Position.x, drop.Position.y + 0.45f, drop.Position.z),
                    drop.Amount,
                    _graveEssenceAnchor,
                    true))
            {
                TotalGraveEssenceFlightsStartedCount++;
            }
        }

        private bool StartCurrencyFlight(
            Vector3 world,
            int amount,
            VisualElement targetAnchor,
            bool isEssence)
        {
            if (_root?.panel == null || _soulFlightLayer == null || amount <= 0)
                return false;

            if (targetAnchor == null)
                return false;

            Camera camera = Camera.main;
            Vector2 start = camera != null
                ? RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, world, camera)
                : new Vector2(_root.resolvedStyle.width * 0.5f, _root.resolvedStyle.height * 0.5f);
            Vector2 target = targetAnchor.worldBound.center;
            float lateral = ((_soulFlights.Count % 5) - 2) * 18f;
            Vector2 control = (start + target) * 0.5f + Vector2.up * 120f + Vector2.right * lateral;

            VisualElement element = GetSoulFlightElement(out Label amountLabel);
            element.EnableInClassList("soul-flight--essence", isEssence);
            element.style.left = start.x - 9f;
            element.style.top = start.y - 9f;
            amountLabel.text = amount > 1 ? $"+{amount:N0}" : string.Empty;
            amountLabel.style.display = amount > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            _soulFlightLayer.Add(element);
            _soulFlights.Add(new SoulFlight
            {
                Element = element,
                AmountLabel = amountLabel,
                Start = start,
                Control = control,
                Target = target,
                TargetAnchor = targetAnchor,
                Amount = amount,
                IsEssence = isEssence,
                Elapsed = 0f,
                Duration = 0.82f
            });
            return true;
        }

        private void UpdateSoulFlights(float deltaTime)
        {
            for (int i = _soulFlights.Count - 1; i >= 0; i--)
            {
                SoulFlight flight = _soulFlights[i];
                flight.Elapsed += deltaTime;
                float t = Mathf.Clamp01(flight.Elapsed / Mathf.Max(0.01f, flight.Duration));
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                Vector2 a = Vector2.Lerp(flight.Start, flight.Control, eased);
                Vector2 b = Vector2.Lerp(flight.Control, flight.Target, eased);
                Vector2 position = Vector2.Lerp(a, b, eased);
                flight.Element.style.left = position.x - 9f;
                flight.Element.style.top = position.y - 9f;
                flight.Element.style.opacity = 1f - Mathf.Clamp01((t - 0.78f) / 0.22f);
                float scale = Mathf.Lerp(0.65f, 1.15f, Mathf.Sin(t * Mathf.PI));
                flight.Element.style.scale = new Scale(new Vector3(scale, scale, 1f));

                if (t < 1f)
                    continue;
                ReleaseSoulFlightElement(flight.Element);
                _soulFlights.RemoveAt(i);
                if (flight.TargetAnchor != null)
                {
                    VisualElement arrivalAnchor = flight.TargetAnchor;
                    arrivalAnchor.AddToClassList("is-arriving");
                    arrivalAnchor.schedule.Execute(
                        () => arrivalAnchor.RemoveFromClassList("is-arriving")).StartingIn(180);
                }
                QueueCurrencyArrival(flight.IsEssence, flight.Amount);
            }

            FlushCurrencyArrivalAudio();
        }

        private void QueueCurrencyArrival(bool isEssence, int amount)
        {
            if (amount <= 0)
                return;

            if (isEssence)
                _pendingEssenceArrivalAmount = SaturatingAdd(_pendingEssenceArrivalAmount, amount);
            else
                _pendingSoulArrivalAmount = SaturatingAdd(_pendingSoulArrivalAmount, amount);
        }

        private void FlushCurrencyArrivalAudio()
        {
            DeadWallsAudioProfileSO profile = DeadWallsAudioProfileSO.LoadDefault();
            if (profile == null || !profile.EnableCurrencyArrival)
            {
                _pendingSoulArrivalAmount = 0;
                _pendingEssenceArrivalAmount = 0;
                return;
            }

            float now = Time.unscaledTime;
            TryPlayCurrencyArrival(
                profile.EssenceArrivalClip,
                ref _pendingEssenceArrivalAmount,
                profile.EssenceArrivalVolume,
                profile,
                now,
                ref _nextEssenceArrivalSfxAt,
                ref _essenceArrivalAudioSource,
                "EssenceArrivalAudio");
            TryPlayCurrencyArrival(
                profile.SoulArrivalClip,
                ref _pendingSoulArrivalAmount,
                profile.SoulArrivalVolume,
                profile,
                now,
                ref _nextSoulArrivalSfxAt,
                ref _soulArrivalAudioSource,
                "SoulArrivalAudio");
        }

        private void TryPlayCurrencyArrival(
            AudioClip clip,
            ref int pendingAmount,
            float baseVolume,
            DeadWallsAudioProfileSO profile,
            float now,
            ref float nextAllowedTime,
            ref AudioSource source,
            string sourceName)
        {
            if (clip == null || pendingAmount <= 0 || now < nextAllowedTime)
                return;

            int amount = pendingAmount;
            pendingAmount = 0;
            nextAllowedTime = now + Mathf.Max(0.02f, profile.CurrencyArrivalMinInterval);

            if (source == null)
            {
                var sourceObject = new GameObject(sourceName);
                sourceObject.transform.SetParent(transform, false);
                source = sourceObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
            }

            source.pitch = CurrencyArrivalAudioPolicy.ResolvePitch(
                amount,
                profile.CurrencyAmountPitchGain);
            float volume = CurrencyArrivalAudioPolicy.ResolveVolume(
                amount,
                baseVolume,
                profile.CurrencyAmountVolumeGain);
            source.PlayOneShot(clip, volume * SoundSettings.SfxVolume);
            TotalCurrencyArrivalSfxPlayedCount++;
            LastCurrencyArrivalSfxAmount = amount;
        }

        private static int SaturatingAdd(int current, int amount)
        {
            long total = (long)Mathf.Max(0, current) + Mathf.Max(0, amount);
            return total >= int.MaxValue ? int.MaxValue : (int)total;
        }

        private VisualElement GetSoulFlightElement(out Label amountLabel)
        {
            VisualElement element;
            if (_soulFlightPool.Count > 0)
            {
                element = _soulFlightPool.Pop();
                amountLabel = element.Q<Label>("amount");
                return element;
            }

            element = new VisualElement();
            element.AddToClassList("soul-flight");
            amountLabel = new Label { name = "amount", pickingMode = PickingMode.Ignore };
            amountLabel.AddToClassList("soul-flight__amount");
            element.Add(amountLabel);
            return element;
        }

        private void ReleaseSoulFlightElement(VisualElement element)
        {
            if (element == null)
                return;

            element.RemoveFromHierarchy();
            element.RemoveFromClassList("soul-flight--essence");
            element.style.opacity = 1f;
            element.style.scale = new Scale(Vector3.one);
            _soulFlightPool.Push(element);
        }

        private void ReleaseAllSoulFlights()
        {
            for (int i = _soulFlights.Count - 1; i >= 0; i--)
                ReleaseSoulFlightElement(_soulFlights[i].Element);
            _soulFlights.Clear();
            _pendingSoulArrivalAmount = 0;
            _pendingEssenceArrivalAmount = 0;
            if (_soulArrivalAudioSource != null)
                _soulArrivalAudioSource.Stop();
            if (_essenceArrivalAudioSource != null)
                _essenceArrivalAudioSource.Stop();
        }
    }
}
