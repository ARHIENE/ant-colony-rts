using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AntColony.Units;
using Object=UnityEngine.Object;

// A live Unity render, sampling the production Animator and grip solver.
// No scene save or production camera changes.
public class HandGrip1106Live : EditorWindow
{
    GameObject root,model; Camera camera; Animator animator; RenderTexture target;
    string tool="Tool_Pickaxe_Stone",clip="Tool_Mine";
    double started; bool moving=true,closeup; float phase=.25f;
    static readonly string[] Tools={"Tool_Axe_Stone","Tool_Pickaxe_Stone","Tool_Shovel_Stone","Tool_Hammer_Stone","Tool_Hoe_Stone","Tool_Sickle_Stone","Weapon_SniperRifle"};
    static readonly string[] Clips={"Tool_Chop","Tool_Mine","Tool_Dig","Tool_Hammer","Tool_Dig","Tool_Harvest","Idle_A"};
    public static string Open(){foreach(var old in Resources.FindObjectsOfTypeAll<EditorWindow>())if(old.GetType().Name=="HandGrip1106Live")old.Close();var w=GetWindow<HandGrip1106Live>("Hand Grip • live Unity",true,typeof(SceneView));w.titleContent=new GUIContent("Hand Grip • live Unity");w.minSize=new Vector2(700,500);w.Build();w.Show();return "Live Unity hand grip preview opened; production Animator and grip solver; edit-mode sampling.";}
    void Build()
    {
        Cleanup();started=EditorApplication.timeSinceStartup;
        root=new GameObject("HandGrip1106 temporary preview");root.hideFlags=HideFlags.HideAndDontSave;root.transform.position=new Vector3(500,500,500);
        model=KemonoModel.Create(root.transform,1105);
        var appearance=model.GetComponent<KemonoAppearance>();appearance.SetVariant("Hair","BOB");appearance.SetVariant("Outfit","CITIZEN");
        KemonoModel.Hold(model,tool);
        animator=model.GetComponentsInChildren<Animator>().First(a=>a.runtimeAnimatorController!=null);animator.enabled=false;
        var go=new GameObject("Preview Camera");go.transform.SetParent(root.transform,false);camera=go.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.nearClipPlane=.03f;camera.farClipPlane=20;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.28f,.30f,.29f);camera.cullingMask=1<<31;
        target=new RenderTexture(1400,900,24);camera.targetTexture=target;
        var key=new GameObject("Preview Light");key.transform.SetParent(root.transform,false);key.transform.rotation=Quaternion.Euler(40,145,0);var light=key.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.cullingMask=1<<31;
        foreach(var t in root.GetComponentsInChildren<Transform>(true)){t.gameObject.layer=31;t.gameObject.hideFlags=HideFlags.HideAndDontSave;}
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;Tick();
    }
    void Tick()
    {
        if(!root||!animator)return;
        float sample=moving?(float)((EditorApplication.timeSinceStartup-started)*.55%1):phase;
        animator.Play("Base Layer."+clip,0,sample);animator.Update(0);
        model.GetComponent<MeshyEquipmentGrip>().Pose(clip=="Idle_A");
        var focus=root.transform.position+Vector3.up*.85f;
        if(closeup)focus=model.GetComponentsInChildren<Transform>().First(t=>t.name=="Socket.Tool.R").position;
        camera.orthographicSize=closeup?.19f:.88f;camera.transform.position=focus+new Vector3(1,.45f,2);camera.transform.LookAt(focus);
        camera.Render();Repaint();
    }
    void OnGUI()
    {
        EditorGUILayout.LabelField("Production character • owned Kiki hand topology • Unity Animator",EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Live edit-mode sampling. Gameplay / final visual acceptance not implied.");
        EditorGUILayout.BeginHorizontal();
        int old=Array.IndexOf(Tools,tool);int selected=EditorGUILayout.Popup(old,Tools);
        if(selected!=old){tool=Tools[selected];clip=Clips[selected];Build();}
        if(GUILayout.Button("Idle / work",GUILayout.Width(95))){clip=clip=="Idle_A"?Clips[Array.IndexOf(Tools,tool)]:"Idle_A";}
        moving=GUILayout.Toggle(moving,"Animate",GUILayout.Width(80));closeup=GUILayout.Toggle(closeup,"Close-up",GUILayout.Width(90));
        EditorGUILayout.EndHorizontal();
        if(!moving)phase=EditorGUILayout.Slider("Phase",phase,0,1);
        if(target)GUI.DrawTexture(GUILayoutUtility.GetRect(position.width,Mathf.Max(100,position.height-100)),target,ScaleMode.ScaleToFit,false);
    }
    void Cleanup(){EditorApplication.update-=Tick;if(root)Object.DestroyImmediate(root);if(target){target.Release();Object.DestroyImmediate(target);}}
    void OnDisable(){Cleanup();}
}
