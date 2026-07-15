using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls.Tests
{
    public class HudCouncilCardPresentationTests
    {
        private const string HudPrefabPath =
            "Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab";

        [Test]
        public void ActiveHudPrefab_UsesSingleCompactCouncilDecisionCard()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            RectTransform panel = FindUnique<RectTransform>(prefab, "CouncilEventPanel");
            Assert.That(panel.gameObject.activeSelf, Is.False,
                "Council karti regular event yokken kapali baslamali.");
            Assert.That(panel.sizeDelta, Is.EqualTo(new Vector2(420f, 236f)));
            Assert.That(panel.anchoredPosition, Is.EqualTo(new Vector2(-700f, -140f)));

            Button optionA = FindUnique<Button>(prefab, "CouncilOptionAButton");
            Button optionB = FindUnique<Button>(prefab, "CouncilOptionBButton");
            Assert.That(optionA.GetComponent<RectTransform>().sizeDelta,
                Is.EqualTo(new Vector2(396f, 44f)));
            Assert.That(optionB.GetComponent<RectTransform>().sizeDelta,
                Is.EqualTo(new Vector2(396f, 44f)));
            Assert.That(optionA.transform.parent, Is.SameAs(panel));
            Assert.That(optionB.transform.parent, Is.SameAs(panel));
        }

        [Test]
        public void CouncilDecisionCard_HasTwoExactQuoteSurfacesAndWorkingCountdownBar()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            Component optionA = FindUniqueText(prefab, "CouncilOptionAText");
            Component optionB = FindUniqueText(prefab, "CouncilOptionBText");
            Component timer = FindUniqueText(prefab, "CouncilTimerText");
            Image timerFill = FindUnique<Image>(prefab, "CouncilTimerFill");

            Assert.That(ReadProperty<bool>(optionA, "richText"), Is.True);
            Assert.That(ReadProperty<bool>(optionB, "richText"), Is.True);
            Assert.That(ReadProperty<object>(optionA, "font"), Is.Not.Null);
            Assert.That(ReadProperty<object>(optionB, "font"), Is.Not.Null);
            Assert.That(timer.gameObject.activeSelf, Is.True);
            Assert.That(ReadProperty<object>(timer, "font"), Is.Not.Null);
            Assert.That(ReadProperty<string>(timer, "text"), Does.StartWith("DECIDE"));
            Assert.That(timerFill.gameObject.activeSelf, Is.True);
            Assert.That(timerFill.type, Is.EqualTo(Image.Type.Filled));
            Assert.That(timerFill.fillMethod, Is.EqualTo(Image.FillMethod.Horizontal));
            Assert.That(timerFill.fillOrigin, Is.EqualTo((int)Image.OriginHorizontal.Left));
            Assert.That(timerFill.raycastTarget, Is.False);
        }

        private static T FindUnique<T>(GameObject root, string objectName) where T : Component
        {
            T[] matches = root.GetComponentsInChildren<T>(true)
                .Where(component => component.gameObject.name == objectName)
                .ToArray();
            Assert.That(matches.Length, Is.EqualTo(1),
                $"{objectName} aktif HUD prefabinda tam bir kez bulunmali.");
            return matches[0];
        }

        private static Component FindUniqueText(GameObject root, string objectName)
        {
            Component[] matches = root.GetComponentsInChildren<Component>(true)
                .Where(component => component.gameObject.name == objectName
                    && component.GetType().Name == "TextMeshProUGUI")
                .ToArray();
            Assert.That(matches.Length, Is.EqualTo(1),
                $"{objectName} aktif HUD prefabinda tam bir kez bulunmali.");
            return matches[0];
        }

        private static T ReadProperty<T>(Component component, string propertyName)
        {
            object value = component.GetType().GetProperty(propertyName).GetValue(component);
            return (T)value;
        }
    }
}
