using UnityEngine;

namespace AntColony.World
{
    public partial class ExpeditionTransport
    {
        private GameObject walkingPack;

        private void AttachWalkingPack()
        {
            if (walkingPack != null) return;
            walkingPack = ProductionProp.Attach(transform, "Transport1115/ExpeditionPack", 1.5f, Position);
            if (walkingPack != null && GetComponent<Renderer>() is Renderer placeholder)
                placeholder.forceRenderingOff = true;
        }
    }
}
