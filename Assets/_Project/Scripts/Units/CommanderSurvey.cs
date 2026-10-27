using AntColony.Buildings;
namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        public UndergroundScanner SurveyAssignment { get; internal set; }
        internal bool SurveyWorkReady => CivilianWorkReady && CanReceiveOrders && !IsWorking && !IsInCombat
            && !IsEating && MealState.pickedFood == 0 && !IsWashing && !IsPlaying && ServiceTarget == null && CorpseTarget == null
            && (IsFlying ? HasReachedDestination() : Agent != null && !Agent.pathPending && !Agent.hasPath);
        private bool TickSurveyAssignment()
        {
            if (SurveyAssignment == null) return false;
            if (!AllowsJob(CommanderJobs.Research) || !CivilianWorkReady || !CanReceiveOrders || IsAwayFromHome || IsDeployed)
            { SurveyAssignment.Release(); return false; }
            suspendedResearchNeed = null;
            if ((Position - SurveyAssignment.Position).sqrMagnitude <= 49) { StopMoving(); SetWorkTarget(SurveyAssignment); }
            else if ((IsFlying ? HasReachedDestination() : !Agent.pathPending && !Agent.hasPath) && TryWorkApproach(SurveyAssignment.Position, out var point)) SetMoveDestination(point);
            return true;
        }
    }
}
