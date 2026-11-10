using System.Linq;
using UnityEngine;
using UnityEditor;
public static class Animation1110Weights {
 public static string Main(){var lines=new System.Collections.Generic.List<string>();foreach(var path in new[]{"Assets/Resources/Kemono/Kemono_Character.fbx","Assets/_Project/Art/MeshyAnimation1106/Rerig_All.fbx"}){var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);lines.Add(path);foreach(var s in go.GetComponentsInChildren<SkinnedMeshRenderer>()){if(path.Contains("Kemono_Character")&&!s.name.StartsWith("Part_Body"))continue;var w=s.sharedMesh.boneWeights;for(int i=0;i<s.bones.Length;i++){var n=s.bones[i].name;if(!n.Contains("hand")&&!n.Contains("Hand")&&!n.StartsWith("f_")&&!n.StartsWith("thumb"))continue;lines.Add(n+" vertices="+w.Count(v=>v.boneIndex0==i&&v.weight0>.01||v.boneIndex1==i&&v.weight1>.01||v.boneIndex2==i&&v.weight2>.01||v.boneIndex3==i&&v.weight3>.01));}}}return string.Join("\n",lines);}
}
