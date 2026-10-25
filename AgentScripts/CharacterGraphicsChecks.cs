using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

public static class CharacterGraphicsChecks
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL " + label); checks++; }
    static Vector3[] Skin(GameObject owner)
    {
        var vertices = new List<Vector3>();
        foreach (var renderer in owner.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.enabled))
        {
            var mesh = new Mesh(); renderer.BakeMesh(mesh);
            vertices.AddRange(mesh.vertices.Select(renderer.transform.TransformPoint)); Object.Destroy(mesh);
        }
        return vertices.ToArray();
    }
    static void Motion(GameObject owner, Animator animator, string clip)
    {
        Check(animator.HasState(0, Animator.StringToHash("Base Layer." + clip)), "state connected " + clip);
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.Play("Base Layer." + clip, 0, 0); animator.Update(.15f); var before = Skin(owner);
        animator.Update(.55f); var after = Skin(owner);
        Check(before.Length > 0 && before.Length == after.Length && before.Where((v, i) => (v - after[i]).sqrMagnitude > .0000001f).Any(), "mesh actually animates " + clip);
    }
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "play mode");
        var deadline = Time.realtimeSinceStartup + 50;
        while (AntColony.Save.SaveSystem.Busy && Time.realtimeSinceStartup < deadline) await Task.Delay(50);
        Check(!AntColony.Save.SaveSystem.Busy, "scene ready");
        var scale = Time.timeScale; Time.timeScale = 0;
        var root = new GameObject("Character graphics checks");
        try
        {
            var home = AntColony.World.WorldMapManager.Instance.HomePosition;
            var hero = GameObject.CreatePrimitive(PrimitiveType.Capsule); hero.transform.SetParent(root.transform);
            hero.transform.position = home; hero.transform.localScale = Vector3.one * .6f;
            AntColony.Units.AntVisual.Attach(hero);
            var animator = AntColony.Units.RigAnimator.Find(hero);
            Check(animator != null && animator.avatar != null && animator.avatar.isValid, "hero has valid animator and avatar");
            foreach (var clip in new[] { "Walk", "Attack", "Tool_Chop", "Tool_Mine", "Tool_Harvest" }) Motion(hero, animator, clip);
            foreach (AntColony.Buildings.Species species in Enum.GetValues(typeof(AntColony.Buildings.Species)))
            {
                var critter = AntColony.World.Critter.Spawn(species, home + Vector3.right * 8, true);
                critter.transform.SetParent(root.transform, true);
                await Task.Yield();
                var visual = critter.GetComponent<AntColony.World.CritterVisual>();
                animator = AntColony.Units.RigAnimator.Find(critter.gameObject);
                Check(visual != null && visual.enabled && animator != null && animator.avatar != null && animator.avatar.isValid, "critter rig connected " + species);
                Check(critter.GetComponent<Renderer>().forceRenderingOff, "capsule hidden " + species);
                Check(critter.GetComponentsInChildren<Renderer>().Where(r => r.transform != critter.transform).SelectMany(r => r.sharedMaterials)
                    .All(m => m != null && m.shader.name == "Universal Render Pipeline/Lit"), "critter URP materials " + species);
                Motion(critter.gameObject, animator, species == AntColony.Buildings.Species.Bee ? "Fly" : "Walk");
                var meshes = critter.GetComponentsInChildren<SkinnedMeshRenderer>();
                var bounds = meshes.Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
                Check(bounds.size.y > .02f && bounds.size.y < 2.2f, "critter character-relative height " + species + " " + bounds.size);
                critter.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
                Time.timeScale = 1; await Task.Yield(); await Task.Yield();
                critter.transform.position += Vector3.forward * .2f;
                typeof(AntColony.World.CritterVisual).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(visual, null);
                var bones = animator.GetComponentsInChildren<Transform>();
                var forward = bones.First(t => t.name == "Head").position - bones.First(t => t.name == (species == AntColony.Buildings.Species.Silkworm ? "Seg0" : "Body")).position; forward.y = 0;
                Check(Vector3.Dot(forward.normalized, Vector3.forward) > .8f, "critter faces movement " + species);
                Time.timeScale = 0;
            }
            return "PASS " + checks + " character/critter rig, animation and material checks";
        }
        finally { Time.timeScale = scale; Object.Destroy(root); }
    }
}
