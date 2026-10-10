using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;
using Resource = AntColony.Data.ResourceType;

// 2026-10-10 미관·온도: 마감 재료·장식·오물/방치 물자 미관, 생활 공간 체류 평균 기분, 시민 주거 미관, 추움·쾌적·더움·단열·난방 연료, 가연성 재료 불.
public static class Spec1010SpaceChecks
{
    static int checks;
    static readonly List<GameObject> made = new List<GameObject>();
    static void Check(bool value, string message) { if (!value) throw new Exception("FAIL " + message); checks++; }
    static GameObject Template(BuildingKind k) => (GameObject)typeof(BuildingPlacementController)
        .GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { k, UnitRole.Worker });
    static T Put<T>(BuildingKind k, Vector3 p) where T : Component
    {
        var go = Object.Instantiate(Template(k), p, Quaternion.identity); go.SetActive(true); made.Add(go); return go.GetComponent<T>();
    }
    static void At(float seconds) => GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds, seconds);
    static void TickSpace(CommanderAnt c, float seconds) => typeof(CommanderAnt).GetMethod("TickSpace", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(c, new object[] { seconds });

    public static async Task<string> Main()
    {
        checks = 0; made.Clear();
        if (!Application.isPlaying) throw new Exception("Play mode required");
        while (SaveSystem.Busy) await Task.Delay(50);
        SaveSystem.NewGame(new NewGameOptions { seed = 261013, mapSize = MapSize.Small });
        while (SaveSystem.Busy) await Task.Delay(50);
        Time.timeScale = 0;
        try
        {
            var rm = ResourceManager.Instance; rm.AddCapacity(Resource.Soil, 5000);
            var home = Object.FindAnyObjectByType<Stockpile>().Position;

            // 1. 바깥 미관: 빈 땅은 평범, 장식은 올리고 방치 물자·시체는 내린다.
            var spot = new Vector3(Mathf.Floor(home.x) - 25.5f, home.y, Mathf.Floor(home.z) - 25.5f);
            var empty = SpaceQuality.BeautyAt(spot);
            Check(Mathf.Abs(empty) < 2 && SpaceQuality.BeautyName(empty) == "평범", "empty ground is plain");
            Put<Decoration>(BuildingKind.Statue, spot + new Vector3(1, .9f, 0)); Put<Decoration>(BuildingKind.FlowerPot, spot + new Vector3(-1, .5f, 0));
            await Task.Yield();
            var pretty = SpaceQuality.BeautyAt(spot);
            Check(pretty >= 2 && SpaceQuality.BeautyName(pretty) == "좋음", "decorations make space pretty: " + pretty);
            var junk = new GameObject("Junk"); junk.SetActive(false); junk.transform.position = spot + Vector3.forward; made.Add(junk);
            var pile = junk.AddComponent<ResourceNode>(); pile.ConfigureLoot(Resource.Soil, 10); junk.SetActive(true);
            Check(SpaceQuality.BeautyAt(spot) < pretty, "abandoned pile lowers beauty");
            Check(Mathf.Abs(SpaceQuality.BeautyAt(spot) - pretty) <= SpaceQuality.BeautyCap * 2 && SpaceQuality.BeautyAt(spot) <= SpaceQuality.BeautyCap, "beauty capped");

            // 2. 장수 기분: 지나가기만 해서는 바뀌지 않고, 생활 공간(작업 중)에서 체류 평균으로 오른다.
            var c = CommanderRoster.Instance.Commanders.First(x => x.IsColonyMember);
            c.CommandStop(); c.Agent.Warp(spot); c.PersonalState.beautyExposure = 0;
            TickSpace(c, 120); Check(Mathf.Approximately(c.PersonalState.beautyExposure, 0) && !c.PersonalState.moodFactors.Any(f => f.reason == "공간 미관"), "passing by does not change mood");
            Object.Destroy(junk); await Task.Yield();
            var siteGo = new GameObject("Work site"); siteGo.transform.position = spot; made.Add(siteGo);
            var site = siteGo.AddComponent<BuildingConstructionSite>(); site.Initialize(null, 500);
            c.SetJobEnabled(CommanderJobs.Building, true); c.CommandBuild(site);
            Check(c.IsWorking, "commander working on site");
            TickSpace(c, 5); var partial = c.PersonalState.beautyExposure;
            Check(partial > 0 && partial < SpaceQuality.BeautyAt(spot), "exposure rises gradually");
            TickSpace(c, 600); Check(c.PersonalState.moodFactors.Any(f => f.reason == "공간 미관" && f.value > 0), "pretty work space lifts mood");
            Check(c.PersonalState.moodFactors.First(f => f.reason == "공간 미관").value <= GameBalance.BeautyMoodCap, "mood capped");
            c.CommandStop(); site.Cancel();

            // 3. 시민 주거 미관 → 민심·이주 수요.
            var pop = ColonyPopulation.Instance; var demand0 = pop.Demand;
            Put<Housing>(BuildingKind.Hut, spot + new Vector3(0, .6f, 3)); await Task.Delay(2100); // 2초 캐시
            Check(pop.HousingBeauty > 0 && pop.Demand > demand0, "pretty housing raises demand");

            // 4. 온도: 겨울 바깥은 추움, 단열 좋은 방은 쾌적, 단열 나쁜 방은 난방(목재)이 있어야 쾌적.
            At(2800); // 겨울
            Check(SpaceQuality.Outdoor == TemperatureBand.Cold, "winter is cold outside");
            var o = new Vector3(Mathf.Floor(home.x) + 22, home.y, Mathf.Floor(home.z) + 22);
            for (var x = 0; x < 6; x++) for (var z = 0; z < 6; z++)
            {
                if (x != 0 && x != 5 && z != 0 && z != 5) continue;
                var p = o + new Vector3(x + .5f, .75f, z + .5f);
                if (x == 0 && z == 2) Put<Door>(BuildingKind.Door, p); else Put<Wall>(BuildingKind.LeafWall, p);
            }
            await Task.Yield();
            var inside = o + new Vector3(2.5f, .5f, 2.5f);
            var room = RoomSystem.RoomAt(inside);
            Check(room != null && room.Walls.Count == 16 && SpaceQuality.Insulation(room) < SpaceQuality.InsulationComfort, "leaf room is poorly insulated");
            Check(SpaceQuality.At(inside) == TemperatureBand.Cold, "poor room stays cold");
            var brazier = Put<Decoration>(BuildingKind.Brazier, inside + new Vector3(0, .4f, 0)); await Task.Yield();
            rm.Add(Resource.Wood, 0); while (rm.GetAmount(Resource.Wood) > 0) rm.TrySpend(Resource.Wood, rm.GetAmount(Resource.Wood));
            Heating.Tick(GameCalendar.SecondsPerMonth); Check(!Heating.Heated(room) && SpaceQuality.At(inside) == TemperatureBand.Cold, "no fuel, no heat");
            rm.Add(Resource.Wood, 50); var wood = rm.GetAmount(Resource.Wood);
            Heating.Tick(GameCalendar.SecondsPerMonth);
            room = RoomSystem.RoomAt(inside); // 건물이 바뀌면 방이 다시 만들어진다
            Check(Heating.Heated(room) && SpaceQuality.At(inside) == TemperatureBand.Comfortable && rm.GetAmount(Resource.Wood) < wood, "heater burns wood, room comfortable");
            var leafBurn = wood - rm.GetAmount(Resource.Wood);
            // 같은 방을 단열 좋은 벽(성벽 재료=돌)으로: 난방 없이도 쾌적, 난방 연료도 덜 쓴다.
            Check(MaterialInfo.For(Resource.Stone).insulation > MaterialInfo.For(Resource.Leaf).insulation, "stone insulates better than leaf");
            c.CommandStop(); c.Agent.Warp(inside); c.PersonalState.moodFactors.Clear();
            Object.Destroy(brazier.gameObject); await Task.Yield(); Heating.Tick(1);
            c.SetJobEnabled(CommanderJobs.Building, true);
            var site2Go = new GameObject("Cold site"); site2Go.transform.position = inside; made.Add(site2Go);
            var site2 = site2Go.AddComponent<BuildingConstructionSite>(); site2.Initialize(null, 500); c.CommandBuild(site2);
            TickSpace(c, 2); Check(c.PersonalState.moodFactors.Any(f => f.reason == "추움" && f.value < 0), "cold room lowers mood");
            c.CommandStop(); site2.Cancel();
            At(1000); // 여름
            Check(SpaceQuality.Outdoor == TemperatureBand.Hot, "summer is hot outside");

            // 5. 가연성 재료: 목재 가구는 불이 붙고, 돌 가구는 타지 않는다.
            var woodTable = Put<Kitchen>(BuildingKind.Kitchen, spot + new Vector3(6, .75f, 6)); woodTable.MainMaterial = Resource.Wood;
            var stoneTable = Put<Kitchen>(BuildingKind.Kitchen, spot + new Vector3(10, .75f, 6)); stoneTable.MainMaterial = Resource.Stone;
            Check(WallFire.Ignite(woodTable) && !WallFire.Ignite(stoneTable), "flammable material burns, stone does not");
            return "PASS " + checks + " Spec1010Space checks";
        }
        finally { foreach (var go in made) if (go != null) Object.Destroy(go); Time.timeScale = 0; }
    }
}
