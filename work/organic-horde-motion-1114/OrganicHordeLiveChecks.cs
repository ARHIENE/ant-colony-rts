using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;

public static class OrganicHordeLiveChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly List<string> log = new List<string>();
    static void Note(string s) { log.Add(s); File.WriteAllLines("work/organic-horde-motion-1114/live-checks.txt", log); }
    static void Check(bool ok, string s) { if (!ok) throw new Exception(s); Note("PASS " + s); }
    static async Task Frame() { int f = Time.frameCount; do { await Task.Delay(10); if (!Application.isPlaying) throw new Exception("Play stopped"); } while (Time.frameCount == f); }
    static int DeadBodies() => UnityEngine.Object.FindObjectsByType<OrganicHordeRemains>().Sum(r => ((Matrix4x4[])typeof(OrganicHordeRemains).GetField("matrices", Private).GetValue(r)).Length);
    public static string Main()
    {
        if (!Application.isPlaying) throw new Exception("Play required");
        log.Clear(); Note("RUNNING"); _ = Run(); return "Started; await RESTORED in live-checks.txt";
    }
    static async Task Run()
    {
        var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride; bool pass = false;
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false; UserSettings.Apply(quiet, false);
            SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "OrganicHordeMotion-" + Guid.NewGuid().ToString("N"));
            while (SaveSystem.Busy) await Frame();
            SaveSystem.NewGame(new NewGameOptions { seed = 261114, mapSize = MapSize.Small, startingCount = 3 });
            while (SaveSystem.Busy || WaveDefense.Instance == null) await Frame();
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            WaveDefense.Instance.enabled = false;
            foreach (var c in CommanderRoster.Instance.Commanders) if (c != null) c.enabled = false;
            var local = SceneObjects.Any<LocalIncursions>();
            var spawn = typeof(LocalIncursions).GetMethod("RestoreSpawn", Private);
            var configure = typeof(WildMonster).GetMethod("ConfigureHorde", Private);
            var hostList = new List<WildMonster>();
            NavMesh.SamplePosition(HomeReturnPoint.Anchor, out var origin, 10, NavMesh.AllAreas);
            for (int kind = 1; kind <= 17; kind++)
            {
                var wanted = origin.position + new Vector3(((kind - 1) % 5 - 2) * 8, 0, ((kind - 1) / 5 - 1) * 8);
                var position = OrganicHordeMotion.Ground(origin.position, wanted);
                var host = (WildMonster)spawn.Invoke(local, new object[] { false, position });
                host.ConfigureOrganic((OrganicEnemyKind)kind); configure.Invoke(host, new object[] { 6 }); host.MakeRaider();
                host.name = "MotionCheck_" + (OrganicEnemyKind)kind; host.gameObject.SetActive(true); host.enabled = false; hostList.Add(host);
                await Frame(); await Frame();
                var motion = host.GetComponent<OrganicHordeMotion>();
                Check(motion != null && motion.AnimatedBodies == 5, (OrganicEnemyKind)kind + " all five extra bodies animated");
                for (int i = 0; i < 5; i++)
                {
                    var p = motion.BodyPosition(i);
                    Check(NavMesh.SamplePosition(p, out var ground, .1f, NavMesh.AllAreas) && Mathf.Abs(p.y - ground.position.y) < .03f, (OrganicEnemyKind)kind + " body " + i + " on navigable ground");
                }
                var visual = host.GetComponent<OrganicEnemyVisual>();
                foreach (string state in new[] { "Idle", "Move", "Attack", "Hit" })
                {
                    int action = state == "Idle" ? 0 : state == "Move" ? 1 : state == "Attack" ? 2 : 3;
                    visual.Act(action, 1000); visual.Animator.Play("Base Layer." + state, 0, .25f); visual.Animator.Update(0);
                    await Frame(); await Frame();
                    Check(motion.AnimatedBodies == 5, (OrganicEnemyKind)kind + " " + state + " keeps all bodies");
                    if (state == "Idle" || state == "Move")
                    {
                        var skin = visual.Model.GetComponentInChildren<SkinnedMeshRenderer>(); var baked = new Mesh();
                        try
                        {
                            skin.BakeMesh(baked, true); float low = float.PositiveInfinity;
                            foreach (var vertex in baked.vertices) low = Mathf.Min(low, skin.localToWorldMatrix.MultiplyPoint3x4(vertex).y);
                            Check(Mathf.Abs(low - host.Position.y) < .06f, (OrganicEnemyKind)kind + " " + state + " actual sole contact error=" + Mathf.Abs(low - host.Position.y));
                        }
                        finally { UnityEngine.Object.Destroy(baked); }
                    }
                }
            }
            int before = DeadBodies(); var victim = hostList[0];
            victim.Wound(victim.CurrentHealth / 3); await Frame(); await Frame();
            Check(victim.HordeCount == 4 && victim.GetComponent<OrganicHordeMotion>().AnimatedBodies == 3, "partial losses shrink living bodies");
            Check(DeadBodies() == before + 2, "lost bodies play death rather than disappear");
            victim.Wound(1000000); await Frame(); await Frame();
            Check(DeadBodies() == before + 5, "final host death releases remaining body death animations");
            Time.timeScale = .5f; await Task.Delay(600); await Frame();
            Check(UnityEngine.Object.FindObjectsByType<OrganicHordeRemains>().Any(r => (float)typeof(OrganicHordeRemains).GetField("age", Private).GetValue(r) > .1f), "fallen bodies advance through death frames");
            Time.timeScale = 0;
            var camera = SceneObjects.Any<AntColony.Camera.IsometricCameraController>();
            typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView", Private).Invoke(camera, new object[] { origin.position, 0f, 28f });
            pass = true; Note("PASS full live suite; mixed-species review scene retained until Play stop");
        }
        catch (Exception e) { Note("FAILED " + e); }
        finally { Time.timeScale = 0; UserSettings.Apply(settings, false); SaveStorage.RootOverride = root; Note("RESTORED result=" + (pass ? "PASS" : "FAIL")); }
    }
}
