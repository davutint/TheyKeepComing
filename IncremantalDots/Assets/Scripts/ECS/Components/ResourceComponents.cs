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
    /// Finite ok envanteri singleton'i. GameState entity uzerinde tutulur.
    /// Refill anlik Wood transaction'idir; Fletcher/production queue V1'de kullanilmaz.
    /// </summary>
    public struct ArrowSupply : IComponentData
    {
        public int Current;
        public int CapacityLevel;
        public int EfficiencyLevel;
        // Castle Heart run bonuslari. Upgrade level fiyat egrisine katilmaz;
        // exact Heart graph restore/replay E6 tarafindan yeniden uygulanir.
        public int HeartCapacityBonus;
        public int HeartEfficiencyBonus;
        // Eski save semasi icin korunur; V1 castle loop'ta her zaman 0'dir.
        public float Accumulator;
    }

    /// <summary>
    /// Yalniz mevcut run icinde yasayan Castle Heart para birimi.
    /// Meta state'e yazilmaz; Game Over run save'ini sildiginde bu deger de silinir.
    /// </summary>
    public struct GraveEssence : IComponentData
    {
        public long Current;
    }
}
