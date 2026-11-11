using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;

namespace AntColony.UI
{
    // 건설 메뉴 초반 건물 툴팁(2026-11-11): 용도 · 지을 시점 · 운영 조건 · 지금 부족한 조건.
    // 상태만 읽는다. 비용·해금·건설 순서는 바꾸지 않으며, 대상이 아닌 건물은 null을 돌려 기존 툴팁을 그대로 쓴다.
    // 수치는 실제 규칙(GameBalance·건물 틀·연구 정의)에서 읽는다.
    public static class BuildTooltips
    {
        private const string Dim = "#b1b3a7", Warn = "#e0a060", Ok = "#a9ba8d";
        private const float CacheSeconds = .5f; // 건설 화면이 매 프레임 갱신하므로 문자열 조립은 0.5초에 한 번만 한다.
        private static readonly Dictionary<BuildingKind, (float until, ResourceType material, string text)> cache
            = new Dictionary<BuildingKind, (float, ResourceType, string)>();

        public static bool Covers(BuildingKind kind) => kind is BuildingKind.Storage or BuildingKind.SleepingMat or BuildingKind.SingleBed
            or BuildingKind.Dormitory or BuildingKind.Toilet or BuildingKind.Farm or BuildingKind.ScienceLab or BuildingKind.AcidTower
            or BuildingKind.SoilWall or BuildingKind.LeafWall or BuildingKind.CapWall or BuildingKind.CastleWall or BuildingKind.Door
            or BuildingKind.TrapPit or BuildingKind.SpikeTrap or BuildingKind.Workshop;

        // name·cost·locked는 건설 화면이 이미 계산한 값(메뉴 표기, 비용 글자, 잠금 사유)을 그대로 받는다.
        public static string For(string name, BuildingKind kind, BuildingData data, string cost, string locked, ResourceType material)
        {
            if (data == null || !Covers(kind)) return null;
            var now = UnityEngine.Time.unscaledTime;
            if (cache.TryGetValue(kind, out var hit) && hit.until > now && hit.material == material) return hit.text;
            var (use, when, run) = Describe(kind, data);
            var missing = Missing(kind, data, locked, material);
            var text = $"<b>{name}</b> <color={Dim}>{cost}</color>\n{use}\n<color={Dim}>언제</color> {when}\n<color={Dim}>운영</color> {run}\n"
                + (missing.Count == 0 ? $"<color={Ok}>지금 부족한 조건 없음</color>" : $"<color={Warn}>부족 · {string.Join(" / ", missing)}</color>");
            cache[kind] = (now + CacheSeconds, material, text);
            return text;
        }

