using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

public static class Review1101P1Checks
{
    private static int count;
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static void Check(bool value, string name) { if (!value) throw new Exception("P1 실패: " + name); count++; }
    public static async Task<string> Main()
    {
        await Task.CompletedTask;
        count = 0;
        Research(); Supply(); Diamonds(); Planning();
        return $"P1 오프라인 {count}검사 통과";
    }

    private static void Research()
    {
        var definitions = AntColony.Core.CampaignResearch.Technologies;
        for (int tier = 1; tier <= 5; tier++)
        {
            var seen = new HashSet<int>(); var visiting = new HashSet<int>();
            void Visit(AntColony.Core.ScienceTechnology technology)
            {
                int id = (int)technology;
                if (seen.Contains(id)) return;
                Check(visiting.Add(id), "승급 연구 순환 없음");
                Check(definitions[id].Tier <= tier, "상위 연구소 선행 없음");
                foreach (var p in definitions[id].Prerequisites) Visit(p);
                visiting.Remove(id); seen.Add(id);
            }
            var promotion = AntColony.Buildings.ScienceLabUpgrades.RequiredResearch(tier);
            Visit(promotion);
            Check(definitions[(int)promotion].Tier == tier && seen.Count == tier, "전용 시대 관문 " + tier);
            Check(Math.Abs(AntColony.Core.CampaignResearch.LabMultiplier(tier) - (1 + .25f * (tier - 1))) < .0001, "승급 속도 " + tier);
        }
        var sampleTick = typeof(AntColony.Core.CampaignResearch).GetMethod("TickResourceResearch", All);
        Check(Calls(sampleTick).Any(m => m.Name == "GainExperience"), "표본 연구 실제 경험치 연결");
        Check(Calls(sampleTick).Any(m => m.Name == "LabRate"), "표본 실행·예상 속도 공통 조건");
        var tick = typeof(AntColony.Core.CampaignResearch).GetMethod("Tick", All);
        Check(Calls(tick).Any(m => m.Name == "LabRate"), "일반 연구 실제 참여 조건");
        Check(Calls(tick).Any(m => m.Name == "Min"), "완료 초과분 경험치 제한");
        Check(typeof(AntColony.UI.GameMenuController).GetMethod("ResearchEffect", All).Invoke(null,
            new object[] { AntColony.Core.ScienceTechnology.UndergroundSurvey }).ToString().Contains("표본"), "산업 연구 효과 안내");
        var old = new AntColony.Core.CampaignResearch.State { completed = new List<int> { 0, 42 } };
        Check(AntColony.Core.CampaignResearch.Validate(old, out _), "구 연구·새 진행 필드 없음");
        Check(!old.completed.Contains((int)AntColony.Core.ScienceTechnology.SettlementPromotion), "구저장 승급 소급 없음");
        Check(definitions[(int)AntColony.Core.ScienceTechnology.AdvancedCrops].Tier == 3
            && definitions[(int)AntColony.Core.ScienceTechnology.Drainage].Tier == 3, "석유 시대 식량 안정 선택");
        Check(definitions[(int)AntColony.Core.ScienceTechnology.Insulation].Tier == 5
            && definitions[(int)AntColony.Core.ScienceTechnology.Regeneration].Tier == 5, "원자 시대 보존·회복 선택");
        Check(AntColony.Core.CampaignResearch.Validate(new AntColony.Core.CampaignResearch.State {
            active = (int)AntColony.Core.ScienceTechnology.Insulation, progress = 1499 }, out _), "구저장 생활연구 진행 보존");
    }

