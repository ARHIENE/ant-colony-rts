using System.Collections.Generic;
using AntColony.Camera;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AntColony.UI
{
    // HUD v4.1 하단 콘솔: 불투명 판 + 청동 띠. 왼쪽(미니맵)·오른쪽(명령) 날개는 맵 쪽으로 WingRise만큼 솟는다.
    public static class HudConsole
    {
        public const float LeftWidth = 294f, RightWidth = 240f, CenterHeight = 224f, WingRise = 32f, WingTop = 22f;

        public static RectTransform Left { get; private set; }
        public static RectTransform Center { get; private set; }
        public static RectTransform Right { get; private set; }

        public static void Build(Transform canvas)
        {
            var root = MenuTheme.Rect("CommandConsole", canvas);
            root.anchorMin = new Vector2(0, 0); root.anchorMax = new Vector2(1, 0); root.pivot = new Vector2(.5f, 0);
            root.sizeDelta = new Vector2(0, CenterHeight); root.anchoredPosition = Vector2.zero;
            root.gameObject.AddComponent<Image>().color = MenuTheme.FrameMat;
            Stripe(root);
            Center = Part(root, "ConsoleCenter", 0, 0);
            Center.anchorMax = new Vector2(1, 1); Center.offsetMin = new Vector2(LeftWidth, 0); Center.offsetMax = new Vector2(-RightWidth, 0);
            Left = Wing(root, "ConsoleLeft", 0, LeftWidth);
            Right = Wing(root, "CommandCard", 1, RightWidth);
            Minimap.Create(Left);
        }

        private static void Stripe(RectTransform parent)
        {
            var stripe = MenuTheme.Bronze(parent, "Bronze");
            stripe.anchorMin = new Vector2(0, 1); stripe.anchorMax = Vector2.one; stripe.pivot = new Vector2(.5f, 1);
            stripe.sizeDelta = new Vector2(0, 7); stripe.anchoredPosition = Vector2.zero;
        }

        // 판 안의 구역(왼쪽 0 / 오른쪽 1에 붙고 아래 기준 높이 전체 + 솟음).
        private static RectTransform Part(RectTransform root, string name, float side, float width)
        {
            var rect = MenuTheme.Rect(name, root);
            rect.anchorMin = new Vector2(side, 0); rect.anchorMax = new Vector2(side, 1); rect.pivot = new Vector2(side, 0);
            rect.sizeDelta = new Vector2(width, 0); rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        // 날개: 판 재질 + 위 청동 띠 + 안쪽 솟은 면 청동 띠 + 네 귀 리벳.
        private static RectTransform Wing(RectTransform root, string name, float side, float width)
        {
            var wing = Part(root, name, side, width);
            wing.sizeDelta = new Vector2(width, WingRise);
            wing.gameObject.AddComponent<Image>().color = MenuTheme.FrameMat;
            Stripe(wing);
            var edge = MenuTheme.Bronze(wing, "WingEdge", true, side == 0);
            edge.anchorMin = edge.anchorMax = new Vector2(1 - side, 1); edge.pivot = new Vector2(side, 1);
            edge.sizeDelta = new Vector2(7, WingRise + 7); edge.anchoredPosition = Vector2.zero;
            foreach (var corner in new[] { new Vector2(0, 1), Vector2.one, Vector2.zero, new Vector2(1, 0) })
                MenuTheme.Rivet(wing, corner, new Vector2(corner.x == 0 ? 8 : -8, corner.y == 0 ? 6 : -12));
            return wing;
        }
    }

    // 전체 지형을 위에서 비추는 저해상도 카메라. 클릭한 지점으로 주 카메라를 옮긴다.
    // HUD v2: 위에 색 점(유닛 = 장수·건물·적 / 자원 노드 / 야생 개체)을 찍고, 옆 필터 버튼으로 종류별로 켜고 끈다.
    public sealed class Minimap : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IDragHandler, IInitializePotentialDragHandler
    {
        public enum Filter { Units, Resources, Wild }
        public static readonly bool[] Filters = { true, false, false };
        private const float RefreshSeconds = .25f;
        private UnityEngine.Camera mapCamera;
        private IsometricCameraController main;
        private RectTransform view, dots, terrainImage;
        private readonly List<Image> pool = new List<Image>();
        private readonly Text[] filterLabels = new Text[3];
        private float refresh;
        private Texture2D terrainMap;
        private Mesh terrainMesh;

        public static int DotCount { get; private set; }

        public static void Create(RectTransform wing)
        {
            // 화면 테두리용 바탕(RawImage에는 Outline을 겹칠 수 없어 뒤에 따로 둔다).
            var frame = MenuTheme.Rect("MinimapScreen", wing);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0, 0);
            MenuTheme.InsetScreen(frame.gameObject).raycastTarget = false;
            var rect = MenuTheme.Rect("Minimap", frame); MenuTheme.Stretch(rect);
            var image = rect.gameObject.AddComponent<RawImage>(); image.color = Color.clear; // 클릭 영역만 담당
            var map = rect.gameObject.AddComponent<Minimap>();
            rect.gameObject.AddComponent<RectMask2D>();
            // 지형 그림은 카메라 방향으로 돌려 화면 위쪽 = 지도 위쪽이 되게 한다(시야 사각형이 가로로 보이도록).
            // 스타크래프트식: 지형은 위에서 내려다본 북쪽 위 정사각형 그대로 둔다.
            map.terrainImage = MenuTheme.Rect("Terrain", rect); MenuTheme.Stretch(map.terrainImage);
            map.terrainImage.gameObject.AddComponent<RawImage>().raycastTarget = false;
            map.dots = MenuTheme.Rect("Dots", rect); MenuTheme.Stretch(map.dots);
            map.view = MenuTheme.Rect("View", rect);
            map.view.anchorMin = map.view.anchorMax = new Vector2(0, 0);
            foreach (var side in new[] { "Top", "Bottom", "Left", "Right" })
            {
                var border = MenuTheme.Rect(side, map.view);
                bool horizontal = side == "Top" || side == "Bottom";
                border.anchorMin = new Vector2(side == "Right" ? 1 : 0, side == "Top" ? 1 : 0);
                border.anchorMax = horizontal ? new Vector2(1, border.anchorMin.y) : new Vector2(border.anchorMin.x, 1);
                border.pivot = new Vector2(side == "Right" ? 1 : 0, side == "Top" ? 1 : 0);
                border.sizeDelta = horizontal ? new Vector2(0, 2) : new Vector2(2, 0);
                var imageBorder = border.gameObject.AddComponent<Image>(); imageBorder.color = Color.white; imageBorder.raycastTarget = false;
            }
            rect.gameObject.AddComponent<MenuTooltip>().Message = "미니맵: 왼쪽 클릭·누른 채 드래그로 카메라 이동";
            string[] labels = { "■ 정착지", "● 자원", "▲ 야생" };
            string[] tips = { "장수·건물·적 표시", "자원 노드 표시(식량·재료·특수 색)", "야생 개체 표시" };
            for (var i = 0; i < 3; i++)
            {
                var index = i;
                var b = MenuTheme.Rect("Minimap Filter " + (Filter)i, wing);
                b.anchorMin = b.anchorMax = b.pivot = new Vector2(0, 1);
                b.gameObject.AddComponent<Image>();
                var button = b.gameObject.AddComponent<Button>(); MenuTheme.StyleFrameButton(button);
                var text = MenuTheme.Text(b, labels[i], 11); MenuTheme.Stretch(text.rectTransform); text.alignment = TextAnchor.MiddleCenter;
                button.onClick.AddListener(() => { Filters[index] = !Filters[index]; map.refresh = 0; });
                map.filterLabels[i] = text;
                b.gameObject.AddComponent<MenuTooltip>().Message = tips[i];
            }
        }

        // v4.3: 날개 높이(위 여백 WingTop·아래 14)를 채우는 정사각형 미니맵 + 오른쪽 40폭 필터 3칸.
        public static void Fit(RectTransform wing, float width, bool filters)
        {
            float height = HudConsole.CenterHeight + HudConsole.WingRise - HudConsole.WingTop - 14;
            float size = Mathf.Min(height, width - 24 - (filters ? 48 : 0));
            var frame = (RectTransform)wing.Find("MinimapScreen");
            frame.sizeDelta = new Vector2(size, size); frame.anchoredPosition = new Vector2(12, 14 + (height - size) * .5f);
            float row = (size - 12) / 3;
            for (var i = 0; i < 3; i++)
            {
                var b = (RectTransform)wing.Find("Minimap Filter " + (Filter)i);
                b.gameObject.SetActive(filters);
                b.sizeDelta = new Vector2(40, row); b.anchoredPosition = new Vector2(12 + size + 8, -(HudConsole.WingTop + (height - size) * .5f + i * (row + 6)));
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
                foreach (var b in AntColony.Core.SceneObjects.All<AntColony.Buildings.BuildingBase>()) if (b.isActiveAndEnabled) Dot(b.transform.position, MenuTheme.Hex(0xa39880), 6);
                foreach (var e in AntColony.Core.SceneObjects.All<AntColony.World.EnemyCommander>()) if (!e.IsDead) Dot(e.transform.position, MenuTheme.Danger, 6);
            }
            if (Filters[(int)Filter.Resources])
                // 바닥 묶음은 점으로 찍지 않는다(G26: 1만 개면 점 1만 개를 매 0.25초 갱신).
                foreach (var n in AntColony.World.ResourceNode.Available)
                    if (n != null && n.isActiveAndEnabled && !n.IsDepleted && !n.IsLooseCargo)
                        Dot(n.transform.position, n.ResourceType == AntColony.Data.ResourceType.Food ? MenuTheme.Hex(0xe5bd6b)
                            : AntColony.Data.ResourceLabels.IsMaterial(n.ResourceType) ? MenuTheme.Hex(0xc49a74) : MenuTheme.Hex(0xb9a2f2), 4);
            if (Filters[(int)Filter.Wild])
                foreach (var m in AntColony.Core.SceneObjects.All<AntColony.World.WildMonster>())
                    if (!m.IsDead && !(m is AntColony.World.EnemyCommander)) Dot(m.transform.position, WildColor(m), 5);
            for (var i = used; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
            DotCount = used;
        }

        // 야생 개체 성향 색(온순/반격/포식자)은 사냥 단계에서 WildMonster에 성향이 생기면 여기서 나눈다.
        private static Color WildColor(AntColony.World.WildMonster m) => MenuTheme.Hex(0xe0913a);

        private void Start()
        {
            main = AntColony.Core.SceneObjects.Any<IsometricCameraController>();
            var go = new GameObject("MinimapCamera");
            mapCamera = go.AddComponent<UnityEngine.Camera>();
            mapCamera.orthographic = true;
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = MenuTheme.Well;
            mapCamera.enabled = false; // 투영 좌표만 사용하고 지형은 단순한 높이 지도로 표시한다.
        }

        private void OnDestroy()
        {
            if (mapCamera == null) return;
            if (terrainMap != null) Destroy(terrainMap);
            Destroy(mapCamera.gameObject);
        }

        private void LateUpdate()
        {
            if (main == null || mapCamera == null) return;
            var bounds = main.PanBounds;
            var terrain = AntColony.Map.MapGenerator.Active;
            var mesh = terrain != null ? terrain.GetComponent<MeshFilter>()?.sharedMesh : null;
            if (mesh != null && mesh != terrainMesh)
            {
                if (terrainMap != null) Destroy(terrainMap);
                terrainMap = MinimapTerrain.Build(terrain, mesh, bounds);
                terrainMesh = mesh;
                terrainImage.GetComponent<RawImage>().texture = terrainMap;
            }
            mapCamera.aspect = 1;
            mapCamera.orthographicSize = Mathf.Max(bounds.width, bounds.height) * .5f;
            mapCamera.transform.SetPositionAndRotation(new Vector3(bounds.center.x, 200f, bounds.center.y), Quaternion.Euler(90f, 0f, 0f));
            mapCamera.farClipPlane = 400f;
            var size = ((RectTransform)transform).rect.size;
            var camera = main.GetComponent<UnityEngine.Camera>();
            terrainImage.SetAsFirstSibling();
            var ground = new Plane(Vector3.up, main.FocusPoint);
            Vector3 Ground(float x, float y)
            {
                var ray = camera.ViewportPointToRay(new Vector3(x, y, 0));
                return ground.Raycast(ray, out var distance) ? ray.GetPoint(distance) : main.FocusPoint;
            }
            var bottomLeft = mapCamera.WorldToViewportPoint(Ground(0, 0));
            var bottomRight = mapCamera.WorldToViewportPoint(Ground(1, 0));
            var topLeft = mapCamera.WorldToViewportPoint(Ground(0, 1));
            var topRight = mapCamera.WorldToViewportPoint(Ground(1, 1));
            // 스타크래프트처럼 화면 비율의 가로 사각형으로 표시한다. 회전한 쿼터뷰 시야를 그대로 그리면 마름모가 되므로
            // 중심은 실제 시야 중심, 넓이는 실제로 보이는 땅 넓이(사다리꼴 면적)와 같게 맞춘다.
            Vector2 a = bottomLeft, b = bottomRight, c = topRight, d = topLeft;
            float area = Mathf.Abs((a.x * b.y - b.x * a.y) + (b.x * c.y - c.x * b.y) + (c.x * d.y - d.x * c.y) + (d.x * a.y - a.x * d.y)) * .5f;
            float aspect = Mathf.Max(1f, camera.aspect);
            var extent = new Vector2(Mathf.Sqrt(area * aspect), Mathf.Sqrt(area / aspect));
            var center = (Vector2)mapCamera.WorldToViewportPoint(main.FocusPoint);
            view.anchoredPosition = Vector2.Scale(center, size);
            view.sizeDelta = Vector2.Scale(extent, size);
            view.localRotation = Quaternion.identity;
            view.SetAsLastSibling();
        }

        public void OnInitializePotentialDrag(PointerEventData data) => data.useDragThreshold = false;
        public void OnPointerDown(PointerEventData data) => MoveCamera(data, false);
        public void OnPointerClick(PointerEventData data) => MoveCamera(data, false);
        public void OnDrag(PointerEventData data) => MoveCamera(data, true);

        private void MoveCamera(PointerEventData data, bool dragging)
        {
            if (main == null || mapCamera == null) return;
            if (data.button != PointerEventData.InputButton.Left) return;
            var rect = ((RectTransform)transform).rect;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, data.position, data.pressEventCamera, out var local) || (!dragging && !rect.Contains(local))) return;
            var ray = mapCamera.ViewportPointToRay(new Vector3(Mathf.Clamp01((local.x - rect.xMin) / rect.width), Mathf.Clamp01((local.y - rect.yMin) / rect.height), 0f));
            main.FocusOn(ray.origin);
        }
    }
}
