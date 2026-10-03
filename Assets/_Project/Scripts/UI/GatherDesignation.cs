using AntColony.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace AntColony.UI
{
    // 기본 커맨드 카드의 '채집 금지 지정'/'지정 취소' 모드. 클릭 또는 드래그 범위의 노드에 적용하고,
    // 우클릭·Esc·다른 입력 모드(건설·어택무브·스킬·메뉴)가 시작되면 끝난다. 금지된 노드는 월드에 표시한다.
    [DefaultExecutionOrder(-100)]
    public sealed class GatherDesignation : MonoBehaviour
    {
        private static GatherDesignation instance;
        private bool active, forbid, dragging;
        private Vector2 start;
        private int consumedFrame = -1;
        private GUIStyle markStyle;

        public static bool IsActive => instance != null && instance.active;
        public static bool Forbidding => IsActive && instance.forbid;
        public static bool ConsumesPointerInput => instance != null && (instance.active || instance.consumedFrame == Time.frameCount);
        private void Awake() => instance = this;
        private void OnDestroy() { if (instance == this) instance = null; }

        public static void Begin(bool forbidNodes)
        {
            if (instance == null) return;
            instance.active = true; instance.forbid = forbidNodes; instance.dragging = false;
            ToastManager.Show(forbidNodes ? "채집 금지 지정: 노드 클릭 또는 드래그, 우클릭 종료" : "지정 취소: 노드 클릭 또는 드래그, 우클릭 종료");
        }
        public static void End() { if (instance != null) instance.active = instance.dragging = false; }

        // 화면 사각형 안의 노드에 적용한다. 반환값은 바뀐 노드 수.
        public static int Apply(Rect screenRect, bool forbidNodes)
        {
            var camera = UnityEngine.Camera.main; var changed = 0;
            if (camera == null) return 0;
            foreach (var node in ResourceNode.Available)
            {
                if (node == null || node.GatheringForbidden == forbidNodes) continue;
                var p = camera.WorldToScreenPoint(node.transform.position);
                if (p.z > 0 && screenRect.Contains(new Vector2(p.x, p.y))) { node.GatheringForbidden = forbidNodes; changed++; }
            }
            return changed;
        }
        public static bool ApplyClick(Vector2 screen, bool forbidNodes)
        {
            var camera = UnityEngine.Camera.main;
            if (camera == null || !Physics.Raycast(camera.ScreenPointToRay(screen), out var hit, 2000)
                || !(hit.collider.GetComponentInParent<ResourceNode>() is ResourceNode node)) return false;
            node.GatheringForbidden = forbidNodes;
            return true;
        }

        private void Update()
        {
            if (!active) return;
            if (GameMenuController.BlocksInput || BuildScreen.IsOpen || SkillTargeting.ConsumesPointerInput
                || FindFirstObjectByType<AntColony.Buildings.BuildingPlacementController>()?.IsPlacing == true
                || FindFirstObjectByType<AntColony.Units.AttackMoveController>()?.IsAttackMode == true) { End(); return; }
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (mouse.rightButton.wasPressedThisFrame || Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            { End(); consumedFrame = Time.frameCount; return; }
            var position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
            { dragging = true; start = position; }
            if (!dragging || !mouse.leftButton.wasReleasedThisFrame) return;
            dragging = false; consumedFrame = Time.frameCount;
            if (Vector2.Distance(start, position) < 8) ApplyClick(position, forbid);
            else Apply(Rect.MinMaxRect(Mathf.Min(start.x, position.x), Mathf.Min(start.y, position.y),
                Mathf.Max(start.x, position.x), Mathf.Max(start.y, position.y)), forbid);
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || WorldMapPanel.AnyOpen) return;
            var camera = UnityEngine.Camera.main;
            if (camera == null) return;
            if (markStyle == null) markStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 12, fontStyle = FontStyle.Bold };
            markStyle.normal.textColor = new Color(1f, .45f, .35f);
            foreach (var node in ResourceNode.Available)
            {
                if (node == null || !node.GatheringForbidden) continue;
                var p = camera.WorldToScreenPoint(node.transform.position + Vector3.up * 2f);
                if (p.z <= 0 || p.x < 0 || p.x > Screen.width || p.y < 0 || p.y > Screen.height) continue;
                GUI.Box(new Rect(p.x - 40, Screen.height - p.y - 22, 80, 20), "채집 금지", markStyle);
            }
            if (!active || !dragging || Mouse.current == null) return;
            var now = Mouse.current.position.ReadValue();
            var rect = Rect.MinMaxRect(Mathf.Min(start.x, now.x), Screen.height - Mathf.Max(start.y, now.y), Mathf.Max(start.x, now.x), Screen.height - Mathf.Min(start.y, now.y));
            var old = GUI.color; GUI.color = forbid ? new Color(1, .4f, .3f, .35f) : new Color(.4f, 1, .5f, .35f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
        }
    }
}
