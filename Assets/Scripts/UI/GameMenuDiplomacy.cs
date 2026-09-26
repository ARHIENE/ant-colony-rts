using System;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.UI;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // 디자인 「외교」(문명 목록 372 + 지도자·어젠다·호감도 + 협정·행동)와 「거래」(우리 | 가치 요약 | 상대).
    public sealed partial class GameMenuController
    {
        private static readonly string[] TreatyNames = { "교역 협정", "불가침 협정", "동맹" };
        private TradeOffer tradeGive = new TradeOffer(), tradeTake = new TradeOffer();
        private int tradeTab;
        public void Diplomacy() => Diplomacy(null);
        public void Diplomacy(Civilization selected)
        {
            var d = DiplomacyManager.Instance;
            if (d?.Available != true) { ToastManager.Show("월드맵 해금 후 외교를 사용할 수 있습니다."); return; }
            var f = Frame("외교 [J]");
            var contacted = d.Data.civilizations.Where(c => c.contacted && !c.extinct).ToArray();
            var tab = L.Plate(f, "DiplomacyTab", 48, 47, 200, 34);
            L.Label(tab, $"<b>외교</b>   <color=#968976>접촉 {contacted.Length}</color>", 14, 16, 0, 180, 34);
            var p = L.Plate(f, "DiplomacyPanel", 48, 80, 1344, 676);

            // 왼쪽 372: 문명 카드 목록 + 캐러밴·교역소.
            L.Label(p, $"<b>접촉한 문명</b> <color=#968976>{contacted.Length}</color>", 13, 12, 8, 340, 24);
            var list = L.List(p, 10, 36, 356, 556, 8);
            foreach (var c in contacted)
            {
                var civ = c;
                var card = L.Button(list, c.name, "", 0, 0, 0, 0, () => Diplomacy(civ));
                card.gameObject.AddComponent<LayoutElement>().preferredHeight = 78;
                if (c == selected) card.GetComponent<Outline>().effectColor = MenuTheme.Accent;
                var badge = L.Box(card.transform, "Badge", 8, 10, 30, 30, c.color);
                L.Label(badge, c.name.Substring(0, 1), 14, 0, 0, 30, 30, MenuTheme.AccentInk, TextAnchor.MiddleCenter, true);
                L.Label(card.transform, $"<b>{c.name}</b>", 14, 46, 4, 200, 22);
                L.Label(card.transform, (c.war ? "<color=#e8574a>전쟁</color>" : "평화") + " · " + c.leader, 12, 46, 24, 200, 18, MenuTheme.Muted);
                L.Label(card.transform, c.affinity.ToString("+0;-0;0"), 16, 250, 6, 88, 24, c.affinity >= 0 ? MenuTheme.Hp : MenuTheme.Danger, TextAnchor.MiddleRight, true);
                L.Meter(card.transform, 46, 46, 292, 5, (c.affinity + 100) / 200f, c.color);
                L.Label(card.transform, "공개 어젠다 " + DiplomacyRules.AgendaNames[(int)c.agenda] + " · " + TreatySummary(c, d), 12, 8, 54, 336, 22, MenuTheme.Dim);
            }
            var unknown = d.Data.civilizations.Count(c => !c.contacted && !c.extinct);
            L.Label(p, $"미접촉 문명 {unknown} · 원정대가 거점에 도착하면 접촉", 12, 12, 596, 350, 20, MenuTheme.Dim);
            var x = 12f;
            if (d.Data.caravanUntil > d.Data.elapsed)
            {
                L.Button(p, "Caravan", "캐러밴 · " + (d.Data.caravanUntil - d.Data.elapsed).ToString("0") + "초", x, 624, 170, 40, () => OpenTrade(d.Market(-1)));
                x += 178;
            }
            foreach (var site in WorldMapManager.Instance.Sites.Where(s => s.Kind == ExpeditionSiteKind.TradePost && s.Visitor?.State == ExpeditionState.Deployed))
            {
                var post = site;
                if (x > 200) break;
                L.Button(p, site.Title + " 거래", site.Title + " 거래", x, 624, 170, 40, () => TradeAt(post));
                x += 178;
            }
            L.Box(p, "Divider", 372, 0, 1, 676, MenuTheme.Line);
            L.Button(f, "Back", "닫기   Esc", 1392 - 110, 764, 110, 34, Resume);

            if (selected == null)
            {
                L.Label(p, "접촉한 문명을 선택하세요. 첫 원정 도착 시 접촉합니다.", 16, 400, 280, 900, 40, MenuTheme.Dim, TextAnchor.MiddleCenter);
                return;
            }
            var s = selected;
            // 가운데 열: 지도자·관계·어젠다·호감도 사유.
            var head = L.Well(p, "Leader", 386, 12, 470, 96);
            var portrait = L.Box(head, "Portrait", 10, 10, 76, 76, s.color);
            L.Label(portrait, s.name.Substring(0, 1), 30, 0, 0, 76, 76, MenuTheme.AccentInk, TextAnchor.MiddleCenter, true);
            L.Label(head, s.leader, 20, 100, 10, 360, 30, bold: true);
            L.Label(head, s.name + " 지도자", 12, 100, 40, 360, 20, MenuTheme.Muted);
            L.Label(head, $"관계 <b>{(s.war ? "<color=#e8574a>전쟁</color>" : "평화")}</b>     호감도 <b>{s.affinity:+0;-0;0}</b>     전쟁 점수 {s.playerScore} : {s.enemyScore}", 13, 100, 62, 360, 24);
            L.Label(p, "<b>어젠다</b>  <color=#968976>지도자마다 공개 1 · 숨김 1</color>", 13, 386, 120, 470, 22);
            L.Label(p, "공개  <b>" + DiplomacyRules.AgendaNames[(int)s.agenda] + "</b>", 13, 386, 144, 470, 22);
            L.Label(p, "숨김  " + (s.hiddenRevealed ? "<b>" + DiplomacyRules.AgendaNames[(int)s.hiddenAgenda] + "</b>" : "<color=#968976>교역 협정을 맺으면 공개</color>"), 13, 386, 166, 470, 22);
            L.Label(p, "<b>호감도</b>  <color=#968976>-100 ~ +100 · 최근 사유</color>", 13, 386, 200, 470, 22);
            L.Meter(p, 386, 226, 470, 8, (s.affinity + 100) / 200f, s.affinity >= 0 ? MenuTheme.Hp : MenuTheme.Danger);
            var reasons = L.List(p, 386, 242, 470, 420, 2);
            foreach (var reason in Enumerable.Reverse(s.reasons)) MenuTheme.Text(reasons, reason, 12, 20).color = MenuTheme.Muted;

            // 오른쪽 열: 협정 + 행동.
            L.Label(p, "<b>협정</b>  <color=#968976>기간 1년 · 만료 전 갱신 가능</color>", 13, 872, 12, 460, 22);
            foreach (TreatyKind kind in Enum.GetValues(typeof(TreatyKind)))
            {
                var i = (int)kind;
                var active = s.HasTreaty(kind, d.Data.elapsed);
                var box = L.Box(p, "Treaty " + kind, 872, 40 + i * 50, 460, 44, MenuTheme.Plate2, true);
                L.Label(box, $"<b>{TreatyNames[i]}</b>  <color=#968976>가치 {DiplomacyRules.TreatyValues[i]}</color>", 13, 10, 0, 300, 44);
                L.Label(box, active ? $"남은 {(s.treaties[i] - d.Data.elapsed) / DiplomacyRules.Month:0.0}개월" : "없음", 13, 300, 0, 150, 44,
                    active ? MenuTheme.Hp : MenuTheme.Dim, TextAnchor.MiddleRight);
            }
            var y = 200f;
            Button Act(string name, string label, Action action, bool enabled)
            { var b = L.Button(p, name, label, 872, y, 460, 40, action); b.interactable = enabled; y += 46; return b; }
            Act("선전포고", "선전포고", () => { if (!d.DeclareWar(s)) ToastManager.Show("불가침 협정 중에는 선전포고할 수 없습니다."); Diplomacy(s); }, !s.war)
                .GetComponentInChildren<Text>().color = MenuTheme.Danger;
            Act("평화 협상", "평화 협상 · 배상 식량 " + d.Reparations(s), () => { if (!d.MakePeace(s)) ToastManager.Show("선전포고 후 10분 및 배상 자원·창고 공간을 확인하세요."); Diplomacy(s); }, d.CanNegotiatePeace(s));
            Act("거래 제안", "거래 제안", () => OpenTrade(s), !s.war);
            Act("협정 제안", "협정 제안", () => { tradeTab = 4; OpenTrade(s); }, !s.war);
            if (s.offerExpires > d.Data.elapsed)
                Act("제안 수락", TreatyNames[(int)s.offeredTreaty] + " 제안 수락 · 남은 " + (s.offerExpires - d.Data.elapsed).ToString("0") + "초",
                    () => { if (!d.AcceptOffer(s)) ToastManager.Show("제안이 만료되었습니다."); Diplomacy(s); }, true);
        }
        private static string TreatySummary(Civilization c, DiplomacyManager d)
        {
            var active = Enum.GetValues(typeof(TreatyKind)).Cast<TreatyKind>().Where(k => c.HasTreaty(k, d.Data.elapsed)).Select(k => TreatyNames[(int)k]).ToArray();
            return c.war ? (d.CanNegotiatePeace(c) ? "평화 협상 가능" : "전쟁 중") : active.Length == 0 ? "협정 없음" : string.Join(", ", active);
        }
        public void TradeAt(ExpeditionSite site)
        { OpenTrade(DiplomacyManager.Instance?.Market(WorldMapManager.Instance.Sites.ToList().IndexOf(site))); }
        private void OpenTrade(Civilization c)
        { if (c == null) return; tradeGive = new TradeOffer(); tradeTake = new TradeOffer(); TradeScreen(c); }
        private void TradeScreen(Civilization c)
        {
            var d = DiplomacyManager.Instance;
            if (!d.CanTrade(c)) { ToastManager.Show("상대가 떠났거나 거래할 수 없는 상태입니다."); Diplomacy(); return; }
            var f = Frame("거래 · " + c.name);
            var p = L.Plate(f, "TradePanel", 48, 48, 1344, 740);
            var badge = L.Box(p, "Badge", 14, 12, 36, 36, c.color);
            L.Label(badge, c.name.Substring(0, 1), 16, 0, 0, 36, 36, MenuTheme.AccentInk, TextAnchor.MiddleCenter, true);
            L.Label(p, $"<b>{c.name}</b>  <color=#968976>{c.leader}</color>", 16, 60, 10, 420, 22);
            L.Label(p, $"관계 {(c.war ? "전쟁" : "평화")} · 호감도 <b>{c.affinity:+0;-0;0}</b> · 공개 어젠다 {DiplomacyRules.AgendaNames[(int)c.agenda]}", 12, 60, 32, 520, 20, MenuTheme.Muted);
            L.Label(p, "상대는 받는 가치가 주는 가치 × (1.2 - 호감도 / 250) 이상이면 수락", 12, 700, 20, 630, 20, MenuTheme.Dim, TextAnchor.MiddleRight);
            L.Line(p, 0, 60, 1344);
            var names = new[] { "자원", "장비", "거점", "포로 장수", "협정" };
            for (var i = 0; i < names.Length; i++)
            {
                var tab = i;
                var button = L.Button(p, names[i], names[i], 14 + i * 104, 70, 100, 30, () => { tradeTab = tab; TradeScreen(c); }, null, false, 13);
                if (i == tradeTab) button.GetComponent<Outline>().effectColor = MenuTheme.Accent;
            }
            var left = L.List(p, 14, 110, 520, 566, 6);
            var right = L.List(p, 810, 110, 520, 566, 6);
            L.Box(p, "DividerL", 544, 100, 1, 580, MenuTheme.Line);
            L.Box(p, "DividerR", 800, 100, 1, 580, MenuTheme.Line);
            var give = L.Label(p, "", 13, 556, 120, 232, 60, align: TextAnchor.UpperCenter);
            var take = L.Label(p, "", 13, 556, 190, 232, 60, align: TextAnchor.UpperCenter);
            var preview = L.Label(p, "", 13, 556, 262, 232, 120, MenuTheme.Muted, TextAnchor.UpperCenter);
            void RefreshPreview()
            {
                var reason = d.TradeReason(c, tradeGive, tradeTake);
                give.text = $"우리가 주는 가치\n<size=22><b>{d.TradeValue(tradeGive, c, true):0}</b></size>";
                take.text = $"상대가 주는 가치\n<size=22><b>{d.TradeValue(tradeTake, c, false):0}</b></size>";
                preview.text = (reason == "" ? "<color=#6cc46a><b>수락 가능성 높음</b></color>" : reason) + "\n성사 시 호감도 +" + (c.agenda == LeaderAgenda.Trader ? 3 : 1);
            }
            TradeSide(left, c, true, RefreshPreview); TradeSide(right, c, false, RefreshPreview); RefreshPreview();
            L.Line(p, 0, 686, 1344);
            L.Button(p, "초기화", "초기화", 14, 696, 110, 34, () => OpenTrade(c));
            L.Button(p, "외교로", "외교로", 130, 696, 110, 34, Diplomacy);
            L.Button(p, "게임으로", "게임으로   Esc", 1344 - 14 - 200 - 8 - 130, 696, 130, 34, Resume);
            L.Button(p, "거래 제안", "거래 제안", 1344 - 14 - 200, 696, 200, 34,
                () => { if (d.TryTrade(c, tradeGive, tradeTake, out var error)) OpenTrade(c); else { ToastManager.Show(error); RefreshPreview(); } }, null, true);
        }
        private void TradeSide(RectTransform parent, Civilization c, bool player, Action preview)
        {
            var d = DiplomacyManager.Instance; var offer = player ? tradeGive : tradeTake;
            MenuTheme.Text(parent, player ? "<b>우리 소굴</b>  <color=#968976>보유 / 제시</color>" : $"<b>{c.name}</b>  <color=#968976>보유 / 요청</color>", 15, 32);
            string[] resourceNames = { "식량", "흙", "특수 자원" };
            // 목록 버튼은 디자인 행 크기(높이 36, 14pt)로 줄인다.
            Button Item(string label, Action action)
            {
                var b = MenuTheme.Button(parent, label, action);
                b.GetComponentInChildren<Text>().fontSize = 14; b.GetComponent<LayoutElement>().preferredHeight = 36;
                return b;
            }
            if (tradeTab == 0)
            {
                for (var i = 0; i < 3; i++)
                {
                    var resource = i; var max = player ? ResourceManager.Instance.GetAmount((ResourceType)i) : c.resources[i];
                    Item(resourceNames[i] + " 보유 " + max + " · " + (offer.resources[i] > 0 ? "빼기" : "넣기"), () => { offer.resources[resource] = offer.resources[resource] > 0 ? 0 : Mathf.Min(1, max); TradeScreen(c); });
                    if (offer.resources[i] == 0) continue;
                    var label = MenuTheme.Text(parent, "수량 " + offer.resources[i], 14, 26);
                    var rect = MenuTheme.Rect("수량", parent); rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 20; rect.gameObject.AddComponent<Image>().color = MenuTheme.Well;
                    var handle = MenuTheme.Rect("Handle", rect); handle.sizeDelta = new Vector2(15, 20); handle.gameObject.AddComponent<Image>().color = MenuTheme.Accent;
                    var slider = rect.gameObject.AddComponent<Slider>(); slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>(); slider.wholeNumbers = true; slider.minValue = 1; slider.maxValue = Mathf.Max(1, max); slider.value = offer.resources[i];
                    slider.onValueChanged.AddListener(v => { offer.resources[resource] = (int)v; label.text = "수량 " + (int)v; preview(); });
                }
                if (!player && c.id == "market:" + d.Data.blueprintSite && CampaignResearch.Instance?.HasBlueprint == false)
                    Item((offer.blueprint ? "✓ " : "") + "비행선 설계도 · 특수 150", () => { offer.blueprint = !offer.blueprint; TradeScreen(c); });
            }
            if (tradeTab == 1)
                foreach (var item in player ? EquipmentInventory.Instance.Items : c.equipment)
                    Item((offer.equipment.Contains(item.id) ? "✓ " : "") + item.Label, () => { if (!offer.equipment.Remove(item.id)) offer.equipment.Add(item.id); TradeScreen(c); });
            if (tradeTab == 2)
                for (var i = 0; i < WorldMapManager.Instance.Sites.Count; i++)
                {
                    var index = i; var site = WorldMapManager.Instance.Sites[i];
                    if (player ? site.Disposition != ConquestDisposition.Annexed : d.Faction(site) != c || site.Disposition == ConquestDisposition.Annexed) continue;
                    Item((offer.sites.Contains(i) ? "✓ " : "") + site.Title, () => { if (!offer.sites.Remove(index)) offer.sites.Add(index); TradeScreen(c); });
                }
            if (tradeTab == 3)
                foreach (var p in player ? DiplomacyManager.PlayerPrisoners : c.prisoners.Concat(c.rebels))
                    Item((offer.prisoners.Contains(p.PersonalState.id) ? "✓ " : "") + p.Name, () => { if (!offer.prisoners.Remove(p.PersonalState.id)) offer.prisoners.Add(p.PersonalState.id); TradeScreen(c); });
            if (tradeTab == 4)
                foreach (TreatyKind kind in Enum.GetValues(typeof(TreatyKind)))
                    Item((offer.treaties.Contains(kind) ? "✓ " : "") + TreatyNames[(int)kind] + " · 1년", () => { if (!offer.treaties.Remove(kind)) offer.treaties.Add(kind); TradeScreen(c); });
        }
    }
}
