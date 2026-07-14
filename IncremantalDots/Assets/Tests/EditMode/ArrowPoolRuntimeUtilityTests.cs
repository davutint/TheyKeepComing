using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls.Tests
{
    public class ArrowPoolRuntimeUtilityTests
    {
        [Test]
        public void Pool_PrewarmExpandsRentsReturnsAndReusesWithoutDestroy()
        {
            using var world = new World("ArrowPoolRuntimeUtilityTests");
            EntityManager entityManager = world.EntityManager;

            Entity prefab = entityManager.CreateEntity(
                typeof(Prefab),
                typeof(ArrowTag),
                typeof(ArrowProjectile),
                typeof(ArrowPoolMember),
                typeof(LocalTransform),
                typeof(SpriteTint));
            entityManager.SetComponentData(prefab, new ArrowProjectile
            {
                Speed = 12f,
                Damage = 10f,
                SlowMultiplier = 1f,
                RemainingLifetime = ArrowProjectile.DefaultLifetimeSeconds
            });
            entityManager.SetComponentData(prefab, LocalTransform.Identity);

            Entity pool = entityManager.CreateEntity(typeof(ArrowPoolRuntimeData));
            entityManager.SetComponentData(pool, new ArrowPoolRuntimeData
            {
                PrewarmTarget = 2,
                ExpandBatch = 2
            });
            entityManager.AddBuffer<ArrowPoolAvailable>(pool);

            Assert.That(ArrowPoolRuntimeUtility.EnsureInitialized(
                entityManager, pool, prefab), Is.True);
            ArrowPoolRuntimeData initialized =
                entityManager.GetComponentData<ArrowPoolRuntimeData>(pool);
            Assert.That(initialized.TotalCreated, Is.EqualTo(2));
            Assert.That(initialized.AvailableCount, Is.EqualTo(2));
            Assert.That(initialized.ActiveCount, Is.Zero);

            Assert.That(ArrowPoolRuntimeUtility.TryRent(
                entityManager, pool, prefab, out Entity first), Is.True);
            Assert.That(ArrowPoolRuntimeUtility.TryRent(
                entityManager, pool, prefab, out _), Is.True);
            Assert.That(ArrowPoolRuntimeUtility.TryRent(
                entityManager, pool, prefab, out Entity expandedArrow), Is.True);

            ArrowPoolRuntimeData expanded =
                entityManager.GetComponentData<ArrowPoolRuntimeData>(pool);
            Assert.That(expanded.TotalCreated, Is.EqualTo(4));
            Assert.That(expanded.ExpansionCount, Is.EqualTo(1));
            Assert.That(expanded.ActiveCount, Is.EqualTo(3));
            Assert.That(entityManager.IsComponentEnabled<ArrowTag>(first), Is.True);

            entityManager.SetComponentData(first, new ArrowProjectile
            {
                Speed = 99f,
                Damage = 55f,
                Target = expandedArrow,
                RemainingLifetime = 1f
            });
            entityManager.SetComponentData(first,
                LocalTransform.FromPositionRotationScale(new float3(3f, 2f, -2.5f), quaternion.identity, 1f));

            Assert.That(ArrowPoolRuntimeUtility.Return(entityManager, pool, first), Is.True);
            Assert.That(entityManager.Exists(first), Is.True);
            Assert.That(entityManager.IsComponentEnabled<ArrowTag>(first), Is.False);
            Assert.That(entityManager.GetComponentData<LocalTransform>(first).Scale, Is.Zero);
            Assert.That(entityManager.GetComponentData<ArrowProjectile>(first).Target, Is.EqualTo(Entity.Null));

            Assert.That(ArrowPoolRuntimeUtility.TryRent(
                entityManager, pool, prefab, out Entity reused), Is.True);
            Assert.That(reused, Is.EqualTo(first));
            Assert.That(entityManager.Exists(reused), Is.True);
            Assert.That(entityManager.IsComponentEnabled<ArrowTag>(reused), Is.True);
            Assert.That(entityManager.GetComponentData<ArrowProjectile>(reused).RemainingLifetime,
                Is.EqualTo(ArrowProjectile.DefaultLifetimeSeconds));
        }
    }
}
