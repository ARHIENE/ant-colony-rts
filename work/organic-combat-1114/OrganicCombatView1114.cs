using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;
public static class OrganicCombatView1114
{
    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Play required");
        Time.timeScale = 0;
        var local = SceneObjects.Any<LocalIncursions>();
        var spawned = local.Visitors.Where(v => v != null && v.isActiveAndEnabled).ToArray();
        if (spawned.Count(v => v.IsOrganic) != 14 || spawned.OfType<EnemyCommander>().Count() != 1)
            throw new Exception("First wave species/leader mismatch");
        foreach (var m in spawned) { m.enabled = false; if (m.Agent.enabled && m.Agent.isOnNavMesh) m.Agent.ResetPath(); }
        var center = spawned.First(m => m.IsOrganic).Position;
        var creature = (WildMonster)typeof(LocalIncursions).GetMethod("RestoreSpawn", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(local, new object[] { false, center + Vector3.right * 4 });
        creature.name = "OrganicHordeCheck"; creature.ConfigureOrganic(OrganicEnemyKind.WallRam);
        typeof(WildMonster).GetMethod("ConfigureHorde", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(creature, new object[] { 20 });
        creature.MakeRaider(); creature.gameObject.SetActive(true); creature.enabled = false;
        var visual = creature.GetComponent<HordeVisual>();
        await Task.Delay(200);
        if (!visual.enabled || visual.ExtraBodies != 19) throw new Exception("Organic GPU horde failed: " + visual.ExtraBodies);
        var body = typeof(HordeVisual).GetField("body", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(visual);
        var mesh = (Mesh)body.GetType().GetField("mesh").GetValue(body);
        var scale = (Vector3)typeof(HordeVisual).GetField("scale", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(visual);
        var size = Vector3.Scale(mesh.bounds.size, scale);
        if (size.magnitude > 6 || size.magnitude < .1f) throw new Exception("Instanced body scale invalid: " + size);
        var camera = SceneObjects.Any<AntColony.Camera.IsometricCameraController>();
        typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(camera, new object[] { center + Vector3.right * 2, 0f, 11f });
        await Task.Delay(250);
        string result = "PASS first-wave 14 organic + original commander; GPU horde 20 bodies (19 instanced); world size=" + size + "; frame prepared";
        File.WriteAllText("work/organic-combat-1114/combat-view.txt", result);
        return result;
    }
}