        private static (string use, string when, string run) Describe(BuildingKind kind, BuildingData data)
        {
            switch (kind)
            {
                case BuildingKind.Storage:
                    return ($"식량·재료 기본 보관 한도 +{data.foodCapacityBonus}/+{data.soilCapacityBonus}. 창고에 들어간 재고만 연구비와 식량 목표에 집계됩니다.",
                        "채집물이 바닥에 쌓이거나 연구에 쓸 재고가 필요할 때.",
                        "운반 작업을 켠 장수가 바닥 물자를 입고합니다. 선행 연구 없음.");
                case BuildingKind.SleepingMat:
                    return ($"1명 잠자리. 비용과 시공 시간이 없습니다. 자리에서 자면 기분 {GameBalance.MatSleepMood:0}, 잠자리가 없으면 {GameBalance.RoughSleepMood:0}.",
                        "첫날 밤이 오기 전에 장수 수만큼. " + Beds(),
                        "인력·선행 연구 없음.");
                case BuildingKind.SingleBed:
                    return ($"1명 잠자리. 자리와 달리 수면 기분 감소({GameBalance.MatSleepMood:0})가 없습니다.",
                        "재료에 여유가 생기면 자리를 대신해서. " + Beds(),
                        "인력·선행 연구 없음.");
                case BuildingKind.Dormitory:
                    return ($"{GameBalance.DormitoryBeds}명 잠자리.",
                        "장수가 늘어 잠자리를 한 번에 채울 때. " + Beds(),
                        "인력·선행 연구 없음.");
                case BuildingKind.Toilet:
                    return ($"위생 +{GameBalanceNeeds.ToiletGain:0}(최대 {GameBalanceNeeds.ToiletCap:0}, 한 번에 1명). 위생이 낮으면 기분 {GameBalanceNeeds.HygieneMood(20):0}~{GameBalanceNeeds.HygieneMood(0):0}.",
                        $"위생은 하루 {GameBalanceNeeds.HygienePerSecond * GameCalendar.SecondsPerDay:0} 감소합니다. {GameBalance.WashBelowHygiene:0} 이하로 내려가 임시 용변을 반복하기 전에.",
                        "인력·선행 연구 없음.");
                case BuildingKind.Farm:
                {
                    var crop = CrashLandingStart.StartingCrop(BiomeRules.Current);
                    return ($"{GameMenuController.CropName(crop)} 기본 성장 {FarmCropRules.GrowSeconds(crop) / GameCalendar.SecondsPerDay:0.#}일·수확 {FarmCropRules.Yield(crop):0}개. 환경·연구에 따라 달라지며 겨울에는 성장하지 않습니다. 수확 뒤 다시 자랍니다.",
                        "첫 수확까지 시간이 걸리므로 시작 식용 자원이 떨어지기 전에 일찍.",
                        "농사 작업을 켠 장수와 종자 1개(야생 작물 채집). 선행 연구 없음.");
                }
                case BuildingKind.ScienceLab:
                    return ("과학 연구를 진행합니다. 흙벽·함정·밭 수확 증가 등이 연구로 열립니다.",
                        "창고 재고와 연구에 배정할 장수 여유가 생기면.",
                        $"연구 작업을 켠 장수 1명을 연구 메뉴에서 담당으로 배정(연구대 곁에 있어야 진행). 정착 시대 연구비 식량 {GameBalance.ScienceFood[0]}·{ResourceType.Soil.DisplayName()} {GameBalance.ScienceSoil[0]}은 창고 재고에서 냅니다.");
                case BuildingKind.AcidTower:
                {
                    var tower = BuildingPlacementController.GetTemplate(kind, UnitRole.Worker)?.GetComponent<AcidTower>();
                    return (tower != null ? $"사거리 {tower.Range:0.#} 안의 적을 {tower.AttackInterval:0.#}초마다 {tower.Damage:0.#} 피해로 자동 공격합니다."
                            : "사거리 안의 적을 자동 공격합니다.",
                        $"첫 방어 뒤 2웨이브({WaveDefense.Count(2)}마리)를 대비할 때. 적 진입로를 사거리 안에 두세요.",
                        "인력·탄약·선행 연구 없음.");
                }
                case BuildingKind.SoilWall: case BuildingKind.LeafWall: case BuildingKind.CapWall: case BuildingKind.CastleWall:
                    return ($"적의 통행을 막습니다. 내구 {data.maxHealth:0}"
                            + (kind == BuildingKind.LeafWall ? " · 불에 탐" : kind == BuildingKind.CastleWall ? $" · 가까운 장수 방어 +{GameBalanceRooms.CastleWallArmor:0}" : "") + ".",
                        kind == BuildingKind.LeafWall ? "연구 전에 급히 길을 막을 때. 오래 버티지는 못합니다."
                            : "2~3웨이브 전에 진입로를 좁혀 적을 탑과 장수 앞으로 모을 때.",
                        "인력 없음. 출입구는 문으로 남깁니다. " + Research(kind));
                case BuildingKind.Door:
                    return ($"아군은 지나가고 적은 막힙니다. 적이 닿으면 문을 공격합니다. 내구 {data.maxHealth:0}.",
                        "벽으로 둘러쌀 때 장수가 드나들 출입구로.",
                        "인력·선행 연구 없음.");
                case BuildingKind.TrapPit:
                    return ($"밟은 지상 적을 {GameBalance.TrapRootSeconds:0}초(보스 {GameBalance.TrapBossRootSeconds:0}초) 묶고 파손됩니다. 비행 적에는 듣지 않습니다.",
                        "벽으로 좁힌 진입로나 탑 사거리 안에.",
                        $"발동 뒤 수리 작업 장수가 {ResourceType.Soil.DisplayName()} {GameBalance.TrapRepairSoil}으로 다시 장전합니다. " + Research(kind));
                case BuildingKind.SpikeTrap:
                    return ($"범위 안 지상 적에게 {GameBalance.SpikeTrapInterval:0.#}초마다 {GameBalance.SpikeTrapDamage:0} 피해, {GameBalance.SpikeTrapHits}회 뒤 파손됩니다.",
                        "벽으로 좁힌 진입로나 탑 사거리 안에.",
                        $"파손 뒤 수리 작업 장수가 {ResourceType.Soil.DisplayName()} {GameBalance.TrapRepairSoil}으로 다시 장전합니다. " + Research(kind));
                default: // Workshop
                    return ("장수 장비를 제작합니다.",
                        "연구대를 2등급으로 올리고 근접 무기 연구를 마친 뒤.",
                        "제작 작업을 켠 장수와 제작 재료. " + Research(kind));
            }
        }

