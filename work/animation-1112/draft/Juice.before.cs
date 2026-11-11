using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AntColony.UI
{
    // 손맛(2026-11-01 재미 구현): 행동마다 짧은 반응. 에셋 없이 크기 튀김·먼지·떠오르는 글자만 쓴다.
    // 건설 완료 → Pop+Dust, 창고 입고 → Float("+N 자원"), 피격 → Punch(AntVisual).
    public sealed class Juice : MonoBehaviour
    {
        private struct Floater { public Vector3 world; public string text; public Color color; public float start; }
        private const float FloatSeconds = 1.1f, FloatRise = 36f;
        private const int MaxFloaters = 24;
        private static Juice instance;
        private readonly List<Floater> floaters = new List<Floater>();
        private GUIStyle style;

        private static Juice Instance
        {
            get
            {
                if (instance == null) instance = new GameObject("Juice").AddComponent<Juice>();
                return instance;
            }
        }

        public static void Pop(Transform target, float overshoot = .12f) { if (target != null) Instance.StartCoroutine(Scale(target, overshoot, .32f)); }
        public static void Punch(Transform target) { if (target != null) Instance.StartCoroutine(Scale(target, .14f, .16f)); }

        public static void Float(Vector3 world, string text, Color color)
        {
            var list = Instance.floaters;
            if (list.Count >= MaxFloaters) list.RemoveAt(0);
            list.Add(new Floater { world = world, text = text, color = color, start = Time.unscaledTime });
        }

        public static void Dust(Vector3 position, float radius)
        {
            var go = new GameObject("Juice Dust");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.duration = .5f; main.loop = false; main.startLifetime = .7f; main.startSpeed = 2.2f;
            main.startSize = .9f; main.startColor = new Color(.92f, .85f, .7f, 1f); main.gravityModifier = .15f; main.maxParticles = 40;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = ps.emission; emission.rateOverTime = 0; emission.SetBursts(new[] { new ParticleSystem.Burst(0, 28) });
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = Mathf.Max(.5f, radius); shape.rotation = new Vector3(-90, 0, 0);
            var color = ps.colorOverLifetime; color.enabled = true;
            color.color = new Gradient { alphaKeys = new[] { new GradientAlphaKey(1f, 0), new GradientAlphaKey(0, 1) }, colorKeys = new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) } };
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = DustMaterial;
            ps.Play();
        }

        private static Material dustMaterial;
        // 기본 파티클은 네모로 보여 부드러운 원형 텍스처를 한 번 만들어 재사용한다.
        private static Material DustMaterial
        {
            get
            {
                if (dustMaterial != null) return dustMaterial;
                const int size = 32; var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1 - d) * Mathf.Clamp01(1 - d)));
                }
                tex.Apply();
                dustMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default"));
                dustMaterial.mainTexture = tex; if (dustMaterial.HasProperty("_BaseMap")) dustMaterial.SetTexture("_BaseMap", tex);
                // URP 파티클 셰이더 투명 블렌드
                if (dustMaterial.HasProperty("_Surface")) { dustMaterial.SetFloat("_Surface", 1); dustMaterial.SetFloat("_Blend", 0); dustMaterial.SetOverrideTag("RenderType", "Transparent"); dustMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); dustMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); dustMaterial.SetInt("_ZWrite", 0); dustMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); dustMaterial.renderQueue = 3000; }
                return dustMaterial;
            }
        }

        private static IEnumerator Scale(Transform target, float overshoot, float seconds)
        {
            var baseScale = target.localScale;
            for (float t = 0; t < seconds && target != null; t += Time.unscaledDeltaTime)
            {
                var k = t / seconds; // 0→1: 커졌다가 원래대로(감쇠 사인)
                target.localScale = baseScale * (1 + overshoot * Mathf.Sin(k * Mathf.PI) * (1 - k * .5f));
                yield return null;
            }
            if (target != null) target.localScale = baseScale;
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || floaters.Count == 0) return;
            var camera = UnityEngine.Camera.main;
            if (camera == null) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Bold };
            var now = Time.unscaledTime; var old = GUI.color;
            for (var i = floaters.Count - 1; i >= 0; i--)
            {
                var f = floaters[i]; var k = (now - f.start) / FloatSeconds;
                if (k >= 1) { floaters.RemoveAt(i); continue; }
                var p = camera.WorldToScreenPoint(f.world);
                if (p.z <= 0) continue;
                var rect = new Rect(p.x - 60, Screen.height - p.y - 40 - FloatRise * k, 120, 20);
                GUI.color = new Color(0, 0, 0, .7f * (1 - k)); GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), f.text, style);
                GUI.color = new Color(f.color.r, f.color.g, f.color.b, 1 - k); GUI.Label(rect, f.text, style);
            }
            GUI.color = old;
        }
    }
}
