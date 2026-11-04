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
        // 정식 캠페인은 첫 보스로 종료하지 않는다. 전용 베타 시나리오 진입부에서만 설정한다.
        public bool BetaScenario
        {
            get => CampaignResearch.Instance != null && CampaignResearch.Instance.BetaScenario;
            set { if (CampaignResearch.Instance != null) CampaignResearch.Instance.BetaScenario = value; }
        }
        private UnityEngine.UI.Text objective;
        private GameObject objectivePanel;
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
            // 같은 패널에서 초반 체크리스트 이후에도 현재 상태에 맞는 다음 준비를 안내한다.
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .5f;
            var guide = StartGuide.Active && !GameMenuController.MenuOpen;
            objectivePanel.SetActive(!GameMenuController.MenuOpen && GameSession.Exists && GameSession.Instance.GameStarted && UserSettings.Current.firstHints);
            if (guide)
            {
                ((RectTransform)objectivePanel.transform).sizeDelta = new Vector2(360, 128);
                objective.text = StartGuide.Text();
                return;
            }
            ((RectTransform)objectivePanel.transform).sizeDelta = new Vector2(360, 92);
            objective.text = "<b>다음 준비</b>\n" + CurrentObjective;
        }

        private string BuildObjective() => CampaignNextSteps.Text();

        private void Victory()
        {
            if (CampaignResearch.Instance != null && CampaignResearch.Instance.Departed) GameMenuController.Instance?.ShowDeparture();
            else if (BetaScenario) GameMenuController.Instance?.ShowOutcome(true);
        }
        private void Defeat() => GameMenuController.Instance?.ShowOutcome(false);
    }
}
