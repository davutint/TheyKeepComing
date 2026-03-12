using Unity.Entities;

namespace DeadWalls
{
    /// <summary>
    /// Nufus durumu singleton. GameState entity uzerinde tutulur.
    /// Tek havuz modeli: Tum insanlar = Workers + Archers + Idle
    /// </summary>
    public struct PopulationState : IComponentData
    {
        public int Total;                   // Toplam nufus
        public int Workers;                 // Binalara atanmis isci (M1.4+ gercek, simdi test)
        public int Archers;                 // Egitilmis okcu (M1.6+ gercek, simdi test)
        public int Idle;                    // Hesaplanan: Total - Workers - Archers
        public int Capacity;                // Maksimum nufus kapasitesi
        public float FoodPerAssignedPerMin; // Atanmis kisi basina yemek/dk
    }
}
