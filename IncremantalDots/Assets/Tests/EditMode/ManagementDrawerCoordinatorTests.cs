using NUnit.Framework;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class ManagementDrawerCoordinatorTests
    {
        [Test]
        public void Claim_KeepsOnlyRequestedManagementDrawerOpen()
        {
            GameObject root = new GameObject("ManagementDrawerCoordinatorTest");
            root.SetActive(false);
            try
            {
                MarketUI market = root.AddComponent<MarketUI>();
                market.ArcherDrawerPanel = CreateRect(root.transform, "ArcherDrawerPanel", new Vector2(540f, 350f));

                WorkerEconomyDrawerUI workers = root.AddComponent<WorkerEconomyDrawerUI>();
                workers.WorkerEconomyDrawerPanel = CreatePanel(root.transform, "WorkerEconomyDrawerPanel");

                ArrowSupplyUI arrows = root.AddComponent<ArrowSupplyUI>();
                arrows.AmmoPanel = CreatePanel(root.transform, "AmmoPurchasePanel");

                ManagementDrawerCoordinatorUI coordinator = root.AddComponent<ManagementDrawerCoordinatorUI>();

                market.SetDrawerOpen(true, true);
                Assert.That(coordinator.ActiveDrawer, Is.EqualTo(ManagementDrawerId.ArcherRecruitment));
                Assert.That(market.IsDrawerOpen, Is.True);
                Assert.That(workers.IsOpen, Is.False);
                Assert.That(arrows.IsOpen, Is.False);

                workers.SetOpen(true);
                Assert.That(coordinator.ActiveDrawer, Is.EqualTo(ManagementDrawerId.WorkersHousing));
                Assert.That(market.IsDrawerOpen, Is.False);
                Assert.That(workers.IsOpen, Is.True);
                Assert.That(arrows.IsOpen, Is.False);

                arrows.SetOpen(true);
                Assert.That(coordinator.ActiveDrawer, Is.EqualTo(ManagementDrawerId.ArrowSupply));
                Assert.That(market.IsDrawerOpen, Is.False);
                Assert.That(workers.IsOpen, Is.False);
                Assert.That(arrows.IsOpen, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CloseAll_ClosesEverySurfaceAndReleasesActiveDrawer()
        {
            GameObject root = new GameObject("ManagementDrawerCloseAllTest");
            root.SetActive(false);
            try
            {
                MarketUI market = root.AddComponent<MarketUI>();
                market.ArcherDrawerPanel = CreateRect(root.transform, "ArcherDrawerPanel", new Vector2(540f, 350f));
                WorkerEconomyDrawerUI workers = root.AddComponent<WorkerEconomyDrawerUI>();
                workers.WorkerEconomyDrawerPanel = CreatePanel(root.transform, "WorkerEconomyDrawerPanel");
                ArrowSupplyUI arrows = root.AddComponent<ArrowSupplyUI>();
                arrows.AmmoPanel = CreatePanel(root.transform, "AmmoPurchasePanel");
                ManagementDrawerCoordinatorUI coordinator = root.AddComponent<ManagementDrawerCoordinatorUI>();

                workers.SetOpen(true);
                coordinator.CloseAll();

                Assert.That(coordinator.ActiveDrawer, Is.EqualTo(ManagementDrawerId.None));
                Assert.That(market.IsDrawerOpen, Is.False);
                Assert.That(workers.IsOpen, Is.False);
                Assert.That(arrows.IsOpen, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 size)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            return rect;
        }

        private static GameObject CreatePanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            panel.SetActive(false);
            return panel;
        }
    }
}
