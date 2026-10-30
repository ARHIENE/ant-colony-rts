using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public static class Review1029K2Checks
{
    private const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Hidden).Invoke(target, args);
    private static void Check(bool ok, string label) { if (!ok) throw new Exception(label); }

    // 실제 게임 DLL의 상태 기계·거리식·입력 검증을 오프라인으로 실행한다.
    public static async Task<string> Main()
    {
        await Task.CompletedTask;
        int count = 0;
        void Assert(bool ok, string label) { Check(ok, label); count++; }
        Assert(AntColony.Boss.AoE.BossAoEState.Valid(null), "old null state");
        Assert(AntColony.Boss.AoE.BossAoEState.Valid(new AntColony.Boss.AoE.BossAoEState())
            && !AntColony.Boss.AoE.BossAoEState.Present(new AntColony.Boss.AoE.BossAoEState()), "JsonUtility empty inline state stays idle");
        foreach (float duration in new[] { 1.5f, 1.2f, 1.2f })
        foreach (float speed in new[] { 1f, 5f })
        {
            var original = new AntColony.Boss.AoE.BossAoEState { captured = true, origin = new Vector3(4, 2, -3), forward = new Vector3(1, 0, 0), remaining = duration, afterHit = .15f };
            Assert(AntColony.Boss.AoE.BossAoEState.Valid(original), "warning valid");
            Assert(!original.Advance(duration / 2), "half warning no hit");
            var restored = original.Copy();
            Assert(restored.origin.x == 4 && restored.origin.y == 2 && restored.origin.z == -3 && restored.forward.x == 1, "fixed position/direction");
            Assert(restored.remaining == duration / 2 && !restored.hitResolved, "remaining preserved");
            Assert(!restored.Advance(0) && restored.remaining == duration / 2, "pause holds timer");
            int hits = 0; float elapsed = 0;
            while (elapsed < duration + 1) { if (restored.Advance(.01f * speed)) hits++; elapsed += .01f * speed; }
            Assert(hits == 1 && restored.hitResolved && restored.remaining == 0, "exactly one hit after resume");
            Assert(original.remaining == duration / 2 && !original.hitResolved, "capture independent from live clock");
            var afterHit = restored.Copy();
            Assert(!afterHit.Advance(100) && afterHit.afterHit == 0, "post-hit load cannot damage again");
            foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, -1f, 61f })
            {
                var bad = original.Copy(); bad.remaining = invalid;
                Assert(!AntColony.Boss.AoE.BossAoEState.Valid(bad), "bad remaining rejected");
            }
            var direction = original.Copy(); direction.forward = new Vector3(0, 0, 0);
            Assert(!AntColony.Boss.AoE.BossAoEState.Valid(direction), "zero direction rejected");
            direction = original.Copy(); direction.origin.x = float.NaN;
            Assert(!AntColony.Boss.AoE.BossAoEState.Valid(direction), "NaN origin rejected");
        }
        foreach (bool walk in new[] { false, true })
        foreach (bool air in new[] { false, true })
        {
            var a = new Vector2(.66f, .5f); var near = new Vector2(.65f, .51f); var far = new Vector2(.34f, .5f);
            float close = AntColony.World.ExpeditionRules.BetweenSeconds(a, near, walk, air);
            float distant = AntColony.World.ExpeditionRules.BetweenSeconds(a, far, walk, air);
            Assert(close < distant / 10, "same home ring, distinct actual travel");
            Assert(Math.Abs(distant - (walk ? 300 : air ? 20 : 40)) < .001, "diameter distance budget");
            Assert(Math.Abs(distant - AntColony.World.ExpeditionRules.BetweenSeconds(far, a, walk, air)) < .001, "symmetric route");
            Assert(AntColony.World.ExpeditionRules.BetweenSeconds(a, a, walk, air) == 0, "zero actual distance");
        }
        var valid = typeof(AntColony.World.DiplomacyManager).GetMethod("ValidHomecomingEntry", BindingFlags.Static | BindingFlags.NonPublic);
        var state = new AntColony.World.DiplomacyManager.State();
        Assert((bool)valid.Invoke(null, new object[] { state }) && !state.homecomingEntrySet, "old entry defaults to boundary");
        state.homecomingEntrySet = true; state.homecomingEntry = new Vector3(-20, 3, 12);
        Assert((bool)valid.Invoke(null, new object[] { state }), "valid signed coordinates");
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, 1000001f })
        { state.homecomingEntry.x = invalid; Assert(!(bool)valid.Invoke(null, new object[] { state }), "bad entry rejected"); }
        return $"K2 offline PASS {count}; Unity roundtrip/NavMesh/UI not executed (RunScene separately).";
    }

    // 메모리 복원은 기존 수송대를 지우지 않는다(실제 로드는 새 장면). 같은 운반자 ID는 마지막 복원분만 남긴다.
    private static void DropStaleShips()
    {
        var ships = (System.Collections.Generic.List<AntColony.World.ExpeditionTransport>)AntColony.World.WorldMapManager.Instance.Transports;
        var seen = new System.Collections.Generic.HashSet<string>();
        for (var i = ships.Count - 1; i >= 0; i--)
            if (ships[i] == null || !seen.Add(AntColony.World.BossRewardTransfer.Carrier(ships[i]))) { if (ships[i] != null) UnityEngine.Object.Destroy(ships[i].gameObject); ships.RemoveAt(i); }
    }
    private static async Task Drain(IEnumerator routine)
    {
        while (routine.MoveNext())
            if (routine.Current is IEnumerator nested) await Drain(nested); else await Task.Yield();
    }

    // Claude 전용: 사용자 저장을 쓰지 않는 격리 새 게임에서 실행. 저장 파일 I/O 없음.
    public static async Task<string> RunScene()
    {
        Check(Application.isPlaying, "isolated Play scene required");
        var before = AntColony.Save.SaveSnapshot.Capture();
        Check(AntColony.Save.SavePreflight.Validate(before, out var error), "baseline: " + error);
        var world = AntColony.World.WorldMapManager.Instance;
        var boss = world.Sites.Select(s => s.Boss).First(b => b != null && !b.IsDead && b.gameObject.activeInHierarchy);
        var added = new System.Collections.Generic.List<Component>();
        T AoE<T>() where T : Component { var c = boss.GetComponent<T>(); if (c == null) { c = boss.gameObject.AddComponent<T>(); added.Add(c); } return c; }
        var circle = AoE<AntColony.Boss.AoE.BossCircleAoE>();
        var cone = AoE<AntColony.Boss.AoE.BossConeAoE>();
        var line = AoE<AntColony.Boss.AoE.BossLineAoE>();
        float speed = Time.timeScale;
        var restore = typeof(AntColony.Save.SaveSnapshot).GetMethod("Restore", BindingFlags.NonPublic | BindingFlags.Static);
        int count = 0;
        void Assert(bool ok, string label) { Check(ok, label); count++; }
        try
        {
            Time.timeScale = 0;
            var origin = boss.Position + Vector3.up * 50;
            circle.CastAt(origin); cone.CastFrom(origin, Vector3.right); line.CastFrom(origin, Vector3.back);
            var c = circle.Capture(); var k = cone.Capture(); var l = line.Capture();
            c.remaining *= .5f; k.remaining *= .5f; l.remaining *= .5f;
            circle.Restore(c); cone.Restore(k); line.Restore(l);
            var save = AntColony.Save.SaveSnapshot.Capture();
            Assert(AntColony.Save.SavePreflight.Validate(save, out error), "warning preflight: " + error);
            var json = JsonUtility.ToJson(save);
            Assert(AntColony.Save.SaveValidator.TryParse(json, out var parsed, out error), "warning JSON parse: " + error);
            await Drain((IEnumerator)restore.Invoke(null, new object[] { parsed })); DropStaleShips();
            Assert(circle.IsCasting && cone.IsCasting && line.IsCasting, "three warnings resume");
            Assert(circle.Capture().remaining == c.remaining && cone.Capture().remaining == k.remaining && line.Capture().remaining == l.remaining, "timers unchanged while paused");
            Assert(circle.Capture().origin == c.origin && cone.Capture().origin == k.origin && line.Capture().origin == l.origin
                && cone.Capture().forward == k.forward && line.Capture().forward == l.forward, "no origin re-snap/rotation on load");
            c.hitResolved = true; c.remaining = 0; circle.Restore(c);
            Assert(!circle.IsCasting && circle.Capture().hitResolved && !circle.Capture().Advance(1), "hit-once restore");
            circle.Restore(new AntColony.Boss.AoE.BossAoEState());
            Assert(!circle.IsCasting && circle.Capture() == null, "old empty state produces no cast");

            var ship = world.Transports.FirstOrDefault(t => t != null && !t.IsDead);
            Check(ship != null, "scene requires one transport");
            var sites = world.Sites.Take(2).ToArray();
            Call(ship, "RestoreState", AntColony.World.ExpeditionState.Deployed, 0f, sites[0], sites[0].Landing, world.HomePosition, 0, 0, 0);
            Call(ship, "StartWithdrawTravel", sites[1]);
            Assert(Math.Abs(ship.Remaining - AntColony.World.ExpeditionRules.BetweenSeconds(sites[0].MapPosition, sites[1].MapPosition, ship.Walking, ship.Aircraft)
                / (ship.Walking ? ship.FormationWalkMultiplier : 1)) < .001, "actual departure uses source and destination");
            Call(ship, "RestoreState", AntColony.World.ExpeditionState.Returning, 7.25f, sites[1], ship.Position, world.HomePosition, 0, 0, 0);
            save = AntColony.Save.SaveSnapshot.Capture();
            var savedShip = save.world.transports.First(t => t.remaining == 7.25f);
            Assert(savedShip.remaining == 7.25f, "capture preserves in-flight timer");
            await Drain((IEnumerator)restore.Invoke(null, new object[] { save })); DropStaleShips();
            Assert(world.Transports.Any(t => t != null && t.State == AntColony.World.ExpeditionState.Returning && t.Remaining == 7.25f), "full restore preserves return timer");

            var dm = AntColony.World.DiplomacyManager.Instance;
            Assert(!dm.SetHomecomingEntry(new Vector3(float.NaN, 0, 0), out _), "invalid landing rejected");
            Assert(!dm.SetHomecomingEntry(new Vector3(999999, 0, 999999), out _), "off-map landing rejected");
            var member = AntColony.Core.CommanderRoster.Instance.Commanders.First(a => a.IsColonyMember && !a.IsAwayFromHome);
            var snapshot = (AntColony.Buildings.Prisoner)typeof(AntColony.World.DiplomacyManager)
                .GetMethod("Snapshot", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { member });
            snapshot.PersonalState.id = Guid.NewGuid().ToString();
            snapshot.PersonalState.equipment.Clear();
            var homecoming = new AntColony.World.Homecoming { commander = snapshot, arrive = dm.Data.elapsed, from = "K2 fixture" };
            dm.Data.homecomings.Add(homecoming);
            dm.Data.homecomingEntrySet = true; dm.Data.homecomingEntry = new Vector3(999999, 0, 999999);
            Assert(!(bool)Call(dm, "Arrive", homecoming) && dm.Data.homecomings.Contains(homecoming), "blocked arrival keeps prisoner pending");
            Assert(dm.HomecomingWaitReason(homecoming).Length > 0, "blocked arrival explains cause");
            var candidate = member.Position;
            Assert(dm.SetHomecomingEntry(candidate, out error), "empty home commander position: " + error);
            var entry = dm.HomecomingAnchor;
            save = AntColony.Save.SaveSnapshot.Capture();
            Assert(AntColony.Save.SavePreflight.Validate(save, out error), "entry preflight: " + error);
            Assert(AntColony.Save.SaveValidator.TryParse(JsonUtility.ToJson(save), out parsed, out error), "entry JSON: " + error);
            await Drain((IEnumerator)restore.Invoke(null, new object[] { parsed })); DropStaleShips();
            Assert(dm.Data.homecomingEntrySet && dm.HomecomingAnchor == entry, "entry saved and restored");
            int people = AntColony.Core.CommanderRoster.Instance.Commanders.Count;
            Call(dm, "TickHomecomings");
            Assert(!dm.Data.homecomings.Any(h => h.commander.PersonalState.id == snapshot.PersonalState.id)
                && AntColony.Core.CommanderRoster.Instance.Commanders.Count == people + 1, "manual safe entry releases pending prisoner");
            Call(dm, "TickHomecomings");
            Assert(AntColony.Core.CommanderRoster.Instance.Commanders.Count == people + 1, "arrival not duplicated");
            dm.ResetHomecomingEntry();
            Assert(!dm.Data.homecomingEntrySet && dm.HomecomingAnchor == AntColony.Core.HomeReturnPoint.Anchor, "reset boundary default");
            AntColony.UI.GameMenuController.Instance.ShowPrisoners(null);
            Assert(AntColony.UI.GameMenuController.Instance.ScreenName.Contains("포로"), "existing prisoner window without camp");
            return $"K2 scene PASS {count}; actual visual widths and hostile landing scenario require UI/NavMesh inspection.";
        }
        finally
        {
            await Drain((IEnumerator)restore.Invoke(null, new object[] { before })); DropStaleShips();
            foreach (var component in added) if (component != null) UnityEngine.Object.Destroy(component);
            Time.timeScale = speed;
        }
    }
}
