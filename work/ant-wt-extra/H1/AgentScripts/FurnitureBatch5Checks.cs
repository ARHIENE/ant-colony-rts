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

// 2026-10-06 가구 5차: 2층침대·해먹·큰 식탁 / 휴게 8종 / 장식 4종(조각상·깃발·그림·기둥).
public static class FurnitureBatch5Checks
{
    static int checks;
    static readonly List<GameObject> made = new List<GameObject>();
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL: " + label); checks++; }
    static async Task Ready() { for (int i = 0; i < 1800 && SaveSystem.Busy; i++) await Task.Delay(50); Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0; }
    static GameObject Template(BuildingKind k) => (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { k, UnitRole.Worker });
    static T Put<T>(BuildingKind k, Vector3 p) where T : Component
    {
        var go = Object.Instantiate(Template(k), p, Quaternion.identity); go.SetActive(true); made.Add(go); return go.GetComponent<T>();
    }
    static async Task Remove(Component c) { made.Remove(c.gameObject); Object.Destroy(c.gameObject); await Task.Yield(); await Task.Yield(); }
    static float Xp(CommanderAnt c, CommanderActivity a) => c.Talents.Level(a) * 100000f + c.Talents.Xp(a);
    static float Rel(CommanderAnt a, CommanderAnt b) => a.PersonalState.relations.Find(r => r.otherId == b.PersonalState.id)?.value ?? 0;

    static readonly BuildingKind[] Rec = { BuildingKind.BoardGame, BuildingKind.Janggi, BuildingKind.Baduk, BuildingKind.WrestlingRing,
        BuildingKind.DartBoard, BuildingKind.ExerciseRig, BuildingKind.Instrument, BuildingKind.WebSwing };
    static readonly BuildingKind[] Deco = { BuildingKind.Statue, BuildingKind.Flag, BuildingKind.Painting, BuildingKind.Pillar };

