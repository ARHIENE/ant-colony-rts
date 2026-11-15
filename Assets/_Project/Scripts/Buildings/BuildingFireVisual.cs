using System.Linq;
using UnityEngine;

namespace AntColony.Buildings
{
    // Imported facility meshes hide the old cube renderer. Show fire independently of its material.
    [RequireComponent(typeof(WallFire))]
    public sealed class BuildingFireVisual : MonoBehaviour
    {
        WallFire fire;
        ParticleSystem particles;
        Material material;
        Texture2D texture;
        Transform measuredModel;
        void Awake() => fire = GetComponent<WallFire>();
        void LateUpdate()
        {
            if (!fire || !fire.Burning) { if (particles && particles.isPlaying) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); return; }
            if (!particles) Create();
            var model = GetComponent<FacilityVisual>()?.Model;
            if (model != measuredModel) { measuredModel = model; Measure(); }
            if (!particles.isPlaying) particles.Play();
        }
        void Create()
        {
            var go = new GameObject("Building fire"); go.transform.SetParent(transform, true);
            particles = go.AddComponent<ParticleSystem>(); particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main; main.playOnAwake = false; main.maxParticles = 96; main.startLifetime = new ParticleSystem.MinMaxCurve(.6f, 1.4f);
            main.startSpeed = 0; main.startSize = new ParticleSystem.MinMaxCurve(.45f, .8f); main.simulationSpace = ParticleSystemSimulationSpace.World; main.useUnscaledTime = true;
            var emission = particles.emission; emission.rateOverTime = 28;
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Box;
            var velocity = particles.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = new ParticleSystem.MinMaxCurve(.8f, 1.6f); velocity.x = new ParticleSystem.MinMaxCurve(-.15f, .15f); velocity.z = new ParticleSystem.MinMaxCurve(-.15f, .15f);
            var color = particles.colorOverLifetime; color.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(new Color(1, .8f, .15f), 0), new GradientColorKey(new Color(1, .24f, .03f), .35f), new GradientColorKey(new Color(.2f, .16f, .12f), 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .15f), new GradientAlphaKey(.8f, .6f), new GradientAlphaKey(0, 1) }); color.color = gradient;
            texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) {
                float u = (x - 15.5f) / 15.5f, v = y / 31f;
                float width = Mathf.Sin(v * Mathf.PI) * Mathf.Lerp(.9f, .15f, v);
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((width - Mathf.Abs(u)) * 8)));
            }
            texture.Apply();
            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); material.SetTexture("_BaseMap", texture);
            material.SetFloat("_Surface", 1); material.SetFloat("_SrcBlend", 5); material.SetFloat("_DstBlend", 10); material.SetFloat("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
            var renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material; renderer.maxParticleSize = .06f;
            Measure();
        }
        void Measure()
        {
            var visible = GetComponentsInChildren<Renderer>().Where(r => !(r is ParticleSystemRenderer) && r.enabled && !r.forceRenderingOff).ToArray();
            var bounds = new Bounds(transform.position, Vector3.one);
            if (visible.Length > 0) { bounds = visible[0].bounds; foreach (var r in visible.Skip(1)) bounds.Encapsulate(r.bounds); }
            particles.transform.position = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * .7f, bounds.center.z);
            var shape = particles.shape; shape.scale = new Vector3(Mathf.Clamp(bounds.size.x * .7f, .3f, 4), .2f, Mathf.Clamp(bounds.size.z * .7f, .3f, 4));
        }
        void OnDisable() { if (particles) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
        void OnDestroy() { if (particles) Destroy(particles.gameObject); if (material) Destroy(material); if (texture) Destroy(texture); }
    }
}
