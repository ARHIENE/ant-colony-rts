using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    public partial class ExpeditionSite
    {
        internal void HoldInvestigator(CommanderAnt commander)
        {
            if (Defense == null) Defense = gameObject.AddComponent<SettlementDefense>();
            Defense.RestorePrisoner(commander);
        }
    }
}
