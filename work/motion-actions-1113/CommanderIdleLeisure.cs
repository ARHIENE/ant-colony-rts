using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using UnityEngine;
using UnityEngine.AI;
namespace AntColony.Units
{
    // Chosen only after the existing work scan found no player-designated work.
    public partial class CommanderAnt
    {
        string idleLeisureMotion;
        float idleLeisureUntil;
        bool IdleLeisureReady => CivilianWorkReady && CanReceiveOrders && !IsWorking && !IsInCombat
            && !HasCombatOrder && !IsDirectMoving && !IsEating && !IsWashing && !IsPlaying
            && !IsCarrying && !WorkState.resting && !WorkPriorities.Red && !LabUpgradeBusy
            && ScienceAssignment == null && CraftingWorkshop == null && ServiceTarget == null
            && SurveyAssignment == null && MedicalJob == MedJob.None && AnimalJob == AnimalTask.None
            && !AntColony.World.ExpeditionTransport.HasPendingCrewOrder(this)
            && AntColony.World.WaveDefense.Instance?.InWave != true;
        public string IdleLeisureMotion => IdleLeisureReady && Time.time < idleLeisureUntil ? idleLeisureMotion : null;
        public bool IsLeisureWalking => IdleLeisureReady && idleLeisureMotion == "IdleLook" && Time.time < idleLeisureUntil;
        internal void TryIdleLeisure()
        {
            if (!IdleLeisureReady || Time.time < idleLeisureUntil) return;
            idleLeisureUntil = Time.time + Random.Range(8f, 16f);
            var companion = CommanderRoster.Instance?.Commanders.FirstOrDefault(c => c && c != this && c.IdleLeisureReady && (c.Position-Position).sqrMagnitude < 16);
            var spots = RecreationSpot.All.Where(s => s && s.HasSeat && (s.Position-Position).sqrMagnitude < 25).ToArray();
            // Traits bias choice, never force it. No resource or production orders are created.
            float talk = companion ? (traits.Has(CommanderTrait.Sociable) ? 3f : traits.Has(CommanderTrait.Loner) ? .25f : 1f) : 0;
            float walk = traits.Has(CommanderTrait.Wanderer) ? 3f : traits.Has(CommanderTrait.Homebody) ? .5f : 1f;
            float rest = traits.Has(CommanderTrait.Lazy) || traits.Has(CommanderTrait.Relaxed) ? 2f : 1f;
            float play = spots.Length > 0 ? 1f : 0;
            float choice = Random.value * (talk + walk + rest + play);
            if (choice < talk) { idleLeisureMotion = "IdleTalk"; return; }
            choice -= talk;
            if (choice < walk)
            {
                var offset = Random.insideUnitCircle * 3f;
                if (NavMesh.SamplePosition(Position + new Vector3(offset.x,0,offset.y), out var hit, 1f, NavMesh.AllAreas) && CanReach(hit.position))
                    SetMoveDestination(hit.position);
                idleLeisureMotion = "IdleLook"; return;
            }
            choice -= walk;
            if (choice < rest || spots.Length == 0) { idleLeisureMotion = "IdleRest"; return; }
            var spot = spots[Random.Range(0, spots.Length)];
            if (!TryPlayApproach(spot, out var approach) || !spot.Join(this)) return;
            approachingPlaySpot = spot; playApproach = approach;
            PlaySpot=spot; JoyState.kind=spot.KindIndex; JoyState.playSeconds=GameBalance.PlaySeconds;
            JoyState.freeWalk=false; JoyState.groupStarted=false; JoyState.waitSeconds=0;
            idleLeisureMotion=null;
        }
    }
}
