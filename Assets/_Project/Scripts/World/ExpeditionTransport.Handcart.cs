using System;
using System.Linq;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    [Serializable]
    public sealed class HandcartPullState
    {
        public bool leaving, paused;
        public string driverId;
        public Vector3 gate, driverPosition;
    }

    public partial class ExpeditionTransport
    {
        public bool Handcart { get; private set; }
        private HandcartPullState cartPull = new HandcartPullState();
        private CommanderAnt CartDriver => crew.FirstOrDefault(c => c != null && c.PersonalState.id == cartPull.driverId);
        public bool PullingHandcart => Handcart && cartPull.leaving;
        public string HandcartStatus => !PullingHandcart ? "" : CartDriver == null || !CartDriver.CanReceiveOrders
            ? "견인 중단 · 장수 회복 또는 귀환 필요" : cartPull.paused ? "견인 일시 중단 · 출발 버튼으로 재개" : "장수가 출입 길목까지 손수레를 끄는 중";

        private void AttachHandcartModel()
        {
            // Vehicle templates are nonuniformly scaled. A rotating cart must keep a rigid model.
            transform.localScale = Vector3.one;
            if (GetComponent<BoxCollider>() is BoxCollider box)
            {
                box.size = new Vector3(1.6f, 1.4f, 3.4f);
                box.center = Vector3.up * .7f;
            }
            var obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle != null) obstacle.enabled = false;
            var model = ProductionProp.Attach(transform, "Transport1115/Handcart", 3.4f, Position);
            if (model != null)
            {
                model.transform.Rotate(Vector3.up, 270, Space.World);
                var placeholder = GetComponent<Renderer>();
                if (placeholder != null) placeholder.forceRenderingOff = true;
            }
        }

        private bool BeginCartPull()
        {
            var driver = crew.FirstOrDefault(c => c != null && !c.IsDead && c.PersonalHealth > 0 && !c.Health.Downed && !c.IsFlying);
            if (driver == null || !HomeReturnPoint.TryGround(Position, out var gate)
                || !NavMesh.SamplePosition(Position + Vector3.forward * 2, out var start, 4, NavMesh.AllAreas)) return false;
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(start.position, gate, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) return false;
            driver.SetEmbarked(false, start.position);
            if (!driver.CanReceiveOrders) { driver.SetEmbarked(true, Position); return false; }
            cartPull = new HandcartPullState { leaving = true, driverId = driver.PersonalState.id, gate = gate, driverPosition = start.position };
            driver.CommandMove(gate);
            return true;
        }

        private bool TickCartPull()
        {
            if (!PullingHandcart) return false;
            var driver = CartDriver;
            if (driver == null || !driver.CanReceiveOrders || driver.IsEmbarked) return true;
            var movement = driver.Position - cartPull.driverPosition;
            movement.y = 0;
            if (movement.sqrMagnitude > .00001f)
            {
                // Keep the shafts behind the puller when the route turns, rather than
                // retaining the initial world-space offset beside the character.
                var facing = movement.normalized;
                var next = driver.Position - facing * 2f;
                next.y = Position.y + driver.Position.y - cartPull.driverPosition.y;
                transform.SetPositionAndRotation(next, Quaternion.LookRotation(facing));
            }
            cartPull.driverPosition = driver.Position;
            if ((driver.Position - cartPull.gate).sqrMagnitude > 2.25f)
            {
                if (!driver.IsFollowingMoveOrder) cartPull.paused = true;
                return true;
            }
            driver.SetEmbarked(true, Position);
            cartPull.leaving = false;
            RecallEscorts();
            return true;
        }

        private bool CancelCartPull()
        {
            var driver = CartDriver;
            if (driver != null) driver.CommandStop();
            cartPull.leaving = false;
            State = ExpeditionState.Home;
            Remaining = 0;
            if (Site != null && Site.Visitor == this) Site.Visitor = null;
            Site = null;
            RecallEscorts();
            // Keep the cart and driver at their actual position. Unload orders use real paths.
            foreach (var c in crew) if (c != null && c != driver) c.SetEmbarked(false, Position);
            TryUnloadCrew();
            return true;
        }

        public bool ResumeHandcartPull()
        {
            var driver = CartDriver;
            if (!PullingHandcart || driver == null || !driver.CanReceiveOrders || !driver.CanReach(cartPull.gate)) return false;
            driver.CommandMove(cartPull.gate);
            cartPull.paused = false;
            return driver.IsFollowingMoveOrder;
        }

        internal HandcartPullState CaptureCartPull() => !Handcart ? null : new HandcartPullState {
            leaving = cartPull.leaving, paused = cartPull.paused, driverId = cartPull.driverId, gate = cartPull.gate,
            driverPosition = CartDriver != null ? CartDriver.Position : cartPull.driverPosition };
        internal void RestoreCartPull(HandcartPullState state) => cartPull = Handcart && state != null ? state : new HandcartPullState();
        private void RestoreCartPuller()
        {
            if (!PullingHandcart || CartDriver == null) return;
            CartDriver.SetEmbarked(false, cartPull.driverPosition);
            if (!cartPull.paused) CartDriver.CommandMove(cartPull.gate);
        }
    }
}
