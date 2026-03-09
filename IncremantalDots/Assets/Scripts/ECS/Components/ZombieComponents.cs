using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    public struct ZombieTag : IComponentData { }

    public struct ZombieStats : IComponentData
    {
        public float MoveSpeed;
        public float MaxHP;
        public float CurrentHP;
        public float AttackDamage;
        public float AttackCooldown;
        public float AttackTimer;
        public int GoldReward;
        public int XPReward;
    }

    public enum ZombieStateType : byte
    {
        Moving,
        Attacking,
        Dead
    }

    public struct ZombieState : IComponentData
    {
        public ZombieStateType Value;
    }

    public struct ReachedTarget : IComponentData { }

    /// <summary>
    /// CrowdGroup entity'sini bulmak icin tag.
    /// CrowdGroupAuthoring ile ayni GameObject'e eklenir.
    /// </summary>
    public struct ZombieCrowdGroupTag : IComponentData { }

    /// <summary>
    /// Zombinin duvara olan durma mesafesi.
    /// Spawn aninda rastgele atanir (front-heavy dagilim).
    /// pos.x <= wallX + Value olunca Attacking'e gecer.
    /// </summary>
    public struct ZombieStopOffset : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// Olum animasyonu suresi. 0'a dusunce entity silinir.
    /// </summary>
    public struct DeathTimer : IComponentData
    {
        public float Value;
    }
}
