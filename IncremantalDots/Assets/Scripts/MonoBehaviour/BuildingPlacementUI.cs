using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// Bina secim menusu + ghost preview + tiklama yerlestirme.
    /// Akis: Panel ac → bina sec → ghost takip → tikla yerlestir / sag tikla iptal.
    /// </summary>
    public class BuildingPlacementUI : MonoBehaviour
    {
        [Header("UI")]
        public GameObject BuildingButtonPanel;   // Bina butonlarini iceren panel
        public Transform ButtonContainer;        // Butonlarin parent'i
        public GameObject BuildingButtonPrefab;  // Her bina icin instantiate edilecek buton

        [Header("Ghost Preview")]
        public SpriteRenderer GhostRenderer;     // Yari saydam preview sprite
        public Color ValidColor = new Color(0f, 1f, 0f, 0.5f);    // Yesil
        public Color InvalidColor = new Color(1f, 0f, 0f, 0.5f);  // Kirmizi

        // Durum
        private BuildingConfigSO _selectedConfig;
        private bool _isPlacing;
        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;

            if (GhostRenderer != null)
                GhostRenderer.gameObject.SetActive(false);

            CreateBuildingButtons();
        }

        private void Update()
        {
            if (!_isPlacing) return;

            // Mouse → world → grid
            Vector3 mouseScreen = Input.mousePosition;
            mouseScreen.z = -_mainCamera.transform.position.z;
            Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(mouseScreen);

            var gridMgr = BuildingGridManager.Instance;
            if (gridMgr == null) return;

            Vector2Int gridPos = gridMgr.WorldToGrid(mouseWorld);

            // Ghost snap — grid pozisyonuna kilitle
            Vector3 ghostWorld = gridMgr.GridToWorld(gridPos.x, gridPos.y, _selectedConfig);
            if (GhostRenderer != null)
                GhostRenderer.transform.position = ghostWorld;

            // Renk — CanPlace kontrolu
            bool canPlace = gridMgr.CanPlace(_selectedConfig, gridPos.x, gridPos.y);
            if (GhostRenderer != null)
                GhostRenderer.color = canPlace ? ValidColor : InvalidColor;

            // Sol tikla → yerlestir
            if (Input.GetMouseButtonDown(0) && canPlace)
            {
                gridMgr.PlaceBuilding(_selectedConfig, gridPos.x, gridPos.y);
                StopPlacement();
            }

            // Sag tikla veya Escape → iptal
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                StopPlacement();
            }
        }

        /// <summary>
        /// Bina butonlarini SO listesinden olustur.
        /// </summary>
        private void CreateBuildingButtons()
        {
            var gridMgr = BuildingGridManager.Instance;
            if (gridMgr == null || gridMgr.BuildingConfigs == null) return;
            if (BuildingButtonPrefab == null || ButtonContainer == null) return;

            foreach (var config in gridMgr.BuildingConfigs)
            {
                var btnObj = Instantiate(BuildingButtonPrefab, ButtonContainer);

                // Buton text'ini ayarla (varsa)
                var text = btnObj.GetComponentInChildren<Text>();
                if (text != null)
                    text.text = config.DisplayName;

                // TMPro destegi (varsa)
                var tmpText = btnObj.GetComponentInChildren<TMPro.TMP_Text>();
                if (tmpText != null)
                    tmpText.text = config.DisplayName;

                // Icon ayarla (varsa)
                var icon = btnObj.transform.Find("Icon");
                if (icon != null)
                {
                    var img = icon.GetComponent<Image>();
                    if (img != null && config.Icon != null)
                        img.sprite = config.Icon;
                }

                // Tikla → yerlestirme modu baslat
                var capturedConfig = config;
                var btn = btnObj.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => StartPlacement(capturedConfig));
            }
        }

        /// <summary>
        /// Yerlestirme modunu baslat — secilen bina config ile ghost preview goster.
        /// </summary>
        public void StartPlacement(BuildingConfigSO config)
        {
            _selectedConfig = config;
            _isPlacing = true;

            if (GhostRenderer != null)
            {
                GhostRenderer.gameObject.SetActive(true);
                if (config.GhostSprite != null)
                    GhostRenderer.sprite = config.GhostSprite;
            }
        }

        /// <summary>
        /// Yerlestirme modunu durdur — ghost gizle.
        /// </summary>
        private void StopPlacement()
        {
            _isPlacing = false;
            _selectedConfig = null;

            if (GhostRenderer != null)
                GhostRenderer.gameObject.SetActive(false);
        }

        /// <summary>
        /// Su an yerlestirme modunda mi?
        /// </summary>
        public bool IsPlacing => _isPlacing;
    }
}
