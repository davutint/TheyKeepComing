using Unity.Entities;

namespace DeadWalls
{
    /// <summary>
    /// Mevcut kaynak miktarlari (int). Singleton — GameState entity uzerinde.
    /// </summary>
    public struct ResourceData : IComponentData
    {
        public int Wood;
        public int Stone;
        public int Iron;
        public int Food;
    }

    /// <summary>
    /// Dakika basina uretim hizlari. Binalar + bonuslar bunu degistirir.
    /// </summary>
    public struct ResourceProductionRate : IComponentData
    {
        public float WoodPerMin;
        public float StonePerMin;
        public float IronPerMin;
        public float FoodPerMin;
    }

    /// <summary>
    /// Dakika basina tuketim hizlari. Nufus + binalar bunu degistirir.
    /// </summary>
    public struct ResourceConsumptionRate : IComponentData
    {
        public float WoodPerMin;
        public float StonePerMin;
        public float IronPerMin;
        public float FoodPerMin;
    }

    /// <summary>
    /// Kesirli birikim tamponu. ±1.0 gecince ResourceData int'e transfer edilir.
    /// Sadece ResourceTickSystem kullanir.
    /// </summary>
    public struct ResourceAccumulator : IComponentData
    {
        public float Wood;
        public float Stone;
        public float Iron;
        public float Food;
    }

    /// <summary>
    /// Ok envanter singleton. GameState entity uzerinde tutulur.
    /// Fletcher ok uretir, ArcherShootSystem ok tuketir.
    /// </summary>
    public struct ArrowSupply : IComponentData
    {
        public int Current;       // Mevcut ok sayisi
        public float Accumulator; // Kesirli birikim (ResourceAccumulator benzeri)
    }
}
