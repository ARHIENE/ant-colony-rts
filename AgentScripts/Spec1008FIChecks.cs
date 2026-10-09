using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// 2026-10-08 기획 F~I: 정치·행정, 포텐(PA/CA) 성장·재분배, 특성 최대 4개, 주거 재개발 + 시민 생산력·장수 미지정 배치·저장 v15.
public static class Spec1008FIChecks
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception("FAIL " + message); checks++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static T Build<T>(BuildingKind kind, Vector3 position) where T : BuildingBase
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, UnitRole.Worker });
        var go = Object.Instantiate(template, position, Quaternion.identity); go.name = kind.ToString(); go.SetActive(true);
        return go.GetComponent<T>();
    }
    public static async Task<string> Main()
    {
        checks = 0; var previousRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Spec1008FI-" + Guid.NewGuid().ToString("N"));
        try
        {
            Check(Application.isPlaying, "play mode"); await Ready();
            SaveSystem.NewGame(new NewGameOptions { seed = 261008, mapSize = MapSize.Small }); await Ready(); await Task.Delay(300);
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            Object.FindAnyObjectByType<UpkeepManager>().enabled = false;
            var pop = ColonyPopulation.Instance; var c = CommanderRoster.Instance.Commanders[0];

            // H 특성 최대 4개
            var traits = new CommanderTraits();
            foreach (var t in new[] { CommanderTrait.Lazy, CommanderTrait.Optimist, CommanderTrait.Swift, CommanderTrait.Genius, CommanderTrait.Glutton }) traits.TryAdd(t);
            Check(traits.values.Count == CommanderTraits.MaxTraits && CommanderTraits.MaxTraits == 4, "trait cap 4");

            // G 포텐: 생성 시 0 <= CA <= PA <= 200, 성장은 PA를 넘지 않음, 활용 안 한 기술은 감소
            var talents = new CommanderTalents(); talents.Generate(new CommanderTraits());
            Check(talents.potential >= talents.Current - .01f && talents.potential <= 200 && CommanderTalents.Count == 14, "PA bounds after generate");
            talents.potential = Mathf.CeilToInt(talents.Current); // PA 도달 상태
            var ca = talents.Current; var research = talents.Value((int)CommanderActivity.Research);
            for (int i = 0; i < 400; i++) talents.Train(CommanderActivity.Research, 5, _ => 1);
            Check(talents.Current <= talents.potential + .01f, $"CA stays under PA ({talents.Current}/{talents.potential})");
            Check(talents.Value((int)CommanderActivity.Research) > research, "focused skill grows by redistribution");
            Check(Enumerable.Range(0, 14).Any(i => i != (int)CommanderActivity.Research && talents.Value(i) < 20 && talents.UsageShare((CommanderActivity)i) < .05f), "underused skills tracked");
            var gather = new CommanderTalents(); gather.potential = 200;
            gather.Train(CommanderActivity.Gathering, 100, _ => 1);
            Check(Mathf.Abs(gather.Xp(CommanderActivity.Gathering) - 80) < .01f && Mathf.Abs(gather.Xp(CommanderActivity.Strength) - 20) < .01f, "main 80 / support 20 split");
            Check(talents.Validate() && gather.Validate(), "talents validate");

            // F 정치·행정: 행정 책상에 일하면 성과가 오르고 세금 징수가 늘어난다
            Check(c.AllowsJob(CommanderJobs.Administration) && CommanderAnt.SkillFor(CommanderJobs.Administration) == CommanderActivity.Politics, "admin job uses politics");
            pop.S.adminWork = 0;
            var lowTax = pop.MonthlyTax; Check(Mathf.Approximately(pop.TaxCollection, 1 - GameBalance.AdminTaxLoss), "tax loss without admin");
            Check(NavMesh.SamplePosition(c.Position + new Vector3(3, 0, 0), out var hit, 10, NavMesh.AllAreas), "desk spot");
            var desk = Build<AdminDesk>(BuildingKind.AdminDesk, hit.position);
            Check(desk.NeedsWork && desk.Work(c, 10), "desk work");
            var politicsXp = c.Talents.Xp(CommanderActivity.Politics) + c.Talents.Level(CommanderActivity.Politics) * 1000;
            Check(pop.Administration > 0 && pop.MonthlyTax >= lowTax, "admin raises collection");
            pop.AddAdministration(1e6f); Check(Mathf.Approximately(pop.Administration, 1) && !desk.NeedsWork, "admin saturates, desk idle");
            var full = pop.S.adminWork; pop.Tick(60); Check(pop.S.adminWork < full && pop.S.adminWork > full * .8f, "admin decays slowly");

            // 시민 생산력: 큰 아파트가 주거 배율을 올린다
            var before = ColonyPopulation.HousingProductivity;
            Check(NavMesh.SamplePosition(c.Position + new Vector3(-12, 0, 8), out var h1, 10, NavMesh.AllAreas), "housing spot");
            var hut = Build<Housing>(BuildingKind.Hut, h1.position);
            var apartment = Build<Housing>(BuildingKind.Apartment, h1.position + new Vector3(8, 0, 0));
            Check(ColonyPopulation.HousingProductivity > before, "apartment raises productivity");
            Object.Destroy(apartment.gameObject); await Task.Delay(50);

            // I 재개발 견적: 거주 비례 보상, 할인된 건설비, 보상 부족 시 불만, 빈집은 보상 없음
            var houseData = Build<Housing>(BuildingKind.House, h1.position + new Vector3(0, 0, -20)); var data = houseData.Data; Object.Destroy(houseData.gameObject);
            pop.S.redevelopCompensation = 1f;
            var quote = Redevelopment.Price(data, hut);
            Check(quote.residents > 0 && quote.TotalSoil < data.soilCost, $"redevelop cheaper than new ({quote.TotalSoil}/{data.soilCost})");
            Check(quote.sentiment == 0, "full compensation no unrest");
            pop.S.redevelopCompensation = .5f; Check(Redevelopment.Price(data, hut).sentiment < 0, "low compensation unrest");
            pop.S.redevelopCompensation = 1f;

            // 장수 미지정 배치 시작
            var placement = Object.FindAnyObjectByType<BuildingPlacementController>();
            var selection = Object.FindAnyObjectByType<AntColony.Units.SelectionManager>(FindObjectsInactive.Include); if (selection != null) selection.ClearSelection();
            Check(placement.BeginPlacement(BuildingKind.Hut, UnitRole.Worker, null) && placement.Builder == null, "placement without commander");
            placement.CancelPlacement();

            // 저장 v15 왕복: PA·활용 이력·행정·재개발 보상 보존
            Object.Destroy(desk.gameObject); Object.Destroy(hut.gameObject); await Task.Delay(50);
            c.Talents.EnsurePotential(); var pa = c.Talents.potential; pop.S.redevelopCompensation = 1.3f; var admin = pop.S.adminWork;
            Check(SaveFileV1.CurrentVersion == 15, "save v15");
            Check(SaveSystem.TrySave(false, 1, out var error), "save " + error); await Ready();
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 1), out error), "load " + error); await Ready(); await Task.Delay(300);
            var loaded = CommanderRoster.Instance.Commanders.First(x => x.CommanderName == c.CommanderName);
            Check(loaded.Talents.potential == pa && Mathf.Abs(ColonyPopulation.Instance.S.redevelopCompensation - 1.3f) < .001f
                && Mathf.Abs(ColonyPopulation.Instance.S.adminWork - admin) < 1f, "v15 roundtrip");
            return $"PASS Spec1008FI {checks}";
        }
        finally { SaveStorage.RootOverride = previousRoot; Time.timeScale = 1; }
    }
}
