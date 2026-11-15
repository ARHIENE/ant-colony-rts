using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.World;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;
public static class FogRetaliation1115Checks
{
    const string Dir = "work/fog-retaliation-1115/";
    const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
    static object Get(WildMonster m, string key) => typeof(WildMonster).GetField(key, Hidden).GetValue(m);
    static void Set(WildMonster m, string key, object value) => typeof(WildMonster).GetField(key, Hidden).SetValue(m, value);
    static void Check(bool ok, string message) { File.AppendAllText(Dir+"checks.txt", (ok?"PASS ":"FAIL ")+message+"\n"); if(!ok)throw new Exception(message); }
    public static async Task<string> Main()
    {
        if (!Application.isPlaying || Time.timeScale != 0) throw new Exception("Paused isolated checkpoint required");
        Directory.CreateDirectory(Dir); File.WriteAllText(Dir+"checks.txt", "Isolated fixture, restore natural checkpoint afterward.\n");
        var monsters = SceneObjects.Any<LocalIncursions>().Visitors.Where(m=>m!=null && !m.IsDead && !m.IsFlying && !m.Retreating).ToArray();
        Check(monsters.Length>0 && monsters.All(m=>(float)Get(m,"attackNoticeRemaining")==0), "Old checkpoint loads with no invented attack notice");
        var m = monsters.First(m=>Get(m,"currentTarget")==null && m.Agent.isOnNavMesh && m.CurrentHealth>5);
        var source = m.Position + Vector3.right*8;
        FormationDamage.Apply(m, 0, source, true);
        Check((float)Get(m,"attackNoticeRemaining")==0, "Zero damage does not trigger investigation");
        FormationDamage.Apply(m, 1, source, true);
        Check((float)Get(m,"attackNoticeRemaining")==10 && (Vector3)Get(m,"attackNoticePoint")==source, "Actual incoming damage records its origin for ten seconds");
        Check(Get(m,"currentTarget")==null, "Notice does not assign an unseen target");
        Set(m,"nextPathRequest",0f);
        typeof(WildMonster).GetMethod("ApproachSettlement",Hidden).Invoke(m,null);
        Check(Vector3.Distance((Vector3)Get(m,"requestedDestination"),source)<.01f && m.Agent.stoppingDistance<=.5f, "Ground investigation routes to the source instead of stopping at weapon range");
        var point = (Vector3)Get(m,"attackNoticePoint");
        typeof(WildMonster).GetMethod("NoticeAttack",Hidden).Invoke(m,new object[]{new Vector3(float.NaN,0,0)});
        Check((Vector3)Get(m,"attackNoticePoint")==point, "Invalid source is rejected");
        var capture = (WildMonsterDto)typeof(WildMonster).GetMethod("CaptureRuntime",Hidden).Invoke(m,null);
        Check(capture.attackNoticeRemaining==10 && capture.attackNoticePoint.ToVector3()==source, "Runtime snapshot preserves investigation");
        Check(SaveSystem.TrySave(false,0,out var error), "Full save accepts notice: "+error);
        Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error), "Full load starts: "+error);
        var deadline=DateTime.UtcNow.AddSeconds(90);
        while(SaveSystem.Busy && DateTime.UtcNow<deadline) await Task.Delay(100);
        SimulationPause.UserSpeed(0);
        Check(!SaveSystem.Busy, "Full load completes");
        Check(SceneObjects.Any<LocalIncursions>().Visitors.Any(x=>x!=null && (float)Get(x,"attackNoticeRemaining")>9.5f && Vector3.Distance((Vector3)Get(x,"attackNoticePoint"),source)<.01f), "Full reload preserves remaining time and origin");
        return File.ReadAllText(Dir+"checks.txt");
    }
}
