using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.UI
{
    // 작업 관리(2026-10-12 기획 ⑦): 드래그로 고른 건설·철거·이동·개보수 예정지와 요리 시설을 한꺼번에 다룬다.
    // 막힌 작업은 사유별 개수, 작업마다 필요/예약/도착 수량·대기 사유·담당 장수(누르면 위치로), 개별·일괄 취소. 손실이 생기는 취소는 두 번 눌러 확인한다.
    public sealed partial class GameMenuController
    {
        private List<Component> taskPick = new List<Component>();
        private int taskFilter;
        private bool waitingOnly;
        private Component confirmTask; private bool confirmAllTasks;
        public static IEnumerable<Component> TaskCandidates() => AntColony.Core.SceneObjects.All<BuildingConstructionSite>().Cast<Component>()
            .Concat(AntColony.Core.SceneObjects.All<Kitchen>().Where(k => !k.IsTable && !k.IsDead))
            .Concat(AntColony.Core.SceneObjects.All<Processor>().Where(p => !p.IsDead && (p.Current != null || p.Orders.Count > 0)))
            .Concat(AntColony.Core.SceneObjects.All<Workshop>().Where(w => !w.IsDead && (w.Jobs.Count > 0 || w.Orders.Count > 0)))
            .Concat(SupplyFacility.All.Where(s => s.Kind == BuildingKind.FuelCharger && s.Supply.containerOrders > 0))
            .Concat(AntColony.World.ResourceNode.Available.Where(n => n != null && n.NeedsDesignation && n.WorkRequested));

        private static string TaskName(Component c) => c is AntColony.World.ResourceNode n ? n.ResourceType.DisplayName() + " " + n.WorkName : c is BuildingConstructionSite s ? s.name.Replace("ConstructionSite", " 건설 예정지") : c is Kitchen k ? k.Data.displayName + " 요리" : c is BuildingBase b ? b.Data.displayName : c.name;
        private static string TaskWait(Component c) => AntColony.Core.WorkDiagnostics.Describe(c);
        private static bool TaskPaused(Component c) => c is SupplyFacility f ? f.Paused : c is Processor p ? p.Paused : c is Workshop w ? w.Paused : c is AntColony.World.ResourceNode n ? n.WorkPaused : c is BuildingConstructionSite s ? s.Paused : c is Kitchen k && k.Paused;
        private static void TaskPause(Component c, bool on) { if (c is SupplyFacility f) f.PauseContainers(on); else if (c is Processor p) p.Paused = on; else if (c is Workshop w) w.Paused = on; else if (c is AntColony.World.ResourceNode n) n.PauseWork(on); else if (c is BuildingConstructionSite s) s.SetPaused(on); else if (c is Kitchen k) k.Paused = on; }
        // 손실이 생기는 취소: 시작된 요리(소비한 재료). 예정지 취소는 재료를 전부 바닥에 돌려준다.
        private static bool TaskLoses(Component c) => c is Kitchen k && k.Current != null || c is Processor p && p.Current != null || c is Workshop w && w.Jobs.Count > 0 && w.Jobs[0].work > 0;
        private static void TaskCancel(Component c) { if (c is SupplyFacility f) f.CancelContainerWork(); else if (c is Processor p) { p.CancelCurrent(); p.Orders.Clear(); } else if (c is Workshop w) { while (w.Jobs.Count > 0) w.Cancel(0); w.Orders.Clear(); } else if (c is AntColony.World.ResourceNode n) n.CancelWork(); else if (c is BuildingConstructionSite s) s.Cancel(); else if (c is Kitchen k) { k.CancelCurrent(); k.Orders.Clear(); k.EjectStaged(); } }
        private static IEnumerable<CommanderAnt> TaskCrew(Component c) => c is BuildingConstructionSite s ? s.Builders.Concat(s.Couriers).Distinct()
            : AntUnitBase.Active.OfType<CommanderAnt>().Where(x => x.ServiceTarget == (Object)c || x.CurrentResourceNode == c || x.CraftingWorkshop == c || x.DeliverySite is Component sink && sink == c);
        private static string TaskAmounts(Component c) => c is BuildingConstructionSite s
            ? string.Join(" · ", s.Needs.Select(n => $"{n.type.DisplayName()} 필요 {n.need}/예약 {n.reserved + s.InTransit(n.type)}/도착 {n.delivered}"))
            : c is SupplyFacility f ? $"남은 용기 {f.Supply.containerOrders}개 · 작업 {f.Supply.work:0.#}/{SupplyFacility.ProcessSeconds:0}초"
            : c is Processor p && p.Current != null ? $"{p.Current.name} {p.Progress:P0}"
            : c is Workshop w && w.Jobs.Count > 0 ? $"제작 대기 {w.Jobs.Count}건 · 진행 {w.Jobs[0].work:0}"
            : c is Kitchen k && k.Current != null ? $"{k.Current.name} {k.Progress:P0}" : "";

        public void ShowTasks(List<Component> picked)
        {
            taskPick = picked ?? new List<Component>(); taskPick.RemoveAll(c => c == null);
            taskPick.RemoveAll(c => c is AntColony.World.ResourceNode n && !n.WorkRequested);
            if (taskPick.Count == 0) { ToastManager.Show("선택 범위에 관리할 작업이 없습니다."); Resume(); return; }
            var window = V5Window("작업 관리", $"선택한 작업 {taskPick.Count}개 · 우선순위 높은 순(9 최우선)");
            content = V5Section(window, "막힌 작업 / 일괄 지시", 20, 104, 520, 544);
            var blocked = taskPick.Select(TaskWait).Where(w => w != "진행 중").GroupBy(w => w).Select(g => $"{g.Key} {g.Count()}").ToList();
            MenuTheme.Text(content, blocked.Count > 0 ? "작업 상태: " + string.Join(" · ", blocked) : "대기 작업 없음", 15, 110);
            MenuTheme.Button(content, $"지정할 단계: {GatherDesignation.PriorityLabel} (눌러서 바꾸기)", () => { GatherDesignation.CyclePriority(); ShowTasks(taskPick); });
            MenuTheme.Button(content, $"선택 전체에 {GatherDesignation.PriorityLabel} 적용", () => {
                foreach (var c in taskPick.Where(c => c != null))
                    WorkPriorities.Set(c, GatherDesignation.PriorityLevel > WorkPriorities.Max ? WorkPriorities.Level(c) : GatherDesignation.PriorityLevel, GatherDesignation.PriorityLevel > WorkPriorities.Max);
                ShowTasks(taskPick);
            });
            MenuTheme.Button(content, "선택 전체 일시정지", () => { foreach (var c in taskPick.Where(c => c != null)) TaskPause(c, true); ShowTasks(taskPick); });
            MenuTheme.Button(content, "선택 전체 재개", () => { foreach (var c in taskPick.Where(c => c != null)) TaskPause(c, false); ShowTasks(taskPick); });
            var losing = taskPick.Count(TaskLoses);
            MenuTheme.Button(content, confirmAllTasks ? $"한 번 더 누르면 {taskPick.Count}개 취소" + (losing > 0 ? $"(진행 중 {losing}건 재료 손실)" : "") : "선택 전체 취소", () => {
                if (confirmAllTasks) { foreach (var c in taskPick.Where(c => c != null).ToList()) TaskCancel(c); confirmAllTasks = false; ShowTasks(taskPick.Where(c => c != null && !(c is BuildingConstructionSite)).ToList()); return; }
                confirmAllTasks = true; ShowTasks(taskPick);
            });
            MenuTheme.Text(content, "예정지 취소는 예약을 풀고 도착한 재료를 바닥에 돌려줍니다(시공 후에도 100%). 우선순위를 바꿔도 이미 예약·운반 중인 재료는 되돌리지 않습니다.", 16, 70);

            content = V5Section(window, "작업 목록", 554, 104, 526, 544);
            var kinds = new[] { "전체", "자연 작업", "건설", "생산" };
            MenuTheme.Button(content, "종류: " + kinds[taskFilter], () => { taskFilter = (taskFilter + 1) % kinds.Length; ShowTasks(taskPick); });
            MenuTheme.Button(content, waitingOnly ? "대기 중인 작업만 표시" : "진행·대기 모두 표시", () => { waitingOnly = !waitingOnly; ShowTasks(taskPick); });
            var visible = taskPick.Where(c => taskFilter == 0 || taskFilter == 1 && c is AntColony.World.ResourceNode
                || taskFilter == 2 && c is BuildingConstructionSite || taskFilter == 3 && (c is Kitchen || c is Processor || c is Workshop || c is SupplyFacility))
                .Where(c => !waitingOnly || !TaskCrew(c).Any(x => x.IsGatheringAnimation || x.IsBuildingAnimation || x.ServiceTarget == c || x.CraftingWorkshop == c));
            foreach (var c in visible.OrderByDescending(WorkPriorities.Yellow).ThenByDescending(WorkPriorities.Level).ThenBy(WorkPriorities.RequestedAt).ToList())
            {
                var row = c;
                var level = WorkPriorities.Yellow(row) ? "노란 경보" : "우선 " + WorkPriorities.Level(row);
                var amounts = TaskAmounts(row);
                MenuTheme.Button(content, "위치 → " + TaskName(row), () => { Resume(); AntColony.Core.SceneObjects.Any<AntColony.Camera.IsometricCameraController>()?.FocusOn(row.transform.position); });
                MenuTheme.Text(content, $"{TaskName(row)} · {level} · {TaskWait(row)}" + (amounts.Length > 0 ? "\n" + amounts : ""), 15, amounts.Length > 0 ? 86 : 64);
                foreach (var crew in TaskCrew(row).ToList())
                {
                    var who = crew;
                    MenuTheme.Button(content, "담당 → " + who.CommanderName, () => { Resume(); AntColony.Core.SceneObjects.Any<AntColony.Camera.IsometricCameraController>()?.FocusOn(who.Position); });
                }
                MenuTheme.Button(content, TaskPaused(row) ? "재개" : "일시정지", () => { TaskPause(row, !TaskPaused(row)); ShowTasks(taskPick); });
                MenuTheme.Button(content, confirmTask == row ? "한 번 더 누르면 취소(소비한 재료 손실)" : "취소", () => {
                    if (TaskLoses(row) && confirmTask != row) { confirmTask = row; ShowTasks(taskPick); return; }
                    confirmTask = null; TaskCancel(row); ShowTasks(taskPick.Where(x => x != null && x != row || row is Kitchen).ToList());
                });
            }
            V5Fit(window);
        }
    }
}
