using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

public static class Review1031N2Checks
{
    private static int count;
    private static void Check(bool ok, string label) { if (!ok) throw new Exception("N2 실패: " + label); count++; }
    public static Task<string> Main()
    {
        count = 0;
        var symbols = new HashSet<string>();
        foreach (MapAffiliation affiliation in Enum.GetValues(typeof(MapAffiliation)))
        {
            symbols.Add(WorldMapSymbols.Symbol(affiliation));
            Check(WorldMapSymbols.Diamond(affiliation) == (affiliation == MapAffiliation.Enemy || affiliation == MapAffiliation.Rebel), "마름모 세력 " + affiliation);
        }
        Check(symbols.Count == 5, "다섯 세력 색 없이 구분");
        var civ = new Civilization();
        foreach (ConquestDisposition disposition in Enum.GetValues(typeof(ConquestDisposition)))
        foreach (ExpeditionSiteKind kind in Enum.GetValues(typeof(ExpeditionSiteKind)))
        foreach (bool cleared in new[] { false, true })
        {
            var expected = disposition == ConquestDisposition.Annexed ? MapAffiliation.Ours
                : disposition == ConquestDisposition.Lost || (!cleared && kind != ExpeditionSiteKind.ResourceSite && kind != ExpeditionSiteKind.Empty) ? MapAffiliation.Enemy : MapAffiliation.Neutral;
            Check(WorldMapSymbols.Affiliation(disposition, kind, cleared, null, 0) == expected, "무소속 현장 상태 " + disposition + kind + cleared);
        }
        civ.treaties[(int)TreatyKind.Alliance] = 10;
        Check(WorldMapSymbols.Affiliation(0, 0, false, civ, 9) == MapAffiliation.Ally, "유효 동맹");
        Check(WorldMapSymbols.Affiliation(0, 0, false, civ, 10) == MapAffiliation.Neutral, "만료 즉시 중립");
        civ.war = true;
        Check(WorldMapSymbols.Affiliation(0, 0, false, civ, 0) == MapAffiliation.Enemy, "전쟁 우선");
        civ.rebel = true;
        Check(WorldMapSymbols.Affiliation(0, 0, false, civ, 0) == MapAffiliation.Rebel, "반란 우선");
        Check(WorldMapSymbols.Affiliation(ConquestDisposition.Annexed, 0, false, civ, 0) == MapAffiliation.Ours, "편입 소유 우선");
        Check(WorldMapSymbols.State(ConquestDisposition.Lost, false) == " LOST", "상실 문자");
        Check(WorldMapSymbols.State(ConquestDisposition.Abandoned, false) == " OFF", "포기 문자");
        Check(WorldMapSymbols.State(ConquestDisposition.Annexed, true) == " ATK", "공격 문자");
        Check(WorldMapSymbols.State(ConquestDisposition.Annexed, false) == "", "정상 거점 경고 없음");
        foreach (int difficulty in new[] { -1, 0, 1, 2, 3, 4, 5, 99 })
            Check(WorldMapSymbols.Risk(difficulty) == "D" + Math.Max(1, difficulty), "위험 숫자 " + difficulty);
        Check(PlacementPreviewSymbol.Symbol(true) == "+" && PlacementPreviewSymbol.Symbol(false) == "X", "배치 가능/불가 범례");
        foreach (bool valid in new[] { false, true })
        for (int stroke = 0; stroke < 2; stroke++)
        {
            var a = PlacementPreviewSymbol.Point(valid, stroke, 0); var b = PlacementPreviewSymbol.Point(valid, stroke, 1);
            Check(a.y > .5f && b.y > .5f, "미리보기 윗면 위");
            Check(a.x == -b.x && a.z == -b.z && (a.x != b.x || a.z != b.z), "기호 대칭·0 길이 없음");
            Check(valid ? (stroke == 0 ? a.z == 0 && b.z == 0 : a.x == 0 && b.x == 0)
                : a.x != 0 && a.z != 0 && a.x == (stroke == 0 ? a.z : -a.z), "실제 선 좌표 +/X 구별");
        }
        Check(WorldMapSymbols.Legend.Contains("[X]") && WorldMapSymbols.Legend.Contains("D숫자") && WorldMapSymbols.SelectionLegend.Contains("X 불가"), "색 외 범례");
        return Task.FromResult($"N2 오프라인 {count}검사 통과");
    }

    // 격리 Play 장면에서만 실행. 이번 작업에서는 실행하지 않는다.
    public static async Task<string> RunScene()
    {
        var result = await Main();
        var preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject clone = null;
        var markerObject = new GameObject("N2 Marker", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(MenuTooltip));
        var siteObject = new GameObject("N2 Site");
        try
        {
            var site = siteObject.AddComponent<ExpeditionSite>();
            var textObject = new GameObject("N2 Label", typeof(RectTransform)); textObject.transform.SetParent(markerObject.transform, false);
            var text = textObject.AddComponent<UnityEngine.UI.Text>();
            var marker = markerObject.AddComponent<WorldMapMarker>(); marker.Initialize(text); marker.Refresh(site, Color.gray);
            Check(text.text.Contains("[!]") && text.text.Contains("D1"), "월드맵 실제 라벨 연결");
            Check(Math.Abs(markerObject.transform.Find("Faction Shape").localEulerAngles.z - 45) < .01f, "실제 적 마름모 회전");
            Check(markerObject.GetComponent<MenuTooltip>().Message.Contains(WorldMapSymbols.Legend), "실제 툴팁 범례 연결");
            var symbol = preview.AddComponent<PlacementPreviewSymbol>(); symbol.Refresh(false);
            Check(preview.GetComponentsInChildren<LineRenderer>().Length == 2, "불가 기호 두 선");
            clone = UnityEngine.Object.Instantiate(preview); clone.GetComponent<PlacementPreviewSymbol>().Refresh(true);
            Check(clone.GetComponentsInChildren<LineRenderer>().Length == 2, "연속 배치 복제 중복 없음");
            var selection = target.AddComponent<AntColony.Units.SelectableObject>(); selection.SetSelected(true);
            var line = target.GetComponentInChildren<LineRenderer>();
            Check(line != null && line.enabled && line.loop && line.positionCount == 4 && line.widthMultiplier == .16f, "선택 굵은 사각 외곽");
            selection.SetSelected(false); Check(!line.enabled, "해제 외곽 숨김");
            selection.SetSelected(true); target.SetActive(false); Check(!line.enabled, "비활성 외곽 숨김");
            return result + " · 장면8검사 통과 (월드맵/화면 육안 별도)";
        }
        finally
        {
            UnityEngine.Object.Destroy(preview); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(markerObject); UnityEngine.Object.Destroy(siteObject);
            if (clone != null) UnityEngine.Object.Destroy(clone);
        }
    }
}
