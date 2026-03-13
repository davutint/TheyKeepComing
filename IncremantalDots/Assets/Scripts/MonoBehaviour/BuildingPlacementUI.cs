using Unity.Entities;
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
            // Placement modu degil + sol tikla → bina detay paneli
            if (!_isPlacing)
            {
                if (Input.GetMouseButtonDown(0)
                    && (UnityEngine.EventSystems.EventSystem.current == null
                        || !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()))
                {
                    Vector3 ms = Input.mousePosition;
                    ms.z = -_mainCamera.transform.position.z;
                    Vector3 mw = _mainCamera.ScreenToWorldPoint(ms);

                    var gm = BuildingGridManager.Instance;
                    if (gm != null)
                    {
                        Vector2Int gp = gm.WorldToGrid(mw);
                        Entity entity = gm.GetBuildingEntity(gp.x, gp.y);
                        if (entity != Entity.Null && BuildingDetailUI.Instance != null)
                            BuildingDetailUI.Instance.ShowDetail(entity);
                    }
                }
                return;
            }

            // Mouse → world → grid
            Vector3 mouseScreen = Input.mousePosition;
            mouseScreen.z = -_mainCamera.transform.position.z;
            Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(mouseScreen);

            var gridMgr = BuildingGridManager.Instance;
            if (gridMgr == null) return;

            // Mouse hangi grid hucresindeyse orasi binanin sol-alt kosesi
            // Ghost gorseli GridToWorld'de zaten merkezleniyor (+GridWidth*0.5)
            Vector2Int gridPos = gridMgr.WorldToGrid(mouseWorld);
            _lastGridPos = gridPos;

            // Ghost snap — grid alanina tam otur
            if (GhostRenderer != null && GhostRenderer.sprite != null)
            {
                Sprite s = GhostRenderer.sprite;
                float spriteW = s.rect.width / s.pixelsPerUnit;
                float spriteH = s.rect.height / s.pixelsPerUnit;

                // Sprite'i GridWidth x GridHeight'a olcekle
                GhostRenderer.transform.localScale = new Vector3(
                    _selectedConfig.GridWidth / spriteW,
                    _selectedConfig.GridHeight / spriteH, 1f);

                // Grid'in sol-alt kosesi (world space)
                Vector3 bottomLeft = new Vector3(
                    gridPos.x + gridMgr.GridOriginDebug.x,
                    gridPos.y + gridMgr.GridOriginDebug.y, 0f);

                // Sprite pivot'una gore offset (0,0=sol-alt, 0.5,0.5=merkez)
                Vector2 pivotNorm = s.pivot / new Vector2(s.rect.width, s.rect.height);
                GhostRenderer.transform.position = bottomLeft + new Vector3(
                    pivotNorm.x * _selectedConfig.GridWidth,
                    pivotNorm.y * _selectedConfig.GridHeight, 0f);
            }

            // Renk — CanPlace kontrolu
            bool canPlace = gridMgr.CanPlace(_selectedConfig, gridPos.x, gridPos.y);
            if (Input.GetKeyDown(KeyCode.F1))
                Debug.Log($"[PlaceDebug] mouseWorld:{mouseWorld} gridPos:{gridPos} canPlace:{canPlace} gridSize:{gridMgr.GridWidthDebug}x{gridMgr.GridHeightDebug} origin:{gridMgr.GridOriginDebug}");
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
                {
                    GhostRenderer.sprite = config.GhostSprite;

                    // Sprite'i GridWidth x GridHeight alanina sigdir
                    float spriteW = config.GhostSprite.rect.width / config.GhostSprite.pixelsPerUnit;
                    float spriteH = config.GhostSprite.rect.height / config.GhostSprite.pixelsPerUnit;
                    GhostRenderer.transform.localScale = new Vector3(
                        config.GridWidth / spriteW,
                        config.GridHeight / spriteH, 1f);
                }
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

        // Gizmos: yerleştirme sirasinda SO'nun GridWidth x GridHeight alanini ciz
        private Vector2Int _lastGridPos;
        private void OnDrawGizmos()
        {
            if (!_isPlacing || _selectedConfig == null) return;

            var gridMgr = BuildingGridManager.Instance;
            if (gridMgr == null) return;

            // Grid'in sol-alt kosesi (sprite ile ayni hesaplama)
            Vector3 bottomLeft = new Vector3(
                _lastGridPos.x + gridMgr.GridOriginDebug.x,
                _lastGridPos.y + gridMgr.GridOriginDebug.y, 0f);
            Vector3 size = new Vector3(_selectedConfig.GridWidth, _selectedConfig.GridHeight, 0f);
            Vector3 center = bottomLeft + size * 0.5f;

            // Grid alani — sari wireframe
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center, size);

            // Her hucreyi ciz
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            for (int x = 0; x < _selectedConfig.GridWidth; x++)
            {
                for (int y = 0; y < _selectedConfig.GridHeight; y++)
                {
                    Vector3 cellCenter = bottomLeft + new Vector3(x + 0.5f, y + 0.5f, 0f);
                    Gizmos.DrawWireCube(cellCenter, Vector3.one);
                }
            }
        }
    }
}
