using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        private BuildingBase repairPick;
        public void RepairOverview()
        {
            var targets = SceneObjects.All<BuildingBase>().Where(BuildingRepair.Needed).OrderBy(b => b.CurrentHealth / b.MaxHealth).ToArray();
            if (repairPick == null || !targets.Contains(repairPick)) repairPick = targets.FirstOrDefault();
            var window = V5Window("수리", "시설·차량의 실제 내구도 · 수리 작업표와 현장 작업에 연결");
            content = V5Section(window, $"손상 대상 {targets.Length}", 20, 104, 520, 544);
            foreach (var target in targets)
            {
                var pick = target;
                var label = target.Data != null ? target.Data.displayName : target.name;
                MenuTheme.Button(content, $"{label} · {target.CurrentHealth:0}/{target.MaxHealth:0}", () => { repairPick = pick; RepairOverview(); });
            }
            if (targets.Length == 0) MenuTheme.Text(content, "수리가 필요한 시설·차량이 없습니다.", 15, 56);
            MenuTheme.Button(content, "수리 작업 우선순위", WorkSchedule);
            content = V5Section(window, "진행 · 담당자", 554, 104, 526, 544);
            if (repairPick != null)
            {
                var target = repairPick;
                var status = MenuTheme.Text(content, "", 15, 76);
                refreshDutyScreen = () => {
                    if (target == null) { status.text = "대상이 없어졌습니다."; return; }
                    var workers = SortedCommanders().Where(c => c.ServiceTarget == target && c.ServiceJob == CommanderJobs.Repair).ToArray();
                    status.text = $"내구도 {target.CurrentHealth:0}/{target.MaxHealth:0}\n담당: " + (workers.Length == 0 ? "대기" : string.Join(" · ", workers.Select(c => c.CommanderName)));
                };
                refreshDutyScreen();
                MenuTheme.Text(content, "담당자가 실제 현장에 도착해 수리합니다. 이미 지불한 수리량은 담당자가 바뀌어도 유지됩니다.", 13, 60);
                foreach (var commander in SortedCommanders().Where(c => c.AllowsJob(CommanderJobs.Repair) && c.CivilianWorkReady))
                {
                    var worker = commander;
                    MenuTheme.Button(content, worker.CommanderName + "에게 수리 지시", () => {
                        if (!worker.StartService(target, CommanderJobs.Repair)) ToastManager.Show("이미 작업 중이거나 현장에 접근할 수 없습니다.");
                        RepairOverview();
                    });
                }
                MenuTheme.Button(content, "현장에서 선택", () => { Resume(); WorkTargetPanel.Select(target); });
            }
            MenuTheme.Button(content, "목록 갱신", RepairOverview);
            V5Fit(window);
        }
    }
}
