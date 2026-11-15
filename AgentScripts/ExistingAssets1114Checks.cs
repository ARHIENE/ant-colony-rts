using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

public static class ExistingAssets1114Checks
{
    static int checks;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("FAIL: " + message); checks++; }
    static async Task Wait(Func<bool> done) { var until=DateTime.UtcNow.AddSeconds(50); while(!done() && DateTime.UtcNow<until) await Task.Delay(80); Check(done(),"wait"); }
    static Workshop Build(Vector3 at)
    {
        var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{BuildingKind.SewingTable,UnitRole.Worker});
        Check(template!=null,"build menu template");
        var go=Object.Instantiate(template,at,Quaternion.identity);go.name="Sewing1114Check";go.SetActive(true);return go.GetComponent<Workshop>();
    }
    public static string Models()
    {
        foreach(var kind in new[]{BuildingKind.SewingTable,BuildingKind.UndergroundScanner,BuildingKind.DeepDrill,BuildingKind.AdvancedDeepDrill,BuildingKind.GasExtractor,BuildingKind.GasGenerator,BuildingKind.GasTank})
        {
            var model=Resources.Load<GameObject>("Facilities/"+FacilityVisual.ModelFor(kind));
            Check(model && model.GetComponentsInChildren<Renderer>(true).Length>0,"model "+kind);
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true)) Check(renderer.sharedMaterials.All(m=>m && m.shader && m.shader.name!="Hidden/InternalErrorShader"),"materials "+kind);
        }
        var preview=new PreviewRenderUtility();
        try
        {
            var go=Object.Instantiate(Resources.Load<GameObject>("Facilities/SewingTable"));preview.AddSingleGO(go);
            var rr=go.GetComponentsInChildren<Renderer>();var bounds=rr[0].bounds;foreach(var r in rr)bounds.Encapsulate(r.bounds);
            preview.BeginStaticPreview(new Rect(0,0,800,800));
            preview.camera.transform.position=bounds.center+new Vector3(1.7f,1.3f,-2.2f)*bounds.size.magnitude;
            preview.camera.transform.LookAt(bounds.center);preview.camera.fieldOfView=35;preview.camera.nearClipPlane=.01f;preview.camera.farClipPlane=1000;
            preview.camera.clearFlags=CameraClearFlags.Color;preview.camera.backgroundColor=new Color(.17f,.19f,.22f);
            preview.lights[0].intensity=1.5f;preview.lights[0].transform.rotation=Quaternion.Euler(40,25,0);preview.lights[1].intensity=.8f;
            preview.Render(true,false);var texture=preview.EndStaticPreview();Directory.CreateDirectory("work/existing-assets-1114");File.WriteAllBytes("work/existing-assets-1114/sewing-preview.png",texture.EncodeToPNG());Object.DestroyImmediate(texture);
        }finally{preview.Cleanup();}
        return "PASS 7 existing model imports and URP materials; sewing preview captured";
    }
    public static async Task<string> Main()
    {
        Check(Application.isPlaying,"Play mode");
        var oldRoot=SaveStorage.RootOverride;var settings=UserSettings.Current.Clone();
        SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"Sewing1114-"+Guid.NewGuid().ToString("N"));
        try
        {
            var quiet=settings.Clone();quiet.autoSaveEnabled=quiet.autoPauseThreat=quiet.autoPauseDowned=false;UserSettings.Apply(quiet,false);
            await Wait(()=>!SaveSystem.Busy);SaveSystem.NewGame(new NewGameOptions{seed=261114,mapSize=MapSize.Small});await Wait(()=>!SaveSystem.Busy);
            GameMenuController.Instance.Resume();Time.timeScale=0;
            var c=CommanderRoster.Instance.Commanders.First();c.CommandStop();c.SetJobEnabled(CommanderJobs.Crafting,true);
            var shop=Build(c.Position+Vector3.right*2);
            Check(shop.IsSewing && shop.Accepts(EquipmentRecipe.ClothClothes) && !shop.Accepts(EquipmentRecipe.Mandible),"sewing recipe isolation");
            Check(shop.TryEnqueue(EquipmentRecipe.ClothClothes),"queue cloth clothing");Check(!shop.InputsReady,"physical cloth delivery required");
            shop.Deliver(new object(),ResourceType.Cloth,4);Check(shop.InputsReady,"cloth accepted");
            Check(shop.TryAssign(c),"crafter assigned");shop.Tick(10000);
            var loot=SceneObjects.All<EquipmentLoot>().FirstOrDefault(l=>l.Items.Any(e=>e.IsClothing));Check(loot!=null,"crafted garment on floor");
            var item=loot.Items.First(e=>e.IsClothing);Check(item.IsValid && item.durability==100,"new durability valid");
            // Move the test output through the inventory boundary without waiting for hauling AI.
            Check(EquipmentInventory.Instance.Add(item),"inventory accepts garment");loot.Items.Remove(item);
            item.durability=35;var id=item.id;var quality=item.quality;
            Check(shop.TryRepair(item) && !shop.TryRepair(item),"unique repair reservation");
            Check(!EquipmentInventory.Instance.Equip(c,item),"reserved garment cannot equip");
            Check(!shop.InputsReady,"repair also needs delivery");shop.Deliver(new object(),ResourceType.Cloth,1);
            c.CommandStop();Check(shop.TryAssign(c),"repair worker assigned");shop.Tick(.5f);shop.Release();
            Check(shop.Jobs.Count==1 && shop.Jobs[0].materialsPaid && shop.Jobs[0].work>0,"partial repair state");
            if(!SaveSystem.TrySave(false,0,out var error))throw new Exception("save: "+error);
            if(!SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error))throw new Exception("load: "+error);
            await Wait(()=>!SaveSystem.Busy);Time.timeScale=0;
            shop=SceneObjects.All<Workshop>().Single(w=>w.name=="Sewing1114Check");item=EquipmentInventory.Instance.Items.Single(e=>e.id==id);
            Check(shop.IsSewing && shop.Jobs.Count==1 && shop.Jobs[0].repairId==id && item.durability==35,"real save/load preserves sewing repair");
            c=CommanderRoster.Instance.Commanders.First(x=>x.PersonalState.id==shop.Jobs[0].makerId);c.CommandStop();
            Check(shop.TryAssign(c),"restored maker resumes");shop.Tick(10000);
            Check(item.durability==100 && item.id==id && item.quality==quality && shop.Jobs.Count==0,"repair preserves identity and quality");
            item.durability=50;Check(shop.TryRepair(item) && shop.Cancel(0) && item.durability==50 && EquipmentInventory.Instance.Items.Count(e=>e.id==id)==1,"cancel preserves original without duplicate");
            Check(EquipmentInventory.Instance.Equip(c,item),"completed or cancelled repair releases garment");
            var absorb=typeof(CommanderAnt).GetMethod("AbsorbDurableDamage",BindingFlags.Instance|BindingFlags.NonPublic);
            Check(Mathf.Approximately((float)absorb.Invoke(c,new object[]{20f}),17f) && Mathf.Approximately(item.durability,47f),"clothing absorbs damage and wears");
            Directory.CreateDirectory("work/existing-assets-1114");var result="PASS "+checks+" checks: craft/delivery, repair, ownership, real save/load, cancellation, wear";File.WriteAllText("work/existing-assets-1114/checks.txt",result);return result;
        }
        finally{Time.timeScale=0;UserSettings.Apply(settings,false);SaveStorage.RootOverride=oldRoot;}
    }
}
