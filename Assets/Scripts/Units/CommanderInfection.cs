using System.Linq;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        private void TickInfection(float seconds)
        {
            while (seconds > 0 && PersonalState.infected && !IsDead)
            {
                var dt = Mathf.Min(1, seconds); seconds -= dt; TickInfectionStep(dt);
            }
        }
        private void TickInfectionStep(float seconds)
        {
            var p = PersonalState;
            if (!p.infected) return;
            // 치료 60초를 작업량으로 보관하므로 방역 연구가 도중 완료되어도 진행도를 잃지 않는다.
            if (p.treating)
            {
                p.moldTreatment += seconds * 60 / ScienceEffects.MoldTreatSeconds;
                if (p.moldTreatment >= 60)
                {
                    p.infected = false; p.moldTreatment = p.moldLoss = p.moldSpread = 0;
                    CampaignHistory.Record("치료", CommanderName, "곰팡이 감염 회복", true); return;
                }
            }
            p.moldLoss += seconds;
            while (p.moldLoss >= EventRules.InfectionLossSeconds)
            {
                p.moldLoss -= EventRules.InfectionLossSeconds;
                LoseTroops(1, "곰팡이 감염");
                if (IsDead) return;
            }
            p.moldSpread += seconds;
            while (p.moldSpread >= EventRules.InfectionSpreadSeconds)
            {
                p.moldSpread -= EventRules.InfectionSpreadSeconds;
                if (IsEmbarked || IsCaptive) continue;
                foreach (var other in Active.OfType<CommanderAnt>().ToArray())
                    if (other != this && !other.IsDead && !other.IsEmbarked && !other.IsCaptive && !other.PersonalState.infected
                        && Vector3.Distance(Position, other.Position) <= EventRules.InfectionRadius
                        && Random.value < EventRules.InfectionChance * ScienceEffects.MoldSpreadMultiplier)
                    {
                        ColonyEvents.Infect(other);
                        CampaignHistory.Record("감염", other.CommanderName, "곰팡이 감염 전파", true);
                    }
            }
        }
    }
}