        private static string Beds()
        {
            var shortage = HudOverview.BedShortage;
            return shortage > 0 ? $"현재 잠자리 {shortage}개 부족." : "현재 잠자리는 충분합니다.";
        }

        // 선행 연구 이름. 그 연구의 선행이 남아 있으면 한 단계만 함께 보여 준다.
        private static string Research(BuildingKind kind)
        {
            var needs = ScienceEffects.BuildingResearch(kind);
            return needs.Length == 0 ? "선행 연구 없음." : "선행 연구: " + string.Join(" · ", needs.Select(ResearchName)) + ".";
        }
        private static string ResearchName(ScienceTechnology t)
        {
            var d = CampaignResearch.Technologies[(int)t];
            var before = d.Prerequisites.Where(p => !ScienceEffects.Has(p)).Select(p => CampaignResearch.Technologies[(int)p].Name).ToArray();
            return d.Name + (d.Tier > 1 ? $"({CampaignResearch.EraNames[d.Tier - 1]} 시대 · 연구소 {d.Tier}등급)" : "")
                + (before.Length > 0 ? " ← " + string.Join(" · ", before) : "");
        }

        private static List<string> Missing(BuildingKind kind, BuildingData data, string locked, ResourceType material)
        {
            var list = new List<string>();
            var research = ScienceEffects.BuildingResearch(kind).Where(t => !ScienceEffects.Has(t)).ToArray();
            if (research.Length > 0) list.Add("연구 미완료: " + string.Join(" · ", research.Select(ResearchName)));
            else if (locked != null) list.Add(locked); // 연구 외 잠금 사유는 기존 문구 그대로.

            // 건설비. 재고는 창고 기준이며, 부족해도 예정지는 둘 수 있고 바닥 물자도 건설에 쓰인다.
            var rm = ResourceManager.Instance;
            var short_ = new List<string>();
            void Need(ResourceType type, int need)
            {
                var have = rm != null ? rm.GetAmount(type) : 0;
                if (need > 0 && have < need) short_.Add($"{type.DisplayName()} {have}/{need}");
            }
            if (EarlyConstruction.HasFixedCost(kind)) foreach (var c in EarlyConstruction.Cost(kind)) Need(c.type, c.amount);
            else
            {
                Need(ResourceType.Food, data.foodCost);
                Need(Demolition.MaterialSelectable(kind) ? material : ResourceType.Soil, data.soilCost);
                Need(ResourceType.Special, data.specialCost);
            }
            if (short_.Count > 0) list.Add("창고 재고 " + string.Join(" · ", short_) + "(예정지는 둘 수 있고 바닥 물자도 사용)");

            var home = ColonyEvents.HomeCommanders().ToArray();
            void Job(CommanderJobs job, string label)
            {
                if (!home.Any(c => c.AllowsJob(job))) list.Add(label + " 작업을 켠 장수 없음");
            }
            if (data.buildTimeSeconds > 0) Job(CommanderJobs.Building, "건설");
            switch (kind)
            {
                case BuildingKind.Storage: Job(CommanderJobs.Hauling, "운반"); break;
                case BuildingKind.Farm:
                    Job(CommanderJobs.Farming, "농사");
                    var crop = CrashLandingStart.StartingCrop(BiomeRules.Current);
                    if (FarmSeeds.Count(crop) < 1) list.Add($"{GameMenuController.CropName(crop)} 종자 {FarmSeeds.Count(crop)}/1");
                    break;
                case BuildingKind.ScienceLab:
                    Job(CommanderJobs.Research, "연구");
                    var fee = new List<string>();
                    foreach (var (type, need) in new[] { (ResourceType.Food, GameBalance.ScienceFood[0]), (ResourceType.Soil, GameBalance.ScienceSoil[0]) })
                    {
                        var have = rm != null ? rm.GetAmount(type) : 0;
                        if (have < need) fee.Add($"{type.DisplayName()} {have}/{need}");
                    }
                    if (fee.Count > 0) list.Add("첫 연구비 " + string.Join(" · ", fee));
                    break;
                case BuildingKind.TrapPit: case BuildingKind.SpikeTrap: Job(CommanderJobs.Repair, "수리"); break;
                case BuildingKind.Workshop: Job(CommanderJobs.Crafting, "제작"); break;
            }
            return list;
        }
    }
}
