using System;
using System.IO;
using System.Linq;
using UnityEngine;
using AntColony.World;
using Object=UnityEngine.Object;

// Inspect the already-running scene with a temporary camera; never move its camera or restart Play.
public static class Environment1110LiveView
{
    public static string Main()
    {
        if(!Application.isPlaying || WorldMapManager.Instance==null) return "No running game; no state changed.";
        var source=Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();
        if(source==null)return "No game camera; no state changed.";
        var go=new GameObject("Temporary environment review camera");
        var rt=new RenderTexture(1800,1100,24);var previous=RenderTexture.active;
        try
        {
            var camera=go.AddComponent<Camera>();camera.CopyFrom(source.GetComponent<Camera>());camera.enabled=false;camera.targetTexture=rt;
            camera.orthographic=true;camera.orthographicSize=18;
            var home=WorldMapManager.Instance.HomePosition;
            camera.transform.position=home+new Vector3(18,25,22);camera.transform.LookAt(home);
            camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
            File.WriteAllBytes("art/environment-1110/Unity_LiveWorld.png",image.EncodeToPNG());Object.DestroyImmediate(image);
            var rows=Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude)
                .Select(n=>new{n,mesh=n.transform.Find("ResourceModel")?.GetComponentInChildren<MeshFilter>()?.sharedMesh})
                .Where(x=>x.mesh!=null&&UnityEditor.AssetDatabase.GetAssetPath(x.mesh).Contains("Environment1110"))
                .GroupBy(x=>x.n.ResourceType).Select(g=>g.Key+"="+g.Count());
            var report="Live biome="+AntColony.Core.GameSession.Instance.Options.biome+"; new resource models: "+string.Join(", ",rows)+". Captured without changing game camera, time, saves or Play state.";
            File.WriteAllText("work/environment-1110/live-validation.txt",report);return report;
        }
        finally{RenderTexture.active=previous;Object.DestroyImmediate(go);rt.Release();Object.DestroyImmediate(rt);}
    }
}
