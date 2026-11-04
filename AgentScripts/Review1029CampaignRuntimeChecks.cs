using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

// Claude가 격리된 새 게임에서 실행한다. 이 J2 작업에서는 Unity를 실행하지 않는다.
public static class Review1029CampaignRuntimeChecks
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private static object Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Hidden).Invoke(obj, args);
    public static async Task<string> Main()
    {
        await Task.CompletedTask;
        if (!Application.isPlaying || AntColony.Core.CommanderRoster.Instance == null) throw new Exception("isolated Play scene required");
        var roster = AntColony.Core.CommanderRoster.Instance.Commanders.ToArray();
        if (roster.Length < 2) throw new Exception("isolated scene needs two commanders");
        var before = AntColony.Save.SaveSnapshot.Capture();
        if (!AntColony.Save.SavePreflight.Validate(before, out var error)) throw new Exception("baseline save: " + error);
        int count = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); count++; }
        float speed = Time.timeScale; Time.timeScale = 0;
        var random = UnityEngine.Random.state;
        var c = roster[0]; var other = roster[1];
        var traits = c.Traits.values.ToArray();
        var personal = c.CapturePersonalState(); var otherPersonal = other.CapturePersonalState();
        int troops = c.TroopCount; float pending = (float)typeof(AntColony.Units.CommanderAnt).GetProperty("PendingTroopDamage", Hidden).GetValue(c);
        var positions = new[] { c.Position, other.Position };
        var population = AntColony.Core.ColonyPopulation.Instance.Capture();
        var pool = AntColony.Core.AntPool.Instance;
        var counts = new[] { pool.Free, pool.Assigned, pool.Reserved };
        var campaign = AntColony.Core.RocketCampaign.Instance.Capture();
        var research = AntColony.Core.CampaignResearch.Instance.CaptureState();
        var history = AntColony.Core.CampaignHistory.Instance?.Capture();
        var boss = AntColony.World.WorldMapManager.Instance.Sites.First(s => s.GetComponent<AntColony.World.BossStronghold>()?.Id == AntColony.Boss.BossId.A).Boss;
        var encounter = boss.GetComponent<AntColony.Boss.BossEncounter>(); var encounterState = encounter.Capture();
        var bossPosition = boss.Position;
        GameObject rocketObject = null; AntColony.Data.BuildingData definition = null;
        AntColony.Buildings.BuildingConstructionSite rebuildSite = null;
        Action<AntColony.Buildings.AirshipYard, AntColony.Core.RocketSnapshot, float> assault = (y, s, dt) => { };
        AntColony.Core.RocketCampaign.AssaultTick += assault;
        try
        {
            c.CommandStop(); other.CommandStop();
            c.Traits.values.Clear();
            var clean = c.CapturePersonalState(); clean.health = new AntColony.Units.HealthState(); clean.work = new AntColony.Units.CommanderWorkState();
            clean.mentalBreak = AntColony.Units.MentalBreak.None; clean.rageRemaining = 0; clean.dead = false;
            c.RestorePersonalState(clean);
            foreach (int n in new[] { 0, 1, 5, 10, 20 })
            {
                Call(c, "RestoreTroops", n, 0f); float attack = c.AttackDamage;
                Call(c, "RestoreTroops", 1, 0f);
                Check(Mathf.Abs(attack - c.AttackDamage * Mathf.Max(1, n)) < .001f, "actual commander attack troop scale " + n);
            }
            Call(c, "RestoreTroops", 0, 0f);
            Call(pool, "RestoreCounts", 20, 0, 0);
            var p = new AntColony.Core.ColonyPopulation.State { ages = new int[65] }; p.ages[20] = 20;
            p.civilianLocations.Add(new AntColony.Core.CivilianLocation { id = 1, count = 20, arrived = true, position = c.Position }); p.nextCivilianLocation = 2;
            AntColony.Core.ColonyPopulation.Instance.Restore(p);
            rocketObject = new GameObject("Review1029 J2 rocket"); rocketObject.SetActive(false); rocketObject.transform.position = c.Position;
            var yard = rocketObject.AddComponent<AntColony.Buildings.AirshipYard>();
            definition = ScriptableObject.CreateInstance<AntColony.Data.BuildingData>(); definition.kind = AntColony.Data.BuildingKind.AirshipYard;
            definition.maxHealth = 300; definition.foodCost = 100; definition.soilCost = 150; definition.buildTimeSeconds = 10;
            typeof(AntColony.Buildings.BuildingBase).GetMethod("ConfigureRuntime", Hidden).Invoke(yard, new object[] { definition });
            yard.RestoreState(new AntColony.Buildings.AirshipYard.State { hull = true, engine = true, fuelType = (int)AntColony.Data.ResourceType.Petroleum, fuelUnits = 15 }, roster);
            rocketObject.SetActive(true);
            var r = AntColony.Core.CampaignResearch.Instance.CaptureState();
            r.bosses.revealed[3] = r.bosses.obtained[3] = r.bosses.delivered[3] = true; r.bosses.claimed[3] = 0; r.bosses.home[3] = 1;
            AntColony.Core.CampaignResearch.Instance.RestoreState(r);
            var economy = AntColony.Core.RocketCampaign.Instance.Economy;
            economy.Observe(AntColony.Core.GameCalendar.GameSeconds, 100, 22);
            Check(yard.TryActivate(100, 22) && yard.PreparationRemaining == 2250, "actual activation consumes imported core and starts full15days");
            Check(AntColony.Core.CampaignResearch.Instance.BossProgress.Home(AntColony.Boss.BossId.D) == 0, "actual core consumed once");
            Check(!yard.TryBoard(c), "boarding blocked during preparation");
            var ready = yard.CaptureState(roster); ready.phase = AntColony.Buildings.RocketPhase.Boarding; ready.preparationRemaining = 0;
            yard.RestoreState(ready, roster);
            Check(yard.TryBoard(1) == 9 && yard.OccupiedSeats == 9, "civilian boarding reserves commander seat");
            Check(!yard.TryDepart(), "citizens alone cannot launch");
            c.PersonalState.Relation(other.PersonalState.id).spouse = true;
            Check(yard.TryBoard(c) && yard.HasLaunchCommander, "commander fills reserved seat");
            Check(yard.TryDisembark(c) && !yard.HasLaunchCommander && yard.CivilianPassengers == 9 && !yard.TryDepart(), "individual defense redeployment retains civilians and blocks empty commander launch");
            Check(yard.TryBoard(c), "commander can reboard manually");
            var ending = AntColony.Core.CampaignOutcome.Capture(yard.Passengers);
            Check(ending.citizensEscaped == 9 && ending.legacy.Any(line => line.Contains("배우자") && line.Contains("탈출") && line.Contains("잔류")), "relationship records split outcome");
            var frozen = JsonUtility.FromJson<AntColony.Core.CampaignOutcome.State>(JsonUtility.ToJson(ending));
            c.PersonalState.relations.Clear(); Check(frozen.legacy.SequenceEqual(ending.legacy), "ending narrative frozen on save");
            if (!c.Traits.Has(AntColony.Units.CommanderTrait.DiamondBuilding)) c.Traits.values.Add(AntColony.Units.CommanderTrait.DiamondBuilding);
            var contributions = new AntColony.Core.WorkContribution();
            contributions.Add(c.PersonalState.id, 7.49f, true);
            Call(yard, "AcceptBabelFoundation", new AntColony.Buildings.BabelState { work = 10, eligible = contributions.Majority(10) != null }, contributions);
            Check(!yard.CaptureState(roster).babelEligible, "74.9 percent insufficient");
            contributions.Add(c.PersonalState.id, .02f, true);
            Call(yard, "AcceptBabelFoundation", new AntColony.Buildings.BabelState { work = 10, eligible = contributions.Majority(10) != null }, contributions);
            Check(yard.CaptureState(roster).babelEligible, "common individual75percent qualifies");
            Check(yard.TryDepart(), "launch queued with commander");
            yard.TakeDamage(100000);
            Call(yard, "ResolveLaunch");
            Check(yard.Phase == AntColony.Buildings.RocketPhase.Destroyed && !AntColony.Core.CampaignResearch.Instance.Departed, "same tick destruction wins");
            Check(yard.CivilianPassengers == 0 && pool.Free == 11, "dead civilian passengers never return to pool");
            Check(!c.IsDead && !c.IsEmbarked && c.PersonalHealth == 0 && c.Health.Serious, "passenger survives wounded and needs rescue");
            Check(yard.HasFoundation && yard.TryRebuildFoundation(), "manual foundation rebuild request");
            rebuildSite = AntColony.Core.SceneObjects.All<AntColony.Buildings.BuildingConstructionSite>().First(s => s.Building == rocketObject);
            Check(rebuildSite.BuildTimeSeconds == 5 && rebuildSite.Needs.Single(n => n.type == yard.MainMaterial).need == 113, "foundation half work and rounded75percent materials");
            var fresh = yard.CaptureState(roster);
            Check(!fresh.hull && !fresh.engine && fresh.cocoons == 0 && fresh.fuelUnits == 0 && fresh.foundationUsed
                && !AntColony.Core.RocketSnapshot.Present(fresh.snapshot), "rebuild keeps no modules fuel core or defense progress");
            var saved = AntColony.Save.SaveSnapshot.Capture();
            var savedRebuild = saved.construction.Single(d => d.name == "로켓 기초 재건");
            Check(savedRebuild.hasRocketState && savedRebuild.rocket?.foundationUsed == true, "construction DTO preserves used foundation");
            var savedCopy = JsonUtility.FromJson<AntColony.Save.SaveFileV1>(JsonUtility.ToJson(saved));
            Check(AntColony.Save.SavePreflight.Validate(savedCopy, out error), "whole save preflight after rocket failure/rebuild: " + error);
            other.transform.position = boss.Position + Vector3.back;
            other.WorkState.health = 100; other.PersonalState.health = new AntColony.Units.HealthState();
            encounter.Restore(new AntColony.Boss.BossEncounter.State { taken = true, elapsed = 0, warning = true, warningRemaining = .5f, pulseRemaining = 6.5f, strike = other.Position });
            foreach (AntColony.Data.UnitRole role in Enum.GetValues(typeof(AntColony.Data.UnitRole)))
                Check(AntColony.Core.CombatTargeting.CanAttack(role, boss, true) == (role == AntColony.Data.UnitRole.Ranged || role == AntColony.Data.UnitRole.Flying), "actual airborne boss targeting " + role);
            float health = other.PersonalHealth;
            encounter.Tick(.6f); Check(other.PersonalHealth < health, "saved telegraph hits actual commander once");
            var resolvedHealth = other.PersonalHealth; encounter.Tick(.01f);
            Check(other.PersonalHealth == resolvedHealth, "resolved telegraph not repeated");
            savedCopy = JsonUtility.FromJson<AntColony.Save.SaveFileV1>(JsonUtility.ToJson(before));
            savedCopy.monsters.First(m => m.key.StartsWith("boss:")).bossEncounter = new AntColony.Boss.BossEncounter.State { taken = true, elapsed = float.NaN };
            Check(!AntColony.Save.SavePreflight.Validate(savedCopy, out error), "save preflight rejects invalid encounter timer");
            return $"PASS {count} J2 runtime checks";
        }
        finally
        {
            if (rebuildSite != null) UnityEngine.Object.DestroyImmediate(rebuildSite.gameObject);
            if (rocketObject != null) UnityEngine.Object.DestroyImmediate(rocketObject);
            if (definition != null) UnityEngine.Object.DestroyImmediate(definition);
            c.Traits.values.Clear(); c.Traits.values.AddRange(traits);
            c.RestorePersonalState(personal); other.RestorePersonalState(otherPersonal);
            Call(c, "RestoreTroops", troops, pending); c.transform.position = positions[0]; other.transform.position = positions[1];
            Call(pool, "RestoreCounts", counts[0], counts[1], counts[2]); AntColony.Core.ColonyPopulation.Instance.Restore(population);
            AntColony.Core.CampaignResearch.Instance.RestoreState(research); AntColony.Core.RocketCampaign.Instance.Restore(campaign);
            if (history != null) AntColony.Core.CampaignHistory.Instance.Restore(history);
            encounter.Restore(encounterState); boss.transform.position = bossPosition;
            AntColony.Core.RocketCampaign.AssaultTick -= assault; UnityEngine.Random.state = random; Time.timeScale = speed;
        }
    }
}
