using System.Linq;
using AntColony.Core;
using UnityEngine;

namespace AntColony.World
{
    // Read the existing reward ledger. Display objects never grant or consume rewards.
    public sealed class BossRewardVisual : MonoBehaviour
    {
        readonly GameObject[] items = new GameObject[7];
        float next;
        void LateUpdate()
        {
            if (Time.unscaledTime < next || AntColony.Save.SaveSystem.Busy || CampaignResearch.Instance == null) return;
            next = Time.unscaledTime + .5f;
            var state = CampaignResearch.Instance.CaptureState().bosses;
            var stronghold = GetComponent<BossStronghold>();
            for (var i = 0; i < items.Length; i++)
            {
                var ground = stronghold != null;
                var show = ground ? i == (int)stronghold.Id && state.unclaimed.Any(p => p.boss == i)
                    : i < 4 ? state.home[i] > 0 : state.tools[i - 3] > 0;
                if (!show) { if (items[i] != null) Destroy(items[i]); items[i] = null; continue; }
                if (items[i] != null) continue;
                var id = i < 4 ? "Prop_BossCore_" + (char)('A' + i) : "Prop_Detector_" + (char)('B' + i - 4);
                var position = ground ? transform.position + Vector3.right * 4 : ResourceManager.HomePosition + new Vector3(i - 3, 0, -3);
                items[i] = ProductionProp.Attach(transform, id, .7f, position);
            }
        }
    }
}
