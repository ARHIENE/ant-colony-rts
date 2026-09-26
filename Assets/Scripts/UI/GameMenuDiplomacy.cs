using System;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        private TradeOffer tradeGive = new TradeOffer(), tradeTake = new TradeOffer();
        private int tradeTab;
        public void Diplomacy() => Diplomacy(null);
        public void Diplomacy(Civilization selected)
        {
            var d = DiplomacyManager.Instance;
            if (d?.Available != true) { ToastManager.Show("월드맵 해금 후 외교를 사용할 수 있습니다."); return; }
            Screen("외교 [J]");
            var row = Columns(out var left, out var right);
            foreach (var c in d.Data.civilizations.Where(c => c.contacted && !c.extinct))
            {
                var button = MenuTheme.Button(left, c.name + "  " + c.affinity, () => Diplomacy(c));
                button.GetComponentInChildren<Text>().color = c.color;
                var bar = MenuTheme.Rect("호감도", left); bar.gameObject.AddComponent<LayoutElement>().preferredHeight = 10;
                bar.gameObject.AddComponent<Image>().color = Color.gray;
                var fill = MenuTheme.Rect("값", bar); MenuTheme.Stretch(fill); fill.anchorMax = new Vector2((c.affinity + 100) / 200f, 1); fill.gameObject.AddComponent<Image>().color = c.color;
            }
            if (selected != null)
            {
                var c = selected;
                MenuTheme.Text(right, c.name + " / " + c.leader, 23, 50);
                MenuTheme.Text(right, (c.war ? "전쟁" : "평화") + " · 호감도 " + c.affinity);
                MenuTheme.Text(right, "공개: " + DiplomacyRules.AgendaNames[(int)c.agenda]);
                MenuTheme.Text(right, "숨김: " + (c.hiddenRevealed ? DiplomacyRules.AgendaNames[(int)c.hiddenAgenda] : "잠김 — 교역 협정 필요"), 18, 60);
                foreach (var reason in c.reasons) MenuTheme.Text(right, reason, 16, 28);
                foreach (TreatyKind kind in Enum.GetValues(typeof(TreatyKind)))
                    if (c.HasTreaty(kind, d.Data.elapsed)) MenuTheme.Text(right, kind + $" · {(c.treaties[(int)kind] - d.Data.elapsed) / DiplomacyRules.Month:0.0}개월");
                var war = MenuTheme.Button(right, "선전포고", () => { if (!d.DeclareWar(c)) ToastManager.Show("불가침 협정 중에는 선전포고할 수 없습니다."); Diplomacy(c); });
                war.interactable = !c.war; war.GetComponentInChildren<Text>().color = MenuTheme.Danger;
                MenuTheme.Button(right, "평화 협상 · 배상 Food " + d.Reparations(c), () => { if (!d.MakePeace(c)) ToastManager.Show("선전포고 후 10분 및 배상 자원·창고 공간을 확인하세요."); Diplomacy(c); }).interactable = d.CanNegotiatePeace(c);
                MenuTheme.Button(right, "거래 제안", () => OpenTrade(c)).interactable = !c.war;
                MenuTheme.Button(right, "협정 제안", () => { tradeTab = 4; OpenTrade(c); }).interactable = !c.war;
                if (c.offerExpires > d.Data.elapsed)
                    MenuTheme.Button(right, c.offeredTreaty + " 제안 수락 · 남은 " + (c.offerExpires - d.Data.elapsed).ToString("0") + "초", () => { if (!d.AcceptOffer(c)) ToastManager.Show("제안이 만료되었습니다."); Diplomacy(c); });
            }
            else MenuTheme.Text(right, "접촉한 문명을 선택하세요. 첫 원정 도착 시 접촉합니다.", 20, 100);
            if (d.Data.caravanUntil > d.Data.elapsed) MenuTheme.Button(content, "캐러밴 거래 · " + (d.Data.caravanUntil - d.Data.elapsed).ToString("0") + "초", () => OpenTrade(d.Market(-1)));
            foreach (var site in WorldMapManager.Instance.Sites.Where(s => s.Kind == ExpeditionSiteKind.TradePost && s.Visitor?.State == ExpeditionState.Deployed))
                MenuTheme.Button(content, site.Title + " 거래", () => TradeAt(site));
            MenuTheme.Button(content, "게임으로", Resume);
        }
        private RectTransform Columns(out RectTransform left, out RectTransform right)
        {
            var row = MenuTheme.Rect("두 목록", content); row.gameObject.AddComponent<LayoutElement>().preferredHeight = 440;
            var a = MenuTheme.Rect("우리", row); a.anchorMin = Vector2.zero; a.anchorMax = new Vector2(.48f, 1); a.offsetMin = a.offsetMax = Vector2.zero;
            var b = MenuTheme.Rect("상대", row); b.anchorMin = new Vector2(.5f, 0); b.anchorMax = Vector2.one; b.offsetMin = b.offsetMax = Vector2.zero;
            left = MenuTheme.Scroll(a); right = MenuTheme.Scroll(b); return row;
        }
        public void TradeAt(ExpeditionSite site)
        { OpenTrade(DiplomacyManager.Instance?.Market(WorldMapManager.Instance.Sites.ToList().IndexOf(site))); }
        private void OpenTrade(Civilization c)
        { if (c == null) return; tradeGive = new TradeOffer(); tradeTake = new TradeOffer(); TradeScreen(c); }
        private void TradeScreen(Civilization c)
        {
            var d = DiplomacyManager.Instance;
            if (!d.CanTrade(c)) { ToastManager.Show("상대가 떠났거나 거래할 수 없는 상태입니다."); Diplomacy(); return; }
            Screen("거래 · " + c.name);
            var tabs = MenuTheme.Rect("거래 탭", content); tabs.gameObject.AddComponent<HorizontalLayoutGroup>(); tabs.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;
            var names = new[] { "자원", "장비", "거점", "포로 장수", "협정" };
            for (var i = 0; i < names.Length; i++) { var tab = i; MenuTheme.Button(tabs, names[i], () => { tradeTab = tab; TradeScreen(c); }); }
            Columns(out var left, out var right);
            var preview = MenuTheme.Text(content, "", 18, 60);
            void RefreshPreview() { var reason = d.TradeReason(c, tradeGive, tradeTake); preview.text = (reason == "" ? "AI 수락 예상" : reason) + " · 성사 시 호감도 +" + (c.agenda == LeaderAgenda.Trader ? 3 : 1); }
            TradeSide(left, c, true, RefreshPreview); TradeSide(right, c, false, RefreshPreview); RefreshPreview();
            MenuTheme.Button(content, "초기화", () => OpenTrade(c));
            MenuTheme.Button(content, "거래 제안", () => { if (d.TryTrade(c, tradeGive, tradeTake, out var error)) OpenTrade(c); else { ToastManager.Show(error); RefreshPreview(); } });
            MenuTheme.Button(content, "외교로", Diplomacy); MenuTheme.Button(content, "게임으로", Resume);
        }
        private void TradeSide(RectTransform parent, Civilization c, bool player, Action preview)
        {
            var d = DiplomacyManager.Instance; var offer = player ? tradeGive : tradeTake;
            MenuTheme.Text(parent, player ? "우리 보유 / 제시" : c.name + " 보유 / 요청", 21, 45);
            if (tradeTab == 0)
            {
                for (var i = 0; i < 3; i++)
                {
                    var resource = i; var max = player ? ResourceManager.Instance.GetAmount((ResourceType)i) : c.resources[i];
                    MenuTheme.Button(parent, (ResourceType)i + " 보유 " + max + " · " + (offer.resources[i] > 0 ? "내리기" : "올리기"), () => { offer.resources[resource] = offer.resources[resource] > 0 ? 0 : Mathf.Min(1, max); TradeScreen(c); });
                    if (offer.resources[i] == 0) continue;
                    var label = MenuTheme.Text(parent, "수량 " + offer.resources[i]);
                    var rect = MenuTheme.Rect("수량", parent); rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 30; rect.gameObject.AddComponent<Image>().color = Color.gray;
                    var handle = MenuTheme.Rect("Handle", rect); handle.sizeDelta = new Vector2(15, 30); handle.gameObject.AddComponent<Image>().color = MenuTheme.Accent;
                    var slider = rect.gameObject.AddComponent<Slider>(); slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>(); slider.wholeNumbers = true; slider.minValue = 1; slider.maxValue = Mathf.Max(1, max); slider.value = offer.resources[i];
                    slider.onValueChanged.AddListener(v => { offer.resources[resource] = (int)v; label.text = "수량 " + (int)v; preview(); });
                }
                if (!player && c.id == "market:" + d.Data.blueprintSite && CampaignResearch.Instance?.HasBlueprint == false)
                    MenuTheme.Button(parent, (offer.blueprint ? "✓ " : "") + "비행선 설계도 · Special 150", () => { offer.blueprint = !offer.blueprint; TradeScreen(c); });
            }
            if (tradeTab == 1)
                foreach (var item in player ? EquipmentInventory.Instance.Items : c.equipment)
                    MenuTheme.Button(parent, (offer.equipment.Contains(item.id) ? "✓ " : "") + item.Label, () => { if (!offer.equipment.Remove(item.id)) offer.equipment.Add(item.id); TradeScreen(c); });
            if (tradeTab == 2)
                for (var i = 0; i < WorldMapManager.Instance.Sites.Count; i++)
                {
                    var index = i; var site = WorldMapManager.Instance.Sites[i];
                    if (player ? site.Disposition != ConquestDisposition.Annexed : d.Faction(site) != c || site.Disposition == ConquestDisposition.Annexed) continue;
                    MenuTheme.Button(parent, (offer.sites.Contains(i) ? "✓ " : "") + site.Title, () => { if (!offer.sites.Remove(index)) offer.sites.Add(index); TradeScreen(c); });
                }
            if (tradeTab == 3)
                foreach (var p in player ? DiplomacyManager.PlayerPrisoners : c.prisoners.Concat(c.rebels))
                    MenuTheme.Button(parent, (offer.prisoners.Contains(p.PersonalState.id) ? "✓ " : "") + p.Name, () => { if (!offer.prisoners.Remove(p.PersonalState.id)) offer.prisoners.Add(p.PersonalState.id); TradeScreen(c); });
            if (tradeTab == 4)
                foreach (TreatyKind kind in Enum.GetValues(typeof(TreatyKind)))
                    MenuTheme.Button(parent, (offer.treaties.Contains(kind) ? "✓ " : "") + kind + " · 1년", () => { if (!offer.treaties.Remove(kind)) offer.treaties.Add(kind); TradeScreen(c); });
        }
    }
}
