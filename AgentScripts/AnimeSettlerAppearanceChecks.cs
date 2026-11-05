using System;
using System.Linq;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;

public static class AnimeSettlerAppearanceChecks
{
    public static string Main()
    {
        var root = new GameObject("Temporary appearance checks");
        var mesh = new Mesh { vertices = new[] { Vector3.zero, Vector3.right, Vector3.up }, triangles = new[] { 0, 1, 2 } };
        mesh.AddBlendShapeFrame("Face.Smile", 100, new Vector3[3], new Vector3[3], new Vector3[3]);
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Meadow_Hair" };
        var texture = new Texture2D(2, 2);
        material.SetTexture("_BaseMap", texture);
        var count = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); count++; }
        SkinnedMeshRenderer Part(string name)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform);
            var r = go.AddComponent<SkinnedMeshRenderer>(); r.sharedMesh = mesh; r.sharedMaterial = material; return r;
        }
        try
        {
            foreach (var name in new[] { "BOB", "CROP", "LONG", "PONY", "TWIN", "UPDO" }) Part("Part_Hair_" + name + "_00");
            foreach (var name in new[] { "CITIZEN", "FARMER", "ARTISAN", "GUARD" }) Part("Part_Outfit_" + name + "_00");
            var face = Part("Part_Face_NEUTRAL_00");
            var appearance = root.AddComponent<KemonoAppearance>(); appearance.Initialize(17);
            foreach (var slot in new[] { "Hair", "Outfit" })
            {
                var choices = appearance.GetVariants(slot);
                Check(choices.Length == (slot == "Hair" ? 6 : 4), slot + " options");
                foreach (var choice in choices)
                {
                    Check(appearance.SetVariant(slot, choice), "Switch " + choice);
                    var active = root.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("Part_" + slot + "_") && r.enabled).ToArray();
                    Check(active.Length == 1 && active[0].name.Contains("_" + choice + "_"), "Overlapping or wrong " + slot);
                }
                var before = root.GetComponentsInChildren<Renderer>().Where(r => r.enabled).Select(r => r.name).ToArray();
                Check(!appearance.SetVariant(slot, "MISSING"), "Reject unknown variant");
                Check(before.SequenceEqual(root.GetComponentsInChildren<Renderer>().Where(r => r.enabled).Select(r => r.name)), "Unknown variant changed visibility");
            }
            foreach (var expression in new[] { "SMILE", "LAUGH", "SURPRISE", "DETERMINED", "SAD", "SLEEP", "PAIN", "DEAD", "NEUTRAL" })
            { appearance.SetExpression(expression); Check(face.enabled, "Morph face hidden: " + expression); }
            appearance.SetVariant("Outfit", "FARMER"); appearance.SetArmor(true);
            Check(root.GetComponentsInChildren<Renderer>().Single(r=>r.enabled && r.name.StartsWith("Part_Outfit_")).name.Contains("_GUARD_"), "Armor uses guard outfit");
            appearance.SetArmor(false);
            Check(root.GetComponentsInChildren<Renderer>().Single(r=>r.enabled && r.name.StartsWith("Part_Outfit_")).name.Contains("_FARMER_"), "Armor restores chosen outfit");
            var hair = root.GetComponentsInChildren<Renderer>().First(r => r.name.StartsWith("Part_Hair_"));
            var block = new MaterialPropertyBlock(); hair.GetPropertyBlock(block, 0);
            Check(block.GetTexture("_BaseMap") == null && material.GetTexture("_BaseMap") == texture, "Hair texture overwritten");
            return count + " appearance checks passed; synthetic parts only, imported model and gameplay still require validation.";
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(mesh); Object.DestroyImmediate(material); Object.DestroyImmediate(texture); }
    }
}
