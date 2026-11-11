using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using AntColony.Units;
using Object=UnityEngine.Object;

public class Meshy1111DirectCompare:EditorWindow
{
    sealed class View
    {
        public GameObject root,model; public Animator animator; public AnimationClip clip;
        public PlayableGraph graph; public AnimationClipPlayable playable;
        public SkinnedMeshRenderer[] skins;public Mesh[] meshes;public MeshRenderer[] visible;
        public Camera camera;public RenderTexture texture;public Material nativeMaterial;
        public void Sample(float phase)
        {
            playable.SetTime(clip.length*phase);graph.Evaluate(0);
            for(int i=0;i<skins.Length;i++){skins[i].BakeMesh(meshes[i]);meshes[i].RecalculateBounds();}
            camera.Render();
        }
        public void Dispose(){if(graph.IsValid())graph.Destroy();if(root)Object.DestroyImmediate(root);if(texture){texture.Release();Object.DestroyImmediate(texture);}if(nativeMaterial)Object.DestroyImmediate(nativeMaterial);if(meshes!=null)foreach(var mesh in meshes)Object.DestroyImmediate(mesh);}
    }
    View direct,converted; bool running=true;float phase;double start;
    public static string Open()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play before comparison.");
        foreach(var w in Resources.FindObjectsOfTypeAll<EditorWindow>().Where(w=>w.GetType().Name=="MeshyAnimation1106Live"||w.GetType().Name==nameof(Meshy1111DirectCompare)).ToArray())w.Close();
        var window=GetWindow<Meshy1111DirectCompare>("Meshy original comparison",true,typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        foreach(var orphan in Resources.FindObjectsOfTypeAll<GameObject>().Where(g=>g.name=="Meshy original comparison"||g.name=="Game rig comparison").ToArray())Object.DestroyImmediate(orphan);window.direct=Build(true);window.converted=Build(false);window.start=EditorApplication.timeSinceStartup;
        EditorApplication.update+=window.Tick;window.Show();return "Left: original Meshy mesh, rig, avatar and FBX clip. Right: modular game model with converted clip and ALL grip IK disabled. No new Meshy generation or credits.";
    }
    static View Build(bool native)
    {
        var v=new View();v.root=new GameObject(native?"Meshy original comparison":"Game rig comparison");v.root.transform.position=new Vector3(native?800:900,800,800);
        const string path="Assets/_Project/Art/MeshyAnimation1106/Heavy_Hammer_Swing.fbx";
        v.model=native?Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),v.root.transform):KemonoModel.Create(v.root.transform,1111);
        if(native){v.animator=v.model.GetComponent<Animator>();if(!v.animator)v.animator=v.model.AddComponent<Animator>();v.animator.avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();v.clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));}
        else {v.animator=RigAnimator.Find(v.model);v.clip=Resources.Load<AnimationClip>("Kemono/MeshyMotions/Heavy_Hammer_Swing");v.model.GetComponent<MeshyEquipmentGrip>().enabled=false;}
        v.animator.enabled=true;v.animator.runtimeAnimatorController=null;v.animator.applyRootMotion=false;v.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        v.graph=PlayableGraph.Create(native?"Original FBX directly":"Converted clip without IK");v.graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        v.playable=AnimationClipPlayable.Create(v.graph,v.clip);AnimationPlayableOutput.Create(v.graph,"Motion",v.animator).SetSourcePlayable(v.playable);v.graph.Play();v.graph.Evaluate(0);
        v.skins=v.model.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();
        if(native){v.nativeMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));v.nativeMaterial.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/MeshyAnimation1106/Meshy_AI_Meadow_Sprout_20261106_animated_mixamo_All_animation.png");}
        v.meshes=v.skins.Select(s=>new Mesh()).ToArray();
        v.visible=v.skins.Select((skin,i)=>{var go=new GameObject("Preview skin");go.transform.SetParent(skin.transform,false);go.transform.localScale=new Vector3(1/skin.transform.lossyScale.x,1/skin.transform.lossyScale.y,1/skin.transform.lossyScale.z);go.AddComponent<MeshFilter>().sharedMesh=v.meshes[i];var r=go.AddComponent<MeshRenderer>();r.sharedMaterials=native?new[]{v.nativeMaterial}:skin.sharedMaterials;skin.enabled=false;skin.BakeMesh(v.meshes[i]);v.meshes[i].RecalculateBounds();return r;}).ToArray();
        var bounds=v.visible.Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
        var camObject=new GameObject("Comparison camera");camObject.transform.SetParent(v.root.transform,false);v.camera=camObject.AddComponent<Camera>();v.camera.enabled=false;v.camera.orthographic=true;v.camera.orthographicSize=bounds.size.y*.7f;v.camera.nearClipPlane=.01f;v.camera.farClipPlane=Mathf.Max(20,bounds.size.y*5);v.camera.cullingMask=1<<31;v.camera.clearFlags=CameraClearFlags.SolidColor;v.camera.backgroundColor=new Color(.18f,.22f,.23f);v.camera.transform.position=bounds.center+new Vector3(-.4f,.12f,1)*bounds.size.y*2;v.camera.transform.LookAt(bounds.center);
        v.texture=new RenderTexture(750,850,24);v.camera.targetTexture=v.texture;
        foreach(var t in v.root.GetComponentsInChildren<Transform>(true)){t.gameObject.layer=31;t.gameObject.hideFlags=HideFlags.HideAndDontSave;}
        return v;
    }
    void Tick(){if(direct==null)return;if(running)phase=(float)((EditorApplication.timeSinceStartup-start)/direct.clip.length%1);direct.Sample(phase);converted.Sample(phase);Repaint();}
    void OnGUI()
    {
        GUILayout.Label("Original Meshy FBX  |  Game rig + converted clip, grip correction OFF",EditorStyles.boldLabel);
        running=GUILayout.Toggle(running,"Play original timing (1x)");if(!running)phase=EditorGUILayout.Slider("Same phase",phase,0,1);
        GUILayout.BeginHorizontal();GUILayout.Label("LEFT: Meshy mesh + its own rig");GUILayout.Label("RIGHT: modular game model / no arm IK");GUILayout.EndHorizontal();
        var area=GUILayoutUtility.GetRect(position.width,Mathf.Max(100,position.height-85));var left=new Rect(area.x,area.y,area.width/2,area.height);var right=new Rect(area.x+area.width/2,area.y,area.width/2,area.height);
        if(direct!=null)GUI.DrawTexture(left,direct.texture,ScaleMode.ScaleToFit);if(converted!=null)GUI.DrawTexture(right,converted.texture,ScaleMode.ScaleToFit);
    }
    void OnDisable(){EditorApplication.update-=Tick;direct?.Dispose();converted?.Dispose();direct=converted=null;}
}
