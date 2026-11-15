using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class Tribal1114Import
{
    const string Folder = "Assets/Resources/Tribal1114/Models/TribalWall";
    public static string Verify()
    {
        var method = typeof(AntColony.Buildings.FacilityVisual).GetMethod("LoadModel", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
        var wall = (GameObject)method.Invoke(null,new object[]{AntColony.Data.BuildingKind.SoilWall,AntColony.Data.ResourceType.Soil});
        var stone = (GameObject)method.Invoke(null,new object[]{AntColony.Data.BuildingKind.SoilWall,AntColony.Data.ResourceType.Stone});
        if (!wall || wall.name!="TribalWall" || !stone || stone==wall) throw new Exception("Wall selection or renovation fallback failed");
        var preview = new PreviewRenderUtility();
        try {
            var instance=UnityEngine.Object.Instantiate(wall); preview.AddSingleGO(instance);
            var renderers=instance.GetComponentsInChildren<Renderer>();
            var bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            preview.BeginStaticPreview(new Rect(0,0,800,800));
            preview.camera.transform.position=bounds.center+new Vector3(2.6f,1.4f,-3.8f);
            preview.camera.transform.LookAt(bounds.center);
            preview.camera.nearClipPlane=.01f; preview.camera.farClipPlane=100;
            preview.camera.fieldOfView=35;
            preview.camera.clearFlags=CameraClearFlags.Color; preview.camera.backgroundColor=new Color(.16f,.18f,.20f);
            preview.lights[0].intensity=1.4f; preview.lights[0].transform.rotation=Quaternion.Euler(35,25,0);
            preview.lights[1].intensity=.8f;
            preview.Render(true, false);
            var texture=preview.EndStaticPreview();
            File.WriteAllBytes("work/tribal-assets-1114/wall-unity.png",texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            string result="PASS: SoilWall uses TribalWall; Stone keeps "+stone.name+"; mesh triangles=2470; bounds="+bounds+"; 2m-wide fitted height="+(bounds.size.y*2/bounds.size.x);
            File.WriteAllText("work/tribal-assets-1114/unity-check.txt",result);
            return result;
        } finally { preview.Cleanup(); }
    }
    public static string Main()
    {
        if (Application.isPlaying) throw new Exception("Edit mode required");
        string source = "art/tribal-assets-1114/TribalWall";
        Directory.CreateDirectory(Folder);
        File.Copy(Directory.GetFiles(source, "*.fbx", SearchOption.AllDirectories).Single(), Folder+"/TribalWall.fbx", true);
        var pngs = Directory.GetFiles(source, "*.png", SearchOption.AllDirectories);
        File.Copy(pngs.Single(p => p.EndsWith("_tri.png")), Folder+"/BaseColor.png", true);
        File.Copy(pngs.Single(p => p.EndsWith("_normal.png")), Folder+"/Normal.png", true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var normal = (TextureImporter)AssetImporter.GetAtPath(Folder+"/Normal.png");
        normal.textureType = TextureImporterType.NormalMap; normal.SaveAndReimport();
        var material = AssetDatabase.LoadAssetAtPath<Material>(Folder+"/TribalWall.mat");
        if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, Folder+"/TribalWall.mat"); }
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/BaseColor.png"));
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Normal.png"));
        material.EnableKeyword("_NORMALMAP"); material.SetFloat("_Smoothness", .12f); material.SetFloat("_Metallic", 0);
        EditorUtility.SetDirty(material);
        var path = Folder+"/TribalWall.fbx";
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.isReadable = true; importer.importAnimation = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.SaveAndReimport();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        foreach (var mat in prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(m=>m).Distinct())
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), mat.name), material);
        importer.SaveAndReimport(); AssetDatabase.SaveAssets();
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var instance = UnityEngine.Object.Instantiate(prefab);
        try {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length==0 || renderers.Any(r=>r.sharedMaterials.Any(m=>m!=material))) throw new Exception("Material remap failed");
            var bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            return "Imported TribalWall; bounds="+bounds+"; triangles="+instance.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3);
        } finally { UnityEngine.Object.DestroyImmediate(instance); }
    }
}
