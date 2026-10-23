using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    // 위생 욕구(2026-10-05): 시간이 지나면 줄고, 낮아지면 위생 가구(화장실·세면대·샤워기)에 가서 채운다.
    // 시설을 쓸 수 없으면 임시 용변으로 최소 위생을 유지한다.
    [Serializable] public class CommanderHygieneState
    {
        public float hygiene = 100, washSeconds, retrySeconds;
        public int kind;
        public bool temporary;
        public int travelDay = -1;
        public bool Valid => Finite(hygiene) && hygiene >= 0 && hygiene <= 100 && Finite(washSeconds) && washSeconds >= 0
            && Finite(retrySeconds) && retrySeconds >= 0 && kind >= 0 && kind < 3 && travelDay >= -1;
        public void Tick(float seconds, bool travelling, int day)
        {
            hygiene = Mathf.Max(0, hygiene - GameBalanceNeeds.HygienePerSecond * seconds);
            if (travelling && travelDay != day)
            {
                travelDay = day;
                hygiene = Mathf.Max(hygiene, GameBalanceNeeds.TemporaryCap);
            }
        }
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
            if (IsDeployed || IsAwayFromHome || IsEmbarked || IsCaptive || WorkPriorities.Red) { LeaveWash(); return false; }
            if (h.washSeconds > 0)
            {
                if (!CanReceiveOrders) { LeaveWash(); return false; }
                if (!h.temporary && WashSpot != null && (WashSpot.IsDead || !WashSpot.isActiveAndEnabled))
                { LeaveWash(); return false; }
                h.washSeconds = Mathf.Max(0, h.washSeconds - seconds);
                if (h.washSeconds == 0) FinishWash();
                return true;
            }
            if (h.retrySeconds > 0) { h.retrySeconds = Mathf.Max(0, h.retrySeconds - seconds); return false; }
            if (h.hygiene > GameBalance.WashBelowHygiene || IsCarrying || LabUpgradeBusy) return false;
            // ponytail: 매 틱 가장 가까운 빈 가구를 찾는다. 가구가 많아지면 목표를 캐시한다.
            var spot = HygieneFixture.All.Where(s => s != null && s.Free && s.CanUse)
                .OrderBy(s => (s.Position - Position).sqrMagnitude).FirstOrDefault();
            if (IsWorking || ServiceTarget != null || HuntTarget != null || ScienceAssignment != null || CraftingWorkshop != null) CommandStop();
            if (!CanReceiveOrders) return false;
            if (spot == null) return StartTemporaryWash();
            if ((spot.Position - Position).sqrMagnitude > 49)
            {
                var moving = IsFlying ? !HasReachedDestination() : Agent.pathPending || Agent.hasPath;
                if (!moving && UnityEngine.AI.NavMesh.SamplePosition(spot.Position, out var hit, 7, UnityEngine.AI.NavMesh.AllAreas) && CanReach(hit.position))
                { base.CommandMove(hit.position); return true; }
                if (moving) return true;
                return StartTemporaryWash();
            }
            if (!spot.Join(this)) return false;
            WashSpot = spot; h.kind = spot.KindIndex; h.temporary = false;
            h.washSeconds = h.kind == 0 ? GameBalanceNeeds.ToiletSeconds : GameBalance.WashSeconds;
            return true;
        }

        private bool StartTemporaryWash()
        {
            if (Hygiene >= GameBalanceNeeds.TemporaryCap) return false;
            StopMoving(); HygieneState.temporary = true;
            HygieneState.washSeconds = GameBalanceNeeds.ToiletSeconds;
            return true;
        }

        private void FinishWash()
        {
            var h = HygieneState;
            if (h.temporary)
            {
                h.hygiene = Mathf.Max(h.hygiene, GameBalanceNeeds.TemporaryCap);
                personalState.AddMood("임시 용변", -3, GameCalendar.SecondsPerDay);
                h.retrySeconds = GameBalance.StarveRetrySeconds;
            }
            else h.hygiene = GameBalanceNeeds.Washed(h.hygiene, h.kind);
            LeaveWash(); RefreshHygieneMood();
        }

        private void LeaveWash()
        {
            WashSpot?.Leave(this); WashSpot = null;
            HygieneState.washSeconds = 0; HygieneState.temporary = false;
        }

        private void RefreshHygieneMood()
        {
            var h = HygieneState.hygiene;
            var low = GameBalanceNeeds.HygieneMood(h);
            if (low != 0) personalState.AddMood("위생 불량", low, 5); else personalState.moodFactors.RemoveAll(f => f.reason == "위생 불량");
        }
    }
}
