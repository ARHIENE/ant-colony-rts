using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using UnityEngine;

public static class RoomTraversalChecks
{
    public static string Main()
    {
        var flood = typeof(RoomSystem).GetMethod("Flood", BindingFlags.NonPublic | BindingFlags.Static);
        var blocked = new HashSet<Vector2Int>();
        for (var x = 0; x <= 31; x++) for (var y = 0; y <= 31; y++)
            if (x == 0 || y == 0 || x == 31 || y == 31) blocked.Add(new Vector2Int(x, y));
        var seen = new HashSet<Vector2Int>();
        for (var x = 1; x <= 30; x++) for (var y = 1; y <= 30; y++)
        {
            var start = new Vector2Int(x, y);
            if (seen.Contains(start)) continue;
            var region = flood.Invoke(null, new object[] { start, blocked, seen, new Vector2Int(-2, -2), new Vector2Int(33, 33), 400 });
            if (region != null) throw new Exception("Oversized connected area was split into a false room");
        }
        if (seen.Count != 900) throw new Exception("Flood did not visit the entire connected area");
        return "PASS oversized room stays outdoors (900 cells)";
    }

    public static async Task<string> Roofs()
    {
        if (!Application.isPlaying) throw new Exception("Play mode required");
        var room = new Room(); room.Cells.Add(Vector2Int.zero);
        RoomRoofs.Rebuild(new[] { room });
        var owner = UnityEngine.Object.FindAnyObjectByType<RoomRoofs>();
        var mesh = owner.GetComponentInChildren<MeshFilter>().sharedMesh;
        var material = owner.GetComponentInChildren<MeshRenderer>().sharedMaterial;
        RoomRoofs.Rebuild(Array.Empty<Room>());
        await Task.Yield(); await Task.Yield();
        if (mesh != null) throw new Exception("Rebuilding roofs leaked the old mesh");
        RoomRoofs.Rebuild(new[] { room });
        mesh = owner.GetComponentInChildren<MeshFilter>().sharedMesh;
        UnityEngine.Object.Destroy(owner.gameObject);
        await Task.Yield(); await Task.Yield();
        if (mesh != null || material != null) throw new Exception("Destroying roofs leaked native resources");
        RoomSystem.MarkDirty();
        return "PASS roof mesh and material lifecycle";
    }
}
