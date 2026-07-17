using System.Collections;
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
            Assert.That(manager.HeartCatalog.CatalogVersion, Is.EqualTo(1));
            Assert.That(manager.HeartCatalog.Nodes, Has.Length.EqualTo(35));

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
            Assert.That(telemetry.CatalogVersion, Is.EqualTo(1));
            Assert.That(telemetry.NodeCount, Is.EqualTo(presentation.Nodes.Count));

            HeartScreenUI screen = Object.FindFirstObjectByType<HeartScreenUI>();
            Assert.That(screen, Is.Not.Null);
            Assert.That(screen.NodeSize.x, Is.GreaterThanOrEqualTo(292f));
            Assert.That(screen.NodeSize.y, Is.GreaterThanOrEqualTo(188f));
        }
    }
}
