using System.Linq;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        // 곰팡이 감염(2026-10-15 의료 개편): 질병(심각도·면역)으로 진행하고, 가까운 장수에게 옮는다. 장수·동물 사이 전염은 없다.
        private float moldSpreadTimer;
        public bool HasMold => personalState.health.illnesses.Any(i => i.kind == IllnessKind.Mold);
        private void TickInfection(float seconds)
        {
            if (!HasMold || IsDead || IsEmbarked || IsCaptive) return;
            moldSpreadTimer += seconds;
            while (moldSpreadTimer >= EventRules.InfectionSpreadSeconds)
            {
                moldSpreadTimer -= EventRules.InfectionSpreadSeconds;
                foreach (var other in Active.OfType<CommanderAnt>().ToArray())
                    if (other != this && !other.IsDead && !other.IsEmbarked && !other.IsCaptive && !other.HasMold
                        && Vector3.Distance(Position, other.Position) <= EventRules.InfectionRadius
                        && Random.value < EventRules.InfectionChance * ScienceEffects.MoldSpreadMultiplier * BiomeRules.MoldSpread)
                    {
                        ColonyEvents.Infect(other);
                        CampaignHistory.Record("감염", other.CommanderName, "곰팡이 감염 전파", true);
                    }
            }
        }
    }
}
