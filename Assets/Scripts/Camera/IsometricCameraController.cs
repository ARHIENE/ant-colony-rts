using UnityEngine;
using UnityEngine.InputSystem;

namespace AntColony.Camera
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class IsometricCameraController : MonoBehaviour
    {
        [SerializeField] private Vector3 fixedEulerAngles = new Vector3(30f, 45f, 0f);
        [SerializeField] private float panSpeed = 15f;
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float minOrthoSize = 5f;
        [SerializeField] private float maxOrthoSize = 30f;

        private UnityEngine.Camera cam;

        private void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
            cam.orthographic = true;
            transform.rotation = Quaternion.Euler(fixedEulerAngles);
        }

        private void Update()
        {
            HandlePan();
            HandleZoom();
        }

        private void HandlePan()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            var input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;

            if (input.sqrMagnitude < 0.001f) return;

            var forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();
            var right = transform.right;
            right.y = 0f;
            right.Normalize();

            transform.position += (forward * input.y + right * input.x) * (panSpeed * Time.deltaTime);
        }

        private void HandleZoom()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f)) return;

            cam.orthographicSize = Mathf.Clamp(
                cam.orthographicSize - scroll * zoomSpeed * Time.deltaTime,
                minOrthoSize,
                maxOrthoSize);
        }
    }
}
