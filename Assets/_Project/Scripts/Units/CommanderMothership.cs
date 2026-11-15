using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        internal void FinishEscort() => FinishReturn();
        internal void BeginMothershipSortie(Vector3 landing)
        {
            AllocateDeploymentFood();
            SetEmbarked(false,landing); CommandStop();
            WorkState.duty=CommanderDuty.Deployed; WorkState.holdDeployment=true;
        }
        internal void EndMothershipSortie(Vector3 home)
        {
            SetEmbarked(false,home); FinishReturn();
        }
    }
}
