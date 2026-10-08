using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using RT = AntColony.Data.ResourceType;

// 벽 줄 드래그(2026-10-03): 가로·세로 한 줄 칸 계산, 칸마다 예정지, 자원 부족 시 중단, 막힌 칸 제외.
public static class WallDragChecks
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL: " + label); checks++; }
    static int Sites() => Object.FindObjectsByType<BuildingConstructionSite>().Count(s => s.name.StartsWith("LeafWall"));

    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "play mode");
        for (int i = 0; i < 1800 && SaveSystem.Busy; i++) await Task.Delay(50);
        SaveSystem.NewGame(new NewGameOptions { seed = 261003, mapSize = MapSize.Small });
        for (int i = 0; i < 1800 && SaveSystem.Busy; i++) await Task.Delay(50);
        GameMenuController.Instance.Resume(); Time.timeScale = 0;
        // 새 게임 로딩 직후 자동 채집이 시작됐어도 건설 검사는 대기 상태에서 시작한다.
        foreach (var commander in CommanderRoster.Instance.Commanders)
        {
            commander.SetJobEnabled(CommanderJobs.All, false);
            commander.CommandStop();
        }
        var placement = Object.FindAnyObjectByType<BuildingPlacementController>();
        var rm = ResourceManager.Instance;
        var builder = CommanderRoster.Instance.Commanders.First(c => c.CanStartConstruction);

        var home = WorldMapManager.Instance.HomePosition + new Vector3(14, 0, -14);
        Check(NavMesh.SamplePosition(home, out var hit, 10, NavMesh.AllAreas), "open ground");
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new object[] { BuildingKind.LeafWall, UnitRole.Worker });
        var snap = typeof(BuildingPlacementController).GetMethod("GetPlacementPosition", BindingFlags.NonPublic | BindingFlags.Instance);
        Vector3 Start() { return (Vector3)snap.Invoke(placement, new object[] { template, hit.position }); }

        // 칸 계산: 가로로 4m 끌면 5칸, 세로가 더 길면 세로 줄, 최대 칸 수 제한.
        Check(placement.BeginPlacement(BuildingKind.LeafWall, UnitRole.Worker, builder), "begin wall placement");
        var start = Start();
        var cells = placement.LineCells(start, start + new Vector3(4.2f, 0, 1f));
        Check(cells.Count == 5 && cells.All(c => Mathf.Approximately(c.z, start.z)) && Mathf.Approximately(cells[4].x - start.x, 4), "horizontal line of 5");
        var vertical = placement.LineCells(start, start + new Vector3(1f, 0, -3f));
        Check(vertical.Count == 4 && vertical.All(c => Mathf.Approximately(c.x, start.x)) && vertical[3].z < start.z, "vertical line follows longer axis");
        Check(placement.LineCells(start, start + Vector3.right * 500).Count == BuildingPlacementController.MaxLineCells, "line capped");

        // 넉넉한 자원: 5칸 모두 예정지, 첫 칸은 고른 장수가 맡는다.
        var cost = template.GetComponent<BuildingBase>().Data;
        rm.Add(RT.Soil, 1000); rm.Add(RT.Food, 1000);
        var before = Sites();
        Check(placement.PlaceLine(cells, cells.ConvertAll(_ => true)) == 5 && Sites() == before + 5, "five sites placed");
        Check(builder.ConstructionTarget != null && !placement.IsPlacing, "builder takes first cell; placement ends");

        // 자원 부족: 낼 수 있는 칸까지만. 막힌 칸은 건너뛴다.
        Check(placement.BeginPlacement(BuildingKind.LeafWall, UnitRole.Worker, CommanderRoster.Instance.Commanders.First(c => c.CanStartConstruction)), "begin again");
        var row2 = placement.LineCells(start + Vector3.forward * 6, start + Vector3.forward * 6 + Vector3.right * 4);
        rm.TrySpend(0, rm.GetAmount(RT.Soil), 0); rm.Add(RT.Soil, cost.soilCost * 3);
        var valid = row2.ConvertAll(_ => true); valid[1] = false;
        before = Sites();
        Check(placement.PlaceLine(row2, valid) == 3 && Sites() == before + 3 && rm.GetAmount(RT.Soil) < cost.soilCost, "stops when materials run out, skips blocked cell");
        // Shift+드래그(2026-10-08): 배치 후에도 선택 유지.
        rm.Add(RT.Soil, 1000); placement.CancelPlacement();
        Check(placement.BeginPlacement(BuildingKind.LeafWall, UnitRole.Worker, CommanderRoster.Instance.Commanders.First(c => c.CanStartConstruction)), "begin shift");
        var row3 = placement.LineCells(start + Vector3.forward * 9, start + Vector3.forward * 9 + Vector3.right * 2);
        Check(placement.PlaceLine(row3, row3.ConvertAll(_ => true), true) == 3 && placement.IsPlacing, "shift placement keeps mode");
        placement.CancelPlacement();
        await Task.Yield();
        return "PASS " + checks + " wall drag checks";
    }
}
