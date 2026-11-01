using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.World;

namespace AntColony.UI
{
    // 불시착 초반 안내(2026-11-01, 림월드 학습 도우미·ONI 초반 목표 참고): 첫 몇 분에 할 일을 체크리스트로 보여 준다.
    // 판정은 모두 현재 게임 상태에서 읽어 저장 필드가 없다. 전부 끝나거나 설정의 '첫 힌트'를 끄면 숨는다.
    public static class StartGuide
    {
        private static readonly (string label, System.Func<bool> done)[] Steps =
        {
            ("불시착 잔해 회수 — 잔해 선택 후 '철거'(목재·돌)", () => !SceneObjects.All<CrashWreck>().Any()),
            ("나무·풀 채집 지정 — 하단 '작업 지정'", () => SceneObjects.All<ResourceNode>().Any(n => n.NeedsDesignation && n.WorkRequested)),
            ("장수 수만큼 침대 — '+ 건설' → 침대", () => HudOverview.BedShortage == 0),
            ("식량 20 비축 — 열매·버섯 채집", () => ResourceManager.Instance != null && ResourceManager.Instance.GetAmount(AntColony.Data.ResourceType.Food) >= 20),
            ("첫 연구 시작 — 하단 '과학'", () => CampaignResearch.Instance != null && (CampaignResearch.Instance.Active != null || CampaignResearch.Instance.CompletedCount > 0)),
        };

        // 첫 계절(6일) 동안만: 중반에 침대가 모자라도 초반 안내가 다시 뜨지 않게 한다.
        public static bool Active => UserSettings.Current.firstHints && GameSession.Exists && GameSession.Instance.GameStarted
            && GameCalendar.Day < GameCalendar.DaysPerSeason && !Steps.All(s => s.done());

        public static string Text()
        {
            var lines = Steps.Select(s => s.done() ? $"<color=#7fbf7f>✓ {s.label}</color>" : $"<color=#efe7da>□ {s.label}</color>");
            return "<color=#f2a93b><b>시작 안내</b></color>  <size=11><color=#b1b3a7>설정 → 게임 → 처음 보는 기능 도움말로 끄기</color></size>\n" + string.Join("\n", lines);
        }
    }
}
