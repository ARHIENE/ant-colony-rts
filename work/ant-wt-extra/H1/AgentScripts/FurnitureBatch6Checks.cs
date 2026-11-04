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

// 2026-10-07 가구 6차: 화롯불·등불·횃불 / 카펫·태피스트리·기념비·동상·전리품 진열대·사람 물건 전시대 / 온천 / 연회용 긴 식탁.
public static class FurnitureBatch6Checks
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

    static readonly BuildingKind[] Deco = { BuildingKind.Brazier, BuildingKind.Lantern, BuildingKind.Torch, BuildingKind.Carpet, BuildingKind.Tapestry,
        BuildingKind.Monument, BuildingKind.BronzeStatue, BuildingKind.TrophyCase, BuildingKind.CuriosDisplay };

    public static async Task<string> Main()
    {
        checks = 0; made.Clear(); Check(Application.isPlaying, "play mode");
        string original = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Furniture6-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 261014, mapSize = MapSize.Small }); await Ready();
            var rm = ResourceManager.Instance;
            foreach (AntColony.Data.ResourceType type in Enum.GetValues(typeof(AntColony.Data.ResourceType))) { rm.AddCapacity(type, 10000); rm.Add(type, 10000); }

            // 1. 템플릿·카탈로그·잠금
            foreach (var k in Deco) Check(Template(k)?.GetComponent<Decoration>()?.Data.kind == k && Decoration.IsKind(k), "decoration template " + k);
            Check(Template(BuildingKind.HotSpring)?.GetComponent<RecreationSpot>()?.Data.kind == BuildingKind.HotSpring && BuildingPlacementController.LockReason(BuildingKind.HotSpring) != null, "hot spring template + research lock");
            Check(Template(BuildingKind.BanquetTable)?.GetComponent<Kitchen>()?.IsTable == true && !Template(BuildingKind.BanquetTable).GetComponent<Kitchen>().NeedsCook, "banquet table is eat-only");
            Check(Deco.Append(BuildingKind.HotSpring).Append(BuildingKind.BanquetTable).All(k => FurnitureCatalog.For(k) != null), "catalog linked");
            Check(RecreationSpot.KindCount == 13, "13 recreation kinds");

            // 2. 장식 기분·조명
            var home = Object.FindAnyObjectByType<Stockpile>().Position;
            var far = home + new Vector3(-30, 0, -30);
            var moods = new Dictionary<BuildingKind, float> { [BuildingKind.Brazier] = 1, [BuildingKind.Lantern] = 1, [BuildingKind.Torch] = 1, [BuildingKind.Carpet] = 1,
                [BuildingKind.Tapestry] = 2, [BuildingKind.Monument] = 5, [BuildingKind.BronzeStatue] = 5, [BuildingKind.TrophyCase] = 3, [BuildingKind.CuriosDisplay] = 4 };
            var i = 0;
            foreach (var pair in moods)
            {
                var d = Put<Decoration>(pair.Key, far + new Vector3(i++ * 3, .5f, 0));
                Check(d.MoodBonus == pair.Value, $"{pair.Key} mood {pair.Value}");
                var lit = pair.Key == BuildingKind.Brazier || pair.Key == BuildingKind.Lantern || pair.Key == BuildingKind.Torch;
                Check((d.GetComponent<Light>() != null) == lit, pair.Key + " light");
                await Remove(d);
            }

            // 3. 방: 긴 식탁 = 장식 3개 미만이면 식당, 3개 이상이면 연회장 / 온천 = 목욕탕
            var o = new Vector3(Mathf.Floor(home.x) + 20, home.y, Mathf.Floor(home.z) + 20);
            for (var x = 0; x < 8; x++) for (var z = 0; z < 8; z++)
            {
                if (x != 0 && x != 7 && z != 0 && z != 7) continue;
                var p = o + new Vector3(x + .5f, .75f, z + .5f);
                if (x == 0 && z == 3) Put<Door>(BuildingKind.Door, p); else Put<Wall>(BuildingKind.LeafWall, p);
            }
            await Task.Yield();
            var inside = o + new Vector3(3.5f, .5f, 3.5f);
            var banquet = Put<Kitchen>(BuildingKind.BanquetTable, o + new Vector3(3.5f, .4f, 4)); await Task.Yield();
            Check(RoomSystem.RoomAt(inside).Kind == RoomKind.Dining, "banquet table without decorations = dining");
            var small = Put<Kitchen>(BuildingKind.Kitchen, o + new Vector3(5.5f, .75f, 2.5f));
            var decos = new[] { Put<Decoration>(BuildingKind.Tapestry, o + new Vector3(1.5f, .8f, 1.2f)), Put<Decoration>(BuildingKind.Carpet, o + new Vector3(1.5f, .02f, 5.5f)),
                Put<Decoration>(BuildingKind.Lantern, o + new Vector3(6.2f, .6f, 6.2f)) };
            await Task.Yield();
            Check(RoomSystem.RoomAt(inside).Kind == RoomKind.BanquetHall, "banquet table + small table + 3 decorations = banquet hall");
            await Remove(banquet); await Remove(small); foreach (var d in decos) await Remove(d);
            var spring = Put<RecreationSpot>(BuildingKind.HotSpring, o + new Vector3(3.5f, .25f, 3.5f)); await Task.Yield();
            Check(spring.Seats == 4 && RoomSystem.RoomAt(inside).Kind == RoomKind.Bathhouse, "hot spring = bathhouse, 4 seats");
            await Remove(spring);

            // 4. 온천 피로 회복, 연회장 식사 기분 1.5배
            var c = CommanderRoster.Instance.Commanders.First(x => x.CivilianWorkReady && x.CanReceiveOrders);
            foreach (var other in CommanderRoster.Instance.Commanders) if (other != c) other.gameObject.SetActive(false);
            c.CommandStop(); c.PersonalState.meal.satiety = 100; c.PersonalState.hygiene.hygiene = 100;
            var bath = Put<RecreationSpot>(BuildingKind.HotSpring, c.Position + Vector3.right * 3);
            c.PersonalState.sleep.fatigue = 80; c.PersonalState.joy.joy = 10; c.PersonalState.joy.retrySeconds = 0;
            var fatigue = c.Fatigue; c.TickDuty(.1f);
            Check(c.IsPlaying && c.PlaySpot == bath, "soaks in hot spring");
            c.TickDuty(GameBalance.PlaySeconds);
            Check(c.Fatigue <= fatigue - GameBalance.HotSpringFatigue + 1, $"hot spring restores fatigue {fatigue} -> {c.Fatigue}");
            await Remove(bath);

            // 5. 저장 왕복
            Put<Decoration>(BuildingKind.Monument, home + new Vector3(-20, 1.2f, -20));
            Put<Decoration>(BuildingKind.Carpet, home + new Vector3(-24, .02f, -20));
            Put<RecreationSpot>(BuildingKind.HotSpring, home + new Vector3(-20, .25f, -26));
            Put<Kitchen>(BuildingKind.BanquetTable, home + new Vector3(-26, .4f, -26));
            var file = SaveSnapshot.Capture();
            Check(new[] { "Monument", "Carpet", "HotSpring", "BanquetTable" }.All(k => file.buildings.Any(b => b.kind == k)), "saved by kind");
            foreach (var cd in file.commanders) cd.personalState.joy.boredom = new float[12];
            Check(SaveValidator.Validate(file, out var error) && file.commanders.All(cd => cd.personalState.joy.boredom.Length == RecreationSpot.KindCount), "12-kind boredom padded: " + error);
            Check(SaveSystem.TrySave(false, 0, out error), "save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error); await Ready();
            Check(Object.FindObjectsByType<Decoration>().Count(d => d.Data.kind == BuildingKind.Monument || d.Data.kind == BuildingKind.Carpet) == 2, "decorations restored");
            Check(RecreationSpot.All.Count(s => s.Data.kind == BuildingKind.HotSpring) == 1, "hot spring restored");
            Check(Object.FindObjectsByType<Kitchen>().Count(k => k.Data.kind == BuildingKind.BanquetTable) == 1, "banquet table restored");
            made.Clear();
            return "PASS " + checks + " furniture batch 6 checks";
        }
        finally
        {
            foreach (var go in made) if (go != null) Object.Destroy(go);
            SaveStorage.RootOverride = original;
        }
    }
}
