using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AntColony.UI
{
    // 월드맵 3D 행성: 멀리 떨어진 전용 레이어의 구체를 전용 카메라가 RenderTexture로 찍어 RawImage에 보인다.
    // 거점 MapPosition(0~1)을 경도·위도로 바꿔 구 표면 위치를 만들고, 화면 좌표로 투영해 UI 마커를 따라 붙인다. 드래그로 회전.
    public sealed class WorldPlanet : MonoBehaviour, IDragHandler
    {
        public const int Layer = 31;
        private const float Radius = 10f, LonSpread = 300f, LatSpread = 120f;
        private static readonly Vector3 Origin = new Vector3(0, -5000, 0);
        private Transform planet;
        private UnityEngine.Camera view;
        private RawImage image;
        private float yaw, pitch, targetYaw, targetPitch;

        public RectTransform Rect => (RectTransform)transform;

        public static WorldPlanet Create(RectTransform parent, float x, float y, float size)
        {
            var rect = MenuTheme.Rect("Planet", parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(size, size);
            var p = rect.gameObject.AddComponent<WorldPlanet>();
            p.image = rect.gameObject.AddComponent<RawImage>();
            p.Build();
            return p;
        }

        private void Build()
        {
            var rig = new GameObject("WorldPlanetRig"); rig.transform.position = Origin;
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); sphere.name = "Planet Sphere";
            Destroy(sphere.GetComponent<Collider>());
            sphere.layer = Layer; sphere.transform.SetParent(rig.transform, false); sphere.transform.localScale = Vector3.one * Radius * 2;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { mainTexture = Surface() };
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", material.mainTexture);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .15f);
            sphere.GetComponent<MeshRenderer>().sharedMaterial = material;
            planet = sphere.transform;

            var sun = new GameObject("Planet Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.cullingMask = 1 << Layer;
            sun.intensity = 1.1f; sun.color = new Color(1f, .93f, .8f);
            sun.transform.SetParent(rig.transform, false); sun.transform.rotation = Quaternion.Euler(25, -35, 0);

            view = new GameObject("Planet Camera").AddComponent<UnityEngine.Camera>();
            view.transform.SetParent(rig.transform, false); view.transform.localPosition = new Vector3(0, 0, -Radius * 3.9f);
            view.fieldOfView = 34; view.nearClipPlane = 1; view.farClipPlane = Radius * 8;
            view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = MenuTheme.Hex(0x16120e);
            view.cullingMask = 1 << Layer;
            var size = Mathf.RoundToInt(Rect.sizeDelta.x);
            view.targetTexture = new RenderTexture(size * 2, size * 2, 16) { name = "WorldPlanet" };
            image.texture = view.targetTexture;
            if (UnityEngine.Camera.main != null) UnityEngine.Camera.main.cullingMask &= ~(1 << Layer);
            view.enabled = false;
        }

        // 갈색·녹색 대륙과 어두운 바다를 가진 작은 절차 텍스처.
        private static Texture2D Surface()
        {
            const int w = 256, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Repeat, name = "PlanetSurface" };
            Color sea = MenuTheme.Hex(0x1f3a3f), land = MenuTheme.Hex(0x6b5a38), moss = MenuTheme.Hex(0x4f6a35);
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    var u = x / (float)w * Mathf.PI * 2;
                    var n = Mathf.PerlinNoise(Mathf.Cos(u) * 2.2f + 5, y / (float)h * 4.4f + Mathf.Sin(u) * 2.2f);
                    var d = Mathf.PerlinNoise(x * .08f + 30, y * .08f + 30);
                    tex.SetPixel(x, y, n < .47f ? Color.Lerp(sea, sea * 1.3f, d) : Color.Lerp(land, moss, d));
                }
            tex.Apply();
            return tex;
        }

        public static Vector3 Direction(Vector2 mapPosition)
        {
            var lon = (Mathf.Clamp01(mapPosition.x) - .5f) * LonSpread * Mathf.Deg2Rad;
            var lat = (Mathf.Clamp01(mapPosition.y) - .5f) * LatSpread * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(lon) * Mathf.Cos(lat), Mathf.Sin(lat), -Mathf.Cos(lon) * Mathf.Cos(lat));
        }

        // 거점을 화면 쪽으로 돌린다(원정 박스 클릭 = 그 수단의 목적지로 카메라 이동).
        public void Focus(Vector2 mapPosition)
        {
            // Y축 회전 θ는 경도를 L−θ로, X축 회전 α는 위도를 φ+α로 옮긴다 → 정면(0,0)에 오도록 θ=L, α=−φ.
            targetYaw = (Mathf.Clamp01(mapPosition.x) - .5f) * LonSpread;
            targetPitch = Mathf.Clamp(-(Mathf.Clamp01(mapPosition.y) - .5f) * LatSpread, -60, 60);
        }
        public bool Focused => Mathf.Abs(Mathf.DeltaAngle(yaw, targetYaw)) < .5f && Mathf.Abs(pitch - targetPitch) < .5f;

        public void OnDrag(PointerEventData e)
        {
            targetYaw = yaw -= e.delta.x * .3f;
            targetPitch = pitch = Mathf.Clamp(pitch + e.delta.y * .3f, -60, 60);
        }

        public void SetVisible(bool visible) { if (view != null) view.enabled = visible; }

        private void LateUpdate()
        {
            if (planet == null || !view.enabled) return;
            yaw = Mathf.LerpAngle(yaw, targetYaw, 1 - Mathf.Exp(-8 * Time.unscaledDeltaTime));
            pitch = Mathf.Lerp(pitch, targetPitch, 1 - Mathf.Exp(-8 * Time.unscaledDeltaTime));
            planet.rotation = Quaternion.Euler(pitch, 0, 0) * Quaternion.Euler(0, yaw, 0);
        }
        public void SnapToTarget() { yaw = targetYaw; pitch = targetPitch; if (planet != null) planet.rotation = Quaternion.Euler(pitch, 0, 0) * Quaternion.Euler(0, yaw, 0); }

        // 거점 위치를 이 RawImage 안의 좌상단 기준 좌표로 투영한다. 뒷면이면 false.
        public bool Project(Vector2 mapPosition, out Vector2 local)
        {
            local = default;
            if (planet == null) return false;
            var dir = planet.rotation * Direction(mapPosition);
            var point = planet.position + dir * Radius;
            if (Vector3.Dot(dir, (view.transform.position - point).normalized) < .12f) return false;
            var v = view.WorldToViewportPoint(point);
            local = new Vector2(v.x * Rect.sizeDelta.x, (1 - v.y) * Rect.sizeDelta.y);
            return true;
        }

        private void OnDestroy()
        {
            if (view != null) { if (view.targetTexture != null) view.targetTexture.Release(); Destroy(view.transform.parent.gameObject); }
        }
    }
}
