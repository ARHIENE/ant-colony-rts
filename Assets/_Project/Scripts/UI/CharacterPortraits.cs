using System.Collections.Generic;
using System.Linq;
using AntColony.Units;
using UnityEngine;

namespace AntColony.UI
{
    // 2026-10-24 장수 초상: 개미 실루엣 대신 실제 kemono 캐릭터 얼굴. 장수 모델(AntVisual이 만든 "Kemono")을 복제해
    // 멀리 떨어진 전용 레이어 스튜디오에서 한 번 찍어 RenderTexture로 둔다(머리·귀·꼬리 조합이 게임 속 외형과 같다).
    public static class CharacterPortraits
    {
        public const int Layer = 30;
        const int Size = 128;
        static readonly Vector3 Studio = new Vector3(0, -9000, 0);
        static readonly Dictionary<CommanderAnt, RenderTexture> cache = new Dictionary<CommanderAnt, RenderTexture>();
        static UnityEngine.Camera camera;

        // 모델이 아직 없으면 null(호출 쪽은 다음 갱신에서 다시 묻는다).
        public static Texture Get(CommanderAnt c)
        {
            if (c == null) return null;
            if (cache.TryGetValue(c, out var tex) && tex != null) return tex;
            var source = c.transform.Find("Kemono");
            if (source == null) return null;
            tex = Render(source.gameObject);
            if (tex != null) cache[c] = tex;
            return tex;
        }

        static RenderTexture Render(GameObject source)
        {
            if (camera == null)
            {
                camera = new GameObject("PortraitCamera").AddComponent<UnityEngine.Camera>();
                camera.orthographic = true; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0, 0, 0, 0);
                camera.cullingMask = 1 << Layer; camera.enabled = false; camera.nearClipPlane = .01f; camera.farClipPlane = 20;
                var light = new GameObject("PortraitLight").AddComponent<Light>(); light.type = LightType.Directional; light.cullingMask = 1 << Layer;
                light.intensity = 1.2f; light.transform.rotation = Quaternion.Euler(20, 160, 0); light.transform.SetParent(camera.transform, true);
                Object.DontDestroyOnLoad(camera.gameObject);
                if (UnityEngine.Camera.main != null) UnityEngine.Camera.main.cullingMask &= ~(1 << Layer);
            }
            var clone = Object.Instantiate(source); clone.transform.SetPositionAndRotation(Studio, Quaternion.identity);
            foreach (var t in clone.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = Layer;
                if (t.name.StartsWith("Socket.")) foreach (var r in t.GetComponentsInChildren<Renderer>()) r.enabled = false; // 손에 든 도구는 빼고 찍는다
            }
            var animator = RigAnimator.Find(clone);
            if (animator != null) { animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.Rebind(); animator.Play("Base Layer.Idle_A", 0, 0); animator.Update(.01f); }
            var renderers = clone.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            if (renderers.Length == 0) { Object.Destroy(clone); return null; }
            var b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
            // 얼굴: 위쪽 40%를 정면(+Z 쪽 → 캐릭터 정면)에서 찍는다.
            var head = new Vector3(b.center.x, b.max.y - b.size.y * .32f, b.center.z);
            camera.orthographicSize = b.size.y * .26f;
            camera.transform.SetPositionAndRotation(head + Vector3.forward * 5, Quaternion.LookRotation(Vector3.back));
            var rt = new RenderTexture(Size, Size, 16) { name = "Portrait " + source.transform.parent?.name };
            camera.targetTexture = rt; camera.Render(); camera.targetTexture = null;
            clone.SetActive(false); Object.Destroy(clone);
            return rt;
        }
    }
}
