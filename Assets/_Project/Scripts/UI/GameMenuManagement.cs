using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        private static string[] ManagementTabNames(string group) => group switch
        {
            "물류·식품" => new[] { "창고", "식단", "건설·작업", "퇴비·비료" },
            "의료·관리" => new[] { "의료", "동물 위생", "묘비·추모", "포로", "인구", "기록" },
            "생산·자원" => new[] { "자원", "생산", "장비·도구", "수리", "매장지·배관", "음주" },
            _ => new[] { "장수", "평가", "외부 조사", "특성 도감", "진형" }
        };

        private void ManagementTabs(RectTransform window, string group, string selected)
        {
            float x = 12;
            foreach (var tab in ManagementTabNames(group))
            {
                var pick = tab;
                var width = Mathf.Max(58, tab.Length * 13 + 20);
                L.Tab(window, tab, x, 64, width, tab == selected, () => Management(group, pick));
                x += width + 6;
            }
            L.Line(window, 12, 97, 1076);
        }

        public void Management(string group, string selected = null)
        {
            selected ??= ManagementTabNames(group)[0];
            if (selected == "창고" && ResourceManager.Instance?.Bins.Count > 0) { ShowStorage(ResourceManager.Instance.Bins[0]); return; }
            if (selected == "식단") { ShowDiet(); return; }
            if (selected == "의료" && SortedCommanders().Length > 0) { ShowHealth(SortedCommanders()[0]); return; }
            if (selected == "묘비·추모") { ShowMemorial(); return; }
            if (selected == "장수") { Roster(); return; }
            var window = V5Window(group, "정착지 관리 · 실제 시설과 장수의 현재 상태");
            ManagementTabs(window, group, selected);
            var left = V5Section(window, selected, 12, 112, 530, 536);
            var right = V5Section(window, "상세 · 관리", 554, 112, 534, 536);
            void Info(string value) => MenuTheme.Text(right, value, 13, 60).color = MenuTheme.Muted;
            void Link(string label, Action action) => MenuTheme.Button(left, label + "  ›", action);
            void Missing(string description)
            {
                MenuTheme.Text(left, "미구현", 15, 36);
                Info(description);
            }
            void Facilities<T>(Action<T> show) where T : Component
            {
                var facilities = FindObjectsByType<T>(FindObjectsSortMode.None);
                foreach (var facility in facilities)
                {
                    var captured = facility;
                    var building = facility.GetComponent<BuildingBase>();
                    Link(building != null ? building.Data.displayName : facility.name, () => show(captured));
                }
                if (facilities.Length == 0) MenuTheme.Text(left, "시설 없음 · 건설 후 이 목록에 표시됩니다.", 13, 40);
            }
            switch (selected)
            {
                case "창고":
                    var rm = ResourceManager.Instance;
                    if (rm != null)
                        foreach (var bin in rm.Bins)
                        {
                            var captured = bin;
                            Link(bin.Name + " · 우선순위 " + bin.priority, () => ShowStorage(captured));
                        }
                    if (rm == null || rm.Bins.Count == 0) MenuTheme.Text(left, "창고 없음", 13, 32);
                    if (rm != null) foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                        MenuTheme.Text(right, type.DisplayName() + "   " + rm.GetAmount(type), 13, 28);
                    Info("창고를 선택하면 허용 품목·우선순위·재고 꺼내기를 관리합니다.");
                    break;
                case "식단":
                    Link("공통 식단", () => ShowDiet());
                    foreach (var c in SortedCommanders()) Link(c.CommanderName, () => ShowDiet(c));
                    Facilities<Kitchen>(ShowKitchen);
                    Info("장수별 허용 식품과 주방 생산 목록을 관리합니다.");
                    break;
                case "건설·작업":
                    Link("작업표", WorkSchedule);
                    Link("전체 작업", () => ShowTasks(TaskCandidates().ToList()));
                    Link("건설", () => { Resume(); BuildScreen.Open(); });
                    Info("기존 작업 우선순위·건설 담당자 선택 화면에 연결합니다.");
                    break;
                case "퇴비·비료":
                    Facilities<CompostBin>(ShowCompost);
                    Info("배설물·썩은 식품을 비료로 숙성합니다. 퇴비통을 선택해 재고와 숙성 상태를 확인하세요.");
                    break;
                case "의료":
                    foreach (var c in SortedCommanders())
                    {
                        Link(c.CommanderName + " · " + c.Health.Summary, () => ShowHealth(c));
                        MenuTheme.Text(right, c.CommanderName + "\n" + HealthLines(c.Health), 13, 72);
                    }
                    Info("대상을 선택해 처치·투약·수술·구조·병상 휴식을 지시합니다.");
                    break;
                case "동물 위생":
                    foreach (var a in FindObjectsByType<Critter>(FindObjectsSortMode.None))
                        Link(a.DisplayName, () => OpenCritterTab(a, true));
                    Facilities<Ranch>(ShowRanch);
                    Info("기존 개체 의료·목장 관리에 연결합니다. 통합 위생 대시보드는 미구현입니다.");
                    break;
                case "묘비·추모": Link("사망 기록 / 묘비", () => ShowMemorial()); Info("사망 기록·시신 수습·추모 설정을 관리합니다."); break;
                case "포로": Facilities<PrisonerCamp>(ShowPrisoners); Info("수용소별 실제 포로와 경비 상태를 관리합니다."); break;
                case "인구": Link("인구 · 이주 · 세금 · 병역", Population); Info("현재 인구와 이주 수요·민심·병역 정책을 확인합니다."); break;
                case "기록": Link("이벤트 기록", EventLog); Info("최근 사건과 누적 자원 획득·소비 기록입니다."); break;
                case "자원":
                    if (ResourceManager.Instance != null) foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                        MenuTheme.Text(left, type.DisplayName() + "   " + ResourceManager.Instance.GetAmount(type), 13, 32);
                    Info("사용 가능한 실제 창고 재고입니다. 시안의 세분화된 전체 자원 도감은 미구현입니다.");
                    Link("창고 관리", () => Management("물류·식품"));
                    break;
                case "생산":
                    Link("맨바닥 제작 · 돌 도구 / 단궁", GroundCrafting);
                    Facilities<Workshop>(ShowWorkshop); Facilities<Processor>(ShowProcessor); Facilities<Kitchen>(ShowKitchen);
                    Info("시설을 선택해 생산 방식·수량·주문 목록을 편집합니다. 시설이 없으면 건설부터 진행하세요.");
                    break;
                case "장비·도구":
                    Link("맨바닥 제작 · 돌 도구 / 단궁", GroundCrafting);
                    foreach (var c in SortedCommanders()) Link(c.CommanderName + " · " + c.WeaponLabel, () => ShowEquipment(c));
                    Facilities<Workshop>(ShowWorkshop);
                    Info("기존 무기·갑옷·장신구 장착과 공방 생산에 연결합니다. 시안의 별도 도구 슬롯은 미구현입니다.");
                    break;
                case "수리":
                    Link("수리 작업 우선순위", WorkSchedule);
                    Link("전체 작업", () => ShowTasks(TaskCandidates().ToList()));
                    Info("기존 수리 작업은 작업표에서 지정합니다. 시안의 통합 수리 주문 대시보드는 미구현입니다.");
                    break;
                case "매장지·배관": Missing("시안의 매장지 개발·배관 연결 관리 기능은 아직 구현되지 않았습니다."); break;
                case "음주": Missing("시안의 음주 정책·배급 관리 기능은 아직 구현되지 않았습니다."); break;
                case "장수":
                    foreach (var c in SortedCommanders()) Link(c.CommanderName + " · " + Status(c), () => Details(c));
                    MenuTheme.Button(right, "장수 전체 관리", Roster);
                    MenuTheme.Button(right, "출전 편성 · 징집소", OpenConscription);
                    MenuTheme.Button(right, "작업표", WorkSchedule);
                    Info("장수 상세·장비·출전 편성은 기존 게임 기능입니다.");
                    break;
                case "외부 조사":
                    Facilities<ScoutPost>(ShowScout);
                    Info("기존 정찰 초소의 영입 파견에 연결합니다. 시안의 세력별 인재 조사·보고서는 미구현입니다.");
                    break;
                case "특성 도감":
                    foreach (var c in SortedCommanders())
                        MenuTheme.Text(left, c.CommanderName + "\n" + string.Join(" · ", c.Traits.values.Select(CommanderTraits.DisplayName)), 13, 64);
                    Info("현재 장수가 실제로 보유한 특성입니다. 시안의 전체 특성 도감·등급 표시는 미구현입니다.");
                    break;
                case "평가": Missing("시안의 담당자 평가·별점·조사 기술 기능은 아직 구현되지 않았습니다."); break;
                case "진형": Missing("시안의 14종 진형 선택·적성·전투 보정 기능은 아직 구현되지 않았습니다."); break;
            }
            V5Fit(window);
        }
    }
}
