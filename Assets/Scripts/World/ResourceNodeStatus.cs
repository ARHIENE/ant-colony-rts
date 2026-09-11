using UnityEngine;

namespace AntColony.World
{
    // 자원량과 해금 상태의 원본은 ResourceNode에 유지한다.
    [RequireComponent(typeof(ResourceNode))]
    public class ResourceNodeStatus : MonoBehaviour
    {
        private ResourceNode node;
        private UnityEngine.Camera cam;
        private GUIStyle style;

        public string StatusText => node.IsRaidLocked ? "Destroy All Nest Buildings"
            : !node.IsUnlocked ? "Research Fishing First"
            : node.IsRegrowing ? $"{(node.RequiresFishing ? "Restocking" : "Growing")} · {Mathf.CeilToInt(node.RegrowTimeRemaining)}s"
            : node.IsDepleted ? "Empty" : $"{(node.RequiresFishing ? "Fish" : "Ready")} · {Mathf.CeilToInt(node.AmountRemaining)} {node.ResourceType}";

        private void Awake()
        {
            node = GetComponent<ResourceNode>();
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (!node.isActiveAndEnabled) return;
            if (cam == null) cam = UnityEngine.Camera.main;
            if (cam == null) return;
            var point = cam.WorldToScreenPoint(transform.position + Vector3.up);
            if (point.z <= 0f || point.x < 0f || point.x > Screen.width || point.y < 0f || point.y > Screen.height) return;

            if (style == null)
                style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
            style.normal.textColor = node.IsRegrowing ? new Color(1f, .85f, .4f) : new Color(.5f, 1f, .5f);
            GUI.Box(new Rect(point.x - 80f, Screen.height - point.y - 26f, 160f, 24f), StatusText, style);
        }
    }
}
