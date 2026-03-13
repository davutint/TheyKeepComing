using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeadWalls
{
    /// <summary>
    /// Kale yukseltme UI — tek buton.
    /// Kaynak yeterliligi ve maks seviye kontrolu yapar.
    /// </summary>
    public class CastleUpgradeUI : MonoBehaviour
    {
        [Header("UI")]
        public Button UpgradeButton;
        public TMP_Text ButtonText;

        private void Update()
        {
            if (UpgradeButton == null || ButtonText == null) return;

            var gm = GameManager.Instance;
            if (gm == null) return;

            // CastleUpgradeData oku (GameManager uzerinden)
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(typeof(CastleUpgradeData));
            if (query.IsEmpty) return;

            var castleEntity = query.GetSingletonEntity();
            var upgrade = em.GetComponentData<CastleUpgradeData>(castleEntity);

            // Maks seviye kontrolu
            if (upgrade.Level >= upgrade.MaxLevel)
            {
                ButtonText.text = "MAKS SEViYE";
                UpgradeButton.interactable = false;
                return;
            }

            // Kaynak yeterliligi kontrolu
            var resources = gm.Resources;
            bool canAfford = resources.Wood >= upgrade.WoodCostPerLevel
                          && resources.Stone >= upgrade.StoneCostPerLevel;

            ButtonText.text = $"Kale Yukselt (Lv.{upgrade.Level + 1}) — {upgrade.WoodCostPerLevel}A {upgrade.StoneCostPerLevel}T";
            UpgradeButton.interactable = canAfford;
        }

        public void OnUpgradeClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.UpgradeCastle();
        }

        private void OnEnable()
        {
            if (UpgradeButton != null)
                UpgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

        private void OnDisable()
        {
            if (UpgradeButton != null)
                UpgradeButton.onClick.RemoveListener(OnUpgradeClicked);
        }
    }
}
