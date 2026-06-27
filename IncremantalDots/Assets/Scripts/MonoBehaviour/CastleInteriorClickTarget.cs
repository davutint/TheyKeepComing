using UnityEngine;
using UnityEngine.EventSystems;

namespace DeadWalls
{
    public class CastleInteriorClickTarget : MonoBehaviour
    {
        public float ClickRadius = 2f;

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.CanOpenCastleEconomy() || CastleEconomyUI.Instance == null)
                return;

            if (!TryGetPointerWorldPosition(out Vector2 worldPosition))
                return;

            Vector2 center = transform.position;
            if (Vector2.Distance(worldPosition, center) <= ClickRadius)
                CastleEconomyUI.Instance.OpenFromCastle();
        }

        private static bool TryGetPointerWorldPosition(out Vector2 worldPosition)
        {
            worldPosition = default;
            Camera camera = Camera.main;
            if (camera == null)
                return false;

            Vector2 screenPosition;
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                if (EventSystem.current != null
                    && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                {
                    return false;
                }

                screenPosition = Input.GetTouch(0).position;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return false;

                screenPosition = Input.mousePosition;
            }
            else
            {
                return false;
            }

            Vector3 world = camera.ScreenToWorldPoint(new Vector3(
                screenPosition.x,
                screenPosition.y,
                -camera.transform.position.z));
            worldPosition = new Vector2(world.x, world.y);
            return true;
        }
    }
}
