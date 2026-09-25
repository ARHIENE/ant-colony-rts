using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;
using ColonyResourceType = AntColony.Data.ResourceType;

// unity command run_script --file AgentScripts/Stage1Checks.cs --entry Stage1Checks.Main (fresh Play mode).
// 2026-09-25 작업 지시서 1단계: 연구 비용, 고치 한도, 수송 정원, 연인 번식, 장수 유지비, 단축키, 저장 유지.
public static class Stage1Checks
{
    static int checks;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL: " + label); checks++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(60);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(30);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static void Move(CommanderAnt c, Vector3 p)
    {
        c.CommandStop();
        Check(NavMesh.SamplePosition(p, out var hit, 10, NavMesh.AllAreas), "walkable test position");
        Check(c.Agent.Warp(hit.position), "commander warp");
    }
    static void Love(CommanderAnt a, CommanderAnt b, float value)
    {
        a.PersonalState.Relation(b.PersonalState.id).value = value;
        b.PersonalState.Relation(a.PersonalState.id).value = value;
    }
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play required");
        var oldRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Stage1Checks-" + Guid.NewGuid().ToString("N"));
        var settings = UserSettings.Current.Clone();
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; isolated.keyBindings = new System.Collections.Generic.List<string>();
        UserSettings.Apply(isolated, false);
        try
        {
            await Ready();
            SaveSystem.NewGame(new NewGameOptions { mapSize = MapSize.Small, seed = 250925, commanderDeath = CommanderDeathMode.Gentle });
            await Ready();
            Object.FindFirstObjectByType<UpkeepManager>().enabled = false;
            Object.FindFirstObjectByType<LocalIncursions>().enabled = false;
            var rm = ResourceManager.Instance;
            foreach (ColonyResourceType type in Enum.GetValues(typeof(ColonyResourceType))) { rm.AddCapacity(type, 10000); rm.Add(type, 10000); }
            var roster = CommanderRoster.Instance;
            foreach (var c in roster.Commanders) c.ApplyTraits(new CommanderTraits(CommanderPersonality.Balanced, 50));

            // 1. 과학 연구량·자원·한글 이름
            var defs = CampaignResearch.Technologies;
            int[] work = { 300, 800, 1800, 4000 }, food = { 50, 100, 200, 400 }, special = { 0, 10, 30, 80 };
            foreach (var d in defs)
                Check(d.Work == work[d.Tier - 1] && d.Food == food[d.Tier - 1] && d.Soil == food[d.Tier - 1] && d.Special == special[d.Tier - 1], "science cost " + d.Technology);
            Check(defs[(int)ScienceTechnology.FungalFarming].Name == "균류 재배" && defs[(int)ScienceTechnology.Engine].Name == "추진기관"
                && defs.All(d => d.Name.Any(ch => ch >= '가' && ch <= '힣')), "Korean science names");
            var labTemplate = Object.FindObjectsByType<ScienceLab>(FindObjectsInactive.Include).First(x => x.name.EndsWith("Template"));
            var lab = Object.Instantiate(labTemplate, roster.Commanders[0].Position + Vector3.forward * 4, Quaternion.identity);
            lab.name = "ScienceLab"; lab.gameObject.SetActive(true);
            Check(lab.TryUpgrade(), "lab tier 2");
            var sp = rm.GetAmount(ColonyResourceType.Special);
            Check(CampaignResearch.Instance.TryStart(ScienceTechnology.Vehicle) && rm.GetAmount(ColonyResourceType.Special) == sp - 10, "tier 2 research spends Special");

            // 2. 비행선 고치 비용·한도
            var world = WorldMapManager.Instance;
            var yardTemplate = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .Invoke(null, new object[] { BuildingKind.AirshipYard, AntColony.Data.UnitRole.Worker });
            var yard = Object.Instantiate(yardTemplate, world.HomePosition + Vector3.left * 12, Quaternion.identity).GetComponent<AirshipYard>();
            yard.name = "AirshipYard"; yard.gameObject.SetActive(true);
            CampaignResearch.Instance.RestoreState(new CampaignResearch.State { completed = { (int)ScienceTechnology.Cocoons } });
            var f0 = rm.GetAmount(ColonyResourceType.Food); var s0 = rm.GetAmount(ColonyResourceType.Soil); sp = rm.GetAmount(ColonyResourceType.Special);
            Check(yard.TryBuild(AirshipPart.Cocoon) && rm.GetAmount(ColonyResourceType.Food) == f0 - 100
                && rm.GetAmount(ColonyResourceType.Soil) == s0 - 50 && rm.GetAmount(ColonyResourceType.Special) == sp - 10 && yard.Remaining == 60, "cocoon cost and time");
            yard.Tick(61);
            for (var i = 1; i < 20; i++) { Check(yard.TryBuild(AirshipPart.Cocoon), "cocoon " + (i + 1)); yard.Tick(61); }
            Check(yard.Cocoons == 20 && !yard.TryBuild(AirshipPart.Cocoon), "21st cocoon blocked");

            // 3. 수송수단 장수 슬롯·일반개미 정원·화물 한도
            Check(world.CanCreateTransport(roster.Commanders[0].Position, out var shipPoint), "transport spawn point");
            var vehicle = world.CreateTransport(false, shipPoint);
            Check(vehicle.CommanderCapacity == 4 && vehicle.Capacity == 40 && vehicle.CargoCapacity == 200, "vehicle capacities");
            var crew = roster.Commanders.Where(c => c.HasTroops).Take(5).ToArray();
            Check(crew.Length == 5, "five commanders with troops");
            foreach (var c in crew) Move(c, vehicle.Position + Vector3.right * 3);
            Check(!vehicle.TryBoard(crew), "five commanders exceed vehicle slots");
            Check(vehicle.TryBoard(crew.Take(4).ToArray()) && vehicle.CommanderLoad == 4 && vehicle.Load == crew.Take(4).Sum(c => c.TroopCount), "four commanders board; troops counted separately");
            Check(!vehicle.TryBoard(new[] { crew[4] }), "5th commander blocked");
            Check(vehicle.TryUnloadCrew(), "unload crew");
            var aircraft = world.CreateTransport(true, shipPoint + Vector3.back * 6);
            Check(aircraft.CommanderCapacity == 8 && aircraft.Capacity == 100 && aircraft.CargoCapacity == 500, "aircraft capacities");
            var before = rm.GetAmount(ColonyResourceType.Food);
            vehicle.DepositResources(ColonyResourceType.Food, 300);
            Check(rm.GetAmount(ColonyResourceType.Food) == before + 200, "vehicle cargo capped at 200");

            // 4. 번식: 연인 쌍만, 양육실 8m, 기분 배율, 중상 정지, 부모-자녀 관계
            var a = roster.Commanders[0]; var b = roster.Commanders[1];
            foreach (var c in roster.Commanders) if (c != a && c != b) Move(c, a.Position + Vector3.right * 30);
            var nursery = new GameObject("Stage1 Nursery").AddComponent<NurseryChamber>();
            nursery.transform.position = a.Position;
            Move(b, a.Position + Vector3.right * 2);
            var count = roster.Count;
            nursery.Tick(60);
            Check(roster.Count == count && nursery.GetAffinity(a, b) == 0, "non-lovers do not breed");
            Love(a, b, 70);
            a.PersonalState.injuries.Add(new CommanderInjury { part = InjuryPart.Legs, severity = InjurySeverity.Serious, remaining = 240 });
            Check(NurseryChamber.BreedMultiplier(a, b) == 0, "serious injury halts breeding");
            a.PersonalState.injuries.Clear();
            a.PersonalState.AddMood("Stage1", 30, 999); b.PersonalState.AddMood("Stage1", 30, 999);
            Check(a.Mood >= 60 && b.Mood >= 60 && NurseryChamber.BreedMultiplier(a, b) == 1.5f, "happy lovers x1.5");
            b.PersonalState.AddMood("Stage1", -80, 999);
            Check(b.Mood <= 30 && NurseryChamber.BreedMultiplier(a, b) == .5f, "sad partner x0.5");
            b.PersonalState.AddMood("Stage1", 30, 999);
            Move(b, a.Position + Vector3.right * 20);
            nursery.Tick(60);
            Check(roster.Count == count, "lover outside nursery radius does not breed");
            Move(b, a.Position + Vector3.right * 2);
            nursery.Tick(10);
            Check(roster.Count == count + 1 && nursery.BirthCount == 1, "lovers near nursery give birth");
            var child = roster.Commanders.Last();
            var toA = child.PersonalState.relations.Find(r => r.otherId == a.PersonalState.id);
            var fromA = a.PersonalState.relations.Find(r => r.otherId == child.PersonalState.id);
            Check(toA != null && fromA != null && toA.value == 40 && fromA.value == 40 && toA.family && fromA.family, "parent-child relation +40");
            Check(a.PersonalState.Relation(b.PersonalState.id).spouse && b.PersonalState.Relation(a.PersonalState.id).spouse, "parents become spouses");
            Love(a, child, 90);
            Check(NurseryChamber.BreedMultiplier(a, child) == 0, "parent and child cannot be lovers");

            // 5. 장수 본인 유지비
            var upkeep = Object.FindFirstObjectByType<UpkeepManager>();
            a.Traits.TryAdd(CommanderTrait.LightEater); b.Traits.TryAdd(CommanderTrait.Glutton);
            var living = roster.Commanders.Where(c => !c.IsDead && !c.IsCaptive).ToArray();
            var expected = AntPool.Instance.Total + Mathf.CeilToInt(living.Sum(c => 2f * c.Traits.FoodMultiplier));
            Check(a.Traits.FoodMultiplier == .7f && b.Traits.FoodMultiplier == 1.5f && upkeep.FoodDue == expected, "commander upkeep with appetite traits");
            vehicle.TryBoard(new[] { crew[2] });
            Check(upkeep.FoodDue == expected, "embarked commander still billed");
            before = rm.GetAmount(ColonyResourceType.Food);
            typeof(UpkeepManager).GetMethod("RunCycle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).Invoke(upkeep, null);
            Check(rm.GetAmount(ColonyResourceType.Food) == before - expected, "upkeep cycle charges home storage");
            vehicle.TryUnloadCrew();

            // 6. 단축키 기본값·재지정
            Check(KeyBindings.Get(GameAction.RotateLeft) == Key.Z && KeyBindings.Get(GameAction.RotateRight) == Key.C
                && KeyBindings.Get(GameAction.Roster) == Key.G && KeyBindings.Get(GameAction.Pause) == Key.P
                && KeyBindings.Get(GameAction.AddTroop) == Key.E && KeyBindings.Get(GameAction.RemoveTroop) == Key.D, "default keys");
            var keys = UserSettings.Current.Clone();
            KeyBindings.Bind(keys, GameAction.Pause, Key.G);
            Check(KeyBindings.Get(keys, GameAction.Pause) == Key.G && KeyBindings.Get(keys, GameAction.Roster) == Key.P, "rebind swaps conflicting key");
            KeyBindings.Bind(keys, GameAction.Pause, Key.Escape);
            Check(KeyBindings.Get(keys, GameAction.Pause) == Key.G, "reserved key rejected");
            var settingsJson = JsonUtility.FromJson<UserSettingsData>(JsonUtility.ToJson(keys));
            Check(KeyBindings.Get(settingsJson, GameAction.Pause) == Key.G, "bindings survive settings json");
            settingsJson.keyBindings[(int)GameAction.Pause] = "99999";
            Check(KeyBindings.Get(settingsJson, GameAction.Pause) == Key.P && !KeyBindings.CanBind((Key)99999), "invalid saved key falls back safely");

            // 7. 저장/불러오기
            foreach (var c in roster.Commanders) c.CommandStop();
            Check(SaveSystem.TrySave(false, 0, out var error), "save accepted: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load accepted: " + error); await Ready();
            var restored = CommanderRoster.Instance.Commanders.First(c => c.PersonalState.id == a.PersonalState.id);
            var restoredChild = CommanderRoster.Instance.Commanders.First(c => c.PersonalState.id == child.PersonalState.id);
            var rel = restored.PersonalState.relations.Find(r => r.otherId == restoredChild.PersonalState.id);
            Check(rel != null && rel.family && restored.PersonalState.relations.Exists(r => r.spouse), "family and spouse survive reload");
            Check(NurseryChamber.BreedMultiplier(restored, restoredChild) == 0, "family block survives reload");
            Check(Object.FindFirstObjectByType<AirshipYard>().Cocoons == 20, "cocoons restored");
            var restoredVehicle = WorldMapManager.Instance.Transports.First(t => !t.Aircraft);
            Check(restoredVehicle.CommanderCapacity == 4 && restoredVehicle.CargoCapacity == 200, "transport capacities after reload");
            return "PASS " + checks + " stage 1 checks";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = oldRoot; UserSettings.Apply(settings, false); }
    }
}