    private static void Supply()
    {
        var gas = AntColony.Buildings.Processor.RecipesFor(AntColony.Data.BuildingKind.GasSmelter);
        var alloy = gas.Single(r => r.output == AntColony.Data.ResourceType.StellarAlloy);
        var metal = gas.Single(r => r.output == AntColony.Data.ResourceType.StellarMetal);
        var ore = AntColony.Buildings.ScienceLabUpgrades.Cost(5).Single(c => c.type == alloy.output).amount / alloy.amount
            * alloy.inputs.Single(i => i.type == metal.output).amount / metal.amount * metal.inputs.Single().amount + 1;
        Check(ore == AntColony.World.StellarSupply.RocketOreRequired, "실제 제조법·표본 총 광석161");
        foreach (AntColony.Core.MapBiome biome in Enum.GetValues(typeof(AntColony.Core.MapBiome)))
        foreach (var radius in new[] { 24f, 40f, 64f })
        for (int seed = -32; seed < 96; seed++)
        {
            var layers = AntColony.World.UndergroundSurvey.Generate(seed, 0, UnityEngine.Vector3.zero, radius, biome);
            Check(layers.Where(l => l.type == (int)AntColony.Data.ResourceType.StellarOre).Sum(l => l.total) >= ore, "시드별 성휘 공급");
            foreach (var input in new[] { AntColony.Data.ResourceType.NaturalGas, AntColony.Data.ResourceType.CrudeOil, AntColony.Data.ResourceType.TitaniumOre })
                Check(layers.Any(l => l.type == (int)input), "성휘 치환 후 산업 원료 " + input);
            Check(layers.Where(l => l.type == (int)AntColony.Data.ResourceType.TitaniumOre).Sum(l => l.total) >= 261, $"티타늄 승급·채굴기·표본 공급 {biome}/{radius}/{seed}");
            Check(layers.Count == 12 && layers.Count(l => l.type < 0) == 4 && layers.Select(l => l.id).Distinct().Count() == 12, "기존 후보·ID·빈층 보존");
            var valid = new AntColony.World.UndergroundSurvey.State { present = true, layers = layers };
            Check(AntColony.World.UndergroundSurvey.Valid(valid, 1), "생성 광층 저장 검증");
            var before = string.Join(";", layers.Select(l => l.type + ":" + l.total));
            AntColony.World.StellarSupply.EnsureHome(layers);
            Check(before == string.Join(";", layers.Select(l => l.type + ":" + l.total)), "보장 재적용 멱등");
        }
        var smelter = AntColony.Buildings.Processor.RecipesFor(AntColony.Data.BuildingKind.Smelter);
        var part = smelter.Last();
        Check(part.output == AntColony.Data.ResourceType.Component && part.work == 16, "기존 제련 목록 끝 부품 추가");
        Check(part.inputs.SequenceEqual(new[] { (AntColony.Data.ResourceType.Iron, 2), (AntColony.Data.ResourceType.Copper, 1) }), "부품 수동 배합");
        Check(smelter[6].name.Contains("숯") && smelter[11].name.Contains("코크스"), "구 고체연료 제조법 인덱스 보존");
        var chemistry = AntColony.Buildings.Processor.RecipesFor(AntColony.Data.BuildingKind.ChemistryBench);
        var electronics = chemistry.Single(r => r.output == AntColony.Data.ResourceType.ElectronicComponent);
        var directWork = 60 * electronics.work + 80 * metal.work + 40 * alloy.work;
        Check(directWork == 3920, "신규 고급재료60/80 기준 직접작업65.33분");
        var recipes = smelter.Concat(gas).Concat(chemistry).Concat(AntColony.Buildings.Processor.RecipesFor(AntColony.Data.BuildingKind.Kiln))
            .GroupBy(r => r.output).ToDictionary(g => g.Key, g => g.First());
        var produced = new Dictionary<AntColony.Data.ResourceType, int>();
        var leaves = new Dictionary<AntColony.Data.ResourceType, int>(); float work = 0; int gasUnits = 0, oilUnits = 0;
        void Expand(AntColony.Data.ResourceType type, int amount)
        {
            if (!recipes.TryGetValue(type, out var recipe)) { leaves[type] = (leaves.TryGetValue(type, out var oldAmount) ? oldAmount : 0) + amount; return; }
            int batches = AntColony.Buildings.Processor.BatchesFor(amount, 0, recipe.amount);
            produced[type] = (produced.TryGetValue(type, out var old) ? old : 0) + batches * recipe.amount;
            work += batches * recipe.work;
            if (gas.Contains(recipe)) gasUnits += batches;
            if (type == AntColony.Data.ResourceType.Plastic) oilUnits += batches * AntColony.Buildings.IndustrialRecipes.PlasticOil;
            foreach (var input in recipe.inputs) Expand(input.type, input.amount * batches);
        }
        foreach (var cost in Enumerable.Range(1, 5).SelectMany(AntColony.Buildings.ScienceLabUpgrades.Cost)) Expand(cost.type, cost.amount);
        Check(work == 21540 && gasUnits == 260 && oilUnits == 120, "승급 전체 산업 사슬359분·가스260·원유120");
        Check(produced[AntColony.Data.ResourceType.Component] == 70 && produced[AntColony.Data.ResourceType.Iron] == 500
            && produced[AntColony.Data.ResourceType.Copper] == 190, "직접 제조 중간재 총량");
        Check(leaves[AntColony.Data.ResourceType.StellarOre] == 160 && leaves[AntColony.Data.ResourceType.TitaniumOre] == 240, "원광석 총량");
    }

