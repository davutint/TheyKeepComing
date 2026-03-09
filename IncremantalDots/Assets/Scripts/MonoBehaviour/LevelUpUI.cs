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

        [Header("Button Text")]
        public TMP_Text AddArcherText;
        public TMP_Text ArrowDamageText;
        public TMP_Text RepairGateText;

        public TMP_Text TitleText;

        private void OnEnable()
        {
            if (TitleText != null)
                TitleText.text = $"Level {GameManager.Instance.GameState.Level + 1}!";

            if (AddArcherText != null)
                AddArcherText.text = "Okcu Ekle\n+1 Okcu";

            if (ArrowDamageText != null)
                ArrowDamageText.text = "Ok Hasari\n+5 Hasar";

            if (RepairGateText != null)
                RepairGateText.text = "Kapi Tamir\nTam HP";

            AddArcherButton.onClick.RemoveAllListeners();
            ArrowDamageButton.onClick.RemoveAllListeners();
            RepairGateButton.onClick.RemoveAllListeners();

            AddArcherButton.onClick.AddListener(() => SelectUpgrade(UpgradeType.AddArcher));
            ArrowDamageButton.onClick.AddListener(() => SelectUpgrade(UpgradeType.ArrowDamageUp));
            RepairGateButton.onClick.AddListener(() => SelectUpgrade(UpgradeType.RepairGate));
        }

        private void SelectUpgrade(UpgradeType type)
        {
            GameManager.Instance.ApplyUpgrade(type);
            UIManager.Instance.HideLevelUp();
        }
    }
}
