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
            var prisoners = player ? PlayerPrisoners : c.prisoners.Concat(c.rebels);
            double value = offer.resources[0] + (double)offer.resources[1] + offer.resources[2] * 10d;
            value += gear.Where(e => offer.equipment.Contains(e.id)).Sum(e => DiplomacyRules.EquipmentValues[e.quality]);
            value += prisoners.Where(p => offer.prisoners.Contains(p.PersonalState.id)).Sum(p => p.Talents.levels.Sum() * 5);
            value += offer.sites.Sum(i => world.Sites[i].Difficulty * 300);
            value += offer.treaties.Sum(t => DiplomacyRules.TreatyValues[(int)t]);
            if (offer.blueprint) value += DiplomacyRules.BlueprintPrice * 10;
            return value;
        }
        public string TradeReason(Civilization c, TradeOffer give, TradeOffer take)
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
            for (var i = 0; i < 3; i++)
                if (give.resources[i] > rm.GetAmount((ResourceType)i) || take.resources[i] > c.resources[i]
                    || (long)rm.GetAmount((ResourceType)i) - give.resources[i] + take.resources[i] > rm.GetCapacity((ResourceType)i)
                    || (long)c.resources[i] - take.resources[i] + give.resources[i] > int.MaxValue) return "보유 자원 또는 창고 공간이 부족합니다.";
            if (give.equipment.Any(id => !EquipmentInventory.Instance.Items.Any(e => e.id == id)) || take.equipment.Any(id => !c.equipment.Any(e => e.id == id))
                || EquipmentInventory.Instance.Items.Count - give.equipment.Count + take.equipment.Count > EquipmentInventory.Capacity) return "장비 소유권 또는 보관함 공간을 확인하세요.";
            if (give.prisoners.Any(id => !PlayerPrisoners.Any(p => p.PersonalState.id == id)) || take.prisoners.Any(id => !c.prisoners.Concat(c.rebels).Any(p => p.PersonalState.id == id))) return "포로 소유권이 변경되었습니다.";
            if (take.prisoners.Count > 0 && CommanderRoster.Instance == null) return "장수를 합류시킬 수 없습니다.";
            bool Busy(ExpeditionSite s) => s.Visitor != null || s.Defense?.UnderAttack == true || s.Settlement?.Garrison.Count > 0 || s.Defense?.Prisoners.Count > 0;
            if (give.sites.Any(i => world.Sites[i].Disposition != ConquestDisposition.Annexed || Busy(world.Sites[i]))
                || take.sites.Any(i => Faction(world.Sites[i]) != c || world.Sites[i].Disposition == ConquestDisposition.Annexed || Busy(world.Sites[i]))) return "거점 소유권 또는 주둔 부대를 확인하세요.";
            var received = TradeValue(give, c, true); var paid = TradeValue(take, c, false);
            if (received == 0 && paid == 0) return "거래 항목을 선택하세요.";
            var multiplier = market ? 1 : DiplomacyRules.AcceptanceMultiplier(c.affinity) * DiplomacyRules.PriceMultiplier(c.affinity)
                * (c.HasTreaty(TreatyKind.Trade, Data.elapsed) ? .9f : 1);
            return received + .0001 >= paid * multiplier ? "" : "상대가 요구하는 가치가 부족합니다.";
        }
        public bool TryTrade(Civilization c, TradeOffer give, TradeOffer take, out string error)
        {
            error = TradeReason(c, give, take); if (error != "") return false;
            // 생성 실패 시 비용·소유권을 변경하기 전에 되돌린다.
            var recruits = new List<CommanderAnt>();
            foreach (var p in c.prisoners.Concat(c.rebels).Where(p => take.prisoners.Contains(p.PersonalState.id)).ToArray())
            {
                var recruit = CommanderRoster.Instance.Create(p.Name, p.Rank, p.Roles, p.Roles[0], p.Traits, world.HomePosition + Vector3.right * 3);
                if (recruit == null)
                {
                    foreach (var r in recruits) { CommanderRoster.Instance.Forget(r); Destroy(r.gameObject); }
                    error = "장수 합류 실패. 거래를 취소했습니다."; return false;
                }
                recruit.RestoreTalents(p.Talents); recruit.RestorePersonalState(p.PersonalState); recruit.Social.departure = DepartureState.None;
                recruit.Social.pendingDeparture = false; recruit.PersonalState.departure = ""; recruit.Traits.SetLoyalty(30);
                recruit.RestoreLabLevels(p.LabAttack, p.LabArmor); recruit.Skills.Restore(false, p.StrikeCooldown, p.StanceCooldown, 0);
                recruits.Add(recruit);
            }
            ResourceManager.Instance.TrySpend(give.resources[0], give.resources[1], give.resources[2], ResourceReason.Trade);
            for (var i = 0; i < 3; i++) { ResourceManager.Instance.Add((ResourceType)i, take.resources[i], ResourceReason.Trade); c.resources[i] += give.resources[i] - take.resources[i]; }
            var inventory = EquipmentInventory.Instance.Items;
            var outgoing = inventory.Where(e => give.equipment.Contains(e.id)).ToArray();
            var incoming = c.equipment.Where(e => take.equipment.Contains(e.id)).ToArray();
            inventory.RemoveAll(e => give.equipment.Contains(e.id)); c.equipment.RemoveAll(e => take.equipment.Contains(e.id));
            inventory.AddRange(incoming); c.equipment.AddRange(outgoing);
            foreach (var camp in Camps) foreach (var p in camp.Prisoners.Where(p => give.prisoners.Contains(p.PersonalState.id)).ToArray())
                if (camp.ReleaseForTrade(p)) c.prisoners.Add(p);
            c.prisoners.RemoveAll(p => take.prisoners.Contains(p.PersonalState.id)); c.rebels.RemoveAll(p => take.prisoners.Contains(p.PersonalState.id));
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
