using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    public enum CrewOrderKind { Board, Station, Unload }

    [Serializable]
    public sealed class CrewOrderState
    {
        public string commanderId;
        public CrewOrderKind kind;
        public ExpeditionState state;
        public int siteIndex = -1, sourceTransportIndex = -1, sourceSettlementIndex = -1;
    }

    public partial class ExpeditionTransport
    {
        private sealed class CrewOrder
        {
            public CommanderAnt commander;
            public CrewOrderKind kind;
            public ExpeditionState state;
            public ExpeditionSite site;
            public ExpeditionTransport sourceTransport;
            public AnnexedSettlement sourceSettlement;
        }

        private readonly List<CrewOrder> crewOrders = new List<CrewOrder>();
        public int PendingCrewOrderCount => crewOrders.Count;
        public string CrewOrderStatus { get; private set; } = "";

        public void CancelCrewOrder(CommanderAnt commander)
        {
            crewOrders.RemoveAll(o => o.commander == commander);
            UpdateCrewOrderStatus();
        }
        // 플레이어가 새 명령을 내린 경계에서만 부른다(내부 CommandStop에 넣으면 큐가 스스로 취소된다).
        // G42: 직접 지시(탑승·주둔·하차)를 기다리는 장수는 자율 작업을 새로 잡지 않는다(생활 욕구·불응 처리는 그대로).
        public static bool HasPendingCrewOrder(CommanderAnt commander)
        {
            var world = WorldMapManager.Instance; if (world == null || commander == null) return false;
            foreach (var ship in world.Transports) if (ship != null && ship.crewOrders.Exists(o => o.commander == commander)) return true;
            return false;
        }
        public static void CancelCrewOrdersFor(CommanderAnt commander)
        {
            var world = WorldMapManager.Instance; if (world == null || commander == null) return;
            foreach (var ship in world.Transports) if (ship != null) ship.CancelCrewOrder(commander);
            foreach (var ship in world.Transports.Where(t => t != null))
            {
                if (ship.IsWithdrawing && ship.Crew.Contains(commander)) ship.CancelWithdrawal();
                if (ship.repairWorker == commander || ship.salvageWorker == commander) ship.CancelFieldWork();
                ship.withdraw.loads.RemoveAll(l => l.workerId == commander.PersonalState.id);
                foreach (var job in ship.withdraw.bodies.Where(j => j.carrierId == commander.PersonalState.id).ToArray()) ship.CancelBodyRecovery(job.bodyId);
            }
        }

        private void ClearCrewOrders()
        {
            crewOrders.Clear();
            UpdateCrewOrderStatus();
        }

        private bool QueueCrewOrders(IReadOnlyList<CommanderAnt> passengers, CrewOrderKind kind)
        {
            if (!isActiveAndEnabled || IsWithdrawing || Route?.IsRunning == true || passengers == null || passengers.Count == 0) return false;
            if (kind == CrewOrderKind.Unload ? State != ExpeditionState.Home
                : kind == CrewOrderKind.Station ? State != ExpeditionState.Deployed || Site?.Settlement == null
                    || Site.Disposition != ConquestDisposition.Annexed
                : State != ExpeditionState.Home && (State != ExpeditionState.Deployed || Site == null)) return false;
            var unique = new HashSet<CommanderAnt>();
            foreach (var c in passengers)
            {
                if (c == null || !unique.Add(c) || !c.isActiveAndEnabled || c.IsDead || !c.IsColonyMember
                    || c.IsCaptive || c.IsDeparting || c.ScoutMission != null) return false;
                if (kind != CrewOrderKind.Board)
                {
                    if (c.Transport != this || !crew.Contains(c) || kind == CrewOrderKind.Station && !c.HasTroops) return false;
                }
                else if (c.Transport == this || (State == ExpeditionState.Home ? c.IsAwayFromHome
                    : c.Transport != null && (c.Transport.Site != Site || c.Transport.State != ExpeditionState.Deployed)
                        || c.Garrison != null && c.Garrison != Site.Settlement)) return false;
            }
            if (kind == CrewOrderKind.Board)
            {
                var reserved = crewOrders.Where(o => o.kind == CrewOrderKind.Board && !unique.Contains(o.commander))
                    .Select(o => o.commander).Where(c => c != null).Concat(passengers).ToArray();
                if (Load + reserved.Sum(c => c.TroopCount) > Capacity || CommanderLoad + reserved.Length > CommanderCapacity
                    || State == ExpeditionState.Home && ColonyPopulation.Instance?.S.policy == MilitaryPolicy.Reserve
                        && passengers.Any(c => c.TroopCount > 0)) return false;
            }
            foreach (var c in passengers)
            {
                // 다른 수송에 내린 예전 직접 지시는 대체한다.
                if (WorldMapManager.Instance != null)
                    foreach (var ship in WorldMapManager.Instance.Transports)
                        if (ship != null) ship.CancelCrewOrder(c);
                CancelCrewOrder(c);
                crewOrders.Add(new CrewOrder { commander = c, kind = kind, state = State, site = Site,
                    sourceTransport = c.Transport, sourceSettlement = c.Garrison });
            }
            TickCrewOrders();
            return true; // 접수 성공. 실제 완료 여부는 PendingCrewOrderCount로 구분한다.
        }

        private bool OrderStillHere(CrewOrder order)
        {
            var c = order.commander;
            return c != null && c.isActiveAndEnabled && !c.IsDead && c.IsColonyMember && !c.IsCaptive && !c.IsDeparting
                && c.ScoutMission == null && State == order.state && Site == order.site
                && c.Transport == order.sourceTransport && c.Garrison == order.sourceSettlement;
        }

        private void TickCrewOrders()
        {
            foreach (var order in crewOrders.ToArray())
            {
                if (!OrderStillHere(order)) { crewOrders.Remove(order); continue; }
                var c = order.commander;
                if (c.OrderRefusalReason != "" || c.LabUpgradeBusy || c.PersonalState.treating) continue;
                if (order.kind == CrewOrderKind.Unload)
                {
                    if (!TryHomeLanding(out var point)) continue;
                    c.SetEmbarked(false, point);
                    c.Transport = null;
                    crew.Remove(c);
                    crewOrders.Remove(order);
                    continue;
                }
                if (!c.CanChangeAllocation) continue;
                if (Vector3.Distance(c.Position, Position) > 8)
                {
                    if (c.TryWorkApproach(Position, out var approach)) c.CommandMove(approach);
                    continue;
                }
                if (order.kind == CrewOrderKind.Board ? BoardNow(c) : StationNow(c)) crewOrders.Remove(order);
            }
            UpdateCrewOrderStatus();
        }

        private void UpdateCrewOrderStatus()
        {
            CrewOrderStatus = string.Join("\n", crewOrders.Where(o => o.commander != null).Select(o =>
                o.commander.CommanderName + ": " + o.kind + " 대기 · "
                + (o.commander.OrderRefusalReason != "" ? o.commander.OrderRefusalReason : "이동/안전 지점/작업 종료 대기")));
            AntColony.UI.ToastManager.SetPinned("crew:" + GetEntityId(), CrewOrderStatus == "" ? null : CrewOrderStatus);
        }

        // B3 저장 연결용. Capture는 참조 목록 확정 후, Restore는 승무원/주둔 재연결 후 호출한다.
        internal List<CrewOrderState> CaptureCrewOrders() => crewOrders.Where(OrderStillHere).Select(o => new CrewOrderState {
            commanderId = o.commander.PersonalState.id, kind = o.kind, state = o.state,
            siteIndex = Save.SaveCatalog.SiteIndex(o.site), sourceTransportIndex = Save.SaveCatalog.ShipIndex(o.sourceTransport),
            sourceSettlementIndex = Save.SaveCatalog.SiteIndex(o.sourceSettlement != null ? o.sourceSettlement.Site : null)
        }).ToList();

        internal void RestoreCrewOrders(List<CrewOrderState> saved)
        {
            ClearCrewOrders();
            if (saved == null) return;
            var world = WorldMapManager.Instance;
            foreach (var o in saved)
            {
                var c = CommanderRoster.Instance?.Commanders.FirstOrDefault(x => x.PersonalState.id == o.commanderId);
                if (c == null) continue;
                crewOrders.Add(new CrewOrder { commander = c, kind = o.kind, state = o.state,
                    site = o.siteIndex < 0 ? null : world.Sites[o.siteIndex],
                    sourceTransport = o.sourceTransportIndex < 0 ? null : world.Transports[o.sourceTransportIndex],
                    sourceSettlement = o.sourceSettlementIndex < 0 ? null : world.Sites[o.sourceSettlementIndex].Settlement });
            }
            // 복원 자체는 이동/탑승/하차를 실행하지 않는다.
            UpdateCrewOrderStatus();
        }
    }
}
