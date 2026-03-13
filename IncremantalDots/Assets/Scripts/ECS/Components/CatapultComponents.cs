using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    /// <summary>
    /// Mancinik birim component'i — sur slotuna yerlestirilen mancinik.
    /// Tas tuketir, AoE hasarli parabolik mermi atar.
    /// </summary>
    public struct CatapultUnit : IComponentData
    {
        public float Damage;           // Mermi hasari (default 40)
        public float SplashRadius;     // AoE yaricapi (default 2.0)
        public float FireRate;         // Atis/sn (default 0.2 = 5sn/atis)
        public float FireTimer;        // Kalan bekleme suresi
        public float Range;            // Menzil (default 25)
        public int StoneCostPerShot;   // Atis basina tas maliyeti (default 1)
    }

    /// <summary>
    /// Mancinik mermisi — parabolik yol izler, hedefe varunca AoE hasar.
    /// </summary>
    public struct CatapultProjectile : IComponentData
    {
        public float Damage;           // Hasar miktari
        public float SplashRadius;     // AoE yaricapi
        public float3 StartPos;        // Baslangic pozisyonu
        public float3 TargetPos;       // Hedef pozisyonu
        public float FlightDuration;   // Toplam ucus suresi (default 1.2s)
        public float FlightTimer;      // Gecen ucus suresi
        public float ArcHeight;        // Parabol yuksekligi (default 5.0)
    }

    /// <summary>
    /// Mancinik mermisi filtreleme tag'i.
    /// </summary>
    public struct CatapultProjectileTag : IComponentData { }
}
