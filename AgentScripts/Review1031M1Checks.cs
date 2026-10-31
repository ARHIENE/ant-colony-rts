using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Save;

public static class Review1031M1Checks
{
    private static int count;
    private static void Check(bool valid, string name)
    { if (!valid) throw new Exception("M1 실패: " + name); count++; }

    public static Task<string> Main()
    {
        count = 0;
        Check(StoreBin.TypeCount == 82, "자원 79→82");
        Check((int)AntColony.Data.ResourceType.SmokedFood == 78, "구 자원 번호 보존");
        var types = new[] { AntColony.Data.ResourceType.ElectronicComponent, AntColony.Data.ResourceType.StellarMetal, AntColony.Data.ResourceType.StellarAlloy };
        var names = new[] { "전자부품", "성휘금속", "성휘합금" };
        var bin = new StoreBin();
        var materials = (AntColony.Data.ResourceType[])typeof(ResourceManager).GetField("OtherMaterials", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        for (int i = 0; i < types.Length; i++)
        {
            Check((int)types[i] == 79 + i && AntColony.Data.ResourceLabels.DisplayName(types[i]) == names[i], "신규 자원 ID·표시명 " + i);
            Check(bin.Allows(types[i]) && bin.Amount(types[i]) == 0, "신규 저장칸 허용·0 기본값 " + i);
            Check(materials.Contains(types[i]), "총재료 저장 연결 " + i);
        }
        var amounts = Enumerable.Range(0, 79).ToArray();
        typeof(StoreBin).GetMethod("Apply", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bin, new object[] { new StoreBin.Dto { amounts = amounts } });
        Check(bin.Amount(AntColony.Data.ResourceType.SmokedFood) == 78 && types.All(t => bin.Amount(t) == 0), "구 저장칸 복원·새 자원0");
        amounts = new int[82]; amounts[79] = 3; amounts[80] = 4; amounts[81] = 5;
        typeof(StoreBin).GetMethod("Apply", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bin, new object[] { new StoreBin.Dto { amounts = amounts, extraForbidden = new List<int> { 80 } } });
        Check(types.Select(bin.Amount).SequenceEqual(new[] { 3, 4, 5 }) && !bin.Allows(types[1]) && bin.Allows(types[0]), "신규 저장칸 재고·64 초과 필터 복원");
        var chemistry = Processor.RecipesFor(AntColony.Data.BuildingKind.ChemistryBench);
        var smelting = Processor.RecipesFor(AntColony.Data.BuildingKind.GasSmelter);
        Check(chemistry[0].output == AntColony.Data.ResourceType.Plastic && chemistry[1].output == AntColony.Data.ResourceType.Rubber, "구 화학 제조법 인덱스 보존");
        Check(smelting[0].output == AntColony.Data.ResourceType.Titanium && smelting[1].output == AntColony.Data.ResourceType.TitaniumAlloy, "구 제련 제조법 인덱스 보존");
        var electronic = chemistry.Single(r => r.output == types[0]);
        Check(electronic.amount == 1 && electronic.work == 20 && electronic.inputs.SequenceEqual(new[] {
            (AntColony.Data.ResourceType.Copper, 2), (AntColony.Data.ResourceType.Glass, 1),
            (AntColony.Data.ResourceType.Plastic, 1), (AntColony.Data.ResourceType.Component, 1) }), "전자부품 배합");
        var metal = smelting.Single(r => r.output == types[1]);
        Check(metal.amount == 1 && metal.inputs.Single() == (AntColony.Data.ResourceType.StellarOre, 2), "기존 지하 성휘광석→성휘금속");
        Check(AntColony.World.UndergroundSurvey.Amount(AntColony.Data.ResourceType.StellarOre) == 80
            && UndergroundProduction.Matches(AntColony.Data.BuildingKind.AdvancedDeepDrill, AntColony.Data.ResourceType.StellarOre)
            && UndergroundProduction.RequiredGrade(AntColony.Data.ResourceType.StellarOre) == 5, "기존 유한 매장·합금 채굴기 경로 유지");
        var alloy = smelting.Single(r => r.output == types[2]);
        Check(alloy.amount == 2 && alloy.inputs.SequenceEqual(new[] { (types[1], 2), (AntColony.Data.ResourceType.TitaniumAlloy, 1) }), "성휘합금 배합");

        Check(CampaignResearch.Technologies.Length == 49 && (int)ScienceTechnology.UndergroundSurvey == 42, "구 연구 ID·49노드");
        var visiting = new HashSet<ScienceTechnology>();
        var visited = new HashSet<ScienceTechnology>();
        void Visit(ScienceTechnology technology, int maxTier)
        {
            if (visited.Contains(technology)) return;
            if (!visiting.Add(technology)) throw new Exception("순환 선행: " + technology);
            var definition = CampaignResearch.Technologies[(int)technology];
            if (definition.Tier > maxTier) throw new Exception("상위 연구소 순환: " + technology);
            foreach (var prerequisite in definition.Prerequisites) Visit(prerequisite, maxTier);
            visiting.Remove(technology); visited.Add(technology);
        }
        for (int i = 0; i < CampaignResearch.Technologies.Length; i++)
        {
            Check((int)CampaignResearch.Technologies[i].Technology == i, "연구 인덱스 " + i);
            visiting.Clear(); visited.Clear(); Visit((ScienceTechnology)i, 6);
        }
        for (int tier = 1; tier <= 6; tier++)
        {
            var promotion = EraPromotionResearch.Promotions[tier - 1];
            Check(CampaignResearch.Technologies[(int)promotion].Tier == tier, "시대별 승급 연구 " + tier);
            if (tier == 6) continue;
            Check(ScienceLabUpgrades.RequiredResearch(tier) == promotion, "직전 승급 연구 관문 " + tier);
            visiting.Clear(); visited.Clear(); Visit(promotion, tier);
            Check(visited.Count == tier, "선행 전부 현재 연구소로 가능 " + tier);
        }
        var expectedCosts = new[] {
            new[] { (AntColony.Data.ResourceType.Iron, 40) }, new[] { (AntColony.Data.ResourceType.Steel, 60) },
            new[] { (AntColony.Data.ResourceType.Steel, 80), (AntColony.Data.ResourceType.Component, 10) },
            new[] { (AntColony.Data.ResourceType.Titanium, 80), (types[0], 20) }, new[] { (types[2], 80), (types[0], 40) } };
        for (int tier = 1; tier <= 5; tier++) Check(ScienceLabUpgrades.Cost(tier).SequenceEqual(expectedCosts[tier - 1]), "기획 승급 재료 " + tier);
        Check(!ScienceLabUpgrades.Cost(6).Any(), "최고등급 추가비용 없음");

        var state = new CampaignResearch.State { completed = Enumerable.Range(0, 49).ToList() };
        Check(CampaignResearch.Validate(state, out _), "49개 완료 연구 저장 검증");
        state.completed.Add(49); Check(!CampaignResearch.Validate(state, out _), "범위 밖 완료 ID 거부");
        state = new CampaignResearch.State { active = 48, progress = 10, partialIds = new List<int> { 43 }, partialProgress = new List<float> { 5 } };
        Check(CampaignResearch.Validate(state, out _), "신규 진행·보류 연구 저장");
        Check(CampaignResearch.Validate(new CampaignResearch.State { completed = new List<int> { 0, 42 } }, out _), "구 완료 연구 보존·승급 자동 완료 없음");
        var save = new SaveFileV1 { version = 23, options = null };
        save.campaign = state;
        save.world.transports.Add(new TransportDto { cargoAmounts = Enumerable.Range(0, 79).ToArray() });
        Check(!SaveValidator.Validate(save, out var error) && error == "Missing required sections.", "이관 이후 필수검증 유지");
        var cargo = save.world.transports[0].cargoAmounts;
        Check(save.version == 24 && cargo.Length == 82 && cargo.Take(79).SequenceEqual(Enumerable.Range(0, 79)) && cargo.Skip(79).All(n => n == 0), "23→24 적재 배열 보존·0 확장");
        Check(ReferenceEquals(save.campaign, state) && state.active == 48 && state.partialIds.Single() == 43, "이관 시 연구 상태 불변");
        cargo[79] = 7; SaveValidator.Validate(save, out _);
        Check(cargo[79] == 7, "현재 저장 신규 재고 재이관 없음");
        save = new SaveFileV1 { version = 23, options = null };
        save.world.transports.Add(new TransportDto { cargoAmounts = Array.Empty<int>() });
        SaveValidator.Validate(save, out _);
        Check(save.world.transports[0].cargoAmounts.Length == 0, "구 세종 적재 fallback 유지");
        save = new SaveFileV1 { savedAtUtc = DateTime.UtcNow.ToString("O") };
        var ship = new TransportDto { walking = true, cargoAmounts = new int[82] };
        ship.cargoAmounts[81] = 8; save.world.transports.Add(ship);
        var validateTransport = typeof(SaveFileV1).Assembly.GetType("AntColony.Save.SaveTransportValidation")
            .GetMethod("Validate", BindingFlags.Static | BindingFlags.NonPublic);
        validateTransport.Invoke(null, new object[] { save });
        Check(ship.cargoAmounts[81] == 8, "현재 적재 검증 신규 자원 보존");
        ship.cargoAmounts = new int[79];
        Check(!SavePreflight.Validate(save, out error) && error.Contains("cargo amounts"), "Preflight 현재버전 짧은 적재 배열 거부");
        ship.cargoAmounts = new int[82]; ship.cargoAmounts[80] = -1;
        Check(!SavePreflight.Validate(save, out error) && error.Contains("cargo amounts"), "Preflight 신규 자원 음수 거부");
        return Task.FromResult($"M1 오프라인 {count}검사 통과");
    }

    // Unity 조작 금지에 따라 진입점만 준비. 전체 씬 복원·실제 생산·UI는 실행 잔여.
    public static async Task<string> RunScene()
    {
        var result = await Main();
        var bin = new StoreBin();
        var dto = new StoreBin.Dto { amounts = new int[82], extraForbidden = new List<int> { 80 } };
        dto.amounts[79] = 3; dto.amounts[80] = 4; dto.amounts[81] = 5;
        typeof(StoreBin).GetMethod("Apply", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bin, new object[] { dto });
        var binRoundtrip = UnityEngine.JsonUtility.FromJson<StoreBin.Dto>(UnityEngine.JsonUtility.ToJson(bin.Capture()));
        Check(binRoundtrip.amounts.SequenceEqual(dto.amounts) && binRoundtrip.extraForbidden.SequenceEqual(dto.extraForbidden), "신규3자원·필터 JSON 왕복");
        var campaign = new CampaignResearch.State { completed = new List<int> { 43, 44, 45, 46, 47 }, active = 48, progress = 10 };
        var campaignRoundtrip = UnityEngine.JsonUtility.FromJson<CampaignResearch.State>(UnityEngine.JsonUtility.ToJson(campaign));
        Check(CampaignResearch.Validate(campaignRoundtrip, out _) && campaignRoundtrip.completed.SequenceEqual(campaign.completed)
            && campaignRoundtrip.active == 48 && campaignRoundtrip.progress == 10, "승급 완료·진행 연구 JSON 왕복");
        var snapshot = SaveSnapshot.Capture();
        var json = UnityEngine.JsonUtility.ToJson(snapshot);
        Check(SaveValidator.TryParse(json, out var parsed, out var error), "실스냅샷 JSON " + error);
        Check(SavePreflight.Validate(parsed, out error), "실스냅샷 Preflight " + error);
        Check(parsed.campaign.completed.SequenceEqual(snapshot.campaign.completed), "완료 연구 JSON 보존");
        Check(parsed.colony.baseBin.amounts.Length == 82 && parsed.world.transports.All(t => t.cargoAmounts.Length == 82), "실스냅샷 배열 크기");
        return result + "; 장면 JSON/Preflight 통과(전체 복원·생산·UI 별도)";
    }
}
