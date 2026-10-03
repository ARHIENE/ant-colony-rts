using System.Collections.Generic;
using AntColony.Camera;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AntColony.UI
{
    // HUD v3 하단 콘솔: 한 판(높이 200)을 왼쪽(미니맵) 228 · 가운데(선택 정보) · 오른쪽(명령) 316으로 나눈다.
    public static class HudConsole
    {
        public const float LeftWidth = 240f, RightWidth = 240f, CenterHeight = 224f;

        public static RectTransform Left { get; private set; }
        public static RectTransform Center { get; private set; }
        public static RectTransform Right { get; private set; }

        public static void Build(Transform canvas)
        {
            var root = MenuTheme.Panel(canvas, "CommandConsole", new Vector2(.5f, 0), new Vector2(0, CenterHeight), Vector2.zero);
            root.anchorMin = new Vector2(0, 0); root.anchorMax = new Vector2(1, 0);
            Left = Part(root, "ConsoleLeft", 0, LeftWidth);
            Right = Part(root, "CommandCard", 1, RightWidth);
            Center = Part(root, "ConsoleCenter", 0, 0);
            Center.anchorMax = new Vector2(1, 1); Center.offsetMin = new Vector2(LeftWidth, 0); Center.offsetMax = new Vector2(-RightWidth, 0);
            Minimap.Create(Left);
        }

        // 판 안의 투명 구역(왼쪽 0 / 오른쪽 1에 붙고 높이 전체).
        private static RectTransform Part(RectTransform root, string name, float side, float width)
        {
            var rect = MenuTheme.Rect(name, root);
            rect.anchorMin = new Vector2(side, 0); rect.anchorMax = new Vector2(side, 1); rect.pivot = new Vector2(side, .5f);
            rect.sizeDelta = new Vector2(width, 0); rect.anchoredPosition = Vector2.zero;
            return rect;
        }
    }

    // 전체 지형을 위에서 비추는 저해상도 카메라. 클릭한 지점으로 주 카메라를 옮긴다.
    // HUD v2: 위에 색 점(유닛 = 장수·건물·적 / 자원 노드 / 야생 개체)을 찍고, 옆 필터 버튼으로 종류별로 켜고 끈다.
    public sealed class Minimap : MonoBehaviour, IPointerClickHandler
    {
        public enum Filter { Units, Resources, Wild }
        public static readonly bool[] Filters = { true, true, true };
        private const float Size = 164f, RefreshSeconds = .25f;
        private UnityEngine.Camera mapCamera;
        private IsometricCameraController main;
        private RectTransform view, dots;
        private readonly List<Image> pool = new List<Image>();
        private readonly Text[] filterLabels = new Text[3];
        private float refresh;

        public static int DotCount { get; private set; }

        public static void Create(RectTransform wing)
        {
            var rect = MenuTheme.Rect("Minimap", wing);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 0);
            rect.sizeDelta = new Vector2(Size, Size); rect.anchoredPosition = new Vector2(12, 30);
            var image = rect.gameObject.AddComponent<RawImage>(); image.color = Color.white;
            var map = rect.gameObject.AddComponent<Minimap>();
            map.dots = MenuTheme.Rect("Dots", rect); MenuTheme.Stretch(map.dots);
            map.view = MenuTheme.Rect("View", rect);
            map.view.anchorMin = map.view.anchorMax = new Vector2(0, 0);
            var marker = map.view.gameObject.AddComponent<Image>(); marker.color = new Color(.94f, .9f, .85f, .85f); marker.raycastTarget = false;
            map.view.sizeDelta = new Vector2(6, 6);
            rect.gameObject.AddComponent<MenuTooltip>().Message = "미니맵: 클릭한 곳으로 카메라 이동";
            string[] labels = { "유닛", "자원", "야생" };
            string[] tips = { "장수·건물·적 표시", "자원 노드 표시(식량·재료·특수 색)", "야생 개체 표시" };
            for (var i = 0; i < 3; i++)
            {
                var index = i;
                var b = MenuTheme.Rect("Minimap Filter " + (Filter)i, wing);
                b.anchorMin = b.anchorMax = b.pivot = new Vector2(0, 1);
                b.sizeDelta = new Vector2(36, 48); b.anchoredPosition = new Vector2(184, -30 - i * 56);
                b.gameObject.AddComponent<Image>();
                var button = b.gameObject.AddComponent<Button>(); MenuTheme.StyleButton(button);
                var text = MenuTheme.Text(b, labels[i], 11); MenuTheme.Stretch(text.rectTransform); text.alignment = TextAnchor.MiddleCenter;
                button.onClick.AddListener(() => { Filters[index] = !Filters[index]; map.refresh = 0; });
                map.filterLabels[i] = text;
                b.gameObject.AddComponent<MenuTooltip>().Message = tips[i];
            }
        }

        private void Update()
        {
            for (var i = 0; i < filterLabels.Length; i++) filterLabels[i].color = Filters[i] ? MenuTheme.TextColor : MenuTheme.Dim;
            if ((refresh -= Time.unscaledDeltaTime) > 0 || mapCamera == null) return;
            refresh = RefreshSeconds;
            var used = 0;
            void Dot(Vector3 world, Color color, float size)
            {
                var p = mapCamera.WorldToViewportPoint(world);
                if (p.x < 0 || p.x > 1 || p.y < 0 || p.y > 1) return;
                if (used == pool.Count)
                {
                    var r = MenuTheme.Rect("Dot", dots); r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
                    var img = r.gameObject.AddComponent<Image>(); img.raycastTarget = false; pool.Add(img);
                }
                var dot = pool[used++]; dot.gameObject.SetActive(true); dot.color = color;
                dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(p.x, p.y); dot.rectTransform.sizeDelta = new Vector2(size, size);
            }
            if (Filters[(int)Filter.Units])
            {
                if (AntColony.Core.CommanderRoster.Instance != null)
                    foreach (var c in AntColony.Core.CommanderRoster.Instance.Commanders) if (c.isActiveAndEnabled && !c.IsDead) Dot(c.Position, MenuTheme.Hp, 5);
                foreach (var b in FindObjectsByType<AntColony.Buildings.BuildingBase>(FindObjectsSortMode.None)) if (b.isActiveAndEnabled) Dot(b.transform.position, MenuTheme.Hex(0xa39880), 6);
                foreach (var e in FindObjectsByType<AntColony.World.EnemyCommander>(FindObjectsSortMode.None)) if (!e.IsDead) Dot(e.transform.position, MenuTheme.Danger, 6);
            }
            if (Filters[(int)Filter.Resources])
                foreach (var n in FindObjectsByType<AntColony.World.ResourceNode>(FindObjectsSortMode.None))
                    if (n.isActiveAndEnabled && !n.IsDepleted)
                        Dot(n.transform.position, n.ResourceType == AntColony.Data.ResourceType.Food ? MenuTheme.Hex(0xe5bd6b)
                            : n.ResourceType == AntColony.Data.ResourceType.Soil ? MenuTheme.Hex(0xc49a74) : MenuTheme.Hex(0xb9a2f2), 4);
            if (Filters[(int)Filter.Wild])
                foreach (var m in FindObjectsByType<AntColony.World.WildMonster>(FindObjectsSortMode.None))
                    if (!m.IsDead && !(m is AntColony.World.EnemyCommander)) Dot(m.transform.position, WildColor(m), 5);
            for (var i = used; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
            DotCount = used;
        }

        // 야생 개체 성향 색(온순/반격/포식자)은 사냥 단계에서 WildMonster에 성향이 생기면 여기서 나눈다.
        private static Color WildColor(AntColony.World.WildMonster m) => MenuTheme.Hex(0xe0913a);

        private void Start()
        {
            main = FindFirstObjectByType<IsometricCameraController>();
            var go = new GameObject("MinimapCamera");
            mapCamera = go.AddComponent<UnityEngine.Camera>();
            mapCamera.orthographic = true;
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = MenuTheme.Well;
            mapCamera.targetTexture = new RenderTexture(256, 256, 16);
            GetComponent<RawImage>().texture = mapCamera.targetTexture;
        }

        private void OnDestroy()
        {
            if (mapCamera == null) return;
            mapCamera.targetTexture.Release();
            Destroy(mapCamera.gameObject);
        }

        private void LateUpdate()
        {
            if (main == null || mapCamera == null) return;
            var bounds = main.PanBounds;
            mapCamera.orthographicSize = Mathf.Max(bounds.width, bounds.height) * .5f;
            mapCamera.transform.SetPositionAndRotation(new Vector3(bounds.center.x, 200f, bounds.center.y), Quaternion.Euler(90f, 0f, 0f));
            mapCamera.farClipPlane = 400f;
            var viewport = mapCamera.WorldToViewportPoint(main.FocusPoint);
            view.anchoredPosition = new Vector2(viewport.x * Size - 3f, viewport.y * Size - 3f);
        }

        public void OnPointerClick(PointerEventData data)
        {
            if (main == null || mapCamera == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, data.position, data.pressEventCamera, out var local);
            var ray = mapCamera.ViewportPointToRay(new Vector3(local.x / Size, local.y / Size, 0f));
            main.FocusOn(ray.origin);
        }
    }
}
