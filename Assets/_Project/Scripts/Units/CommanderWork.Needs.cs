using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        private string suspendedResearchNeed;
        public bool ResearchWorkReady => CivilianWorkReady && AllowsJob(CommanderJobs.Research)
            && (WorkPriorities.Red || !IsEating && !IsWashing && !IsPlaying && !WorkState.resting)
            && (IsFlying ? HasReachedDestination() : Agent != null && !Agent.pathPending && !Agent.hasPath);
        // 생활로 잠깐 떠나도 플레이어가 지정한 연구 담당은 유지한다. 직접 정지/다른 명령은 기존대로 해제한다.
        private void SuspendForNeeds(string need)
        {
            if ((ScienceAssignment != null || SurveyAssignment != null) && suspendedResearchNeed == need) return;
            var lab = ScienceAssignment;
            var scanner = SurveyAssignment;
            var candidate = scanner != null ? scanner.CandidateId : -1;
            CommandStop();
            if (lab != null && lab.isActiveAndEnabled && !lab.IsDead && !IsDeployed && !IsAwayFromHome)
            { lab.RestoreAssignment(lab.Tier, this); suspendedResearchNeed = need; }
            if (scanner != null && scanner.isActiveAndEnabled && !scanner.IsDead && !IsDeployed && !IsAwayFromHome)
            { scanner.RestoreAssignment(this, candidate); suspendedResearchNeed = need; }
        }
        private void ReturnToResearch()
        {
            suspendedResearchNeed = null;
            var lab = ScienceAssignment;
            if (lab == null) return;
            if ((Position - lab.Position).sqrMagnitude <= 49) { StopMoving(); SetWorkTarget(lab); return; }
            if ((!IsFlying && Agent.pathPending || !IsFlying && Agent.hasPath)) return;
            if (TryWorkApproach(lab.Position, out var point)) SetMoveDestination(point);
        }
    }
}
