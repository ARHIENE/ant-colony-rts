using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;

namespace AntColony.UI
{
    // 저장값·식별자는 유지하고 화면에 표시하는 enum 이름만 번역한다.
    public static class DisplayNames
    {
        public static string Target(UnityEngine.Component value) => value switch
        {
            BuildingBase building => building.Data?.displayName ?? "건물",
            BuildingConstructionSite site => (site.Building != null ? site.Building.GetComponent<BuildingBase>()?.Data?.displayName : null) + " 건설 예정지",
            ResourceNode node => node.ResourceType.DisplayName(),
            _ => "대상"
        };
        public static string SaveLabel(string value) => (value ?? "").Replace("Small", "작음").Replace("Medium", "보통 크기").Replace("Large", "큼")
            .Replace("Gentle", "쉬움").Replace("Normal", "보통 난이도").Replace("Harsh", "어려움");
        public static string RouteStatus(string value) => value switch
        {
            "Off" => "꺼짐", "Ready" => "출발 준비", "Home" => "본거지", "Outbound" => "출정 중", "Deployed" => "현지 활동 중", "Returning" => "귀환 중",
            "Stopped: destination lost" => "중단: 목적지 상실", "Stopped: check site and worker crew" => "중단: 편입 거점과 채집 가능한 탑승 장수를 확인하세요",
            "Waiting for home storage" => "본거지 하역 대기 · 창고 여유를 확인하세요", "Waiting for destination" => "목적지 출발 조건 대기",
            "Stopped: destination changed" => "중단: 목적지 변경", "Stopped: resource unreachable" => "중단: 자원까지 이동할 수 없음",
            "Collecting / depositing" => "채집·적재 중", "Waiting for crew / garrison cargo" => "승무원·주둔 부대 화물 대기",
            _ => LegacyRouteProgress(value)
        };
        private static string LegacyRouteProgress(string value)
        {
            if (value == null) return "꺼짐";
            if (value.StartsWith("Next trip in ") && value.EndsWith("s")) return "다음 출발까지 " + value.Substring(13, value.Length - 14) + "초";
            if (value.StartsWith("Moving to stop ")) return "경유지 " + value.Substring(15) + "번으로 이동 중";
            return value;
        }
        public static string AcquisitionButton(string value) => value switch
        {
            "Next Prison" => "다음 수용소", "Next Post" => "다음 초소", "Dispatch" => "정찰 파견",
            "Next POW" => "다음 포로", "Persuade" => "회유", "Execute" => "처형", _ => value
        };
        public static string RocketPart(AirshipPart value) => value switch
        {
            AirshipPart.Hull => "선체", AirshipPart.Engine => "엔진", AirshipPart.Cocoon => "50석 확장 모듈", _ => "로켓 부품"
        };
        public static string RocketPhase(AntColony.Buildings.RocketPhase value) => value switch
        {
            AntColony.Buildings.RocketPhase.Construction => "건설 중", AntColony.Buildings.RocketPhase.Preparing => "발사 준비 중",
            AntColony.Buildings.RocketPhase.Boarding => "탑승 가능", AntColony.Buildings.RocketPhase.Launched => "발사 완료",
            AntColony.Buildings.RocketPhase.Destroyed => "파괴됨", _ => "상태 확인 필요"
        };
        public static string Defense(DefenseLine value) => value switch
        {
            DefenseLine.Firepower => "화력", DefenseLine.Range => "사거리", DefenseLine.Durability => "내구도", DefenseLine.Traps => "함정", _ => "방어 강화"
        };
        public static string Expedition(ExpeditionState value) => value switch
        {
            ExpeditionState.Home => "본거지", ExpeditionState.Outbound => "출정 중", ExpeditionState.Deployed => "현지 활동 중", ExpeditionState.Returning => "귀환 중", _ => "상태 확인 필요"
        };
        public static string Treaty(TreatyKind value) => value switch
        {
            TreatyKind.Trade => "교역 협정", TreatyKind.NonAggression => "불가침 협정", TreatyKind.Alliance => "동맹", _ => "협정"
        };
        public static string Artwork(ArtworkPurpose value) => value switch
        {
            ArtworkPurpose.Contact => "접촉", ArtworkPurpose.Peace => "평화", ArtworkPurpose.Release => "석방", _ => "걸작 선물"
        };
        public static string Conquest(ConquestDisposition value) => value switch
        {
            ConquestDisposition.Undecided => "처리 미정", ConquestDisposition.Annexed => "편입", ConquestDisposition.Abandoned => "포기", ConquestDisposition.Lost => "상실", _ => "처리 미정"
        };
        public static string Wildlife(WildlifeTemperament value) => value switch
        {
            WildlifeTemperament.Timid => "도주형", WildlifeTemperament.Defensive => "방어형", WildlifeTemperament.Predator => "포식형", _ => "성향 미확인"
        };
        public static string Personality(CommanderPersonality value) => value switch
        {
            CommanderPersonality.Balanced => "균형적", CommanderPersonality.Brave => "용감함", CommanderPersonality.Cautious => "신중함", CommanderPersonality.Devoted => "헌신적", _ => "성향 미확인"
        };
        public static string Decoration(BuildingKind value) => value switch
        {
            BuildingKind.FlowerPot => "화분", BuildingKind.ShellDecoration => "조개 장식", BuildingKind.MarbleMosaic => "대리석 모자이크",
            BuildingKind.BottleMobile => "병 모빌", BuildingKind.FireflyLamp => "반딧불 등", _ => "장식"
        };
    }
}
