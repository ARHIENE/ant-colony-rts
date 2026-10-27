using System.Collections.Generic;
using AntColony.Data;
using UnityEngine;

namespace AntColony.World
{
    // 자원량과 해금 상태의 원본은 ResourceNode에 유지한다.
    // G26: 바닥 더미 1만 개가 각자 OnGUI를 받으면 GUI 호출 비용만 프레임당 100ms를 넘어 표시는 한 그리기 객체가 모아서 한다.
    [RequireComponent(typeof(ResourceNode))]
    public class ResourceNodeStatus : MonoBehaviour
    {
        private static readonly List<ResourceNodeStatus> Shown = new List<ResourceNodeStatus>();
        private static Drawer drawer;
        private ResourceNode node;

        public string StatusText => GetComponent<AntColony.Units.GatherOrderFeedback>()?.OrderText ?? (node.IsRaidLocked ? "둥지 건물 파괴 필요"
            : !node.IsUnlocked ? "낚시 연구 필요"
            : node.FishedOut ? "고갈 · 다음 계절 보충"
            : node.NeedsSowing ? $"파종 필요 · {node.SowRemaining:0.#}s"
            : node.IsRegrowing ? $"{(node.RequiresFishing ? "보충 중" : "성장 중")} · {Mathf.CeilToInt(node.RegrowTimeRemaining)}초"
            : node.IsDepleted ? "고갈" : $"{node.ResourceType.DisplayName()} · {Mathf.CeilToInt(node.AmountRemaining)}"
                + (node.NeedsDesignation && node.WorkRequested ? node.WorkPaused ? " · 일시중지" : " · " + node.WorkName + " 지정" : ""));

        private void Awake()
        {
            node = GetComponent<ResourceNode>();
        }

        private void OnEnable()
        {
            Shown.Add(this);
            if (drawer == null) drawer = new GameObject("ResourceNodeStatusDrawer").AddComponent<Drawer>();
        }

        private void OnDisable() => Shown.Remove(this);

        private sealed class Drawer : MonoBehaviour
        {
            private UnityEngine.Camera cam;
            private GUIStyle style;

            private void OnGUI()
            {
                if (Event.current.type != EventType.Repaint) return;
                if (!AntColony.Core.GameSession.Exists || !AntColony.Core.GameSession.Instance.GameStarted) return;
                if (AntColony.UI.WorldMapPanel.AnyOpen || AntColony.UI.GameMenuController.BlocksInput || AntColony.UI.BuildScreen.IsOpen) return;
                if (cam == null) cam = UnityEngine.Camera.main;
                if (cam == null) return;
                if (style == null)
                    style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
                foreach (var status in Shown)
                {
                    var node = status.node;
                    if (!node.isActiveAndEnabled) continue;
                    var point = cam.WorldToScreenPoint(status.transform.position + Vector3.up);
                    if (point.z <= 0f || point.x < 0f || point.x > Screen.width || point.y < 0f || point.y > Screen.height) continue;
                    style.normal.textColor = node.IsRegrowing ? new Color(1f, .85f, .4f) : new Color(.5f, 1f, .5f);
                    var width = status.GetComponent<AntColony.Units.GatherOrderFeedback>()?.OrderText != null ? 240f : 160f;
                    var rect = new Rect(point.x - width * .5f, Screen.height - point.y - 26f, width, 24f);
                    if (AntColony.UI.HudResponsiveLayout.ReserveWorldLabel(rect)) GUI.Box(rect, status.StatusText, style);
                }
            }
        }
    }
}
