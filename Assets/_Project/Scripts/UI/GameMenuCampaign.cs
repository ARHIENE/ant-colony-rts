using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        public void Science()
        {
            Screen("Science / Rocket", "과학 · 로켓");
            var research = CampaignResearch.Instance;
            if (research == null) { MenuTheme.Text(content, "과학 화면 준비 중입니다."); MenuTheme.Button(content, "Back", Back); return; }
            MenuTheme.Text(content, research.Active == null ? "연구 항목을 고른 뒤 연구소마다 장수를 지시하세요. 완료되면 배정이 풀리고 자동으로 다음 연구를 하지 않습니다."
                : $"{research.Active.Name}: 작업량 {research.Progress:0}/{research.Active.Work:0} · 게임을 재개하면 연구가 진행됩니다.", 18, 65);
            foreach (var lab in FindObjectsByType<ScienceLab>(FindObjectsSortMode.None))
            {
                MenuTheme.Text(content, $"과학연구소 {lab.Tier}티어({CampaignResearch.EraNames[lab.Tier - 1]}) | 연구 장수: {lab.Target?.CommanderName ?? "없음"}", 19, 48);
                var captured = lab;
                MenuTheme.Button(content, $"연구소 강화 (식량 {lab.Tier * 60} / 재료 {lab.Tier * 80})", () => { if (!captured.TryUpgrade()) ToastManager.Show("강화 조건을 충족하지 못했거나 자원이 부족합니다."); Science(); }).interactable = lab.Tier < ScienceLab.MaxTier && !lab.Busy;
                if (lab.Target != null) MenuTheme.Button(content, "연구 배정 해제", () => { captured.ReleaseResearcher(); Science(); });
                else if (research.Active != null) foreach (var c in SortedCommanders().Where(c => !c.IsAwayFromHome && c.CanChangeAllocation && c.AllowsJob(CommanderJobs.Research)))
                    MenuTheme.Button(content, "연구 지시: " + c.CommanderName, () => { if (!c.SendToResearch(captured)) ToastManager.Show("이 장수에게 연구를 지시할 수 없습니다(작업표 연구 금지·출전·수면 등)."); Science(); });
            }
            MenuTheme.Text(content, "지시한 장수가 연구소로 걸어가 연구합니다. 선택한 연구와 같은 시대 이상의 연구소가 필요합니다.", 17, 60);
            foreach (var definition in CampaignResearch.Technologies)
            {
                var reason = research.BlockReason(definition.Technology);
                var done = research.Has(definition.Technology);
                var button = MenuTheme.Button(content, $"T{definition.Tier} {CampaignResearch.EraNames[definition.Tier - 1]} · {definition.Name} — {(done ? "완료" : $"식량 {definition.Food}/재료 {definition.Soil}{(definition.Special > 0 ? $"/특수 자원 {definition.Special}" : "")}, 작업량 {definition.Work:0}")}",
                    () => { if (!research.TryStart(definition.Technology)) ToastManager.Show("선행 연구와 보유 자원을 확인하세요."); Science(); }, reason == "" ? "공동 연구 하나를 시작합니다." : reason);
                button.interactable = reason == "";
            }
            MenuTheme.Text(content, "로켓 엔진 설계도: " + (research.HasBlueprint ? "획득 완료" : "월드맵 보스 보상 또는 교역으로 획득"), 18, 55);
            MenuTheme.Button(content, "의무실 건설 (식량 40 / 재료 40 / 인력 4마리)", () => {
                Resume(); FindFirstObjectByType<BuildingPlacementController>()?.BeginInfirmaryPlacement();
            }, "중상 장수를 최대 2명 치료합니다. 장수 상세에서 근처 환자를 배정하세요.").interactable = Infirmary.Unlocked;
            ScienceBuildings();
            MenuTheme.Button(content, "로켓 발사대 건설 (식량 100 / 재료 150 / 인력 10마리)", () => {
                Resume(); FindFirstObjectByType<BuildingPlacementController>()?.BeginAirshipYardPlacement();
            });
            foreach (var yard in FindObjectsByType<AirshipYard>(FindObjectsSortMode.None))
            {
                MenuTheme.Text(content, $"로켓 발사대 | 선체 {(yard.Hull ? "완성" : "미완성")} | 엔진 {(yard.Engine ? "완성" : "미완성")} | 고치 {yard.Cocoons}개 | {yard.Remaining:0}초", 18, 55);
                foreach (AirshipPart part in Enum.GetValues(typeof(AirshipPart)))
                    MenuTheme.Button(content, part + " 건조" + (part == AirshipPart.Cocoon ? $" (식량 {GameBalance.CocoonFood}/재료 {GameBalance.CocoonSoil}/특수 자원 {GameBalance.CocoonSpecial}, {yard.Cocoons}/{GameBalance.MaxCocoons})" : " (식량 100/재료 150/특수 자원 150)"),
                        () => { if (!yard.TryBuild(part)) ToastManager.Show("관련 연구·자원과 작업 중이 아닌 발사대가 필요합니다."); Science(); });
                foreach (var c in SortedCommanders().Where(c => c.CanChangeAllocation && !c.IsAwayFromHome && Vector3.Distance(c.Position, yard.Position) <= 8))
                    MenuTheme.Button(content, "탑승: " + c.CommanderName, () => { if (!yard.TryBoard(c)) ToastManager.Show("선체·엔진을 완성하고 탑승 장수마다 고치 1개를 준비하세요."); Science(); });
                MenuTheme.Text(content, "탑승 장수: " + string.Join(", ", yard.Passengers.Select(c => c.CommanderName)), 18, 50);
                MenuTheme.Button(content, "탑승 장수 하선", () => { yard.Unload(); Science(); });
                MenuTheme.Button(content, "로켓 발사 — 이번 게임 종료", () => { if (!yard.TryDepart()) ToastManager.Show("선체와 엔진을 먼저 완성하세요."); }).interactable = yard.Ready;
            }
            MenuTheme.Button(content, "새로고침", Science);
            MenuTheme.Button(content, "Back", Back);
        }
        // 장수 상세 아래쪽 행동 버튼: 치료·재생·포상·장비.
        private void CommanderActions(CommanderAnt c)
        {
            if (c.TreatmentFacility != null)
                MenuTheme.Button(content, "치료 중단", () => { c.TreatmentFacility?.Release(c); Details(c); }, "치료 진행도는 보존됩니다. 게임을 재개하면 회복합니다.");
            else if (c.PersonalState.NeedsTreatment)
            {
                MenuTheme.Text(content, "의무실을 연구·건설한 뒤 8m 안으로 이동하세요. 시설당 환자 2명을 치료하며 게임을 재개해야 진행됩니다.", 17, 65);
                foreach (var infirmary in FindObjectsByType<Infirmary>(FindObjectsSortMode.None))
                    MenuTheme.Button(content, $"의무실에서 치료 ({infirmary.Patients.Count}/{Infirmary.Capacity})",
                        () => { if (!infirmary.TryAdmit(c)) ToastManager.Show("근처의 대기 중인 부상 장수와 빈 침대가 필요합니다."); Details(c); }).interactable = infirmary.CanTreat(c);
            }
            RegenerationButtons(c);
            MenuTheme.Button(content, "포상 (식량 30)", () => { if (!c.TryReward()) ToastManager.Show("본거지에서 게임 시간으로 한 달에 한 번 가능합니다."); Details(c); }).interactable = c.CanReceiveOrders && !c.IsAwayFromHome && c.PersonalState.rewardCooldown <= 0;
            var inventory = EquipmentInventory.Instance;
            if (inventory == null) return;
            MenuTheme.Text(content, $"장비 보관함 {inventory.Items.Count}/{EquipmentInventory.Capacity}", 18, 36);
            foreach (var item in c.PersonalState.equipment.ToArray())
                MenuTheme.Button(content, "해제: " + item.Label, () => { if (!inventory.Unequip(c, item)) ToastManager.Show("장비를 바꿀 수 없는 상태이거나 안전한 착륙 지점이 없습니다."); Details(c); });
            foreach (var item in inventory.Items.ToArray())
                MenuTheme.Button(content, "장착: " + item.Label, () => { if (!inventory.Equip(c, item)) ToastManager.Show("장비를 바꿀 수 없는 상태이거나 날개 부상·착륙 지점 문제로 장착할 수 없습니다."); Details(c); });
        }
    }
}
