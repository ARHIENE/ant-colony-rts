using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

// 2026-11-01 (GAMEFEEL P1-25): 새 게임 본거지 지하 매장층이 실제 지형에서 걸어서 닿고 시설을 세울 수 있는 자리인지.
// 매장 총량이 충분해도 접근이 막히면 몇 시간 뒤 진행이 끊긴다. Play 모드 + 게임 시작 상태에서 실행.
public static class Review1101UndergroundReach
{
    public static Task<string> Main()
    {
        var survey = AntColony.World.UndergroundSurvey.Instance;
        var home = AntColony.World.WorldMapManager.Instance.HomePosition;
        var sb = new StringBuilder(); int total = 0, reach = 0, fail = 0;
        var path = new NavMeshPath();
        foreach (var l in survey.Layers.Where(l => l.map == 0 && l.type >= 0))
        {
            total++;
            var ok = NavMesh.SamplePosition(l.position, out var hit, AntColony.Buildings.UndergroundScanner.Radius, NavMesh.AllAreas)
                && NavMesh.CalculatePath(home, hit.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
            var water = Physics.Raycast(l.position + Vector3.up * 20, Vector3.down, out var g, 60) && g.collider.name.ToLower().Contains("water");
            if (ok && !water) reach++;
            else { fail++; sb.Append($"닿지 않음: {(AntColony.Data.ResourceType)l.type} {l.total} @({l.position.x:0},{l.position.z:0}) water={water}\n"); }
        }
        var byType = survey.Layers.Where(l => l.map == 0 && l.type >= 0).GroupBy(l => (AntColony.Data.ResourceType)l.type)
            .Select(g => $"{g.Key} {g.Count()}층/{g.Sum(x => x.total)}");
        return Task.FromResult($"{(fail == 0 && total > 0 ? "PASS" : "FAIL")} 본거지 매장층 {reach}/{total} 도달\n{string.Join(", ", byType)}\n{sb}");
    }
}
