using System.Collections.Generic;
using System.Linq;
using AntColony.Map;
using UnityEditor;
using UnityEngine;

// 2026-10-03 바이옴 맵 스타일 셋업(다시 돌려도 됨): ThirdParty 팩의 기본 셰이더 재질을 URP로 바꾸고,
// 물 재질과 Resources/Biomes/{바이옴}.asset 6개를 만든다. 크기·밀도는 잠정, 인스펙터에서 고쳐도 된다.
public static class SetupBiomeStyles
{
    const string V = "Assets/ThirdParty/LowPoly Fantasy Village/Prefabs/Nature/", P = "Assets/ThirdParty/LowPoly Fantasy Village/Prefabs/Props/";
    const string A = "Assets/ThirdParty/AGWYN Low Poly Vegetation/Prefabs/", E = "Assets/ThirdParty/EmaceArt LavaPlant/Prefabs/";
    const string T = "Assets/ThirdParty/Free Stylized Textures/Textures/";
    static readonly List<string> missing = new List<string>();

    static MapGenerator.Layer L(string name, float start)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{T}{name}/{name}_Albedo.png");
        if (tex == null) missing.Add(name);
        return new MapGenerator.Layer { texture = tex, startHeight = start };
    }
    // h0~h1: 높이 비율, p: 정점당 확률, s0~s1: 배율, d: 최소 간격, leaf: 계절 색, solid: 길을 막음
    static IEnumerable<MapGenerator.SpawnObject> S(string folder, string names, float h0, float h1, float p, float s0, float s1, float d, bool leaf = false, bool solid = false)
    {
        foreach (var n in names.Split(','))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + n.Trim() + ".prefab");
            if (prefab == null) { missing.Add(n); continue; }
            yield return new MapGenerator.SpawnObject { prefab = prefab, minHeight = h0, maxHeight = h1, spawnChance = p, minScale = s0, maxScale = s1, minDistanceBetween = d, foliage = leaf, solid = solid };
        }
    }

    public static string Main()
    {
        missing.Clear();
        var converted = ConvertStandardMaterials();
        var water = WaterMaterial();
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/Biomes")) AssetDatabase.CreateFolder("Assets/_Project/Resources", "Biomes");

        Save("Forest", new[] { L("Forest_Ground_38", 0), L("Forest_Ground_38", .25f), L("Grass_37", .45f), L("Rocky_Dirt_2", .65f), L("Cliff_Rock_Surface_21", .82f) },
            S(A, "Tree_Oak,Tree_Pine", .3f, .85f, .012f, .7f, 1.1f, 3, true, true)
            .Concat(S(V, "Tree_01,Tree_03,Tree_07,Tree_09,Tree_10", .3f, .85f, .004f, .7f, 1.1f, 3, true, true))
            .Concat(S(A, "Bush,Bush_Tall", .25f, .8f, .01f, .6f, 1f, 1.5f, true))
            .Concat(S(V, "Mushroom_01,Mushroom_02,Mushroom_03", .2f, .8f, .01f, .8f, 1.4f, 1))
            .Concat(S(A, "Mushroom_Red", .2f, .8f, .004f, .8f, 1.3f, 1))
            .Concat(S(V, "Log_01,Log_02,Stump_01", .25f, .8f, .004f, .8f, 1.2f, 2))
            .Concat(S(V, "Grass_01,Grass_03,Grass_05,Plant_02", .2f, .75f, .02f, .7f, 1.2f, .8f, true))
            .Concat(S(V, "Rock_07,BigRock_03", .6f, 1f, .003f, .7f, 1.2f, 3, false, true)), true, .18f, water);

        Save("Garden", new[] { L("Rippled_Sand_2", 0), L("Rocky_Dirt_2", .2f), L("Grass_37", .4f), L("Forest_Ground_38", .85f), L("Rocky_Dirt_2", .93f) },
            S(A, "Tree_Apple", .3f, .8f, .006f, .7f, 1f, 4, true, true)
            .Concat(S(V, "Tree_04,Tree_06", .3f, .8f, .004f, .8f, 1.1f, 3, true, true))
            .Concat(S(A, "Bush_Berries_Red,Bush_Berries_Blue", .25f, .75f, .006f, .6f, 1f, 1.5f, true))
            .Concat(S(A, "Flower_Blue", .2f, .7f, .015f, .7f, 1f, .6f, true))
            .Concat(S(V, "Flower_01,Flower_02,Flower_03,Flower_04,Flower_05", .2f, .7f, .015f, .9f, 1.5f, .5f, true))
            .Concat(S(V, "Grass_02,Grass_04", .2f, .75f, .02f, .7f, 1.1f, .6f, true))
            .Concat(S(P, "Fence_01,Fence_04", .3f, .7f, .0015f, .9f, 1f, 3, false, true))
            .Concat(S(P, "Barrel_01,Bucket_01", .3f, .7f, .001f, .8f, 1f, 2, false, true))
            .Concat(S(P, "Well", .3f, .6f, .0002f, 1f, 1f, 8, false, true)), true, .15f, water);

        Save("Waterside", new[] { L("Underwater_Rock_Surface_3", 0), L("Rippled_Sand_2", .25f), L("Muddy_Cracked_Sand_6", .4f), L("Grass_37", .6f), L("Rocky_Dirt_2", .82f) },
            S(V, "Reeds_01", .25f, .45f, .06f, .8f, 1.4f, .6f, true)
            .Concat(S(A, "Misc_SugarCane", .3f, .5f, .004f, .5f, .8f, 2, true))
            .Concat(S(V, "Rock_01,Rock_02,Rock_03,Rock_05,Rock_06", .2f, .7f, .008f, .7f, 1.3f, 1.2f))
            .Concat(S(V, "Log_01,Log_03", .3f, .6f, .003f, .8f, 1.1f, 2))
            .Concat(S(P, "Boat", .3f, .42f, .0006f, .9f, 1f, 8, false, true))
            .Concat(S(A, "Tree_Poplar", .5f, .9f, .006f, .7f, 1f, 3, true, true))
            .Concat(S(V, "Tree_05,Tree_08", .5f, .9f, .004f, .8f, 1.1f, 3, true, true))
            .Concat(S(V, "Grass_01,Grass_05", .45f, 1f, .02f, .7f, 1.1f, .7f, true)), true, .38f, water);

        Save("City", new[] { L("Cracked_Asphalt_5", 0), L("Cracked_Asphalt_5", .35f), L("Pavement_7", .55f), L("Cracked_Concrete_26", .75f), L("Rocky_Dirt_2", .9f) },
            S(P, "Box,Barrel_01,Barrel_02", .2f, .9f, .003f, .8f, 1.1f, 1.5f, false, true)
            .Concat(S(P, "Cart_01,Logs", .2f, .8f, .0008f, .9f, 1f, 4, false, true))
            .Concat(S(P, "Bucket_01,Bottle_01,Bottle_02,Bottle_03,Board_01,Board_02,Board_03", .2f, .9f, .003f, 1f, 1.6f, 1))
            .Concat(S(P, "FenceBig_01,FenceBig_05", .3f, .9f, .0008f, .9f, 1f, 4, false, true))
            .Concat(S(V, "Grass_04,Grass_06", .2f, .8f, .01f, .7f, 1f, .8f, true))
            .Concat(S(V, "Tree_05", .3f, .8f, .0015f, .8f, 1f, 3, true, true)), true, .12f, water);

        Save("Desert", new[] { L("Muddy_Cracked_Sand_6", 0), L("Rippled_Sand_2", .2f), L("Rippled_Sand_2", .5f), L("Sandy_Rock_Surface_17", .7f), L("Rocky_Dirt_2", .88f) },
            S(V, "BigRock_01,BigRock_02,BigRock_03", .3f, 1f, .002f, .8f, 1.5f, 4, false, true)
            .Concat(S(V, "Rock_07,Rock_08,Rock_09", .2f, 1f, .005f, .8f, 1.4f, 2, false, true))
            .Concat(S(V, "Rock_01,Rock_02,Rock_03,Rock_04", .1f, 1f, .008f, .7f, 1.3f, 1))
            .Concat(S(E, "EA_Clif_01b_Default,EA_Clif_01c_Default", .5f, 1f, .0004f, .15f, .3f, 12, false, true))
            .Concat(S(E, "Rocks_01c_Default", .5f, 1f, .0003f, .08f, .15f, 12, false, true))
            .Concat(S(V, "Stump_02,Stump_04,Branch", .1f, .8f, .003f, .8f, 1.2f, 1.5f))
            .Concat(S(V, "Plant_03", .1f, .6f, .002f, .7f, 1f, 1.5f, true)), false, 0, water);

        Save("Cave", new[] { L("Underwater_Rock_Surface_3", 0), L("Rocky_Dirt_2", .25f), L("Coal_Surface_2", .5f), L("Rocky_Dirt_2", .7f), L("Crystal_Surface_1", .88f) },
            S(E, "EA_Plant_01a_Default,EA_Plant_01b_Default,EA_Plant_01d_Default", .2f, .8f, .003f, .4f, .7f, 3, true)
            .Concat(S(E, "EA_Fruit_01a_Default,EA_Fruit_01c_Default", .2f, .8f, .003f, .6f, 1f, 1.5f))
            .Concat(S(E, "EA_Clif_01c_Default,EA_Clif_01d_Default", .5f, 1f, .0004f, .15f, .3f, 12, false, true))
            .Concat(S(E, "Rocks_01c_Default,Rocks_01d_Default", .5f, 1f, .0004f, .06f, .12f, 12, false, true))
            .Concat(S(V, "BigRock_01,BigRock_02,BigRock_03", .3f, 1f, .002f, .8f, 1.4f, 4, false, true))
            .Concat(S(V, "Rock_01,Rock_03,Rock_05,Rock_07,Rock_09", .1f, 1f, .008f, .7f, 1.3f, 1.2f))
            .Concat(S(V, "Mushroom_01,Mushroom_03,Mushroom_05,Mushroom_07", .1f, .8f, .012f, .9f, 1.6f, .8f))
            .Concat(S(A, "Mushroom_RedWhite", .1f, .8f, .005f, .9f, 1.5f, 1)), true, .22f, water);

        AssetDatabase.SaveAssets();
        return $"converted {converted} materials; missing: {(missing.Count == 0 ? "none" : string.Join(",", missing))}";
    }

    static void Save(string biome, MapGenerator.Layer[] layers, IEnumerable<MapGenerator.SpawnObject> spawns, bool water, float waterHeight, Material waterMaterial)
    {
        var path = $"Assets/_Project/Resources/Biomes/{biome}.asset";
        var style = AssetDatabase.LoadAssetAtPath<BiomeMapStyle>(path);
        if (style == null) { style = ScriptableObject.CreateInstance<BiomeMapStyle>(); AssetDatabase.CreateAsset(style, path); }
        style.layers = layers.ToList(); style.spawns = spawns.ToList();
        style.water = water; style.waterHeight = waterHeight; style.waterMaterial = waterMaterial;
        style.snowGround = L("Dirty_Snow_2", 0).texture; style.leafGround = L("Forest_Ground_38", 0).texture; // 계절 바닥(2026-10-04)
        EditorUtility.SetDirty(style);
    }

    // 기본(Built-in) 셰이더 재질은 URP에서 분홍색으로 깨진다. 텍스처·색만 URP/Lit으로 옮긴다.
    static int ConvertStandardMaterials()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit"); var count = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/ThirdParty" }))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (mat == null || !mat.shader.name.StartsWith("Standard")) continue;
            var tex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            var color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            mat.shader = lit; mat.SetTexture("_BaseMap", tex); mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat); count++;
        }
        return count;
    }

    static Material WaterMaterial()
    {
        const string path = "Assets/_Project/Materials/Water.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
        mat.SetFloat("_Surface", 1); mat.SetFloat("_Blend", 0); mat.SetFloat("_Smoothness", .85f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0); mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetColor("_BaseColor", new Color(.25f, .55f, .75f, .62f));
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
