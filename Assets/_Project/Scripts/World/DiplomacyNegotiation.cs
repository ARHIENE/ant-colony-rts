using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    // Trade = 우리가 제안하는 거래, Demand = 우리의 요구(최후통첩 가능), 나머지는 상대가 먼저 꺼낸 요청.
    public enum TalkKind { Trade, Demand, TreatyOffer, AidRequest, Tribute }
    public enum TalkReaction { Warm, Hesitant, Cold }

    // 협상 한 건. 페이지를 닫아도 보류로 남고(자원 예약 없음), 성립·거절·결렬·기한 만료까지 오른쪽 알림에 유지된다.
    [Serializable] public sealed class Negotiation
    {
        public int id;
        public string civ, negotiator = "";
        public TalkKind kind;
        public TradeOffer give = new TradeOffer(), take = new TradeOffer(); // give = 우리가 내는 것, take = 우리가 받는 것
        public int patience = 100, asked;     // asked: 상대 요청의 원래 가치(역제안 판단 기준)
        public float expires, deadline;       // expires: 상대 요청 기한, deadline: 최후통첩 거절 후 실행/철회 기한
        public bool ultimatum, invalid;
        public double lastRatio = -1;
        public string last = "";
        public List<string> log = new List<string>();
    }

    // 2026-10-08/09 정치 기반 협상: 담당 장수·대략적 반응·수락/거절/역제안·인내·보류·최후통첩·외교/위협 신뢰.
    public sealed partial class DiplomacyManager
    {
        // ponytail: 기획 미정 수치(잠정). 반응 구간, 정치 설득 최대 10%, 인내 소모 12(반복 25), 결렬 대기 1개월, 크게 개선 = 1.1배.
        public const double WarmRatio = 1, HesitantRatio = .85, CounterRatio = .6, ImprovedRatio = 1.1;
        public const int PatienceCost = 12, RepeatPatienceCost = 25;

        public IEnumerable<Negotiation> Talks(Civilization c) => Data.talks.Where(n => n.civ == c.id);
        public Civilization CivOf(Negotiation n) => Data.civilizations.Concat(Data.markets).FirstOrDefault(c => c.id == n.civ);
        public Negotiation StartTalk(Civilization c, TalkKind kind)
        {
            var n = Data.talks.Find(x => x.civ == c.id && x.kind == kind);
            if (n != null) return n;
            n = new Negotiation { id = ++Data.nextTalk, civ = c.id, kind = kind };
            Data.talks.Add(n); return n;
        }
        public void EndTalk(Negotiation n, string result)
        {
            Data.talks.Remove(n);
            var c = CivOf(n); if (c != null && result != null) CampaignHistory.Record("협상", c.name, result, true);
        }

        // 담당 장수: 본거지에서 활동 가능한 장수만(원정·수송·전투·치료·쓰러짐·귀환 중 제외).
        public static bool CanNegotiate(CommanderAnt a) => a != null && !a.IsDead && a.IsColonyMember && !a.IsCaptive && !a.IsAwayFromHome
            && !a.IsDeployed && a.CanReceiveOrders && !a.PersonalState.treating && a.PersonalHealth > 0 && !a.IsChild;
        public static CommanderAnt Negotiator(Negotiation n) => CommanderRoster.Instance?.Commanders.FirstOrDefault(c => c.PersonalState.id == n.negotiator);
        public static float Politics(Negotiation n) => Negotiator(n)?.Talents.Level(CommanderActivity.Politics) ?? 0;
        // 정치력은 애매한 조건의 설득·추가 양보에만 쓴다(좋은 조건은 정치력과 무관하게 성사).
        public static double Persuasion(Negotiation n) => Mathf.Clamp(Politics(n), 0, 20) * .005;

        // 무효: 대상 포로가 회유·사망·이미 석방되어 사라짐. 자원 부족은 무효가 아니라 조건 수정 대상.
        public bool CheckInvalid(Negotiation n)
        {
            var c = CivOf(n); if (n.invalid || c == null) return n.invalid = true;
            if (n.give.prisoners.Any(id => !ReleasableTo(c).Any(p => p.PersonalState.id == id)) || n.take.prisoners.Any(id => !RansomableIds(c).Contains(id))) n.invalid = true;
            return n.invalid;
        }

        // 상대 입장에서 받는 가치 ÷ 요구 가치. 정확한 값은 보여주지 않고 반응 구간만 공개한다.
        public double Ratio(Civilization c, Negotiation n)
        {
            if (n.kind == TalkKind.Demand) return DemandScore(c, n);
            var paid = TradeValue(n.take, c, false) * PriceFactor(c);
            return paid <= 0 ? 10 : TradeValue(n.give, c, true) / paid;
        }
        // 정치력이 낮으면 반응을 덜 정확하게 읽는다(협상마다 고정된 오차, 확답 없음).
        public TalkReaction Reaction(Civilization c, Negotiation n)
        {
            var noise = ((n.id * 7919 % 21) - 10) / 100d * (1 - Politics(n) / 20d);
            var r = Ratio(c, n) + Persuasion(n) + noise;
            return r >= WarmRatio ? TalkReaction.Warm : r >= HesitantRatio ? TalkReaction.Hesitant : TalkReaction.Cold;
        }
        public static string ReactionName(TalkReaction r) => r == TalkReaction.Warm ? "호의적" : r == TalkReaction.Hesitant ? "망설임" : "냉담";

        // 군사력 추정(잠정): 우리 = 장수 20 + 인구, 상대 = 거점 수 × 20 + 거점 식량 재고 / 10.
        public float PlayerPower => (CommanderRoster.Instance?.Commanders.Count(c => c.IsColonyMember && !c.IsDead) ?? 0) * 20 + (AntPool.Instance?.Total ?? 0);
        public float PowerOf(Civilization c) => world.Sites.Where(s => Faction(s) == c).Sum(s => 20 + (s.Colony != null ? s.Colony.GetStock(ResourceType.Food) / 10f : 0)) + c.resources[0] / 20f;
        private double DemandScore(Civilization c, Negotiation n)
        {
            var stock = Math.Max(1, c.resources[0] + c.resources[1] + c.resources[2] * 10d);
            var credibility = 1 + (c.threatCred + Data.threat * .5) / 100d;
            return PlayerPower / Math.Max(1, PowerOf(c)) * credibility * (n.ultimatum ? 1.25 : 1) / (1 + TradeValue(n.take, c, false) / stock * 3);
        }

        // 제안(거래·요구). 결과 문구를 돌려주고 협상 상태를 갱신한다.
        public string Propose(Negotiation n)
        {
            var c = CivOf(n);
            if (CheckInvalid(n)) return "상황이 바뀌어 이 협상은 무효입니다.";
            if (!CanNegotiate(Negotiator(n))) return "협상 담당 장수를 지정하세요(본거지에서 활동 가능한 장수).";
            if (n.kind != TalkKind.Trade && n.kind != TalkKind.Demand) return "상대 요청에는 수락·거절·역제안으로 답하세요.";
            if (n.deadline > 0) return "최후통첩 거절됨 — 실행 또는 철회를 선택하세요.";
            var ratio = Ratio(c, n) + Persuasion(n);
            if (Data.elapsed < c.talksCooldown && ratio < ImprovedRatio)
                return $"결렬 후 재협상 대기 중({c.talksCooldown - Data.elapsed:0}초). 조건을 크게 개선하면 바로 재개할 수 있습니다.";
            var reason = TradeReason(c, n.give, n.take, false);
            if (reason != "") return reason;
            if (ratio >= 1)
            {
                if (!TryTrade(c, n.give, n.take, false, out reason)) return reason;
                if (n.kind == TalkKind.Demand) c.ChangeAffinity(-5, "요구 수용");
                c.talksCooldown = 0; EndTalk(n, n.kind == TalkKind.Demand ? "요구 수락" : "거래 성립"); return "수락";
            }
            n.patience -= Math.Abs(ratio - n.lastRatio) < .03 ? RepeatPatienceCost : PatienceCost; n.lastRatio = ratio;
            if (n.kind == TalkKind.Demand)
            {
                c.ChangeAffinity(-3, "무리한 요구");
                if (n.ultimatum) { n.deadline = Data.elapsed + DiplomacyRules.Month; n.last = "최후통첩 거절 — 기한 안에 실행 또는 철회"; return n.last; }
            }
            if (n.patience <= 0)
            {
                c.talksCooldown = Data.elapsed + DiplomacyRules.Month; c.ChangeAffinity(-2, "협상 결렬");
                EndTalk(n, "결렬"); return "상대가 인내심을 잃어 협상이 결렬되었습니다.";
            }
            if (n.kind == TalkKind.Trade && ratio >= CounterRatio && Counter(c, n)) return n.last = "역제안: " + Summary(n.give);
            return n.last = "거절 (" + ReactionName(Reaction(c, n)) + ")";
        }
        // 역제안: 부족한 가치를 우리 쪽 자원(식량 → 재료 → 특수)으로 채워 다시 내민다.
        private bool Counter(Civilization c, Negotiation n)
        {
            var deficit = TradeValue(n.take, c, false) * PriceFactor(c) * (1 - Persuasion(n)) - TradeValue(n.give, c, true);
            var rm = ResourceManager.Instance; if (rm == null || deficit <= 0) return false;
            for (var i = 0; i < 3 && deficit > 0; i++)
            {
                var unit = i == 2 ? 10 : 1; var room = rm.GetAmount((ResourceType)i) - n.give.resources[i];
                var add = Mathf.Min(room, Mathf.CeilToInt((float)(deficit / unit)));
                if (add <= 0) continue;
                n.give.resources[i] += add; deficit -= add * unit;
            }
            return deficit <= 0;
        }

        // 상대 요청 응답. counter = 우리가 줄인 양으로 다시 제시.
        public string Respond(Negotiation n, bool accept)
        {
            var c = CivOf(n); if (CheckInvalid(n)) return "상황이 바뀌어 이 협상은 무효입니다.";
            if (!CanNegotiate(Negotiator(n))) return "협상 담당 장수를 지정하세요.";
            if (n.kind == TalkKind.Trade || n.kind == TalkKind.Demand) return "";
            if (!accept) { Refuse(c, n); return "거절했습니다."; }
            if (n.kind == TalkKind.TreatyOffer)
            {
                var treaty = n.take.treaties.FirstOrDefault();
                if (!SignTreaty(c, treaty)) return "협정을 맺을 수 없는 상태입니다.";
                EndTalk(n, "협정 제안 수락"); return "수락";
            }
            var ratio = TradeValue(n.give, c, true) / Math.Max(1, n.asked) + Persuasion(n);
            var reason = TradeReason(c, n.give, n.take, false);
            if (reason != "") return reason;
            // 우리가 양을 줄인 역제안: 원래 요청의 70% 이상이면 받아들인다(정치력이 설득을 보탬).
            if (ratio < .7)
            {
                n.patience -= Math.Abs(ratio - n.lastRatio) < .03 ? RepeatPatienceCost : PatienceCost; n.lastRatio = ratio;
                if (n.patience <= 0) { Refuse(c, n); return "상대가 역제안을 받아들이지 않고 협상을 끝냈습니다."; }
                return n.last = "역제안 거절 (" + (ratio >= .55 ? "망설임" : "냉담") + ")";
            }
            if (!TryTrade(c, n.give, n.take, false, out reason)) return reason;
            if (n.kind == TalkKind.AidRequest) TrustEvent(5, "동맹 원조 제공", c);
            if (n.kind == TalkKind.Tribute) c.ChangeAffinity(3, "공물 수용");
            EndTalk(n, n.kind == TalkKind.AidRequest ? "원조 제공" : "공물 지급"); return "수락";
        }
        // 거절 결과는 요구 종류·성향·관계에 따라 다르다. 일반 협정 제안은 변화 없음.
        private void Refuse(Civilization c, Negotiation n)
        {
            if (n.kind == TalkKind.AidRequest && c.HasTreaty(TreatyKind.Alliance, Data.elapsed)) c.ChangeAffinity(-8, "원조 거절 실망");
            if (n.kind == TalkKind.Tribute)
            {
                c.ChangeAffinity(-10, "공물 거절");
                if (UnityEngine.Random.value < ThreatChance(c)) { DeclareWar(c); AntColony.UI.ToastManager.Show(c.name + "이(가) 위협을 실행했습니다: 선전포고", AntColony.UI.ToastKind.Warning); }
            }
            EndTalk(n, KindName(n.kind) + " 거절");
        }
        // 위협 실행 확률(허세일 수 있음): 상대 군사력이 강할수록·정복자일수록 높다.
        public float ThreatChance(Civilization c) => Mathf.Clamp01(PowerOf(c) / Mathf.Max(1, PlayerPower) * .5f + (c.agenda == LeaderAgenda.Conqueror ? .2f : 0));
        // 거절 전 대략적 위험 안내. 정치력이 높을수록 실제 확률에 가깝게 짐작(확답 없음).
        public string RefusalRisk(Negotiation n)
        {
            var c = CivOf(n);
            if (n.kind == TalkKind.TreatyOffer) return "거절해도 관계 변화 없음";
            if (n.kind == TalkKind.AidRequest) return c.HasTreaty(TreatyKind.Alliance, Data.elapsed) ? "동맹이 실망할 수 있음" : "관계 변화 거의 없음";
            if (n.kind != TalkKind.Tribute) return "";
            var guess = ThreatChance(c) + ((n.id * 31 % 21) - 10) / 50f * (1 - Politics(n) / 20f);
            return "관계 악화 · 공격 위험 " + (guess >= .6f ? "높음" : guess >= .3f ? "중간" : "낮음") + " (짐작)";
        }

        // 최후통첩 거절 후 선택. 실행 = 선전포고(위협 신뢰 회복), 철회 = 위협 신뢰 하락. 기한 초과는 철회.
        public void ExecuteUltimatum(Negotiation n)
        {
            var c = CivOf(n); if (c == null || n.deadline <= 0) return;
            DeclareWar(c);
            c.threatCred = Mathf.Clamp(c.threatCred + 15, -100, 100); Data.threat = Mathf.Clamp(Data.threat + 10, -100, 100);
            Reputation("최후통첩을 실행함 (" + c.name + ")");
            EndTalk(n, "최후통첩 실행");
        }
        public void WithdrawUltimatum(Negotiation n)
        {
            var c = CivOf(n); if (c == null) return;
            c.threatCred = Mathf.Clamp(c.threatCred - 20, -100, 100);
            foreach (var other in Data.civilizations.Where(x => x != c)) other.threatCred = Mathf.Clamp(other.threatCred - 5, -100, 100);
            Data.threat = Mathf.Clamp(Data.threat - 10, -100, 100);
            Reputation("최후통첩을 철회함 (" + c.name + ")");
            EndTalk(n, "최후통첩 철회");
        }

        // 외교 신뢰: 국가적 좋은 행동은 오르고 배신·협정 위반은 내린다. 수혜·우호 세력에 더 크게 반영.
        public void TrustEvent(int delta, string text, Civilization beneficiary)
        {
            Data.trust = Mathf.Clamp(Data.trust + delta, -100, 100);
            Reputation(text + (beneficiary != null ? " (" + beneficiary.name + ")" : ""));
            foreach (var c in Data.civilizations.Where(x => !x.extinct))
            {
                var change = c == beneficiary ? delta * 2 : c.affinity >= 30 ? delta / 2 : 0;
                if (change != 0) c.ChangeAffinity(change, text);
            }
        }
        private void Reputation(string text)
        {
            Data.reputation.Insert(0, $"{Data.elapsed / DiplomacyRules.Month:0}개월 · {text}");
            if (Data.reputation.Count > 12) Data.reputation.RemoveAt(12);
        }
        // 평판은 내부 점수 대신 설명으로 보여준다.
        public string ReputationSummary()
        {
            var parts = new List<string>();
            parts.Add(Data.trust >= 20 ? "약속을 잘 지킴" : Data.trust <= -20 ? "신뢰하기 어려움" : "외교 신뢰 보통");
            parts.Add(Data.threat >= 20 ? "말한 것은 실행함" : Data.threat <= -20 ? "허세가 잦음" : "위협 신뢰 보통");
            return string.Join(" · ", parts);
        }
        public static string KindName(TalkKind k) => k switch
        { TalkKind.Trade => "거래", TalkKind.Demand => "요구", TalkKind.TreatyOffer => "협정 제안", TalkKind.AidRequest => "원조 요청", _ => "공물 요구" };
        public static string Summary(TradeOffer o)
        {
            string[] names = { "식량", "재료", "특수" };
            var parts = Enumerable.Range(0, 3).Where(i => o.resources[i] > 0).Select(i => names[i] + " " + o.resources[i]).ToList();
            if (o.equipment.Count > 0) parts.Add("장비 " + o.equipment.Count);
            if (o.prisoners.Count > 0) parts.Add("장수 " + o.prisoners.Count);
            if (o.sites.Count > 0) parts.Add("거점 " + o.sites.Count);
            if (o.treaties.Count > 0) parts.Add(string.Join("·", o.treaties.Select(t => new[] { "교역 협정", "불가침", "동맹" }[(int)t])));
            if (o.blueprint) parts.Add("설계도");
            return parts.Count == 0 ? "없음" : string.Join(", ", parts);
        }

        // 매달: AI 요청 생성(협정 제안·동맹 원조·공물), 위협 신뢰 서서히 회복, 지킨 협정 신뢰.
        private void MonthlyTalks(Civilization c)
        {
            for (var i = 0; i < 3; i++)
                if (!c.war && c.treaties[i] > Data.elapsed - DiplomacyRules.Month && c.treaties[i] <= Data.elapsed) TrustEvent(2, "협정 기간 준수", c);
            c.threatCred -= Math.Sign(c.threatCred) * Math.Min(2, Math.Abs(c.threatCred));
            if (!c.contacted || c.war || c.rebel || Data.talks.Any(n => n.civ == c.id && n.kind != TalkKind.Trade && n.kind != TalkKind.Demand)) return;
            var r = UnityEngine.Random.value;
            if (c.HasTreaty(TreatyKind.Alliance, Data.elapsed) && r < .15f) Request(c, TalkKind.AidRequest, 50 + UnityEngine.Random.Range(0, 3) * 50);
            else if ((c.affinity <= -20 || c.agenda == LeaderAgenda.Conqueror) && r < .15f) Request(c, TalkKind.Tribute, 100 + UnityEngine.Random.Range(0, 3) * 50);
        }
        private void Request(Civilization c, TalkKind kind, int food)
        {
            var n = StartTalk(c, kind); n.give.resources[0] = food; n.asked = food;
            n.expires = Data.elapsed + DiplomacyRules.Month; n.patience = 100;
            AntColony.UI.ToastManager.Show($"{c.name} {KindName(kind)}: 식량 {food} — 오른쪽 알림에서 협상", AntColony.UI.ToastKind.Warning);
        }
        internal void OfferTreaty(Civilization c, TreatyKind kind)
        {
            var n = StartTalk(c, TalkKind.TreatyOffer); n.take = new TradeOffer(); n.take.treaties.Add(kind); n.expires = Data.elapsed + DiplomacyRules.Month;
            AntColony.UI.ToastManager.Show(c.name + " 협정 제안 — 오른쪽 알림에서 확인 (1개월)");
        }
        // 기한: 상대 요청 만료는 거절과 같은 결과(관계 변화 없이 사라지지 않음), 최후통첩 기한 초과는 철회.
        private void TickTalks()
        {
            foreach (var n in Data.talks.ToArray())
            {
                if (n.deadline > 0 && Data.elapsed >= n.deadline) WithdrawUltimatum(n);
                else if (n.expires > 0 && Data.elapsed >= n.expires) { var c = CivOf(n); if (c != null) Refuse(c, n); else Data.talks.Remove(n); }
            }
        }
        public float Remaining(Negotiation n) => n.deadline > 0 ? n.deadline - Data.elapsed : n.expires > 0 ? n.expires - Data.elapsed : -1;
    }
}
