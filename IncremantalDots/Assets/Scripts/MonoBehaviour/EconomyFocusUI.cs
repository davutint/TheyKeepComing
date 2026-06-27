using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    public class EconomyFocusUI : MonoBehaviour
    {
        public TMP_Text EconomyFocusText;
        public Button EconomyBalancedButton;
        public Button EconomyWoodButton;
        public Button EconomyStoneButton;
        public Button EconomyIronButton;
        public Button EconomyFoodButton;

        [Header("Selected Frames")]
        public GameObject EconomyBalancedSelected;
        public GameObject EconomyWoodSelected;
        public GameObject EconomyStoneSelected;
        public GameObject EconomyIronSelected;
        public GameObject EconomyFoodSelected;

        private float _nextRefreshTime;

        private void OnEnable()
        {
            BindButtons();
            Refresh();
        }

        private void OnDisable()
        {
            UnbindButtons();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefreshTime)
                return;

            _nextRefreshTime = Time.unscaledTime + 0.20f;
            Refresh();
        }

        public void Refresh()
        {
            var gm = GameManager.Instance;
            bool available = gm != null && gm.CanUseEconomyFocus();
            EconomyFocusType focus = available ? gm.GetEconomyFocus() : EconomyFocusType.Balanced;

            if (EconomyFocusText != null)
                EconomyFocusText.text = "FOCUS: " + GetFocusLabel(focus);

            SetButtonLabel(EconomyBalancedButton, "BAL");
            SetButtonLabel(EconomyWoodButton, "WOOD");
            SetButtonLabel(EconomyStoneButton, "STONE");
            SetButtonLabel(EconomyIronButton, "IRON");
            SetButtonLabel(EconomyFoodButton, "FOOD");

            SetButtonInteractable(EconomyBalancedButton, available);
            SetButtonInteractable(EconomyWoodButton, available);
            SetButtonInteractable(EconomyStoneButton, available);
            SetButtonInteractable(EconomyIronButton, available);
            SetButtonInteractable(EconomyFoodButton, available);

            SetSelected(EconomyBalancedSelected, focus == EconomyFocusType.Balanced);
            SetSelected(EconomyWoodSelected, focus == EconomyFocusType.Wood);
            SetSelected(EconomyStoneSelected, focus == EconomyFocusType.Stone);
            SetSelected(EconomyIronSelected, focus == EconomyFocusType.Iron);
            SetSelected(EconomyFoodSelected, focus == EconomyFocusType.Food);
        }

        private void BindButtons()
        {
            UnbindButtons();
            EconomyBalancedButton?.onClick.AddListener(HandleBalancedClicked);
            EconomyWoodButton?.onClick.AddListener(HandleWoodClicked);
            EconomyStoneButton?.onClick.AddListener(HandleStoneClicked);
            EconomyIronButton?.onClick.AddListener(HandleIronClicked);
            EconomyFoodButton?.onClick.AddListener(HandleFoodClicked);
        }

        private void UnbindButtons()
        {
            EconomyBalancedButton?.onClick.RemoveListener(HandleBalancedClicked);
            EconomyWoodButton?.onClick.RemoveListener(HandleWoodClicked);
            EconomyStoneButton?.onClick.RemoveListener(HandleStoneClicked);
            EconomyIronButton?.onClick.RemoveListener(HandleIronClicked);
            EconomyFoodButton?.onClick.RemoveListener(HandleFoodClicked);
        }

        private void HandleBalancedClicked() => SetFocus(EconomyFocusType.Balanced);
        private void HandleWoodClicked() => SetFocus(EconomyFocusType.Wood);
        private void HandleStoneClicked() => SetFocus(EconomyFocusType.Stone);
        private void HandleIronClicked() => SetFocus(EconomyFocusType.Iron);
        private void HandleFoodClicked() => SetFocus(EconomyFocusType.Food);

        private void SetFocus(EconomyFocusType focus)
        {
            GameManager.Instance?.SetEconomyFocus(focus);
            Refresh();
        }

        private static void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }

        private static void SetButtonLabel(Button button, string value)
        {
            if (button == null)
                return;

            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.text = value;
        }

        private static void SetSelected(GameObject selectedObject, bool selected)
        {
            if (selectedObject != null && selectedObject.activeSelf != selected)
                selectedObject.SetActive(selected);
        }

        private static string GetFocusLabel(EconomyFocusType focus)
        {
            switch (EconomyFocusUtility.Normalize(focus))
            {
                case EconomyFocusType.Wood:
                    return "WOOD";
                case EconomyFocusType.Stone:
                    return "STONE";
                case EconomyFocusType.Iron:
                    return "IRON";
                case EconomyFocusType.Food:
                    return "FOOD";
                default:
                    return "BALANCED";
            }
        }
    }
}
