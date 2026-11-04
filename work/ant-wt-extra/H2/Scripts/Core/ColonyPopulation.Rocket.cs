using AntColony.Buildings;
using UnityEngine;

namespace AntColony.Core
{
    public sealed partial class ColonyPopulation
    {
        private float observeAt = -10;
        private void Start() => ObserveRocketEconomy();
        private void LateUpdate() { if (Time.unscaledTime - observeAt >= 1f) ObserveRocketEconomy(); }

        // 전 지역 재산 순회는 무거워 1초마다만 관측한다(시간가중 평균에는 충분한 해상도).
        public void ObserveRocketEconomy()
        {
            if (AntColony.Save.SaveSystem.Busy || !GameSession.Exists || !GameSession.Instance.GameStarted) return;
            observeAt = Time.unscaledTime;
            var owned = RocketEconomy.CaptureOwned();
            ApplyExpectationTier(GameBalanceNeeds.ExpectationTier(owned.wealth));
            RocketCampaign.Instance?.Economy?.Observe(GameCalendar.GameSeconds, owned.wealth, owned.population);
        }

        // G31: 재산 등급이 오르면 기대 수준 보정이 줄어든다(저재산 +15 → 정착지급 +5 → 산업급 0).
        private static void ApplyExpectationTier(int tier)
        {
            if (CommanderRoster.Instance == null) return;
            foreach (var c in CommanderRoster.Instance.Commanders)
                if (c != null && c.PersonalState != null) c.PersonalState.expectationTier = tier;
        }
    }
}
