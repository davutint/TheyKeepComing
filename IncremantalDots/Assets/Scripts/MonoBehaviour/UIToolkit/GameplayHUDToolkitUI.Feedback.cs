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
            public float Elapsed;
            public float Duration;
        }

        private readonly List<SoulFlight> _soulFlights = new List<SoulFlight>();
        private readonly Stack<VisualElement> _soulFlightPool = new Stack<VisualElement>();
        private string _lastDawnToast = string.Empty;
        private string _lastNightToast = string.Empty;
        private string _lastWaveToast = string.Empty;

        private void RefreshFeedbackPresentation()
        {
            bool hintVisible = _onboardingLegacy != null
                               && _onboardingLegacy.HintPanel != null
                               && _onboardingLegacy.HintPanel.activeInHierarchy
                               && _onboardingLegacy.HintText != null
                               && !string.IsNullOrWhiteSpace(_onboardingLegacy.HintText.text);
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
            if (_root?.panel == null || _soulFlightLayer == null || pickup.Amount <= 0)
                return;

            Camera camera = Camera.main;
            Vector3 world = new Vector3(pickup.Position.x, pickup.Position.y + 0.45f, pickup.Position.z);
            Vector2 start = camera != null
                ? RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, world, camera)
                : new Vector2(_root.resolvedStyle.width * 0.5f, _root.resolvedStyle.height * 0.5f);
            Vector2 target = _soulAnchor != null
                ? _soulAnchor.worldBound.center
                : new Vector2(_root.resolvedStyle.width - 90f, 160f);
            float lateral = ((_soulFlights.Count % 5) - 2) * 18f;
            Vector2 control = (start + target) * 0.5f + Vector2.up * 120f + Vector2.right * lateral;

            VisualElement element = GetSoulFlightElement(out Label amountLabel);
            element.style.left = start.x - 9f;
            element.style.top = start.y - 9f;
            amountLabel.text = pickup.Amount > 1 ? $"+{pickup.Amount:N0}" : string.Empty;
            amountLabel.style.display = pickup.Amount > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            _soulFlightLayer.Add(element);
            _soulFlights.Add(new SoulFlight
            {
                Element = element,
                AmountLabel = amountLabel,
                Start = start,
                Control = control,
                Target = target,
                Elapsed = 0f,
                Duration = 0.82f
            });
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
                if (_soulAnchor != null)
                {
                    _soulAnchor.AddToClassList("is-arriving");
                    _soulAnchor.schedule.Execute(() => _soulAnchor.RemoveFromClassList("is-arriving")).StartingIn(180);
                }
            }
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
            element.style.opacity = 1f;
            element.style.scale = new Scale(Vector3.one);
            _soulFlightPool.Push(element);
        }

        private void ReleaseAllSoulFlights()
        {
            for (int i = _soulFlights.Count - 1; i >= 0; i--)
                ReleaseSoulFlightElement(_soulFlights[i].Element);
            _soulFlights.Clear();
        }
    }
}
