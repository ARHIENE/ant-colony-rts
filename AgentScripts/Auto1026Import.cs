using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class Auto1026Import
{
    public static string Main()
    {
        if(Application.isPlaying)throw new Exception("Stop Play before replacing animation assets");
        var result=AntColony.EditorTools.KemonoImport.Run();
        var tools=Directory.GetFiles("Assets/Resources/Kemono/Handheld","*.fbx");
        foreach(var path in tools)
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace('\\','/'));
            if(model==null||model.GetComponentsInChildren<Renderer>().Length==0)throw new Exception("Missing hand model "+path);
            foreach(var r in model.GetComponentsInChildren<Renderer>())
                if(r.sharedMaterials.Any(m=>m==null||m.shader==null))throw new Exception("Missing hand material "+path);
        }
        result+="; validated handhelds "+tools.Length;
        File.WriteAllText("Temp/auto1026-import-result.txt",result);return result;
    }
}
