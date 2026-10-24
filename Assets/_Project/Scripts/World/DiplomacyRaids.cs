using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.UI;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    // 월드 이동은 플레이어 원정과 같이 도착 시각으로 표현한다. 출발 때 병력 비용을 내고 저장한다.
    [Serializable] public sealed class TravellingRaid
    {
        public int source, count;
        public string faction = "";
        public float arrive;
    }

    public sealed partial class DiplomacyManager
    {
        private void MarkContact(Civilization c)
        {
            if (c.contacted) return;
            c.contacted = true; c.contactedAt = Data.elapsed;
            c.nextConquestCheck = Data.elapsed + DiplomacyRules.Season;
            CampaignHistory.Record("접촉", c.name, c.leader, true);
        }

        // 기존 외교 전력 추정 단위(장수 20 + 병력)를 본거지 가용 인원에만 적용한다.
        public float AvailableHomePower
        {
            get
            {
                var commanders = CommanderRoster.Instance?.Commanders.Where(c => c != null && c.IsColonyMember
                    && c.CanReceiveOrders && !c.IsAwayFromHome && !c.IsChild && !c.PersonalState.treating && !c.PersonalState.health.Downed).ToArray();
                return commanders == null || commanders.Length == 0 ? 0 : (AntPool.Instance?.Free ?? 0) + commanders.Sum(c => 20 + c.TroopCount);
            }
        }
        public static bool IsWeakAgainst(float raidPower, float availablePower) => raidPower > 0
            && raidPower >= Mathf.Max(0, availablePower) * DiplomacyRules.WeaknessRatio;
        private ExpeditionSite RaidSource(Civilization c) => world.Sites.FirstOrDefault(s => Faction(s) == c
            && s.Disposition != ConquestDisposition.Annexed && s.Colony != null && !s.Colony.IsDefeated)
            ?? (c.rebel ? world.Sites.FirstOrDefault(s => Faction(s) == c && s.Disposition != ConquestDisposition.Annexed) : null);
        private static int RaidCount(float food) => Mathf.Clamp((int)food / 20, 1, 20);

        private void TickConquerors()
        {
            foreach (var c in Data.civilizations.Where(c => c.contacted && !c.extinct && c.agenda == LeaderAgenda.Conqueror))
            {
                if (Data.elapsed < c.contactedAt + DiplomacyRules.Season || Data.elapsed < c.nextConquestCheck) continue;
                c.nextConquestCheck = Data.elapsed + DiplomacyRules.Season;
                if (RocketCampaign.SuppressNewCrises) continue; // G39: 로켓 가동 중엔 이번 계절 판단을 건너뛴다(밀린 판단 누적 없음).
                if (c.war)
                {
                    Raid(c, c.rebel && Data.elapsed - c.founded < DiplomacyRules.Year);
                    c.nextRaid = c.nextConquestCheck;
                    continue;
                }
                var source = RaidSource(c);
                var food = source?.Colony?.GetStock(AntColony.Data.ResourceType.Food) ?? c.resources[0];
                var count = RaidCount(food);
                if (source == null || food < count * 10 || !IsWeakAgainst(count * 20, AvailableHomePower)) continue;
                if (DeclareWar(c) && Raid(c, true)) c.nextRaid = Data.elapsed + DiplomacyRules.Season;
            }
        }

        public float EstimatedTravel(Civilization c) => world.Sites.Where(s => Faction(s) == c && s.Disposition != ConquestDisposition.Annexed)
            .Select(ExpeditionRules.WalkSeconds).DefaultIfEmpty(GameCalendar.SecondsPerDay).Min();

        private bool DispatchRaid(ExpeditionSite source, Civilization c, int count)
        {
            if (source == null || Data.travellingRaids.Count >= 100 || Data.travellingRaids.Any(r => r.source == world.Sites.ToList().IndexOf(source))) return false;
            Data.travellingRaids.Add(new TravellingRaid { source = world.Sites.ToList().IndexOf(source),
                faction = c?.id ?? "", count = count, arrive = Data.elapsed + ExpeditionRules.WalkSeconds(source) });
            ToastManager.Show($"{c?.name ?? source.Title} 원정대 {count}명 출발 · 본거지 도착까지 {ExpeditionRules.WalkSeconds(source) / GameCalendar.SecondsPerDay:0.0}일", ToastKind.Warning);
            return true;
        }

        public bool TryDispatchIndependentRaid()
        {
            if (!Available || ExpeditionRules.HomeCommanders == 0 || RocketCampaign.SuppressNewCrises || Data.travellingRaids.Any(r => r.faction == "")) return false;
            foreach (var source in world.Sites.Where(s => s.Kind == ExpeditionSiteKind.Settlement && Faction(s) == null
                && s.Disposition != ConquestDisposition.Annexed && s.Colony != null && !s.Colony.IsDefeated))
            {
                var count = Mathf.Min(3, RaidCount(source.Colony.GetStock(AntColony.Data.ResourceType.Food)));
                if (source.Colony.GetStock(AntColony.Data.ResourceType.Food) < count * 10 || !DispatchRaid(source, null, count)) continue;
                source.Colony.TrySpendResources(count * 10, 0);
                CampaignHistory.Record("원정대 출발", source.Title, "본거지로 이동 중", true);
                return true;
            }
            return false;
        }

        private void TickRaidTravel()
        {
            foreach (var raid in Data.travellingRaids.ToArray())
            {
                var c = Data.civilizations.Find(c => c.id == raid.faction);
                if (raid.faction != "" && (c == null || c.extinct || !c.war)) { Data.travellingRaids.Remove(raid); continue; }
                if (Data.elapsed < raid.arrive || c == null && ExpeditionRules.HomeCommanders == 0) continue;
                var incursions = FindAnyObjectByType<LocalIncursions>();
                if (incursions == null || !incursions.TryArriveRaid(c, raid.count)) continue;
                Data.travellingRaids.Remove(raid);
            }
        }

        private static bool ValidRaidTravel(State s, int sites)
        {
            s.travellingRaids ??= new List<TravellingRaid>();
            return s.travellingRaids.Count <= 100 && s.travellingRaids.All(r => r != null && r.source >= 0 && r.source < sites
                    && r.count >= 1 && r.count <= 20 && r.faction != null && (r.faction == "" || s.civilizations.Any(c => c.id == r.faction))
                    && !float.IsNaN(r.arrive) && !float.IsInfinity(r.arrive) && r.arrive >= 0)
                && s.travellingRaids.Select(r => r.source).Distinct().Count() == s.travellingRaids.Count;
        }
    }
}
