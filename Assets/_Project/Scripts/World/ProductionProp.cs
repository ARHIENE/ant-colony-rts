using AntColony.Data;
using UnityEngine;

namespace AntColony.World
{
    public static class ProductionProp
    {
        public static string ForResource(ResourceType type) => type switch {
            ResourceType.Food => "Prop_PreservedRation",
            ResourceType.Stone => "Prop_StoneChunk",
            ResourceType.Drum or ResourceType.FilledFuelDrum => "Prop_OilDrum",
            ResourceType.GasCylinder => "Prop_GasCanister", _ => null
        };

        // Preserve the imported axis correction. Fit only the display; owner colliders and stock stay intact.
        public static GameObject Attach(Transform owner, string id, float width, Vector3 floor)
        {
            var prefab = Resources.Load<GameObject>("Kemono/Props/" + id);
            if (prefab == null) return null;
            var model = Object.Instantiate(prefab);
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { Object.Destroy(model); return null; }
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            var scale = width / Mathf.Max(bounds.size.x, bounds.size.z, .01f);
            model.transform.localScale *= scale;
            bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            model.transform.position += floor - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            model.transform.SetParent(owner, true);
            foreach (var collider in model.GetComponentsInChildren<Collider>()) Object.Destroy(collider);
            return model;
        }

        public static void ShowResource(ResourceNode node)
        {
            var id = ForResource(node.ResourceType);
            if (id == null || node.transform.Find(id + "(Clone)") != null) return;
            var box = node.GetComponent<Renderer>();
            if (box == null) return;
            var b = box.bounds;
            if (Attach(node.transform, id, Mathf.Max(b.size.x, b.size.z), new Vector3(b.center.x, b.min.y, b.center.z)) != null)
                box.forceRenderingOff = true;
        }
    }
}
