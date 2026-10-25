using AntColony.Buildings;
using AntColony.Data;
using UnityEngine;

namespace AntColony.World
{
    // 제작한 Blender 자원·자연물 모델을 채집 노드와 바닥 물품에 연결한다.
    public static class ResourceVisual
    {
        public const string Folder = "ResourceModels/";

        public static string ModelFor(ResourceType type, bool loose) => !loose ? type switch
        {
            ResourceType.Food => "CC_WildFungus", ResourceType.Wood => "CC_TreeBroadleaf",
            ResourceType.Leaf => "CC_Bush", ResourceType.Fiber => "CC_GrassTuft", ResourceType.Resin => "CC_TreeTall",
            ResourceType.Stone => "CC_RockOutcrop",
            ResourceType.IronOre or ResourceType.CopperOre or ResourceType.TinOre or ResourceType.Coal or ResourceType.Sulfur => "CC_OreOutcrop",
            _ => CargoModel(type)
        } : CargoModel(type);

        private static string CargoModel(ResourceType type) => type switch
        {
            ResourceType.Food => "CC_ResourceGrain", ResourceType.Wood => "CC_ResourceWood",
            ResourceType.Leaf => "CC_ResourceLeaf", ResourceType.Fiber => "CC_ResourceFiber",
            ResourceType.Cobweb or ResourceType.Thread => "CC_ResourceSilk",
            ResourceType.Stone => "CC_ResourceStone", ResourceType.Plank => "CC_ResourcePlanks",
            ResourceType.IronOre => "Ore_Iron", ResourceType.CopperOre => "Ore_Copper", ResourceType.TinOre => "Ore_Tin",
            ResourceType.Coal => "Fossil_Coal", ResourceType.Sulfur => "Mineral_Sulfur",
            ResourceType.Sand => "Mineral_Sand", ResourceType.Clay => "Mineral_Clay", ResourceType.Resin => "Gather_Resin",
            ResourceType.StoneBlock => "Block_Granite", ResourceType.Brick => "Proc_Brick", ResourceType.Glass => "Proc_Glass",
            ResourceType.Cloth => "Cloth_Plant", ResourceType.Iron => "Ingot_Iron", ResourceType.Copper => "Ingot_Copper",
            ResourceType.Bronze => "Ingot_Bronze", ResourceType.Steel => "Ingot_Steel",
            ResourceType.Honeydew => "Ranch_Honeydew", ResourceType.Honey => "Ranch_Honey",
            ResourceType.AnimalMedicine => "CC_MedicineAnimal", ResourceType.SurgeryKit => "CC_SurgeryKit",
            ResourceType.RottenFood => "CC_FoodRotten", ResourceType.SimpleMeal => "CC_MealSimple", ResourceType.FineMeal => "CC_MealGood",
            ResourceType.Manure => "CC_Manure", ResourceType.Fertilizer => "CC_ResourceCompost",
            ResourceType.MedicineBasic => "CC_MedicineHerbal", ResourceType.MedicineNormal => "CC_MedicineStandard",
            ResourceType.MedicineAdvanced => "CC_MedicineAdvanced", ResourceType.PlantingMaterial => "Grain_Wheat",
            ResourceType.CrudeOil => "Fossil_CrudeOil", ResourceType.Petroleum => "Proc_Fuel", ResourceType.Herb => "Crop_Herb",
            _ => null
        };

        public static void Attach(ResourceNode node)
        {
            if (node.RequiresFishing || node.GetComponent<BuildingBase>() != null || node.transform.Find("ResourceModel") != null) return;
            var box = node.GetComponent<Renderer>();
            if (box == null) return;
            if (!node.IsLooseCargo && node.ResourceType == ResourceType.Soil) { box.forceRenderingOff = true; return; }
            if (node.IsLooseCargo && ProductionProp.ForResource(node.ResourceType) != null) return;
            var id = ModelFor(node.ResourceType, node.IsLooseCargo);
            if (id == null) return;
            var width = !node.IsLooseCargo && node.ResourceType is ResourceType.Wood or ResourceType.Resin ? 2.8f
                : !node.IsLooseCargo && ModelFor(node.ResourceType, false) is "CC_RockOutcrop" or "CC_OreOutcrop" ? 1.8f : 1.2f;
            var bounds = box.bounds;
            var floor = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            if (!node.IsLooseCargo && UnityEngine.AI.NavMesh.SamplePosition(node.transform.position, out var hit, 3, UnityEngine.AI.NavMesh.AllAreas)) floor = hit.position;
            var model = ProductionProp.Attach(node.transform, Folder + id, width, floor);
            if (model == null) return;
            model.name = "ResourceModel"; box.forceRenderingOff = true;
            FitHitbox(node, model);
        }

        internal static void FitHitbox(ResourceNode node, GameObject model)
        {
            var visible = model.GetComponentsInChildren<Renderer>();
            var modelBounds = visible[0].bounds;
            foreach (var r in visible) modelBounds.Encapsulate(r.bounds);
            var hitbox = node.GetComponent<BoxCollider>();
            if (hitbox == null)
            {
                foreach (var old in node.GetComponents<Collider>()) Object.Destroy(old);
                hitbox = node.gameObject.AddComponent<BoxCollider>();
            }
            hitbox.isTrigger = true;
            hitbox.center = node.transform.InverseTransformPoint(modelBounds.center);
            var scale = node.transform.lossyScale;
            hitbox.size = new Vector3(modelBounds.size.x / Mathf.Abs(scale.x), modelBounds.size.y / Mathf.Abs(scale.y), modelBounds.size.z / Mathf.Abs(scale.z));
        }
    }
}
