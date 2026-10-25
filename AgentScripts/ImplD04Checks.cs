using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public static class ImplD04Checks
{
    static int checks;
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string label) { if (!value) throw new Exception("D04 FAIL: " + label); checks++; }
    static void Same(float a, float b, string label) => Check(Mathf.Abs(a - b) < .0001f, label + " " + a + "/" + b);
    static void Set(object target, string field, object value) => target.GetType().GetField(field, Hidden).SetValue(target, value);
    static void Call(AntColony.Units.CommanderAnt c, string method, params object[] args) =>
        typeof(AntColony.Units.CommanderAnt).GetMethod(method, Hidden).Invoke(c, args);
    static float[] Values(AntColony.Units.FormationModifiers m) => new[] {
        m.Attack, m.Armor, m.Move, m.Range, m.AttackSpeed, m.WorldMove, m.RangedReduction, m.AreaReduction, m.RearIncrease };

    static void Rules()
    {
        // Notion 전투 표 + D04 A. 유리한 조건(원거리·산/숲·전방·철수·4적중)에서 14종 전체를 대조한다.
        float[][] expected = {
            new float[9], new[] {.15f,.15f,-.1f,0,0,0,0,0,0}, new[] {.2f,-.1f,.15f,0,0,0,0,0,0},
            new[] {0,.2f,-.1f,0,0,0,0,0,0}, new[] {-.1f,-.1f,.2f,0,0,0,0,0,0},
            new[] {.15f,-.1f,0,.15f,0,0,0,0,0}, new[] {-.1f,.15f,0,0,0,0,.15f,0,0},
            new[] {.2f,.2f,0,0,0,0,0,0,0}, new[] {.15f,0,0,0,0,0,0,0,.15f},
            new[] {-.1f,0,.2f,0,0,.2f,0,0,0}, new[] {.2f,-.1f,0,0,0,0,0,0,0},
            new[] {-.1f,0,0,0,0,0,0,.2f,0}, new[] {-.15f,.15f,0,0,0,0,.15f,0,0},
            new[] {.2f,0,0,0,-.1f,0,0,0,0} };
        Check(AntColony.Units.FormationRules.All.Length == 14, "14 formations");
        foreach (var f in AntColony.Units.FormationRules.All)
        foreach (var legion in new[] { false, true })
        {
            var actual = Values(AntColony.Units.FormationRules.Modifiers(f, legion, true, true, true, true, 4));
            for (int i = 0; i < actual.Length; i++)
            {
                var value = expected[(int)f][i];
                Same(actual[i], value > 0 && i != 8 && legion ? value * 1.5f : value, "table " + f + "/" + i + "/" + legion);
            }
        }
        Same(AntColony.Units.FormationRules.Modifiers(AntColony.Units.Formation.Geese).Attack, 0, "geese melee no attack bonus");
        Same(AntColony.Units.FormationRules.Modifiers(AntColony.Units.Formation.Geese).Range, 0, "geese melee no range bonus");
        Same(AntColony.Units.FormationRules.Modifiers(AntColony.Units.Formation.Snake).Attack, -.1f, "snake outside terrain");
        Same(AntColony.Units.FormationRules.Modifiers(AntColony.Units.Formation.Snake, true).Armor, -.1f, "legion keeps terrain penalty");
        Same(AntColony.Units.FormationRules.Modifiers(AntColony.Units.Formation.Wedge).Attack, 0, "wedge outside front");
        Same(AntColony.Units.FormationRules.Modifiers(AntColony.Units.Formation.Cover).RangedReduction, 0, "cover while attacking");
        Check(AntColony.Units.FormationRules.Front(Vector3.forward, new Vector3(1,0,1)), "45 degree inclusive");
        Check(!AntColony.Units.FormationRules.Front(Vector3.forward, new Vector3(1,0,.99f)), "outside 45 degrees");
        Check(!AntColony.Units.FormationRules.Rear(Vector3.forward, Vector3.right), "90 degrees not rear");
        Check(AntColony.Units.FormationRules.Rear(Vector3.forward, new Vector3(1,0,-.001f)), "over 90 degrees rear");
        Check(!AntColony.Units.FormationRules.Rear(Vector3.zero, Vector3.back), "missing direction not rear");
        Same(AntColony.Units.FormationRules.ReceivedMultiplier(.15f, .9f), .4f, "reductions cap at 75 then increases");
        Same(AntColony.Units.FormationRules.ReceivedMultiplier(.1f + .15f, .15f + .2f), .9f, "same damage sums once");
        Same(AntColony.Units.FormationRules.Stat(100, 10, .2f, .8f, 3), 312, "add then health then rage");
        Same(AntColony.Units.FormationRules.Stat(100, 10, .2f, .8f, .5f), 52, "add then health then exhaustion");
        Check(AntColony.Units.FormationRules.CommandLimit(7, 3, .8f, true) == 32, "D04 command example");
        Check(AntColony.Units.FormationRules.CommandLimit(0, -100, 0, false) == 1, "command minimum");
        Check(!AntColony.Units.FormationRules.CanAllocate(33, 32), "new excess allocation rejected");
        var state = new AntColony.Units.FormationState();
        for (int i = 0; i < 8; i++) state.Hit("first");
        Check(state.hits == 4, "focus capped four hits"); state.Tick(2.99f); Check(state.hits == 4, "focus before timeout");
        state.Tick(.01f); Check(state.hits == 0, "focus expires at three seconds");
        state.Hit("first"); state.Target("second"); Check(state.hits == 0, "focus new target resets before hit");
        var copy = JsonUtility.FromJson<AntColony.Units.CommanderPersonalState>("{}");
        Check(copy.formation == null || copy.formation.selected == AntColony.Units.Formation.Plain, "old JSON default plain");
        state.selected = (AntColony.Units.Formation)99; Check(!state.Valid, "invalid formation rejected");
        foreach (var d in AntColony.Core.FormationResearch.Definitions())
        {
            bool steam = d.Technology <= AntColony.Core.ScienceTechnology.FormationWedge;
            Check(d.Tier == (steam ? 2 : 3) && d.Work == (steam ? 400 : 800)
                && d.Food == (steam ? 100 : 150) && d.Soil == (steam ? 100 : 150) && d.Special == (steam ? 10 : 20)
                && d.Prerequisites.Length == 0, "independent research cost " + d.Technology);
            Check(AntColony.Core.CampaignResearch.Technologies[(int)d.Technology].Technology == d.Technology, "research index append " + d.Technology);
            Check(AntColony.Core.CampaignResearch.Topic(d.Technology) == AntColony.Units.CommanderActivity.Command, "military topic");
        }
    }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(60);
        while (AntColony.Save.SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(20);
        Check(!AntColony.Save.SaveSystem.Busy && AntColony.Core.GameSession.Instance.GameStarted, "session ready");
        Time.timeScale = 0;
    }
    public static async Task<string> Main()
    {
        checks = 0; Rules(); Check(Application.isPlaying, "Play mode required for integration checks");
        var root = AntColony.Save.SaveStorage.RootOverride;
        var settings = AntColony.Core.UserSettings.Current.Clone(); var random = UnityEngine.Random.state;
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; AntColony.Core.UserSettings.Apply(isolated, false);
        AntColony.Save.SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ImplD04-" + Guid.NewGuid().ToString("N"));
        GameObject targetObject = null;
        try
        {
            var candidate = AntColony.Core.StartingCandidate.Roll(0); candidate.traits.values.Clear();
            candidate.talents.levels[(int)AntColony.Units.CommanderActivity.Command] = 0;
            candidate.talents.experience[(int)AntColony.Units.CommanderActivity.Command] = 0;
            AntColony.Core.CrashLandingStart.Begin(new AntColony.Core.NewGameOptions { startingCount = 1, seed = 261025,
                mapSize = AntColony.Core.MapSize.Small, biome = AntColony.Core.MapBiome.Forest, startingTeam = new[] { candidate } });
            await Ready();
            var c = AntColony.Core.CommanderRoster.Instance.Commanders.Single(); c.CommandStop();
            Same(c.EffectiveAttackRange, c.Data.attackRange, "plain uses base attack range without recursion");
            Same(c.EffectiveAttackInterval, c.Data.attackInterval, "plain uses base attack interval");
            c.Talents.levels[(int)AntColony.Units.CommanderActivity.Command] = 0;
            Check(c.FormationAffinities.Count == 1, "command zero one affinity");
            c.Talents.levels[(int)AntColony.Units.CommanderActivity.Command] = 10;
            Check(c.FormationAffinities.Count == 2, "command ten second affinity");
            c.Talents.levels[(int)AntColony.Units.CommanderActivity.Command] = 20;
            Check(c.FormationAffinities.Count == 3, "command twenty third affinity");
            c.Talents.levels[(int)AntColony.Units.CommanderActivity.Command] = 0;
            Check(c.FormationAffinities.Count == 3, "affinities permanent after decline");
            c.Talents.levels[(int)AntColony.Units.CommanderActivity.Command] = 20;
            Check(c.FormationAffinities.Distinct().Count() == 3, "threshold never grants duplicates");
            var missing = AntColony.Units.FormationRules.All.First(f => AntColony.Units.FormationRules.Traditional(f) && !c.FormationAffinities.Contains(f));
            Check(!c.TrySelectFormation(missing), "no traditional affinity denied");
            Check(c.Traits.TryAdd(AntColony.Units.CommanderTrait.Legion), "legion trait added");
            foreach (var f in AntColony.Units.FormationRules.All.Where(AntColony.Units.FormationRules.Traditional))
                Check(c.TrySelectFormation(f), "legion traditional " + f);
            foreach (var f in AntColony.Units.FormationRules.All.Where(f => f >= AntColony.Units.Formation.Line))
                Check(!c.TrySelectFormation(f), "legion still needs research " + f);
            var research = AntColony.Core.CampaignResearch.Instance;
            var researchState = research.CaptureState();
            foreach (var d in AntColony.Core.FormationResearch.Definitions()) researchState.completed.Add((int)d.Technology);
            research.RestoreState(researchState);
            foreach (var f in AntColony.Units.FormationRules.All) Check(c.TrySelectFormation(f), "all unlocked selection " + f);
            Check(c.TrySelectFormation(AntColony.Units.Formation.Column), "column selected");
            Same(c.FormationWorldMoveMultiplier, 1.3f, "legion walking column speed");
            Check(c.TrySelectFormation(AntColony.Units.Formation.Focus), "focus selected before mobilize");
            AntColony.Core.AntPool.Instance.Breed(50);
            Check(AntColony.Core.AntPool.Instance.TryAssign(5), "allocate five existing citizens");
            Call(c, "Mobilize", 5, c.Position);
            Check(!c.TrySelectFormation(AntColony.Units.Formation.Plain), "cannot change while deployed");
            targetObject = new GameObject("D04 focus target"); targetObject.transform.position = c.Position + Vector3.forward;
            var target = targetObject.AddComponent<AntColony.World.WildMonster>();
            Set(target, "maxHealth", 10000f); Set(target, "currentHealth", 10000f);
            c.CommandAttack(target);
            for (int i = 0; i < 4; i++) Call(c, "DealDamage", target);
            Check(c.FormationState.hits == 4, "actual general attacks accumulate focus");
            c.FormationState.Tick(.5f);
            var before = AntColony.Save.SaveSnapshot.Capture();
            Check(before.commanders[0].personalState.formation.hitTarget.StartsWith("saved:"), "focus stable save target reference");
            Check(AntColony.Save.SaveValidator.Validate(before, out var error), "snapshot valid: " + error);
            Check(AntColony.Save.SaveSystem.TrySave(false, 0, out error), "disk save accepted: " + error);
            Check(AntColony.Save.SaveSystem.TryLoad(AntColony.Save.SaveSlots.PathFor(false, 0), out error), "disk load accepted: " + error);
            await Ready();
            c = AntColony.Core.CommanderRoster.Instance.Commanders.Single();
            var after = AntColony.Save.SaveSnapshot.Capture();
            Check(before.version == after.version, "save version unchanged");
            Check(c.SelectedFormation == AntColony.Units.Formation.Focus && c.FormationState.hits == 4, "selected formation and focus roundtrip");
            Same(c.FormationState.sinceHit, .5f, "focus timeout remainder roundtrip");
            Check(c.FormationAffinities.Count == 3, "affinities roundtrip");
            Check((c.FormationState.forward - before.commanders[0].personalState.formation.forward).sqrMagnitude < .0001f, "facing roundtrip");
            Check(after.commanders[0].personalState.formation.hitTarget.StartsWith("saved:"), "focus reconnected after load");
            foreach (var d in AntColony.Core.FormationResearch.Definitions()) Check(AntColony.Core.CampaignResearch.Instance.Has(d.Technology), "research roundtrip " + d.Name);
            c.FormationState.selected = AntColony.Units.Formation.Dispersed;
            var health = c.CurrentHealth; AntColony.Core.FormationDamage.Apply(c, .5f, c.Position, area: true);
            Same(health - c.CurrentHealth, .35f, "legion area reduction after armor");
            c.FormationState.selected = AntColony.Units.Formation.Line; c.FormationState.forward = Vector3.forward;
            health = c.CurrentHealth; AntColony.Core.FormationDamage.Apply(c, .5f, c.Position + Vector3.back);
            Same(health - c.CurrentHealth, .575f, "rear penalty not enhanced by legion");
            var troops = c.TroopCount;
            c.Talents.levels[(int)AntColony.Units.CommanderActivity.Command] = 0;
            Call(c, "RestoreTroops", c.CommandLimit + 1, 0f);
            var excess = c.TroopCount;
            Call(c, "TickFormation", 0f);
            Check(c.TroopCount == excess && !c.TryAssign(1), "over limit retained and new reinforcement blocked");
            Call(c, "RestoreTroops", troops, 0f);
            // 구저장의 formation 필드가 없던 상황을 실제 로더에 전달한다.
            var legacy = after; legacy.commanders[0].personalState.formation = null;
            var path = Path.Combine(AntColony.Save.SaveStorage.Root, "d04-legacy.json");
            Check(AntColony.Save.SaveValidator.Validate(legacy, out error), "old missing formation validates: " + error);
            AntColony.Save.SaveStorage.WriteAtomic(path, JsonUtility.ToJson(legacy));
            Check(AntColony.Save.SaveSystem.TryLoad(path, out error), "old load accepted: " + error); await Ready();
            Check(AntColony.Core.CommanderRoster.Instance.Commanders.Single().SelectedFormation == AntColony.Units.Formation.Plain, "old save loads plain");
            var result = "IMPL-D04 " + checks + " checks passed"; File.WriteAllText("Temp/save-d04-result.txt", result); return result;
        }
        finally
        {
            if (targetObject != null) UnityEngine.Object.Destroy(targetObject);
            UnityEngine.Random.state = random;
            AntColony.Save.SaveStorage.RootOverride = root; AntColony.Core.UserSettings.Apply(settings, false);
        }
    }
}
