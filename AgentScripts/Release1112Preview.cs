using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AntColony.World;
using AntColony.Units;
using Object = UnityEngine.Object;

public static class Release1112Preview
{
    public static string Main()
    {
        var root = new GameObject("Environment asset review (temporary)");
        root.transform.position = new Vector3(700,700,700);
        var rt = new RenderTexture(1800,1100,24);
        var previous = RenderTexture.active;
        var oldAmbient = RenderSettings.ambientLight;
        var oldMode = RenderSettings.ambientMode;
        var asyncShaders = ShaderUtil.allowAsyncCompilation;
        var materials = new List<Material>();
        try
        {
            ShaderUtil.allowAsyncCompilation = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.58f,.60f,.63f);
            var lightObject = new GameObject("Review light"); lightObject.transform.SetParent(root.transform,false);
            lightObject.transform.rotation = Quaternion.Euler(50,-35,0);
            var light = lightObject.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.1f; light.cullingMask=1<<31;
            var cameraObject = new GameObject("Review camera"); cameraObject.transform.SetParent(root.transform,false);
            var camera = cameraObject.AddComponent<Camera>(); camera.enabled=false; camera.orthographic=true; camera.orthographicSize=8;
            camera.cullingMask=1<<31; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.18f,.21f,.23f);
            camera.nearClipPlane=.1f; camera.farClipPlane=80; camera.targetTexture=rt;
            camera.transform.localPosition=new Vector3(0,18,25); camera.transform.LookAt(root.transform.position+Vector3.up);
            var names = new[]{"Carpentry","Stockpile","ResearchDesk","Kitchen","Workshop","Campfire","SpikeTrap","Smelter"};
            for (var i=0;i<names.Length;i++)
            {
                var p = new Vector3((i%5-2)*4,0,i<5?-4:3);
                var model = ProductionProp.Attach(root.transform,"Release1112/Models/"+names[i]+"/"+names[i],i<5?2.8f:2.3f,root.transform.position+p);
                if(model==null) throw new Exception("Missing review model "+names[i]);
                var labelObject=new GameObject(names[i]); labelObject.transform.SetParent(root.transform,false); labelObject.transform.localPosition=p+new Vector3(0,.08f,1.8f);
                var label=labelObject.AddComponent<TextMesh>(); label.text=names[i]; label.fontSize=48; label.characterSize=.11f; label.anchor=TextAnchor.MiddleCenter; label.color=Color.white;
                labelObject.transform.rotation=camera.transform.rotation;
            }
            var holder=new GameObject("Existing anime character scale reference");holder.transform.SetParent(root.transform,false); holder.transform.localPosition=new Vector3(0,0,0);
            var character=KemonoModel.Create(holder.transform,1110);
            var animator=character.GetComponentsInChildren<Animator>().FirstOrDefault(a=>a.runtimeAnimatorController!=null);
            if(animator!=null){animator.Play("Base Layer.Idle_A",0,.2f);animator.Update(.01f);animator.enabled=false;}
            foreach(var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=31;
            Write(camera,rt,"Unity_Models.png");
            foreach(var child in root.transform.Cast<Transform>().Where(t=>t!=cameraObject.transform && t!=lightObject.transform).ToArray()) Object.DestroyImmediate(child.gameObject);
            return "Rendered eight actual URP release models.";
        }
        finally
        {
            RenderTexture.active=previous;RenderSettings.ambientLight=oldAmbient;RenderSettings.ambientMode=oldMode;ShaderUtil.allowAsyncCompilation=asyncShaders;
            Object.DestroyImmediate(root);foreach(var m in materials)Object.DestroyImmediate(m);rt.Release();Object.DestroyImmediate(rt);
        }
    }
    static void Write(Camera camera,RenderTexture rt,string file)
    {
        camera.Render();RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes("art/release-assets-1112/"+file,image.EncodeToPNG());Object.DestroyImmediate(image);
    }
}
