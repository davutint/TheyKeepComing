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

            // Mouse → world
            Vector3 mouseScreen = Input.mousePosition;
            mouseScreen.z = -_mainCamera.transform.position.z;
            Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(mouseScreen);

            // Grid yerlestirme
            var gridMgr = BuildingGridManager.Instance;
            if (gridMgr == null) return;

            // Mouse hangi grid hucresindeyse orasi binanin sol-alt kosesi
            // Ghost gorseli GridToWorld'de zaten merkezleniyor (+GridWidth*0.5)
            Vector2Int gridPos = gridMgr.WorldToGrid(mouseWorld);
            _lastGridPos = gridPos;

            // Ghost snap — izometrik grid hucresine otur
            if (GhostRenderer != null && GhostRenderer.sprite != null)
            {
                // GridToWorld ile binanin merkez world pozisyonunu al
                Vector3 center = gridMgr.GridToWorld(gridPos.x, gridPos.y, _selectedConfig);
                GhostRenderer.transform.position = center;

                // Izometrik cell boyutlarina gore olcekle
                Sprite s = GhostRenderer.sprite;
                float spriteWorldW = s.rect.width / s.pixelsPerUnit;
                float spriteWorldH = s.rect.height / s.pixelsPerUnit;
                Vector3 cellSize = gridMgr.IsoGrid.cellSize;
                float footprintW = _selectedConfig.GridWidth * cellSize.x;
                float footprintH = _selectedConfig.GridHeight * cellSize.y;
                GhostRenderer.transform.localScale = new Vector3(
                    footprintW / spriteWorldW,
                    footprintH / spriteWorldH, 1f);
            }

            // Renk — CanPlace kontrolu
            bool gridCanPlace = gridMgr.CanPlace(_selectedConfig, gridPos.x, gridPos.y);
            if (Input.GetKeyDown(KeyCode.F1))
                Debug.Log($"[PlaceDebug] mouseWorld:{mouseWorld} gridPos:{gridPos} canPlace:{gridCanPlace} gridSize:{gridMgr.GridWidthDebug}x{gridMgr.GridHeightDebug} origin:{gridMgr.GridOriginDebug}");
            if (GhostRenderer != null)
                GhostRenderer.color = gridCanPlace ? ValidColor : InvalidColor;

            // Sol tikla → yerlestir
            if (Input.GetMouseButtonDown(0) && gridCanPlace)
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
                {
                    btn.onClick.AddListener(() =>
                    {
                        // Demirci gereksinimi kontrolu
                        if (capturedConfig.RequireBlacksmith &&
                            (BuildingGridManager.Instance == null ||
                             !BuildingGridManager.Instance.HasBuildingOfType(BuildingType.Blacksmith)))
                        {
                            Debug.Log("[BuildingUI] Demirci binasi gerekli!");
                            return;
                        }
                        StartPlacement(capturedConfig);
                    });
                }
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

                    // Izometrik cell boyutlarina gore sprite olcekle
                    float spriteW = config.GhostSprite.rect.width / config.GhostSprite.pixelsPerUnit;
                    float spriteH = config.GhostSprite.rect.height / config.GhostSprite.pixelsPerUnit;
                    var gridMgr = BuildingGridManager.Instance;
                    if (gridMgr != null && gridMgr.IsoGrid != null)
                    {
                        Vector3 cellSize = gridMgr.IsoGrid.cellSize;
                        GhostRenderer.transform.localScale = new Vector3(
                            config.GridWidth * cellSize.x / spriteW,
                            config.GridHeight * cellSize.y / spriteH, 1f);
                    }
                }
            }

            // Zone gerektiren bina icin overlay goster
            if (config.RequiredZone != ResourcePointType.None)
            {
                var gridMgr = BuildingGridManager.Instance;
                if (gridMgr != null) gridMgr.ShowResourceZones();
            }
        }

        /// <summary>
        /// Yerlestirme modunu durdur — ghost gizle.
        /// </summary>
        private void StopPlacement()
        {
            // Zone overlay'i gizle
            var gridMgr = BuildingGridManager.Instance;
            if (gridMgr != null) gridMgr.HideResourceZones();

            _isPlacing = false;
            _selectedConfig = null;

            if (GhostRenderer != null)
                GhostRenderer.gameObject.SetActive(false);
        }

        /// <summary>
        /// Su an yerlestirme modunda mi?
        /// </summary>
        public bool IsPlacing => _isPlacing;

        // Gizmos: yerleştirme sirasinda SO'nun GridWidth x GridHeight alanini diamond (baklava) olarak ciz
        private Vector2Int _lastGridPos;
        private void OnDrawGizmos()
        {
            if (!_isPlacing || _selectedConfig == null) return;

            var gridMgr = BuildingGridManager.Instance;
            if (gridMgr == null || gridMgr.IsoGrid == null) return;

            Grid grid = gridMgr.IsoGrid;
            int ox = _lastGridPos.x + gridMgr.GridOriginDebug.x;
            int oy = _lastGridPos.y + gridMgr.GridOriginDebug.y;

            // Her hucreyi diamond olarak ciz
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            for (int x = 0; x < _selectedConfig.GridWidth; x++)
            {
                for (int y = 0; y < _selectedConfig.GridHeight; y++)
                {
                    int cx = ox + x;
                    int cy = oy + y;
                    // Diamond 4 kosesi: CellToWorld ile izometrik pozisyon
                    Vector3 bot   = grid.CellToWorld(new Vector3Int(cx, cy, 0));
                    Vector3 right = grid.CellToWorld(new Vector3Int(cx + 1, cy, 0));
                    Vector3 top   = grid.CellToWorld(new Vector3Int(cx + 1, cy + 1, 0));
                    Vector3 left  = grid.CellToWorld(new Vector3Int(cx, cy + 1, 0));
                    Gizmos.DrawLine(bot, right);
                    Gizmos.DrawLine(right, top);
                    Gizmos.DrawLine(top, left);
                    Gizmos.DrawLine(left, bot);
                }
            }

            // Dis ceper — sari wireframe (footprint siniri)
            Gizmos.color = Color.yellow;
            Vector3 fBot   = grid.CellToWorld(new Vector3Int(ox, oy, 0));
            Vector3 fRight = grid.CellToWorld(new Vector3Int(ox + _selectedConfig.GridWidth, oy, 0));
            Vector3 fTop   = grid.CellToWorld(new Vector3Int(ox + _selectedConfig.GridWidth, oy + _selectedConfig.GridHeight, 0));
            Vector3 fLeft  = grid.CellToWorld(new Vector3Int(ox, oy + _selectedConfig.GridHeight, 0));
            Gizmos.DrawLine(fBot, fRight);
            Gizmos.DrawLine(fRight, fTop);
            Gizmos.DrawLine(fTop, fLeft);
            Gizmos.DrawLine(fLeft, fBot);
        }
    }
}
