using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    public class CastleInteriorWorkerSiteGizmo : MonoBehaviour
    {
        public EconomyFocusType Resource = EconomyFocusType.Wood;
        public Transform WorkerSpawnRoot;
        public Transform DeliveryRoot;
        public bool DrawAlways;
        public float SiteRadius = 0.7f;
        public float MarkerRadius = 0.06f;

        private readonly List<Transform> _markers = new List<Transform>();
        private readonly List<Transform> _deliveryMarkers = new List<Transform>();

        private void OnDrawGizmos()
        {
            if (DrawAlways)
                DrawGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmos(true);
        }

        private void DrawGizmos(bool selected)
        {
            Color color = GetResourceColor(Resource);
            color.a = selected ? 0.95f : 0.35f;

            Vector3 center = transform.position;
            Gizmos.color = color;
            Gizmos.DrawWireSphere(center, SiteRadius);

            Transform root = WorkerSpawnRoot != null ? WorkerSpawnRoot : transform;
            RebuildMarkers(root);
            RebuildMarkers(DeliveryRoot, _deliveryMarkers);

            for (int i = 0; i < _markers.Count; i++)
            {
                Vector3 position = _markers[i].position;
                Gizmos.color = color;
                Gizmos.DrawWireSphere(position, MarkerRadius);
                DrawCross(position, MarkerRadius * 1.4f);

                if (selected)
                {
                    Color lineColor = color;
                    lineColor.a = 0.28f;
                    Gizmos.color = lineColor;
                    Gizmos.DrawLine(center, position);
                    DrawDeliveryRoute(position, i, lineColor);
                }
            }

            if (_markers.Count == 0)
            {
                Gizmos.color = color;
                Gizmos.DrawWireSphere(center, MarkerRadius);
                DrawCross(center, MarkerRadius * 1.4f);
            }
        }

        private void RebuildMarkers(Transform root)
        {
            _markers.Clear();
            RebuildMarkers(root, _markers);
        }

        private static void RebuildMarkers(Transform root, List<Transform> markers)
        {
            markers.Clear();
            if (root == null)
                return;

            for (int i = 0; i < root.childCount; i++)
                markers.Add(root.GetChild(i));

            markers.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        private void DrawDeliveryRoute(Vector3 pickupPosition, int index, Color lineColor)
        {
            if (_deliveryMarkers.Count == 0)
                return;

            int deliveryIndex = (index + GetResourceDeliveryOffset(Resource)) % _deliveryMarkers.Count;
            Vector3 deliveryPosition = _deliveryMarkers[deliveryIndex].position;
            Gizmos.color = lineColor;
            Gizmos.DrawLine(pickupPosition, deliveryPosition);

            Color deliveryColor = lineColor;
            deliveryColor.a = 0.65f;
            Gizmos.color = deliveryColor;
            Gizmos.DrawWireSphere(deliveryPosition, MarkerRadius * 1.15f);
        }

        private static void DrawCross(Vector3 position, float size)
        {
            Gizmos.DrawLine(position + Vector3.left * size, position + Vector3.right * size);
            Gizmos.DrawLine(position + Vector3.down * size, position + Vector3.up * size);
        }

        private static Color GetResourceColor(EconomyFocusType resource)
        {
            switch (EconomyFocusUtility.Normalize(resource))
            {
                case EconomyFocusType.Stone:
                    return new Color(0.65f, 0.70f, 0.78f, 1f);
                case EconomyFocusType.Iron:
                    return new Color(0.50f, 0.62f, 0.86f, 1f);
                case EconomyFocusType.Food:
                    return new Color(0.60f, 0.90f, 0.42f, 1f);
                default:
                    return new Color(0.95f, 0.68f, 0.22f, 1f);
            }
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
