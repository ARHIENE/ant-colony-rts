using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    [Serializable] public sealed class TradeOffer
    {
        public int[] resources = new int[3];
        public List<string> equipment = new List<string>(), prisoners = new List<string>();
        public List<int> sites = new List<int>();
        public List<TreatyKind> treaties = new List<TreatyKind>();
        public bool blueprint;
        public bool Empty => resources.All(r => r == 0) && equipment.Count + prisoners.Count + sites.Count + treaties.Count == 0 && !blueprint;
    }
    public sealed partial class DiplomacyManager
    {
        public Civilization Market(int index)
        {
            if (!Available || index < -1 || index >= world.Sites.Count || index == -1 && Data.caravanUntil <= Data.elapsed
                || index >= 0 && (world.Sites[index].Kind != ExpeditionSiteKind.TradePost || world.Sites[index].Visitor?.State != ExpeditionState.Deployed)) return null;
            var id = "market:" + index;
            var market = Data.markets.Find(m => m.id == id);
            if (market == null)
            {
                market = new Civilization { id = id, name = index < 0 ? "교역 캐러밴" : world.Sites[index].Title, contacted = true };
                for (var i = 0; i < 10; i++) market.equipment.Add(EquipmentRecipes.Create((EquipmentRecipe)i, i % 4));
                Data.markets.Add(market);
            }
            return market;
        }
        public bool CanTrade(Civilization c) => Available && c != null && (Data.civilizations.Contains(c) && c.contacted && !c.extinct && !c.war
            || Data.markets.Contains(c) && int.TryParse(c.id.Substring(7), out var index) && Market(index) == c);
        public static IEnumerable<PrisonerCamp> Camps => FindObjectsByType<PrisonerCamp>(FindObjectsSortMode.None).Where(c => c.isActiveAndEnabled);
        public static IEnumerable<Prisoner> PlayerPrisoners => Camps.SelectMany(c => c.Prisoners);
        public double TradeValue(TradeOffer offer, Civilization c, bool player)
        {
            var gear = player ? EquipmentInventory.Instance.Items : c.equipment;
            double value = offer.resources[0] + (double)offer.resources[1] + offer.resources[2] * 10d;
            value += gear.Where(e => offer.equipment.Contains(e.id)).Sum(e => DiplomacyRules.EquipmentValues[e.quality]);
            value += offer.prisoners.Sum(id => RansomValue(id, c));
            value += offer.sites.Sum(i => world.Sites[i].Difficulty * 300);
            value += offer.treaties.Sum(t => DiplomacyRules.TreatyValues[(int)t]);
            if (offer.blueprint) value += DiplomacyRules.BlueprintPrice * 10;
            return value;
        }
        // 상대가 요구하는 최소 가치 배율(호감도·교역 협정·외교 신뢰 반영).
        public double PriceFactor(Civilization c) => Data.markets.Contains(c) ? 1 : DiplomacyRules.AcceptanceMultiplier(c.affinity) * DiplomacyRules.PriceMultiplier(c.affinity)
            * (c.HasTreaty(TreatyKind.Trade, Data.elapsed) ? .9f : 1) * (1 - Mathf.Clamp(Data.trust, -100, 100) / 1000d);
        // checkValue=false: 협상 계층이 수락 판단을 끝낸 뒤 소유·재고만 다시 확인한다(보류 중 자원은 예약하지 않으므로 성립 직전 재확인).
        public string TradeReason(Civilization c, TradeOffer give, TradeOffer take, bool checkValue = true)
        {
            if (!CanTrade(c) || EquipmentInventory.Instance == null || ResourceManager.Instance == null) return "거래할 수 없는 상대입니다.";
            bool Shape(TradeOffer o) => o != null && o.resources != null && o.resources.Length == 3 && o.resources.All(r => r >= 0 && r <= 1000000)
                && o.equipment != null && o.prisoners != null && o.sites != null && o.treaties != null
                && o.equipment.Distinct().Count() == o.equipment.Count && o.prisoners.Distinct().Count() == o.prisoners.Count
                && o.sites.Distinct().Count() == o.sites.Count && o.sites.All(i => i >= 0 && i < world.Sites.Count)
                && o.treaties.Distinct().Count() == o.treaties.Count && o.treaties.All(t => Enum.IsDefined(typeof(TreatyKind), t));
            if (!Shape(give) || !Shape(take)) return "잘못된 거래 항목입니다.";
            var market = Data.markets.Contains(c);
            if (give.blueprint || take.blueprint && (c.id != "market:" + Data.blueprintSite || CampaignResearch.Instance?.HasBlueprint != false || give.resources[2] < DiplomacyRules.BlueprintPrice)) return "설계도는 지정 교역소에서 Special 150에 판매합니다.";
            if (market && (give.sites.Count + take.sites.Count + give.prisoners.Count + take.prisoners.Count + give.treaties.Count + take.treaties.Count > 0)) return "교역소는 자원·장비만 거래합니다.";
            if (give.treaties.Concat(take.treaties).Any(t => c.treaties[(int)t] - Data.elapsed > DiplomacyRules.Month)) return "협정 갱신은 만료 1개월 전부터 가능합니다.";
            var rm = ResourceManager.Instance;
            // 받는 자원·장비가 창고를 넘치면 주변 바닥에 둔다(2026-10-09). 보유량만 확인한다.
            for (var i = 0; i < 3; i++)
                if (give.resources[i] > rm.GetAmount((ResourceType)i) || take.resources[i] > c.resources[i]
                    || (long)c.resources[i] - take.resources[i] + give.resources[i] > int.MaxValue) return "보유 자원이 부족합니다. 조건을 수정하세요.";
            if (give.equipment.Any(id => !EquipmentInventory.Instance.Items.Any(e => e.id == id)) || take.equipment.Any(id => !c.equipment.Any(e => e.id == id))) return "장비 소유권이 바뀌었습니다. 조건을 수정하세요.";
            // 포로: 그 세력 출신 포로를 돌려주거나 자국 장수를 되찾는 것만 허용(제3자 포로 구매 없음).
            if (give.prisoners.Any(id => !ReleasableTo(c).Any(p => p.PersonalState.id == id)) || take.prisoners.Any(id => !RansomableIds(c).Contains(id))) return "포로 대상이 바뀌었습니다(회유·사망·석방).";
            if (take.prisoners.Count > 0 && CommanderRoster.Instance == null) return "장수를 합류시킬 수 없습니다.";
            bool Busy(ExpeditionSite s) => s.Visitor != null || s.Defense?.UnderAttack == true || s.Settlement?.Garrison.Count > 0 || s.Defense?.Prisoners.Count > 0;
            if (give.sites.Any(i => world.Sites[i].Disposition != ConquestDisposition.Annexed || Busy(world.Sites[i]))
                || take.sites.Any(i => Faction(world.Sites[i]) != c || world.Sites[i].Disposition == ConquestDisposition.Annexed || Busy(world.Sites[i]))) return "거점 소유권 또는 주둔 부대를 확인하세요.";
            var received = TradeValue(give, c, true); var paid = TradeValue(take, c, false);
            if (received == 0 && paid == 0) return "거래 항목을 선택하세요.";
            if (!checkValue) return "";
            return received + .0001 >= paid * PriceFactor(c) ? "" : "상대가 요구하는 가치가 부족합니다.";
        }
        public bool TryTrade(Civilization c, TradeOffer give, TradeOffer take, out string error) => TryTrade(c, give, take, true, out error);
        public bool TryTrade(Civilization c, TradeOffer give, TradeOffer take, bool checkValue, out string error)
        {
            error = TradeReason(c, give, take, checkValue); if (error != "") return false;
            ResourceManager.Instance.TrySpend(give.resources[0], give.resources[1], give.resources[2], ResourceReason.Trade);
            for (var i = 0; i < 3; i++) { StoreResource((ResourceType)i, take.resources[i], ResourceReason.Trade); c.resources[i] += give.resources[i] - take.resources[i]; }
            var inventory = EquipmentInventory.Instance.Items;
            var outgoing = inventory.Where(e => give.equipment.Contains(e.id)).ToArray();
            var incoming = c.equipment.Where(e => take.equipment.Contains(e.id)).ToArray();
            inventory.RemoveAll(e => give.equipment.Contains(e.id)); c.equipment.RemoveAll(e => take.equipment.Contains(e.id));
            c.equipment.AddRange(outgoing);
            // 석방과 함께 반환하기로 한 압수 장비는 그 장수와 함께 귀환 시 도착한다. 나머지는 즉시 교환(넘치면 바닥).
            var withCaptive = incoming.Where(e => take.prisoners.Any(id => IsSeizedFrom(e.id, id))).ToList();
            StoreEquipment(incoming.Except(withCaptive), world.HomePosition);
            foreach (var id in take.prisoners) BeginHomecoming(id, c, withCaptive.Where(e => IsSeizedFrom(e.id, id)).ToList());
            Data.seizures.RemoveAll(s => s.holder == "player" && give.equipment.Contains(s.item));
            foreach (var camp in Camps) foreach (var p in camp.Prisoners.Where(p => give.prisoners.Contains(p.PersonalState.id)).ToArray())
                if (camp.ReleaseForTrade(p)) CampaignHistory.Record("석방", p.Name, c.name + "로 안전 귀환", true);
            if (give.prisoners.Count > 0) TrustEvent(3, "포로 석방", c);
            c.rebels.RemoveAll(p => take.prisoners.Contains(p.PersonalState.id));
            RemoveTradedRebels(c, take);
            foreach (var i in give.sites) { Data.owners[i] = c.id; world.Sites[i].RestoreState(true, ConquestDisposition.Undecided); }
            foreach (var i in take.sites) world.Sites[i].TransferToPlayer();
            foreach (var treaty in give.treaties.Concat(take.treaties).Distinct()) SignTreaty(c, treaty);
            if (take.blueprint) CampaignResearch.Instance.AcquireBlueprint();
            c.ChangeAffinity(c.agenda == LeaderAgenda.Trader ? 3 : 1, "거래 성사");
            CampaignHistory.Record("거래", c.name, "자원·장비·거점·장수 교환", true); return true;
        }
    }
}
