using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEngine;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        public void ShowSurvey(UndergroundScanner selected = null)
        {
            var window = V5Window("지하 탐사", "전기 시대 · 200W · 반경 35m · 후보당 연구 작업 300초");
            ManagementTabs(window, "생산·자원", "매장지·배관");
            var left = V5Section(window, "탐사기 / 담당 장수", 12, 112, 530, 536);
            var scanners = SceneObjects.All<UndergroundScanner>().Where(s => !s.IsDead).ToArray();
            selected ??= scanners.FirstOrDefault();
            foreach (var scanner in scanners)
            { var pick = scanner; MenuTheme.Button(left, scanner.Data.displayName + " · " + scanner.Status, () => ShowSurvey(pick)); }
            if (scanners.Length == 0) MenuTheme.Text(left, "탐사기가 없습니다. 지하 탐사 연구 후 건설할 수 있습니다.", 13, 52);
            MenuTheme.Button(left, "지하 탐사기 건설 · 강철60 구리20 부품6", () => { Resume(); SceneObjects.Any<BuildingPlacementController>()?.BeginSciencePlacement(BuildingKind.UndergroundScanner); })
                .interactable = ScienceEffects.BuildingUnlocked(BuildingKind.UndergroundScanner);
            if (selected != null)
            {
                var scanner = selected;
                MenuTheme.Text(left, scanner.Status + "\n담당: " + (scanner.Target?.CommanderName ?? "없음") + $" · 진행 {scanner.Progress:0}/300초", 14, 58);
                MenuTheme.Button(left, "탐사기 위치로", () => { Resume(); SceneObjects.Any<AntColony.Camera.IsometricCameraController>()?.FocusOn(scanner.Position); });
                MenuTheme.Button(left, "담당 해제 · 후보 진행 보존", () => { scanner.Release(); ShowSurvey(scanner); });
                foreach (var commander in SortedCommanders().Where(c => c.AllowsJob(AntColony.Units.CommanderJobs.Research) && c.IsColonyMember && !c.IsAwayFromHome && !c.IsDeployed))
                {
                    var pick = commander;
                    MenuTheme.Button(left, "탐사 담당 지정: " + commander.CommanderName, () => {
                        if (!scanner.RequestAssignment(pick)) ToastManager.Show("연구 허용·생활 상태·접근 경로·다른 담당 배정을 확인하세요."); ShowSurvey(scanner); });
                }
            }
            var right = V5Section(window, "발견된 매장층", 554, 112, 534, 536);
            var survey = UndergroundSurvey.Instance;
            if (survey != null && StellarSupply.Notice(survey.Layers) is string notice) MenuTheme.Text(right, notice, 13, 100);
            var discovered = survey?.Layers.Where(l => l.discovered).ToArray();
            if (discovered == null || discovered.Length == 0) MenuTheme.Text(right, "아직 발견한 매장층이 없습니다. 미조사 후보의 자원·좌표는 공개하지 않습니다.", 13, 64);
            else foreach (var layer in discovered)
            {
                var id = layer.id;
                MenuTheme.Text(right, (layer.map == 0 ? "본거지" : WorldMapManager.Instance.Sites[layer.map - 1].Title) + " · " + survey.Description(layer), 13, 80);
                MenuTheme.Button(right, "발견 범위 보기", () => { Resume(); survey.Highlight(new[] { id }); });
                if (layer.type >= 0 && layer.remaining >= 1)
                {
                    var kind = layer.type == (int)ResourceType.CrudeOil ? BuildingKind.OilDrill
                        : layer.type == (int)ResourceType.NaturalGas ? BuildingKind.GasExtractor
                        : layer.type == (int)ResourceType.StellarOre ? BuildingKind.AdvancedDeepDrill : BuildingKind.DeepDrill;
                    MenuTheme.Button(right, "채취 시설 배치 · " + UndergroundProduction.Preview(kind, layer.position), () => {
                        Resume(); survey.Highlight(new[] { id }); SceneObjects.Any<BuildingPlacementController>()?.BeginSciencePlacement(kind);
                    }).interactable = ScienceEffects.BuildingUnlocked(kind) && !UndergroundProduction.Occupied(layer);
                }
            }
            MenuTheme.Text(right, "광석: 채집 담당 + 전력 → 바닥 산출 → 운반. 원유/가스: 자동 추출 → 각 전용 배관·탱크. 연구 전에는 표본 1개만 확보합니다.", 12, 60);
            MenuTheme.Button(right, "새로고침", () => ShowSurvey(selected));
            V5Fit(window);
        }
    }
}
