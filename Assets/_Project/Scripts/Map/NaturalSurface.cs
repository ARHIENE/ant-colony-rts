using System.Collections.Generic;
using UnityEngine;

namespace AntColony.Map
{
    // Instance-owned materials: source Meshy textures and shared project assets stay intact.
    public sealed class NaturalSurface : MonoBehaviour
    {
        readonly List<Material> materials = new List<Material>();
        public static void Attach(GameObject model, bool foliage, bool evergreen = false)
        {
            if (model == null || model.GetComponent<NaturalSurface>() != null) return;
            var template = Resources.Load<Material>("SeasonFoliage");
            if (template == null) return;
            var owner = model.AddComponent<NaturalSurface>();
            var clones = new Dictionary<Material, Material>();
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    var source = slots[i]; if (source == null) continue;
                    if (!clones.TryGetValue(source, out var material))
                    {
                        material = new Material(template) { name = source.name + " (natural weather)" };
                        string map = source.HasProperty("_BaseMap") ? "_BaseMap" : source.HasProperty("_MainTex") ? "_MainTex" : null;
                        if (map != null) { material.SetTexture("_BaseMap", source.GetTexture(map)); material.SetTextureScale("_BaseMap", source.GetTextureScale(map)); material.SetTextureOffset("_BaseMap", source.GetTextureOffset(map)); }
                        material.SetColor("_BaseColor", source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white);
                        material.SetFloat("_Foliage", foliage ? 1 : 0); material.SetFloat("_Evergreen", evergreen ? 1 : 0);
                        clones.Add(source, material); owner.materials.Add(material);
                    }
                    slots[i] = material;
                }
                renderer.sharedMaterials = slots;
            }
        }
        void OnDestroy() { foreach (var material in materials) if (material != null) Destroy(material); }
    }
}
