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
            if (game == null) return "Preparing colony...";
            var campaign = CampaignResearch.Instance;
            if (campaign != null && campaign.Departed) return "GREAT MIGRATION COMPLETE";
            if (campaign != null && campaign.Active != null)
                return $"RESEARCH - {campaign.Active.Name}\n{campaign.Progress:0}/{campaign.Active.Work:0}: assign matching-tier researchers";
            if (game.SavedBoss) return "SCIENCE ESCAPE - Collect boss reward cargo\nReturn home; research the Great Migration";
            var world = WorldMapManager.Instance;
            if (world != null)
            {
                foreach (var ship in world.Transports)
                {
                    if (ship == null || ship.State == ExpeditionState.Home) continue;
                    if (ship.State == ExpeditionState.Outbound)
                        return $"TRAVELLING - {Mathf.CeilToInt(ship.Remaining)}s\n{ship.Site.Title}: View Battlefield on arrival";
                    if (ship.State == ExpeditionState.Returning)
                        return $"RETURNING - {Mathf.CeilToInt(ship.Remaining)}s\nCargo is delivered at home";
                    return ship.Site.Cleared || ship.Site.Kind == ExpeditionSiteKind.ResourceSite
                        ? "LOOT - Gather into your transport\nRally crew within 8m, then Return Home"
                        : $"BATTLE - {ship.Site.Title}\nView Battlefield; command your troops";
                }
                foreach (var ship in world.Transports)
                {
                    if (ship == null) continue;
                    return ship.Crew.Count > 0
                        ? $"READY - Commanders {ship.CommanderLoad}/{ship.CommanderCapacity}, troops {ship.Load}/{ship.Capacity}\nWorld / Science: choose a nest, Depart"
                        : $"BOARD - {ship.CommanderCapacity} commanders + {ship.Capacity} troops\nBring troops within 8m; Board Selected";
                }
                foreach (var lab in FindObjectsByType<ScienceLab>())
                    if (lab.Busy)
                        return $"{(lab.Constructing ? "CONSTRUCTION" : "RESEARCH")} - {Mathf.CeilToInt(lab.Remaining)}s\n{(lab.Aircraft ? "Aircraft" : "Vehicle")} in progress";
                if (world.VehicleResearched) return "BUILD VEHICLE - 50 Food / 60 Soil\nWorld / Science: Build Vehicle";
            }
            if (FindAnyObjectByType<ScienceLab>() != null)
                return "RESEARCH VEHICLE - Tier 2 lab\nScience / Researchers: upgrade and assign";
            if (ScienceLab.PrerequisitesMet)
                return "BUILD SCIENCE LAB - World / Science\nSelect a commander; keep 8 free ants";
            var population = AntPool.Instance != null ? AntPool.Instance.Total : 0;
            if (population < ScienceLab.RequiredPopulation)
                return $"GROW - {population}/{ScienceLab.RequiredPopulation} ants\nGather Food, then Produce Ant";
            if (!game.FishingUnlocked) return "UNLOCK FISHING - Queen Chamber\nUse Unlock Fishing in the colony panel";
            var barracks = FindAnyObjectByType<Barracks>();
            if (barracks == null) return "BUILD BARRACKS - Select a commander\nChoose a combat role, then Build Barracks";
            foreach (var candidate in FindObjectsByType<Barracks>())
                if (candidate.IsUpgrading) return "BARRACKS UPGRADE - In progress\nGather resources for the Science Lab";
            var resources = ResourceManager.Instance;
            if (resources != null && (resources.GetAmount(ResourceType.Food) >= resources.GetCapacity(ResourceType.Food)
                || resources.GetAmount(ResourceType.Soil) >= resources.GetCapacity(ResourceType.Soil)))
                return "STORAGE FULL - Spend or Build Storage\nNext: upgrade any barracks to Tier 2";
            return "UPGRADE BARRACKS - Reach Tier 2\nChoose its role, then use the T1>T2 button";
        }

        private void Victory()
        {
            if (CampaignResearch.Instance != null && CampaignResearch.Instance.Departed) GameMenuController.Instance?.ShowDeparture();
            else GameMenuController.Instance?.ShowOutcome(true);
        }
        private void Defeat() => GameMenuController.Instance?.ShowOutcome(false);
    }
}
