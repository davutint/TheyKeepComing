using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeadWalls
{
    /// <summary>
    /// Her bina tipinin maliyet, boyut, max isci gibi sabit verilerini tutan ScriptableObject.
    /// Inspector'dan her bina tipi icin 1 asset olusturulur.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingConfig", menuName = "DeadWalls/Building Config")]
    public class BuildingConfigSO : ScriptableObject
    {
        public BuildingType Type;
        public string DisplayName;        // "Oduncu", "Ciftlik" vs.
        public Sprite Icon;               // UI butonu icin
        public Sprite GhostSprite;        // Yerlestirme preview icin
        public int GridWidth = 3;
        public int GridHeight = 3;

        [Header("Maliyet")]
        public int WoodCost;
        public int StoneCost;
        public int IronCost;
        public int FoodCost;

        [Header("Uretim (sadece kaynak binalari)")]
        public ResourceType ProducedResource;
        public float RatePerWorkerPerMin;
        public int MaxWorkers;

        [Header("Dogal Kaynak Gereksinimi")]
        public ResourcePointType RequiredZone = ResourcePointType.None;
        public int ZoneProximityRadius = 3;

        [Header("Nufus (sadece Ev)")]
        public int PopulationCapacity;
        public float FoodCostPerMin;

        [Header("Kisla (Barracks)")]
        public float TrainingDuration;      // Saniye, default 30
        public int FoodCostPerArcher;       // default 20
        public int WoodCostPerArcher;       // default 10

        [Header("Ok Atolyesi (Fletcher)")]
        public float ArrowsPerWorkerPerMin; // default 10
        public float WoodCostPerBatchPerMin; // Isci basina ahsap tuketim/dk, default 2

        [Header("On Kosullar")]
        public bool RequireBlacksmith;     // true ise Demirci binasi olmadan yapilamaz

        [Header("Tile Layout (Building Tile Composer ile doldurulur)")]
        [Tooltip("Duvar, kapi, zemin tile'lari — base tilemap layer'ina konur")]
        public TileBase[] TileLayoutBase;

        [Tooltip("Cati, bayrak, detay tile'lari — top tilemap layer'ina konur")]
        public TileBase[] TileLayoutTop;
    }
}
