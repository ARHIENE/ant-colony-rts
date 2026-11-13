using System;
using System.Linq;
using System.Collections.Generic;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;

public static class EarlyFlow1113Routes
{
    public static string Validate()
    {
        var pile = ResourceNode.Available.First(n => n.IsLooseCargo && n.ResourceType == ResourceType.Food
            && (n.transform.position - new Vector3(202.62f, 10.07f, 204.35f)).sqrMagnitude < .25f);
        int checks = 0;
        foreach (var c in CommanderRoster.Instance.Commanders.Where(c => c != null && !c.IsDead))
        {
            var before = c.Position;
            if (!c.TryWorkApproach(pile.transform.position, out var point)) throw new Exception("Nearby food still unreachable: " + c.CommanderName);
            var path = new NavMeshPath(); c.Agent.CalculatePath(point, path);
            if (path.status != NavMeshPathStatus.PathComplete || Vector3.Distance(point, pile.transform.position) > 2.25f)
                throw new Exception("Fallback must be a local complete path");
            if (float.IsInfinity(c.TravelDistance(pile.transform.position))) throw new Exception("Resolved pile must not rank as infinite travel");
            if (c.TryWorkApproach(new Vector3(3000, 0, 3000), out _) || !float.IsInfinity(c.TravelDistance(new Vector3(3000, 0, 3000))))
                throw new Exception("Unreachable distant target must remain rejected");
            if (c.Position != before) throw new Exception("Path query moved the character");
            checks += 5;
        }
        var result = "PASS " + checks + " route assertions on the unmodified natural save: local complete path, finite ranking, distant rejection, no teleport.";
        System.IO.File.WriteAllText("work/early-flow-1113/route-regression.txt", result); return result;
    }
    public static string Inspect()
    {
        var c = CommanderRoster.Instance.Commanders.First(x => x != null && !x.IsDead);
        var nodes = ResourceNode.Available.Where(n => n.IsLooseCargo && n.ResourceType == ResourceType.Food && !n.IsDepleted
            && (n.transform.position - c.Position).sqrMagnitude < 400 && !c.TryWorkApproach(n.transform.position, out _)).ToArray();
        var rows = new List<string>();
        foreach (var n in nodes.Take(1))
        {
            var p = n.transform.position;
            rows.Add("PILE " + p + " amount=" + n.AmountRemaining);
            for (float radius = 0; radius <= 3; radius += 1)
                for (int i = 0; i < (radius == 0 ? 1 : 8); i++)
                {
                    var q = p + Quaternion.Euler(0, i * 45, 0) * Vector3.forward * radius;
                    if (!NavMesh.SamplePosition(q, out var hit, 1, NavMesh.AllAreas)) { rows.Add("NO SAMPLE " + q); continue; }
                    var path = new NavMeshPath(); c.Agent.CalculatePath(hit.position, path);
                    rows.Add("radius=" + radius + " hit=" + hit.position + " offset=" + Vector3.Distance(hit.position, p) + " path=" + path.status);
                }
        }
        return string.Join("\n", rows);
    }
}
