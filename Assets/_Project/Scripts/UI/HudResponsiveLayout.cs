using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    // 시안의 세 영역을 같은 Canvas 좌표로 배치한다. 작은 화면은 장수 바를 다음 줄로 보낸다.
    [DefaultExecutionOrder(500)]
    public sealed class HudResponsiveLayout : MonoBehaviour
    {
        private RectTransform canvas, top, console, roster, clock, materials, toolbar, notifications, objective, detail;
        private Vector2 lastScreen;
        private bool wasBuilding;
        private float lastCanvasWidth;
        public static float TopHeight { get; private set; } = 54;

        private void Start()
        {
            canvas = (RectTransform)transform;
            top = (RectTransform)transform.Find("ResourceBar");
            console = (RectTransform)transform.Find("CommandConsole");
            roster = (RectTransform)transform.Find("RosterBar");
            clock = (RectTransform)transform.Find("HudClock");
            materials = (RectTransform)transform.Find("MaterialsList");
            var rects = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            toolbar = rects.FirstOrDefault(r => r.name == "MenuToolbar");
            notifications = rects.FirstOrDefault(r => r.name == "Toasts");
            objective = rects.FirstOrDefault(r => r.name == "Objective");
            detail = (RectTransform)transform.Find("DetailTabs");
            Apply();
        }
        private void LateUpdate()
        {
            if (lastScreen != new Vector2(Screen.width, Screen.height) || wasBuilding != BuildScreen.IsOpen || canvas != null && Mathf.Abs(lastCanvasWidth - canvas.rect.width) > .1f) Apply();
        }
        public void Apply()
        {
            if (canvas == null) return;
            lastScreen = new Vector2(Screen.width, Screen.height);
            wasBuilding = BuildScreen.IsOpen;
            // 900 기준 높이. 세로 화면도 최소 720 폭을 확보해 글자·버튼을 보존한다.
            float height = Mathf.Max(900, 720f * Screen.height / Mathf.Max(1, Screen.width));
            foreach (var scaler in FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (scaler.name != "HUDCanvas" && scaler.name != "Notifications" && scaler.name != "BetaObjectives") continue;
                scaler.referenceResolution = new Vector2(1440, height); scaler.matchWidthOrHeight = 1;
            }
            Canvas.ForceUpdateCanvases();
            lastCanvasWidth = canvas.rect.width;
            float width = height * Screen.width / Mathf.Max(1, Screen.height);
            bool narrow = width <= 1000, stacked = width <= 720.1f;
            TopHeight = width <= 1280 ? 100 : 54;
            top.sizeDelta = new Vector2(0, TopHeight);
            roster.anchoredPosition = new Vector2(width <= 1280 ? 0 : -10, width <= 1280 ? -58 : -5);
            if (toolbar != null)
            {
                float ratio = GetComponent<Canvas>().scaleFactor / toolbar.GetComponentInParent<Canvas>().scaleFactor;
                toolbar.localScale = Vector3.one * ratio; toolbar.anchoredPosition = new Vector2(8, -10) * ratio;
            }
            if (objective != null) objective.anchoredPosition = new Vector2(10, -TopHeight - 10);
            clock.anchoredPosition = new Vector2(-10, -TopHeight - 10);
            if (notifications != null) notifications.anchoredPosition = new Vector2(-10, -TopHeight - 112);
            materials.anchoredPosition = new Vector2(-380, -TopHeight - 10);
            float commandHeight = wasBuilding ? 200 : 88;
            console.sizeDelta = new Vector2(0, stacked ? 224 + commandHeight : 224);
            if (detail != null) detail.anchoredPosition = new Vector2(8, console.sizeDelta.y + 8);
            float left = narrow ? 0 : width <= 1280 ? 188 : 240;
            float right = width <= 1280 ? 200 : 240;
            HudConsole.Left.gameObject.SetActive(!narrow);
            HudConsole.Left.sizeDelta = new Vector2(left, 0);
            foreach (Transform child in HudConsole.Left)
                if (child.name.StartsWith("Minimap Filter")) child.gameObject.SetActive(width > 1280);
            HudConsole.Right.anchorMin = new Vector2(stacked ? 0 : 1, 0);
            HudConsole.Right.anchorMax = new Vector2(1, stacked ? 0 : 1);
            HudConsole.Right.pivot = new Vector2(1, stacked ? 0 : .5f);
            HudConsole.Right.sizeDelta = new Vector2(stacked ? 0 : right, stacked ? commandHeight : 0);
            HudConsole.Center.offsetMin = new Vector2(left, stacked ? commandHeight : 0);
            HudConsole.Center.offsetMax = new Vector2(stacked ? 0 : -right, 0);
        }
    }
}
