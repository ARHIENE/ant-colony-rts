using AntColony.Camera;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AntColony.UI
{
    // 디자인 HUD 하단 콘솔: 왼쪽 날개(미니맵) 236 · 가운데(선택 장수) · 오른쪽 날개(커맨드 카드/건설) 372, 높이 208.
    public static class HudConsole
    {
        public const float LeftWidth = 236f, RightWidth = 372f, WingHeight = 180f, CenterHeight = 208f;

        public static RectTransform Left { get; private set; }
        public static RectTransform Center { get; private set; }
        public static RectTransform Right { get; private set; }

        public static void Build(Transform canvas)
        {
            var root = MenuTheme.Rect("CommandConsole", canvas);
            root.anchorMin = new Vector2(0, 0); root.anchorMax = new Vector2(1, 0); root.pivot = new Vector2(.5f, 0);
            root.sizeDelta = new Vector2(0, CenterHeight); root.anchoredPosition = Vector2.zero;
            Left = MenuTheme.Panel(root, "ConsoleLeft", new Vector2(0, 0), new Vector2(LeftWidth, WingHeight), Vector2.zero);
            Right = MenuTheme.Panel(root, "CommandCard", new Vector2(1, 0), new Vector2(RightWidth, WingHeight), Vector2.zero);
            Center = MenuTheme.Panel(root, "ConsoleCenter", new Vector2(0, 0), new Vector2(1440f - LeftWidth - RightWidth + 12f, CenterHeight), new Vector2(LeftWidth - 6f, 0));
            Minimap.Create(Left);
        }
    }

    // 전체 지형을 위에서 비추는 저해상도 카메라. 클릭한 지점으로 주 카메라를 옮긴다.
    public sealed class Minimap : MonoBehaviour, IPointerClickHandler
    {
        private const float Size = 164f;
        private UnityEngine.Camera mapCamera;
        private IsometricCameraController main;
        private RectTransform view;

        public static void Create(RectTransform wing)
        {
            var rect = MenuTheme.Rect("Minimap", wing);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 0);
            rect.sizeDelta = new Vector2(Size, Size); rect.anchoredPosition = new Vector2(8, 8);
            var image = rect.gameObject.AddComponent<RawImage>(); image.color = Color.white;
            var map = rect.gameObject.AddComponent<Minimap>();
            map.view = MenuTheme.Rect("View", rect);
            map.view.anchorMin = map.view.anchorMax = new Vector2(0, 0);
            var marker = map.view.gameObject.AddComponent<Image>(); marker.color = new Color(.94f, .9f, .85f, .85f); marker.raycastTarget = false;
            map.view.sizeDelta = new Vector2(6, 6);
            rect.gameObject.AddComponent<MenuTooltip>().Message = "미니맵: 클릭한 곳으로 카메라 이동";
        }

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
