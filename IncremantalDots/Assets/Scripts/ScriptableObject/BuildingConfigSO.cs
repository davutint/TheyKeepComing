using UnityEngine;

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

        [Header("Nufus (sadece Ev)")]
        public int PopulationCapacity;
        public float FoodCostPerMin;
    }
}
