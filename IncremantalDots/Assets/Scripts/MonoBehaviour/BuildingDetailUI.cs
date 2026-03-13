using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// Binaya tiklaninca acilan detay paneli.
    /// Isci atama (+/-), bina yikma, uretim bilgisi gosterir.
    /// </summary>
    public class BuildingDetailUI : MonoBehaviour
    {
        public static BuildingDetailUI Instance { get; private set; }

        [Header("Panel")]
        public GameObject DetailPanel;

        [Header("Bilgi")]
        public TMP_Text BuildingNameText;
        public TMP_Text ProductionText;

        [Header("Isci")]
        public GameObject WorkerSection;       // Isci bolumu (ev icin gizlenir)
        public TMP_Text WorkersText;
        public TMP_Text IdleText;
        public Button AddWorkerButton;
        public Button RemoveWorkerButton;

        [Header("Kapasite (Ev icin)")]
        public GameObject CapacitySection;     // Kapasite bolumu (kaynak binalari icin gizlenir)
        public TMP_Text CapacityText;

        [Header("Aksiyonlar")]
        public Button DemolishButton;
        public Button CloseButton;

        // Secili bina
        private Entity _selectedEntity;
        private EntityManager _entityManager;
        private bool _hasEntity;

        // Turkce bina isimleri
        private static readonly string[] BuildingNames = {
            "Oduncu", "Tas Ocagi", "Maden", "Ciftlik", "Ev",
            "Kisla", "Ok Atolyesi", "Demirci", "Buyucu Kulesi", "Mancinik"
        };

        // Turkce kaynak isimleri
        private static readonly string[] ResourceNames = { "Ahsap", "Tas", "Demir", "Yemek" };

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (DetailPanel != null)
                DetailPanel.SetActive(false);

            // Buton listener'lari
            if (AddWorkerButton != null)
                AddWorkerButton.onClick.AddListener(OnAddWorker);
            if (RemoveWorkerButton != null)
                RemoveWorkerButton.onClick.AddListener(OnRemoveWorker);
            if (DemolishButton != null)
                DemolishButton.onClick.AddListener(OnDemolish);
            if (CloseButton != null)
                CloseButton.onClick.AddListener(CloseDetail);
        }

        private void Update()
        {
            if (!_hasEntity) return;

            // Entity hala var mi kontrol (restart sonrasi silinmis olabilir)
            if (!_entityManager.Exists(_selectedEntity))
            {
                CloseDetail();
                return;
            }

            // BuildingData oku
            var buildingData = _entityManager.GetComponentData<BuildingData>(_selectedEntity);
            int typeIndex = (int)buildingData.Type;
            string name = typeIndex < BuildingNames.Length ? BuildingNames[typeIndex] : buildingData.Type.ToString();

            if (BuildingNameText != null)
                BuildingNameText.text = $"{name} (Sv.{buildingData.Level})";

            // Bina tipi kontrolleri
            bool isProducer = _entityManager.HasComponent<ResourceProducer>(_selectedEntity);
            bool isHouse = _entityManager.HasComponent<PopulationProvider>(_selectedEntity);
            bool isTrainer = _entityManager.HasComponent<ArcherTrainer>(_selectedEntity);
            bool isArrowProducer = _entityManager.HasComponent<ArrowProducer>(_selectedEntity);

            // Isci bolumu — kaynak binalari ve Fletcher icin
            if (WorkerSection != null)
                WorkerSection.SetActive(isProducer || isArrowProducer);

            // Kapasite bolumu — sadece ev icin
            if (CapacitySection != null)
                CapacitySection.SetActive(isHouse);

            if (isProducer)
            {
                var producer = _entityManager.GetComponentData<ResourceProducer>(_selectedEntity);
                int resIndex = (int)producer.ResourceType;
                string resName = resIndex < ResourceNames.Length ? ResourceNames[resIndex] : producer.ResourceType.ToString();
                float totalRate = producer.RatePerWorkerPerMin * producer.AssignedWorkers;

                if (ProductionText != null)
                    ProductionText.text = $"Uretim: {resName} ({totalRate:F1}/dk)";

                if (WorkersText != null)
                    WorkersText.text = $"Isci: {producer.AssignedWorkers} / {producer.MaxWorkers}";

                var pop = GameManager.Instance != null ? GameManager.Instance.Population : default;
                int idle = pop.Idle;
                if (IdleText != null)
                    IdleText.text = $"Bos Isci: {idle}";

                if (AddWorkerButton != null)
                    AddWorkerButton.interactable = idle > 0 && producer.AssignedWorkers < producer.MaxWorkers;
                if (RemoveWorkerButton != null)
                    RemoveWorkerButton.interactable = producer.AssignedWorkers > 0;
            }
            else if (isArrowProducer)
            {
                // Fletcher — ok uretim bilgisi + isci atama
                var ap = _entityManager.GetComponentData<ArrowProducer>(_selectedEntity);
                float totalArrowRate = ap.ArrowsPerWorkerPerMin * ap.AssignedWorkers;
                float totalWoodCost = ap.WoodCostPerBatchPerMin * ap.AssignedWorkers;

                if (ProductionText != null)
                {
                    if (ap.AssignedWorkers > 0)
                        ProductionText.text = $"Ok: {totalArrowRate:F1}/dk | Ahsap: -{totalWoodCost:F1}/dk";
                    else
                        ProductionText.text = "Isci atanmadi";
                }

                if (WorkersText != null)
                    WorkersText.text = $"Isci: {ap.AssignedWorkers} / {ap.MaxWorkers}";

                var pop = GameManager.Instance != null ? GameManager.Instance.Population : default;
                int idle = pop.Idle;
                if (IdleText != null)
                    IdleText.text = $"Bos Isci: {idle}";

                if (AddWorkerButton != null)
                    AddWorkerButton.interactable = idle > 0 && ap.AssignedWorkers < ap.MaxWorkers;
                if (RemoveWorkerButton != null)
                    RemoveWorkerButton.interactable = ap.AssignedWorkers > 0;
            }
            else if (isTrainer)
            {
                // Kisla — egitim durumu gosterimi, isci bolumu gizle
                if (WorkerSection != null)
                    WorkerSection.SetActive(false);

                var trainer = _entityManager.GetComponentData<ArcherTrainer>(_selectedEntity);
                if (ProductionText != null)
                {
                    if (trainer.IsTraining)
                        ProductionText.text = $"Egitim: {trainer.TrainingTimer:F1}s kaldi";
                    else
                    {
                        var pop = GameManager.Instance != null ? GameManager.Instance.Population : default;
                        bool hasIdle = pop.Idle > 0;
                        var res = GameManager.Instance != null ? GameManager.Instance.Resources : default;
                        bool hasFood = res.Food >= trainer.FoodCostPerArcher;
                        bool hasWood = res.Wood >= trainer.WoodCostPerArcher;

                        if (!hasIdle)
                            ProductionText.text = "Bekleniyor (Bos nufus yok)";
                        else if (!hasFood || !hasWood)
                            ProductionText.text = $"Bekleniyor (Kaynak yetersiz: {trainer.FoodCostPerArcher}Y {trainer.WoodCostPerArcher}A)";
                        else
                            ProductionText.text = "Egitim basliyor...";
                    }
                }
            }
            else
            {
                // Demirci vs. — bos bilgi
                if (ProductionText != null)
                    ProductionText.text = "";
            }

            if (isHouse)
            {
                var provider = _entityManager.GetComponentData<PopulationProvider>(_selectedEntity);
                if (CapacityText != null)
                    CapacityText.text = $"Kapasite: +{provider.CapacityAmount}";
            }
        }

        /// <summary>
        /// Detay panelini ac — secilen entity bilgilerini goster.
        /// </summary>
        public void ShowDetail(Entity entity)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            _entityManager = world.EntityManager;
            if (!_entityManager.Exists(entity)) return;

            _selectedEntity = entity;
            _hasEntity = true;

            if (DetailPanel != null)
                DetailPanel.SetActive(true);
        }

        /// <summary>
        /// Detay panelini kapat.
        /// </summary>
        public void CloseDetail()
        {
            _hasEntity = false;
            _selectedEntity = Entity.Null;

            if (DetailPanel != null)
                DetailPanel.SetActive(false);
        }

        private void OnAddWorker()
        {
            if (!_hasEntity || !_entityManager.Exists(_selectedEntity)) return;
            var pop = GameManager.Instance != null ? GameManager.Instance.Population : default;
            if (pop.Idle <= 0) return;

            // ResourceProducer (kaynak binalari)
            if (_entityManager.HasComponent<ResourceProducer>(_selectedEntity))
            {
                var producer = _entityManager.GetComponentData<ResourceProducer>(_selectedEntity);
                if (producer.AssignedWorkers >= producer.MaxWorkers) return;
                producer.AssignedWorkers++;
                _entityManager.SetComponentData(_selectedEntity, producer);
                return;
            }

            // ArrowProducer (Fletcher)
            if (_entityManager.HasComponent<ArrowProducer>(_selectedEntity))
            {
                var ap = _entityManager.GetComponentData<ArrowProducer>(_selectedEntity);
                if (ap.AssignedWorkers >= ap.MaxWorkers) return;
                ap.AssignedWorkers++;
                _entityManager.SetComponentData(_selectedEntity, ap);
            }
        }

        private void OnRemoveWorker()
        {
            if (!_hasEntity || !_entityManager.Exists(_selectedEntity)) return;

            // ResourceProducer (kaynak binalari)
            if (_entityManager.HasComponent<ResourceProducer>(_selectedEntity))
            {
                var producer = _entityManager.GetComponentData<ResourceProducer>(_selectedEntity);
                if (producer.AssignedWorkers <= 0) return;
                producer.AssignedWorkers--;
                _entityManager.SetComponentData(_selectedEntity, producer);
                return;
            }

            // ArrowProducer (Fletcher)
            if (_entityManager.HasComponent<ArrowProducer>(_selectedEntity))
            {
                var ap = _entityManager.GetComponentData<ArrowProducer>(_selectedEntity);
                if (ap.AssignedWorkers <= 0) return;
                ap.AssignedWorkers--;
                _entityManager.SetComponentData(_selectedEntity, ap);
            }
        }

        private void OnDemolish()
        {
            if (!_hasEntity || !_entityManager.Exists(_selectedEntity)) return;

            var buildingData = _entityManager.GetComponentData<BuildingData>(_selectedEntity);
            int gridX = buildingData.GridX;
            int gridY = buildingData.GridY;

            CloseDetail();

            if (BuildingGridManager.Instance != null)
                BuildingGridManager.Instance.RemoveBuilding(gridX, gridY);
        }
    }
}
