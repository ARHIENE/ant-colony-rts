using System.Collections.Generic;
using System.Linq;
using AntColony.Units;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AntColony.UI
{
    // 2026-10-24 장수 초상: 개미 실루엣 대신 실제 kemono 캐릭터 얼굴. 장수 모델(AntVisual이 만든 "Kemono")을 복제해
    // 멀리 떨어진 전용 레이어 스튜디오에서 한 번 찍어 RenderTexture로 둔다(머리·귀·꼬리 조합이 게임 속 외형과 같다).
    public static class CharacterPortraits
    {
        public const int Layer = 30;
        const int Size = 256;
        static readonly Vector3 Studio = new Vector3(0, -100, 0);
        static readonly Dictionary<CommanderAnt, RenderTexture> cache = new Dictionary<CommanderAnt, RenderTexture>();
        static UnityEngine.Camera camera;
        static Light portraitLight;

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

        static void CopyProperties(Renderer source, Renderer target)
        {
            var block = new MaterialPropertyBlock();
            source.GetPropertyBlock(block); target.SetPropertyBlock(block);
            for (int i = 0; i < source.sharedMaterials.Length; i++)
            { source.GetPropertyBlock(block, i); target.SetPropertyBlock(block, i); }
        }

        static RenderTexture Render(GameObject source)
        {
            if (camera == null)
            {
                camera = new GameObject("PortraitCamera").AddComponent<UnityEngine.Camera>();
                camera.orthographic = true; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0, 0, 0, 0);
                camera.cullingMask = 1 << Layer; camera.enabled = false; camera.nearClipPlane = .01f; camera.farClipPlane = 20;
                var light = new GameObject("PortraitLight").AddComponent<Light>(); portraitLight = light; light.type = LightType.Directional; light.cullingMask = 1 << Layer;
                light.intensity = 1.0f; light.color = Color.white; light.shadows = LightShadows.None; light.transform.rotation = Quaternion.Euler(20, 160, 0); light.transform.SetParent(camera.transform, true); light.enabled = false;
                var data = camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing = false; data.renderShadows = false;
                Object.DontDestroyOnLoad(camera.gameObject);
                if (UnityEngine.Camera.main != null) UnityEngine.Camera.main.cullingMask &= ~(1 << Layer);
            }
            var clone = Object.Instantiate(source); clone.transform.SetPositionAndRotation(Studio, Quaternion.identity);
            var sourceRenderers = source.GetComponentsInChildren<Renderer>(true);
            var cloneRenderers = clone.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < sourceRenderers.Length; i++) CopyProperties(sourceRenderers[i], cloneRenderers[i]);
            // Native world animation suppresses the legacy body with forceRenderingOff.
            // A portrait is an isolated snapshot: restore this only on its clone.
            foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true)) renderer.forceRenderingOff = false;
            foreach (var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (var animator in clone.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var t in clone.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = Layer;
                if (t.name.StartsWith("Socket.") || t.name.StartsWith("Holstered_") || t.name == "Native equipment grip")
                    foreach (var r in t.GetComponentsInChildren<Renderer>()) r.enabled = false; // 손·등의 무기는 초상 구도에서 제외
            }
            // Snapshot the live pose: a newly cloned skinned renderer has not been updated
            // by Unity yet, so rendering it immediately can cache an empty portrait.
            var bakedMeshes = new List<Mesh>();
            var originals = source.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var copies = clone.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < copies.Length; i++)
            {
                var copy = copies[i];
                if (!copy.enabled || !copy.gameObject.activeInHierarchy) continue;
                var mesh = new Mesh(); originals[i].BakeMesh(mesh); bakedMeshes.Add(mesh);
                var snapshot = new GameObject("Portrait mesh"); snapshot.layer = Layer;
                snapshot.transform.SetParent(copy.transform, false);
                snapshot.AddComponent<MeshFilter>().sharedMesh = mesh;
                var still = snapshot.AddComponent<MeshRenderer>();
                still.sharedMaterials = copy.sharedMaterials; CopyProperties(copy, still); copy.enabled = false;
            }
            var renderers = clone.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            if (renderers.Length == 0) { Object.Destroy(clone); return null; }
            var b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
            // 얼굴: 위쪽 40%를 정면(+Z 쪽 → 캐릭터 정면)에서 찍는다.
            var head = new Vector3(b.center.x, b.max.y - b.size.y * .32f, b.center.z);
            camera.orthographicSize = b.size.y * .26f;
            camera.transform.SetPositionAndRotation(head + Vector3.forward * 5, Quaternion.LookRotation(Vector3.back));
            var rt = new RenderTexture(Size, Size, 16) { name = "Portrait " + source.transform.parent?.name };
            // URP chooses one main directional light. Use the isolated studio key while rendering,
            // then restore world lighting immediately; gameplay models/materials are never modified.
            var sun = RenderSettings.sun; var ambient = RenderSettings.ambientProbe;
            bool wasEnabled = portraitLight.enabled;
            try
            {
                portraitLight.transform.rotation = Quaternion.Euler(20, 160, 0);
                portraitLight.enabled = true; RenderSettings.sun = portraitLight;
                var fill = new SphericalHarmonicsL2(); fill.AddAmbientLight(new Color(.3f, .3f, .3f)); RenderSettings.ambientProbe = fill;
                foreach (var r in renderers) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = LightProbeUsage.Off; }
                camera.targetTexture = rt; camera.Render();
            }
            finally { camera.targetTexture = null; RenderSettings.sun = sun; RenderSettings.ambientProbe = ambient; portraitLight.enabled = wasEnabled; }
            clone.SetActive(false); Object.Destroy(clone);
            foreach (var mesh in bakedMeshes) Object.Destroy(mesh);
            return rt;
        }
    }
}
