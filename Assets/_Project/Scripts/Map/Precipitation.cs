using UnityEngine;

namespace AntColony.Map
{
    // 화면(카메라 초점) 위에서 떨어지는 입자: 계절의 꽃잎·낙엽·눈, 날씨의 비·눈보라·모래바람.
    public sealed class Precipitation : MonoBehaviour
    {
        private ParticleSystem system;
        private ParticleSystem.EmissionModule emission;
        private Transform follow;
        private Material material;
        private Texture2D texture;

        public static Precipitation Create(string name, Transform parent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var p = go.AddComponent<Precipitation>();
            p.system = go.AddComponent<ParticleSystem>();
            p.system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = p.system.main; main.loop = true; main.maxParticles = 20000; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 6; main.playOnAwake = false;
            var shape = p.system.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(80, 1, 80);
            p.emission = p.system.emission; p.emission.rateOverTime = 0;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            p.texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++)
            {
                var radius = new Vector2((x - 15.5f) / 15.5f, (y - 15.5f) / 15.5f).magnitude;
                p.texture.SetPixel(x, y, new Color(1, 1, 1, 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.35f, 1, radius))));
            }
            p.texture.Apply();
            p.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            p.material.SetTexture("_BaseMap", p.texture);
            p.material.SetFloat("_Surface", 1);
            p.material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            p.material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            p.material.SetFloat("_ZWrite", 0);
            p.material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            p.material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            renderer.sharedMaterial = p.material;
            p.system.Play();
            return p;
        }

        // rate 0 = 끔. stretch > 0 이면 빗줄기처럼 늘인다. drift = 옆바람.
        public void Set(float rate, Color color, float size, float fallSpeed, float stretch = 0, float drift = 0)
        {
            emission.rateOverTime = rate;
            var main = system.main; main.startColor = color; main.startSize = size; main.startSpeed = 0;
            main.startLifetime = 30f / Mathf.Max(1, fallSpeed);
            var velocity = system.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(drift - .5f, drift + .5f); velocity.y = new ParticleSystem.MinMaxCurve(-fallSpeed, -fallSpeed); velocity.z = new ParticleSystem.MinMaxCurve(-.5f, .5f);
            var renderer = GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = stretch > 0 ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            renderer.velocityScale = stretch; renderer.lengthScale = 1;
        }

        public float Rate => emission.rateOverTime.constant;

        private void OnDestroy() { Destroy(material); Destroy(texture); }

        private void LateUpdate()
        {
            if (follow == null) follow = FindFirstObjectByType<AntColony.Camera.IsometricCameraController>()?.transform;
            var focus = follow != null ? follow.GetComponent<AntColony.Camera.IsometricCameraController>().FocusPoint : Vector3.zero;
            transform.position = focus + Vector3.up * 25;
        }
    }
}
