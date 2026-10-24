using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    // 묘비 안치 운반(2026-10-15, 운반 작업): 안치 지시가 걸린 묘비의 시신·유해를 운반 장수가 옮긴다.
    public partial class CommanderAnt
    {
        private Corpse burialBody; private Tombstone burialTomb; private bool burialCarrying;
        public Corpse BurialBody => burialBody;
        private float memorialTimer;

        private IEnumerable<(Component target, Func<bool> start)> BurialTasks()
        {
            foreach (var tomb in Tombstone.All.Where(t => t.Data2.burialOrdered && !t.Data2.body))
            {
                var body = Tombstone.BodyOf(tomb.Data2.deceasedId);
                if (body == null || body.Handler != null || Active.OfType<CommanderAnt>().Any(c => c.burialBody == body)) continue;
                var (b, t) = (body, tomb);
                yield return (body, () => StartBurial(b, t));
            }
        }
        private bool StartBurial(Corpse body, Tombstone tomb)
        {
            if (!CivilianWorkReady || !AllowsJob(CommanderJobs.Hauling) || IsCarrying || !SameBattlefield(body.Position) || !TryWorkApproach(body.Position, out var approach)) return false;
            CommandStop(); if (!body.Claim(this)) return false;
            burialBody = body; burialTomb = tomb; burialCarrying = false; SetMoveDestination(approach); return true;
        }
        internal void EndBurial() { burialBody?.Release(this); burialBody = null; burialTomb = null; burialCarrying = false; }

        private bool TickBurial(float seconds)
        {
            if (burialBody == null) return false;
            if (!CivilianWorkReady || !AllowsJob(CommanderJobs.Hauling) || burialTomb == null || !burialTomb.isActiveAndEnabled || !burialTomb.Data2.burialOrdered || burialBody.Handler != this)
            {
                // 취소·묘비 사라짐: 들고 있던 시신은 그 자리에 내려놓는다.
                EndBurial(); return false;
            }
            if (!burialCarrying)
            {
                if ((burialBody.Position - Position).sqrMagnitude > 6.25f)
                {
                    if (!TryWorkApproach(burialBody.Position, out var approach)) EndBurial();
                    else if (IsFlying || Agent != null && !Agent.pathPending && !Agent.hasPath) SetMoveDestination(approach);
                    return true;
                }
                burialCarrying = true;
                if (TryWorkApproach(burialTomb.Position, out var a)) SetMoveDestination(a); else { EndBurial(); return false; }
                return true;
            }
            burialBody.transform.position = Position;
            if ((burialTomb.Position - Position).sqrMagnitude > 9)
            {
                if (!TryWorkApproach(burialTomb.Position, out var approach)) EndBurial();
                else if (IsFlying || Agent != null && !Agent.pathPending && !Agent.hasPath) SetMoveDestination(approach);
                return true;
            }
            burialTomb.Inter(burialBody); burialBody = null; burialTomb = null; burialCarrying = false;
            return true;
        }
    }
}
