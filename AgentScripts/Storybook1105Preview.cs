using System;
using System.IO;
using System.Linq;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;

// Temporary camera stage: actual imported meshes, URP materials and production animation.
public static class Storybook1105Preview
{
    public static string Main()
    {
        var root = new GameObject("Temporary storybook camera stage"); root.transform.position = new Vector3(500,500,500);
        var rt = new RenderTexture(1500,900,24); var previous = RenderTexture.active;
        var oldAmbient = RenderSettings.ambientLight; var oldMode = RenderSettings.ambientMode;
        var floorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")); floorMaterial.color = new Color(.76f,.73f,.65f);
        try
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.transform.SetParent(root.transform,false);ground.transform.localScale=Vector3.one*3;
            ground.GetComponent<Renderer>().sharedMaterial=floorMaterial;
            var lightObject = new GameObject("Stage key");lightObject.transform.SetParent(root.transform,false);lightObject.transform.rotation=Quaternion.Euler(45,145,0);
            var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.cullingMask=1<<31;light.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.52f,.54f,.56f);
            var cameraObject = new GameObject("Stage camera");cameraObject.transform.SetParent(root.transform,false);
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.76f,.73f,.65f);
            camera.cullingMask=1<<31;camera.orthographic=true;camera.orthographicSize=1.65f;camera.nearClipPlane=.1f;camera.farClipPlane=30;camera.targetTexture=rt;
            var models = new System.Collections.Generic.List<GameObject>();
            for(var i=0;i<5;i++)
            {
                var holder=new GameObject("Colour "+i);holder.transform.SetParent(root.transform,false);holder.transform.localPosition=new Vector3((i-2)*1.1f,0,0);
                var model=KemonoModel.Create(holder.transform,1105+i);models.Add(holder);
                model.GetComponent<KemonoAppearance>().SetPaletteColor(StartingCandidate.AppearanceColor(i));
                var animator=model.GetComponentsInChildren<Animator>().First(a=>a.runtimeAnimatorController!=null);
                animator.Play("Base Layer.Idle_A",0,.2f);animator.Update(.01f);animator.enabled=false;
            }
            camera.transform.localPosition=new Vector3(0,2.5f,7);camera.transform.LookAt(root.transform.position+Vector3.up*.8f);
            Layer(root);Write(camera,rt,"Unity_Characters.png");
            foreach(var model in models)Object.DestroyImmediate(model);
            var names=new[]{"CC_Hut","CC_House","CC_Storage","CC_Watchtower"};
            for(var i=0;i<4;i++)
            {
                var model=Object.Instantiate(Resources.Load<GameObject>("Facilities/"+names[i]),root.transform,false);
                model.transform.localPosition=new Vector3((i%2)*4-2,0,(i/2)*4-2);model.transform.localScale*=.7f;
            }
            camera.orthographicSize=5.8f;camera.transform.localPosition=new Vector3(8,10,12);camera.transform.LookAt(root.transform.position+Vector3.up);
            Layer(root);Write(camera,rt,"Unity_Buildings.png");
            return "Rendered actual Unity models: art/storybook-1105/Unity_Characters.png and Unity_Buildings.png";
        }
        finally
        {
            RenderTexture.active=previous;RenderSettings.ambientLight=oldAmbient;RenderSettings.ambientMode=oldMode;
            Object.DestroyImmediate(root);Object.DestroyImmediate(floorMaterial);rt.Release();Object.DestroyImmediate(rt);
        }
    }
    static void Layer(GameObject root){foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;}
    static void Write(Camera camera,RenderTexture rt,string name)
    {
        camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
        File.WriteAllBytes("art/storybook-1105/"+name,image.EncodeToPNG());Object.DestroyImmediate(image);
    }
}