    private static void Diamonds()
    {
        IEnumerable<MethodInfo> Methods(Type type) => type.GetMethods(All | BindingFlags.DeclaredOnly)
            .Concat(type.GetNestedTypes(All).SelectMany(Methods));
        bool Edge(Type from, string to) => Methods(from).Any(m => Calls(m).Any(c => c.Name == to));
        Check(Edge(typeof(AntColony.Units.WorkerAnt), "ApplyWork") && Edge(typeof(AntColony.Buildings.BuildingBase), "LeaveBabelFoundation"), "바벨 시공·파괴 루프");
        Check(Edge(typeof(AntColony.World.ResourceNode), "NoteFarmer") && Edge(typeof(AntColony.World.ColonyEvents), "get_Blessed"), "낙토 파종·성장 루프");
        Check(Edge(typeof(AntColony.Units.WorkerAnt), "FishingTreasure"), "태공 실제 어획 루프");
        Check(Edge(typeof(AntColony.Core.CampaignResearch), "GrantInsight") && Edge(typeof(AntColony.Core.CampaignResearch), "ApplyInsight"), "천기 완료·선택 루프");
        Check(Edge(typeof(AntColony.Buildings.Kitchen), "Note") && Edge(typeof(AntColony.Units.CommanderAnt), "ApplyDiamondMeal"), "식선 요리·식사 루프");
        Check(Edge(typeof(AntColony.Buildings.BuildingConstructionSite), "CompleteAttribution") && Edge(typeof(AntColony.UI.GameMenuController), "OfferArtwork"), "화룡 작품·직접선물 루프");
        Check(Edge(typeof(AntColony.Core.ColonyPopulation), "SettlementDiscontentMultiplier") && Edge(typeof(AntColony.Units.CommanderPersonalState), "DiscontentMultiplier"), "여래장 시민·장수 불만 루프");
        Check(Edge(typeof(AntColony.Units.CommanderAnt), "NoteTamer") && Edge(typeof(AntColony.World.Critter), "get_HamelinProtected")
            && Edge(typeof(AntColony.World.WildMonster), "get_HamelinProtected"), "하멜른 돌봄·선동 억제 루프");
        var work = new AntColony.Core.WorkContribution(); work.Add("a", 74.9f, true); work.Add("b", 25.1f, false);
        Check(work.Majority(100) == null, "바벨·천기75% 미달"); work.Add("a", .1f, true);
        Check(work.Majority(100) == "a", "바벨·천기75% 경계");
        var sole = new AntColony.Core.SoleWorker(); sole.Note("a", true); Check(sole.Empowered, "단독 담당");
        sole.Note("b", true); Check(!sole.Empowered, "다른 장수 개입 해제");
        Check(AntColony.World.ResourceNode.TreasureCatch(.04999f) && !AntColony.World.ResourceNode.TreasureCatch(.05f), "태공5% 경계");
        Check(Math.Abs(AntColony.Core.CampaignResearch.PoliticalMultiplier(2) - .55f) < .0001, "여래장 체감 중첩");
        Check(Math.Abs(AntColony.World.DiplomacyManager.ArtworkChance(0) - .8f) < .0001, "화룡30%p");
        var food = new AntColony.Core.FoodLots(); food.Add(1, 150, 6, true);
        Check(food.Take(1).Inspired, "식선 운반 묶음 보존");
    }

    private static void Planning()
    {
        Check(AntColony.Buildings.Processor.BatchesFor(80, 1, 2) == 40, "재고 목표 묶음 올림");
        Check(AntColony.Buildings.Processor.BatchesFor(80, 81, 2) == 0, "목표 초과 생산 없음");
        Check(AntColony.Buildings.Processor.BatchesFor(int.MaxValue, 0, 2) == 1073741824, "목표 계산 정수 오버플로 없음");
        Check(AntColony.Buildings.ScienceLabUpgrades.StockStatus(5, _ => 0).Contains("부족 80"), "승급 부족 수량");
        var state = new AntColony.Core.ColonyPopulation.State { old = 100, agingDue = 20, elderlyService = true };
        Check(AntColony.Core.ColonyPopulation.NaturalDeaths(state) == 12 && state.old == 88 && state.agingDue == 20, "동원 노년 자연사 제외");
        Check(AntColony.Core.ColonyPopulation.MilitaryAgeCount(40, state) == 128, "노년 병역·성체 중복 없음");
    }

