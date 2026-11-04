using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;

// 2026-10-05 위생 욕구(욕구 4종) + 전력 1차(쳇바퀴·장작 발전기·전선·배터리·전등) 흐름 확인.
public static class HygienePowerChecks
{
    static int checks;
    static readonly List<GameObject> made = new List<GameObject>();
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL: " + label); checks++; }
    static async Task Ready() { for (int i = 0; i < 1800 && SaveSystem.Busy; i++) await Task.Delay(50); Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0; }
    static T Put<T>(BuildingKind k, Vector3 p) where T : Component
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { k, UnitRole.Worker });
        Check(template != null, "template " + k);
        var go = Object.Instantiate(template, p, Quaternion.identity); go.SetActive(true); made.Add(go); return go.GetComponent<T>();
    }

    public static async Task<string> Main()
    {
        checks = 0; made.Clear(); Check(Application.isPlaying, "play mode");
        string original = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "HygienePower-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 261005, mapSize = MapSize.Small }); await Ready();
            var rm = ResourceManager.Instance;
            foreach (AntColony.Data.ResourceType type in Enum.GetValues(typeof(AntColony.Data.ResourceType))) { rm.AddCapacity(type, 10000); rm.Add(type, 10000); }
            var c = CommanderRoster.Instance.Commanders.First(x => x.CivilianWorkReady && x.CanReceiveOrders);
            foreach (var other in CommanderRoster.Instance.Commanders) if (other != c) other.gameObject.SetActive(false); // 다른 장수가 가구를 차지하지 않게
            c.CommandStop();

            // 1. 위생: 가구가 없으면 참고 기분 하락, 화장실이 옆에 있으면 가서 채운다.
            c.PersonalState.hygiene.hygiene = 20; c.PersonalState.meal.satiety = 100;
            c.TickDuty(.1f);
            Check(c.PersonalState.moodFactors.Any(f => f.reason == "위생 불량"), "low hygiene lowers mood");
            Check(c.Hygiene < 20 && !c.IsWashing, "no fixture: holds it");
            var toilet = Put<HygieneFixture>(BuildingKind.Toilet, c.Position + Vector3.right * 2);
            c.TickDuty(.1f);
            Check(c.IsWashing && c.WashSpot == toilet && !toilet.Free, "goes to toilet");
            c.TickDuty(GameBalance.WashSeconds);
            Check(c.Hygiene > 70 && !c.IsWashing && toilet.Free, "toilet restores hygiene");
            c.TickDuty(.1f);
            Check(!c.PersonalState.moodFactors.Any(f => f.reason == "위생 불량"), "mood penalty cleared");
            Check(RoomSystem.KindOf(toilet) == RoomKind.Bathroom, "toilet requires bathroom");

            // 2. 전력: 장작 발전기 → 전선 → 전등. 끊기면 꺼진다.
            var o = c.Position + new Vector3(12, 0, 12); o = new Vector3(Mathf.Floor(o.x) + .5f, o.y, Mathf.Floor(o.z) + .5f);
            var generator = Put<PowerNode>(BuildingKind.WoodGenerator, o + new Vector3(.5f, 0, .5f)); // 2×2 = 칸 n..n+1
            var wire1 = Put<PowerNode>(BuildingKind.PowerWire, o + new Vector3(2, 0, 0));
            var wire2 = Put<PowerNode>(BuildingKind.PowerWire, o + new Vector3(3, 0, 0));
            var lamp = Put<PowerNode>(BuildingKind.ElectricLamp, o + new Vector3(4, 0, 0));
            PowerGrid.MarkDirty();
            Check(PowerGrid.NetworkOf(lamp).Contains(generator), "wire connects generator and lamp");
            var soil = rm.GetAmount(AntColony.Data.ResourceType.Soil);
            typeof(PowerNode).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(generator, null); // 장작 넣기
            Check(generator.Output == GameBalance.WoodGeneratorWatts && rm.GetAmount(AntColony.Data.ResourceType.Soil) == soil - 1, "wood generator burns material");
            PowerGrid.Tick(PowerGrid.TickSeconds);
            Check(lamp.Powered, "lamp powered through wires");
            made.Remove(wire2.gameObject); Object.DestroyImmediate(wire2.gameObject); PowerGrid.MarkDirty(); PowerGrid.Tick(PowerGrid.TickSeconds);
            Check(!lamp.Powered && !PowerGrid.NetworkOf(lamp).Contains(generator), "cut wire turns lamp off");

            // 3. 배터리: 남는 전력을 저장하고, 발전이 멈추면 꺼내 쓴다.
            var battery = Put<PowerNode>(BuildingKind.Battery, o + new Vector3(2, 0, 1)); // 발전기·전선1 둘 다와 맞닿음
            PowerGrid.MarkDirty(); PowerGrid.Tick(1);
            Check(Mathf.Abs(battery.Charge - GameBalance.WoodGeneratorWatts) < .01f, "battery stores surplus");
            var wire3 = Put<PowerNode>(BuildingKind.PowerWire, o + new Vector3(3, 0, 0)); PowerGrid.MarkDirty();
            made.Remove(generator.gameObject); Object.DestroyImmediate(generator.gameObject); PowerGrid.MarkDirty();
            PowerGrid.Tick(1);
            Check(lamp.Powered && Mathf.Abs(battery.Charge - (GameBalance.WoodGeneratorWatts - GameBalance.LampWatts)) < .01f, "battery feeds lamp");

            // 4. 쳇바퀴: 장수가 운반 작업으로 뛰면 발전, 근력 경험치·피로가 오른다.
            var w = c.Position + Vector3.forward * 3; var wheel = Put<PowerNode>(BuildingKind.Treadmill, new Vector3(Mathf.Floor(w.x), w.y, Mathf.Floor(w.z) + .5f)); // 2×1
            var wheelWire = Put<PowerNode>(BuildingKind.Battery, wheel.Position + Vector3.right * 1.5f); PowerGrid.MarkDirty();
            Check(wheel.NeedsRunner && RoomSystem.KindOf(wheel) == RoomKind.PowerPlant, "treadmill needs a runner");
            var strength = (c.Talents.Level(CommanderActivity.Strength) * 100000f + c.Talents.Xp(CommanderActivity.Strength)); var fatigue = c.Fatigue;
            Check(c.StartService(wheel, CommanderJobs.Hauling), "commander takes treadmill (hauling job)");
            c.TickDuty(1);
            Check(c.ServiceTarget == wheel && wheel.Runner == c && wheel.Output == GameBalance.TreadmillWatts, "running generates power");
            Check((c.Talents.Level(CommanderActivity.Strength) * 100000f + c.Talents.Xp(CommanderActivity.Strength)) > strength && c.Fatigue > fatigue, "strength xp and fatigue rise");
            PowerGrid.Tick(1); Check(wheelWire.Charge > 0, "treadmill charges battery");
            c.CommandStop();

            // 5. 저장: 위생·배터리 충전량
            c.PersonalState.hygiene.hygiene = 42;
            Check(SaveSystem.TrySave(false, 0, out var error), "save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error); await Ready();
            var restored = CommanderRoster.Instance.Commanders.First(x => x.CommanderName == c.CommanderName);
            Check(Mathf.Abs(restored.Hygiene - 42) < .5f, "hygiene saved");
            Check(PowerNode.All.Any(p => p.IsBattery && p.Charge > 50), "battery charge saved");
            Check(HygieneFixture.All.Count == 1, "toilet saved");
            return "PASS " + checks + " hygiene/power checks";
        }
        finally
        {
            foreach (var go in made) if (go != null) Object.Destroy(go);
            SaveStorage.RootOverride = original;
        }
    }
}
