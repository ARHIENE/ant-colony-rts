using System;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        public void EventLog()
        {
            Screen("이벤트 로그");
            var history = CampaignHistory.Instance;
            if (history != null)
            {
                var events = ColonyEvents.Instance;
                if (events != null) MenuTheme.Text(content, $"한파 {events.ColdRemaining:0}초 | 홍수 {events.FloodRemaining:0}초 | 가뭄 {events.DroughtRemaining:0}초", 18, 40);
                foreach (var e in history.Data.recent.AsEnumerable().Reverse())
                {
                    var month = (int)(e.seconds / GameCalendar.SecondsPerMonth);
                    MenuTheme.Text(content, $"{month / 12 + 1}년 {month % 12 + 1}월 — {e.name} ({e.kind})\n{e.result}", 18, 84);
                }
                if (history.Data.recent.Count == 0) MenuTheme.Text(content, "아직 기록된 사건이 없습니다.");
                MenuTheme.Text(content, "누적 기록", 23, 45);
                foreach (ResourceType resource in Enum.GetValues(typeof(ResourceType)))
                {
                    var i = (int)resource;
                    MenuTheme.Text(content, $"{resource}: 획득 {history.Data.acquired[i]} / 사용 {history.Data.spent[i]}", 18, 36);
                    foreach (ResourceReason reason in Enum.GetValues(typeof(ResourceReason)))
                    {
                        var spent = history.Data.spentByReason[i * Enum.GetValues(typeof(ResourceReason)).Length + (int)reason];
                        if (spent > 0) MenuTheme.Text(content, $"  {reason}: {spent}", 16, 28);
                    }
                }
                MenuTheme.Text(content, $"일반개미 생산·합류 {history.Data.antsProduced} / 손실 {history.Data.antsLost}", 18, 40);
            }
            MenuTheme.Button(content, "새로고침", EventLog);
            MenuTheme.Button(content, "게임으로", Resume);
        }
    }
}
