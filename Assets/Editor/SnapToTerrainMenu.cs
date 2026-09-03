using UnityEditor;
using UnityEngine;

// 랜덤맵(MapGenerator) 생성 후, 미리 배치해둔 건물/자원/몬스터를 지형 표면 위로 스냅시키는 유틸리티.
// 오브젝트 피벗이 전부 중심(center)인 기본 Cube/Sphere라 접지 시 +0.5 오프셋을 더한다.
public static class SnapToTerrainMenu
{
    private static readonly string[] TargetNames =
    {
        "QueenChamber", "Barracks", "Storage", "DigSite", "ExpansionZone",
        "FoodNode1", "FoodNode2", "SoilNode1", "SoilNode2", "WildMonster"
    };

    [MenuItem("Tools/Ant Colony/Snap Scene Objects To Terrain")]
    public static void SnapAll()
    {
        var terrainGo = GameObject.Find("MapGenerator") ?? GameObject.Find("TerrainGenerator");
        if (terrainGo == null)
        {
            Debug.LogWarning("[SnapToTerrainMenu] MapGenerator/TerrainGenerator not found in scene.");
            return;
        }

        var collider = terrainGo.GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogWarning("[SnapToTerrainMenu] MapGenerator has no Collider yet (generate the map first).");
            return;
        }

        var snapped = 0;
        foreach (var name in TargetNames)
        {
            var go = GameObject.Find(name);
            if (go == null) continue;

            var origin = new Vector3(go.transform.position.x, 200f, go.transform.position.z);
            if (Physics.Raycast(origin, Vector3.down, out var hit, 500f))
            {
                var pos = go.transform.position;
                pos.y = hit.point.y + 0.5f;
                go.transform.position = pos;
                snapped++;
            }
        }

        Debug.Log($"[SnapToTerrainMenu] Snapped {snapped}/{TargetNames.Length} object(s) onto the terrain.");
    }
}
