using Unity.Entities;
using UnityEngine;

namespace DeadWalls
{
    public class ZombieAuthoring : MonoBehaviour
    {
        public float MoveSpeed = 1.5f;
        public float MaxHP = 20f;
        public float AttackDamage = 5f;
        public float AttackCooldown = 1f;
        public int GoldReward = 5;
        public int XPReward = 10;

        public class Baker : Baker<ZombieAuthoring>
        {
            public override void Bake(ZombieAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new ZombieTag());
                AddComponent(entity, new ZombieStats
                {
                    MoveSpeed = authoring.MoveSpeed,
                    MaxHP = authoring.MaxHP,
                    CurrentHP = authoring.MaxHP,
                    AttackDamage = authoring.AttackDamage,
                    AttackCooldown = authoring.AttackCooldown,
                    AttackTimer = 0f,
                    GoldReward = authoring.GoldReward,
                    XPReward = authoring.XPReward
                });
                AddComponent(entity, new ZombieState
                {
                    Value = ZombieStateType.Moving
                });
                AddComponent(entity, new ZombieStopOffset { Value = 0f });
            }
        }
    }
}
