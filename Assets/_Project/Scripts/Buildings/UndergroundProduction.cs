using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    // 기존 시추기의 잠정 처리량·전력·건설비를 재사용한다. 최종 설비 밸런스 확정 시 교체.
    public static class UndergroundProduction
    {
        public const int MachineGrade = 4; // 강철급. 성휘석의 티타늄 합금급(5) 요구는 우회하지 않는다.
        public static bool IsSolidDrill(BuildingKind kind) => kind is BuildingKind.DeepDrill or BuildingKind.AdvancedDeepDrill;
        public static int Grade(BuildingKind kind) => kind == BuildingKind.AdvancedDeepDrill ? 5 : MachineGrade;
        public static bool IsExtractor(BuildingKind kind) => kind is BuildingKind.DeepDrill or BuildingKind.AdvancedDeepDrill or BuildingKind.OilDrill or BuildingKind.GasExtractor;
        public static int RequiredGrade(ResourceType type) => type == ResourceType.StellarOre ? 5
            : type == ResourceType.TitaniumOre ? 4 : type == ResourceType.IronOre ? 3 : 2;
        public static bool Matches(BuildingKind kind, ResourceType type) => kind == BuildingKind.OilDrill ? type == ResourceType.CrudeOil
            : kind == BuildingKind.GasExtractor ? type == ResourceType.NaturalGas
            : IsSolidDrill(kind) && type is ResourceType.IronOre or ResourceType.CopperOre or ResourceType.Coal or ResourceType.TitaniumOre or ResourceType.StellarOre;
        public static UndergroundSurvey.Layer LayerAt(BuildingKind kind, Vector3 at) => UndergroundSurvey.Instance?.Layers.FirstOrDefault(l => l.discovered && l.type >= 0
            && Matches(kind, (ResourceType)l.type) && UndergroundSurvey.Instance.MapAt(at) == l.map
            && Mathf.Abs(at.x - l.position.x) <= 1.5f && Mathf.Abs(at.z - l.position.z) <= 1.5f);
        public static SupplyFacility SurfaceOil(Vector3 at) => SupplyFacility.All.Where(s => s.Operational && s.Kind == BuildingKind.OilDeposit
            && (s.Position - at).sqrMagnitude <= 16).OrderBy(s => (s.Position - at).sqrMagnitude).FirstOrDefault();
        public static bool Occupied(UndergroundSurvey.Layer layer, BuildingBase ignore = null) => layer != null
            && (SupplyFacility.All.Any(s => s != ignore && s.Operational && IsExtractor(s.Kind) && LayerAt(s.Kind, s.Position)?.id == layer.id)
                || SceneObjects.All<BuildingConstructionSite>().Any(s => s.Building != null && s.Building.GetComponent<BuildingBase>() != ignore
                    && s.BuildingKind.HasValue && IsExtractor(s.BuildingKind.Value) && LayerAt(s.BuildingKind.Value, s.Position)?.id == layer.id));
        public static string PlacementReason(BuildingKind kind, Vector3 at, BuildingBase ignore = null)
        {
            if (!IsExtractor(kind)) return null;
            var layer = LayerAt(kind, at);
            if (layer == null && kind == BuildingKind.OilDrill && SurfaceOil(at) is SupplyFacility source)
            {
                if (source.Supply.depositRemaining < 1) return "고갈된 지표 원유 매장지입니다.";
                return SupplyFacility.All.Any(s => s != ignore && s.Operational && s.Kind == kind && SurfaceOil(s.Position) == source)
                    || SceneObjects.All<BuildingConstructionSite>().Any(s => s.BuildingKind == kind && SurfaceOil(s.Position) == source)
                    ? "이 지표 매장지에는 시추기 또는 예정지가 이미 있습니다." : null;
            }
            if (layer == null) return "발견한 해당 매장층의 3×3 범위에 설치하세요.";
            if (layer.remaining < 1) return "고갈된 매장층입니다.";
            if (Occupied(layer, ignore)) return "이 매장층에는 채취 시설 또는 건설 예정지가 이미 있습니다.";
            return null;
        }
        public static int Batch(SupplyFacility facility)
        {
            var layer = LayerAt(facility.Kind, facility.Position); var research = CampaignResearch.Instance;
            var surface = layer == null && facility.Kind == BuildingKind.OilDrill ? SurfaceOil(facility.Position) : null;
            var remaining = layer?.remaining ?? surface?.Supply.depositRemaining ?? 0;
            if (remaining < 1 || research == null) return 0;
            var type = layer == null ? ResourceType.CrudeOil : (ResourceType)layer.type;
            if (research.CanMine(type, Grade(facility.Kind), RequiredGrade(type))) return Mathf.Min(10, Mathf.FloorToInt(remaining));
            // 연구 전 완전한 표본 한 개만. 바닥·배관·운반 중 표본도 중복 생산하지 않는다.
            if (!research.CanRecoverSample(type, Grade(facility.Kind), RequiredGrade(type))
                || ResourceNode.Available.Any(n => n.IsLooseCargo && n.ResourceType == type && n.AmountRemaining >= 1)
                || SupplyFacility.All.Any(s => s.Supply.fluid == (int)type && s.Supply.amount > 0)
                || AntUnitBase.Active.OfType<WorkerAnt>().Any(w => w.CarriedType == type && w.CarriedAmount >= 1)) return 0;
            return 1;
        }
        public static int FloorRoom(SupplyFacility facility) => Mathf.Max(0, SupplyFacility.Capacity - Mathf.CeilToInt(ResourceNode.Available
            .Where(n => n.IsLooseCargo && (n.transform.position - facility.Position).sqrMagnitude <= 16).Sum(n => n.AmountRemaining)));
        public static bool Ready(SupplyFacility facility) => facility != null && facility.Operational && facility.CountsTowardPlayerDefeat && facility.Powered
            && ScienceEffects.BuildingUnlocked(facility.Kind) && PlacementReason(facility.Kind, facility.Position, facility) == null && Batch(facility) > 0
            && (IsSolidDrill(facility.Kind) ? FloorRoom(facility) >= Batch(facility)
                : FluidNetwork.CanDeliver(facility, facility.Kind == BuildingKind.GasExtractor ? ResourceType.NaturalGas : ResourceType.CrudeOil, Batch(facility)));
        internal static int Take(SupplyFacility facility, int amount)
        {
            var survey = UndergroundSurvey.Instance; var l = LayerAt(facility.Kind, facility.Position);
            if (amount <= 0 || amount > Batch(facility)) return 0;
            if (l == null && facility.Kind == BuildingKind.OilDrill && SurfaceOil(facility.Position) is SupplyFacility source)
            {
                source.Supply.depositRemaining -= amount;
                if (source.Supply.depositRemaining == 0) AntColony.UI.ToastManager.ShowAt("지표 원유 매장지 고갈", () => AntColony.Core.SceneObjects.Any<AntColony.Camera.IsometricCameraController>()?.FocusOn(source.Position));
                return amount;
            }
            if (l == null) return 0;
            return Mathf.FloorToInt(CampaignResearch.Instance.ResourceResearched((ResourceType)l.type)
                ? survey.Extract(l.id, facility.Position, (ResourceType)l.type, amount)
                : survey.ExtractSample(l.id, facility.Position, (ResourceType)l.type));
        }
        public static string Status(SupplyFacility facility)
        {
            var layer = LayerAt(facility.Kind, facility.Position);
            if (layer == null && (facility.Kind != BuildingKind.OilDrill || SurfaceOil(facility.Position) == null)) return "해당 매장층 위에 설치 필요";
            var reason = PlacementReason(facility.Kind, facility.Position, facility);
            if (reason != null) return reason;
            if (!facility.Powered) return "정전 · 진행 보존";
            var type = layer == null ? ResourceType.CrudeOil : (ResourceType)layer.type;
            if (Batch(facility) == 0) return RequiredGrade(type) > Grade(facility.Kind)
                && CampaignResearch.Instance.ResourceResearched(type) ? "기계 등급 부족 · 티타늄 합금급 필요" : "표본 반입·자원 연구 대기";
            if (!Ready(facility)) return IsSolidDrill(facility.Kind) ? "바닥 출력 공간 부족 · 운반 대기" : "출력 공간 부족 · 배관/탱크 확인";
            return IsSolidDrill(facility.Kind) ? "채집 담당 작업 대기/진행" : "자동 추출 중";
        }
        public static string Preview(BuildingKind kind, Vector3 position)
        {
            var l = LayerAt(kind, position);
            var surface = l == null && kind == BuildingKind.OilDrill ? SurfaceOil(position) : null;
            return (l == null ? surface == null ? "해당 발견 매장층 필요" : $"지표 원유 잔량 {surface.Supply.depositRemaining}" : $"{((ResourceType)l.type).DisplayName()} 잔량 {l.remaining:0}")
                + $" · 기본 10/{SupplyFacility.ProcessSeconds:0}초 · {SupplyFacility.DrillWatts:0}W"
                + (IsSolidDrill(kind) ? (kind == BuildingKind.AdvancedDeepDrill ? " · 티타늄 합금급 · 작업자 필요" : " · 강철급 · 작업자 필요") : " · 자동 추출");
        }
    }
}
