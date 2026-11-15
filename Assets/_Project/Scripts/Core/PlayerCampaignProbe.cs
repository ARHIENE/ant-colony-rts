#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using AntColony.Save;
using AntColony.Units;
using AntColony.UI;
using UnityEngine;
namespace AntColony.Core
{
    // Opt-in packaged-player regression; never uses the player's own save directory.
    public sealed class PlayerCampaignProbe : MonoBehaviour
    {
        string checkpoint, output;
        bool finished;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-ant-campaign");
            if(at<0 || at+2>=args.Length)return;
            string output=Path.GetFullPath(args[at+2]);Directory.CreateDirectory(Path.GetDirectoryName(output));
            SaveStorage.RootOverride=Path.Combine(Path.GetDirectoryName(output),"campaign-player-save-"+Guid.NewGuid().ToString("N"));
            UserSettings.Apply(new UserSettingsData{autoSaveEnabled=false,autoPauseThreat=false,autoPauseDowned=false,vSync=false,frameLimit=60},false);
            Application.runInBackground=true;
            var go=new GameObject("Packaged campaign validation");DontDestroyOnLoad(go);
            var probe=go.AddComponent<PlayerCampaignProbe>();probe.checkpoint=Path.GetFullPath(args[at+1]);probe.output=output;
        }
        IEnumerator Start()
        {
            File.WriteAllText(output,"RUNNING packaged checkpoint regression, not natural campaign playthrough\n");
            var run=Run();
            while(true)
            {
                object current;try{if(!run.MoveNext())break;current=run.Current;}
                catch(Exception e){File.AppendAllText(output,"FAIL "+e+"\n");finished=true;Application.Quit(1);yield break;}
                yield return current;
            }
            finished=true;File.AppendAllText(output,"PASS packaged campaign\n");Application.Quit(0);
        }
        void Check(bool okay,string message){if(!okay)throw new Exception(message);File.AppendAllText(output,"PASS "+message+"\n");}
        void Update(){if(!finished && Time.realtimeSinceStartup>180){File.AppendAllText(output,"FAIL timeout\n");Application.Quit(2);finished=true;}}
        IEnumerator Run()
        {
            yield return null;
            while(SaveSystem.Busy||!GameSession.Exists||GameMenuController.Instance==null)yield return null;
            Check(SaveSystem.TryLoad(checkpoint,out var error),"Load mothership checkpoint: "+error);
            while(SaveSystem.Busy)yield return null;
            Time.timeScale=0;var campaign=RocketCampaign.Instance;
            Check(campaign!=null && campaign.Mothership.reached && !campaign.MothershipVictory,"Final encounter loaded before victory");
            var boss=campaign.MothershipBoss;var commander=campaign.MothershipCrew.First(c=>c&&!c.IsEmbarked&&!c.IsDead);
            Check(boss && boss.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),"Final boss renderer packaged");
            Check(boss.transform.Find("본선 지휘체 · 생물 핵")!=null,"Biological final boss packaged");
            Check(commander.Agent.isOnNavMesh,"Crew navigation restored");
            Check(SaveSystem.TrySave(false,0,out error),"Save checkpoint in isolated slot: "+error);
            float hp=boss.CurrentHp;
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error),"Reload packaged save: "+error);
            while(SaveSystem.Busy)yield return null;
            Time.timeScale=0;campaign=RocketCampaign.Instance;boss=campaign.MothershipBoss;
            commander=campaign.MothershipCrew.First(c=>c&&!c.IsEmbarked&&!c.IsDead);
            Check(Mathf.Abs(hp-boss.CurrentHp)<.01f,"Boss HP survives packaged reload");
            yield return null;
            Check(commander.GetComponentsInChildren<SkinnedMeshRenderer>().Any(r=>r.enabled),"Mothership commander model is visible after reload");
            GameMenuController.Instance.Resume();commander.CommandAttack(boss);Time.timeScale=1;
            float until=Time.realtimeSinceStartup+40;
            while(boss.CurrentHp>=hp && Time.realtimeSinceStartup<until && commander.PersonalHealth>0)yield return null;
            Time.timeScale=0;Check(boss.CurrentHp<hp,"Normal attack damages packaged boss");
            SceneObjects.Any<AntColony.Camera.IsometricCameraController>()?.RestoreView((commander.Position+boss.Position)*.5f+Vector3.up*3,0,14);
            var viewport=UnityEngine.Camera.main.WorldToViewportPoint(boss.Position+Vector3.up*2);
            Check(viewport.z>0 && viewport.x>.05f && viewport.x<.95f && viewport.y>.05f && viewport.y<.85f,"Boss is framed below the HUD in packaged player");
            boss.TakeDamage(boss.CurrentHp); // Victory dispatch only; not a combat balance claim.
            yield return null;
            Check(campaign.MothershipVictory&&CampaignResearch.Instance.Departed,"Final-boss death opens victory");
            Check(SaveSystem.TrySave(false,1,out error),"Save victory: "+error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,1),out error),"Reload victory: "+error);
            while(SaveSystem.Busy)yield return null;
            Check(RocketCampaign.Instance.MothershipVictory&&CampaignResearch.Instance.Departed,"Victory persists in packaged player");
        }
    }
}
#endif
