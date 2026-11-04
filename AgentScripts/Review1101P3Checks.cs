using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

public static class Review1101P3Checks
{
    private static int count;
    private static void Check(bool ok, string label) { if (!ok) throw new Exception("P3 실패: " + label); count++; }
    private static bool Korean(string value) => !string.IsNullOrEmpty(value) && value.Any(c => c >= '가' && c <= '힣');
    private static void Names<T>(Func<T, string> name) where T : Enum
    {
        var values = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
        foreach (var value in values) Check(Korean(name(value)), typeof(T).Name + ": " + value);
        Check(values.Select(name).Distinct().Count() == values.Length, typeof(T).Name + " 표시 이름 구분");
    }
    public static async Task<string> Main()
    {
        count = 0;
        Names<AntColony.Buildings.AirshipPart>(AntColony.UI.DisplayNames.RocketPart);
        Names<AntColony.Buildings.RocketPhase>(AntColony.UI.DisplayNames.RocketPhase);
        Names<AntColony.Core.DefenseLine>(AntColony.UI.DisplayNames.Defense);
        Names<AntColony.World.ExpeditionState>(AntColony.UI.DisplayNames.Expedition);
        Names<AntColony.World.TreatyKind>(AntColony.UI.DisplayNames.Treaty);
        Names<AntColony.World.ArtworkPurpose>(AntColony.UI.DisplayNames.Artwork);
        Names<AntColony.World.ConquestDisposition>(AntColony.UI.DisplayNames.Conquest);
        Names<AntColony.World.WildlifeTemperament>(AntColony.UI.DisplayNames.Wildlife);
        Names<AntColony.Units.CommanderPersonality>(AntColony.UI.DisplayNames.Personality);
        Names<AntColony.Core.MapSize>(AntColony.Core.MapSizes.Label);
        Names<AntColony.Core.DifficultyLevel>(AntColony.Core.DifficultyProfile.Label);
        foreach (AntColony.Data.ResourceType resource in Enum.GetValues(typeof(AntColony.Data.ResourceType)))
            Check(Korean(AntColony.Data.ResourceLabels.DisplayName(resource)), "자원 이름 " + resource);
        foreach (AntColony.Units.CommanderTrait trait in Enum.GetValues(typeof(AntColony.Units.CommanderTrait)))
            Check(Korean(AntColony.Units.CommanderTraits.DisplayName(trait)), "특성 이름 " + trait);
        foreach (AntColony.Buildings.FarmCrop crop in Enum.GetValues(typeof(AntColony.Buildings.FarmCrop)))
            Check(Korean(AntColony.UI.GameMenuController.CropName(crop)), "작물 이름 " + crop);
        foreach (var key in new[] { "Next Prison", "Next Post", "Dispatch", "Next POW", "Persuade", "Execute" })
            Check(Korean(AntColony.UI.DisplayNames.AcquisitionButton(key)), "영입 버튼 이름 " + key);
        foreach (var old in new[] { "Off", "Ready", "Home", "Outbound", "Deployed", "Returning", "Stopped: destination lost",
            "Stopped: check site and worker crew", "Waiting for home storage", "Waiting for destination", "Stopped: destination changed",
            "Stopped: resource unreachable", "Collecting / depositing", "Waiting for crew / garrison cargo", "Next trip in 2.5s", "Moving to stop 3" })
            Check(Korean(AntColony.UI.DisplayNames.RouteStatus(old)), "구저장 수송 표시 " + old);
        Check(AntColony.UI.DisplayNames.RouteStatus("Next trip in 2.5s") == "다음 출발까지 2.5초", "남은 시간 값 보존");
        Check(AntColony.UI.DisplayNames.RouteStatus("Moving to stop 3") == "경유지 3번으로 이동 중", "경유지 번호 보존");
        Check(AntColony.UI.DisplayNames.RouteStatus("직접 지정 사유") == "직접 지정 사유", "기존 한국어 상태 보존");
        foreach (var size in new[] { "Small", "Medium", "Large" })
        foreach (var difficulty in new[] { "Gentle", "Normal", "Harsh" })
        {
            var label = AntColony.UI.DisplayNames.SaveLabel(size + " / " + difficulty);
            Check(Korean(label) && !label.Any(c => c >= 'A' && c <= 'Z'), "구저장 라벨 " + size + difficulty);
        }
        Check(AntColony.UI.DisplayNames.SaveLabel("사용자 문구") == "사용자 문구", "기존 한국어 라벨 보존");
        Check(AntColony.Save.SaveSlots.DisplayName(false, 0) == "Slot 1", "저장 버튼 식별자 유지");
        Check(AntColony.Save.SaveSlots.FileName(false, 0) == "manual1.json", "저장 파일명 유지");
        Check(new AntColony.Save.SaveSlots.SlotInfo().Summary() == "비어 있음", "빈 슬롯 안내");
        Check(!AntColony.Save.SaveValidator.Validate(null, out var error) && error == "저장 데이터가 없습니다.", "데이터 없음 오류 한국어");
        var method = typeof(AntColony.UI.GameMenuController).GetMethod("ActionName", BindingFlags.Static | BindingFlags.NonPublic);
        foreach (AntColony.Core.GameAction action in Enum.GetValues(typeof(AntColony.Core.GameAction)))
            Check(Korean((string)method.Invoke(null, new object[] { action })), "단축키 동작 이름 " + action);
        Check(AntColony.UI.WorldMapSymbols.State(AntColony.World.ConquestDisposition.Lost, false) == " 상실", "상실 표시");
        Check(AntColony.UI.WorldMapSymbols.State(AntColony.World.ConquestDisposition.Annexed, true) == " 공격 중", "공격 표시");
        await Task.CompletedTask;
        return "P3 " + count + "검사 통과 (문자열·이름·구저장 표시; Unity 화면은 별도 확인)";
    }

    // Claude 전용: 격리 Play에서 확인할 화면을 먼저 열고 실행한다. 화면을 바꾸거나 게임을 진행시키지 않는다.
    public static async Task<string> RunP3Scene()
    {
        var result = await Main();
        var texts = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(UnityEngine.FindObjectsInactive.Exclude);
        var fragments = new[] { "Build Storage", "Select 1 Commander", "research complete", "construction complete", "Commander Acquisition", "Not enough food", "Autosaved", "Boss HP", "watchtower alert", "Collecting / depositing" };
        var leaks = texts.Where(t => fragments.Any(f => t.text.Contains(f))).Select(t => t.name + ": " + t.text).ToArray();
        Check(leaks.Length == 0, "현재 화면의 기존 영어 문구: " + string.Join(" / ", leaks));
        return result + "\n활성 텍스트 " + texts.Length + "개 검사. 한글 줄바꿈·겹침·잘림은 Claude 육안 확인 필요.";
    }
}
