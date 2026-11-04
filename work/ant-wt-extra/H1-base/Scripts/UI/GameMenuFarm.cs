using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // V5 농사 화면(2026-10-15): 밭 목록 → 선택 밭 상태·작물·우선순위·비료·지시. 실제 작업은 작업표의 농사 장수가 한다.
    public sealed partial class GameMenuController
    {
        private FarmPlot uprootConfirm;

        public static string CropName(FarmCrop c) => c switch { FarmCrop.Honeydew => "감로", FarmCrop.AdvancedFungus => "고급 균류", _ => "균류" };

        public void ShowFarm(FarmPlot p)
        {
            if (p == null) { Resume(); return; }
            var window = V5Window("농사 / 밭", "수확 후 심어진 채 다시 자람 · 뿌리뽑기는 빈 밭으로 남김 · 병충해는 상하좌우 밭으로만 전파");

            content = V5Section(window, $"밭 목록 ({FarmPlot.All.Count})", 20, 104, 360, 544);
            foreach (var other in FarmPlot.All.OrderBy(f => f.transform.position.x).ThenBy(f => f.transform.position.z).ToArray())
            {
                var pick = other;
                MenuTheme.Button(content, (other == p ? "▶ " : "") + $"{CropName(other.Crop)} · {other.Status}", () => ShowFarm(pick));
            }

            content = V5Section(window, $"선택한 밭 · {CropName(p.Crop)}" + (p.Data.nextCrop >= 0 ? $" → 다음 {CropName((FarmCrop)p.Data.nextCrop)}" : ""), 394, 104, 686, 544);
            var status = MenuTheme.Text(content, "", 16, 70);
            void Refresh()
            {
                if (p == null) return;
                var workers = AntUnitBase.Active.OfType<CommanderAnt>().Where(c => c.CurrentResourceNode == p.Node).Select(c => c.CommanderName).ToArray();
                var farmers = AntUnitBase.Active.OfType<CommanderAnt>().Count(c => c.IsColonyMember && !c.IsDead && c.AllowsJob(CommanderJobs.Farming));
                status.text = $"{p.Status}\n작업 장수: " + (workers.Length > 0 ? string.Join(", ", workers) : farmers == 0 ? "<color=#f2c94c>농사 작업을 켠 장수 없음</color>" : "대기")
                    + $" · 인접 밭(상하좌우) {p.Neighbors().Count()}곳" + (p.Node.GatheringForbidden ? " · <color=#f2c94c>작업 금지</color>" : "");
            }
            Refresh(); refreshDutyScreen = Refresh;

            MenuTheme.Text(content, "작물(다음 수확 이후 적용, 빈 밭이면 다시 심을 때)", 15, 26);
            var crops = L.Cell(content, "Crops", 44);
            var i = 0;
            foreach (FarmCrop crop in System.Enum.GetValues(typeof(FarmCrop)))
            {
                var choice = crop; var unlocked = ScienceEffects.CropUnlocked(crop);
                var b = L.Button(crops, "Crop " + crop, CropName(crop) + (unlocked ? "" : " (연구 필요)"), i++ * 210, 0, 204, 40,
                    () => { p.SetNextCrop(choice); ShowFarm(p); }, primary: (p.Data.nextCrop >= 0 ? (FarmCrop)p.Data.nextCrop : p.Crop) == crop);
                b.interactable = unlocked;
            }

            var level = WorkPriorities.Level(p.Node);
            var prio = L.Cell(content, "FarmPriority", 44);
            L.Label(prio, $"작업 우선순위 {level}", 15, 0, 6, 200, 30, MenuTheme.Muted);
            L.Button(prio, "Priority Minus", "−", 210, 0, 44, 40, () => { WorkPriorities.Set(p.Node, level - 1, WorkPriorities.Yellow(p.Node)); ShowFarm(p); });
            L.Button(prio, "Priority Plus", "+", 260, 0, 44, 40, () => { WorkPriorities.Set(p.Node, level + 1, WorkPriorities.Yellow(p.Node)); ShowFarm(p); });

            var fert = ResourceManager.Instance != null ? ResourceManager.Instance.GetAmount(ResourceType.Fertilizer) : 0;
            MenuTheme.Button(content, (p.Data.fertilize ? "비료 사용 허용됨" : "비료 사용 안 함") + $" · 재고 {fert}" + (p.Data.fertilized ? " · 이번 주기 적용됨" : ""),
                () => { p.Data.fertilize = !p.Data.fertilize; ShowFarm(p); }, "성장 주기당 1회, 농사 장수가 넣습니다. 없어도 성장은 멈추지 않습니다.");

            if (p.Data.fallow)
                MenuTheme.Button(content, "다시 심기", () => { p.Replant(); ShowFarm(p); }, "농사 장수가 파종합니다.");
            else if (p.Data.uproot)
                MenuTheme.Button(content, "뿌리뽑기 취소", () => { p.OrderUproot(false); ShowFarm(p); });
            else if (uprootConfirm == p)
                MenuTheme.Button(content, "정말 뿌리뽑기 — 작물·병충해가 사라지고 빈 밭으로 남습니다", () => { uprootConfirm = null; p.OrderUproot(true); ShowFarm(p); });
            else
                MenuTheme.Button(content, "뿌리뽑기(확인 필요)", () => { uprootConfirm = p; ShowFarm(p); });
            MenuTheme.Button(content, p.Node.GatheringForbidden ? "농사 작업 허용" : "농사 작업 금지", () => { p.Node.GatheringForbidden = !p.Node.GatheringForbidden; ShowFarm(p); });
            V5Fit(window);
        }
    }
}
