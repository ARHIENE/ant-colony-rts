using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class MeshyEquipmentImport
{
    [Serializable] public class MaterialSpec { public string name, texture; }
    [Serializable] public class Item { public int id, triangles; public string name, kind, source; public MaterialSpec[] materials; }
    [Serializable] public class Manifest { public Item[] items; }
    public static string Main(int first=1,int last=28)
    {
        if(Application.isPlaying)throw new Exception("Import in edit mode");
        const string src="art/meshy-equipment-1105/export/", dst="Assets/Resources/Kemono/Handheld/", mats="Assets/Resources/MeadowEquipment/";
        Directory.CreateDirectory(dst);Directory.CreateDirectory(mats);Directory.CreateDirectory("art/meshy-equipment-1105/backups");
        var manifest=new Manifest { items=Newtonsoft.Json.JsonConvert.DeserializeObject<Item[]>(File.ReadAllText(src+"manifest.json")) };
        manifest.items=manifest.items.Where(item=>item.id>=first&&item.id<=last).ToArray();
        foreach(var item in manifest.items)
        {
            var path=dst+item.name+".fbx";
            var backup="art/meshy-equipment-1105/backups/"+item.name+".fbx";
            if(File.Exists(path)&&!File.Exists(backup))File.Copy(path,backup);
            File.Copy(src+item.name+".fbx",path+".incoming",true);
            if(File.Exists(path))File.Replace(path+".incoming",path,null);else File.Move(path+".incoming",path);
            foreach(var mat in item.materials)if(!string.IsNullOrEmpty(mat.texture))File.Copy(src+mat.texture,mats+mat.texture,true);
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(var item in manifest.items)
        {
            var path=dst+item.name+".fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.materialLocation=ModelImporterMaterialLocation.InPrefab;
            importer.importAnimation=false;importer.SaveAndReimport();
            importer=(ModelImporter)AssetImporter.GetAtPath(path);
            foreach(var spec in item.materials)
            {
                var materialPath=mats+spec.name+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,materialPath);}
                material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(mats+spec.texture));
                material.SetColor("_BaseColor",Color.white);material.SetFloat("_Smoothness",.08f);material.SetFloat("_Metallic",0);
                material.SetFloat("_SpecularHighlights",0);material.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                material.SetFloat("_Cull",0);EditorUtility.SetDirty(material);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),spec.name),material);
            }
            EditorUtility.SetDirty(importer);AssetDatabase.WriteImportSettingsIfDirty(path);importer.SaveAndReimport();
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(model.GetComponentsInChildren<Renderer>().Any(r=>r.sharedMaterials.Any(m=>m==null||m.GetTexture("_BaseMap")==null)))throw new Exception("Untextured "+item.name);
        }
        AssetDatabase.SaveAssets();return "Imported "+manifest.items.Length+" textured Meshy equipment models; previous files backed up, GUIDs preserved.";
    }
}
