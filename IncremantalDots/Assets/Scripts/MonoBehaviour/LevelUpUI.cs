using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    public class LevelUpUI : MonoBehaviour
    {
        [Header("Buttons")]
        public Button AddArcherButton;
        public Button ArrowDamageButton;
        public Button RepairGateButton;
        public Button[] CardButtons;

        [Header("Button Text")]
        public TMP_Text AddArcherText;
        public TMP_Text ArrowDamageText;
        public TMP_Text RepairGateText;
        public TMP_Text[] CardTexts;

        public TMP_Text TitleText;

        private void OnEnable()
        {
            RefreshState();
        }

        private void RefreshState()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null)
                return;

            if (TitleText != null)
                TitleText.text = $"Level {gameManager.GameState.Level + 1}!";

            var cards = gameManager.GetCurrentUpgradeCards();
            for (int i = 0; i < 3; i++)
            {
                var button = GetCardButton(i);
                var text = GetCardText(i);
                if (button == null)
                    continue;

                button.onClick.RemoveAllListeners();
                bool hasCard = cards != null && i < cards.Length;
                button.gameObject.SetActive(hasCard);
                if (!hasCard)
                    continue;

                var card = cards[i];
                bool canApply = gameManager.CanApplyUpgrade(card.Type);
                button.interactable = canApply;

                if (text != null)
                    text.text = $"{card.Title}\n{card.Description}\nTier {card.Tier}";

                var capturedType = card.Type;
                button.onClick.AddListener(() => SelectUpgrade(capturedType));
            }
        }

        private Button GetCardButton(int index)
        {
            if (CardButtons != null && index < CardButtons.Length && CardButtons[index] != null)
                return CardButtons[index];

            switch (index)
            {
                case 0: return AddArcherButton;
                case 1: return ArrowDamageButton;
                case 2: return RepairGateButton;
                default: return null;
            }
        }

        private TMP_Text GetCardText(int index)
        {
            if (CardTexts != null && index < CardTexts.Length && CardTexts[index] != null)
                return CardTexts[index];

            switch (index)
            {
                case 0: return AddArcherText;
                case 1: return ArrowDamageText;
                case 2: return RepairGateText;
                default: return null;
            }
        }

        private void SelectUpgrade(UpgradeType type)
        {
            if (GameManager.Instance != null && GameManager.Instance.ApplyUpgrade(type))
            {
                if (UIManager.Instance != null)
                    UIManager.Instance.HideLevelUp();
                return;
            }

            RefreshState();
        }
    }
}
