using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using UnityEngine;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        public void CivicServices()
        {
            var pop = ColonyPopulation.Instance; if (pop == null) return;
            pop.SyncCivilianLocations();
            var f = Frame("공공서비스"); var p = L.Plate(f, "CivicServices", 300, 100, 840, 660);
            L.Label(p, "시민 · 공공서비스", 20, 24, 16, 792, 34);
            L.Label(p, $"서비스 {pop.PublicServiceCapacity}명 / 시민 {pop.Total}명 · 충족 {pop.PublicServiceCoverage:P0} · 이주 수요에 곱함", 13, 24, 60, 792, 28);
            L.Label(p, $"기본 500명 · 시설당 500명 · 시민 서비스는 거리 무관     행정 하루 {pop.AdminDemandWork:0.###}업무 · 성과 {pop.Administration:P0}", 13, 24, 90, 792, 28)
                .gameObject.AddComponent<MenuTooltip>().Message = "시민의 공공서비스·교육·의료는 본거지 전체의 정원 비율입니다. 장수의 식탁·휴식·오락 이용과 주거 주변 미관은 실제 위치에 영향을 받습니다.";
            L.Line(p, 24, 128, 792);
            L.Label(p, $"학당 {pop.SchoolCount}동 · 어린 시민 {pop.S.young}명 · 교육 {pop.EducationCoverage:P0}" + (pop.SchoolsOperating ? "" : " · 운영비 부족"), 15, 24, 140, 792, 28);
            L.Label(p, "1동당 어린 시민 20명 · 하루 식량 0.3 / 재료(흙) 0.15. 성인 전환 시 교육 효과 적용.", 13, 24, 172, 792, 26, MenuTheme.Muted);
            L.Label(p, $"교육 과세 생산 +{pop.S.educationBonus:P0} · 남은 {pop.S.educationSeconds / GameCalendar.SecondsPerDay:0.#}일 · 교육 민심 +{pop.S.educationBonus * 50:0.#}", 13, 24, 202, 792, 26);
            L.Label(p, $"진료소 {pop.ClinicCount}동 · 의료 충족 {pop.MedicalCoverage:P0} · 민심 +{pop.MedicalCoverage * 5:0.#}" + (pop.ClinicsOperating ? "" : " · 운영비 부족"), 15, 24, 244, 792, 28);
            L.Label(p, "1동당 시민 50명 · 하루 기초약 0.15 / 식량 0.3. 시민 질병은 생성하지 않습니다.", 13, 24, 276, 792, 26, MenuTheme.Muted);
            L.Label(p, $"운영비 미정산: 학당 식량 {pop.S.schoolFoodDebt:0.##} / 흙 {pop.S.schoolMaterialDebt:0.##} · 진료소 식량 {pop.S.clinicFoodDebt:0.##} / 약 {pop.S.clinicMedicineDebt:0.##}", 12, 24, 308, 792, 26, MenuTheme.Muted);
            L.Line(p, 24, 350, 792);
            L.Label(p, "주거 · 대피 지점 (피해 10당 1명 사망, 이동 그룹 최대 50명)", 15, 24, 360, 792, 28);
            var list = L.List(p, 24, 394, 792, 142);
            foreach (var location in pop.CivilianLocations)
            {
                if (location.count == 0) continue;
                var row = L.Cell(list, "Civilian " + location.id, 32);
                var status = location.moving ? "이동 중" : location.arrived ? "도착" : location.sheltered ? "보호" : "노출";
                L.Label(row, $"#{location.id} · {location.count}명 · {status} · 피해 {location.damage:0.#} · 위치 {location.position.x:0.#}, {location.position.z:0.#}", 13, 8, 0, 500, 30);
                if (location.moving) continue;
                // G16 대피: 실제 경로로 최대 50명씩 이동한다(도착 전 탑승·보호 없음). 로켓은 탑승 대기 지점, 주거는 가장 가까운 다른 집.
                var id = location.id; var from = location.position;
                var rocket = AntColony.Core.SceneObjects.Any<AntColony.Buildings.AirshipYard>();
                var home = AntColony.Buildings.Housing.All.Where(h => h != null && h.isActiveAndEnabled && !h.IsDead && (h.Position - from).sqrMagnitude > 4)
                    .OrderBy(h => (h.Position - from).sqrMagnitude).FirstOrDefault();
                void Evacuate(Vector3 to, string where) { ToastManager.Show(pop.BeginCivilianEvacuation(id, to) > 0 ? where + "(으)로 대피를 시작했습니다." : "대피 경로가 없습니다."); CivicServices(); }
                if (rocket != null) L.Button(row, "Evacuate Rocket", "로켓으로", 516, 1, 120, 28, () => Evacuate(rocket.Position, "로켓"));
                if (home != null) L.Button(row, "Evacuate Home", "가까운 주거로", 644, 1, 140, 28, () => Evacuate(home.Position, "주거"));
            }
            L.Button(p, "Civic Refresh", "새로 고침", 24, 548, 130, 34, CivicServices);
            L.Button(p, "Reserve Warning", "최종전 비축 확인", 170, 548, 180, 34, () => FinalBattleReserveWarning());
            L.Button(p, "Population Back", "인구로", 24, 600, 140, 36, Population);
            L.Button(p, "Close", "닫기", 676, 600, 140, 36, Resume);
        }

        public void FinalBattleReserveWarning(Action activate = null, IEnumerable<ReserveRequirement> fuelAndAmmo = null)
        {
            var f = Frame("최종전 비축"); var p = L.Plate(f, "FinalBattleReserves", 300, 100, 840, 660);
            L.Label(p, "가동 전 · 권장 비축", 20, 24, 16, 792, 34);
            L.Label(p, "비축 부족은 경고입니다. 가동하면 15일 준비를 중단할 수 없습니다.", 13, 24, 58, 792, 32, MenuTheme.Warning);
            var list = L.List(p, 24, 104, 792, 470);
            foreach (var line in FinalBattleEconomy.ReserveLines(fuelAndAmmo))
            {
                var row = L.Cell(list, "Reserve", 54);
                L.Label(row, line, 13, 8, 0, 760, 50);
            }
            L.Button(p, "Back", "돌아가기", 24, 600, 140, 36, CivicServices);
            if (activate != null) L.Button(p, "Activate Confirm", "확인 · 가동", 636, 600, 180, 36, () => activate(), primary: true);
        }
    }
}
