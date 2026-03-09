using Unity.Entities;
using UnityEngine;

namespace DeadWalls
{
    public class CastleAuthoring : MonoBehaviour
    {
        public float WallHP = 200f;
        public float GateHP = 100f;
        public float CastleMaxHP = 500f;
        public float WallXPos = 4.76f;

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
                AddComponent(entity, new GateComponent
                {
                    MaxHP = authoring.GateHP,
                    CurrentHP = authoring.GateHP
                });
                AddComponent(entity, new CastleHP
                {
                    MaxHP = authoring.CastleMaxHP,
                    CurrentHP = authoring.CastleMaxHP
                });
                AddComponent(entity, new WallXPosition
                {
                    Value = authoring.WallXPos
                });
            }
        }
    }
}
