using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    // 위생 욕구(2026-10-05): 시간이 지나면 줄고, 낮아지면 위생 가구(화장실·세면대·샤워기)에 가서 채운다.
    // 가구가 없거나 못 가면 참는다 → 기분 하락. 일반개미는 숫자로만 있어 해당 없음.
    [Serializable] public class CommanderHygieneState
    {
        public float hygiene = 100, washSeconds, retrySeconds;
        public int kind;
        public bool Valid => Finite(hygiene) && hygiene >= 0 && hygiene <= 100 && Finite(washSeconds) && washSeconds >= 0
            && Finite(retrySeconds) && retrySeconds >= 0 && kind >= 0 && kind < GameBalance.WashHygiene.Length;
        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }

    public partial class CommanderAnt
    {
        private CommanderHygieneState HygieneState => PersonalState.hygiene;
        public float Hygiene => HygieneState.hygiene;
        public bool IsWashing => HygieneState.washSeconds > 0;
        public HygieneFixture WashSpot { get; private set; }

        // TickDuty에서 오락 앞에 부른다. true면 이번 틱은 위생으로 끝낸다.
        private bool TickHygiene(float seconds)
        {
            var h = HygieneState;
            h.hygiene = Mathf.Max(0, h.hygiene - GameBalance.HygienePerSecond * seconds);
            RefreshHygieneMood();
            if (IsDeployed || IsAwayFromHome || IsEmbarked || IsCaptive) { LeaveWash(); return false; }
            if (h.washSeconds > 0)
            {
                h.washSeconds = Mathf.Max(0, h.washSeconds - seconds);
                if (h.washSeconds == 0) FinishWash();
                return true;
            }
            if (h.retrySeconds > 0) { h.retrySeconds = Mathf.Max(0, h.retrySeconds - seconds); return false; }
            if (h.hygiene > GameBalance.WashBelowHygiene || IsCarrying || LabUpgradeBusy) return false;
            // ponytail: 매 틱 가장 가까운 빈 가구를 찾는다. 가구가 많아지면 목표를 캐시한다.
            var spot = HygieneFixture.All.Where(s => s != null && s.Free).OrderBy(s => (s.Position - Position).sqrMagnitude).FirstOrDefault();
            if (spot == null) return false; // 가구가 없으면 참는다(기분 페널티만).
            if (IsWorking || ServiceTarget != null || HuntTarget != null || ScienceAssignment != null || CraftingWorkshop != null) CommandStop();
            if (!CanReceiveOrders) return false;
            if ((spot.Position - Position).sqrMagnitude > 49)
            {
                var moving = IsFlying ? !HasReachedDestination() : Agent.pathPending || Agent.hasPath;
                if (!moving && UnityEngine.AI.NavMesh.SamplePosition(spot.Position, out var hit, 7, UnityEngine.AI.NavMesh.AllAreas) && CanReach(hit.position))
                { base.CommandMove(hit.position); return true; }
                if (moving) return true;
                h.retrySeconds = GameBalance.StarveRetrySeconds; return false;
            }
            if (!spot.Join(this)) return false;
            WashSpot = spot; h.kind = spot.KindIndex; h.washSeconds = GameBalance.WashSeconds;
            return true;
        }

        private void FinishWash()
        {
            var h = HygieneState;
            h.hygiene = Mathf.Min(100, h.hygiene + GameBalance.WashHygiene[h.kind]);
            LeaveWash(); RefreshHygieneMood();
        }

        private void LeaveWash()
        {
            WashSpot?.Leave(this); WashSpot = null;
            HygieneState.washSeconds = 0;
        }

        private void RefreshHygieneMood()
        {
            var h = HygieneState.hygiene;
            var low = h <= 10 ? GameBalance.VeryLowHygieneMood : h <= GameBalance.WashBelowHygiene ? GameBalance.LowHygieneMood : 0;
            if (low != 0) personalState.AddMood("위생 불량", low, 5); else personalState.moodFactors.RemoveAll(f => f.reason == "위생 불량");
        }
    }
}
