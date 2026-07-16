using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls.Tests
{
    public class EnemyPoolRuntimeUtilityTests
    {
        [Test]
        public void Pool_PrewarmExpandsRentsReturnsAndResetsTransientState()
        {
            using var world = new World("EnemyPoolRuntimeUtilityTests");
            EntityManager entityManager = world.EntityManager;

            Entity prefab = entityManager.CreateEntity(
                typeof(Prefab),
                typeof(ZombieTag),
                typeof(ZombieStats),
                typeof(ZombieState),
                typeof(ZombieSlow),
                typeof(DeathTimer),
                typeof(PhysicsBody),
                typeof(LocalTransform),
                typeof(SpriteTint),
                typeof(SpriteAnimation));
            entityManager.SetComponentData(prefab, new ZombieStats
            {
                MoveSpeed = 0.85f,
                MaxHP = 20f,
                CurrentHP = 20f,
                AttackDamage = 5f,
                AttackCooldown = 1f,
                XPReward = 10
            });
            entityManager.SetComponentData(prefab, LocalTransform.Identity);
            entityManager.SetComponentData(prefab, new SpriteAnimation
            {
                TotalColumns = 15,
                TotalRows = 32,
                DirectionRow = 4,
                FrameCount = 15,
                CurrentFrame = 0,
                FrameTimer = 0f,
                FrameInterval = 0.1f
            });
            entityManager.SetComponentEnabled<ZombieSlow>(prefab, false);
            entityManager.SetComponentEnabled<DeathTimer>(prefab, false);

            Entity pool = entityManager.CreateEntity(
                typeof(EnemyCatalogRuntimeData), typeof(EnemyPoolRuntimeData));
            entityManager.SetComponentData(pool,
                new EnemyCatalogRuntimeData { EntryCount = 1, ActiveEntryIndex = 0 });
            entityManager.SetComponentData(pool, new EnemyPoolRuntimeData
            {
                ActiveEntryIndex = 0,
                PrewarmTarget = 2,
                ExpandBatch = 2
            });
            entityManager.AddBuffer<EnemyCatalogEntryData>(pool).Add(new EnemyCatalogEntryData
            {
                Prefab = prefab,
                BaseHP = 20f,
                BaseDamage = 5f,
                BaseMoveSpeed = 0.85f,
                Scale = 1.4f,
                XPReward = 10,
                PoolPrewarm = 2,
                PoolExpandBatch = 2
            });
            entityManager.AddBuffer<EnemyPoolAvailable>(pool);

            Assert.That(EnemyPoolRuntimeUtility.EnsureInitialized(entityManager, pool), Is.True);
            var initialized = entityManager.GetComponentData<EnemyPoolRuntimeData>(pool);
            Assert.That(initialized.TotalCreated, Is.EqualTo(2));
            Assert.That(initialized.AvailableCount, Is.EqualTo(2));

            Assert.That(EnemyPoolRuntimeUtility.TryRent(entityManager, pool, out _), Is.True);
            Assert.That(EnemyPoolRuntimeUtility.TryRent(entityManager, pool, out _), Is.True);
            Assert.That(EnemyPoolRuntimeUtility.TryRent(entityManager, pool, out Entity third), Is.True);
            var expanded = entityManager.GetComponentData<EnemyPoolRuntimeData>(pool);
            Assert.That(expanded.TotalCreated, Is.EqualTo(4));
            Assert.That(expanded.ExpansionCount, Is.EqualTo(1));
            Assert.That(expanded.ActiveCount, Is.EqualTo(3));

            uint firstGeneration = entityManager.GetComponentData<EnemyPoolMember>(third).Generation;
            entityManager.SetComponentData(third, new ZombieState { Value = ZombieStateType.Dead });
            entityManager.SetComponentData(third, new ZombieSlow { Duration = 5f, SpeedMultiplier = 0.2f });
            entityManager.SetComponentEnabled<ZombieSlow>(third, true);
            entityManager.SetComponentData(third, new DeathTimer { Value = 2f });
            entityManager.SetComponentEnabled<DeathTimer>(third, true);
            entityManager.SetComponentData(third, new PhysicsBody
            {
                Velocity = new float2(4f, 2f),
                Force = new float2(3f, 1f),
                Mass = 1f,
                Damping = 3f
            });

            Assert.That(EnemyPoolRuntimeUtility.Return(entityManager, pool, third), Is.True);
            Assert.That(entityManager.IsComponentEnabled<ZombieTag>(third), Is.False);
            Assert.That(entityManager.IsComponentEnabled<ZombieSlow>(third), Is.False);
            Assert.That(entityManager.IsComponentEnabled<DeathTimer>(third), Is.False);
            Assert.That(entityManager.GetComponentData<LocalTransform>(third).Scale, Is.Zero);
            Assert.That(entityManager.GetComponentData<PhysicsBody>(third).Velocity, Is.EqualTo(float2.zero));

            Assert.That(EnemyPoolRuntimeUtility.TryRent(entityManager, pool, out Entity reused), Is.True);
            Assert.That(reused, Is.EqualTo(third));
            Assert.That(entityManager.GetComponentData<EnemyPoolMember>(reused).Generation,
                Is.Not.EqualTo(firstGeneration));
            Assert.That(entityManager.GetComponentData<ZombieState>(reused).Value,
                Is.EqualTo(ZombieStateType.Moving));
            SpriteAnimation reusedAnimation = entityManager.GetComponentData<SpriteAnimation>(reused);
            Assert.That(reusedAnimation.CurrentFrame, Is.InRange(0, 14));
            Assert.That(reusedAnimation.FrameTimer, Is.GreaterThan(0f).And.LessThan(0.1f));
            Assert.That(reusedAnimation.FrameInterval, Is.EqualTo(0.1f).Within(0.0001f));

            int totalBeforeChurn = entityManager.GetComponentData<EnemyPoolRuntimeData>(pool).TotalCreated;
            Entity churnEntity = reused;
            for (int i = 0; i < 512; i++)
            {
                Assert.That(EnemyPoolRuntimeUtility.Return(entityManager, pool, churnEntity), Is.True);
                Assert.That(EnemyPoolRuntimeUtility.TryRent(entityManager, pool, out churnEntity), Is.True);
            }
            Assert.That(entityManager.GetComponentData<EnemyPoolRuntimeData>(pool).TotalCreated,
                Is.EqualTo(totalBeforeChurn));
        }
    }
}
