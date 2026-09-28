using System;
using AntColony.Buildings;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    // 피로·수면 상태(2026-09-28 낮밤 기획). 저장은 PersonalState에 들어간다.
    [Serializable] public class CommanderSleepState
    {
        public float fatigue, phaseSeconds, bedSeconds;
        public int roughNights;
        public bool inPhase, asleep, rough, interrupted, poorly;
        public bool Valid => Finite(fatigue) && fatigue >= 0 && fatigue <= 100 && Finite(phaseSeconds) && phaseSeconds >= 0
            && Finite(bedSeconds) && bedSeconds >= 0 && roughNights >= 0;
        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }

    public partial class CommanderAnt
    {
        private CommanderSleepState Sleep => PersonalState.sleep;
        public bool IsNocturnal => traits.Has(CommanderTrait.Nocturnal);
        public bool IsAsleep => Sleep.asleep;
        public bool SleepsRough => Sleep.asleep && Sleep.rough;
        public float Fatigue => Sleep.fatigue;
        public bool SleptPoorly => Sleep.poorly;
        // 작업 속도 공통 배율: 특성 × 잠을 설친 다음날 -20%.
        public float WorkFactor => traits.WorkMultiplier * (Sleep.poorly ? GameBalance.PoorSleepWork : 1f);
        // 밤이 되면 전원 수면, 야행성은 반대로 낮에 잔다.
        public bool IsSleepTime => GameCalendar.IsNight != IsNocturnal;
        private float SleepLength => IsNocturnal ? GameCalendar.DaySeconds : GameCalendar.SecondsPerDay - GameCalendar.DaySeconds;

        // TickDuty 앞에서 부른다. true면 이번 틱은 잠자리 처리로 끝낸다.
        private bool TickSleep(float seconds)
        {
            var s = Sleep;
            if (IsSleepTime && !s.inPhase) { s.inPhase = true; s.phaseSeconds = s.bedSeconds = 0; s.interrupted = s.rough = false; }
            else if (!IsSleepTime && s.inPhase) { s.inPhase = false; WakeUp(); }

            if (!IsSleepTime)
            {
                if ((IsWorking || LabUpgradeBusy || ServiceTarget != null || CraftingWorkshop != null || HuntTarget != null) && !traits.Has(CommanderTrait.Workaholic)) s.fatigue += GameBalance.FatiguePerWorkSecond * seconds;
                else if (WorkState.resting) s.fatigue -= GameBalance.FatigueRestPerSecond * (RestRoom.Serves(this) ? 2 : 1) * seconds;
                s.fatigue = Mathf.Clamp(s.fatigue, 0, 100);
                return false;
            }

            s.phaseSeconds += seconds;
            if (!IsDeployed && !IsAwayFromHome && !IsCarrying && (ScienceAssignment != null || CraftingWorkshop != null || ServiceTarget != null || HuntTarget != null)) CommandStop();
            var canSleep = !IsDeployed && !IsAwayFromHome && !IsEmbarked && !IsCaptive && CanReceiveOrders && !LabUpgradeBusy && !IsCarrying;
            if (!canSleep)
            {
                if (s.asleep) { s.asleep = false; s.interrupted = true; }
                return personalState.treating; // 입원 중인 장수는 병상에서 쉰다.
            }
            if (!s.asleep)
            {
                s.asleep = true;
                SuspendWork(); ScienceAssignment?.ReleaseResearcher(); CraftingWorkshop?.Release();
                automaticFacility = null; WorkState.resting = false;
            }
            var dorm = Dormitory.Assign(this);
            s.rough = dorm == null;
            if (dorm != null && (dorm.Position - Position).sqrMagnitude > 36)
            {
                if ((IsFlying ? HasReachedDestination() : !Agent.pathPending && !Agent.hasPath)
                    && UnityEngine.AI.NavMesh.SamplePosition(dorm.Position, out var hit, 6, UnityEngine.AI.NavMesh.AllAreas) && CanReach(hit.position))
                    base.CommandMove(hit.position);
                if ((IsFlying ? !HasReachedDestination() : Agent.pathPending || Agent.hasPath)) return true;
                s.rough = true;
            }
            // 숙소 한 밤 = 완전 회복(90% 이상 자면). 노숙은 절반 속도.
            if (!s.rough) s.bedSeconds += seconds;
            s.fatigue = Mathf.Max(0, s.fatigue - 100f / (GameBalance.FullSleepShare * SleepLength) * (s.rough ? .5f : 1f) * seconds);
            return true;
        }

        private void WakeUp()
        {
            var s = Sleep;
            var slept = s.asleep || s.bedSeconds > 0;
            var full = !s.rough && !s.interrupted && s.bedSeconds >= GameBalance.FullSleepShare * SleepLength;
            if (full) s.fatigue = 0;
            s.poorly = !full;
            if (slept && s.rough)
            {
                personalState.AddMood("노숙", GameBalance.RoughSleepMood, GameCalendar.SecondsPerDay);
                if (++s.roughNights % GameBalance.RoughSleepNights == 0) traits.ChangeLoyalty(GameBalance.RoughSleepLoyalty, "연속 노숙");
            }
            else if (slept) s.roughNights = 0;
            if (Dormitory.Of(this) is Dormitory dorm && dorm.LivesWithRival(this))
                personalState.AddMood("라이벌과 같은 숙소", GameBalance.RivalRoommateMood, GameCalendar.SecondsPerDay);
            s.asleep = false;
        }

        // 잠든 장수를 징집소에서 고르면 바로 깨어 출전한다. 대신 잠을 설친다.
        private void WakeForDuty()
        {
            if (!Sleep.asleep) return;
            Sleep.asleep = false; Sleep.interrupted = true;
            personalState.AddMood("잠 설침", GameBalance.BadSleepMood, GameCalendar.SecondsPerDay);
        }
    }
}
