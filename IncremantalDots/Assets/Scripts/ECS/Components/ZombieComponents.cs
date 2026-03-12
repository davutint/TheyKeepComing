using Unity.Entities;

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
        public int XPReward;
    }

    public enum ZombieStateType : byte
    {
        Moving,
        Attacking,
        Dead,
        /// <summary>
        /// Domino etkisi: Onundeki Attacking/Queued zombiye cakisan zombi durur ama saldirmaz.
        /// Onundeki zombi olurse/giderse tekrar Moving'e doner.
        /// </summary>
        Queued
    }

    public struct ZombieState : IComponentData
    {
        public ZombieStateType Value;
    }

    /// <summary>
    /// Olum animasyonu suresi. 0'a dusunce entity silinir.
    /// </summary>
    public struct DeathTimer : IComponentData
    {
        public float Value;
    }
}