    // 실제 컴파일 결과의 호출 간선을 검사한다. 문자열 검색으로 효과 연결을 대신하지 않는다.
    private static IEnumerable<MethodBase> Calls(MethodInfo method)
    {
        var bytes = method.GetMethodBody()?.GetILAsByteArray(); if (bytes == null) yield break;
        var ops = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static).Select(f => (OpCode)f.GetValue(null)).ToDictionary(o => unchecked((ushort)o.Value));
        for (int i = 0; i < bytes.Length;)
        {
            ushort code = bytes[i++]; if (code == 0xfe) code = (ushort)(0xfe00 | bytes[i++]);
            var op = ops[code];
            if (op.OperandType == OperandType.InlineMethod)
                yield return method.Module.ResolveMethod(BitConverter.ToInt32(bytes, i), method.DeclaringType.GetGenericArguments(), method.GetGenericArguments());
            i += op.OperandType == OperandType.InlineSwitch ? 4 + 4 * BitConverter.ToInt32(bytes, i)
                : op.OperandType == OperandType.InlineNone ? 0
                : op.OperandType == OperandType.ShortInlineBrTarget || op.OperandType == OperandType.ShortInlineI || op.OperandType == OperandType.ShortInlineVar ? 1
                : op.OperandType == OperandType.InlineVar ? 2
                : op.OperandType == OperandType.InlineI8 || op.OperandType == OperandType.InlineR ? 8 : 4;
        }
    }

    public static async Task<string> RunP1Scene()
    {
        var result = await Main();
        var pop = AntColony.Core.ColonyPopulation.Instance;
        var pool = AntColony.Core.AntPool.Instance;
        var research = AntColony.Core.CampaignResearch.Instance;
        if (pop == null || pool == null || research == null) throw new Exception("시작 완료된 격리 Play 장면 필요");
        var snapshot = AntColony.Save.SaveSnapshot.Capture();
        Check(AntColony.Save.SaveValidator.TryParse(UnityEngine.JsonUtility.ToJson(snapshot), out var parsed, out var error), "씬 JSON " + error);
        Check(AntColony.Save.SavePreflight.Validate(parsed, out error), "씬 Preflight " + error);
        var original = pop.S; var free = pool.Free; var assigned = pool.Assigned; var reserved = pool.Reserved;
        var history = AntColony.Core.CampaignHistory.Instance?.Capture();
        var stateField = typeof(AntColony.Core.CampaignResearch).GetField("state", All);
        var originalResearch = stateField.GetValue(research);
        void Counts(int f, int a, int r) => typeof(AntColony.Core.AntPool).GetMethod("RestoreCounts", All).Invoke(pool, new object[] { f, a, r });
        try
        {
            stateField.SetValue(research, new AntColony.Core.CampaignResearch.State { paused = true,
                samples = new List<int> { (int)AntColony.Data.ResourceType.IronOre } });
            Check(research.TryStartResourceResearch(AntColony.Data.ResourceType.IronOre) && !research.Paused, "실제 표본 시작 정지 해제");
            research.SetPaused(true); research.Tick(10);
            Check(research.ResourceProgress == 0, "실제 표본 정지 중 진행0");
            typeof(AntColony.Core.ColonyPopulation).GetField("S").SetValue(pop,
                new AntColony.Core.ColonyPopulation.State { old = 20, elderlyService = true, sentiment = 60, policy = AntColony.Core.MilitaryPolicy.Total, ages = new int[65] });
            Counts(0, 0, 0);
            Check(pop.TryMilitaryDraft(5, false) && pop.S.old == 20 && pop.S.agingDue == 5, "실제 노년 편입·나이 보존");
            Check(pool.TryAssign(5), "실제 노년 동원");
            pool.ReturnAssigned(3); Check(pop.S.old == 20 && pop.S.agingDue == 2 && pool.Free == 0, "실제 귀환 풀 정산");
            pool.LoseAssigned(2); Check(pop.S.old == 18 && pop.S.agingDue == 0, "실제 전사1회 차감");
        }
        finally
        {
            stateField.SetValue(research, originalResearch);
            typeof(AntColony.Core.ColonyPopulation).GetField("S").SetValue(pop, original);
            Counts(free, assigned, reserved);
            if (history != null) AntColony.Core.CampaignHistory.Instance.Restore(history);
        }
        foreach (var lab in AntColony.Core.SceneObjects.All<AntColony.Buildings.ScienceLab>())
            Check(research.LabWaitReason(lab) != "" || research.LabRate(lab) > 0, "실제 연구소 상태·속도 일치");
        return result + "; 씬 JSON/Preflight·노년 동원/귀환/전사 통과. 실생산·화면·장기 시간표 별도 확인";
    }
}
