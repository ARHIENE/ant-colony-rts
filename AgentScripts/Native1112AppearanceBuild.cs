using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AntColony.Units;
public static class Native1112AppearanceBuild
{
 static string Map(string n)
 {
  var basic=new Dictionary<string,string>{{"pevis","Hips"},{"spine","Spine"},{"spine_01","Spine1"},{"spine_02","Spine2"},{"neck","Neck"},{"head","Head"}};
  if(basic.TryGetValue(n,out var result))return "mixamorig:"+result;
  foreach(var side in new[]{"L","R"}){
   var prefix=side=="L"?"Left":"Right";
   foreach(var p in new[]{("shoulder","Shoulder"),("upper_arm","Arm"),("forearm","ForeArm"),("hand","Hand"),("thigh","UpLeg"),("shin","Leg"),("foot","Foot")})if(n==p.Item1+"."+side)return "mixamorig:"+prefix+p.Item2;
   foreach(var f in new[]{"index","middle","ring","pinky","thumb"})for(int j=1;j<=3;j++)if(n==(f=="thumb"?"thumb":"f_"+f)+".0"+j+"."+side)return "mixamorig:"+prefix+"Hand"+char.ToUpper(f[0])+f.Substring(1)+j;
  }
  return "mixamorig:Hips";
 }
 static Dictionary<Transform,Matrix4x4> Rest(GameObject go){var d=new Dictionary<Transform,Matrix4x4>();foreach(var s in go.GetComponentsInChildren<SkinnedMeshRenderer>())for(int i=0;i<s.bones.Length;i++)if(s.bones[i]&&!d.ContainsKey(s.bones[i]))d[s.bones[i]]=s.transform.localToWorldMatrix*s.sharedMesh.bindposes[i].inverse;return d;}
 static Matrix4x4 Fit(Vector3 a,Vector3 b,Vector3 sa,Vector3 sb,float radial)
 {
  var u=sa-a;var v=sb-b;if(u.magnitude<.0001f||v.magnitude<.0001f)return Matrix4x4.TRS(b,Quaternion.identity,Vector3.one*radial)*Matrix4x4.Translate(-a);
  float axial=v.magnitude/u.magnitude;u.Normalize();var m=Matrix4x4.Scale(Vector3.one*radial);
  for(int r=0;r<3;r++)for(int c=0;c<3;c++)m[r,c]+=(axial-radial)*u[r]*u[c];
  return Matrix4x4.TRS(b,Quaternion.FromToRotation(u,v.normalized),Vector3.one)*m*Matrix4x4.Translate(-a);
 }
 static Vector3 Transform(Vector3 v,BoneWeight w,Matrix4x4[] map,bool direction=false)
 {
  Vector3 Part(int i,float weight)=>weight==0?Vector3.zero:weight*(direction?map[i].MultiplyVector(v):map[i].MultiplyPoint3x4(v));
  return Part(w.boneIndex0,w.weight0)+Part(w.boneIndex1,w.weight1)+Part(w.boneIndex2,w.weight2)+Part(w.boneIndex3,w.weight3);
 }
 public static string Run()
 {
  if(Application.isPlaying)throw new Exception("Edit mode required");
  var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Library");var old=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Kemono/Kemono_Character"));var report=new List<string>();
  try {
   var sr=Rest(old);var skins=old.GetComponentsInChildren<SkinnedMeshRenderer>();
   var reference=skins.Where(s=>s.name.StartsWith("Part_Body_")||s.name.StartsWith("Part_Face_")||s.name.StartsWith("Part_Ears_")||s.name.StartsWith("Part_Hair_BOB_")||s.name.StartsWith("Part_Outfit_CITIZEN_")).ToArray();
   float oldHeight=MeshyNativeBounds.Rest(reference).size.y;
   for(int ri=0;ri<lib.characters.Length;ri++){
    var target=UnityEngine.Object.Instantiate(lib.characters[ri]);
    try {
     var dr=Rest(target);var dst=dr.Keys.GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());float radial=MeshyNativeBounds.Rest(target.GetComponentsInChildren<SkinnedMeshRenderer>()).size.y/oldHeight;
     string folder="Assets/Resources/MeshyNative/Appearance/Rig"+ri;Directory.CreateDirectory(folder);
     var parts=new List<MeshyNativeAppearanceSet.Part>();
     foreach(var skin in skins){
      if(!skin.name.StartsWith("Part_"))continue;
      var source=skin.sharedMesh;var names=skin.bones.Select(b=>Map(b.name)).ToArray();var matrices=new Matrix4x4[names.Length];var bind=new Matrix4x4[names.Length];
      for(int i=0;i<names.Length;i++){
       var bone=skin.bones[i];var dest=dst[names[i]];var a=(Vector3)sr[bone].GetColumn(3);var b=(Vector3)dr[dest].GetColumn(3);
       var child=bone.Cast<Transform>().FirstOrDefault(t=>sr.ContainsKey(t)&&Map(t.name)!="mixamorig:Hips"&&dst.ContainsKey(Map(t.name)));
       var sa=child?(Vector3)sr[child].GetColumn(3):a;var sb=child?(Vector3)dr[dst[Map(child.name)]].GetColumn(3):b;
       matrices[i]=target.transform.worldToLocalMatrix*Fit(a,b,sa,sb,radial)*skin.transform.localToWorldMatrix;
       bind[i]=dr[dest].inverse*target.transform.localToWorldMatrix;
      }
      var mesh=UnityEngine.Object.Instantiate(source);mesh.name=skin.name+"_Native"+ri;var weights=source.boneWeights;var verts=source.vertices;
      for(int i=0;i<verts.Length;i++)verts[i]=Transform(verts[i],weights[i],matrices);mesh.vertices=verts;mesh.bindposes=bind;
      mesh.ClearBlendShapes();var dv=new Vector3[verts.Length];var dn=new Vector3[verts.Length];var dt=new Vector3[verts.Length];
      for(int sh=0;sh<source.blendShapeCount;sh++)for(int f=0;f<source.GetBlendShapeFrameCount(sh);f++){
       source.GetBlendShapeFrameVertices(sh,f,dv,dn,dt);
       for(int v=0;v<verts.Length;v++){dv[v]=Transform(dv[v],weights[v],matrices,true);dn[v]=Vector3.zero;dt[v]=Vector3.zero;}
       mesh.AddBlendShapeFrame(source.GetBlendShapeName(sh),source.GetBlendShapeFrameWeight(sh,f),dv,dn,dt);
      }
      mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
      var path=folder+"/"+skin.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
      parts.Add(new MeshyNativeAppearanceSet.Part{name=skin.name,mesh=mesh,materials=skin.sharedMaterials,bones=names});
     }
     var kitPath="Assets/Resources/MeshyNative/Appearance/Kit"+ri+".asset";var kit=AssetDatabase.LoadAssetAtPath<MeshyNativeAppearanceSet>(kitPath);if(!kit){kit=ScriptableObject.CreateInstance<MeshyNativeAppearanceSet>();AssetDatabase.CreateAsset(kit,kitPath);}kit.parts=parts.ToArray();EditorUtility.SetDirty(kit);report.Add($"Rig{ri}: {parts.Count} original cosmetic parts; radial={radial:F4}; original clip curves untouched");
    } finally{UnityEngine.Object.DestroyImmediate(target);}
   }
   AssetDatabase.SaveAssets();File.WriteAllLines("work/animation-1112/appearance-build.txt",report);return string.Join("\n",report);
  } finally{UnityEngine.Object.DestroyImmediate(old);}
 }
}
