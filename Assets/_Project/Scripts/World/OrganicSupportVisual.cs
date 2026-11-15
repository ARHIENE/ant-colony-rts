using UnityEngine;

namespace AntColony.World
{
    public sealed class OrganicSupportVisual : MonoBehaviour
    {
        WildMonster owner;
        LineRenderer ring;
        Material material;
        void Awake()
        {
            owner = GetComponent<WildMonster>();
            var go = new GameObject("OrganicSupportRange"); go.transform.SetParent(transform, false);
            ring = go.AddComponent<LineRenderer>(); ring.useWorldSpace = true; ring.loop = true; ring.widthMultiplier = .045f;
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); ring.sharedMaterial = material;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        void LateUpdate()
        {
            bool heal = owner.OrganicPhase == 2, mist = owner.OrganicMistLeft > 0, burrow = owner.OrganicPhase == 6;
            ring.enabled = !owner.IsDead && (heal || mist || burrow);
            if (!ring.enabled) return;
            material.SetColor("_BaseColor", heal ? new Color(.55f, 1, .9f) : mist ? new Color(.6f, .2f, .85f) : new Color(.6f, .4f, .15f));
            ring.positionCount = 32;
            float radius = burrow ? 1.2f + Mathf.Sin(Time.time * 5) * .1f : 5;
            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI * 2 / 32;
                ring.SetPosition(i, owner.Position + new Vector3(Mathf.Cos(angle) * radius, .07f, Mathf.Sin(angle) * radius));
            }
        }
        public static void Pulse(Vector3 from, Vector3 to, Color color)
        {
            var go = new GameObject("회복 포자 흐름");
            var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = true; line.widthMultiplier = .055f; line.positionCount = 2;
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); material.SetColor("_BaseColor", color); line.sharedMaterial = material;
            line.SetPosition(0, from + Vector3.up * .6f); line.SetPosition(1, to + Vector3.up * .4f);
            var pulse = go.AddComponent<OrganicPulseLifetime>(); pulse.Material = material;
        }
        void OnDestroy() { if (material != null) Destroy(material); }
    }
    public sealed class OrganicPulseLifetime : MonoBehaviour
    {
        public Material Material;
        float left = .6f;
        void Update() { left -= Time.deltaTime; if (left <= 0) Destroy(gameObject); }
        void OnDestroy() { if (Material != null) Destroy(Material); }
    }
}
