using System.Collections.Generic;
using System.Linq;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    public static class FluidNetwork
    {
        // 재배치·철거·저장 복원 직후에도 낡은 연결을 쓰지 않도록 필요할 때 작은 배관망을 순회한다.
        private static List<SupplyFacility> Connected(BuildingBase from, AntColony.Data.ResourceType fluid)
        {
            if (from is SupplyFacility origin && !origin.AcceptsFluid(fluid)) return new List<SupplyFacility>();
            var active = SupplyFacility.All.Where(n => n != null && n.Operational && !n.IsNaturalSource && n.AcceptsFluid(fluid)).ToList();
            var reached = new List<SupplyFacility>(); var queue = new Queue<BuildingBase>(); queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue(); var cells = RoomSystem.Footprint(current);
                foreach (var node in active)
                {
                    if (reached.Contains(node) || !RoomSystem.Footprint(node).Any(c => cells.Any(a => Mathf.Abs(a.x - c.x) + Mathf.Abs(a.y - c.y) <= 1))) continue;
                    reached.Add(node); queue.Enqueue(node);
                }
            }
            return reached;
        }
        private static IEnumerable<SupplyFacility> Outputs(SupplyFacility from, AntColony.Data.ResourceType fluid, int amount)
            => Connected(from, fluid).Where(n => (n.Kind == BuildingKind.FluidTank || n.Kind == BuildingKind.GasTank) && n.CanHold(fluid, amount))
                .Concat(from.CanHold(fluid, amount) ? new[] { from } : System.Array.Empty<SupplyFacility>());
        public static bool CanDeliver(SupplyFacility from, AntColony.Data.ResourceType fluid, int amount) => Outputs(from, fluid, amount).Any();
        public static bool Deliver(SupplyFacility from, AntColony.Data.ResourceType fluid, int amount)
            => Outputs(from, fluid, amount).FirstOrDefault()?.Fill(fluid, amount) == true;
        public static bool Has(BuildingBase from, AntColony.Data.ResourceType fluid, int amount)
            => amount > 0 && Connected(from, fluid).Where(n => n.Supply.fluid == (int)fluid).Sum(n => n.Supply.amount) >= amount;
        public static bool Take(BuildingBase from, AntColony.Data.ResourceType fluid, int amount)
        {
            var sources = Connected(from, fluid).Where(n => n.Supply.fluid == (int)fluid).ToList();
            if (amount <= 0 || sources.Sum(n => n.Supply.amount) < amount) return false;
            foreach (var n in sources) { var take = Mathf.Min(amount, n.Supply.amount); n.Drain(take); amount -= take; if (amount == 0) break; }
            return true;
        }
    }
}
