using System;using System.Linq;using System.IO;using System.Collections.Generic;using UnityEngine;using UnityEditor;using AntColony.Units;
public static class Native1112SourceParts {
 static string Group(Vector3 p,Bounds b){float y=(p.y-b.min.y)/b.size.y;float x=(p.x-b.center.x)/b.size.y;float z=(p.z-b.center.z)/b.size.y;if(y>.95f)return "Ears";if(y>.69f){if(y>.85f||z<-.015f||Mathf.Abs(x)>.105f&&y>.73f)return "Hair";return "Face";}if(y<.35f||Mathf.Abs(x)>.17f&&y<.60f)return "Body";return "Outfit";}
 public static string Run(){if(Application.isPlaying)throw new Exception("Edit required");var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Library");var report=new List<string>();
 for(int ri=0;ri<2;ri++){var skin=lib.characters[ri].GetComponentInChildren<SkinnedMeshRenderer>();var src=skin.sharedMesh;var v=src.vertices;var uv=src.uv;var n=src.normals;var w=src.boneWeights;var tri=src.triangles;var groups=new Dictionary<string,List<int>>();var bounds=src.bounds;
 for(int i=0;i<tri.Length;i+=3){var group=Group((v[tri[i]]+v[tri[i+1]]+v[tri[i+2]])/3,bounds);if(!groups.ContainsKey(group))groups[group]=new List<int>();groups[group].AddRange(new[]{tri[i],tri[i+1],tri[i+2]});}
 string folder="Assets/Resources/MeshyNative/SourceParts/Rig"+ri;Directory.CreateDirectory(folder);var parts=new List<MeshyNativeAppearanceSet.Part>();
 foreach(var pair in groups){var ids=pair.Value.Distinct().ToArray();var map=ids.Select((x,i)=>(x,i)).ToDictionary(p=>p.x,p=>p.i);string[] variants=pair.Key=="Hair"?new[]{"BOB","CROP","LONG","PONY","TWIN","UPDO"}:pair.Key=="Outfit"?new[]{"CITIZEN","FARMER","ARTISAN","GUARD"}:new[]{pair.Key=="Body"?"Meadow":pair.Key=="Face"?"NEUTRAL":"ANTENNA"};
 foreach(var variant in variants){string name="Part_"+pair.Key+"_"+variant+"_00";var mesh=new Mesh{name=name};mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;var vertices=ids.Select(i=>v[i]).ToArray();
 var other=groups.Where(g=>g.Key!=pair.Key).SelectMany(g=>g.Value).Select(i=>v[i]).ToHashSet();
 var seam=ids.Where(i=>other.Contains(v[i])).Select(i=>v[i]).ToArray();
 for(int j=0;j<vertices.Length;j++)if(pair.Key=="Hair"&&variant!="BOB"){
  var q=vertices[j];float y=(q.y-bounds.min.y)/bounds.size.y;float x=(q.x-bounds.center.x)/bounds.size.y;float z=(q.z-bounds.center.z)/bounds.size.y;
  float edge=seam.Length==0?1:Mathf.Clamp01(Mathf.Sqrt(seam.Min(a=>(a-q).sqrMagnitude))/.045f);
  float lower=Mathf.Clamp01((.87f-y)/.15f)*edge; bool back=z<0; float amount=bounds.size.y*lower;
  if(variant=="CROP")q.y+=amount*.045f;
  if(variant=="LONG")q.y-=amount*.12f;
  if(variant=="PONY"&&back){q.y-=amount*.19f;q.x=Mathf.Lerp(q.x,bounds.center.x,.35f*lower);q.z-=amount*.04f;}
  if(variant=="TWIN"&&Mathf.Abs(x)>.07f){q.y-=amount*.15f;q.x+=Mathf.Sign(x)*amount*.035f;}
  if(variant=="UPDO"&&back){q.y+=amount*.08f;q.z-=amount*.015f;}
  vertices[j]=q;
 }
 mesh.vertices=vertices;mesh.uv=ids.Select(i=>uv[i]).ToArray();mesh.normals=ids.Select(i=>n[i]).ToArray();mesh.boneWeights=pair.Key=="Hair"?ids.Select(i=>new BoneWeight{boneIndex0=Array.FindIndex(skin.bones,b=>b.name=="mixamorig:Head"),weight0=1}).ToArray():ids.Select(i=>w[i]).ToArray();mesh.bindposes=src.bindposes;mesh.triangles=pair.Value.Select(i=>map[i]).ToArray();
 if(pair.Key=="Face"){
  var oldFace=Resources.Load<GameObject>("Kemono/Kemono_Character").GetComponentsInChildren<SkinnedMeshRenderer>().First(a=>a.name.StartsWith("Part_Face_")).sharedMesh;
  for(int sh=0;sh<oldFace.blendShapeCount;sh++){string shape=oldFace.GetBlendShapeName(sh);var delta=new Vector3[vertices.Length];
   for(int j=0;j<vertices.Length;j++){var q=vertices[j];float y=(q.y-bounds.min.y)/bounds.size.y;float x=(q.x-bounds.center.x)/bounds.size.y;float z=(q.z-bounds.center.z)/bounds.size.y;if(z<.01f)continue;
    float eye=Mathf.Exp(-Mathf.Pow((Mathf.Abs(x)-.057f)/.04f,4)-Mathf.Pow((y-.805f)/.045f,4));float mouth=Mathf.Exp(-Mathf.Pow(x/.035f,4)-Mathf.Pow((y-.735f)/.025f,4));
    string key=shape.ToLowerInvariant();float dy=0;if(key.Contains("blink")||key.Contains("sleep")||key.Contains("laugh")||key.Contains("pain"))dy=-(y-.805f)*eye*.85f;
    if(key.Contains("smile")||key.Contains("laugh"))dy+=(Mathf.Abs(x)/.035f*.008f-.002f)*mouth;
    if(key.Contains("surprise"))dy+=(y-.805f)*eye*.15f+(y-.735f)*mouth*.5f;
    if(key.Contains("sad"))dy-=(Mathf.Abs(x)-.057f)*eye*.1f;
    if(key.Contains("determined"))dy+=(Mathf.Abs(x)-.057f)*eye*.1f;
    delta[j]=Vector3.up*(dy*bounds.size.y);
   }mesh.AddBlendShapeFrame(shape,100,delta,new Vector3[vertices.Length],new Vector3[vertices.Length]);
  }
 }
 mesh.RecalculateBounds();mesh.RecalculateTangents();var path=folder+"/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
 var matPath=folder+"/"+pair.Key+(pair.Key=="Outfit"?"_"+variant:"")+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name="Meadow_Original_"+pair.Key;mat.SetTexture("_BaseMap",lib.texture);mat.SetFloat("_Smoothness",.3f);AssetDatabase.CreateAsset(mat,matPath);}if(pair.Key=="Outfit"){mat.SetColor("_BaseColor",variant=="GUARD"?new Color(.72f,.78f,.86f):variant=="FARMER"?new Color(1f,.87f,.69f):variant=="ARTISAN"?new Color(.88f,.76f,.66f):Color.white);EditorUtility.SetDirty(mat);}parts.Add(new MeshyNativeAppearanceSet.Part{name=name,mesh=mesh,materials=new[]{mat},bones=skin.bones.Select(b=>b.name).ToArray()});}
 report.Add("Rig"+ri+" "+pair.Key+" triangles="+pair.Value.Count/3);}
 var kitPath="Assets/Resources/MeshyNative/SourceParts/Kit"+ri+".asset";var kit=AssetDatabase.LoadAssetAtPath<MeshyNativeAppearanceSet>(kitPath);if(!kit){kit=ScriptableObject.CreateInstance<MeshyNativeAppearanceSet>();AssetDatabase.CreateAsset(kit,kitPath);}kit.parts=parts.ToArray();EditorUtility.SetDirty(kit);
 }AssetDatabase.SaveAssets();File.WriteAllLines("work/animation-1112/source-parts.txt",report);return string.Join("\n",report);}
}
