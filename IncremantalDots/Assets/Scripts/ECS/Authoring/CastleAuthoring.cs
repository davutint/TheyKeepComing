using Unity.Entities;
using UnityEngine;

namespace DeadWalls
{
    public class CastleAuthoring : MonoBehaviour
    {
        public float WallHP = 200f;
        [HideInInspector] public float GateHP = 100f;
        [HideInInspector] public float CastleMaxHP = 500f;
        public float WallXPos = 4.76f;

        [Header("Kale Yukseltme")]
        public int MaxUpgradeLevel = 5;
        public int CapacityPerLevel = 10;
        public int UpgradeWoodCost = 20;
        public int UpgradeStoneCost = 30;

        public class Baker : Baker<CastleAuthoring>
        {
            public override void Bake(CastleAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new WallSegment
                {
                    MaxHP = authoring.WallHP,
                    CurrentHP = authoring.WallHP
                });
                AddComponent(entity, new WallXPosition
                {
                    Value = authoring.WallXPos
                });
                AddComponent(entity, new CastleUpgradeData
                {
                    Level = 0,
                    MaxLevel = authoring.MaxUpgradeLevel,
                    CapacityPerLevel = authoring.CapacityPerLevel,
                    WoodCostPerLevel = authoring.UpgradeWoodCost,
                    StoneCostPerLevel = authoring.UpgradeStoneCost
                });
            }
        }
    }
}
