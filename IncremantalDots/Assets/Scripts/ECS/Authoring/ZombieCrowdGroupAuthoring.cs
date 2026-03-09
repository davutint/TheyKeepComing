using Unity.Entities;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// CrowdGroupAuthoring ile ayni GameObject'e eklenir.
    /// Zombi spawn'inda CrowdGroup entity'sini bulmak icin singleton tag olusturur.
    /// </summary>
    public class ZombieCrowdGroupAuthoring : MonoBehaviour
    {
        public class Baker : Baker<ZombieCrowdGroupAuthoring>
        {
            public override void Bake(ZombieCrowdGroupAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ZombieCrowdGroupTag());
            }
        }
    }
}
