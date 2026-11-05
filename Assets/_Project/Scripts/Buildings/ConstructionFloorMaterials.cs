using System.Collections.Generic;
using System.Linq;
using AntColony.Data;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    // 불시착 뒤 첫 창고도 바닥에 있는 재료로 실제 배송해 지을 수 있어야 한다.
    internal static class ConstructionFloorMaterials
    {
        internal static IEnumerable<ResourceNode> Available(ResourceType type) => ResourceNode.Available.Where(n =>
            n != null && n.IsLooseCargo && n.ResourceType == type && n.CanGather && !n.IsRaidLoot
            && n.GetComponentInParent<ExpeditionSite>() == null);
        internal static int Amount(ResourceType type) => Available(type).Sum(n => Mathf.FloorToInt(n.AmountRemaining));
    }
}