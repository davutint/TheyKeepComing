using Unity.Entities;
using UnityEngine;

namespace DeadWalls
{
    public struct ZombiePrefabData : IComponentData
    {
        public Entity ZombiePrefab;
    }

    public struct ArrowPrefabData : IComponentData
    {
        public Entity ArrowPrefab;
    }

    public struct ArcherPrefabData : IComponentData
    {
        public Entity ArcherPrefab;
    }

    public struct CatapultPrefabData : IComponentData
    {
        public Entity CatapultPrefab;
    }

    public struct CatapultProjectilePrefabData : IComponentData
    {
        public Entity CatapultProjectilePrefab;
    }

    public class WaveConfigAuthoring : MonoBehaviour
    {
        public GameObject ZombiePrefab;
        public GameObject ArrowPrefab;
        public GameObject ArcherPrefab;
        public GameObject CatapultPrefab;
        public GameObject CatapultProjectilePrefab;

        public class Baker : Baker<WaveConfigAuthoring>
        {
            public override void Bake(WaveConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new ZombiePrefabData
                {
                    ZombiePrefab = GetEntity(authoring.ZombiePrefab, TransformUsageFlags.Dynamic)
                });
                AddComponent(entity, new ArrowPrefabData
                {
                    ArrowPrefab = GetEntity(authoring.ArrowPrefab, TransformUsageFlags.Dynamic)
                });
                AddComponent(entity, new ArcherPrefabData
                {
                    ArcherPrefab = GetEntity(authoring.ArcherPrefab, TransformUsageFlags.Dynamic)
                });

                if (authoring.CatapultPrefab != null)
                {
                    AddComponent(entity, new CatapultPrefabData
                    {
                        CatapultPrefab = GetEntity(authoring.CatapultPrefab, TransformUsageFlags.Dynamic)
                    });
                }

                if (authoring.CatapultProjectilePrefab != null)
                {
                    AddComponent(entity, new CatapultProjectilePrefabData
                    {
                        CatapultProjectilePrefab = GetEntity(authoring.CatapultProjectilePrefab, TransformUsageFlags.Dynamic)
                    });
                }
            }
        }
    }
}
