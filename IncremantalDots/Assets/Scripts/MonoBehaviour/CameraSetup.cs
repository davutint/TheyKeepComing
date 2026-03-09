using UnityEngine;

namespace DeadWalls
{
    [RequireComponent(typeof(Camera))]
    public class CameraSetup : MonoBehaviour
    {
        private void Awake()
        {
            var cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 15f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        }
    }
}
