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
    public static class PortraitPreview1111
    {
        public static string Run()
        {
            string dir="Temp/presentation-1111/neutral-preview";
            System.IO.Directory.CreateDirectory(dir);int i=0;
            try
            {
                foreach(var c in AntColony.Core.CommanderRoster.Instance.Commanders.Where(c=>c!=null))
                {
                    var rt=Get(c) as RenderTexture;
                    var prev=RenderTexture.active;
                    var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
                    try { RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();System.IO.File.WriteAllBytes(dir+"/portrait-"+(i++)+".png",tex.EncodeToPNG()); }
                    finally { RenderTexture.active=prev;Object.Destroy(tex); }
                }
                return "Rendered "+i+" studio-only neutral portraits; world state unchanged";
            }
            finally
            {
                foreach(var rt in cache.Values) {rt.Release();Object.Destroy(rt);}cache.Clear();
                if(camera!=null)Object.Destroy(camera.gameObject);
            }
        }
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

            }
            var clone = Object.Instantiate(source); clone.transform.SetPositionAndRotation(Studio, Quaternion.identity);
            var sourceRenderers = source.GetComponentsInChildren<Renderer>(true);
            var cloneRenderers = clone.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < sourceRenderers.Length; i++) CopyProperties(sourceRenderers[i], cloneRenderers[i]);
            // Native world animation suppresses the legacy body with forceRenderingOff.
            // A portrait is an isolated snapshot: restore this only on its clone.
            foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true)) renderer.forceRenderingOff = false;
            foreach (var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            // Portrait framing must not depend on the moment Get was first called
            // (sleeping, mining, falling). Pose only the studio clone in its idle state.
            foreach (var animator in clone.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = true; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind(); animator.Play("Base Layer.Idle_A", 0, 0); animator.Update(.01f);
                animator.enabled = false;
            }
            for (int i = 0; i < sourceRenderers.Length; i++) cloneRenderers[i].enabled = sourceRenderers[i].enabled;
            clone.GetComponent<KemonoAppearance>()?.SetExpression("NEUTRAL");
            foreach (var skin in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (skin.name.StartsWith("Part_Face_") && skin.sharedMesh != null)
                    for (int i = 0; i < skin.sharedMesh.blendShapeCount; i++)
                    {
                        var name = skin.sharedMesh.GetBlendShapeName(i);
                        name = name.Substring(name.LastIndexOf('.') + 1).ToUpperInvariant();
                        skin.SetBlendShapeWeight(i, name == "NEUTRAL" ? 100 : 0);
                    }
            foreach (var t in clone.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = Layer;
                if (t.name.StartsWith("Socket.") || t.name.StartsWith("Holstered_") || t.name == "Native equipment grip")
                    foreach (var r in t.GetComponentsInChildren<Renderer>()) r.enabled = false; // 손·등의 무기는 초상 구도에서 제외
            }
            // Bake the evaluated studio pose; the live source may currently be lying down.
            var bakedMeshes = new List<Mesh>();
            var copies = clone.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < copies.Length; i++)
            {
                var copy = copies[i];
                if (!copy.enabled || !copy.gameObject.activeInHierarchy) continue;
                var mesh = new Mesh(); copy.BakeMesh(mesh); bakedMeshes.Add(mesh);
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
