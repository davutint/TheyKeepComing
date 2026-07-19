using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DeadWalls.Tests
{
    public sealed class HeartProductionRuntimePlayModeTests
    {
        [UnityTest]
        public IEnumerator NewGameScene_UsesGeneratedValidatedProductionHeartAndReviewedIcons()
        {
            SceneManager.LoadScene("NewGameScene", LoadSceneMode.Single);

            GameManager manager = null;
            GameplayHUDToolkitUI toolkit = null;
            for (int frame = 0; frame < 180 && (manager == null || toolkit == null); frame++)
            {
                manager = GameManager.Instance;
                toolkit = Object.FindFirstObjectByType<GameplayHUDToolkitUI>(FindObjectsInactive.Include);
                yield return null;
            }

            Assert.That(manager, Is.Not.Null);
            Assert.That(toolkit, Is.Not.Null);
            Assert.That(manager.HeartCatalog, Is.Not.Null);
            Assert.That(manager.HeartCatalog.CatalogVersion, Is.EqualTo(2));
            Assert.That(manager.HeartCatalog.Nodes, Has.Length.EqualTo(37));
            Assert.That(manager.HeartCatalog.Nodes, Has.All.Matches<HeartNodeDefinitionSO>(node =>
                node != null && node.Icon != null));

            HeartGraphPresentation presentation = null;
            IReadOnlyList<string> errors = null;
            bool built = false;
            for (int frame = 0; frame < 300 && !built; frame++)
            {
                built = manager.TryBuildHeartPresentation(out presentation, out errors);
                if (!built)
                    yield return null;
            }

            Assert.That(built, Is.True, errors == null ? string.Empty : string.Join(" | ", errors));
            Assert.That(errors, Is.Empty);
            Assert.That(presentation.Nodes.Count, Is.InRange(17, 21));
            Assert.That(presentation.Nodes.Count(node => node.IsExactContentVisible), Is.EqualTo(5));
            Assert.That(presentation.Nodes.Count(node => node.IsExactContentVisible && node.IsRoot), Is.EqualTo(1));
            Assert.That(presentation.Nodes.Count(node => node.IsExactContentVisible && node.Depth == 1), Is.EqualTo(4));

            HeartRuntimeTuningTelemetry telemetry = manager.GetHeartRuntimeTuningTelemetry();
            Assert.That(telemetry.HasCatalog, Is.True);
            Assert.That(telemetry.RuntimeReady, Is.True, telemetry.RuntimeError);
            Assert.That(telemetry.RuntimeError, Is.Empty);
            Assert.That(telemetry.CatalogVersion, Is.EqualTo(2));
            Assert.That(telemetry.NodeCount, Is.EqualTo(presentation.Nodes.Count));

            VisualElement root = toolkit.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(root.Q<VisualElement>("heartScreen"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("heartGraphContent"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>(className: "heart-legend"), Is.Null);
            Assert.That(root.Q<Button>("heartQuantityTen"), Is.Null);
            Assert.That(root.Q<Button>("heartQuantityMax"), Is.Null);
        }

        [UnityTest]
        public IEnumerator NewGameScene_RendersOnlyVisibleToolkitNodesAndDirectReveal()
        {
            SceneManager.LoadScene("NewGameScene", LoadSceneMode.Single);

            GameManager manager = null;
            GameplayHUDToolkitUI toolkit = null;
            for (int frame = 0; frame < 180 && (manager == null || toolkit == null); frame++)
            {
                manager = GameManager.Instance;
                toolkit = Object.FindFirstObjectByType<GameplayHUDToolkitUI>(FindObjectsInactive.Include);
                yield return null;
            }

            Assert.That(manager, Is.Not.Null);
            Assert.That(toolkit, Is.Not.Null);

            bool granted = false;
            for (int frame = 0; frame < 300 && !granted; frame++)
            {
                granted = manager.GrantGraveEssence(1_000_000_000L);
                if (!granted)
                    yield return null;
            }
            Assert.That(granted, Is.True, "Grave Essence ECS owner'i hazir olmadi.");

            Assert.That(manager.TryBuildHeartPresentation(
                    out HeartGraphPresentation before,
                    out IReadOnlyList<string> beforeErrors),
                Is.True,
                beforeErrors == null ? string.Empty : string.Join(" | ", beforeErrors));

            HeartGraphNodePresentation candidate = before.Nodes.First(node =>
                node.IsExactContentVisible && !node.IsRoot && node.Level == 0);
            HashSet<string> hiddenDirectTargetSlots = before.Edges
                .Where(edge => edge.FromSlotId == candidate.SlotId)
                .Select(edge => edge.ToSlotId)
                .Where(slot => before.Nodes.Any(node => node.SlotId == slot && !node.IsExactContentVisible))
                .ToHashSet();
            Assert.That(hiddenDirectTargetSlots, Is.Not.Empty);

            OpenHeartToolkit(toolkit);
            yield return null;

            VisualElement root = toolkit.GetComponent<UIDocument>().rootVisualElement;
            VisualElement graphContent = root.Q<VisualElement>("heartGraphContent");
            Assert.That(graphContent.Query<Button>(className: "heart-tech-node").ToList(), Has.Count.EqualTo(5));
            Assert.That(SimulationPauseService.ActiveLeaseCount, Is.Zero);
            Assert.That(SimulationPauseService.IsPaused, Is.False);

            HeartPurchaseResult result = manager.TryPurchaseHeartNode(
                candidate.ExactNodeId,
                HeartPurchaseQuantity.One);
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.KeystoneConflictApplied, Is.False);
            Assert.That(result.NewlyRevealedNodeIds, Is.Not.Empty);

            RebuildHeartToolkit(toolkit);
            yield return null;

            Assert.That(manager.TryBuildHeartPresentation(
                    out HeartGraphPresentation after,
                    out IReadOnlyList<string> afterErrors),
                Is.True,
                afterErrors == null ? string.Empty : string.Join(" | ", afterErrors));
            HashSet<string> actualNewSlots = after.Nodes
                .Where(node => !string.IsNullOrWhiteSpace(node.ExactNodeId)
                               && result.NewlyRevealedNodeIds.Contains(node.ExactNodeId))
                .Select(node => node.SlotId)
                .ToHashSet();
            Assert.That(actualNewSlots, Is.EquivalentTo(hiddenDirectTargetSlots));

            int visibleCount = after.Nodes.Count(node => node.IsExactContentVisible);
            Assert.That(
                graphContent.Query<Button>(className: "heart-tech-node").ToList(),
                Has.Count.EqualTo(visibleCount));
            foreach (HeartGraphNodePresentation hidden in after.Nodes.Where(node => !node.IsExactContentVisible))
            {
                Assert.That(
                    graphContent.Q<Button>("heartNode-" + hidden.SlotId.Replace(':', '-')),
                    Is.Null,
                    $"Hidden slot visual tree'ye sizdi: {hidden.SlotId}");
            }

            yield return new WaitForSecondsRealtime(0.7f);
            foreach (string revealedSlot in actualNewSlots)
            {
                Button revealed = graphContent.Q<Button>("heartNode-" + revealedSlot.Replace(':', '-'));
                Assert.That(revealed, Is.Not.Null);
                Assert.That(revealed.ClassListContains("is-revealing"), Is.False);
            }
            Assert.That(SimulationPauseService.ActiveLeaseCount, Is.Zero);
            Assert.That(SimulationPauseService.IsPaused, Is.False);
        }

        private static void OpenHeartToolkit(GameplayHUDToolkitUI toolkit)
        {
            System.Type type = typeof(GameplayHUDToolkitUI);
            System.Type surfaceType = type.GetNestedType("SurfaceKind", BindingFlags.NonPublic);
            MethodInfo toggle = type.GetMethod(
                "ToggleSurface",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(surfaceType, Is.Not.Null);
            Assert.That(toggle, Is.Not.Null);
            toggle.Invoke(toolkit, new[] { System.Enum.Parse(surfaceType, "Heart") });
        }

        private static void RebuildHeartToolkit(GameplayHUDToolkitUI toolkit)
        {
            MethodInfo rebuild = typeof(GameplayHUDToolkitUI).GetMethod(
                "RebuildHeartGraph",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(rebuild, Is.Not.Null);
            rebuild.Invoke(toolkit, new object[] { true });
        }
    }
}
