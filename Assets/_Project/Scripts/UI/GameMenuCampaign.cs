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
                        () => { if (!yard.TryBuild(part)) ToastManager.Show("관련 연구·자원과 작업 중이 아닌 발사대가 필요합니다."); ScienceFacilities(); });
                foreach (var c in SortedCommanders().Where(c => c.CanChangeAllocation && !c.IsAwayFromHome && Vector3.Distance(c.Position, yard.Position) <= 8))
                    MenuTheme.Button(content, "탑승: " + c.CommanderName, () => { if (!yard.TryBoard(c)) ToastManager.Show("선체·엔진을 완성하고 탑승 장수마다 고치 1개를 준비하세요."); ScienceFacilities(); });
                MenuTheme.Text(content, "탑승 장수: " + string.Join(", ", yard.Passengers.Select(c => c.CommanderName)), 18, 50);
                MenuTheme.Button(content, "탑승 장수 하선", () => { yard.Unload(); ScienceFacilities(); });
                MenuTheme.Button(content, "로켓 발사 — 이번 게임 종료", () => { if (!yard.TryDepart()) ToastManager.Show("선체와 엔진을 먼저 완성하세요."); }).interactable = yard.Ready;
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
