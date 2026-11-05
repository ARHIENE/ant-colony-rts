using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    public partial class WorkerAnt
    {
        private bool FetchFloorFor(IMaterialSink site, AntColony.Data.ResourceType type, int needed)
        {
            if (!(site is BuildingConstructionSite)) return false;
            foreach (var node in ConstructionFloorMaterials.Available(type).OrderBy(n => (n.transform.position - Position).sqrMagnitude))
            {
                if (!node.CanGatherWith(this) || !TryWorkApproach(node.transform.position, out var approach)) continue;
                var amount = Mathf.Min(needed, Mathf.FloorToInt(CarryCapacity), Mathf.FloorToInt(node.AmountRemaining));
                if (amount <= 0) continue;
                CommandStop();
                if (!site.BeginPickup(this, type, amount)) return false;
                // targetNode는 기존 WorkerOrderDto.node로 저장·복원한다. fetchBin이 없는 배송만 바닥에서 집는다.
                deliverySite = site; targetNode = node; fetchBin = null; fetchType = type; fetchAmount = amount;
                SetMoveDestination(approach); state = State.FetchingMaterial; return true;
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