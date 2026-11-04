using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    [Serializable]
    public sealed class WithdrawBodyJob
    {
        public string bodyId, carrierId;
        public bool picked;
        public float progress;
    }
    public partial class ExpeditionTransport
    {
        internal List<Corpse.State> bodyCargo = new List<Corpse.State>();
        public IReadOnlyList<Corpse.State> BodyCargo => bodyCargo;
        internal static int BodyWeight(Corpse.State body) => body.remaining > 0 ? ExpeditionWithdraw.BodyWeight : ExpeditionWithdraw.RemainsWeight;
        private int BodyLoad => bodyCargo.Sum(BodyWeight);
        private int BodyReservations => withdraw.bodies.Sum(j => {
            var body = Corpse.All.FirstOrDefault(b => b.Data.commanderId == j.bodyId);
            return body != null ? BodyWeight(body.Data) : ExpeditionWithdraw.BodyWeight;
        });
        public bool TryRecoverBody(Corpse body, CommanderAnt carrier)
        {
            if (IsDead || State != ExpeditionState.Deployed || body == null || !body.IsCommanderBody || !body.Available || body.Handler != null
                || carrier == null || carrier.Transport != this || !carrier.CanReceiveOrders || carrier.IsCarrying
                || repairWorker == carrier || salvageWorker == carrier
                || withdraw.bodies.Any(j => j.carrierId == carrier.PersonalState.id || j.bodyId == body.Data.commanderId)
                || withdraw.jobs.Any(j => !j.boarded && j.helperId == carrier.PersonalState.id)
                || ReturnCargoSpace < BodyWeight(body.Data) || !carrier.CanReach(body.Position)) return false;
            carrier.CommandStop();
            if (!body.Claim(carrier)) return false;
            withdraw.bodies.Add(new WithdrawBodyJob { bodyId = body.Data.commanderId, carrierId = carrier.PersonalState.id });
            return true;
        }
        public void CancelBodyRecovery(string bodyId)
        {
            foreach (var j in withdraw.bodies.Where(j => j.bodyId == bodyId).ToArray())
            {
                var body = Corpse.All.FirstOrDefault(b => b.Data.commanderId == j.bodyId);
                body?.Release(Person(j.carrierId)); withdraw.bodies.Remove(j);
            }
        }
        private void TickBodies(float seconds, ref int places)
        {
            foreach (var j in withdraw.bodies.ToArray())
            {
                var body = Corpse.All.FirstOrDefault(b => b.Data.commanderId == j.bodyId);
                var c = Person(j.carrierId);
                if (body == null || !body.Available || c == null || !c.CanReceiveOrders || c.IsCarrying || body.Handler != c)
                { CancelBodyRecovery(j.bodyId); continue; }
                if (!j.picked)
                {
                    if (!Approach(c, body.Position)) continue;
                    j.picked = true;
                }
                body.transform.position = c.Position;
                if (!Approach(c, Position) || places <= 0) continue;
                places--; j.progress += seconds * c.WorkRate(CommanderActivity.Strength);
                if (j.progress < ExpeditionWithdraw.LoadSeconds) continue;
                withdraw.bodies.Remove(j);
                var state = body.Capture(); state.workerId = null; state.progress = 0; state.eating = false;
                bodyCargo.Add(state); body.Bury();
            }
        }
        private void UnloadBodies(Vector3 at)
        {
            foreach (var state in bodyCargo) { state.position = at; Corpse.Spawn(state); }
            bodyCargo.Clear();
        }
        internal void RestoreBodyWork()
        {
            foreach (var j in withdraw.bodies)
            {
                var body = Corpse.All.FirstOrDefault(b => b.Data.commanderId == j.bodyId);
                var c = Person(j.carrierId);
                if (body != null && c != null) body.Claim(c);
            }
        }
        internal static Corpse.State CaptureFloorBody(Corpse body)
        {
            var state = body.Capture();
            if (WorldMapManager.Instance.Transports.Any(t => t != null && t.withdraw.bodies.Any(j => j.bodyId == state.commanderId)))
            { state.workerId = null; state.progress = 0; state.eating = false; }
            return state;
        }
    }
}
