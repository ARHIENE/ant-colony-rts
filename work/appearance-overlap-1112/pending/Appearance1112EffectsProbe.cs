using System;using System.Linq;using System.Threading.Tasks;using UnityEngine;using UnityEngine.Rendering.Universal;using AntColony.Core;using AntColony.Units;
public static class Appearance1112EffectsProbe {
 public static async Task<string> Run(){
  var c=CommanderRoster.Instance.Commanders.First(x=>x&&x.IsColonyMember);var p=c.GetComponent<MeshyNativeMotionPlayer>();p.PreviewMotion="Idle_3";p.PreviewPhase=.3f;await Task.Delay(100);
  foreach(var s in c.transform.Find("Kemono").GetComponentsInChildren<SkinnedMeshRenderer>())for(int i=0;i<s.sharedMesh.blendShapeCount;i++)s.SetBlendShapeWeight(i,0);
  var features=Resources.FindObjectsOfTypeAll<ScriptableRendererData>().SelectMany(x=>x.rendererFeatures).Where(f=>f&&f.name.Contains("AmbientOcclusion")).ToArray();var states=features.Select(f=>f.isActive).ToArray();
  try{foreach(var f in features)f.SetActive(false);await Task.Delay(150);ScreenCapture.CaptureScreenshot("work/appearance-overlap-1112/no-ssao.png");await Task.Delay(150);}
  finally{for(int i=0;i<features.Length;i++)features[i].SetActive(states[i]);}
  var skins=p.NativeModel.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();var mats=skins.Select(s=>s.sharedMaterials).ToArray();var made=new System.Collections.Generic.List<Material>();
  try{for(int i=0;i<skins.Length;i++)skins[i].sharedMaterials=mats[i].Select(m=>{var n=new Material(Shader.Find("Universal Render Pipeline/Unlit"));n.SetTexture("_BaseMap",m.GetTexture("_BaseMap"));n.SetColor("_BaseColor",m.GetColor("_BaseColor"));made.Add(n);return n;}).ToArray();await Task.Delay(150);ScreenCapture.CaptureScreenshot("work/appearance-overlap-1112/live-unlit.png");await Task.Delay(150);}
  finally{for(int i=0;i<skins.Length;i++)skins[i].sharedMaterials=mats[i];foreach(var m in made)UnityEngine.Object.Destroy(m);}
  return "Temporary renderer effect comparison; restored "+features.Length+" SSAO features and all materials";
 }
}
