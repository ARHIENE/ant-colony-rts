using System;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    // Per-instance finish: construction, renovation and load all use BuildingBase.MainMaterial.
    // Keep roof foliage, windows, food and other detail materials intact.
    public static class FacilityMaterialVisual
    {
        public static bool IsStructure(string name) => name.IndexOf("Story_Oak", StringComparison.Ordinal) >= 0
            || name.IndexOf("Story_CutWood", StringComparison.Ordinal) >= 0
            || name.IndexOf("Story_OchrePlaster", StringComparison.Ordinal) >= 0
            || name.IndexOf("Story_CreamPlaster", StringComparison.Ordinal) >= 0
            || name.IndexOf("CC_Wood", StringComparison.Ordinal) >= 0
            || name.IndexOf("CC_Bark", StringComparison.Ordinal) >= 0
            || name.IndexOf("CC_Stone", StringComparison.Ordinal) >= 0;

        public static void Apply(Transform model, ResourceType material)
        {
            if (!MaterialInfo.For(material).structural) return;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                for (var i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    var source = renderer.sharedMaterials[i];
                    if (source == null || !IsStructure(source.name)) continue;
                    var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block, i);
                    // Soil is the legacy/default finish; returning to it restores the authored palette.
                    var original = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.color;
                    block.SetColor("_BaseColor", material == ResourceType.Soil ? original : ColorFor(material));
                    var metal = material is ResourceType.Iron or ResourceType.Copper or ResourceType.Steel;
                    block.SetFloat("_Metallic", metal ? .55f : source.HasProperty("_Metallic") && material == ResourceType.Soil ? source.GetFloat("_Metallic") : 0);
                    block.SetFloat("_Smoothness", metal ? .24f : source.HasProperty("_Smoothness") && material == ResourceType.Soil ? source.GetFloat("_Smoothness") : .05f);
                    renderer.SetPropertyBlock(block, i);
                }
        }

        public static Color ColorFor(ResourceType material) => material switch
        {
            ResourceType.Stone => new Color(.48f, .49f, .43f),
            ResourceType.StoneBlock => new Color(.61f, .62f, .55f),
            ResourceType.Clay => new Color(.68f, .44f, .29f),
            ResourceType.Brick => new Color(.58f, .29f, .19f),
            ResourceType.Wood => new Color(.49f, .32f, .18f),
            ResourceType.Plank => new Color(.64f, .47f, .28f),
            ResourceType.Leaf => new Color(.40f, .52f, .29f),
            ResourceType.Chitin => new Color(.39f, .31f, .22f),
            ResourceType.Glass => new Color(.60f, .76f, .74f),
            ResourceType.Cloth => new Color(.79f, .71f, .55f),
            ResourceType.Iron => new Color(.37f, .41f, .42f),
            ResourceType.Copper => new Color(.64f, .36f, .20f),
            ResourceType.Steel => new Color(.56f, .60f, .59f),
            ResourceType.Wax => new Color(.82f, .69f, .38f),
            _ => new Color(.57f, .41f, .27f)
        };
    }
}
