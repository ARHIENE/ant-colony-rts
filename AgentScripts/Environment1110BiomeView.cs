using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AntColony.Map;
using AntColony.Core;
using Object=UnityEngine.Object;

public static class Environment1110BiomeView
{
    public static string Main()
    {
        var root=new GameObject("Temporary biome shader review");root.transform.position=new Vector3(1200,1200,1200);
        var owned=new List<Object>();var previous=RenderTexture.active;var asyncShaders=ShaderUtil.allowAsyncCompilation;
        var output=new RenderTexture(1800,1100,24);var tileTarget=RenderTexture.GetTemporary(512,512);
        try
        {
            ShaderUtil.allowAsyncCompilation=false;
            var cameraObject=new GameObject("Biome review camera");cameraObject.transform.SetParent(root.transform,false);
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=12;
            camera.cullingMask=1<<31;camera.targetTexture=output;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.21f,.23f);
            camera.transform.localPosition=new Vector3(0,32,18);camera.transform.LookAt(root.transform.position);
            var biomes=new[]{MapBiome.Forest,MapBiome.Garden,MapBiome.Waterside,MapBiome.Desert,MapBiome.Cave,MapBiome.City};
            for(int i=0;i<biomes.Length;i++)
            {
                var style=BiomeMapStyle.For(biomes[i]);var array=new Texture2DArray(512,512,style.layers.Count,TextureFormat.RGBA32,true){wrapMode=TextureWrapMode.Repeat};owned.Add(array);
                var read=new Texture2D(512,512,TextureFormat.RGBA32,false);owned.Add(read);
                for(int j=0;j<style.layers.Count;j++){Graphics.Blit(style.layers[j].texture,tileTarget);RenderTexture.active=tileTarget;read.ReadPixels(new Rect(0,0,512,512),0,0);array.SetPixels(read.GetPixels(),j);}array.Apply();
                var mat=new Material(Shader.Find("AntColony/TerrainBlend"));owned.Add(mat);mat.SetTexture("terrainTextures",array);mat.SetInt("numTextures",style.layers.Count);
                var heights=new float[32];for(int j=0;j<style.layers.Count;j++)heights[j]=style.layers[j].startHeight;mat.SetFloatArray("terrainHeights",heights);
                mat.SetFloat("minTerrainHeight",1197);mat.SetFloat("maxTerrainHeight",1206);mat.SetFloat("terraceStep",0);mat.SetFloat("_textureScale",6);mat.SetColor("_Tint",BiomeRules.GroundTint(biomes[i]));
                var tile=GameObject.CreatePrimitive(PrimitiveType.Plane);tile.transform.SetParent(root.transform,false);tile.transform.localPosition=new Vector3((i%3-1)*11,0,(i/3-.5f)*12);tile.layer=31;tile.GetComponent<Renderer>().sharedMaterial=mat;
                var labelObject=new GameObject(biomes[i].ToString());labelObject.transform.SetParent(root.transform,false);labelObject.transform.localPosition=tile.transform.localPosition+new Vector3(0,.1f,5.6f);labelObject.layer=31;labelObject.transform.rotation=camera.transform.rotation;
                var label=labelObject.AddComponent<TextMesh>();label.text=biomes[i].ToString();label.fontSize=48;label.characterSize=.2f;label.anchor=TextAnchor.MiddleCenter;
            }
            camera.Render();RenderTexture.active=output;var image=new Texture2D(output.width,output.height,TextureFormat.RGB24,false);owned.Add(image);
            image.ReadPixels(new Rect(0,0,output.width,output.height),0,0);image.Apply();File.WriteAllBytes("art/environment-1110/Unity_Biomes.png",image.EncodeToPNG());
            return "Rendered all 6 biome styles with production TerrainBlend shader and texture array path.";
        }
        finally{RenderTexture.active=previous;ShaderUtil.allowAsyncCompilation=asyncShaders;Object.DestroyImmediate(root);foreach(var item in owned)Object.DestroyImmediate(item);output.Release();Object.DestroyImmediate(output);RenderTexture.ReleaseTemporary(tileTarget);}
    }
}
