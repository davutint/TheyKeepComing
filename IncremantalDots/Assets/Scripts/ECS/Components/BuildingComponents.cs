using Unity.Entities;

namespace DeadWalls
{
    /// <summary>
    /// Bina tipi enum — tum binalar tek enum'da tanimlanir.
    /// Castle enum'da YOK — kale zaten sahnede, bina olarak yerlestirilmiyor.
    /// </summary>
    public enum BuildingType : byte
    {
        Lumberjack,   // Oduncu
        Quarry,       // Tas Ocagi
        Mine,         // Maden
        Farm,         // Ciftlik
        House,        // Ev
        Barracks,     // Kisla
        Fletcher,     // Ok Atolyesi
        Blacksmith,   // Demirci
        WizardTower   // Buyucu Kulesi
    }

    /// <summary>
    /// Kaynak tipi enum — ResourceProducer icin hangi kaynagi urettigini belirtir.
    /// </summary>
    public enum ResourceType : byte
    {
        Wood,
        Stone,
        Iron,
        Food
    }

    /// <summary>
    /// Her bina entity'sinde bulunur. Bina tipi, seviyesi ve grid pozisyonu.
    /// </summary>
    public struct BuildingData : IComponentData
    {
        public BuildingType Type;
        public int Level;    // Baslangic 1
        public int GridX;    // Grid pozisyonu (sol-alt kose)
        public int GridY;
    }

    /// <summary>
    /// Kaynak ureten binalar icin (Oduncu, Tas Ocagi, Maden, Ciftlik).
    /// </summary>
    public struct ResourceProducer : IComponentData
    {
        public ResourceType ResourceType;
        public float RatePerWorkerPerMin;  // Isci basina dk/kaynak
        public int AssignedWorkers;
        public int MaxWorkers;
    }

    /// <summary>
    /// Nufus kapasitesi artiran binalar icin (Ev).
    /// </summary>
    public struct PopulationProvider : IComponentData
    {
        public int CapacityAmount;
    }

    /// <summary>
    /// Yemek tuketen binalar icin (Ev).
    /// </summary>
    public struct BuildingFoodCost : IComponentData
    {
        public float FoodPerMin;
    }
}
