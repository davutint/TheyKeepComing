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
        WizardTower,  // Buyucu Kulesi
        Catapult      // Mancinik
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
    /// Dogal kaynak zone tipi — bina yerlestirmede zone yakinlik kontrolu icin.
    /// None ise zone gereksinimi yok.
    /// </summary>
    public enum ResourcePointType : byte
    {
        None,    // Zone gereksinimi yok
        Forest,  // Orman (Oduncu icin)
        Stone,   // Tas kaynagi (Tas Ocagi icin)
        Iron     // Demir kaynagi (Maden icin)
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

    /// <summary>
    /// Kisla icin — okcu egitim component'i.
    /// Idle nufustan okcu egitir (kaynak harcar, zaman alir).
    /// </summary>
    public struct ArcherTrainer : IComponentData
    {
        public float TrainingTimer;      // Kalan egitim suresi
        public float TrainingDuration;   // Tek okcu egitim suresi (sn)
        public int FoodCostPerArcher;    // Egitim yemek maliyeti
        public int WoodCostPerArcher;    // Egitim ahsap maliyeti
        public bool IsTraining;          // Egitim devam ediyor mu
    }

    /// <summary>
    /// Fletcher (Ok Atolyesi) icin — ok uretim component'i.
    /// ResourceProducer KULLANILMAZ — ok 4 ana kaynaktan biri degil.
    /// </summary>
    public struct ArrowProducer : IComponentData
    {
        public float ArrowsPerWorkerPerMin;  // Isci basina ok/dk
        public int AssignedWorkers;
        public int MaxWorkers;
        public float WoodCostPerBatchPerMin; // Isci basina ahsap tuketim/dk
    }
}
