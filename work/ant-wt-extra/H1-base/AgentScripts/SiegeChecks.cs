using System;
using System.Collections.Generic;
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
using UnityEngine.AI;
using Object = UnityEngine.Object;

// 공성(2026-10-03): 벽에 길이 막힌 침공 개체가 벽을 노리고, 나뭇잎 벽엔 불을 질러 태우며 불은 옆 벽으로 번진다.
public static class SiegeChecks
{
    static int checks;
    static readonly List<GameObject> made = new List<GameObject>();
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL: " + label); checks++; }
    static GameObject Template(BuildingKind k) => (GameObject)typeof(BuildingPlacementController)
        .GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).Invoke(null, new object[] { k, UnitRole.Worker });
    static T Put<T>(BuildingKind k, Vector3 p) where T : Component
    { var go = Object.Instantiate(Template(k), p, Quaternion.identity); go.SetActive(true); made.Add(go); return go.GetComponent<T>(); }
    static async Task Until(Func<bool> done, float seconds) { var end = Time.realtimeSinceStartup + seconds; while (!done() && Time.realtimeSinceStartup < end) await Task.Delay(50); }

    public static async Task<string> Main()
    {
        checks = 0; made.Clear(); Check(Application.isPlaying, "play mode");
        while (SaveSystem.Busy) await Task.Delay(50);
        SaveSystem.NewGame(new NewGameOptions { seed = 261005, mapSize = MapSize.Small });
        while (SaveSystem.Busy) await Task.Delay(50);
        GameMenuController.Instance.Resume(); Time.timeScale = 0;
        try
        {
            Check(WildMonster.IsBarrier(Put<Wall>(BuildingKind.LeafWall, new Vector3(0, -50, 0))) && !WildMonster.IsBarrier(Object.FindAnyObjectByType<Stockpile>()), "barrier kinds");
            var gate = Put<Gate>(BuildingKind.Gate, new Vector3(5, -50, 0)); gate.SetOpen(true);
            Check(!WildMonster.IsBarrier(gate), "open gate is not a barrier"); gate.SetOpen(false); Check(WildMonster.IsBarrier(gate), "closed gate is a barrier");

            // 불 번짐: 붙은 나뭇잎 벽 2칸 중 하나에 불 → 2초 뒤 확률로 옆 벽, 벽마다 한 번만 탄다.
            var home = Object.FindAnyObjectByType<Stockpile>().Position;
            Check(NavMesh.SamplePosition(home + new Vector3(40, 0, 40), out var far, 20, NavMesh.AllAreas), "far ground");
            var o = new Vector3(Mathf.Floor(far.position.x), far.position.y, Mathf.Floor(far.position.z));
            Time.timeScale = 4;
            bool spread = false;
            for (var i = 0; i < 8 && !spread; i++)
            {
                var a = Put<Wall>(BuildingKind.LeafWall, o + new Vector3(20.5f + i * 3, .75f, .5f));
                var b = Put<Wall>(BuildingKind.LeafWall, o + new Vector3(21.5f + i * 3, .75f, .5f));
                await Task.Yield();
                Check(WallFire.Ignite(a) && !WallFire.Ignite(a), "ignite once");
                await Until(() => b.GetComponent<WallFire>() != null, 1.2f);
                spread = b != null && b.GetComponent<WallFire>() != null;
            }
            Check(spread, "fire spreads to neighbouring leaf wall");
            var cap = Put<Wall>(BuildingKind.CapWall, o + new Vector3(50.5f, .8f, .5f));
            Check(!WallFire.Ignite(cap), "cap wall does not burn");

            // 공성: 닫힌 7×7 나뭇잎 벽(안쪽 5×5) 안에 장수, 밖의 침공 개체는 벽을 노려 불을 지른다. 벽 높이는 칸마다 지면에 맞춘다.
            var homeHit = NavMesh.SamplePosition(home, out var hh, 10, NavMesh.AllAreas) ? hh.position : home;
            bool Connected(Vector3 p) { var path = new NavMeshPath(); return NavMesh.CalculatePath(p, homeHit, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete; }
            Vector3 outsidePos = default; var found = false; NavMeshHit os = default, ins = default;
            for (var t = 0; t < 30 && !found; t++)
            {
                var c = home + new Vector3(Mathf.Cos(t) * (25 + t), 0, Mathf.Sin(t) * (25 + t));
                if (!NavMesh.SamplePosition(c, out var s, 4, NavMesh.AllAreas) || !Connected(s.position)) continue;
                o = new Vector3(Mathf.Floor(s.position.x), s.position.y, Mathf.Floor(s.position.z));
                found = NavMesh.SamplePosition(o + new Vector3(3.5f, 0, -3f), out os, 2, NavMesh.AllAreas) && Connected(os.position)
                    && NavMesh.SamplePosition(o + new Vector3(3.5f, 0, 3.5f), out ins, 1, NavMesh.AllAreas) && Mathf.Abs(ins.position.y - os.position.y) < .6f;
                outsidePos = os.position;
            }
            Check(found, "flat connected ground for siege ring");
            for (var x = 0; x < 7; x++) for (var z = 0; z < 7; z++)
                if ((x == 0 || x == 6 || z == 0 || z == 6) && NavMesh.SamplePosition(o + new Vector3(x + .5f, 0, z + .5f), out var g, 2, NavMesh.AllAreas))
                    Put<Wall>(BuildingKind.LeafWall, new Vector3(o.x + x + .5f, g.position.y + .75f, o.z + z + .5f));
            await Task.Delay(500);
            var bait = CommanderRoster.Instance.Commanders.First(c => c.IsColonyMember && !c.IsEmbarked);
            bait.CommandStop(); bait.WorkState.jobs = 0; bait.Agent.Warp(o + new Vector3(3.5f, 0, 3.5f));
            var incursions = Object.FindAnyObjectByType<LocalIncursions>();
            var raiderTemplate = (WildMonster)typeof(LocalIncursions).GetField("raiderTemplate", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(incursions);
            var enclosed = new NavMeshPath(); NavMesh.CalculatePath(outsidePos, bait.Agent.nextPosition, NavMesh.AllAreas, enclosed);
            Check(enclosed.status == NavMeshPathStatus.PathPartial, "ring blocks the path: " + enclosed.status);
            var raider = Object.Instantiate(raiderTemplate, outsidePos, Quaternion.identity); made.Add(raider.gameObject);
            raider.gameObject.SetActive(true); raider.MakeRaider();
            var target = typeof(WildMonster).GetField("currentTarget", BindingFlags.Instance | BindingFlags.NonPublic);
            await Until(() => WildMonster.IsBarrier(target.GetValue(raider) as AntColony.Core.IDamageable), 15);
            var ag = raider.GetComponent<NavMeshAgent>();
            Check(target.GetValue(raider) is Wall, "blocked raider targets the wall: target=" + target.GetValue(raider) + " path=" + (ag.enabled ? ag.pathStatus + "/" + ag.pathPending + "/" + ag.isOnNavMesh : "off") + " raider=" + raider.Position + " bait=" + bait.Position + " dead=" + raider.IsDead + " stopped=" + ag.isStopped + " speed=" + ag.speed + " vel=" + ag.velocity + " dest=" + ag.destination + " rem=" + ag.remainingDistance + " root=" + raider.RootRemaining + " ts=" + Time.timeScale + " corners=" + ag.path.corners.Length + " barrier=" + typeof(WildMonster).GetMethod("FindBarrier", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(raider, null));
            await Until(() => made.Any(g => g != null && g.GetComponent<WallFire>() != null && g.transform.position.x < o.x + 7 && g.transform.position.z < o.z + 7 && g.transform.position.x > o.x), 20);
            Check(made.Any(g => g != null && g.GetComponent<WallFire>() != null && g.transform.position.x > o.x && g.transform.position.x < o.x + 7 && g.transform.position.z < o.z + 7), "raider sets leaf wall on fire");
            return "PASS " + checks + " siege checks";
        }
        finally
        {
            Time.timeScale = 0;
            foreach (var g in made) if (g != null) Object.Destroy(g);
        }
    }
}
