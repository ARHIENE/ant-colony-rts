using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Object=UnityEngine.Object;
public class MeshyAnimation1106Live:EditorWindow
{
 const string Dir="Assets/_Project/Art/MeshyAnimation1106/";
 GameObject root,model; Camera cam; RenderTexture rt; Material mat; AnimationClip clip;
 PlayableGraph graph; AnimationClipPlayable playable;
 SkinnedMeshRenderer sourceSkin; Mesh bakedMesh; MeshRenderer visibleSkin;
 bool running=true; float phase; double start; int selection=7; string[] options=new[]{"REJECTED - Meshy chopping","Meshy running","Meshy walking","Preset - Heavy Hammer Swing","Preset - Archery Shot 3","Preset - Run 02","Preset - Run 03","Rerig - Running"}.Concat(Directory.GetFiles(Dir+"Life/", "*.fbx").OrderBy(x=>x).Select(x=>"Life - "+Path.GetFileNameWithoutExtension(x))).ToArray();
 public static string Open(){foreach(var old in Resources.FindObjectsOfTypeAll<EditorWindow>())if(old.GetType().Name=="MeshyAnimation1106Live")old.Close(); var w=GetWindow<MeshyAnimation1106Live>("Meshy validation",true,typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));w.titleContent=new GUIContent("Meshy animation validation");w.minSize=new Vector2(900,650);w.Build();w.Show();return "Unity live preview opened. Imported Meshy clip; no production character replacement.";}
 void Build(){Cleanup();start=EditorApplication.timeSinceStartup; root=new GameObject("Meshy validation only");root.transform.position=new Vector3(800,800,800);
 string file=selection>=8?"Life/"+Path.GetFileName(Directory.GetFiles(Dir+"Life/", "*.fbx").OrderBy(x=>x).ElementAt(selection-8)):selection==7?"Rerig_All.fbx":selection==6?"Run_03.fbx":selection==5?"Run_02.fbx":selection==4?"Archery_Shot_3.fbx":selection==3?"Heavy_Hammer_Swing.fbx":selection==0?"Tool_Chop_SharedRig.fbx":"Meshy_AI_Meadow_Sprout_20261106_animated_mixamo_All_animation.fbx";
 AssetDatabase.ImportAsset(Dir+file,ImportAssetOptions.ForceSynchronousImport); var importer=(ModelImporter)AssetImporter.GetAtPath(Dir+file); if(importer.animationType!=ModelImporterAnimationType.Generic||importer.animationCompression!=ModelImporterAnimationCompression.Off){importer.animationType=ModelImporterAnimationType.Generic;importer.animationCompression=ModelImporterAnimationCompression.Off;importer.resampleCurves=true;importer.SaveAndReimport();}
 model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+file),root.transform);model.transform.localPosition=Vector3.zero;
 foreach(var a in model.GetComponentsInChildren<Animator>())a.enabled=false;
 var clips=AssetDatabase.LoadAllAssetsAtPath(Dir+file).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();clip=selection==7?clips.First(c=>c.name=="Running"):selection==0||selection>=3?clips[0]:clips.First(c=>c.name==(selection==1?"Running":"Walking"));
 var animator=model.GetComponent<Animator>();if(!animator)animator=model.AddComponent<Animator>();animator.enabled=true;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
 graph=PlayableGraph.Create("Meshy clip verification");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);playable=AnimationClipPlayable.Create(graph,clip);var output=AnimationPlayableOutput.Create(graph,"Imported motion",animator);output.SetSourcePlayable(playable);graph.Play();
 mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"Meshy_AI_Meadow_Sprout_20261106_animated_mixamo_All_animation.png");
 foreach(var r in model.GetComponentsInChildren<Renderer>()){r.sharedMaterial=mat;if(r is SkinnedMeshRenderer skin){skin.updateWhenOffscreen=true;skin.forceMatrixRecalculationPerRender=true;}}
 var bounds=model.GetComponentsInChildren<Renderer>()[0].bounds;model.transform.localScale*=1.6f/bounds.size.y;
 if(selection==3){clip.SampleAnimation(model,clip.length*.2f);var bones=model.GetComponentsInChildren<Transform>();var hand=bones.First(t=>t.name=="mixamorig:RightHand");var left=bones.First(t=>t.name=="mixamorig:LeftHand");var index=bones.First(t=>t.name=="mixamorig:RightHandMiddle1");var leftIndex=bones.First(t=>t.name=="mixamorig:LeftHandMiddle1");var palm=(hand.position+index.position)*.5f;var other=(left.position+leftIndex.position)*.5f;var tool=Object.Instantiate(Resources.Load<GameObject>("Kemono/Handheld/Tool_Pickaxe_Stone"));tool.transform.position=palm;tool.transform.rotation=Quaternion.FromToRotation(Vector3.up,(other-palm).normalized);tool.transform.SetParent(hand,true);}
 sourceSkin=model.GetComponentInChildren<SkinnedMeshRenderer>();bakedMesh=new Mesh();var skinGo=new GameObject("Editor CPU skin refresh");skinGo.transform.SetParent(sourceSkin.transform,false);skinGo.AddComponent<MeshFilter>().sharedMesh=bakedMesh;visibleSkin=skinGo.AddComponent<MeshRenderer>();visibleSkin.sharedMaterial=mat;sourceSkin.enabled=false;
 var co=new GameObject("Validation camera");co.transform.SetParent(root.transform,false);cam=co.AddComponent<Camera>();cam.enabled=false;cam.orthographic=true;cam.orthographicSize=.85f;cam.nearClipPlane=.01f;cam.farClipPlane=20;cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.2f,.24f,.23f);rt=new RenderTexture(1400,950,24);cam.targetTexture=rt;
 foreach(var t in root.GetComponentsInChildren<Transform>(true)){t.gameObject.layer=31;t.gameObject.hideFlags=HideFlags.HideAndDontSave;}
 EditorApplication.update+=Tick;Tick();}
 void Tick(){if(!root||!clip)return;phase=running?(float)((EditorApplication.timeSinceStartup-start)/clip.length%1):phase;playable.SetTime(phase*clip.length);graph.Evaluate(0);sourceSkin.BakeMesh(bakedMesh);var center=visibleSkin.bounds.center;cam.transform.position=center+new Vector3(-1,.25f,3);cam.transform.LookAt(center);cam.Render();Repaint();}
 void OnGUI(){EditorGUILayout.LabelField("Imported Meshy animation — isolated Unity validation",EditorStyles.boldLabel);EditorGUILayout.BeginHorizontal();int value=EditorGUILayout.Popup(selection,options);if(value!=selection){selection=value;Build();}running=GUILayout.Toggle(running,"Play",GUILayout.Width(70));if(GUILayout.Button("Capture",GUILayout.Width(90)))Capture();EditorGUILayout.EndHorizontal();if(!running)phase=EditorGUILayout.Slider("Phase",phase,0,1);EditorGUILayout.LabelField(selection==3?"Fixed right-hand tool mount; left-hand contact under validation.":"Original imported clip at 1x speed | "+(clip?clip.name+" | "+clip.length.ToString("F3")+" s":""));if(rt)GUI.DrawTexture(GUILayoutUtility.GetRect(position.width,Mathf.Max(100,position.height-100)),rt,ScaleMode.ScaleToFit,false);}
 void Capture(){var prev=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes("art/meshy-animation-1106/unity-"+selection+".png",tex.EncodeToPNG());Object.DestroyImmediate(tex);RenderTexture.active=prev;}
 public static string CaptureCurrent(){var w=Resources.FindObjectsOfTypeAll<MeshyAnimation1106Live>().First();w.Capture();return "art/meshy-animation-1106/unity-"+w.selection+".png";}
 void Cleanup(){EditorApplication.update-=Tick;if(graph.IsValid())graph.Destroy();if(root)Object.DestroyImmediate(root);if(rt){rt.Release();Object.DestroyImmediate(rt);}if(mat)Object.DestroyImmediate(mat);if(bakedMesh)Object.DestroyImmediate(bakedMesh);}
 void OnDisable(){Cleanup();}
}












