using UnityEngine;

namespace AntColony.Map
{
    // 화면(카메라 초점) 위에서 떨어지는 입자: 계절의 꽃잎·낙엽·눈, 날씨의 비·눈보라·모래바람.
    public sealed class Precipitation : MonoBehaviour
    {
        private ParticleSystem system;
        private ParticleSystem.EmissionModule emission;
        private Transform follow;

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
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default"));
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
            velocity.x = new ParticleSystem.MinMaxCurve(drift - .5f, drift + .5f); velocity.y = -fallSpeed; velocity.z = new ParticleSystem.MinMaxCurve(-.5f, .5f);
            var renderer = GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = stretch > 0 ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            renderer.velocityScale = stretch; renderer.lengthScale = 1;
        }

        public float Rate => emission.rateOverTime.constant;

        private void LateUpdate()
        {
            if (follow == null) follow = FindFirstObjectByType<AntColony.Camera.IsometricCameraController>()?.transform;
            var focus = follow != null ? follow.GetComponent<AntColony.Camera.IsometricCameraController>().FocusPoint : Vector3.zero;
            transform.position = focus + Vector3.up * 25;
        }
    }
}
