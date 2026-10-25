using UnityEngine;

namespace AntColony.World
{
    public sealed class CorpseProp : MonoBehaviour
    {
        Corpse corpse;
        bool shown;
        float born;
        void Awake() { corpse = GetComponent<Corpse>(); born = Time.time; }
        void LateUpdate()
        {
            if (shown || corpse.Data == null || corpse.Data.kind != CorpseKind.Wildlife && Time.time - born < .5f) return;
            var id = corpse.Remains || corpse.Data.kind == CorpseKind.Wildlife ? "Prop_Remains" : corpse.Data.kind == CorpseKind.Ant ? "Prop_CorpseBundle" : null;
            if (id == null) return;
            var prop = ProductionProp.Attach(transform, id, .9f, corpse.Position);
            if (prop == null) return;
            foreach (var r in GetComponentsInChildren<Renderer>())
                if (!r.transform.IsChildOf(prop.transform)) r.enabled = false;
            shown = true;
        }
    }
}
