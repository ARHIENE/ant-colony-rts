using System;
using System.Linq;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

namespace AntColony.UI
{
    // 엔딩 화면: 출항 장면 + 4단계에서 누적한 기록 → 메인 메뉴. 점수·등급 없음.
    public sealed partial class GameMenuController
    {
        private static readonly string[] ResourceNames = { "Food", "Soil", "Special" };
        private static readonly string[] ReasonNames = { "채집", "건설", "연구", "유지비", "원정", "거래", "제작", "생산", "치료", "포상", "환불", "기타" };

        public void ShowDeparture()
        {
            var research = CampaignResearch.Instance;
            if (research == null || !research.Departed) return;
            Screen("출항 — GREAT MIGRATION VICTORY"); Time.timeScale = 0;
            var scene = MenuTheme.Text(content, "~ ~ ~   비행선이 소굴을 떠나 새 하늘로 날아오릅니다   ~ ~ ~", 22, 60);
            scene.alignment = TextAnchor.MiddleCenter; scene.color = MenuTheme.Accent;
            foreach (var line in EndingRecord(research)) MenuTheme.Text(content, line, 17, Mathf.Max(30, 24 * (1 + line.Length / 70)));
            MenuTheme.Button(content, "Main Menu", () => { GameSession.Instance.MarkNotStarted(); Main(); });
        }

        // 엔딩 기록 줄. 검사도 같은 줄을 읽는다.
        public static string[] EndingRecord(CampaignResearch research)
        {
            var d = CampaignHistory.Instance != null ? CampaignHistory.Instance.Data : new CampaignHistory.State();
            var months = Mathf.FloorToInt(research.EndingGameSeconds / GameCalendar.SecondsPerMonth);
            var play = TimeSpan.FromSeconds(GameSession.Exists ? GameSession.Instance.PlaySeconds : 0);
            string Names(string kind) { var n = d.milestones.Where(e => e.kind == kind).Select(e => e.name + (e.result.Length > 0 ? $"({e.result})" : "")).ToArray(); return n.Length == 0 ? "없음" : string.Join(", ", n); }
            int Count(string kind) => d.milestones.Count(e => e.kind == kind);
            var reasons = Enum.GetValues(typeof(ResourceReason)).Length;
            var lines = new System.Collections.Generic.List<string>
            {
                $"<b>기간</b>  {months / 12}년 {months % 12}개월   ·   <b>실제 플레이</b> {(int)play.TotalHours}시간 {play.Minutes}분",
            };
            for (var r = 0; r < 3; r++)
            {
                var uses = string.Join(", ", Enumerable.Range(0, reasons).Where(i => d.spentByReason[r * reasons + i] > 0)
                    .Select(i => $"{(i < ReasonNames.Length ? ReasonNames[i] : ((ResourceReason)i).ToString())} {d.spentByReason[r * reasons + i]}"));
                lines.Add($"<b>{ResourceNames[r]}</b>  획득 {d.acquired[r]} · 사용 {d.spent[r]}" + (uses.Length > 0 ? $"  ({uses})" : ""));
            }
            lines.Add($"<b>일반개미</b>  생산 {d.antsProduced} · 잃음 {d.antsLost}");
            lines.Add("<b>탑승</b>  " + (research.Passengers.Count == 0 ? "없음" : string.Join(", ", research.Passengers)));
            lines.Add("<b>남겨진 장수</b>  " + (research.LeftBehind.Count == 0 ? "없음" : string.Join(", ", research.LeftBehind)));
            lines.Add("<b>사망</b>  " + Names("사망"));
            lines.Add("<b>포로</b>  " + Names("포로"));
            lines.Add("<b>합류</b>  " + Names("합류"));
            lines.Add("<b>떠난 장수</b>  " + Names("떠난 장수"));
            lines.Add("<b>처치한 보스</b>  " + Names("보스 처치"));
            lines.Add($"<b>거점</b>  편입 {Count("편입")} · 유기 {Count("유기")}");
            lines.Add($"<b>전쟁·동맹</b>  선전포고 {Count("선전포고")} · 협정 {Count("협정")} · 평화 {Count("평화")}");
            var events = string.Join(", ", Enumerable.Range(0, Math.Min(d.events.Length, EventRules.Names.Length)).Where(i => d.events[i] > 0).Select(i => $"{EventRules.Names[i]} {d.events[i]}"));
            lines.Add("<b>주요 이벤트</b>  " + (events.Length == 0 ? "없음" : events));
            return lines.ToArray();
        }
    }
}
