using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadWalls.Tests
{
    public sealed class HeartProductionRuntimePlayModeTests
    {
        [UnityTest]
        public IEnumerator NewGameScene_UsesGeneratedValidatedProductionHeart()
        {
            SceneManager.LoadScene("NewGameScene", LoadSceneMode.Single);

            GameManager manager = null;
            for (int frame = 0; frame < 180 && manager == null; frame++)
            {
                manager = GameManager.Instance;
                yield return null;
            }

            Assert.That(manager, Is.Not.Null);
            Assert.That(manager.HeartCatalog, Is.Not.Null);
            Assert.That(manager.HeartCatalog.CatalogVersion, Is.EqualTo(2));
            Assert.That(manager.HeartCatalog.Nodes, Has.Length.EqualTo(37));

            HeartGraphPresentation presentation = null;
            System.Collections.Generic.IReadOnlyList<string> errors = null;
            bool built = false;
            for (int frame = 0; frame < 300 && !built; frame++)
            {
                built = manager.TryBuildHeartPresentation(out presentation, out errors);
                if (!built)
                    yield return null;
            }
            Assert.That(built, Is.True,
                errors == null ? string.Empty : string.Join(" | ", errors));
            Assert.That(errors, Is.Empty);
            Assert.That(presentation.Nodes.Count, Is.InRange(17, 21));

            HeartRuntimeTuningTelemetry telemetry = manager.GetHeartRuntimeTuningTelemetry();
            Assert.That(telemetry.HasCatalog, Is.True);
            Assert.That(telemetry.RuntimeReady, Is.True, telemetry.RuntimeError);
            Assert.That(telemetry.RuntimeError, Is.Empty);
            Assert.That(telemetry.CatalogVersion, Is.EqualTo(2));
            Assert.That(telemetry.NodeCount, Is.EqualTo(presentation.Nodes.Count));

            HeartScreenUI screen = Object.FindFirstObjectByType<HeartScreenUI>();
            Assert.That(screen, Is.Not.Null);
            Assert.That(screen.NodeSize, Is.EqualTo(new Vector2(264f, 156f)));
            Assert.That(screen.IconSlotSize, Is.EqualTo(new Vector2(52f, 52f)));
            Assert.That(screen.ShowAuthoredNodeIcons, Is.False,
                "Owner ikonlari hazir olmadan Castle Heart gecici sprite veya glyph gostermemeli.");
        }

        [UnityTest]
        public IEnumerator NewGameScene_PresentsKeystoneAsARealTwoCardCommitment()
        {
            SceneManager.LoadScene("NewGameScene", LoadSceneMode.Single);
            yield return null;

            GameManager manager = null;
            for (int frame = 0; frame < 180 && manager == null; frame++)
            {
                manager = GameManager.Instance;
                yield return null;
            }

            Assert.That(manager, Is.Not.Null);
            bool granted = false;
            for (int frame = 0; frame < 300 && !granted; frame++)
            {
                granted = manager.GrantGraveEssence(1_000_000_000L);
                if (!granted)
                    yield return null;
            }
            Assert.That(granted, Is.True, "Grave Essence ECS owner'i hazir olmadi.");

            HeartGraphPresentation presentation = null;
            HeartGraphNodePresentation first = null;
            HeartGraphNodePresentation second = null;
            for (int pass = 0; pass < 48 && first == null; pass++)
            {
                Assert.That(manager.TryBuildHeartPresentation(
                        out presentation,
                        out System.Collections.Generic.IReadOnlyList<string> errors),
                    Is.True,
                    errors == null ? string.Empty : string.Join(" | ", errors));

                first = presentation.Nodes.FirstOrDefault(node =>
                    node.IsExactContentVisible
                    && node.Type == HeartNodeType.Keystone
                    && node.Level == 0
                    && node.LockState == HeartNodeLockState.Available
                    && node.KeystoneConflict != null
                    && node.KeystoneConflict.ConflictingChoiceIsRevealed);
                if (first != null)
                {
                    second = presentation.Nodes.Single(node =>
                        node.SlotId == first.KeystoneConflict.ConflictingChoiceSlotId);
                    break;
                }

                HeartGraphNodePresentation[] candidates = presentation.Nodes
                    .Where(node =>
                        node.IsExactContentVisible
                        && !node.IsRoot
                        && node.Level == 0
                        && node.LockState == HeartNodeLockState.Available
                        && node.Type != HeartNodeType.Keystone)
                    .ToArray();
                Assert.That(candidates, Is.Not.Empty,
                    "Keystone ciftine ilerleyecek acik Heart node'u kalmadi.");

                bool progressed = false;
                for (int i = 0; i < candidates.Length; i++)
                {
                    HeartPurchaseResult purchase = manager.TryPurchaseHeartNode(
                        candidates[i].ExactNodeId,
                        HeartPurchaseQuantity.One);
                    progressed |= purchase != null && purchase.Succeeded;
                }
                Assert.That(progressed, Is.True, "Heart graph ilerlemesi satin alim uretemedi.");
                yield return null;
            }

            Assert.That(first, Is.Not.Null, "Production graph'ta gorunur Keystone cifti bulunamadi.");
            Assert.That(second, Is.Not.Null);
            Assert.That(second.IsExactContentVisible, Is.True);
            Assert.That(second.Type, Is.EqualTo(HeartNodeType.Keystone));
            Assert.That(second.Branch, Is.EqualTo(first.Branch));
            Assert.That(second.KeystoneConflict, Is.Not.Null);
            Assert.That(second.KeystoneConflict.ConflictingChoiceSlotId, Is.EqualTo(first.SlotId));

            HeartScreenUI screen = Object.FindFirstObjectByType<HeartScreenUI>();
            Assert.That(screen, Is.Not.Null);
            screen.OpenPanel();
            yield return null;

            RectTransform firstCard = screen.HeartContent.Find(
                "HeartNode_" + first.SlotId.Replace(':', '_')) as RectTransform;
            RectTransform secondCard = screen.HeartContent.Find(
                "HeartNode_" + second.SlotId.Replace(':', '_')) as RectTransform;
            Assert.That(firstCard, Is.Not.Null);
            Assert.That(secondCard, Is.Not.Null);
            RectTransform iconSocket = firstCard.Find("HeartNodeIconSocket") as RectTransform;
            Assert.That(iconSocket, Is.Not.Null);
            Assert.That(iconSocket.sizeDelta, Is.EqualTo(screen.IconSlotSize));
            Transform iconImage = firstCard.Find("HeartNodeIconImage");
            Assert.That(iconImage, Is.Not.Null);
            Assert.That(iconImage.gameObject.activeSelf, Is.False,
                "Authored icon surface owner onayina kadar bos kalmali.");
            TMPro.TMP_Text iconFallback = firstCard
                .GetComponentsInChildren<TMPro.TMP_Text>(true)
                .Single(text => text.name == "HeartNodeIconFallbackText");
            Assert.That(iconFallback.gameObject.activeSelf, Is.False);
            Assert.That(iconFallback.text, Is.Empty);
            Assert.That(screen.HeartContent.Cast<Transform>().Any(child =>
                child.name.StartsWith("HeartAxis_", System.StringComparison.Ordinal)), Is.False,
                "Sert pusula ekseni yerine yalniz gercek graph damarlarinin cizilmesi gerekiyor.");
            bool horizontalBranch = first.Branch == HeartNodeBranch.Army
                                    || first.Branch == HeartNodeBranch.Defense;
            if (horizontalBranch)
            {
                Assert.That(Mathf.Abs(firstCard.anchoredPosition.y - secondCard.anchoredPosition.y),
                    Is.GreaterThan(screen.NodeSize.y));
                Assert.That(firstCard.anchoredPosition.x,
                    Is.EqualTo(secondCard.anchoredPosition.x).Within(0.1f));
            }
            else
            {
                Assert.That(Mathf.Abs(firstCard.anchoredPosition.x - secondCard.anchoredPosition.x),
                    Is.GreaterThan(screen.NodeSize.x));
                Assert.That(firstCard.anchoredPosition.y,
                    Is.EqualTo(secondCard.anchoredPosition.y).Within(0.1f));
            }
            Assert.That(screen.HeartContent.Cast<Transform>().Any(child =>
                child.name.StartsWith("HeartKeystoneChoice_", System.StringComparison.Ordinal)), Is.True);

            string chosenId = first.ExactNodeId;
            string lockedId = second.ExactNodeId;
            HeartPurchaseResult result = manager.TryPurchaseHeartNode(
                chosenId,
                HeartPurchaseQuantity.One);
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.KeystoneConflictApplied, Is.True);
            Assert.That(manager.TryBuildHeartPresentation(
                    out HeartGraphPresentation afterPurchase,
                    out System.Collections.Generic.IReadOnlyList<string> afterErrors),
                Is.True,
                afterErrors == null ? string.Empty : string.Join(" | ", afterErrors));
            HeartGraphNodePresentation chosen = afterPurchase.Nodes.Single(node =>
                node.ExactNodeId == chosenId);
            HeartGraphNodePresentation locked = afterPurchase.Nodes.Single(node =>
                node.ExactNodeId == lockedId);
            Assert.That(chosen.Level, Is.EqualTo(1));
            Assert.That(locked.LockState, Is.EqualTo(HeartNodeLockState.KeystoneConflict));
            Assert.That(locked.KeystoneConflict.SourceIsLockedByConflictingChoice, Is.True);
            screen.ClosePanel();
            yield return null;
            Assert.That(SimulationPauseService.ActiveLeaseCount, Is.Zero);
            Assert.That(SimulationPauseService.IsPaused, Is.False);
            Assert.That(screen.HasActiveOwnedTweens, Is.False,
                "Heart kapanisinda owned UI tween'leri yasamaya devam etmemeli.");
        }
    }
}
