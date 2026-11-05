using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;

public static class Storybook1105Checks
{
    static int count;
    static void Check(bool value, string label) { if (!value) throw new Exception(label); count++; }
    public static string Main()
    {
        count = 0;
        var root = new GameObject("Storybook verification (temporary)"); root.transform.position = new Vector3(500, 500, 500);
        try
        {
            var model = KemonoModel.Create(root.transform, 1105);
            var animator = model.GetComponentsInChildren<Animator>().First(a => a.runtimeAnimatorController != null);
            Check(animator.avatar != null && animator.avatar.isValid, "production avatar");
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Check(renderers.Length == 34, "all new modular meshes imported");
            foreach (var r in renderers)
            {
                Check(r.bones.All(b => b != null), "all skin bones resolve: " + r.name);
                Check(r.sharedMaterials.All(m => m != null && m.shader.name == "Universal Render Pipeline/Lit"), "URP materials: " + r.name);
            }
            foreach (var clip in new[] { "Idle_A", "Walk", "Tool_Chop", "Eat", "Sleep", "Hit", "Death" })
            {
                Check(animator.HasState(0, Animator.StringToHash("Base Layer." + clip)), "clip " + clip);
                animator.Play("Base Layer." + clip, 0, .4f); animator.Update(.01f);
                foreach (var r in renderers.Where(r => r.enabled))
                {
                    var mesh = new Mesh(); r.BakeMesh(mesh);
                    Check(mesh.vertices.All(v => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z)), "finite pose " + clip);
                    var worldSize = r.bounds.size.magnitude;
                    Check(worldSize > .05f && worldSize < 3, "world pose scale " + clip + ": " + r.name);
                    if (r.name.StartsWith("Part_Hair_")) Check(worldSize > .4f, "head remains full size " + clip);
                    Object.DestroyImmediate(mesh);
                }
            }
            var appearance = model.GetComponent<KemonoAppearance>();
            appearance.SetExpression("SLEEP");
            Check(renderers.Count(r => r.enabled && r.name.StartsWith("Part_Face_")) == 1 && renderers.Any(r => r.enabled && r.name.StartsWith("Part_Face_SLEEP_")), "sleep face selection");
            appearance.SetExpression("NEUTRAL");
            appearance.SetArmor(true); Check(renderers.Any(r => r.enabled && r.name.StartsWith("Part_Equipment_Armor_")), "armor visible");
            appearance.SetArmor(false); Check(!renderers.Any(r => r.enabled && r.name.StartsWith("Part_Equipment_Armor_")), "armor removed");
            var body = renderers.First(r => r.name.StartsWith("Part_Body_"));
            var before = new MaterialPropertyBlock(); body.GetPropertyBlock(before, 0);
            appearance.SetPaletteColor(new Color(.3f, .2f, .1f));
            var after = new MaterialPropertyBlock(); body.GetPropertyBlock(after, 0);
            Check(before.GetColor("_BaseColor") == after.GetColor("_BaseColor"), "starting tint preserves body/skin");
            KemonoModel.Hold(model, "Tool_Axe_Stone");
            var socket = model.GetComponentsInChildren<Transform>().First(t => t.name == "Socket.Tool.R");
            Check(socket.childCount > 0, "tool attached to original socket");

            foreach (var id in new[] { "CC_Hut", "CC_House", "CC_Storage", "CC_Watchtower" })
            {
                var facility = Object.Instantiate(Resources.Load<GameObject>("Facilities/" + id), root.transform);
                Check(facility.GetComponentsInChildren<Renderer>().Length == 1, "merged facility mesh " + id);
                Check(facility.GetComponentsInChildren<Transform>().Any(t => t.name.Contains("WorkPoint")), "work point " + id);
                var renderer = facility.GetComponentInChildren<Renderer>();
                var index = Array.FindIndex(renderer.sharedMaterials, m => FacilityMaterialVisual.IsStructure(m.name));
                Check(index >= 0, "structural slot " + id);
                var source = renderer.sharedMaterials[index]; var original = source.GetColor("_BaseColor");
                FacilityMaterialVisual.Apply(facility.transform, AntColony.Data.ResourceType.Copper);
                var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block, index);
                Check(block.GetColor("_BaseColor") == FacilityMaterialVisual.ColorFor(AntColony.Data.ResourceType.Copper), "copper appearance " + id);
                Check(block.GetFloat("_Metallic") > 0 && source.GetColor("_BaseColor") == original, "instance-only finish " + id);
                FacilityMaterialVisual.Apply(facility.transform, AntColony.Data.ResourceType.Soil); renderer.GetPropertyBlock(block, index);
                Check(block.GetColor("_BaseColor") == original, "restore authored finish " + id);
                var trim = Array.FindIndex(renderer.sharedMaterials, m => !FacilityMaterialVisual.IsStructure(m.name));
                if (trim >= 0) { renderer.GetPropertyBlock(block, trim); Check(block.isEmpty, "detail colours preserved " + id); }

                // Exercise the exact setter called by renovation completion and save restoration.
                var host = new GameObject("Inactive building fixture"); host.SetActive(false); host.transform.SetParent(root.transform);
                var building = host.AddComponent<BuildingBase>(); var visual = host.AddComponent<FacilityVisual>();
                typeof(FacilityVisual).GetProperty("Model").SetValue(visual, facility.transform);
                building.MainMaterial = AntColony.Data.ResourceType.Stone;
                renderer.GetPropertyBlock(block, index);
                Check(block.GetColor("_BaseColor") == FacilityMaterialVisual.ColorFor(AntColony.Data.ResourceType.Stone), "material setter refreshes model " + id);
            }
            var result = "PASS " + count + " checks: imported rig/poses/expression/armor/tool, isolated material finish and renovation/load setter.";
            File.WriteAllText("art/storybook-1105/unity-validation.txt", result);
            return result;
        }
        finally { Object.DestroyImmediate(root); }
    }
}
