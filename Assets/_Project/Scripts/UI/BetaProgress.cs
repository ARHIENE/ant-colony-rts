using AntColony.Core;
using AntColony.Save;
using AntColony.World;
using AntColony.Buildings;
using AntColony.Data;
using UnityEngine;

namespace AntColony.UI
{
    public sealed class BetaProgress : MonoBehaviour
    {
        private UnityEngine.UI.Text objective;
        private GameObject objectivePanel;
        private WorldMapPanel worldMap;
        private GameManager game;
        private float nextRefresh;
        public string CurrentObjective => BuildObjective();

        private void Start()
        {
            game = GameManager.Instance;
            if (game == null) return;
            game.OnBossDefeated += Victory; game.OnDefeat += Defeat;
            var canvas = MenuTheme.Canvas("BetaObjectives", transform, 1);
            var panel = MenuTheme.Panel(canvas.transform, "Objective", new Vector2(0, 1), new Vector2(300, 92), new Vector2(8, -48));
            panel.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            objectivePanel = panel.gameObject;
            objective = MenuTheme.Text(panel, "", 13, 80);
            MenuTheme.Stretch(objective.rectTransform);
            objective.rectTransform.offsetMin = new Vector2(12, 8); objective.rectTransform.offsetMax = new Vector2(-12, -8);
            if (GameSession.Instance.GameStarted)
            {
                if (game.SavedDefeat) Defeat();
                else if (game.SavedBoss) Victory();
            }
        }

        private void OnDestroy()
        {
            if (game == null) return;
            game.OnBossDefeated -= Victory; game.OnDefeat -= Defeat;
        }

        private void Update()
        {
            if (objective == null) return;
            // 전체 화면 월드맵이 열려 있으면 원정대 판을 가리지 않게 숨긴다.
            if (worldMap == null) worldMap = FindFirstObjectByType<WorldMapPanel>();
            objectivePanel.SetActive(worldMap == null || !worldMap.IsOpen);
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .5f;
            objective.text = "<color=#f2a93b><b>목표</b></color>\n" + CurrentObjective + "\n<color=#968976>F2 설명서</color>";
        }

        private string BuildObjective()
        {
            if (game == null) return "소굴 준비 중…";
            var campaign = CampaignResearch.Instance;
            if (campaign != null && campaign.Departed) return "로켓 발사 완료";
            if (campaign != null && campaign.Active != null)
                return $"연구 · {campaign.Active.Name}\n{campaign.Progress:0}/{campaign.Active.Work:0} · 과학 화면에서 연구 장수 지시";
            if (game.SavedBoss) return "로켓 탈출 · 보스 보상 회수\n본거지로 귀환 후 로켓 연구";
            var world = WorldMapManager.Instance;
            if (world != null)
            {
                foreach (var ship in world.Transports)
                {
                    if (ship == null || ship.State == ExpeditionState.Home) continue;
                    if (ship.State == ExpeditionState.Outbound)
                        return $"이동 중 · {Mathf.CeilToInt(ship.Remaining)}초\n{ship.Site.Title} · 도착 후 전장 보기";
                    if (ship.State == ExpeditionState.Returning)
                        return $"귀환 중 · {Mathf.CeilToInt(ship.Remaining)}초\n도착하면 화물을 본거지에 반납합니다";
                    return ship.Site.Cleared || ship.Site.Kind == ExpeditionSiteKind.ResourceSite
                        ? "전리품 · 수송수단에 자원 반납\n탑승 장수를 8m 안에 모은 뒤 귀환"
                        : $"전투 · {ship.Site.Title}\n전장 보기에서 부대를 지휘하세요";
                }
                foreach (var ship in world.Transports)
                {
                    if (ship == null) continue;
                    return ship.Crew.Count > 0
                        ? $"출정 준비 · 장수 {ship.CommanderLoad}/{ship.CommanderCapacity} · 병력 {ship.Load}/{ship.Capacity}\n월드맵에서 목적지 선택 후 출정"
                        : $"탑승 · 장수 {ship.CommanderCapacity}명 · 병력 {ship.Capacity}마리\n장수를 8m 안에 모은 뒤 선택 부대 탑승";
                }
                foreach (var lab in FindObjectsByType<ScienceLab>())
                    if (lab.Busy)
                        return $"{(lab.Constructing ? "건조" : "연구")} - {Mathf.CeilToInt(lab.Remaining)}초\n{(lab.Aircraft ? "비행기" : "차량")} 진행 중";
                if (world.VehicleResearched) return "차량 건조 · 식량 50 / 재료 60\n과학연구소 선택 후 차량 건조";
            }
            if (FindAnyObjectByType<ScienceLab>() != null)
                return "차량 연구 · 증기 시대 연구소 필요\n과학 화면에서 연구소 강화·장수 배정";
            if (ScienceLab.PrerequisitesMet)
                return "과학연구소 건설\n건설 메뉴에서 과학연구소 배치";
            var population = AntPool.Instance != null ? AntPool.Instance.Total : 0;
            if (population < ScienceLab.RequiredPopulation)
                return $"인구 증가 · {population}/{ScienceLab.RequiredPopulation}마리\n주거·공공서비스·민심을 확보해 이주 유도";
            var barracks = FindAnyObjectByType<Barracks>();
            if (barracks == null) return "병영 건설 · 장수 선택\n건설 메뉴에서 병영을 배치하세요";
            foreach (var candidate in FindObjectsByType<Barracks>())
                if (candidate.IsUpgrading) return "병영 강화 중\n과학연구소에 필요한 자원을 모으세요";
            var resources = ResourceManager.Instance;
            if (resources != null && (resources.GetAmount(ResourceType.Food) >= resources.GetCapacity(ResourceType.Food)
                || resources.GetAmount(ResourceType.Soil) >= resources.GetCapacity(ResourceType.Soil)))
                return "창고 가득 참 · 자원 사용 또는 창고 건설\n다음 목표 · 병영 2티어 강화";
            return "병영 강화 · 2티어 달성\n병영을 선택해 티어를 강화하세요";
        }

        private void Victory()
        {
            if (CampaignResearch.Instance != null && CampaignResearch.Instance.Departed) GameMenuController.Instance?.ShowDeparture();
            else GameMenuController.Instance?.ShowOutcome(true);
        }
        private void Defeat() => GameMenuController.Instance?.ShowOutcome(false);
    }
}
