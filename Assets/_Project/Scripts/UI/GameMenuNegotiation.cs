using System.Linq;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.UI;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // 협상 페이지(2026-10-09): 열려 있는 동안 게임 시간을 멈추고, 닫으면 이전 속도로 돌아가며 협상은 보류로 오른쪽 알림에 남는다.
    public sealed partial class GameMenuController
    {
        public void OpenTalk(Negotiation n)
        {
            var d = DiplomacyManager.Instance; var c = d?.CivOf(n);
            if (c == null || !d.Data.talks.Contains(n)) { ToastManager.Show("이미 끝난 협상입니다."); return; }
            d.CheckInvalid(n);
            // 담당 장수가 원정·치료 등으로 참여할 수 없으면 비운다(다른 장수 선택). 이력·인내는 유지.
            if (!DiplomacyManager.CanNegotiate(DiplomacyManager.Negotiator(n))) n.negotiator = "";
            tradeGive = n.give; tradeTake = n.take; rebuildTrade = () => OpenTalk(n);
            var f = Frame("협상 · " + c.name);
            Time.timeScale = 0; // 메뉴 일시정지 설정과 무관하게 협상 중에는 항상 정지(기한·타이머 포함)
            var p = L.Plate(f, "TalkPanel", 48, 48, 1344, 740);
            var badge = L.Box(p, "Badge", 14, 12, 36, 36, c.color);
            L.Label(badge, c.name.Substring(0, 1), 16, 0, 0, 36, 36, MenuTheme.AccentInk, TextAnchor.MiddleCenter, true);
            L.Label(p, $"<b>{c.name}</b>  <color=#968976>{c.leader} · {DiplomacyManager.KindName(n.kind)}</color>", 16, 60, 10, 520, 22);
            L.Label(p, $"관계 {(c.war ? "전쟁" : "평화")} · 호감도 <b>{c.affinity:+0;-0;0}</b> · 평판: {d.ReputationSummary()}", 12, 60, 32, 700, 20, MenuTheme.Muted);
            L.Line(p, 0, 60, 1344);

            if (n.invalid)
            {
                // 무효: 상황 변화 대사와 확인 버튼만. ponytail: 대사는 기획 미정이라 잠정 문구.
                L.Label(p, $"<b>{c.leader}</b>: \"이야기하던 포로는 더 이상 거래 대상이 아니오. 이 협상은 없던 일로 하겠소.\"", 18, 60, 260, 1224, 60, align: TextAnchor.MiddleCenter);
                L.Button(p, "Talk Confirm", "확인", 572, 360, 200, 40, () => { d.EndTalk(n, "무효"); Resume(); }, null, true);
                return;
            }

            // 담당 장수: 클릭할 때마다 다음 가능 장수로 바뀐다.
            var eligible = CommanderRoster.Instance.Commanders.Where(DiplomacyManager.CanNegotiate).OrderByDescending(a => a.Talents.Level(CommanderActivity.Politics)).ToList();
            var current = DiplomacyManager.Negotiator(n);
            L.Button(p, "Talk Negotiator", current == null ? "담당 장수 지정 ▸" : $"담당: {current.CommanderName} (정치 {current.Talents.Level(CommanderActivity.Politics)}) ▸", 760, 12, 300, 36, () =>
            {
                if (eligible.Count == 0) { ToastManager.Show("본거지에서 활동 가능한 장수가 없습니다."); return; }
                var next = eligible[(eligible.IndexOf(DiplomacyManager.Negotiator(n)) + 1) % eligible.Count];
                n.negotiator = next.PersonalState.id; OpenTalk(n);
            });
            L.Label(p, "인내", 13, 1076, 12, 50, 18, MenuTheme.Muted);
            L.Meter(p, 1076, 34, 250, 8, Mathf.Clamp01(n.patience / 100f), n.patience > 30 ? MenuTheme.Hp : MenuTheme.Danger);
            var left = d.Remaining(n);
            if (left >= 0) L.Label(p, $"남은 기한 {(int)left / 60}:{(int)left % 60:00}", 12, 1176, 12, 150, 18, MenuTheme.Warning, TextAnchor.MiddleRight);

            var status = n.kind == TalkKind.Trade || n.kind == TalkKind.Demand
                ? $"상대 반응: <b>{DiplomacyManager.ReactionName(d.Reaction(c, n))}</b>" + (current == null ? " <color=#968976>(담당 장수 없음)</color>" : "")
                : $"요청: <b>{DiplomacyManager.Summary(n.give)}</b>" + (n.take.Empty ? "" : " → 받는 것 " + DiplomacyManager.Summary(n.take)) + $"   <color=#968976>{d.RefusalRisk(n)}</color>";
            L.Label(p, status, 15, 14, 66, 1316, 26);
            if (n.last != "") L.Label(p, "최근 응답: " + n.last, 13, 14, 92, 1316, 20, MenuTheme.Accent);

            if (n.kind == TalkKind.Trade || n.kind == TalkKind.Demand)
            {
                var names = new[] { "자원", "장비", "거점", "포로 장수", "협정" };
                for (var i = 0; i < names.Length; i++)
                {
                    var tab = i;
                    var button = L.Button(p, names[i], names[i], 14 + i * 104, 118, 100, 30, () => { tradeTab = tab; OpenTalk(n); }, null, false, 13);
                    if (i == tradeTab) button.GetComponent<Outline>().effectColor = MenuTheme.Accent;
                }
                var mine = L.List(p, 14, 156, 640, 520, 6); var theirs = L.List(p, 690, 156, 640, 520, 6);
                if (n.kind == TalkKind.Trade) TradeSide(mine, c, true, () => { }); else MenuTheme.Text(mine, "요구는 상대에게 받을 것만 고릅니다. 군사력·위협 신뢰가 수락에 영향을 줍니다.", 13, 40).color = MenuTheme.Muted;
                TradeSide(theirs, c, false, () => { });
            }
            else if (n.kind != TalkKind.TreatyOffer)
            {
                // 상대 요청 역제안: 지급량을 줄여 다시 제시(원래 요청 대비 비율·정치력으로 판단).
                var amount = L.Label(p, $"지급 식량 {n.give.resources[0]} / 요청 {n.asked}", 14, 14, 140, 400, 24);
                L.Button(p, "Talk Less", "− 25", 420, 138, 80, 30, () => { n.give.resources[0] = Mathf.Max(0, n.give.resources[0] - 25); OpenTalk(n); });
                L.Button(p, "Talk More", "+ 25", 506, 138, 80, 30, () => { n.give.resources[0] = Mathf.Min(n.asked, n.give.resources[0] + 25); OpenTalk(n); });
            }
            var log = L.List(p, 14, 600, 640, 80, 2);
            foreach (var line in n.log.Take(4)) MenuTheme.Text(log, line, 12, 18).color = MenuTheme.Muted;

            L.Line(p, 0, 686, 1344);
            L.Button(p, "Talk Hold", "보류하고 닫기   Esc", 14, 696, 200, 34, Resume);
            L.Button(p, "Talk Back", "외교로", 222, 696, 110, 34, () => Diplomacy(c));
            string Act(string result) { if (!string.IsNullOrEmpty(result)) { n.log.Insert(0, result); ToastManager.Show(result); } return result; }
            void After() { if (d.Data.talks.Contains(n)) OpenTalk(n); else Diplomacy(c); }
            if (n.kind == TalkKind.Trade)
                L.Button(p, "Talk Propose", "제안", 1130, 696, 200, 34, () => { Act(d.Propose(n)); After(); }, null, true);
            else if (n.kind == TalkKind.Demand)
            {
                if (n.deadline > 0)
                {
                    L.Button(p, "Talk Execute", "최후통첩 실행 (선전포고)", 860, 696, 260, 34, () => { d.ExecuteUltimatum(n); Diplomacy(c); }, null, true).GetComponentInChildren<Text>().color = MenuTheme.Danger;
                    L.Button(p, "Talk Withdraw", "철회 (위협 신뢰 하락)", 1130, 696, 200, 34, () => { d.WithdrawUltimatum(n); Diplomacy(c); });
                }
                else
                {
                    L.Button(p, "Talk Ultimatum", n.ultimatum ? "최후통첩: 켬 (거절 시 공격)" : "최후통첩: 끔 (일반 요구)", 860, 696, 260, 34, () => { n.ultimatum = !n.ultimatum; OpenTalk(n); });
                    L.Button(p, "Talk Propose", "요구", 1130, 696, 200, 34, () => { Act(d.Propose(n)); After(); }, null, true);
                }
            }
            else
            {
                L.Button(p, "Talk Refuse", "거절", 860, 696, 130, 34, () => { Act(d.Respond(n, false)); After(); });
                L.Button(p, "Talk Accept", n.kind == TalkKind.TreatyOffer || n.give.resources[0] >= n.asked ? "수락" : "역제안", 1000, 696, 330, 34, () => { Act(d.Respond(n, true)); After(); }, null, true);
            }
        }
    }
}
