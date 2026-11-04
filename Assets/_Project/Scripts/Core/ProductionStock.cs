using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Data;
using AntColony.Map;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.Core
{
    // 재고 유지의 본거지 완제품. 입고 예약은 실물이 아니므로 더하지 않는다.
    public static class ProductionStock
    {
        public static bool AtHome(Vector3 position)
        {
            var bounds = HomeMapBuilder.CurrentWorldBounds;
            return bounds.Contains(new Vector3(position.x, 0, position.z));
        }

        public static int Carried(ResourceType type, IEnumerable<WorkerAnt> workers, Func<WorkerAnt, bool> inScope)
            => Mathf.FloorToInt(workers.Where(w => inScope(w)
                && w.CarriedType == type && w.CountsForHomeStock).Sum(w => w.CarriedAmount));

        public static int Count(ResourceType type) => (ResourceManager.Instance?.Bins.Where(b => b.Spendable && AtHome(b.Position)).Sum(b => b.Amount(type)) ?? 0)
            + Mathf.FloorToInt(ResourceNode.Available.Where(n => n != null && n.IsLooseCargo && !n.IsRaidLoot
                && n.ResourceType == type && AtHome(n.transform.position)).Sum(n => n.AmountRemaining))
            + Carried(type, AntUnitBase.Active.OfType<WorkerAnt>(), w => w != null && w.isActiveAndEnabled && !w.IsDead && AtHome(w.Position));
    }
}
