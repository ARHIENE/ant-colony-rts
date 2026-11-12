using System;using System.IO;using System.Linq;using System.Threading.Tasks;using UnityEngine;using UnityEditor;using AntColony.Units;
public static class EquipmentFlipMedia1112{
 public static Task<string> Chop()=>Run("chopping");
 public static Task<string> Dig()=>Run("digging");
 public static async Task<string> Run(string label="mining"){
 var c=AntColony.Core.CommanderRoster.Instance.Commanders.First(x=>x&&x.IsColonyMember);var p=c.GetComponent<MeshyNativeMotionPlayer>();p.PreviewMotion=null;p.PreviewPhase=-1;var v=c.GetComponent<AntVisual>();
 var cc=UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();if(cc)cc.enabled=false;var cam=Camera.main;var model=c.transform.Find("Kemono");var focus=c.Position+model.localPosition+Vector3.up*.85f;cam.orthographicSize=1.35f;cam.nearClipPlane=.02f;cam.transform.position=focus+model.right*3+model.forward*1.2f+Vector3.up*.3f;cam.transform.LookAt(focus);
 var path="work/equipment-flip-1112/"+label;Directory.CreateDirectory(path);var rt=new RenderTexture(640,640,24);var tex=new Texture2D(640,640,TextureFormat.RGB24,false);var old=cam.targetTexture;var active=RenderTexture.active;
 AntColony.UI.SimulationPause.UserSpeed(.25f);EditorApplication.isPaused=false;string action=v.MotionName;int captured=0;int first=Time.frameCount;float armDelta=0;Quaternion previous=Quaternion.identity;
 try{for(int i=0;i<40;i++){await Task.Delay(80);if(v.MotionName!=action)break;captured++;if(!p.NativeModel)throw new Exception("Native model absent");var arm=p.NativeModel.GetComponentsInChildren<Transform>().First(t=>t.name=="mixamorig:RightArm");if(i>0)armDelta+=Quaternion.Angle(previous,arm.localRotation);previous=arm.localRotation;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,640,640),0,0);tex.Apply();File.WriteAllBytes(path+"/"+i.ToString("D3")+".png",tex.EncodeToPNG());}var result=$"Actual {action}; captured={captured}, simulationSpeed=.25 / {v.MotionName} / {p.PlayingMotion}, preview={p.PreviewMotion??"none"}, frames={Time.frameCount-first}, accumulated arm angle={armDelta:F2}, tool={v.HeldTool}";File.WriteAllText(path+"/result.txt",result);return result;}finally{cam.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);AntColony.UI.SimulationPause.UserSpeed(0);}
 }
}
