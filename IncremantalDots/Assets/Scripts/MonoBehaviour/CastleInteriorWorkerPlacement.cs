using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace DeadWalls
{
    public class CastleInteriorWorkerPlacement : MonoBehaviour
    {
        public const string RootName = "CastleInteriorEconomyArea";
        public const string SpawnRootName = "WorkerSpawnPoints";
        public const string HubName = "CastleWorkerHub";
        public const string DeliveryRootName = "DeliveryPoints";

        [Header("Sites")]
        public Transform WoodWorkerSpawnRoot;
        public Transform StoneWorkerSpawnRoot;
        public Transform IronWorkerSpawnRoot;
        public Transform FoodWorkerSpawnRoot;
        public Transform HubDeliveryRoot;

        [Header("Placement")]
        public float SpawnZ = MobileCastleRenderDepth.UnitZ;
        public float RepeatOffsetRadius = 0.12f;

        [Header("Visible Route Corridor")]
        public float RouteCorridorX = -0.9f;
        public float HubApproachY = 0.6f;
        public float RouteLaneSpacing = 0.1f;
        public int RouteLaneCount = 5;

        private readonly List<Vector3> _woodPoints = new List<Vector3>();
        private readonly List<Vector3> _stonePoints = new List<Vector3>();
        private readonly List<Vector3> _ironPoints = new List<Vector3>();
        private readonly List<Vector3> _foodPoints = new List<Vector3>();
        private readonly List<Vector3> _hubPoints = new List<Vector3>();
        private bool _cacheDirty = true;

        private void OnValidate()
        {
            _cacheDirty = true;
        }

        private void OnTransformChildrenChanged()
        {
            _cacheDirty = true;
        }

        public static CastleInteriorWorkerPlacement GetOrCreateRuntime()
        {
            var placement = FindObjectOfType<CastleInteriorWorkerPlacement>();
            if (placement != null)
                return placement;

            GameObject root = GameObject.Find(RootName);
            return root != null ? root.AddComponent<CastleInteriorWorkerPlacement>() : null;
        }

        public bool TryGetSpawnPosition(EconomyFocusType resource, int workerIndex, out float3 position)
        {
            position = default;
            resource = EconomyFocusUtility.Normalize(resource);
            if (resource == EconomyFocusType.Balanced)
                return false;

            EnsureCache();
            List<Vector3> points = GetPoints(resource);
            if (points == null || points.Count == 0)
                return false;

            int baseIndex = Mathf.Abs(workerIndex) % points.Count;
            int stackIndex = Mathf.Abs(workerIndex) / points.Count;
            Vector3 point = points[baseIndex] + GetRepeatOffset(stackIndex);
            position = new float3(point.x, point.y, SpawnZ);
            return true;
        }

        public bool TryGetLogisticsPositions(EconomyFocusType resource, int workerIndex, out float3 pickup, out float3 delivery)
        {
            pickup = default;
            delivery = default;

            if (!TryGetSpawnPosition(resource, workerIndex, out pickup))
                return false;

            EnsureCache();
            if (_hubPoints.Count == 0)
                return false;

            int deliveryIndex = (Mathf.Abs(workerIndex) + GetResourceDeliveryOffset(resource)) % _hubPoints.Count;
            int stackIndex = Mathf.Abs(workerIndex) / _hubPoints.Count;
            Vector3 point = _hubPoints[deliveryIndex] + GetRepeatOffset(stackIndex) * 0.7f;
            delivery = new float3(point.x, point.y, SpawnZ);
            return true;
        }

        public bool TryGetLogisticsRoutePositions(
            EconomyFocusType resource,
            int workerIndex,
            out float3 pickup,
            out float3 siteApproach,
            out float3 hubApproach,
            out float3 delivery)
        {
            siteApproach = default;
            hubApproach = default;
            if (!TryGetLogisticsPositions(resource, workerIndex, out pickup, out delivery))
                return false;

            int laneCount = Mathf.Max(1, RouteLaneCount);
            int laneIndex = Mathf.Abs(workerIndex) % laneCount;
            float centeredLane = laneIndex - (laneCount - 1) * 0.5f;
            float corridorX = RouteCorridorX + centeredLane * Mathf.Max(0f, RouteLaneSpacing);

            siteApproach = new float3(corridorX, pickup.y, SpawnZ);
            hubApproach = new float3(corridorX, HubApproachY, SpawnZ);
            return true;
        }

        private void EnsureCache()
        {
            if (!_cacheDirty)
                return;

            ResolveMissingRoots();
            RebuildPoints(WoodWorkerSpawnRoot, _woodPoints);
            RebuildPoints(StoneWorkerSpawnRoot, _stonePoints);
            RebuildPoints(IronWorkerSpawnRoot, _ironPoints);
            RebuildPoints(FoodWorkerSpawnRoot, _foodPoints);
            RebuildPoints(HubDeliveryRoot, _hubPoints);
            _cacheDirty = false;
        }

        private void ResolveMissingRoots()
        {
            Transform root = transform;
            if (root == null || root.name != RootName)
            {
                GameObject rootObject = GameObject.Find(RootName);
                if (rootObject != null)
                    root = rootObject.transform;
            }

            if (root == null)
                return;

            WoodWorkerSpawnRoot ??= FindSiteSpawnRoot(root, "WoodSite");
            StoneWorkerSpawnRoot ??= FindSiteSpawnRoot(root, "StoneSite");
            IronWorkerSpawnRoot ??= FindSiteSpawnRoot(root, "IronSite");
            FoodWorkerSpawnRoot ??= FindSiteSpawnRoot(root, "FoodSite");
            HubDeliveryRoot ??= FindHubDeliveryRoot(root);
        }

        private static Transform FindHubDeliveryRoot(Transform root)
        {
            Transform hub = FindChildRecursive(root, HubName);
            if (hub == null)
                return null;

            Transform deliveryRoot = FindChildRecursive(hub, DeliveryRootName);
            return deliveryRoot != null ? deliveryRoot : hub;
        }

        private static Transform FindSiteSpawnRoot(Transform root, string siteName)
        {
            Transform site = FindChildRecursive(root, siteName);
            if (site == null)
                return null;

            Transform spawnRoot = FindChildRecursive(site, SpawnRootName);
            return spawnRoot != null ? spawnRoot : site;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                    return child;

                Transform nested = FindChildRecursive(child, childName);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static void RebuildPoints(Transform root, List<Vector3> points)
        {
            points.Clear();
            if (root == null)
                return;

            var children = new List<Transform>();
            for (int i = 0; i < root.childCount; i++)
                children.Add(root.GetChild(i));

            children.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (Transform child in children)
                points.Add(child.position);

            if (points.Count == 0)
                points.Add(root.position);
        }

        private List<Vector3> GetPoints(EconomyFocusType resource)
        {
            switch (resource)
            {
                case EconomyFocusType.Wood:
                    return _woodPoints;
                case EconomyFocusType.Stone:
                    return _stonePoints;
                case EconomyFocusType.Iron:
                    return _ironPoints;
                case EconomyFocusType.Food:
                    return _foodPoints;
                default:
                    return null;
            }
        }

        private Vector3 GetRepeatOffset(int stackIndex)
        {
            if (stackIndex <= 0 || RepeatOffsetRadius <= 0f)
                return Vector3.zero;

            int ring = 1 + (stackIndex - 1) / 8;
            int step = (stackIndex - 1) % 8;
            float angle = step * Mathf.PI * 0.25f;
            float radius = RepeatOffsetRadius * ring;
            return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }

        private static int GetResourceDeliveryOffset(EconomyFocusType resource)
        {
            switch (EconomyFocusUtility.Normalize(resource))
            {
                case EconomyFocusType.Stone:
                    return 2;
                case EconomyFocusType.Iron:
                    return 4;
                case EconomyFocusType.Food:
                    return 6;
                default:
                    return 0;
            }
        }
    }
}
