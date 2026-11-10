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
        // 스타크래프트식: 정면(남쪽)에서 비스듬히 내려다봐 맵 격자가 화면과 나란하고 위에서 본 지도처럼 보인다(2026-10-23 사용자 요청).
        // 씬에 직렬화된 옛 값(35°·45°)을 쓰지 않도록 상수로 둔다.
        private const float pitch = 55f;
        private const float startYaw = 0f;
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
        { focusPoint = focus; yaw = rotation; rotateTimer = -1; cam.orthographicSize = targetOrthoSize = size; ApplyTransform(); }
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
        // 조작감(2026-11-10): 줌은 목표값으로 부드럽게 따라가고, 가운데 버튼 드래그는 잡은 지면 지점을 끌고 다닌다.
        private float targetOrthoSize;
        private bool dragPanning;
        private Vector3 dragAnchor;
        private const float ZoomStep = .88f, ZoomSharpness = 14f;

        private void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
            cam.orthographic = true;
            cam.orthographicSize = targetOrthoSize = startOrthoSize;
            yaw = startYaw;
            ApplyTransform();
        }

        private void Update()
        {
            if (!AntColony.Core.GameSession.Exists || !AntColony.Core.GameSession.Instance.GameStarted
                || AntColony.UI.GameMenuController.BlocksInput) { dragPanning = false; return; }
            HandleRotateInput();
            if (rotateTimer >= 0f)
            {
                UpdateRotating();
            }
            else if (!HandleDragPan())
            {
                HandlePan();
            }
            HandleZoom();
            ApplyTransform();
        }

        private static bool PointerOverUi => UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        private static bool PointerInWindow(Vector2 pos) => Application.isFocused && pos.x >= 0 && pos.y >= 0 && pos.x <= Screen.width && pos.y <= Screen.height;

        // 화면의 한 점이 가리키는 지면(초점 높이의 수평면) 위치.
        private bool GroundUnder(Vector2 screen, out Vector3 point)
        {
            var ray = cam.ScreenPointToRay(screen);
            if (new Plane(Vector3.up, new Vector3(0, focusPoint.y, 0)).Raycast(ray, out var d)) { point = ray.GetPoint(d); return true; }
            point = focusPoint; return false;
        }

        // 가운데 버튼 드래그: 누른 순간의 지면 지점이 커서 밑에 계속 붙어 있도록 초점을 옮긴다.
        private bool HandleDragPan()
        {
            var mouse = Mouse.current;
            if (mouse == null) { dragPanning = false; return false; }
            var pos = mouse.position.ReadValue();
            if (mouse.middleButton.wasPressedThisFrame && PointerInWindow(pos) && !PointerOverUi)
                dragPanning = GroundUnder(pos, out dragAnchor);
            if (!mouse.middleButton.isPressed) dragPanning = false;
            if (!dragPanning) return false;
            if (GroundUnder(pos, out var now))
            {
                focusPoint.x = Mathf.Clamp(focusPoint.x + dragAnchor.x - now.x, minX, maxX);
                focusPoint.z = Mathf.Clamp(focusPoint.z + dragAnchor.z - now.z, minZ, maxZ);
            }
            return true;
        }

        // 화면 가장자리 스크롤 + 방향키. 둘 다 같은 속도 규칙을 쓴다.
        private void HandlePan()
        {
            var move = Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard != null && Application.isFocused)
            {
                if (keyboard.upArrowKey.isPressed) move.y += 1f;
                if (keyboard.downArrowKey.isPressed) move.y -= 1f;
                if (keyboard.rightArrowKey.isPressed) move.x += 1f;
                if (keyboard.leftArrowKey.isPressed) move.x -= 1f;
            }
            var mouse = Mouse.current;
            if (mouse != null && edgeScrollThickness > 0 && move.sqrMagnitude < 0.001f)
            {
                var pos = mouse.position.ReadValue();
                // 창이 포커스를 잃었거나 커서가 창 밖이면 흘러가지 않는다(창 모드·Alt+Tab, 2026-11-01).
                if (PointerInWindow(pos))
                {
                    if (pos.y >= Screen.height - edgeScrollThickness) move.y += 1f;
                    if (pos.y <= edgeScrollThickness) move.y -= 1f;
                    if (pos.x >= Screen.width - edgeScrollThickness) move.x += 1f;
                    if (pos.x <= edgeScrollThickness) move.x -= 1f;
                }
            }

            if (move.sqrMagnitude < 0.001f) return;

            var yawRotation = Quaternion.Euler(0f, yaw, 0f);
            var forward = yawRotation * Vector3.forward;
            var right = yawRotation * Vector3.right;

            // 카메라 조작은 일시정지·배속과 독립적으로, 줌아웃 때도 비슷한 화면 속도로 움직인다.
            var speed = panSpeed * 2f * (ViewSize / startOrthoSize);
            focusPoint += (forward * move.y + right * move.x).normalized * (speed * Time.unscaledDeltaTime);
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
            rotateTimer += Time.unscaledDeltaTime; // 일시정지(timeScale 0) 중에도 회전한다.
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

            // 휠 한 칸 = 일정 비율. 프레임 속도·일시정지와 무관하고, 메뉴·목록 위에서는 화면이 줌되지 않는다.
            var pos = mouse.position.ReadValue();
            var scroll = mouse.scroll.ReadValue().y;
            if (!Mathf.Approximately(scroll, 0f) && PointerInWindow(pos) && !PointerOverUi)
                targetOrthoSize = Mathf.Clamp(targetOrthoSize * (scroll > 0 ? ZoomStep : 1f / ZoomStep), minOrthoSize, maxOrthoSize);
            if (Mathf.Approximately(cam.orthographicSize, targetOrthoSize)) return;

            // 커서가 가리키는 지면을 기준으로 줌한다(커서 밑 지점이 화면에서 움직이지 않는다).
            var before = focusPoint;
            var anchored = PointerInWindow(pos) && !dragPanning && GroundUnder(pos, out before);
            var next = Mathf.Lerp(cam.orthographicSize, targetOrthoSize, 1f - Mathf.Exp(-ZoomSharpness * Time.unscaledDeltaTime));
            cam.orthographicSize = Mathf.Abs(next - targetOrthoSize) < .01f ? targetOrthoSize : next;
            if (!anchored) return;
            ApplyTransform();
            GroundUnder(pos, out var after);
            focusPoint.x = Mathf.Clamp(focusPoint.x + before.x - after.x, minX, maxX);
            focusPoint.z = Mathf.Clamp(focusPoint.z + before.z - after.z, minZ, maxZ);
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
