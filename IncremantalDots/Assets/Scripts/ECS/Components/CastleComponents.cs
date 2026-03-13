using Unity.Entities;

namespace DeadWalls
{
    public struct WallSegment : IComponentData
    {
        public float MaxHP;
        public float CurrentHP;
    }

    public struct GateComponent : IComponentData
    {
        public float MaxHP;
        public float CurrentHP;
    }

    public struct CastleHP : IComponentData
    {
        public float MaxHP;
        public float CurrentHP;
    }

    public struct WallXPosition : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// Kale yukseltme verisi. Castle entity uzerinde tutulur.
    /// </summary>
    public struct CastleUpgradeData : IComponentData
    {
        public int Level;             // Su anki yukseltme seviyesi (0 = yukseltilmemis)
        public int MaxLevel;          // Maksimum seviye
        public int CapacityPerLevel;  // Her seviye basina ek nufus kapasitesi
        public int WoodCostPerLevel;  // Her yukseltme icin ahsap maliyeti
        public int StoneCostPerLevel; // Her yukseltme icin tas maliyeti
    }
}
