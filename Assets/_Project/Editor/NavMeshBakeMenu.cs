using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

// 씬의 모든 NavMeshSurface를 한 번에 베이크하는 메뉴. 인스펙터의 Bake 버튼과 동일한 동작.
public static class NavMeshBakeMenu
{
    [MenuItem("Tools/Ant Colony/Bake All NavMesh Surfaces")]
    public static void BakeAll()
    {
        var surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        foreach (var surface in surfaces)
        {
            surface.BuildNavMesh();
        }
        Debug.Log($"[NavMeshBakeMenu] Baked {surfaces.Length} NavMeshSurface(s).");
    }
}
