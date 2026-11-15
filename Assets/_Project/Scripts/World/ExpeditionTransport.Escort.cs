using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    public partial class ExpeditionTransport
    {
        static bool EscortOrder(CrewOrderKind kind) => kind == CrewOrderKind.Escort || kind == CrewOrderKind.EscortReturn;
        public IEnumerable<CommanderAnt> Escorts => crewOrders.Where(o => EscortOrder(o.kind) && OrderStillHere(o)).Select(o => o.commander);
        public static bool IsEscorting(CommanderAnt commander) => WorldMapManager.Instance != null
            && WorldMapManager.Instance.Transports.Any(t => t != null && t.Escorts.Contains(commander));
        public bool EscortReturning => crewOrders.Any(o => o.kind == CrewOrderKind.EscortReturn);
        public string EscortStatus => !Escorts.Any() ? "호위 없음" : (EscortReturning ? "호위대 복귀" : "출정 길목 호위")
            + " · " + string.Join(", ", Escorts.Select(c => c.CommanderName));

        // Escorts remain home-map commanders: ordinary targeting, damage and player orders still apply.
        public bool AssignEscorts(IReadOnlyList<CommanderAnt> commanders)
        {
            if (IsDead || commanders == null || commanders.Count == 0 || State == ExpeditionState.Returning) return false;
            var unique = commanders.Distinct().ToArray();
            var reinforcementPoint = State == ExpeditionState.Home ? homePosition : Escorts.FirstOrDefault()?.Position ?? homePosition;
            if (unique.Any(c => c == null || c.IsAwayFromHome || !c.CanReceiveOrders || c.IsCaptive
                || !c.IsColonyMember || crew.Contains(c) || !c.IsDeployed && !c.CanMobilize
                || !c.TryWorkApproach(homePosition, out _))) return false;
            foreach (var c in unique)
            {
                foreach (var ship in WorldMapManager.Instance.Transports) ship?.CancelCrewOrder(c);
                if (!c.IsDeployed) c.Mobilize(0, c.Position);
                c.CommandAttackMove(reinforcementPoint);
                crewOrders.Add(new CrewOrder { commander = c, kind = State == ExpeditionState.Home ? CrewOrderKind.Escort : CrewOrderKind.EscortReturn,
                    state = State, site = Site });
            }
            UpdateCrewOrderStatus();
            return true;
        }

        public void RecallEscorts()
        {
            foreach (var order in crewOrders.Where(o => EscortOrder(o.kind)))
            {
                order.kind = CrewOrderKind.EscortReturn;
                if (order.commander != null && order.commander.CanReceiveOrders)
                    order.commander.CommandAttackMove(order.commander.WorkState.returnPosition);
            }
            UpdateCrewOrderStatus();
        }

        void TickEscort(CrewOrder order)
        {
            var c = order.commander;
            if (!c.CanReceiveOrders || c.LabUpgradeBusy || c.PersonalState.treating) return;
            var destination = order.kind == CrewOrderKind.Escort ? homePosition : c.WorkState.returnPosition;
            if ((c.Position - destination).sqrMagnitude <= 9)
            {
                if (order.kind == CrewOrderKind.EscortReturn && !c.IsFollowingCombatOrder
                    && !c.IsFollowingMoveOrder && !c.IsInCombat)
                {
                    crewOrders.Remove(order);
                    c.FinishEscort();
                }
                return;
            }
            // Do not overwrite manual attacks, retreats or an active fight.
            if (!c.IsFollowingCombatOrder && !c.IsFollowingMoveOrder && !c.IsInCombat
                && c.TryWorkApproach(destination, out var point)) c.CommandAttackMove(point);
        }
    }
}
