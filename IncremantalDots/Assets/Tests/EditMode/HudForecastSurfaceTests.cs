using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class HudForecastSurfaceTests
    {
        private const string HudPrefabPath =
            "Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab";

        [Test]
        public void ActiveHudContract_ExcludesHordeForecastSurface()
        {
            string[] controllerFieldNames = typeof(HUDController)
                .GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Select(field => field.Name)
                .ToArray();
            Assert.That(controllerFieldNames.Any(name => name.StartsWith("HordePressure")), Is.False);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            string[] prefabObjectNames = prefab.GetComponentsInChildren<Transform>(true)
                .Select(transform => transform.name)
                .ToArray();
            Assert.That(prefabObjectNames.Any(name => name.StartsWith("HordePressure")), Is.False);
        }

        [Test]
        public void ContinuousSiegeData_RetainsGameplayPressureSignal()
        {
            FieldInfo field = typeof(ContinuousSiegeCycleData).GetField(
                nameof(ContinuousSiegeCycleData.HordePressure01));

            Assert.That(field, Is.Not.Null);
            Assert.That(field.FieldType, Is.EqualTo(typeof(float)));
        }
    }
}
