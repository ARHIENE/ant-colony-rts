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

        [Header("Pan Bounds (지형 크기에 맞춰 설정)")]
        [SerializeField] private float minX = 0f;
        [SerializeField] private float maxX = 400f;
        [SerializeField] private float minZ = 0f;
        [SerializeField] private float maxZ = 400f;

        [SerializeField] private float zoomSpeed = 15f;
        [SerializeField] private float minOrthoSize = 8f;
        [SerializeField] private float maxOrthoSize = 35f;
        [SerializeField] private float startOrthoSize = 18f;

        [SerializeField] private float rotateStepDegrees = 90f;
        [SerializeField] private float rotateDuration = 0.25f;

        private UnityEngine.Camera cam;
        public Vector3 FocusPoint => focusPoint;
        public float Yaw => yaw;
        internal void RestoreView(Vector3 focus, float rotation, float size)
        { focusPoint = focus; yaw = rotation; rotateTimer = -1; cam.orthographicSize = size; ApplyTransform(); }
        public void ApplySettings(float speed, float edge) { panSpeed = speed; edgeScrollThickness = edge; }
        public Rect PanBounds => Rect.MinMaxRect(minX, minZ, maxX, maxZ);
        public float ViewSize => cam != null ? cam.orthographicSize : startOrthoSize;
        // 미니맵 클릭: 높이는 유지하고 수평 위치만 옮긴다.
        public void FocusOn(Vector3 point)
        {
            focusPoint.x = Mathf.Clamp(point.x, minX, maxX);
            focusPoint.z = Mathf.Clamp(point.z, minZ, maxZ);
            ApplyTransform();
        }

        public void SetRegion(Vector3 focus, Bounds bounds)
        {
            focusPoint = focus;
            minX = bounds.min.x;
            maxX = bounds.max.x;
            minZ = bounds.min.z;
            maxZ = bounds.max.z;
            ApplyTransform();
        }
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
            if (AntColony.UI.GameMenuController.BlocksInput) return;
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
            focusPoint.x = Mathf.Clamp(focusPoint.x, minX, maxX);
            focusPoint.z = Mathf.Clamp(focusPoint.z, minZ, maxZ);
        }

        private void HandleRotateInput()
        {
            if (rotateTimer >= 0f) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (AntColony.Core.KeyBindings.Pressed(AntColony.Core.GameAction.RotateLeft)) BeginRotate(rotateStepDegrees);
            else if (AntColony.Core.KeyBindings.Pressed(AntColony.Core.GameAction.RotateRight)) BeginRotate(-rotateStepDegrees);
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
            if (cam != null)
            {
                // 초점뿐 아니라 화면의 지면 투영 범위가 맵 안에 머물도록 한다.
                var right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
                var forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                var halfWidth = cam.orthographicSize * cam.aspect;
                var halfDepth = cam.orthographicSize / Mathf.Sin(pitch * Mathf.Deg2Rad);
                var marginX = Mathf.Min((maxX - minX) * .5f, Mathf.Abs(right.x) * halfWidth + Mathf.Abs(forward.x) * halfDepth + 20f);
                var marginZ = Mathf.Min((maxZ - minZ) * .5f, Mathf.Abs(right.z) * halfWidth + Mathf.Abs(forward.z) * halfDepth + 20f);
                focusPoint.x = Mathf.Clamp(focusPoint.x, minX + marginX, maxX - marginX);
                focusPoint.z = Mathf.Clamp(focusPoint.z, minZ + marginZ, maxZ - marginZ);
            }
            transform.rotation = rotation;
            // 줌아웃 때 화면 상단의 지형이 near clip 뒤로 넘어가지 않게 뒤로 물러난다.
            var viewDistance = cam == null ? distance : Mathf.Max(distance,
                cam.orthographicSize / Mathf.Tan(pitch * Mathf.Deg2Rad) + Mathf.Max(0, 20 - focusPoint.y) / Mathf.Sin(pitch * Mathf.Deg2Rad) + 5);
            transform.position = focusPoint - rotation * Vector3.forward * viewDistance;
        }
    }
}
