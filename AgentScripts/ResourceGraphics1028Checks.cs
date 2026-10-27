using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using AntColony.Save;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
using R=AntColony.Data.ResourceType;
public static class ResourceGraphics1028Checks
{
 public static readonly string[] Models={"Mat_Chitin","Deposit_Chitin","Mat_Wax","Part_Component","Water_Jug","Resource_Soil"};
 public static string Import()
 {
  if(Application.isPlaying)throw new Exception("Stop Play before import");AssetDatabase.Refresh();var method=typeof(AntColony.EditorTools.KemonoImport).GetMethod("ImportProp",BindingFlags.NonPublic|BindingFlags.Static);
  foreach(var id in Models){var path="Assets/Resources/ResourceModels/"+id+".fbx";method.Invoke(null,new object[]{path,true});var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(model==null||model.GetComponentsInChildren<Renderer>().Length==0)throw new Exception("Missing model "+id);foreach(var r in model.GetComponentsInChildren<Renderer>())if(r.sharedMaterials.Any(m=>m==null||!m.shader.name.StartsWith("Universal Render Pipeline/")))throw new Exception("Missing URP material "+id);}
  AssetDatabase.SaveAssets();return "PASS 6 resource FBX/URP imports";
 }
 static int count;static void Check(bool ok,string label){if(!ok)throw new Exception("Resource graphics FAIL "+count+" "+label);count++;File.WriteAllText("Temp/resource-graphics1028-progress.txt",count+" "+label);}
 static bool ModelVisible(ResourceNode n)=>n.GetComponentsInChildren<Renderer>().Any(r=>r.gameObject!=n.gameObject&&r.enabled&&!r.forceRenderingOff);
 static ResourceNode Make(R type,Vector3 at,bool loose)
 {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.SetActive(false);go.name="Resource review "+type;go.transform.position=at;go.transform.localScale=Vector3.one*.6f;go.GetComponent<Collider>().isTrigger=true;var n=go.AddComponent<ResourceNode>();if(loose)n.ConfigureLoot(type,7);else n.ConfigureNatural(type,150,0);go.SetActive(true);return n;}
 public static async Task<string> Main()
 {
  count=0;var root=SaveStorage.RootOverride;var scale=Time.timeScale;Time.timeScale=0;SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"ResourceGraphics1028-"+Guid.NewGuid().ToString("N"));
  var origin=WorldMapManager.Instance.HomePosition+Vector3.up*15;var types=(R[])Enum.GetValues(typeof(R));var made=new System.Collections.Generic.List<ResourceNode>();
  try
  {
   for(int i=0;i<types.Length;i++)made.Add(Make(types[i],origin+new Vector3((i%10)*3,0,(i/10)*3),true));
   var naturalChitin=Make(R.Chitin,origin+Vector3.left*4,false);var naturalSoil=Make(R.Soil,origin+Vector3.left*8,false);await Task.Delay(150);Physics.SyncTransforms();
   foreach(var n in made)
   {
    Check(n.GetComponent<Renderer>().forceRenderingOff&&ModelVisible(n),"actual model hides cargo primitive "+n.ResourceType);
    var box=n.GetComponent<BoxCollider>();Check(box!=null&&box.isTrigger&&box.bounds.size.sqrMagnitude>0,"click collider "+n.ResourceType);
    Check(n.IsLooseCargo&&n.AmountRemaining==7,"cargo amount unchanged "+n.ResourceType);
   }
   foreach(var n in new[]{naturalChitin,naturalSoil}){Check(ModelVisible(n)&&n.GetComponent<Renderer>().forceRenderingOff,"natural visible model "+n.ResourceType);Check(!n.IsLooseCargo&&n.AmountRemaining==150,"natural stock unchanged "+n.ResourceType);}
   var water=made.First(n=>n.ResourceType==R.Water);var bounds=water.GetComponentsInChildren<Renderer>().Where(r=>r.gameObject!=water.gameObject).Select(r=>r.bounds).First();Check(bounds.size.y<1&&Mathf.Max(bounds.size.x,bounds.size.z)<.6f,"water jug stays below character height");
   var component=made.First(n=>n.ResourceType==R.Component);Check(component.GetComponent<BoxCollider>().bounds.size.x<1,"component cargo has compact click bounds");
   Check(SaveSystem.TrySave(false,0,out var error),"all resource types save: "+error);Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error),"all resource types load: "+error);var end=DateTime.UtcNow.AddSeconds(90);while(SaveSystem.Busy&&DateTime.UtcNow<end)await Task.Delay(80);Time.timeScale=0;await Task.Delay(150);Check(!SaveSystem.Busy,"resource scene restored");
   for(int i=0;i<types.Length;i++){var at=origin+new Vector3((i%10)*3,0,(i/10)*3);var n=ResourceNode.Available.FirstOrDefault(n=>n.IsLooseCargo&&n.ResourceType==types[i]&&(n.transform.position-at).sqrMagnitude<.01f);Check(n!=null&&n.AmountRemaining==7&&n.GetComponent<Renderer>().forceRenderingOff&&ModelVisible(n),"restored cargo model and stock "+types[i]);}
   foreach(var type in new[]{R.Chitin,R.Soil})Check(ResourceNode.Available.Any(n=>!n.IsLooseCargo&&n.ResourceType==type&&n.AmountRemaining==150&&ModelVisible(n)),"restored natural visibility "+type);
   var result="PASS "+count+" all resource cargo models/colliders/stock, natural chitin/soil and real save/load checks";File.WriteAllText("Temp/resource-graphics1028-result.txt",result);return result;
  }
  catch(Exception e){File.WriteAllText("Temp/resource-graphics1028-result.txt",e.ToString());throw;}
  finally{SaveStorage.RootOverride=root;Time.timeScale=scale;}
 }
}
