using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;

namespace AntColony.Buildings
{
    public sealed partial class Workshop
    {
        public bool IsSewing => Data != null && Data.kind == BuildingKind.SewingTable;
        public bool Accepts(EquipmentRecipe recipe) => EquipmentRecipes.Unlocked(recipe) && IsSewing == EquipmentRecipes.IsClothing(recipe);
        public static bool RepairReserved(string id) => SceneObjects.All<Workshop>().Any(w => w.jobs.Any(j => j.repairId == id));
        public bool TryRepair(EquipmentItem item)
        {
            if (!IsSewing || Ruined || IsDead || !isActiveAndEnabled || jobs.Count >= GameBalance.CraftQueueCapacity
                || item == null || !item.IsClothing || item.durability >= EquipmentItem.ClothingCapacity
                || EquipmentInventory.Instance?.Items.Contains(item) != true || RepairReserved(item.id)) return false;
            jobs.Add(new Job { recipe = EquipmentRecipe.ClothClothes, repairId = item.id });
            return true;
        }
        internal static IEnumerable<(ResourceType type, int amount)> Materials(Job job)
        {
            if (EquipmentRecipes.IsClothing(job.recipe))
            {
                yield return (ResourceType.Cloth, string.IsNullOrEmpty(job.repairId) ? 4 : 1);
                yield break;
            }
            var cost = EquipmentRecipes.Cost(job.recipe);
            if (cost.x > 0) yield return (ResourceType.Food, cost.x);
            if (cost.y > 0) yield return (ResourceType.Soil, cost.y);
            if (cost.z > 0) yield return (ResourceType.Special, cost.z);
        }
    }
}