    public static async Task<string> Main()
    {
        checks = 0; made.Clear(); Check(Application.isPlaying, "play mode");
        string original = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Furniture5-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 261010, mapSize = MapSize.Small }); await Ready();
            var rm = ResourceManager.Instance;
            foreach (AntColony.Data.ResourceType type in Enum.GetValues(typeof(AntColony.Data.ResourceType))) { rm.AddCapacity(type, 10000); rm.Add(type, 10000); }

            // 1. 템플릿·컴포넌트·카탈로그·잠금
            foreach (var k in new[] { BuildingKind.BunkBed, BuildingKind.Hammock }) Check(Template(k)?.GetComponent<Dormitory>()?.Data.kind == k, "bed template " + k);
            Check(Template(BuildingKind.BigTable)?.GetComponent<Kitchen>()?.Data.kind == BuildingKind.BigTable, "big table template");
            foreach (var k in Rec) Check(Template(k)?.GetComponent<RecreationSpot>()?.Data.kind == k && BuildingPlacementController.LockReason(k) != null, "recreation template + research lock " + k);
            foreach (var k in Deco) Check(Template(k)?.GetComponent<Decoration>()?.Data.kind == k && Decoration.IsKind(k), "decoration template " + k);
            Check(Template(BuildingKind.Kitchen).GetComponent<BuildingBase>().Data.kind == BuildingKind.Kitchen && Template(BuildingKind.Dormitory).GetComponent<BuildingBase>().Data.kind == BuildingKind.Dormitory, "base templates kept");
            Check(new[] { BuildingKind.BunkBed, BuildingKind.Hammock, BuildingKind.BigTable }.Concat(Rec).Concat(Deco).All(k => FurnitureCatalog.For(k) != null), "catalog linked");
            Check(RecreationSpot.KindCount >= 12, "batch 5 recreation kinds present");

            // 2. 방: 2층침대 혼자 = 숙소, 해먹 혼자 = 개인실, 큰 식탁 = 식당, 놀이판 2종 = 휴게실, 기둥 = 방 점수
            var home = Object.FindAnyObjectByType<Stockpile>().Position;
            var o = new Vector3(Mathf.Floor(home.x) + 20, home.y, Mathf.Floor(home.z) + 20);
            for (var x = 0; x < 6; x++) for (var z = 0; z < 6; z++)
            {
                if (x != 0 && x != 5 && z != 0 && z != 5) continue;
                var p = o + new Vector3(x + .5f, .75f, z + .5f);
                if (x == 0 && z == 2) Put<Door>(BuildingKind.Door, p); else Put<Wall>(BuildingKind.LeafWall, p);
            }
            await Task.Yield();
            var inside = o + new Vector3(2.5f, .5f, 2.5f);
            var bunk = Put<Dormitory>(BuildingKind.BunkBed, o + new Vector3(2, .8f, 2)); await Task.Yield();
            Check(bunk.Beds == 2 && bunk.RoomBedCount == 2 && RoomSystem.RoomAt(inside).Kind == RoomKind.Bedroom, "bunk bed alone = bedroom");
            await Remove(bunk);
            var hammock = Put<Dormitory>(BuildingKind.Hammock, o + new Vector3(2, .3f, 2)); await Task.Yield();
            Check(hammock.Beds == 1 && hammock.IsHammock && !hammock.IsMat && RoomSystem.RoomAt(inside).Kind == RoomKind.PrivateRoom, "hammock alone = private room");
            await Remove(hammock);
            var table = Put<Kitchen>(BuildingKind.BigTable, o + new Vector3(2.5f, .4f, 2.5f)); await Task.Yield();
            Check(table.IsTable && !table.NeedsCook && RoomSystem.RoomAt(inside).Kind == RoomKind.Dining, "big table = dining, no cooking");
            await Remove(table);
            var board = Put<RecreationSpot>(BuildingKind.BoardGame, o + new Vector3(1.5f, .3f, 1.5f));
            var dart = Put<RecreationSpot>(BuildingKind.DartBoard, o + new Vector3(3.5f, .75f, 3.5f)); await Task.Yield();
            Check(RoomSystem.RoomAt(inside).Kind == RoomKind.Recreation, "two recreation kinds = rec room");
            var before = RoomSystem.RoomAt(inside).Score;
            var pillar = Put<Decoration>(BuildingKind.Pillar, o + new Vector3(1.5f, 1.1f, 3.5f)); await Task.Yield();
            Check(RoomSystem.RoomAt(inside).Score > before && pillar.MoodBonus == 0 && Decoration.CountNear(pillar.Position) == 0, "pillar raises room score only");
            await Remove(board); await Remove(dart); await Remove(pillar);

            // 3. 장식 기분
            var statue = Put<Decoration>(BuildingKind.Statue, o + new Vector3(-10, .9f, -10));
            var flag = Put<Decoration>(BuildingKind.Flag, o + new Vector3(-12, 1, -10));
            var painting = Put<Decoration>(BuildingKind.Painting, o + new Vector3(-14, .5f, -10));
            Check(statue.MoodBonus == 4 && flag.MoodBonus == 2 && painting.MoodBonus == 3, "decoration moods 4/2/3");
            await Remove(statue); await Remove(flag); await Remove(painting);

            // 4. 이용 효과(혼자): 장기판 지휘 / 가시 다트 원거리 / 운동기구 근력 / 악기 회복 1.5배
            var all = CommanderRoster.Instance.Commanders.Where(x => x.CivilianWorkReady && x.CanReceiveOrders).ToList();
            var c = all[0]; var d = all[1];
            foreach (var other in CommanderRoster.Instance.Commanders) if (other != c && other != d) other.gameObject.SetActive(false);
            d.gameObject.SetActive(false);
            foreach (var x in new[] { c, d }) { x.CommandStop(); x.PersonalState.meal.satiety = 100; x.PersonalState.hygiene.hygiene = 100; }
            async Task<float> Play(CommanderAnt who, BuildingKind k, CommanderActivity a)
            {
                var spot = Put<RecreationSpot>(k, who.Position + Vector3.right * 2);
                who.PersonalState.joy.joy = 10; who.PersonalState.joy.retrySeconds = 0; who.PersonalState.joy.boredom = new float[RecreationSpot.KindCount];
                var xp = Xp(who, a); who.TickDuty(.1f);
                Check(who.IsPlaying && who.PlaySpot == spot, "plays at " + k);
                who.TickDuty(GameBalance.PlaySeconds);
                Check(!who.IsPlaying, "finished " + k);
                await Remove(spot);
                return Xp(who, a) - xp;
            }
            Check(await Play(c, BuildingKind.Janggi, CommanderActivity.Command) > 0, "janggi gives command xp");
            Check(await Play(c, BuildingKind.Baduk, CommanderActivity.Command) > 0, "baduk gives command xp");
            Check(await Play(c, BuildingKind.DartBoard, CommanderActivity.Ranged) > 0, "dart gives ranged xp");
            Check(await Play(c, BuildingKind.ExerciseRig, CommanderActivity.Strength) > 0, "exercise gives strength xp");
            await Play(c, BuildingKind.Instrument, CommanderActivity.Art);
            Check(c.Joy > 10 + GameBalance.PlayJoy, "instrument restores more joy: " + c.Joy);
            await Play(c, BuildingKind.WebSwing, CommanderActivity.Art);
            Check(c.Joy > 10, "web swing restores joy");

            // 4-1. 큰 식탁: 멀리 있는 화덕의 비축 식사를 집어 식탁에서 먹는다.
            var hearth = Put<Kitchen>(BuildingKind.Hearth, c.Position + new Vector3(25, .6f, 0));
            hearth.Meals.meals.Add(new Kitchen.Meal { quality = 2, skill = 10 });
            var eatTable = Put<Kitchen>(BuildingKind.BigTable, c.Position + Vector3.right * 2);
            c.PersonalState.meal.satiety = 0; c.PersonalState.meal.retrySeconds = 0; c.TickDuty(.1f);
            Check(hearth.Meals.meals.Count == 0 && c.PersonalState.meal.eatSeconds > 0 && c.PersonalState.meal.quality == 2 && eatTable.Meals.meals.Count == 0, "eats hearth meal at big table");
            c.TickDuty(GameBalance.EatSeconds * 2);
            await Remove(hearth); await Remove(eatTable);

            // 5. 2인: 놀이판 관계 +, 씨름판 라이벌 관계 -
            d.gameObject.SetActive(true); d.CommandStop(); d.transform.position = c.Position + Vector3.forward;
            async Task Pair(BuildingKind k)
            {
                var spot = Put<RecreationSpot>(k, c.Position + Vector3.right * 2);
                Check(spot.Seats == 2, "two seats " + k);
                foreach (var x in new[] { c, d }) { x.PersonalState.joy.joy = 10; x.PersonalState.joy.retrySeconds = 0; x.TickDuty(.1f); }
                Check(c.PlaySpot == spot && d.PlaySpot == spot, "both play " + k);
                c.TickDuty(GameBalance.PlaySeconds); d.TickDuty(GameBalance.PlaySeconds);
                await Remove(spot);
            }
            var rel = Rel(c, d);
            await Pair(BuildingKind.BoardGame);
            Check(Rel(c, d) > rel, "board game raises relation");
            c.PersonalState.Relation(d.PersonalState.id).value = -50; d.PersonalState.Relation(c.PersonalState.id).value = -50;
            await Pair(BuildingKind.WrestlingRing);
            Check(Rel(c, d) < -50, "rival wrestling lowers relation");

            // 6. 다양한 오락 기분 상한 10
            c.PersonalState.joy.boredom = Enumerable.Repeat(.5f, RecreationSpot.KindCount).ToArray(); c.PersonalState.joy.joy = 100; c.TickDuty(.1f);
            var variety = c.PersonalState.moodFactors.Find(f => f.reason == "다양한 오락");
            Check(variety != null && variety.value <= GameBalance.VarietyMoodMax, "variety mood capped");

            // 7. 저장: 종류 이름, 질림 배열 4 → 12 확장, 왕복
            Put<Dormitory>(BuildingKind.BunkBed, home + new Vector3(-20, .8f, -20));
            Put<Dormitory>(BuildingKind.Hammock, home + new Vector3(-23, .3f, -20));
            Put<Kitchen>(BuildingKind.BigTable, home + new Vector3(-26, .4f, -20));
            Put<RecreationSpot>(BuildingKind.Janggi, home + new Vector3(-20, .3f, -25));
            Put<Decoration>(BuildingKind.Statue, home + new Vector3(-23, .9f, -25));
            Put<Decoration>(BuildingKind.Pillar, home + new Vector3(-26, 1.1f, -25));
            var file = SaveSnapshot.Capture();
            Check(new[] { "BunkBed", "Hammock", "BigTable", "Janggi", "Statue", "Pillar" }.All(k => file.buildings.Any(b => b.kind == k)), "new furniture saved by kind");
            foreach (var cd in file.commanders) cd.personalState.joy.boredom = new float[4];
            Check(SaveValidator.Validate(file, out var error) && file.commanders.All(cd => cd.personalState.joy.boredom.Length == RecreationSpot.KindCount), "4-kind boredom arrays padded: " + error);
            Check(SaveSystem.TrySave(false, 0, out error), "save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error); await Ready();
            Check(Object.FindObjectsByType<Dormitory>().Count(x => x.Data.kind == BuildingKind.BunkBed) == 1 && Object.FindObjectsByType<Dormitory>().Count(x => x.Data.kind == BuildingKind.Hammock) == 1, "beds restored");
            Check(Object.FindObjectsByType<Kitchen>().Count(x => x.Data.kind == BuildingKind.BigTable) == 1, "big table restored");
            Check(RecreationSpot.All.Count(s => s.Data.kind == BuildingKind.Janggi) == 1, "janggi restored");
            Check(Object.FindObjectsByType<Decoration>().Count(x => x.Data.kind == BuildingKind.Statue || x.Data.kind == BuildingKind.Pillar) == 2, "decorations restored");
            made.Clear();
            return "PASS " + checks + " furniture batch 5 checks";
        }
        finally
        {
            foreach (var go in made) if (go != null) Object.Destroy(go);
            SaveStorage.RootOverride = original;
        }
    }
}
