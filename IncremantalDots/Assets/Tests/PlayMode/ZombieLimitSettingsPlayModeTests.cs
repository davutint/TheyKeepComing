#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadWalls.Tests
{
    public class ZombieLimitSettingsPlayModeTests
    {
        private ZombieLimitPreset _originalPreset;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _originalPreset = GameplayPerformanceSettings.CurrentZombieLimitPreset;
            GameplayPerformanceSettings.CurrentZombieLimitPreset = ZombieLimitPreset.Balanced;
            RunPersistence.Delete();
            SceneManager.LoadScene("NewGameScene", LoadSceneMode.Single);

            for (int frame = 0; frame < 300; frame++)
            {
                if (GameManager.Instance != null
                    && GameManager.Instance.TryGetMobileCombatConfig(out _))
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("GameManager/MobileCastleCombatConfig 300 frame icinde hazir olmadi.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ClearDevelopmentHorde();
                GameManager.Instance.CompleteDevelopmentTestSession();
            }

            GameplayPerformanceSettings.CurrentZombieLimitPreset = _originalPreset;
            RunPersistence.Delete();
            yield return null;
        }

        [UnityTest]
        public IEnumerator RuntimeSetting_AppliesImmediatelyWithoutDespawningLivingZombies()
        {
            GameManager gameManager = GameManager.Instance;
            Assert.That(gameManager, Is.Not.Null);
            Assert.That(gameManager.ActiveZombieLimit,
                Is.EqualTo(GameplayPerformanceSettings.BalancedLimit));

            Assert.That(gameManager.TrySpawnDevelopmentHorde(
                DevelopmentTestRules.Horde2K,
                out int spawned,
                out string message), Is.True, message);
            Assert.That(spawned, Is.EqualTo(DevelopmentTestRules.Horde2K));

            EntityManager entityManager =
                World.DefaultGameObjectInjectionWorld.EntityManager;
            using EntityQuery zombies = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<ZombieTag>(),
                    ComponentType.ReadWrite<ZombieStats>()
                },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
            using (NativeArray<Entity> entities = zombies.ToEntityArray(Allocator.Temp))
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    ZombieStats stats = entityManager.GetComponentData<ZombieStats>(entities[i]);
                    stats.MaxHP = 1_000_000_000f;
                    stats.CurrentHP = stats.MaxHP;
                    stats.AttackDamage = 0f;
                    entityManager.SetComponentData(entities[i], stats);
                }
            }

            GameplayPerformanceSettings.CurrentZombieLimitPreset = ZombieLimitPreset.Balanced;
            Assert.That(gameManager.ApplyZombieLimitSetting(), Is.True);
            Assert.That(gameManager.ActiveZombieLimit,
                Is.EqualTo(GameplayPerformanceSettings.BalancedLimit));
            Assert.That(zombies.CalculateEntityCount(), Is.EqualTo(DevelopmentTestRules.Horde2K));

            yield return null;
            yield return null;

            Assert.That(zombies.CalculateEntityCount(), Is.EqualTo(DevelopmentTestRules.Horde2K),
                "Limit dusurmek sahadaki canli zombileri silmemeli.");
        }
    }
}
#endif
