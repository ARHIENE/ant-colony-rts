using System.Linq;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        private ExpeditionSite investigationSite;
        private ExpeditionTransport investigationShip;
        private CommanderAnt investigationEvaluator, investigationEscort;
        private string investigationFocus;
        private bool investigationFoodConfirmed, investigationUnattendedConfirmed;
        private static string InvestigationState(InvestigationOutcome outcome) => outcome switch
        {
            InvestigationOutcome.Investigating => "조사 중", InvestigationOutcome.Completed => "조사 완료 · 귀환 보고 대기",
            InvestigationOutcome.Expelled => "추방 · 귀환 중", InvestigationOutcome.Captured => "평가자 포로 · 이번 정보 소실",
            InvestigationOutcome.Interrupted => "중단 · 미완료 정보 폐기", _ => "보고 완료"
        };
        private static string TransportState(ExpeditionState state) => state switch
        { ExpeditionState.Home => "본거지", ExpeditionState.Outbound => "현지로 이동", ExpeditionState.Deployed => "현지", _ => "귀환 중" };
        public void ShowInvestigation(ExpeditionSite site = null, ExpeditionTransport ship = null)
        {
            if (site != null && site != investigationSite) { investigationSite = site; investigationFocus = null; investigationFoodConfirmed = investigationUnattendedConfirmed = false; }
            if (ship != null && ship != investigationShip) { investigationShip = ship; investigationFoodConfirmed = investigationUnattendedConfirmed = false; }
            var world = WorldMapManager.Instance;
            if (world == null) { Resume(); return; }
            var window = V5Window("외부 인재 조사", "집중 1명 / 탐색 최대 3명 · 현지 150초 · 정착지 귀환 후 보고");
            ManagementTabs(window, "인재·편성", "외부 조사");
            var left = V5Section(window, "세력 / 이동수단", 12, 112, 530, 536);
            foreach (var s in world.Sites.Where(s => DiplomacyManager.Instance?.Faction(s) != null))
            { var pick = s; MenuTheme.Button(left, (s == investigationSite ? "선택 · " : "") + s.Title, () => { investigationSite = pick; investigationFocus = null; investigationFoodConfirmed = investigationUnattendedConfirmed = false; ShowInvestigation(); }); }
            MenuTheme.Button(left, "도보 원정대 만들기", () => {
                if (world.CanCreateTransport(world.HomePosition, out var point)) investigationShip = world.CreateTransport(false, point, walking: true);
                else ToastManager.Show("원정대를 만들 수 있는 본거지 출입 공간이 없습니다."); ShowInvestigation(); });
            foreach (var t in world.Transports.Where(t => t != null && !t.IsDead))
            { var pick = t; MenuTheme.Button(left, (t == investigationShip ? "선택 · " : "") + t.name + " · " + TransportState(t.State), () => { investigationShip = pick; investigationFoodConfirmed = investigationUnattendedConfirmed = false; ShowInvestigation(); }); }
            var right = V5Section(window, "담당 / 파견", 554, 112, 534, 536);
            if (investigationShip != null && investigationShip.Investigation != null)
            {
                var mission = investigationShip.Investigation;
                MenuTheme.Text(right, investigationShip.name + $" · {TransportState(investigationShip.State)}\n조사 상태: {InvestigationState(mission.outcome)} · {mission.elapsed:0}/150초 · 대상 {mission.targets.Count}명\n발각 판정: {(mission.riskChecked ? "완료" : "대기")}", 14, 96);
                MenuTheme.Button(right, "조사 중단 · 귀환 요청", () => { world.Assessment.CancelInvestigation(investigationShip); ShowInvestigation(); })
                    .interactable = mission.outcome != InvestigationOutcome.Captured;
            }
            else
            {
                MenuTheme.Text(right, "대상: " + (investigationSite?.Title ?? "세력 선택 필요") + "\n평가자: " + (investigationEvaluator?.CommanderName ?? "미지정")
                    + " · 호위: " + (investigationEscort?.CommanderName ?? "없음"), 13, 56);
                foreach (var c in SortedCommanders().Where(c => !c.IsChild && !c.IsCaptive && !c.IsAwayFromHome && !c.IsDeployed))
                {
                    var pick = c;
                    MenuTheme.Button(right, "평가자로 선택: " + c.CommanderName + " · 조사 " + c.Talents.Level(CommanderActivity.Investigation), () => { investigationEvaluator = pick; if (investigationEscort == pick) investigationEscort = null; investigationFoodConfirmed = investigationUnattendedConfirmed = false; ShowInvestigation(); });
                    MenuTheme.Button(right, "호위로 선택: " + c.CommanderName, () => { if (pick != investigationEvaluator) investigationEscort = pick; investigationFoodConfirmed = investigationUnattendedConfirmed = false; ShowInvestigation(); });
                }
                MenuTheme.Button(right, "호위 없이 조사", () => { investigationEscort = null; investigationFoodConfirmed = investigationUnattendedConfirmed = false; ShowInvestigation(); });
                MenuTheme.Button(right, "방식: 세력 인재 탐색 (미보고 최대 3명)", () => { investigationFocus = null; investigationFoodConfirmed = investigationUnattendedConfirmed = false; ShowInvestigation(); });
                if (investigationSite != null)
                    foreach (var t in world.Assessment.Targets(investigationSite))
                    { var id = t.Id; MenuTheme.Button(right, "집중 평가: " + t.Name, () => { investigationFocus = id; investigationFoodConfirmed = investigationUnattendedConfirmed = false; ShowInvestigation(); }); }
                MenuTheme.Text(right, "선택 방식: " + (string.IsNullOrEmpty(investigationFocus) ? "세력 탐색" : "특정 인재 집중") + "\n평가자와 호위만 탑승해야 합니다. 발각 시 추방·포로 위험이 있습니다.", 12, 58);
                if (investigationShip != null)
                {
                    MenuTheme.Text(right, $"현재 탑승 {investigationShip.Crew.Count}명 · 탑승 대기 {investigationShip.PendingCrewOrderCount}명\n식량 {investigationShip.AvailableFood} / 권장 {(investigationSite != null ? investigationShip.RecommendedInvestigationFood(investigationSite) : 0)}", 13, 52);
                    MenuTheme.Button(right, "담당·호위 탑승 지시", () => {
                        var people = new[] { investigationEvaluator, investigationEscort }.Where(c => c != null).ToArray();
                        if (!investigationShip.TryBoard(people)) ToastManager.Show("본거지에서 탑승 가능한 담당·호위를 확인하세요."); ShowInvestigation(); });
                    MenuTheme.Button(right, "권장 식량 싣기", () => {
                        if (investigationSite == null || !investigationShip.TryLoadFood(Mathf.Min(investigationShip.ReturnCargoSpace, investigationShip.RecommendedInvestigationFood(investigationSite) - investigationShip.AvailableFood))) ToastManager.Show("창고 식량·적재 공간·본거지 상태를 확인하세요."); ShowInvestigation(); });
                    MenuTheme.Button(right, investigationFoodConfirmed || investigationUnattendedConfirmed ? "주의 사항 확인 · 조사 강행" : "조사 출발", () => {
                        if (investigationSite == null || investigationEvaluator == null) { ToastManager.Show("세력과 평가자를 먼저 선택하세요."); return; }
                        var shortage = investigationShip.AvailableFood < investigationShip.RecommendedInvestigationFood(investigationSite);
                        if (shortage && !investigationFoodConfirmed) { investigationFoodConfirmed = true; ToastManager.Show("왕복·현지 조사·예비 식량이 부족합니다. 다시 누르면 강행합니다."); ShowInvestigation(); return; }
                        if (investigationShip.LeavesHomeUnattended && !investigationUnattendedConfirmed) { investigationUnattendedConfirmed = true; ToastManager.Show("본거지에 남는 장수가 없습니다. 방어·생활 작업이 중단될 수 있습니다. 다시 누르면 출발합니다."); ShowInvestigation(); return; }
                        if (!investigationShip.TryInvestigate(investigationSite, investigationEvaluator, investigationEscort, investigationFocus, investigationFoodConfirmed)) ToastManager.Show("탑승 완료·좌석·조사 가능한 인재·수송 상태를 확인하세요.");
                        else { investigationFoodConfirmed = investigationUnattendedConfirmed = false; Resume(); return; } ShowInvestigation(); });
                }
            }
            MenuTheme.Button(right, "귀환 보고서", () => ShowAssessment());
            MenuTheme.Button(right, "새로고침", () => ShowInvestigation());
            V5Fit(window);
        }
    }
}
