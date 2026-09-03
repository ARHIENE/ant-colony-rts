using UnityEngine;
using UnityEngine.InputSystem;

namespace AntColony.Camera
{
    // 스타크래프트식 RTS 카메라: 고정 피치의 아이소메트릭 시점으로 지면의 한 지점(focusPoint)을 항상 바라보며,
    // 그 지점 기준으로 팬(가장자리 스크롤)·줌·90도 회전(Q/E, focusPoint를 축으로 궤도 회전)한다.
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class IsometricCameraController : MonoBehaviour
    {
        [SerializeField] private Vector3 focusPoint = new Vector3(5f, 8f, 5f);
        [SerializeField] private float pitch = 35f;
        [SerializeField] private float startYaw = 45f;
        [SerializeField] private float distance = 30f;

        [SerializeField] private float panSpeed = 25f;
        [SerializeField] private float edgeScrollThickness = 18f;

        [SerializeField] private float zoomSpeed = 15f;
        [SerializeField] private float minOrthoSize = 8f;
        [SerializeField] private float maxOrthoSize = 35f;
        [SerializeField] private float startOrthoSize = 18f;

        [SerializeField] private float rotateStepDegrees = 90f;
        [SerializeField] private float rotateDuration = 0.25f;

        private UnityEngine.Camera cam;
        private float yaw;
        private float yawFrom;
        private float yawTo;
        private float rotateTimer = -1f;

        private void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
            cam.orthographic = true;
            cam.orthographicSize = startOrthoSize;
            yaw = startYaw;
            ApplyTransform();
        }

        private void Update()
        {
            HandleRotateInput();
            if (rotateTimer >= 0f)
            {
                UpdateRotating();
            }
            else
            {
                HandleEdgePan();
            }
            HandleZoom();
            ApplyTransform();
        }

        private void HandleEdgePan()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            var pos = mouse.position.ReadValue();
            var move = Vector2.zero;

            if (pos.y >= Screen.height - edgeScrollThickness) move.y += 1f;
            if (pos.y <= edgeScrollThickness) move.y -= 1f;
            if (pos.x >= Screen.width - edgeScrollThickness) move.x += 1f;
            if (pos.x <= edgeScrollThickness) move.x -= 1f;

            if (move.sqrMagnitude < 0.001f) return;

            var yawRotation = Quaternion.Euler(0f, yaw, 0f);
            var forward = yawRotation * Vector3.forward;
            var right = yawRotation * Vector3.right;

            focusPoint += (forward * move.y + right * move.x) * (panSpeed * Time.deltaTime);
        }

        private void HandleRotateInput()
        {
            if (rotateTimer >= 0f) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.qKey.wasPressedThisFrame) BeginRotate(rotateStepDegrees);
            else if (keyboard.eKey.wasPressedThisFrame) BeginRotate(-rotateStepDegrees);
        }

        private void BeginRotate(float deltaDegrees)
        {
            yawFrom = yaw;
            yawTo = yaw + deltaDegrees;
            rotateTimer = 0f;
        }

        private void UpdateRotating()
        {
            rotateTimer += Time.deltaTime;
            var t = rotateDuration <= 0f ? 1f : Mathf.Clamp01(rotateTimer / rotateDuration);
            t = Mathf.SmoothStep(0f, 1f, t);
            yaw = Mathf.LerpAngle(yawFrom, yawTo, t);

            if (t >= 1f)
            {
                yaw = yawTo;
                rotateTimer = -1f;
            }
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

        private void ApplyTransform()
        {
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.rotation = rotation;
            transform.position = focusPoint - rotation * Vector3.forward * distance;
        }
    }
}
