using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    public sealed partial class PowerNode
    {
        // 기존 증기 발전기와 같은 잠정 400W·연료1당10초. 저장된 연소 시간은 배관과 무관하다.
        public void TickGas(float seconds)
        {
            if (Kind != BuildingKind.GasGenerator || !isActiveAndEnabled || IsDead || !(seconds > 0) || float.IsInfinity(seconds)
                || Time.timeScale <= 0 || AntColony.Save.SaveSystem.Busy || !PowerGrid.WantsPower(this)) return;
            var remaining = seconds;
            while (remaining > 0)
            {
                if (burnSeconds <= 0)
                {
                    if (!FluidNetwork.Take(this, ResourceType.NaturalGas, 1)) return;
                    burnSeconds = GameBalance.WoodBurnSeconds;
                }
                var dt = Mathf.Min(remaining, burnSeconds); burnSeconds -= dt; remaining -= dt;
            }
        }
        public string GasStatus => $"가스 발전 {Output:0}/{SteamWatts:0}W · 투입 연료 {FuelSeconds:0.#}초\n기체 배관 공급 · 가스1당 {GameBalance.WoodBurnSeconds:0}초 · 수요 없으면 대기";
    }
}
