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
        private void RocketSection(CampaignResearch research)
        {
            if (research == null) return;
            MenuTheme.Text(content, "로켓 핵심물: " + (research.HasBlueprint ? "반입 완료" : "D 보스 보상 본거지 반입 필요"), 18, 55);
            BossProgressionSection(research);
            MenuTheme.Button(content, "의무실 건설 (식량 40 / 재료 40 / 인력 4마리)", () => {
                Resume(); FindFirstObjectByType<BuildingPlacementController>()?.BeginInfirmaryPlacement();
            }, "중상 장수를 최대 2명 치료합니다. 장수 상세에서 근처 환자를 배정하세요.").interactable = Infirmary.Unlocked;
            ScienceBuildings();
            MenuTheme.Button(content, "로켓 발사대 건설 (식량 100 / 재료 150 / 인력 10마리)", () => {
                Resume(); FindFirstObjectByType<BuildingPlacementController>()?.BeginAirshipYardPlacement();
            });
            foreach (var yard in FindObjectsByType<AirshipYard>(FindObjectsSortMode.None))
            {
                MenuTheme.Text(content, $"로켓 {yard.Phase} · 선체 {(yard.Hull ? "완성" : "미완성")} / 엔진 {(yard.Engine ? "완성" : "미완성")} · 좌석 {yard.OccupiedSeats}/{yard.Capacity} · 준비 잔여 {yard.PreparationRemaining / GameCalendar.SecondsPerDay:0.00}일", 13, 55);
                foreach (AirshipPart part in Enum.GetValues(typeof(AirshipPart)))
                    MenuTheme.Button(content, (part == AirshipPart.Cocoon ? "50석 확장 모듈" : part.ToString()) + " 건조" + (part == AirshipPart.Cocoon ? $" (식량 {GameBalance.CocoonFood}/재료 {GameBalance.CocoonSoil}/특수 자원 {GameBalance.CocoonSpecial}, {yard.Cocoons}/{GameBalance.MaxCocoons})" : " (식량 100/재료 150/특수 자원 150)"),
                        () => { if (!yard.TryBuild(part)) ToastManager.Show("관련 연구·자원과 작업 중이 아닌 발사대가 필요합니다."); ScienceFacilities(); }).interactable = !yard.ModificationsLocked;
                RocketActivation(yard);
                var population = ColonyPopulation.Instance;
                if (population != null)
                {
                    population.SyncCivilianLocations();
                    foreach (var group in population.CivilianLocations.Where(g => g.count > 0 && g.arrived && !g.moving && Vector3.Distance(g.position, yard.Position) <= 8))
                    {
                        int groupId = group.id;
                        MenuTheme.Button(content, $"도착 시민 #{groupId} 탑승 ({group.count}명)", () => {
                            if (yard.TryBoard(groupId) == 0) ToastManager.Show("도착·거리·빈 좌석을 확인하세요.");
                            ScienceFacilities();
                        }).interactable = yard.Phase == RocketPhase.Boarding && yard.OccupiedSeats < yard.Capacity;
                    }
                }
                MenuTheme.Text(content, $"일반 시민 승객: {yard.CivilianPassengers}명", 13, 30);
                foreach (var c in SortedCommanders().Where(c => c.CanChangeAllocation && !c.IsAwayFromHome && Vector3.Distance(c.Position, yard.Position) <= 8))
                    MenuTheme.Button(content, "탑승: " + c.CommanderName, () => ConfirmRocketBoarding(yard, c)).interactable = yard.Phase == RocketPhase.Boarding;
                MenuTheme.Text(content, "탑승 장수: " + string.Join(", ", yard.Passengers.Where(c => c != null).Select(c => c.CommanderName)), 13, 50);
                MenuTheme.Text(content, CampaignOutcome.Preview(yard.Passengers), 13, 50);
                MenuTheme.Button(content, "탑승자 전원 하선", () => { yard.Unload(); ScienceFacilities(); }).interactable = yard.Phase != RocketPhase.Launched;
                MenuTheme.Button(content, "로켓 발사 — 이번 게임 종료", () => ConfirmRocketLaunch(yard)).interactable = yard.Phase == RocketPhase.Boarding && yard.OccupiedSeats > 0;
            }
        }
        // 장수 상세 아래쪽 행동 버튼: 치료·재생·포상·장비.
        private void CommanderActions(CommanderAnt c)
        {
            MenuTheme.Button(content, $"건강 · 의료 ({c.Health.Summary})", () => ShowHealth(c));
            RegenerationButtons(c);
            MenuTheme.Button(content, "포상 (식량 30)", () => { if (!c.TryReward()) ToastManager.Show("본거지에서 게임 시간으로 한 달에 한 번 가능합니다."); Details(c); }).interactable = c.CanReceiveOrders && !c.IsAwayFromHome && c.PersonalState.rewardCooldown <= 0;
            var gear = string.Join(", ", c.PersonalState.equipment.Select(e => e.Label));
            MenuTheme.Button(content, "장비 관리" + (gear == "" ? " (장착 없음)" : $" ({gear})"), () => ShowEquipment(c));
            MenuTheme.Button(content, "식단: " + (c.PersonalState.meal.diet < 0 ? "공통 식단" : "개별 설정"), () => ShowDiet(c));
        }
    }
}
