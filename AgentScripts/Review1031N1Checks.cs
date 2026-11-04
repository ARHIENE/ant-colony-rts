using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

public static class Review1031N1Checks
{
    private static int count;
    private static void Check(bool ok, string label)
    { if (!ok) throw new Exception("N1 실패: " + label); count++; }
    public static Task<string> Main()
    {
        count = 0;
        Supply(); FixedWorkers();
        return Task.FromResult($"N1 오프라인 {count}검사 통과");
    }
    private static void Supply()
    {
        var metal = Processor.RecipesFor(AntColony.Data.BuildingKind.GasSmelter).Single(r => r.output == AntColony.Data.ResourceType.StellarMetal);
        var alloy = Processor.RecipesFor(AntColony.Data.BuildingKind.GasSmelter).Single(r => r.output == AntColony.Data.ResourceType.StellarAlloy);
        var alloyCost = ScienceLabUpgrades.Cost(5).Single(c => c.type == AntColony.Data.ResourceType.StellarAlloy).amount;
        int ore = alloyCost / alloy.amount * alloy.inputs.Single(i => i.type == AntColony.Data.ResourceType.StellarMetal).amount
            / metal.amount * metal.inputs.Single(i => i.type == AntColony.Data.ResourceType.StellarOre).amount;
        Check(ore == 160 && StellarSupply.RocketOreRequired == ore + 1, "실제 제조법·승급 비용 + 표본1 = 광석161");
        var seeds = Enumerable.Range(-100, 201).Concat(new[] { int.MinValue, int.MaxValue, 0, 12345 }).Distinct();
        foreach (int seed in seeds)
            foreach (MapSize size in Enum.GetValues(typeof(MapSize)))
                foreach (MapBiome biome in Enum.GetValues(typeof(MapBiome)))
                {
                    float radius = 40 * MapSizes.Scale(size);
                    var center = new Vector3(10, 0, -20);
                    var layers = UndergroundSurvey.Generate(seed, 0, center, radius, biome);
                    var stellar = layers.Where(l => l.type == (int)AntColony.Data.ResourceType.StellarOre).ToArray();
                    Check(layers.Count == 12 && layers.Count(l => l.type < 0) == 4 && stellar.Length >= 3, "모든 크기·바이옴·시드 표본 본거지3층");
                    Check(stellar.Sum(l => l.total) >= StellarSupply.RocketOreRequired && layers.All(l => l.remaining == l.total), "유한 총량·초기 잔량");
                    Check(UndergroundSurvey.Valid(new UndergroundSurvey.State { present = true, layers = layers }, 1), "새 층 기존 저장 검증 통과");
                    Check(layers.Select(l => l.id).SequenceEqual(Enumerable.Range(0, 12)) && layers.All(l => !l.discovered && l.work == 0), "ID·미발견 상태 보존");
                    Check(layers.All(l => Math.Abs(l.position.x - center.x) <= radius && Math.Abs(l.position.z - center.z) <= radius), "본거지 범위 안");
                    var again = UndergroundSurvey.Generate(seed, 0, center, radius, biome);
                    Check(layers.Zip(again, (a, b) => a.type == b.type && a.total == b.total && a.position.Equals(b.position)).All(v => v), "동일 입력 재현");
                }
        var legacy = Enumerable.Range(0, 12).Select(i => new UndergroundSurvey.Layer { id = i, type = i < 4 ? -1 : (int)AntColony.Data.ResourceType.IronOre,
            total = i < 4 ? 0 : 400, remaining = i < 4 ? 0 : 400 }).ToList();
        Check(UndergroundSurvey.Valid(new UndergroundSurvey.State { present = true, layers = legacy }, 1), "성휘0 구저장 거부하지 않음");
        Check(StellarSupply.Notice(legacy)?.Contains("161") == true && legacy.All(l => l.type != (int)AntColony.Data.ResourceType.StellarOre), "부족 구저장 읽기 안내만");
        StellarSupply.EnsureHome(legacy);
        Check(legacy.Count(l => l.type == (int)AntColony.Data.ResourceType.StellarOre) == 3 && StellarSupply.Notice(legacy) == null, "부족분 최소 치환·과다 치환 없음");
        var types = legacy.Select(l => l.type).ToArray();
        StellarSupply.EnsureHome(legacy);
        Check(legacy.Select(l => l.type).SequenceEqual(types), "보장 중복 호출 불변");
        foreach (var l in legacy.Where(l => l.type == (int)AntColony.Data.ResourceType.StellarOre)) l.remaining = 0;
        Check(StellarSupply.Notice(legacy) == null, "정상 소비 뒤 최초 공급 부족 오경고 없음");
        var oldLayer = legacy.Last(l => l.type == (int)AntColony.Data.ResourceType.StellarOre);
        oldLayer.type = (int)AntColony.Data.ResourceType.IronOre; oldLayer.total = 400;
        Check(StellarSupply.Notice(legacy) != null, "옛2층160은 표본 포함 부족 안내");
        var away = UndergroundSurvey.Generate(12345, 2, new Vector3(100, 0, 100), 30, MapBiome.Desert);
        Check(away.Select(l => l.id).SequenceEqual(Enumerable.Range(24, 12)) && away.All(l => l.map == 2), "다른 지도 ID·번호 유지");
        Check(AntColony.Save.SaveFileV1.CurrentVersion == 24, "저장 버전 불변");
    }
    private static void FixedWorkers()
    {
        Check(FixedTraitWork.Allows((SoleWorker)null, "normal", CommanderJobs.Farming), "구저장 null 귀속 일반 작업 허용");
        Check(FixedTraitWork.Allows(new SoleWorker(), "normal", CommanderJobs.Farming), "JSON이 되살린 기본 객체 일반 작업 허용");
        foreach (string effect in new[] { "낙토", "식선", "화룡" })
        {
            var job = effect == "낙토" ? CommanderJobs.Farming : effect == "식선" ? CommanderJobs.Cooking : CommanderJobs.Art;
            var worker = new SoleWorker(); worker.Note("owner", true);
            Check(worker.Empowered && FixedTraitWork.Allows(worker, "owner", job), effect + " 고정 담당 허용");
            Check(!FixedTraitWork.Allows(worker, "normal", job) && !FixedTraitWork.Allows(worker, "other-diamond", job), effect + " 다른 장수·보유자 자동 제외");
            if (FixedTraitWork.Allows(worker, "normal", job)) worker.Note("normal", false);
            Check(worker.Empowered && worker.id == "owner", effect + " 정상 자동 작업으로 효과 끊김 재현 방지");
            foreach (var service in new[] { CommanderJobs.Firefighting, CommanderJobs.Repair, CommanderJobs.Hauling })
                Check(FixedTraitWork.Allows(worker, "normal", service), effect + " 소화/수리/배송은 보호에서 제외");
            worker.Note("normal", false);
            Check(!worker.Empowered && FixedTraitWork.Allows(worker, "normal", job), effect + " 직접 작업은 기존 혼합 담당 판정");
            var ordinary = new SoleWorker(); ordinary.Note("normal", false);
            Check(FixedTraitWork.Allows(ordinary, "other", job), effect + " 비보유 작업은 보호하지 않음");
        }
        var kitchen = (Kitchen)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Kitchen));
        var state = new Kitchen.State { current = 0, cook = new SoleWorker { id = "owner", diamond = true } };
        typeof(Kitchen).GetField("meals", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(kitchen, state);
        var fixedCook = typeof(Kitchen).GetProperty("FixedCook", BindingFlags.Instance | BindingFlags.NonPublic);
        Check(ReferenceEquals(fixedCook.GetValue(kitchen), state.cook), "진행 조리 기존 저장 귀속 재사용");
        state.current = -1;
        Check(fixedCook.GetValue(kitchen) == null, "완성 후 옛 요리사가 다음 주문을 예약하지 않음");
        Check(typeof(Kitchen.State).GetFields().All(f => f.Name != "fixedOwner") && typeof(FarmPlot.State).GetFields().All(f => f.Name != "fixedOwner"), "새 저장 필드 없음");
    }
    // Unity 조작 금지: 진입점만 제공한다. 자동 배정·경로·생활/직접 지시·JSON 복원은 격리 장면에서 후속 검증한다.
    public static Task<string> RunScene() => Task.FromResult("N1 장면 검사 실행 잔여: HANDOFF-CODEX-N1-1031.md 참조");
}
