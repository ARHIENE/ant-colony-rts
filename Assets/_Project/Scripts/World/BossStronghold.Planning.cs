using System.Linq;
using AntColony.Core;

namespace AntColony.World
{
    public sealed partial class BossStronghold
    {
        public string OutcomeSummary
        {
            get
            {
                var progress = CampaignResearch.Instance?.BossProgress;
                if (progress == null) return "보스 정보 준비 중";
                var remaining = progress.RespawnRemaining(Id, GameCalendar.GameSeconds);
                var status = progress.Defeated(Id)
                    ? remaining > 0 ? $"재등장까지 {remaining / GameCalendar.SecondsPerDay:0.0}일" : "재등장 대기 · 아군/수송 수단이 떠나야 함"
                    : site.Boss != null ? $"남은 체력 {site.Boss.CurrentHp:0}/{site.Boss.MaxHp:0}" : "보스 정보 없음";
                var transit = progress.Cargo.Where(p => p.boss == (int)Id).Sum(p => p.count);
                return $"보스 {Id} · {status}\n핵심물: 현지 {progress.UnclaimedCount(Id)} · 운송 {transit} · 본거지 {progress.Home(Id)}\n"
                    + (progress.Defeated(Id) ? "핵심물·장비는 전리품 싣기 후 귀환으로 반입" : Boss.BossProgress.Hazard(Id));
            }
        }
    }
}
