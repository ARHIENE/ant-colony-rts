using System;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    public partial class ResourceNode
    {
        public static bool TreasureCatch(float roll) => roll >= 0 && roll < .05f;
        public static bool TreasureEquipment(float roll) => roll >= 0 && roll < .1f;
        public static int TreasureQuality(float roll) => roll < .05f ? 0 : roll < .2f ? 1 : roll < .7f ? 2 : 3;
        public static ResourceType[] TreasureResources(int era) => era >= 4 ? new[] { ResourceType.Special, ResourceType.Iron, ResourceType.Component }
            : era >= 3 ? new[] { ResourceType.Special, ResourceType.Iron, ResourceType.Copper } : new[] { ResourceType.Special, ResourceType.Stone, ResourceType.Herb };
        internal void FishingTreasure(AntColony.Units.WorkerAnt worker, float caught)
        {
            if (!(caught > 0) || !(worker is CommanderAnt c) || !c.Traits.Has(CommanderTrait.DiamondFishing) || !TreasureCatch(UnityEngine.Random.value)) return;
            var era = SceneObjects.All<AntColony.Buildings.ScienceLab>().Where(l => l.isActiveAndEnabled && !l.IsDead).Select(l => l.Tier).DefaultIfEmpty(1).Max();
            var resources = TreasureResources(era);
            foreach (var type in resources) DiplomacyManager.DropFloor(type, type == ResourceType.Special ? 1 : 3, worker.Position);
            if (TreasureEquipment(UnityEngine.Random.value))
            {
                var recipes = Enum.GetValues(typeof(EquipmentRecipe)).Cast<EquipmentRecipe>().Where(r => EquipmentRecipes.Unlocked(r)).ToArray();
                if (recipes.Length > 0) EquipmentLoot.Drop(worker.Position, new[] { EquipmentRecipes.Create(recipes[UnityEngine.Random.Range(0, recipes.Length)], TreasureQuality(UnityEngine.Random.value)) });
            }
            AntColony.UI.ToastManager.Show(c.CommanderName + ": 태공 · 희귀 묶음 어획");
        }
    }
}
