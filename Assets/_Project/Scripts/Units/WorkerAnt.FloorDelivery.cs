using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    public partial class WorkerAnt
    {
        protected bool TryMaterialSource(IMaterialSink site, AntColony.Data.ResourceType type, int needed,
            out StoreBin bin, out ResourceNode node, out Vector3 approach, out int amount)
        {
            bin = null; node = null; approach = default; amount = 0;
            var rm = ResourceManager.Instance;
            if (rm == null) return false;
            foreach (var candidate in rm.Bins.Where(b => b.Spendable && b.Amount(type) > 0).OrderBy(b => (b.Position - Position).sqrMagnitude))
            {
                var host = DepositHost(candidate);
                if (host == null || !TryWorkApproach(host.Position, out approach)) continue;
                amount = Mathf.Min(needed, Mathf.FloorToInt(CarryCapacity), candidate.Amount(type));
                if (amount <= 0) return false;
                bin = candidate; return true;
            }
            if (!(site is BuildingConstructionSite)) return false;
            foreach (var candidate in ConstructionFloorMaterials.Available(type).OrderBy(n => (n.transform.position - Position).sqrMagnitude))
            {
                if (!candidate.CanGatherWith(this) || !TryWorkApproach(candidate.transform.position, out approach)) continue;
                amount = Mathf.Min(needed, Mathf.FloorToInt(CarryCapacity), Mathf.FloorToInt(candidate.AmountRemaining));
                if (amount <= 0) continue;
                node = candidate; return true;
            }
            return false;
        }

        private int TakeFloorMaterial(out FoodLots lots)
        {
            lots = null;
            if (!(deliverySite is BuildingConstructionSite) || targetNode == null || !targetNode.IsLooseCargo
                || targetNode.ResourceType != fetchType || !targetNode.CanGatherWith(this)
                || Vector3.Distance(Position, targetNode.transform.position) > 4f) return 0;
            var food = targetNode.GetComponent<FloorFood>();
            var sourceLots = food != null ? food.Sync() : null;
            int amount = Mathf.FloorToInt(targetNode.Extract(Mathf.Min(fetchAmount, Mathf.FloorToInt(targetNode.AmountRemaining))));
            if (sourceLots != null) lots = sourceLots.Take(amount);
            targetNode = null;
            return amount;
        }
    }
}
