using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    public sealed class StartingAppearance : MonoBehaviour
    {
        private void Start()
        {
            var c = GetComponent<CommanderAnt>();
            if (c == null || c.PersonalState.appearanceSeed <= 0) return;
            var seed = c.PersonalState.appearanceSeed;
            var tint = new MaterialPropertyBlock(); var color = StartingCandidate.AppearanceColor(seed);
            tint.SetColor("_BaseColor", color); tint.SetColor("_Color", color);
            foreach (var r in GetComponentsInChildren<Renderer>()) if (!(r is LineRenderer)) r.SetPropertyBlock(tint);
            var model = GetComponentInChildren<Animator>();
            if (model != null) model.transform.localScale *= .9f + seed % 3 * .1f;
        }
    }
}
