using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

namespace SmallScaleInc.CharacterCreatorFantasy
{
    [RequireComponent(typeof(Camera))]
    public class UtilityManager : MonoBehaviour
    {
        [Header("Target & Offset")]
        public Transform target;
        public Vector3 offset;

        [Header("Smooth Movement")]
        public float smoothTime = 0.3f;
        private Vector3 velocity = Vector3.zero;

        [Header("Look-Ahead Settings")]
        public bool enableLookAhead = true;
        public float lookAheadDistance = 2f;
        public float lookAheadSpeed = 5f;
        private Vector3 currentLookAhead = Vector3.zero;
        private Vector3 lastTargetPosition;

        [Header("Zoom Settings")]
        public float zoomSpeed = 5f;
        public float minZoom = 2f;
        public float maxZoom = 10f;
        private Camera cam;

        [Header("Tilemap Switching")]
        [Tooltip("One Toggle per Tilemap. 5 max.")]
        public Toggle[] tilemapToggles;
        [Tooltip("The Tilemaps to switch between.")]
        public Tilemap[] tilemaps;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
                Debug.LogError("UtilityManager requires a Camera component.");

            if (target != null)
                lastTargetPosition = target.position;

            // Hook up your toggles
            for (int i = 0; i < tilemapToggles.Length && i < tilemaps.Length; i++)
            {
                int idx = i; // local copy for closure
                tilemapToggles[i].onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                        ActivateTilemap(idx);
                });
            }

            // Initialize by activating first one if any
            if (tilemapToggles.Length > 0 && tilemapToggles[0] != null)
                tilemapToggles[0].isOn = true;
        }

        void LateUpdate()
        {
            if (target == null || cam == null)
                return;

            // Zoom
            float scroll = GetMouseScroll();
            if (Mathf.Abs(scroll) > 0f
                && !EventSystem.current.IsPointerOverGameObject())
            {
                cam.orthographicSize = Mathf.Clamp(
                    cam.orthographicSize - scroll * zoomSpeed,
                    minZoom, maxZoom
                );
            }

            // Look‐ahead
            Vector3 targetDelta = target.position - lastTargetPosition;
            lastTargetPosition = target.position;

            if (enableLookAhead)
            {
                Vector3 desiredLookAhead = targetDelta.normalized * lookAheadDistance;
                currentLookAhead = Vector3.Lerp(
                    currentLookAhead,
                    desiredLookAhead,
                    Time.deltaTime * lookAheadSpeed
                );
            }
            else
            {
                currentLookAhead = Vector3.zero;
            }

            // Smooth follow
            Vector3 desiredPosition = target.position + offset + currentLookAhead;
            desiredPosition.z = transform.position.z;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref velocity,
                smoothTime
            );
        }

        private static float GetMouseScroll()
        {
            if (Mouse.current == null) return 0f;
            return Mouse.current.scroll.ReadValue().y / 120f;
        }

        /// <summary>
        /// Activates only the tilemap at index, disables all others.
        /// </summary>
        public void ActivateTilemap(int index)
        {
            for (int i = 0; i < tilemaps.Length; i++)
            {
                if (tilemaps[i] != null)
                    tilemaps[i].gameObject.SetActive(i == index);
            }

            // Ensure only the selected toggle stays on
            for (int i = 0; i < tilemapToggles.Length; i++)
            {
                if (tilemapToggles[i] != null)
                    tilemapToggles[i].isOn = (i == index);
            }
        }
    }
}
